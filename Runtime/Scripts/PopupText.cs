using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    [Serializable]
    public class PopupTextAnimationSettings
    {
        [Min(0.05f)] public float itemLifetime = 1.5f;
        [Min(0f)] public float riseDistance = 1f;
        [Min(0.01f)] public float riseSpeed = 1f;
        [Range(0.05f, 2f)] public float fadeDuration = 0.5f;
        [Min(0f)] public float horizontalJitter = 0.25f;
        [Min(0f)] public float verticalStackSpacing = 0.35f;
        [Min(0.01f)] public float stackResetSeconds = 0.15f;
        public bool billboard = true;
        public bool useUnscaledTime = true;
    }

    public class PopupText : SingletonBehaviour<PopupText>
    {
        [Header("References")]
        [SerializeField] private GameObject popupTextItemPrefab;
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private Camera worldCamera;

        [Header("Pool")]
        [SerializeField] [Min(0)] private int initialPoolSize = 8;

        [Header("Animation")]
        [SerializeField] private PopupTextAnimationSettings animationSettings = new();

        private readonly Queue<PopupTextItem> _available = new();
        private readonly List<PopupTextItem> _active = new();
        private Vector3 _lastSpawnPosition;
        private float _lastSpawnTime;
        private int _spawnStack;

        protected override void Awake()
        {
            base.Awake();
            EnsureContainer();
            PrewarmPool();
        }

        public void Show(Vector3 worldPosition, string text, Color color)
        {
            if (!TryPrepareShow(worldPosition, out Vector3 spawnPosition))
                return;

            SpawnItem(spawnPosition, text, color);
        }

        public void Show(Transform target, Vector3 worldOffset, string text, Color color)
        {
            if (target == null)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN] PopupText.Show: target transform is null.");
                return;
            }

            Show(target.position + worldOffset, text, color);
        }

        private bool TryPrepareShow(Vector3 worldPosition, out Vector3 spawnPosition)
        {
            spawnPosition = worldPosition;
            if (popupTextItemPrefab == null)
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR] PopupText: popupTextItemPrefab is not assigned.");
                return false;
            }

            EnsureContainer();
            if (itemContainer == null)
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR] PopupText: item container is not available.");
                return false;
            }

            float jitter = animationSettings != null ? animationSettings.horizontalJitter : 0f;
            if (jitter > 0f)
                spawnPosition += new Vector3(UnityEngine.Random.Range(-jitter, jitter), 0f, UnityEngine.Random.Range(-jitter, jitter));

            float stackSpacing = animationSettings != null ? animationSettings.verticalStackSpacing : 0.35f;
            float stackReset = animationSettings != null ? animationSettings.stackResetSeconds : 0.15f;
            if (stackSpacing > 0f)
            {
                float now = Time.unscaledTime;
                if (now - _lastSpawnTime > stackReset || Vector3.SqrMagnitude(spawnPosition - _lastSpawnPosition) > 0.25f)
                    _spawnStack = 0;
                else
                    _spawnStack++;

                spawnPosition += Vector3.up * (_spawnStack * stackSpacing);
                _lastSpawnPosition = spawnPosition;
                _lastSpawnTime = now;
            }

            return true;
        }

        private void SpawnItem(Vector3 worldPosition, string text, Color color)
        {
            PopupTextItem item = GetPooledItem();
            if (item == null)
                return;

            _active.Add(item);
            item.Play(
                worldPosition,
                text,
                color,
                animationSettings,
                ResolveCamera(),
                () => ReturnToPool(item));
        }

        private PopupTextItem GetPooledItem()
        {
            PopupTextItem item;
            while (_available.Count > 0)
            {
                item = _available.Dequeue();
                if (item != null)
                    return item;
            }

            return CreateItem();
        }

        private PopupTextItem CreateItem()
        {
            if (popupTextItemPrefab == null || itemContainer == null)
                return null;

            GameObject instance = Instantiate(popupTextItemPrefab, itemContainer);
            instance.SetActive(false);
            var item = instance.GetComponent<PopupTextItem>();
            if (item == null)
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR] PopupText: popupTextItemPrefab is missing PopupTextItem component.");
                Destroy(instance);
                return null;
            }

            return item;
        }

        private void ReturnToPool(PopupTextItem item)
        {
            if (item == null)
                return;

            _active.Remove(item);
            item.StopAndReset();
            _available.Enqueue(item);
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                PopupTextItem item = CreateItem();
                if (item == null)
                    break;
                _available.Enqueue(item);
            }
        }

        private void EnsureContainer()
        {
            if (itemContainer != null)
                return;

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                var canvasGo = new GameObject("PopupTextCanvas", typeof(RectTransform), typeof(Canvas));
                canvasGo.transform.SetParent(transform, false);
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = ResolveCamera();

                var rect = canvasGo.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100f, 100f);
                rect.localScale = Vector3.one * 0.01f;
            }
            else if (canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = ResolveCamera();
            }

            itemContainer = canvas.transform as RectTransform;
        }

		private Camera ResolveCamera()
		{
			Camera cam = worldCamera;
			if (cam == null && itemContainer != null)
			{
				var canvas = itemContainer.GetComponent<Canvas>();
				if (canvas != null)
					cam = canvas.worldCamera;
			}

			if (cam == null)
				cam = Camera.main;

			if (cam != null && itemContainer != null)
			{
				var canvas = itemContainer.GetComponent<Canvas>();
				if (canvas != null && canvas.worldCamera == null)
					canvas.worldCamera = cam;
			}

			return cam;
		}
    }
}
