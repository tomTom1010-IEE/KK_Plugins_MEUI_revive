using System;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Owns target release, restoration and invalidation across the retained
    /// Material Editor session and its active UI host.
    /// </summary>
    internal sealed class MaterialEditorTargetLifecycle
    {
        private readonly MaterialEditorSessionState _session;
        private readonly MaterialEditorInterpolableSelectionState
            _interpolableSelection;
        private readonly Func<MaterialEditorUI> _getHost;

        internal MaterialEditorTargetLifecycle(
            MaterialEditorSessionState session,
            MaterialEditorInterpolableSelectionState interpolableSelection,
            Func<MaterialEditorUI> getHost)
        {
            _session = session
                ?? throw new ArgumentNullException(nameof(session));
            _interpolableSelection = interpolableSelection
                ?? throw new ArgumentNullException(nameof(interpolableSelection));
            _getHost = getHost
                ?? throw new ArgumentNullException(nameof(getHost));
        }

        internal void ReleaseTransientUiContent()
        {
            var host = _getHost();
            if (ReferenceEquals(host, null))
                return;

            host.CancelAssetImportsForLifecycle();
            host.CancelPendingRefreshesForLifecycle();
            host.LifecycleVirtualList?.ReleaseContent();
            host.LifecycleSelectionController?.ReleaseTransientContent();
            host.LifecycleWindowView?.ReleasePresentation();
            host.LifecyclePresentation = null;
            host.LifecycleTransientContentReleased = true;
        }

        internal void ReleaseRetainedTargetContext()
        {
            var host = _getHost();
            if (ReferenceEquals(host, null))
                return;

            ReleaseTransientUiContent();
            host.LifecycleSelectionController?.ReleaseTargetContent();
            CloseTargetColorPalette();
            host.LifecycleTransientContentReleased = true;
        }

        internal void RestoreTransientUiContent()
        {
            var host = _getHost();
            if (ReferenceEquals(host, null)
                || !host.LifecycleTransientContentReleased)
                return;

            var gameObject = _session.CurrentGameObject;
            var data = _session.CurrentData;
            var filter = _session.Filter;
            if (gameObject == null)
            {
                var destroyedTarget = !ReferenceEquals(gameObject, null);
                ReleaseRetainedTargetContext();
                _session.ClearTargetReferences();
                if (destroyedTarget)
                    _interpolableSelection.ClearForTarget(gameObject);
                _interpolableSelection.PruneDestroyed();
                return;
            }

            host.LifecycleTransientContentReleased = false;
            host.CancelPendingRefreshesForLifecycle();
            host.PopulateListCoreForRefresh(
                gameObject,
                data,
                filter,
                null,
                true);
        }

        internal bool IsCurrentTargetWithin(GameObject root) =>
            IsGameObjectWithin(_session.CurrentGameObject, root);

        internal void ReleaseCurrentTargetSelections()
        {
            try
            {
                MaterialEditorUI.Visible = false;
                var host = _getHost();
                host?.LifecycleSelectionController?.ReleaseTargetContent();
                CloseTargetColorPalette();
            }
            finally
            {
                _session.CancelObjExport();
                _session.ClearSelections();
                _interpolableSelection.PruneDestroyed();
            }
        }

        internal void InvalidateCurrentTarget()
        {
            var target = _session.CurrentGameObject;
            try
            {
                MaterialEditorUI.Visible = false;
                ReleaseRetainedTargetContext();
            }
            finally
            {
                _session.ClearTargetReferences();
                if (ReferenceEquals(target, null))
                {
                    _interpolableSelection.ClearAll();
                }
                else
                {
                    _interpolableSelection.ClearForTarget(target);
                    _interpolableSelection.PruneDestroyed();
                }
            }
        }

        internal void InvalidateAllTargetState()
        {
            try
            {
                MaterialEditorUI.Visible = false;
                ReleaseRetainedTargetContext();
            }
            finally
            {
                _session.ClearTargetReferences();
                _interpolableSelection.ClearAll();
            }
        }

        internal bool NotifyTargetDestroyed(GameObject root)
        {
            _interpolableSelection.ClearForTarget(root);
            _interpolableSelection.PruneDestroyed();
            if (!IsCurrentTargetWithin(root))
                return false;

            InvalidateCurrentTarget();
            return true;
        }

        internal void ClearInterpolablesForTarget(GameObject root) =>
            _interpolableSelection.ClearForTarget(root);

        internal void PruneDestroyedInterpolables() =>
            _interpolableSelection.PruneDestroyed();

        internal void SetupColorPalette(
            object data,
            Material material,
            string title,
            Color value,
            Action<Color> onChanged,
            bool useAlpha)
        {
            var palette = _getHost().LifecycleColorPalette;
            var name = material.name;
            if (palette.IsShowing(title, data, name))
            {
                palette.Close();
                return;
            }

            try
            {
                palette.Setup(
                    title,
                    data,
                    name,
                    value,
                    onChanged,
                    useAlpha);
            }
            catch (ArgumentException)
            {
                MaterialEditorPluginBase.Logger.LogError(
                    $"Color value is out of range. ({value})");
                palette.Close();
            }
        }

        internal void SetColorToPalette(
            object data,
            Material material,
            string title,
            Color value)
        {
            var palette = _getHost().LifecycleColorPalette;
            if (palette.IsShowing(title, data, material.name))
            {
                try
                {
                    palette.SetColor(value);
                }
                catch (ArgumentException)
                {
                    MaterialEditorPluginBase.Logger.LogError(
                        $"Color value is out of range. ({value})");
                    palette.Close();
                }
            }
        }

        internal void CloseTargetColorPalette()
        {
            try
            {
                _getHost()?.LifecycleColorPalette?.Close();
            }
            catch (Exception ex)
            {
                MaterialEditorPluginBase.Logger?.LogWarning(
                    "Could not close the Material Editor color palette while "
                    + "releasing a target: " + ex);
            }
        }

        internal static bool IsGameObjectWithin(
            GameObject candidate,
            GameObject root)
        {
            if (ReferenceEquals(candidate, root))
                return !ReferenceEquals(candidate, null);
            if (ReferenceEquals(candidate, null)
                || ReferenceEquals(root, null)
                || candidate == null
                || root == null)
                return false;

            try
            {
                return candidate.transform.IsChildOf(root.transform);
            }
            catch (MissingReferenceException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Owns the current Material Editor selections exposed to Timeline.
    /// </summary>
    internal sealed class MaterialEditorInterpolableSelectionState
    {
        internal SelectedInterpolable SelectedMaterial { get; private set; }

        internal SelectedProjectorInterpolable SelectedProjector
        {
            get;
            private set;
        }

        internal void SelectMaterial(
            GameObject gameObject,
            RowModel.RowItemType rowType,
            string materialName,
            string propertyName,
            string rendererName)
        {
            SelectedMaterial = new SelectedInterpolable(
                gameObject,
                rowType,
                materialName,
                propertyName,
                rendererName);
            MaterialEditorPluginBase.Logger.LogMessage(
                $"Activated interpolable(s), {SelectedMaterial}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal void SelectProjector(
            GameObject gameObject,
            ProjectorProperties property,
            string projectorName)
        {
            SelectedProjector = new SelectedProjectorInterpolable(
                gameObject,
                property,
                projectorName);
            MaterialEditorPluginBase.Logger.LogMessage(
                $"Activated interpolable(s), {SelectedProjector}");
#if !API && !EC
            TimelineCompatibilityHelper.RefreshInterpolablesList();
#endif
        }

        internal void ClearAll()
        {
            SelectedMaterial = null;
            SelectedProjector = null;
        }

        internal void ClearForTarget(GameObject root)
        {
            if (ReferenceEquals(root, null))
                return;
            if (SelectedMaterial != null
                && MaterialEditorTargetLifecycle.IsGameObjectWithin(
                    SelectedMaterial.GameObject,
                    root))
                SelectedMaterial = null;
            if (SelectedProjector != null
                && MaterialEditorTargetLifecycle.IsGameObjectWithin(
                    SelectedProjector.GameObject,
                    root))
                SelectedProjector = null;
        }

        internal void PruneDestroyed()
        {
            if (SelectedMaterial != null
                && SelectedMaterial.GameObject == null)
                SelectedMaterial = null;
            if (SelectedProjector != null
                && SelectedProjector.GameObject == null)
                SelectedProjector = null;
        }
    }

    internal sealed class SelectedInterpolable
    {
        public string MaterialName;
        public string PropertyName;
        public string RendererName;
        public GameObject GameObject;
        public RowModel.RowItemType RowType;

        internal SelectedInterpolable(
            GameObject gameObject,
            RowModel.RowItemType rowType,
            string materialName,
            string propertyName,
            string rendererName)
        {
            GameObject = gameObject;
            RowType = rowType;
            MaterialName = materialName;
            PropertyName = propertyName;
            RendererName = rendererName;
        }

        public override string ToString()
        {
            var details = string.Join(" - ", new[] { PropertyName, MaterialName, RendererName }.Where(x => !x.IsNullOrEmpty()).ToArray());
            return $"{RowType}: {details}";
        }
    }

    internal sealed class SelectedProjectorInterpolable
    {
        public string ProjectorName;
        public ProjectorProperties Property;
        public GameObject GameObject;

        internal SelectedProjectorInterpolable(
            GameObject gameObject,
            ProjectorProperties property,
            string projectorName)
        {
            GameObject = gameObject;
            Property = property;
            ProjectorName = projectorName;
        }

        public override string ToString()
        {
            var details = string.Join(" - ", new[] { Property.ToString(), ProjectorName }.Where(x => !x.IsNullOrEmpty()).ToArray());
            return $"Projector: {details}";
        }
    }
}
