#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
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
	/// Survives the domain reload triggered by inserting new fields into a script: after Unity
	/// recompiles, resolves the captured GlobalObjectIds and assigns the object references that
	/// couldn't be wired before the new fields existed.
	/// </summary>
	internal static class PendingFieldAttach
	{
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
			File.WriteAllText(PendingPath, JsonUtility.ToJson(payload));
		}

		private const int MaxPendingAttempts = 600;
		private static int pendingAttempts;

		[InitializeOnLoadMethod]
		private static void RegisterProcessPending()
		{
			pendingAttempts = 0;
			EditorApplication.update += PollForPendingFile;
		}

		private static void PollForPendingFile()
		{
			string path = PendingPath;
			if (!File.Exists(path))
			{
				pendingAttempts = 0;
				return;
			}

			if (ProcessPending(path))
			{
				pendingAttempts = 0;
				EditorApplication.update -= PollForPendingFile;
				return;
			}

			pendingAttempts++;
			if (pendingAttempts >= MaxPendingAttempts)
			{
				EditorApplication.update -= PollForPendingFile;
				string failedPath = path + ".failed";
				try
				{
					if (File.Exists(failedPath))
						File.Delete(failedPath);
					File.Move(path, failedPath);
				}
				catch
				{
					try { File.Delete(path); } catch { }
				}
				Debug.LogWarning(
					$"[UIWidgets] UIRefBinder gave up wiring pending references after {MaxPendingAttempts} attempts. " +
					$"Renamed to '{failedPath}'.");
			}
		}

		private static bool ProcessPending(string path)
		{
			bool attached = false;
			try
			{
				if (!File.Exists(path))
					return false;

				string json = File.ReadAllText(path);
				var payload = JsonUtility.FromJson<PendingFieldAttachPayload>(json);
				if (payload == null || string.IsNullOrEmpty(payload.targetComponentGlobalId))
					return false;

				if (!GlobalObjectId.TryParse(payload.targetComponentGlobalId, out GlobalObjectId targetId))
					return false;

				UnityEngine.Object targetObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(targetId);
				if (targetObj is not Component targetComponent)
					return false;

				var so = new SerializedObject(targetComponent);
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
						if (prop != null)
							prop.objectReferenceValue = compObj;
					}
					so.ApplyModifiedProperties();
				}
				attached = true;
			}
			finally
			{
				if (attached && File.Exists(path))
					File.Delete(path);
			}
			return attached;
		}
	}
}
#endif
