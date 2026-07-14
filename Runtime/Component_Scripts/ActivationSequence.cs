using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AetherNexus.UIWidgets
{
	public interface IActivatable
	{
		void OnEnter();
		void OnExit();
	}

	public class ActivationSequence : MonoBehaviour
	{
		[Header("Source")]
		[SerializeField] private bool customSequence = false;
		[SerializeField] private bool includeInactiveChildren = false;

		[Header("Behavior")]
		[SerializeField] private bool autoInitialize = true;
		[SerializeField] private bool wrapAround = false;
		[SerializeField] private bool keepOnlyOneActive = true;
		[SerializeField] private bool invokeEndOnlyWhenPastLast = true;

		[Header("Transition")]
		[Tooltip("Optional delay between deactivating the current item and activating the next.")]
		[SerializeField] private float activationDelay = 0f;

		[Header("Sequence")]
		[SerializeField]
		[InspectorName("Sequence (Assets + Scene)")]
		private List<GameObject> sequence = new List<GameObject>();
		[SerializeField] private int currentIndex = 0;

		[Header("Events")]
		public UnityEvent OnSequenceStart;
		public UnityEvent OnSequenceEnd;
		public UnityEvent<int> OnIndexChanged;
		public UnityEvent<int> OnShown;
		public UnityEvent<int> OnHidden;
		public UnityEvent<int, int> OnTransition; // (from, to)

		private Coroutine transitionCoroutine;

		public int CurrentIndex => currentIndex;
		public int Count => sequence == null ? 0 : sequence.Count;
		public bool CanNext => Count > 0 && (wrapAround || currentIndex < Count - 1);
		public bool CanPrevious => Count > 0 && (wrapAround || currentIndex > 0);

		private void OnEnable()
		{
			AutoPopulateIfNeeded();
			ValidateSequence();

			if (!autoInitialize || Count == 0)
			{
				return;
			}

			InitializeState();
			OnSequenceStart?.Invoke();
			OnIndexChanged?.Invoke(currentIndex);
			OnShown?.Invoke(currentIndex);
		}

		private void AutoPopulateIfNeeded()
		{
			if (!customSequence)
			{
				sequence.Clear();
				for (int i = 0; i < transform.childCount; i++)
				{
					var child = transform.GetChild(i).gameObject;
					if (includeInactiveChildren || child.activeSelf)
					{
						sequence.Add(child);
					}
				}
			}
		}

		private void InitializeState()
		{
			if (Count == 0)
			{
				return;
			}

			if (keepOnlyOneActive)
			{
				for (int i = 0; i < Count; i++)
				{
					var go = sequence[i];
					if (go == null) continue;
					bool shouldBeActive = i == 0;
					if (go.activeSelf != shouldBeActive)
					{
						go.SetActive(shouldBeActive);
					}
				}
				currentIndex = 0;
			}
			else
			{
				// Keep existing actives; set currentIndex to first active if any
				int firstActive = -1;
				for (int i = 0; i < Count; i++)
				{
					var go = sequence[i];
					if (go != null && go.activeSelf)
					{
						firstActive = i;
						break;
					}
				}
				currentIndex = firstActive >= 0 ? firstActive : 0;
				var target = sequence[currentIndex];
				if (target != null && !target.activeSelf)
				{
					target.SetActive(true);
				}
			}
		}

		public void Next()
		{
			if (Count == 0)
			{
				return;
			}

			bool atLast = currentIndex == Count - 1;
			if (atLast && !wrapAround)
			{
				if (invokeEndOnlyWhenPastLast)
				{
					// Fire only when attempting to advance past last
					OnSequenceEnd?.Invoke();
					return;
				}
				else
				{
					// Fire upon reaching last and keep it active
					OnSequenceEnd?.Invoke();
					return;
				}
			}

			int target = currentIndex + 1;
			if (target >= Count)
			{
				target = wrapAround ? 0 : Count - 1;
			}
			Show(target);
		}

		public void Previous()
		{
			if (Count == 0)
			{
				return;
			}

			bool atFirst = currentIndex == 0;
			if (atFirst && !wrapAround)
			{
				return;
			}

			int target = currentIndex - 1;
			if (target < 0)
			{
				target = wrapAround ? Count - 1 : 0;
			}
			Show(target);
		}

		public void Show(int index)
		{
			if (Count == 0) return;
			if (index < 0 || index >= Count) return;
			if (index == currentIndex) return;

			if (transitionCoroutine != null)
			{
				StopCoroutine(transitionCoroutine);
			}

			transitionCoroutine = StartCoroutine(ShowRoutine(currentIndex, index));
		}

		private IEnumerator ShowRoutine(int from, int to)
		{
			OnTransition?.Invoke(from, to);

			var fromGo = (from >= 0 && from < Count) ? sequence[from] : null;
			var toGo = (to >= 0 && to < Count) ? sequence[to] : null;

			// Exit current
			if (keepOnlyOneActive && fromGo != null && fromGo.activeSelf)
			{
				var fromActivatables = fromGo.GetComponents<IActivatable>();
				for (int i = 0; i < fromActivatables.Length; i++)
				{
					fromActivatables[i].OnExit();
				}
				if (activationDelay > 0f)
				{
					yield return new WaitForSeconds(activationDelay);
				}
				fromGo.SetActive(false);
				OnHidden?.Invoke(from);
			}

			// Enter target
			if (toGo != null && !toGo.activeSelf)
			{
				toGo.SetActive(true);
			}
			if (toGo != null)
			{
				var toActivatables = toGo.GetComponents<IActivatable>();
				for (int i = 0; i < toActivatables.Length; i++)
				{
					toActivatables[i].OnEnter();
				}
			}
			OnShown?.Invoke(to);

			currentIndex = to;
			OnIndexChanged?.Invoke(currentIndex);

			transitionCoroutine = null;
		}

		public void GoToFirst()
		{
			if (Count > 0)
			{
				Show(0);
			}
		}

		public void GoToLast()
		{
			if (Count > 0)
			{
				Show(Count - 1);
			}
		}

		public void ResetSequence()
		{
			if (transitionCoroutine != null)
			{
				StopCoroutine(transitionCoroutine);
				transitionCoroutine = null;
			}

			InitializeState();
			OnIndexChanged?.Invoke(currentIndex);
			OnShown?.Invoke(currentIndex);
		}

		[ContextMenu("Populate From Children")]
		private void ContextPopulate()
		{
			customSequence = false;
			AutoPopulateIfNeeded();
		}

		[ContextMenu("Validate & Clean Sequence")]
		private void ContextValidate()
		{
			ValidateSequence();
		}

		private void ValidateSequence()
		{
			if (sequence == null)
			{
				sequence = new List<GameObject>();
			}
			else
			{
				for (int i = sequence.Count - 1; i >= 0; i--)
				{
					if (sequence[i] == null)
					{
						sequence.RemoveAt(i);
					}
				}
			}

			if (currentIndex < 0) currentIndex = 0;
			if (currentIndex >= Count) currentIndex = Mathf.Max(0, Count - 1);
		}

		private void OnValidate()
		{
			ValidateSequence();
		}
	}
}