using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal sealed class VirtualListViewPool
    {
        private static readonly bool InstantiateWithParentExists =
            typeof(UnityEngine.Object).GetMethod(
                "Instantiate",
                new[] { typeof(GameObject), typeof(Transform) }) != null;

        private readonly GameObject _entryTemplate;
        private readonly List<RowView> _cachedViews = new List<RowView>();

        internal VirtualListViewPool(GameObject entryTemplate)
        {
            _entryTemplate = entryTemplate
                ?? throw new ArgumentNullException(nameof(entryTemplate));
            SetupEntryTemplate();
        }

        internal int ActiveCapacity { get; private set; }

        internal int CachedViewCount
        {
            get { return _cachedViews.Count; }
        }

        internal bool EnsureCapacity(
            float viewportHeight,
            float rowHeight,
            int modelCount)
        {
            var required = VirtualListCachePolicy.RequiredViewCount(
                viewportHeight,
                rowHeight,
                modelCount);
            while (_cachedViews.Count < required)
                _cachedViews.Add(CreatePooledView());

            if (ActiveCapacity == required)
                return false;

            if (required < ActiveCapacity)
            {
                for (var index = required;
                     index < ActiveCapacity;
                     index++)
                    _cachedViews[index].Release();
            }

            ActiveCapacity = required;
return true;
        }

        internal RowModel FindBoundModel(GameObject selectedGameObject)
        {
            for (var index = 0; index < _cachedViews.Count; index++)
            {
                var cachedEntry = _cachedViews[index];
                if (cachedEntry.gameObject == selectedGameObject)
                    return cachedEntry.CurrentModel;
            }
            return null;
        }

        internal GameObject Bind(int index, RowModel model)
        {
            var cachedEntry = _cachedViews[index];
            cachedEntry.Bind(model, false);
            cachedEntry.SetVisible(true);
            return cachedEntry.gameObject;
        }

        internal void ReleaseActiveFrom(int startIndex)
        {
            for (var index = startIndex; index < ActiveCapacity; index++)
                _cachedViews[index].Release();
        }

        internal void ReleaseCachedFrom(int startIndex)
        {
            for (var index = startIndex; index < _cachedViews.Count; index++)
                _cachedViews[index].Release();
        }

        internal void ReleaseAll()
        {
            for (var index = 0; index < _cachedViews.Count; index++)
                _cachedViews[index].Release();
        }

        internal void SuspendListeners()
        {
            for (var index = 0; index < _cachedViews.Count; index++)
                _cachedViews[index].SuspendListeners();
        }

        internal void SelectFirst()
        {
            if (ActiveCapacity > 0)
                _cachedViews[0].GetComponent<Button>().Select();
        }

        private void SetupEntryTemplate()
        {
            _entryTemplate.SetActive(false);

            var rowView = _entryTemplate.AddComponent<RowView>();
            var listEntry = _entryTemplate.AddComponent<RowBinder>();
            rowView.Initialize(listEntry);
            rowView.Bind(null, true);
}

        private RowView CreatePooledView()
        {
            GameObject copy;
            if (InstantiateWithParentExists)
            {
                copy = UnityEngine.Object.Instantiate(
                    _entryTemplate,
                    _entryTemplate.transform.parent);
            }
            else
            {
                copy = UnityEngine.Object.Instantiate(_entryTemplate);
                copy.transform.parent = _entryTemplate.transform.parent;
            }
            var entry = copy.GetComponent<RowView>();
            entry.Initialize(copy.GetComponent<RowBinder>());
            entry.Release();
            return entry;
        }
    }
}
