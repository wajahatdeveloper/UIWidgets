#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	[Serializable]
	internal class PendingFieldAttachEntry
	{
		public string fieldName;
		public string componentGlobalId;
	}

	[Serializable]
	internal class PendingFieldAttachPayload
	{
		public string targetComponentGlobalId;
		public PendingFieldAttachEntry[] fields;
	}

	/// <summary>
	/// Survives the domain reload triggered by inserting new fields into a script. Gates on
	/// compilation actually finishing — via CompilationPipeline.compilationFinished plus an
	/// InitializeOnLoadMethod/delayCall fallback for reloads that already landed before this domain
	/// started listening, mirroring PanelBaseEditor's generate/replace gate — before resolving the
	/// captured GlobalObjectIds and assigning the object references that couldn't be wired before
	/// the new fields existed. A field that fails to resolve (e.g. the new assembly hasn't loaded
	/// yet) is retried rather than silently dropped.
	/// </summary>
	internal static class PendingFieldAttach
	{
		private const string PendingRetryCountKey = "UIWidgets.UIRefBinder.PendingFieldAttach.RetryCount";
		private const int MaxRetries = 20;

		private static string PendingPath => Path.Combine(Application.persistentDataPath, "UIRefBinderPendingAttach.json");

		internal static void Write(Component targetComponent, List<(string fieldName, UnityEngine.Object component)> fields)
		{
			var entries = new PendingFieldAttachEntry[fields.Count];
			for (int i = 0; i < fields.Count; i++)
			{
				entries[i] = new PendingFieldAttachEntry
				{
					fieldName = fields[i].fieldName,
					componentGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(fields[i].component).ToString()
				};
			}

			var payload = new PendingFieldAttachPayload
			{
				targetComponentGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(targetComponent).ToString(),
				fields = entries
			};
			SessionState.EraseInt(PendingRetryCountKey);
			File.WriteAllText(PendingPath, JsonUtility.ToJson(payload));
		}

		[InitializeOnLoadMethod]
		private static void RegisterProcessPending()
		{
			CompilationPipeline.compilationFinished += _ => TryProcessPending();
			// Covers the case where the domain reload that resolves the pending fields already
			// happened by the time this method runs, so no compilationFinished event is coming.
			EditorApplication.delayCall += TryProcessPending;
		}

		private static void TryProcessPending()
		{
			string path = PendingPath;
			if (!File.Exists(path))
				return;

			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				EditorApplication.delayCall += TryProcessPending;
				return;
			}

			if (ProcessPending(path))
			{
				SessionState.EraseInt(PendingRetryCountKey);
				return;
			}

			var retryCount = SessionState.GetInt(PendingRetryCountKey, 0);
			if (retryCount >= MaxRetries)
			{
				Debug.LogWarning($"[UIWidgets] UIRefBinder gave up wiring pending references after {MaxRetries} attempts.");
				File.Delete(path);
				SessionState.EraseInt(PendingRetryCountKey);
				return;
			}

			SessionState.SetInt(PendingRetryCountKey, retryCount + 1);
			EditorApplication.delayCall += TryProcessPending;
		}

		/// <summary>Returns true once every pending field resolved and the file was consumed; false to retry later.</summary>
		private static bool ProcessPending(string path)
		{
			string json;
			try { json = File.ReadAllText(path); }
			catch { return false; }

			var payload = JsonUtility.FromJson<PendingFieldAttachPayload>(json);
			if (payload == null || string.IsNullOrEmpty(payload.targetComponentGlobalId))
			{
				File.Delete(path);
				return true;
			}

			if (!GlobalObjectId.TryParse(payload.targetComponentGlobalId, out GlobalObjectId targetId))
			{
				File.Delete(path);
				return true;
			}

			UnityEngine.Object targetObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(targetId);
			if (targetObj is not Component targetComponent)
				return false;

			var so = new SerializedObject(targetComponent);
			bool allResolved = true;
			if (payload.fields != null)
			{
				foreach (var entry in payload.fields)
				{
					if (string.IsNullOrEmpty(entry.fieldName) || string.IsNullOrEmpty(entry.componentGlobalId))
						continue;
					if (!GlobalObjectId.TryParse(entry.componentGlobalId, out GlobalObjectId cid))
						continue;

					UnityEngine.Object compObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(cid);
					if (compObj == null)
						continue;

					var prop = so.FindProperty(entry.fieldName);
					if (prop == null)
					{
						allResolved = false;
						continue;
					}
					prop.objectReferenceValue = compObj;
				}
				so.ApplyModifiedProperties();
			}

			if (!allResolved)
				return false;

			File.Delete(path);
			return true;
		}
	}
}
#endif
