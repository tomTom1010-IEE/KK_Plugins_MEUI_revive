using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal class VirtualList : MonoBehaviour
    {
        private readonly List<RowModel> _models = new List<RowModel>();
        private VirtualListViewPool _viewPool;

        internal event Action<int> ViewportAnchorIndexChanged;

        public GameObject EntryTemplate;
        public ScrollRect ScrollRect;

        private bool _dirty;
        private int _lastItemsAboveViewRect;
        private float _lastScrollPosition = float.NaN;
        private float _lastViewportHeight = float.NaN;
        private int _viewportAnchorIndex = -1;
        private int _viewportRestoreVersion;
        private bool _viewportRestorePending;
        private bool _programmaticViewportAnchorPinned;
        private float _programmaticScrollPosition = float.NaN;
        private bool _rangeMutationAnchorPublished;
        private float _rangeMutationScrollPosition = float.NaN;

        private int _paddingBot;
        private int _paddingTop;

        private VerticalLayoutGroup _verticalLayoutGroup;

        public void Initialize()
        {
            if (ScrollRect == null) throw new ArgumentNullException(nameof(ScrollRect));

            _verticalLayoutGroup = ScrollRect.content.GetComponent<VerticalLayoutGroup>();
            if (_verticalLayoutGroup == null) throw new ArgumentNullException(nameof(_verticalLayoutGroup));

            _paddingTop = _verticalLayoutGroup.padding.top;
            _paddingBot = _verticalLayoutGroup.padding.bottom;

            if (EntryTemplate == null) throw new ArgumentNullException(nameof(EntryTemplate));
            _viewPool = new VirtualListViewPool(EntryTemplate);
            Clear();
        }

        internal void EnsureViewportCapacity(float viewportHeight)
        {
            if (_viewPool != null
                && _viewPool.EnsureCapacity(
                    viewportHeight,
                    PanelHeight,
                    _models.Count))
                _dirty = true;
        }

        public void Clear()
        {
            SetList(null);
        }

        public void SetList(IEnumerable<RowModel> items)
        {
            SetList(items, true);
        }

        internal void SetList(
            IEnumerable<RowModel> items,
            bool publishViewportAnchor)
        {
            SuspendRowListeners();
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            _models.Clear();
            if (items != null)
                _models.AddRange(items);

            EnsureViewportCapacity(GetViewportHeight());

            _viewPool.ReleaseCachedFrom(_models.Count);

            _dirty = true;
            UpdateViewportAnchor(true, publishViewportAnchor);
        }

        internal void ReplaceRange(
            int startIndex,
            int removeCount,
            IList<RowModel> replacementRows,
            int anchorFallbackIndex)
        {
            if (startIndex < 0 || startIndex > _models.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (removeCount < 0
                || removeCount > _models.Count - startIndex)
                throw new ArgumentOutOfRangeException(nameof(removeCount));

            var replacementCount = replacementRows == null
                ? 0
                : replacementRows.Count;
            if (removeCount == 0 && replacementCount == 0)
                return;

            SuspendRowListeners();
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();

            var scrollPosition = Mathf.Max(
                0f,
                ScrollRect.content.localPosition.y);
            var topRowIndex = _models.Count == 0
                ? -1
                : Mathf.Clamp(
                    Mathf.FloorToInt(scrollPosition / PanelHeight),
                    0,
                    _models.Count - 1);
            var offsetWithinRow = topRowIndex < 0
                ? 0f
                : scrollPosition - topRowIndex * PanelHeight;
            var nextTopRowIndex = ShiftIndexForRange(
                topRowIndex,
                startIndex,
                removeCount,
                replacementCount,
                anchorFallbackIndex);
            var nextViewportAnchor = ShiftIndexForRange(
                _viewportAnchorIndex,
                startIndex,
                removeCount,
                replacementCount,
                anchorFallbackIndex);

            if (removeCount != 0)
                _models.RemoveRange(startIndex, removeCount);
            if (replacementCount != 0)
                _models.InsertRange(startIndex, replacementRows);

            EnsureViewportCapacity(GetViewportHeight());
            var position = ScrollRect.content.localPosition;
            if (_models.Count == 0)
            {
                position.y = 0f;
                nextViewportAnchor = -1;
            }
            else
            {
                nextTopRowIndex = Mathf.Clamp(
                    nextTopRowIndex,
                    0,
                    _models.Count - 1);
                nextViewportAnchor = Mathf.Clamp(
                    nextViewportAnchor,
                    0,
                    _models.Count - 1);
                var viewport = ScrollRect.viewport != null
                    ? ScrollRect.viewport
                    : ScrollRect.GetComponent<RectTransform>();
                var maximum = Mathf.Max(
                    0f,
                    _models.Count * PanelHeight - viewport.rect.height);
                position.y = Mathf.Clamp(
                    nextTopRowIndex * PanelHeight + offsetWithinRow,
                    0f,
                    maximum);
            }

            ScrollRect.StopMovement();
            ScrollRect.content.localPosition = position;
            _dirty = true;
            _rangeMutationAnchorPublished = true;
            _rangeMutationScrollPosition = position.y;
            SetViewportAnchorIndex(nextViewportAnchor, true, true);
            // A targeted collapse is complete when this method returns: stale
            // child RowViews and their listener graphs must not survive until
            // the next Unity frame. Update consumes the one dirty/layout pass;
            // the normal frame callback then remains on its idle fast path.
            Update();
        }

        private static int ShiftIndexForRange(
            int index,
            int startIndex,
            int removeCount,
            int replacementCount,
            int fallbackIndex)
        {
            if (index < 0 || index < startIndex)
                return index;
            if (removeCount != 0
                && index < startIndex + removeCount)
                return fallbackIndex;
            return index + replacementCount - removeCount;
        }

        internal void ReleaseContent()
        {
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            _models.Clear();
            _viewPool.ReleaseAll();

            // Keep the exact scroll, published anchor, and padding for reopening.
            // Recording the current geometry also makes an accidental Update while
            // hidden a no-op instead of publishing an empty-list anchor.
            _lastScrollPosition = ScrollRect.content.localPosition.y;
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            _lastViewportHeight = viewport.rect.height;
            _dirty = false;
        }

        internal void SuspendRowListeners()
        {
            _viewPool.SuspendListeners();
        }

        private void Update()
        {
            var scrollPosition = ScrollRect.content.localPosition.y;
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            var viewportHeight = viewport.rect.height;
            if (viewportHeight != _lastViewportHeight)
                EnsureViewportCapacity(viewportHeight);
            // Explicit navigation owns the published anchor until the content
            // actually moves again. Virtualization still runs while it is pinned.
            if (_programmaticViewportAnchorPinned
                && !Mathf.Approximately(
                    scrollPosition,
                    _programmaticScrollPosition))
                ClearProgrammaticViewportAnchor();
            var rangeMutationAnchorPublished =
                _rangeMutationAnchorPublished
                && Mathf.Approximately(
                    scrollPosition,
                    _rangeMutationScrollPosition);
            ClearRangeMutationAnchor();
            if (!_dirty
                && scrollPosition == _lastScrollPosition
                && viewportHeight == _lastViewportHeight)
                return;

            _lastScrollPosition = scrollPosition;
            _lastViewportHeight = viewportHeight;
            if (!_programmaticViewportAnchorPinned
                && !rangeMutationAnchorPublished)
            {
                UpdateViewportAnchor(false, !_viewportRestorePending);
            }
            // How many items are not visible in current view
            var offscreenItemCount = Mathf.Max(
                0,
                _models.Count - _viewPool.ActiveCapacity);
            // How many items are above current view rect and not visible
            var itemsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / PanelHeight, 0, offscreenItemCount));

            if (_lastItemsAboveViewRect == itemsAboveViewRect && !_dirty)
                return;

            _lastItemsAboveViewRect = itemsAboveViewRect;
            _dirty = false;

            // Store selected item to preserve selection when moving the list with mouse
            RowModel selectedItem = null;
            if (EventSystem.current != null)
            {
                selectedItem = _viewPool.FindBoundModel(
                    EventSystem.current.currentSelectedGameObject);
            }

            var visibleCount = Mathf.Min(
                _viewPool.ActiveCapacity,
                _models.Count - itemsAboveViewRect);
            var hasEventSystem = EventSystem.current != null;
            for (var index = 0; index < visibleCount; index++)
            {
                var item = _models[itemsAboveViewRect + index];
                var boundGameObject = _viewPool.Bind(index, item);

                if (hasEventSystem && ReferenceEquals(selectedItem, item))
                    EventSystem.current.SetSelectedGameObject(boundGameObject);
            }

            // Keep the GameObjects pooled, but release every stale model/listener graph.
            _viewPool.ReleaseActiveFrom(visibleCount);

            RecalculateOffsets(
                itemsAboveViewRect,
                _viewPool.ActiveCapacity);

            // Needed after changing _verticalLayoutGroup.padding since it doesn't make the object dirty
            LayoutRebuilder.MarkLayoutForRebuild(_verticalLayoutGroup.GetComponent<RectTransform>());
        }

        private void RecalculateOffsets(
            int itemsAboveViewRect,
            int activeViewCapacity)
        {
            var topOffset = Mathf.RoundToInt(itemsAboveViewRect * PanelHeight);
            _verticalLayoutGroup.padding.top = _paddingTop + topOffset;

            var totalHeight = _models.Count * PanelHeight;
            var cacheEntriesHeight = activeViewCapacity * PanelHeight;
            var trailingHeight = totalHeight - cacheEntriesHeight - topOffset;
            _verticalLayoutGroup.padding.bottom = Mathf.FloorToInt(Mathf.Max(0, trailingHeight) + _paddingBot);
        }

        internal int ViewportAnchorIndex => _viewportAnchorIndex;
        internal bool ViewportAnchorIsProgrammatic =>
            _programmaticViewportAnchorPinned;
        internal int ActiveViewCapacity => _viewPool.ActiveCapacity;
        internal int CachedViewCount => _viewPool.CachedViewCount;

        internal bool TryGetVisibleRowRange(
            out int firstVisibleRowIndex,
            out int lastVisibleRowIndex)
        {
            firstVisibleRowIndex = -1;
            lastVisibleRowIndex = -1;
            if (_models.Count == 0
                || ScrollRect == null
                || ScrollRect.content == null)
                return false;

            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            if (viewport == null)
                return false;

            var viewportHeight = viewport.rect.height;
            if (viewportHeight <= 0f
                || float.IsNaN(viewportHeight)
                || float.IsInfinity(viewportHeight))
                return false;

            var scrollPosition = Mathf.Max(
                0f,
                ScrollRect.content.localPosition.y);
            firstVisibleRowIndex = Mathf.Clamp(
                Mathf.FloorToInt(scrollPosition / PanelHeight),
                0,
                _models.Count - 1);
            const float boundaryEpsilon = 0.001f;
            var visibleBottom = Mathf.Max(
                scrollPosition,
                scrollPosition + viewportHeight - boundaryEpsilon);
            lastVisibleRowIndex = Mathf.Clamp(
                Mathf.FloorToInt(visibleBottom / PanelHeight),
                firstVisibleRowIndex,
                _models.Count - 1);
            return true;
        }

        internal VirtualListRowAnchorResolver.TopRowAnchor CaptureTopRowAnchor()
        {
            return VirtualListRowAnchorResolver.Capture(
                _models,
                ScrollRect.content.localPosition.y,
                PanelHeight);
        }

        internal void RestoreTopRowAnchor(
            VirtualListRowAnchorResolver.TopRowAnchor anchor,
            bool publishViewportAnchor = true)
        {
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            var restoreVersion = ++_viewportRestoreVersion;
            _viewportRestorePending = true;
            ApplyTopRowAnchor(anchor, publishViewportAnchor);
            StartCoroutine(RestoreTopRowAnchorAfterLayout(anchor, restoreVersion));
        }

        internal void PublishViewportAnchor()
        {
            ViewportAnchorIndexChanged?.Invoke(_viewportAnchorIndex);
        }

        internal void ScrollToIndex(int index)
        {
            _viewportRestoreVersion++;
            _viewportRestorePending = false;
            ClearProgrammaticViewportAnchor();
            ClearRangeMutationAnchor();
            if (_models.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, _models.Count - 1);
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            var maximum = Mathf.Max(
                0f,
                _models.Count * PanelHeight - viewport.rect.height);
            var position = ScrollRect.content.localPosition;
            position.y = Mathf.Clamp(index * PanelHeight, 0f, maximum);
            ScrollRect.StopMovement();
            ScrollRect.content.localPosition = position;
            _dirty = true;
            // The requested row is the semantic selection even when the last rows
            // cannot be aligned with the top because the scroll position clamps.
            _programmaticViewportAnchorPinned = true;
            _programmaticScrollPosition = position.y;
            SetViewportAnchorIndex(index, true, true);
        }

        private void ClearProgrammaticViewportAnchor()
        {
            _programmaticViewportAnchorPinned = false;
            _programmaticScrollPosition = float.NaN;
        }

        private void ClearRangeMutationAnchor()
        {
            _rangeMutationAnchorPublished = false;
            _rangeMutationScrollPosition = float.NaN;
        }

        private IEnumerator RestoreTopRowAnchorAfterLayout(
            VirtualListRowAnchorResolver.TopRowAnchor anchor,
            int restoreVersion)
        {
            yield return null;
            if (restoreVersion != _viewportRestoreVersion)
                yield break;

            _viewportRestorePending = false;
            ApplyTopRowAnchor(anchor, false);
        }

        private void ApplyTopRowAnchor(
            VirtualListRowAnchorResolver.TopRowAnchor anchor,
            bool publishViewportAnchor)
        {
            if (_models.Count == 0)
            {
                var emptyPosition = ScrollRect.content.localPosition;
                emptyPosition.y = 0f;
                ScrollRect.StopMovement();
                ScrollRect.content.localPosition = emptyPosition;
                _dirty = true;
                UpdateViewportAnchor(true, publishViewportAnchor);
                return;
            }

            if (anchor == null)
            {
                if (publishViewportAnchor)
                    PublishViewportAnchor();
                return;
            }

            var targetIndex = VirtualListRowAnchorResolver.ResolveRestoreIndex(
                anchor,
                _models);
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            var maximum = Mathf.Max(
                0f,
                _models.Count * PanelHeight - viewport.rect.height);
            var position = ScrollRect.content.localPosition;
            position.y = Mathf.Clamp(
                targetIndex * PanelHeight + anchor.OffsetWithinRow,
                0f,
                maximum);
            ScrollRect.StopMovement();
            ScrollRect.content.localPosition = position;
            _dirty = true;
            UpdateViewportAnchor(true, publishViewportAnchor);
        }

        private void UpdateViewportAnchor(bool force, bool publish = true)
        {
            int next;
            if (_models.Count == 0)
            {
                next = -1;
            }
            else
            {
                var viewport = ScrollRect.viewport != null
                    ? ScrollRect.viewport
                    : ScrollRect.GetComponent<RectTransform>();
                var scrollPosition = Mathf.Max(0f, ScrollRect.content.localPosition.y);
                var anchorPosition = scrollPosition + viewport.rect.height * 0.3f;
                next = Mathf.Clamp(
                    Mathf.FloorToInt(anchorPosition / PanelHeight),
                    0,
                    _models.Count - 1);
            }

            SetViewportAnchorIndex(next, force, publish);
        }

        private void SetViewportAnchorIndex(
            int next,
            bool force,
            bool publish)
        {
            if (!force && next == _viewportAnchorIndex)
                return;

            _viewportAnchorIndex = next;
            if (publish)
                ViewportAnchorIndexChanged?.Invoke(next);
        }

        public void SelectFirstItem()
        {
            _viewPool.SelectFirst();
        }

        private float GetViewportHeight()
        {
            var viewport = ScrollRect.viewport != null
                ? ScrollRect.viewport
                : ScrollRect.GetComponent<RectTransform>();
            return viewport.rect.height;
        }
    }

}
