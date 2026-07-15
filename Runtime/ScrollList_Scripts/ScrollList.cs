using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.FrameworkInspector;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    [AddComponentMenu("UI (Canvas)/ScrollList")]
    public class ScrollList : MonoBehaviour, ISerializationCallbackReceiver
    {
        #region Events
        public event Action<IListItemBinder, int> OnItemBound = delegate { };
        public event Action<IListItemBinder, int> OnItemUnbound = delegate { };
        public event Action<ScrollList> OnListRefreshed = delegate { };
        #endregion

        #region Public Configuration
        [SerializeField] private bool horizontalScroll = true;
        [SerializeField] private bool verticalScroll = true;
        [SerializeField] private bool invertScrollDirection = false;

        [SerializeField] [Required] private ScrollRect scrollRect;
        [SerializeField] [Required] private Transform content;
        [SerializeField] [Required] private GameObject itemPrefab;
        [SerializeField] private Transform poolTransform;

        [SerializeField] private bool enableSorting = true;
        [SerializeField] private bool enableFiltering = true;

        [SerializeField] private bool usePooling = true;
        [SerializeField] [ShowIf("usePooling")] private int initialPoolSize = 10;
        [SerializeField] [ShowIf("usePooling")] private int maxPoolSize = 50;

        [Header("Virtualization")]
        [SerializeField] private bool useVirtualization = false;
        [SerializeField] [ShowIf("useVirtualization")] private float itemHeight = 0f;
        [SerializeField] [ShowIf("useVirtualization")] private float itemWidth = 0f;
        [SerializeField] [ShowIf("useVirtualization")] private int virtualizationBuffer = 2;
        #endregion

        #region Private Fields
        private readonly List<GameObject> items = new();
        private readonly Queue<GameObject> pooledItems = new();
        private readonly Dictionary<GameObject, object> itemDataMap = new();
        private readonly List<ScrollItemView> itemComponents = new();

        private bool isInitialized = false;
        private bool isRefreshing = false;
        private Coroutine refreshCoroutine;
        private Coroutine scrollCoroutine;
        private string currentFilter = string.Empty;
        private Comparison<object> currentSortComparison;
        private bool isAscending = true;

        private object _dataSourceSubscribed;
        private Action _unsubscribeDataSource;

        private List<object> _virtualData;
        private readonly Dictionary<int, GameObject> _virtualIndexToRow = new();
        private float _virtualItemHeight;
        private float _virtualItemWidth;
        private bool _virtualMeasuredSize;
        private UnityEngine.Events.UnityAction<Vector2> _virtualScrollHandler;

        private RectTransform contentRectTransform;
        private RectTransform scrollRectTransform;
        #endregion

        #region Public Properties
        [ShowInInspector, ReadOnly]
        public int ItemCount => _virtualData != null ? _virtualData.Count : items.Count;
        public bool UseVirtualization { get => useVirtualization; set => useVirtualization = value; }
        public float VirtualItemHeight => _virtualItemHeight;
        public float VirtualItemWidth => _virtualItemWidth;

        [ShowInInspector, ReadOnly]
        public bool IsRefreshing => isRefreshing;

        [ShowInInspector, ReadOnly]
        public string CurrentFilter => currentFilter;

        public bool HorizontalScroll
        {
            get => horizontalScroll;
            set { horizontalScroll = value; UpdateScrollConfiguration(); }
        }

        public bool VerticalScroll
        {
            get => verticalScroll;
            set { verticalScroll = value; UpdateScrollConfiguration(); }
        }

        public bool InvertScrollDirection
        {
            get => invertScrollDirection;
            set { invertScrollDirection = value; UpdateScrollConfiguration(); }
        }

        public bool UsePooling
        {
            get => usePooling;
            set
            {
                if (usePooling != value)
                {
                    usePooling = value;
                    if (usePooling && !isInitialized) InitializePool();
                    else if (!usePooling) ClearPool();
                }
            }
        }

        public int InitialPoolSize
        {
            get => initialPoolSize;
            set
            {
                initialPoolSize = Mathf.Max(value, 1);
                if (usePooling && isInitialized) { ClearPool(); InitializePool(); }
            }
        }

        public int MaxPoolSize
        {
            get => maxPoolSize;
            set => maxPoolSize = Mathf.Max(value, 1);
        }

        public bool EnableSorting { get => enableSorting; set => enableSorting = value; }
        public bool EnableFiltering { get => enableFiltering; set => enableFiltering = value; }

        public GameObject ItemPrefab
        {
            get => itemPrefab;
            set
            {
                if (itemPrefab != value)
                {
                    itemPrefab = value;
                    if (usePooling && isInitialized) { ClearPool(); InitializePool(); }
                }
            }
        }
        #endregion

        #region Inspector Attributes
        [FoldoutGroup("Runtime Controls")]
        [Button("Update List Settings", ButtonSizes.Medium)]
        private void UpdateListSettings()
        {
            if (!isInitialized)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Warning("[ScrollList] {Name}: Not initialized.", name);
                return;
            }
            UpdateScrollConfiguration();
            Refresh();
        }

        [FoldoutGroup("Runtime Controls")]
        [Button("Clear All Items", ButtonSizes.Medium)]
        private void ClearAllItemsButton() => ClearAllItems();

        [FoldoutGroup("Runtime Controls")]
        [Button("Refresh List", ButtonSizes.Medium)]
        private void RefreshListButton() => Refresh();

        [FoldoutGroup("Runtime Controls")]
        [Button("Open List", ButtonSizes.Small)]
        private void OpenListButton() => Open();

        [FoldoutGroup("Runtime Controls")]
        [Button("Close List", ButtonSizes.Small)]
        private void CloseListButton() => Close();
        #endregion

        #region Unity Lifecycle
        private void Awake() => Initialize();

        private void OnEnable()
        {
            if (isInitialized) EnableScroll();
        }

        private void OnDisable()
        {
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
            UnsubscribeFromDataSource();
            UnregisterVirtualScroll();
            ReturnAllVirtualRowsToPool();
            ClearAllItems();
            ClearPool();
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (isInitialized) return;

            if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (content == null && scrollRect != null) content = scrollRect.content;

            if (poolTransform == null)
            {
                var poolGO = new GameObject($"{name}_Pool");
                poolTransform = poolGO.transform;
                poolTransform.SetParent(transform, false);
                poolGO.SetActive(false);
            }

            if (content != null) contentRectTransform = content.GetComponent<RectTransform>();
            if (scrollRect != null) scrollRectTransform = scrollRect.GetComponent<RectTransform>();

            if (scrollRect == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ScrollList] {Name}: ScrollRect not found.", name);
                return;
            }
            if (content == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ScrollList] {Name}: Content transform not found.", name);
                return;
            }
            if (itemPrefab == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ScrollList] {Name}: ItemPrefab not assigned.", name);
                return;
            }

            if (usePooling) InitializePool();
            UpdateScrollConfiguration();
            isInitialized = true;
        }

        private void InitializePool()
        {
            if (itemPrefab == null || poolTransform == null) return;
            int poolSize = Mathf.Max(initialPoolSize, 5);
            for (int i = 0; i < poolSize; i++)
            {
                var go = Instantiate(itemPrefab, poolTransform);
                go.SetActive(false);
                pooledItems.Enqueue(go);
            }
        }

        private void UpdateScrollConfiguration()
        {
            if (scrollRect == null) return;
            scrollRect.horizontal = horizontalScroll;
            scrollRect.vertical = verticalScroll;
            scrollRect.verticalNormalizedPosition = invertScrollDirection ? 0f : 1f;
        }

        private void EnableScroll()
        {
            if (!isInitialized) { Initialize(); return; }
            UpdateScrollConfiguration();
        }
        #endregion

        #region Data-binding
        public void SetDataSource<T>(IEnumerable<T> source, bool useVirtualization = false) where T : class
        {
            UnsubscribeFromDataSource();
            UnregisterVirtualScroll();
            ClearAllItems();
            _virtualData = null;
            _virtualIndexToRow.Clear();
            if (source == null) return;
            if (!isInitialized) { Initialize(); if (!isInitialized) return; }
            if (content == null || itemPrefab == null) return;

            if (useVirtualization && !this.useVirtualization)
                DebugX.Builder(LogChannels.UI).WithContext(this).Warning("[ScrollList] {Name}: SetDataSource requested virtualization but the UseVirtualization field is disabled; falling back to non-virtual list.", name);

            if (useVirtualization && this.useVirtualization)
            {
                _virtualData = source.Cast<object>().ToList();
                EnsureVirtualItemSize();
                SetVirtualContentSize(_virtualData.Count);
                RegisterVirtualScroll();
                RefreshVirtualVisibleRange();
                return;
            }

            int index = 0;
            foreach (var data in source)
            {
                var row = GetFromPoolOrInstantiate();
                if (row == null) continue;
                var binder = BindRow(row, data, index);
                row.SetActive(true);
                items.Add(row);
                itemComponents.Add(row.GetComponent<ScrollItemView>());
                if (data != null) itemDataMap[row] = data;
                OnItemBound?.Invoke(binder, index);
                index++;
            }
        }

        public void SetDataSource<T>(ObservableList<T> source, bool useVirtualization = false) where T : class
        {
            UnsubscribeFromDataSource();
            UnregisterVirtualScroll();
            ClearAllItems();
            _virtualData = null;
            _virtualIndexToRow.Clear();
            if (source == null) return;
            if (!isInitialized) { Initialize(); if (!isInitialized) return; }
            if (content == null || itemPrefab == null) return;

            if (useVirtualization && this.useVirtualization)
            {
                _virtualData = new List<object>(source.Count);
                for (int i = 0; i < source.Count; i++) _virtualData.Add(source[i]);
                _dataSourceSubscribed = source;
                EnsureVirtualItemSize();
                SetVirtualContentSize(_virtualData.Count);
                RegisterVirtualScroll();

                void OnVirtualAdded(T item)
                {
                    int insertedAt = source.IndexOf(item);
                    if (insertedAt < 0) insertedAt = _virtualData.Count;
                    if (insertedAt >= _virtualData.Count) _virtualData.Add(item);
                    else _virtualData.Insert(insertedAt, item);
                    SetVirtualContentSize(_virtualData.Count);
                    ReturnAllVirtualRowsToPool();
                    RefreshVirtualVisibleRange();
                }
                void OnVirtualRemoved(T item)
                {
                    int i = _virtualData.IndexOf(item);
                    if (i >= 0) { _virtualData.RemoveAt(i); ReturnAllVirtualRowsToPool(); SetVirtualContentSize(_virtualData.Count); RefreshVirtualVisibleRange(); }
                }
                void OnVirtualCleared() { ReturnAllVirtualRowsToPool(); _virtualData.Clear(); SetVirtualContentSize(0); }
                source.ItemAdded += OnVirtualAdded;
                source.ItemRemoved += OnVirtualRemoved;
                source.Cleared += OnVirtualCleared;
                _unsubscribeDataSource = () =>
                {
                    source.ItemAdded -= OnVirtualAdded;
                    source.ItemRemoved -= OnVirtualRemoved;
                    source.Cleared -= OnVirtualCleared;
                };
                RefreshVirtualVisibleRange();
                return;
            }

            _dataSourceSubscribed = source;

            void SyncAll()
            {
                ClearAllItems();
                for (int i = 0; i < source.Count; i++)
                {
                    var data = source[i];
                    var row = GetFromPoolOrInstantiate();
                    if (row == null) continue;
                    var binder = BindRow(row, data, i);
                    row.SetActive(true);
                    items.Add(row);
                    itemComponents.Add(row.GetComponent<ScrollItemView>());
                    if (data != null) itemDataMap[row] = data;
                    OnItemBound?.Invoke(binder, i);
                }
            }

            void OnAdded(T item)
            {
                int i = source.IndexOf(item);
                // Insert / indexer-set raise ItemAdded for a non-last item; the
                // append-only fast path below is only valid for a true append.
                if (i != source.Count - 1)
                {
                    SyncAll();
                    return;
                }
                var data = source[i];
                var row = GetFromPoolOrInstantiate();
                if (row == null) return;
                var binder = BindRow(row, data, i);
                row.SetActive(true);
                items.Add(row);
                itemComponents.Add(row.GetComponent<ScrollItemView>());
                if (data != null) itemDataMap[row] = data;
                OnItemBound?.Invoke(binder, i);
            }

            void OnRemoved(T item)
            {
                int idx = items.FindIndex(go => itemDataMap.TryGetValue(go, out var d) && ReferenceEquals(d, item));
                if (idx >= 0) RemoveItem(items[idx]);
            }

            void OnCleared() => ClearAllItems();

            source.ItemAdded += OnAdded;
            source.ItemRemoved += OnRemoved;
            source.Cleared += OnCleared;
            _unsubscribeDataSource = () =>
            {
                source.ItemAdded -= OnAdded;
                source.ItemRemoved -= OnRemoved;
                source.Cleared -= OnCleared;
            };

            SyncAll();
        }

        private IListItemBinder BindRow(GameObject row, object data, int index)
        {
            var binder = row.GetComponent<IListItemBinder>();
            if (binder == null)
                throw new InvalidOperationException(
                    $"[ScrollList] Prefab '{row.name}' has no IListItemBinder component. " +
                    $"Item view must extend ScrollItemView<T>.");
            binder.BindRaw(data, index);
            return binder;
        }

        private void UnsubscribeFromDataSource()
        {
            _unsubscribeDataSource?.Invoke();
            _unsubscribeDataSource = null;
            _dataSourceSubscribed = null;
        }
        #endregion

        #region Virtualization
        private bool VirtualIsVertical => verticalScroll;
        private float VirtualItemSize => VirtualIsVertical ? _virtualItemHeight : _virtualItemWidth;

        private void EnsureVirtualItemSize()
        {
            if (itemPrefab == null || content == null) return;
            if (!_virtualMeasuredSize)
            {
                var measure = Instantiate(itemPrefab, content);
                measure.SetActive(false);
                var rt = measure.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (_virtualItemHeight <= 0f) _virtualItemHeight = rt.rect.height;
                    if (_virtualItemWidth <= 0f) _virtualItemWidth = rt.rect.width;
                }
                if (measure.TryGetComponent<LayoutElement>(out var le))
                {
                    if (_virtualItemHeight <= 0f) _virtualItemHeight = le.preferredHeight;
                    if (_virtualItemWidth <= 0f) _virtualItemWidth = le.preferredWidth;
                }
                if (_virtualItemHeight <= 0f) _virtualItemHeight = 64f;
                if (_virtualItemWidth <= 0f) _virtualItemWidth = 64f;
                DestroyObject(measure);
                _virtualMeasuredSize = true;
            }
            if (itemHeight > 0f) _virtualItemHeight = itemHeight;
            if (itemWidth > 0f) _virtualItemWidth = itemWidth;
        }

        private void SetVirtualContentSize(int logicalCount)
        {
            if (contentRectTransform == null) return;
            var size = contentRectTransform.sizeDelta;
            if (VirtualIsVertical) size.y = _virtualItemHeight * Mathf.Max(0, logicalCount);
            else size.x = _virtualItemWidth * Mathf.Max(0, logicalCount);
            contentRectTransform.sizeDelta = size;
        }

        private void RegisterVirtualScroll()
        {
            if (scrollRect == null || _virtualScrollHandler != null) return;
            _virtualScrollHandler = _ => RefreshVirtualVisibleRange();
            scrollRect.onValueChanged.AddListener(_virtualScrollHandler);
        }

        private void UnregisterVirtualScroll()
        {
            if (scrollRect != null && _virtualScrollHandler != null)
            {
                scrollRect.onValueChanged.RemoveListener(_virtualScrollHandler);
                _virtualScrollHandler = null;
            }
        }

        private void GetVisibleRange(out int first, out int last)
        {
            first = 0; last = -1;
            float itemSize = VirtualItemSize;
            if (_virtualData == null || _virtualData.Count == 0 || contentRectTransform == null || scrollRect == null || itemSize <= 0f) return;
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRectTransform;
            bool vert = VirtualIsVertical;
            float viewportSize = viewport != null ? (vert ? viewport.rect.height : viewport.rect.width) : 400f;
            int count = _virtualData.Count;
            float contentSize = vert ? contentRectTransform.rect.height : contentRectTransform.rect.width;
            if (contentSize <= 0f) return;
            float scrollOffset = vert ? -contentRectTransform.anchoredPosition.y : -contentRectTransform.anchoredPosition.x;
            if (invertScrollDirection) scrollOffset = contentSize - viewportSize - scrollOffset;
            int firstIndex = Mathf.FloorToInt(scrollOffset / itemSize);
            int lastIndex = Mathf.Min(count - 1, firstIndex + Mathf.CeilToInt(viewportSize / itemSize) - 1);
            first = Mathf.Max(0, firstIndex - virtualizationBuffer);
            last = Mathf.Min(count - 1, lastIndex + virtualizationBuffer);
        }

        private void RefreshVirtualVisibleRange()
        {
            if (_virtualData == null || content == null) return;
            GetVisibleRange(out int first, out int last);

            var toRecycle = new List<int>();
            foreach (var kv in _virtualIndexToRow)
                if (kv.Key < first || kv.Key > last) toRecycle.Add(kv.Key);
            foreach (var idx in toRecycle)
            {
                if (!_virtualIndexToRow.TryGetValue(idx, out var go)) continue;
                UnbindAndReturnVirtualRow(go, idx);
                _virtualIndexToRow.Remove(idx);
            }

            for (int i = first; i <= last; i++)
            {
                if (_virtualIndexToRow.ContainsKey(i)) continue;
                var row = GetFromPoolOrInstantiate();
                if (row == null) continue;
                var binder = BindRow(row, _virtualData[i], i);
                row.SetActive(true);
                PositionVirtualRow(row, i);
                items.Add(row);
                itemComponents.Add(row.GetComponent<ScrollItemView>());
                itemDataMap[row] = _virtualData[i];
                _virtualIndexToRow[i] = row;
                OnItemBound?.Invoke(binder, i);
            }
        }

        private void UnbindAndReturnVirtualRow(GameObject row, int index)
        {
            var binder = row.GetComponent<IListItemBinder>();
            OnItemUnbound?.Invoke(binder, index);
            binder?.Unbind();
            items.Remove(row);
            itemDataMap.Remove(row);
            var itemComp = row.GetComponent<ScrollItemView>();
            if (itemComp != null) itemComponents.Remove(itemComp);
            ReturnToPool(row);
        }

        private void ReturnAllVirtualRowsToPool()
        {
            foreach (var kv in _virtualIndexToRow)
            {
                if (kv.Value != null) UnbindAndReturnVirtualRow(kv.Value, kv.Key);
            }
            _virtualIndexToRow.Clear();
        }

        private void PositionVirtualRow(GameObject row, int index)
        {
            var rt = row.GetComponent<RectTransform>();
            if (rt == null) return;
            if (VirtualIsVertical)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -index * _virtualItemHeight);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, _virtualItemHeight);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(index * _virtualItemWidth, 0f);
                rt.sizeDelta = new Vector2(_virtualItemWidth, rt.sizeDelta.y);
            }
        }
        #endregion

        #region Item Operations
        public List<T> GetItems<T>() where T : ScrollItemView
        {
            var result = new List<T>(itemComponents.Count);
            for (int i = 0; i < itemComponents.Count; i++)
                if (itemComponents[i] is T typed) result.Add(typed);
            return result;
        }

        public List<ScrollItemView> GetAllItemComponents()
        {
            var result = new List<ScrollItemView>(itemComponents.Count);
            for (int i = 0; i < itemComponents.Count; i++)
                if (itemComponents[i] != null) result.Add(itemComponents[i]);
            return result;
        }

        public void ClearAllItems()
        {
            if (_virtualIndexToRow != null && _virtualIndexToRow.Count > 0)
                ReturnAllVirtualRowsToPool();

            if (content == null)
            {
                items.Clear();
                itemComponents.Clear();
                itemDataMap.Clear();
                return;
            }

            if (usePooling)
            {
                foreach (var go in items)
                {
                    if (go == null) continue;
                    var binder = go.GetComponent<IListItemBinder>();
                    binder?.Unbind();
                    ReturnToPool(go);
                }
            }
            else
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    var child = content.GetChild(i);
                    if (child != null)
                    {
                        var binder = child.GetComponent<IListItemBinder>();
                        binder?.Unbind();
                        DestroyObject(child.gameObject);
                    }
                }
            }

            items.Clear();
            itemComponents.Clear();
            itemDataMap.Clear();
        }

        public bool RemoveItem(GameObject item)
        {
            if (item == null || !items.Contains(item)) return false;

            int index = items.IndexOf(item);
            var binder = item.GetComponent<IListItemBinder>();

            OnItemUnbound?.Invoke(binder, index);
            binder?.Unbind();

            items.Remove(item);
            itemDataMap.Remove(item);
            var itemComp = item.GetComponent<ScrollItemView>();
            if (itemComp != null) itemComponents.Remove(itemComp);

            if (usePooling) ReturnToPool(item);
            else DestroyObject(item);

            return true;
        }

        public bool RemoveItemAt(int index)
        {
            if (index < 0 || index >= items.Count) return false;
            return RemoveItem(items[index]);
        }

        public GameObject GetItemAt(int index)
        {
            if (_virtualIndexToRow != null && _virtualIndexToRow.TryGetValue(index, out var go)) return go;
            if (index < 0 || index >= items.Count) return null;
            return items[index];
        }

        public object GetItemDataAt(int index)
        {
            if (_virtualData != null && index >= 0 && index < _virtualData.Count) return _virtualData[index];
            var item = GetItemAt(index);
            return item != null && itemDataMap.TryGetValue(item, out var data) ? data : null;
        }

        public void SetItemData(GameObject item, object data)
        {
            if (item == null) return;
            itemDataMap[item] = data;
        }

        public void Close() => gameObject.SetActive(false);
        public void Open() { gameObject.SetActive(true); EnableScroll(); }
        public void SetScroll(bool horizontal, bool vertical) { horizontalScroll = horizontal; verticalScroll = vertical; UpdateScrollConfiguration(); }
        #endregion

        #region Pooling
        private GameObject GetFromPoolOrInstantiate()
        {
            if (!usePooling) return Instantiate(itemPrefab, content);
            if (pooledItems.Count > 0)
            {
                var go = pooledItems.Dequeue();
                go.transform.SetParent(content, false);
                return go;
            }
            return Instantiate(itemPrefab, content);
        }

        private void ReturnToPool(GameObject go)
        {
            if (go == null || poolTransform == null) return;
            if (pooledItems.Count >= maxPoolSize) { DestroyObject(go); return; }
            go.SetActive(false);
            go.transform.SetParent(poolTransform, false);
            pooledItems.Enqueue(go);
        }

        private void ClearPool()
        {
            while (pooledItems.Count > 0)
            {
                var go = pooledItems.Dequeue();
                if (go != null) DestroyObject(go);
            }
        }

        // DestroyImmediate is intended for edit-mode tooling and can corrupt
        // Unity internal state when used at runtime (mid-frame, inside callbacks
        // or while iterating). Use Destroy during play, DestroyImmediate only in
        // the editor (e.g. inspector buttons before entering play mode).
        private static new void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
        #endregion

        #region Filtering & Sorting
        public void FilterItems(string filter, bool searchInTitle = true, bool searchInSubtitle = true, bool caseSensitive = false)
        {
            if (!enableFiltering) return;
            currentFilter = filter ?? string.Empty;
            string searchFilter = caseSensitive ? currentFilter : currentFilter.ToLower();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                var itemComp = item.GetComponent<ScrollItemView>();
                if (itemComp == null) { item.SetActive(true); continue; }

                bool shouldShow = string.IsNullOrEmpty(currentFilter);
                if (!shouldShow)
                {
                    string titleText = caseSensitive ? itemComp.Title : itemComp.Title.ToLower();
                    string subtitleText = caseSensitive ? itemComp.Subtitle : itemComp.Subtitle.ToLower();
                    shouldShow = (searchInTitle && titleText.Contains(searchFilter)) ||
                                 (searchInSubtitle && subtitleText.Contains(searchFilter));
                }
                item.SetActive(shouldShow);
            }
        }

        public void ClearFilter() => FilterItems(string.Empty);

        public void SortItems(Comparison<object> comparison, bool ascending = true)
        {
            if (!enableSorting || comparison == null) return;
            currentSortComparison = comparison;
            isAscending = ascending;

            var withData = new List<(GameObject item, object data, ScrollItemView comp)>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                object data = itemDataMap.TryGetValue(item, out var d) ? d : null;
                withData.Add((item, data, item.GetComponent<ScrollItemView>()));
            }
            withData.Sort((a, b) => ascending ? comparison(a.data, b.data) : comparison(b.data, a.data));
            for (int i = 0; i < withData.Count; i++) withData[i].item.transform.SetSiblingIndex(i);

            items.Clear();
            itemComponents.Clear();
            foreach (var (item, _, comp) in withData)
            {
                items.Add(item);
                if (comp != null) itemComponents.Add(comp);
            }
        }

        public void Refresh()
        {
            if (isRefreshing) return;
            refreshCoroutine = StartCoroutine(RefreshCoroutine());
        }

        private IEnumerator RefreshCoroutine()
        {
            isRefreshing = true;
            yield return null;
            if (contentRectTransform != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            if (!string.IsNullOrEmpty(currentFilter)) FilterItems(currentFilter);
            if (currentSortComparison != null) SortItems(currentSortComparison, isAscending);
            isRefreshing = false;
            refreshCoroutine = null;
            OnListRefreshed?.Invoke(this);
        }
        #endregion

        #region Scroll To
        public void ScrollToItem(int index, bool smooth = true)
        {
            int count = ItemCount;
            if (scrollRect == null || index < 0 || index >= count) return;
            if (!isActiveAndEnabled) return;

            if (_virtualData != null && contentRectTransform != null && scrollRect.viewport != null && VirtualItemSize > 0f)
            {
                bool vert = VirtualIsVertical;
                float contentSize = vert ? contentRectTransform.rect.height : contentRectTransform.rect.width;
                float viewportSize = vert ? scrollRect.viewport.rect.height : scrollRect.viewport.rect.width;
                float maxScroll = Mathf.Max(0f, contentSize - viewportSize);
                float targetOffset = index * VirtualItemSize;
                float normalized = invertScrollDirection
                    ? targetOffset / Mathf.Max(1f, maxScroll)
                    : 1f - targetOffset / Mathf.Max(1f, maxScroll);
                normalized = Mathf.Clamp01(normalized);
                if (smooth) StartScrollCoroutine(SmoothScrollTo(normalized, vert));
                else
                {
                    if (vert) scrollRect.verticalNormalizedPosition = normalized;
                    else scrollRect.horizontalNormalizedPosition = normalized;
                }
                return;
            }

            var item = GetItemAt(index);
            if (item == null) return;
            var itemRect = item.GetComponent<RectTransform>();
            if (itemRect == null) return;
            if (contentRectTransform != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            float normalizedPosition = CalculateNormalizedPositionFromLayout(itemRect);
            if (normalizedPosition < 0f) return;
            if (smooth) StartScrollCoroutine(SmoothScrollTo(normalizedPosition));
            else
            {
                if (verticalScroll) scrollRect.verticalNormalizedPosition = normalizedPosition;
                if (horizontalScroll) scrollRect.horizontalNormalizedPosition = normalizedPosition;
            }
        }

        private float CalculateNormalizedPositionFromLayout(RectTransform itemRect)
        {
            if (contentRectTransform == null || scrollRectTransform == null || scrollRect == null) return -1f;
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRectTransform;
            float contentSize = verticalScroll ? contentRectTransform.rect.height : contentRectTransform.rect.width;
            float viewportSize = verticalScroll ? viewport.rect.height : viewport.rect.width;
            float maxScroll = Mathf.Max(0f, contentSize - viewportSize);
            if (maxScroll <= 0f) return 1f;

            Vector2 itemCenter = itemRect.anchoredPosition;
            Rect itemRectLocal = itemRect.rect;
            float itemStart = verticalScroll
                ? itemCenter.y + itemRectLocal.yMax
                : itemCenter.x + itemRectLocal.xMin;
            float scrollOffset = Mathf.Clamp(verticalScroll ? -itemStart : itemStart, 0f, maxScroll);
            return 1f - scrollOffset / maxScroll;
        }

        private void StartScrollCoroutine(IEnumerator routine)
        {
            // Only one smooth scroll may run at a time; otherwise stacked
            // coroutines fight over scrollRect.*NormalizedPosition each frame.
            if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
            scrollCoroutine = StartCoroutine(routine);
        }

        private IEnumerator SmoothScrollTo(float targetPosition, bool? verticalAxis = null)
        {
            float startTime = Time.time;
            float duration = 0.3f;
            bool useVertical = verticalAxis ?? verticalScroll;
            bool useHorizontal = verticalAxis.HasValue ? !verticalAxis.Value : horizontalScroll;
            float startPosition = useVertical ? scrollRect.verticalNormalizedPosition : scrollRect.horizontalNormalizedPosition;

            while (Time.time - startTime < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, (Time.time - startTime) / duration);
                float current = Mathf.Lerp(startPosition, targetPosition, t);
                if (useVertical) scrollRect.verticalNormalizedPosition = current;
                if (useHorizontal) scrollRect.horizontalNormalizedPosition = current;
                yield return null;
            }
            if (useVertical) scrollRect.verticalNormalizedPosition = targetPosition;
            if (useHorizontal) scrollRect.horizontalNormalizedPosition = targetPosition;
            scrollCoroutine = null;
        }
        #endregion
    }
}
