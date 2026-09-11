using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Code for the MaterialEditor UI
    /// </summary>
#pragma warning disable BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    public abstract partial class MaterialEditorUI : BaseUnityPlugin
#pragma warning restore BepInEx001 // Class inheriting from BaseUnityPlugin missing BepInPlugin attribute
    {
        /// <summary>
        /// Element containing the entire UI
        /// </summary>
        public static Canvas MaterialEditorWindow;
        /// <summary>
        /// Main panel
        /// </summary>
        public static Image MaterialEditorMainPanel;
        /// <summary>
        /// Draggable header
        /// </summary>
        public static Image DragPanel;
        private static readonly MaterialEditorSessionState Session = new MaterialEditorSessionState();
        private static MaterialEditorWindowView ActiveView;
        private static MaterialEditorUI ActiveUi;

        private static readonly MaterialEditorInterpolableSelectionState
            InterpolableSelection =
                new MaterialEditorInterpolableSelectionState();
        private static readonly MaterialEditorTargetLifecycle TargetLifecycle =
            new MaterialEditorTargetLifecycle(
                Session,
                InterpolableSelection,
                () => ActiveUi);

        private MaterialEditorWindowView _windowView;
        private MaterialEditorSelectionController _selectionController;
        private MaterialEditorPresenter _presenter;
        private MaterialEditorPresentation _presentation;
        private MaterialEditorAssetWorkflow _assetWorkflow;
        private MaterialEditorRefreshController _refresh;
        private MaterialEditorRefreshController RefreshController =>
            _refresh ?? (_refresh = new MaterialEditorRefreshController(this));
        private bool _transientContentReleased;

        private static readonly List<Action<MaterialEditorLabelClickEventArgs>> LabelClickHandlers = new List<Action<MaterialEditorLabelClickEventArgs>>();

        private VirtualList VirtualList;

        internal const float MarginSize = MaterialEditorLayout.Margin;
        internal const float HeaderSize = MaterialEditorLayout.HeaderHeight;
        internal const float ScrollOffsetX = MaterialEditorLayout.ScrollbarOffset;
        internal const float PanelHeight = MaterialEditorLayout.RowHeight;

        #region Entry Item Width
        // General
        internal const float LabelWidth = MaterialEditorLayout.LabelWidth;
        internal const float ButtonWidth = MaterialEditorLayout.ButtonWidth;
        internal const float SmallButtonWidth = MaterialEditorLayout.SmallButtonWidth;
        internal const float ResetButtonWidth = MaterialEditorLayout.ResetButtonWidth;
        internal const float InterpolableButtonWidth = MaterialEditorLayout.InterpolableButtonWidth;
        internal const float ContentFullWidth = MaterialEditorLayout.ContentWidth;
        // Renderer (Enbale/ShadowCastingMode/ReceiveShadows/RendererUpdateWhenOffscreen/RecalulateNormals)
        internal const float RendererButtonWidth = MaterialEditorLayout.RendererButtonWidth;
        internal const float RendererToggleWidth = MaterialEditorLayout.RendererToggleWidth;
        internal const float RendererDropdownWidth = MaterialEditorLayout.RendererDropdownWidth;
        // Material
        internal const float MaterialButtonWidth = MaterialEditorLayout.MaterialButtonWidth;
        internal const float MaterialRenameButtonWidth = MaterialEditorLayout.MaterialRenameButtonWidth;
        // Shader
        internal const float ShaderLabelMinimumWidth = MaterialEditorLayout.ShaderLabelMinimumWidth;
        internal const float ShaderDropdownMinimumWidth = MaterialEditorLayout.ShaderDropdownMinimumWidth;
        internal const float ShaderDropdownWidth = MaterialEditorLayout.ShaderDropdownWidth;
        // RenderQueue
        internal const float RenderQueueInputFieldWidth = MaterialEditorLayout.RenderQueueInputWidth;
        // Texture
        internal const float TextureButtonWidth = ContentFullWidth / 2f;
        // Texture Offset and Scale
        internal const float OffsetScaleLabelXWidth = MaterialEditorLayout.OffsetScaleLabelXWidth;
        internal const float OffsetScaleLabelYWidth = MaterialEditorLayout.OffsetScaleLabelYWidth;
        internal const float OffsetScaleInputFieldWidth = MaterialEditorLayout.OffsetScaleInputWidth;
        // Color
        internal const float ColorLabelWidth = MaterialEditorLayout.ColorLabelWidth;
        internal const float ColorInputFieldWidth = MaterialEditorLayout.ColorInputWidth;
        internal const float ColorEditButtonWidth = MaterialEditorLayout.ColorEditButtonWidth;
        // Float
        internal const float FloatSliderWidth = MaterialEditorLayout.FloatSliderWidth;
        internal const float FloatInputFieldWidth = MaterialEditorLayout.FloatInputWidth;
        // Keyword
        internal const float KeywordToggleWidth = MaterialEditorLayout.KeywordToggleWidth;
        #endregion

        internal static RectOffset Padding => MaterialEditorLayout.RowPadding;

        #region Colors
        internal static Color RowColor => MaterialEditorStyles.RowColor;
        internal static Color RendererColor => MaterialEditorStyles.RendererColor;
        internal static Color MaterialColor => MaterialEditorStyles.MaterialColor;
        internal static Color CategoryColor => MaterialEditorStyles.CategoryColor;
        internal static Color SubcategoryColor => MaterialEditorStyles.SubcategoryColor;
        internal static Color ItemColor => MaterialEditorStyles.PropertyColor;
        internal static Color ItemColorChanged => MaterialEditorStyles.ChangedRowColor;
        #endregion

        private protected IMaterialEditorColorPalette ColorPalette;

        internal GameObject CurrentGameObject
        {
            get => Session.CurrentGameObject;
            set => Session.CurrentGameObject = value;
        }

        internal object CurrentData
        {
            get => Session.CurrentData;
            set => Session.CurrentData = value;
        }

        internal static GameObject RetainedTargetGameObject =>
            Session.CurrentGameObject;

        internal static object RetainedTargetData => Session.CurrentData;

        private MaterialEditService _materialEditService;
        private static string CurrentFilter
        {
            get => Session.Filter;
            set => Session.Filter = value;
        }

        internal string RefreshFilter
        {
            get => CurrentFilter;
            set => CurrentFilter = value;
        }

        internal MaterialEditorPresentation RefreshPresentation => _presentation;

        internal VirtualList RefreshVirtualList => VirtualList;

        internal Coroutine StartRefreshCoroutine(IEnumerator routine) =>
            StartCoroutine(routine);

        internal void StopRefreshCoroutine(Coroutine coroutine) =>
            StopCoroutine(coroutine);

        internal void PopulateListCoreForRefresh(
            GameObject go,
            object data,
            string filter,
            VirtualListRowAnchorResolver.TopRowAnchor topRowAnchor,
            bool preserveRenamePanel)
        {
            PopulateListCore(
                go,
                data,
                filter,
                topRowAnchor,
                preserveRenamePanel);
        }

        internal static SelectedInterpolable selectedInterpolable =>
            InterpolableSelection.SelectedMaterial;

        internal static SelectedProjectorInterpolable
            selectedProjectorInterpolable =>
                InterpolableSelection.SelectedProjector;

        internal VirtualList LifecycleVirtualList => VirtualList;

        internal MaterialEditorSelectionController LifecycleSelectionController =>
            _selectionController;

        internal MaterialEditorWindowView LifecycleWindowView => _windowView;

        internal MaterialEditorPresentation LifecyclePresentation
        {
            get => _presentation;
            set => _presentation = value;
        }

        internal bool LifecycleTransientContentReleased
        {
            get => _transientContentReleased;
            set => _transientContentReleased = value;
        }

        internal IMaterialEditorColorPalette LifecycleColorPalette =>
            ColorPalette;

        internal void CancelPendingRefreshesForLifecycle() =>
            CancelPendingRefreshes();
        private protected MaterialEditService EditService =>
            _materialEditService ?? (_materialEditService = CreateMaterialEditService());

        private MaterialEditorAssetWorkflow AssetWorkflow =>
            _assetWorkflow ?? (_assetWorkflow =
                new MaterialEditorAssetWorkflow(this, EditService));

        private protected virtual MaterialEditService CreateMaterialEditService() =>
            new MaterialEditService(new LegacyMaterialEditRepository(this));

        /// <summary>
        /// Register a callback for clicks on renderer, material, shader, and property labels.
        /// Registering the same callback more than once has no effect.
        /// </summary>
        /// <param name="handler">Callback invoked with the current Material Editor context.</param>
        public static void RegisterLabelClickHandler(Action<MaterialEditorLabelClickEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (!LabelClickHandlers.Contains(handler))
                LabelClickHandlers.Add(handler);
        }

        /// <summary>
        /// Unregister a callback previously registered with <see cref="RegisterLabelClickHandler"/>.
        /// </summary>
        /// <param name="handler">Callback to remove.</param>
        public static void UnregisterLabelClickHandler(Action<MaterialEditorLabelClickEventArgs> handler)
        {
            if (handler == null)
                return;
            LabelClickHandlers.Remove(handler);
        }

        internal static void RaiseLabelClicked(MaterialEditorLabelClickEventArgs eventArgs)
        {
            foreach (var handler in LabelClickHandlers.ToArray())
            {
                try
                {
                    handler(eventArgs);
                }
                catch (Exception ex)
                {
                    MaterialEditorPluginBase.Logger?.LogError($"Exception in Material Editor label click handler: {ex}");
                }
            }
        }

        /// <summary>
        /// Initialize the MaterialEditor UI
        /// </summary>
        protected void InitUI()
        {
            ActiveUi = this;
            if (_refresh == null)
                _refresh = new MaterialEditorRefreshController(this);
            MaterialEditorExtensionRegistry.SetActiveEditService(EditService);
            _windowView = new MaterialEditorWindowView(
                transform,
                CurrentFilter,
                HandleFilterChanged,
                () => Visible = false,
                () => _selectionController.ToggleCategoriesPanel(),
                () => _selectionController.ToggleSelectionPanels(),
                ToggleAllCategories,
                ToggleAllSections,
                NavigateToCategory,
                ToggleCategory);
            _selectionController = new MaterialEditorSelectionController(
                Session,
                _windowView,
                EditService,
                PopulateList);
            _selectionController.InitializeViewState();
            _presenter = new MaterialEditorPresenter(
                EditService,
                Session,
                new MaterialEditorPresentationActions
                {
                    Refresh = (go, data, filter) => PopulateList(
                        go,
                        data,
                        ResolveFilterForCurrentTarget(go, data, filter)),
                    RefreshDeferred = SchedulePopulateList,
                    RequestCondition = HandleConditionChanged,
                    RefreshMaterialSelection = PopulateMaterialList,
                    SetRendererCollapsed = SetRendererCollapsed,
                    ShowRename = PopulateRenameList,
                    ExportUv = Export.ExportUVMaps,
                    RequestObjExport = Session.RequestObjExport,
                    ExportTexture = ExportTexture,
                    ImportTexture = ImportTexture,
                    ExportCubemap = ExportCubemap,
                    ImportCubemap = ImportCubemap,
                    SelectInterpolable = SelectInterpolableButtonOnClick,
                    SelectProjectorInterpolable = SelectProjectorInterpolableButtonOnClick,
                    EditColor = (data, material, title, value, onChanged) =>
                        SetupColorPalette(data, material, title, value, onChanged, true),
                    SetColorToPalette = SetColorToPalette,
                    IsPropertyBlacklisted = (materialName, propertyName) =>
                        Instance.CheckBlacklist(materialName, propertyName)
                });

            ActiveView = _windowView;
            MaterialEditorWindow = _windowView.Window;
            MaterialEditorMainPanel = _windowView.MainPanel;
            DragPanel = _windowView.HeaderPanel;
            VirtualList = _windowView.VirtualList;
            Visible = false;
        }

        private protected void SetHeaderTitleHorizontalOffset(float offset)
        {
            _windowView?.SetHeaderTitleHorizontalOffset(offset);
        }

        private protected Transform HeaderContextSlot =>
            _windowView?.HeaderContextSlot;

        private protected void SetHeaderContextControlVisible(bool visible)
        {
            _windowView?.SetHeaderContextControlVisible(visible);
        }

        /// <summary>
        /// Refresh the MaterialEditor UI
        /// </summary>
        public void RefreshUI() => RefreshUI(CurrentFilter);
        /// <summary>
        /// Refresh the MaterialEditor UI using the specified filter text
        /// </summary>
        public void RefreshUI(string filterText) => PopulateList(CurrentGameObject, CurrentData, filterText);

        /// <summary>
        /// Get or set the MaterialEditor UI visibility
        /// </summary>
        public static bool Visible
        {
            get
            {
                if (MaterialEditorWindow != null && MaterialEditorWindow.gameObject != null)
                    return MaterialEditorWindow.gameObject.activeInHierarchy;
                return false;
            }
            set
            {
                var wasVisible = Visible;
                if (MaterialEditorWindow != null)
                    MaterialEditorWindow.gameObject.SetActive(value);
                if (!value)
                    ActiveUi?.ReleaseTransientUiContent();
                else if (!wasVisible)
                    ActiveUi?.RestoreTransientUiContent();
            }
        }

        internal static void UISettingChanged(object sender, EventArgs e)
        {
            ActiveView?.ApplySettings();
        }

        internal static void UIThemeSettingChanged(object sender, EventArgs e)
        {
            var mode = UITheme == null
                ? MaterialEditorThemeMode.Legacy
                : GetConfiguredUITheme();
            if (!MaterialEditorTheme.SetMode(mode))
                return;

            ActiveView?.ApplyTheme();
        }

        /// <summary>
        /// Search text using wildcards.
        /// </summary>
        /// <param name="text">Text to search in</param>
        /// <param name="filter">Filter with which to search the text</param>
        internal static bool WildCardSearch(string text, string filter)
        {
            return MaterialEditorFilter.Matches(text, filter);
        }

        /// <summary>
        /// Populate the renderer list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="rendListFull">List of all renderers to display</param>
        private void PopulateRendererList(GameObject go, object data, IEnumerable<Renderer> rendListFull)
        {
            _selectionController.PopulateRendererList(go, data, rendListFull);
        }


        /// <summary>
        /// Populate the materials list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="materials">List of all materials to display</param>
        private void PopulateMaterialList(GameObject go, object data, IEnumerable<Renderer> materials)
        {
            _selectionController.PopulateMaterialList(go, data, materials);
        }

        /// <summary>
        /// Populate the rename list
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers</param>
        /// <param name="material">Material to be renamed</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        private void PopulateRenameList(GameObject go, Material material, object data)
        {
            _selectionController.ShowRenamePanel(go, material, data);
        }

        /// <summary>
        /// Populate the MaterialEditor UI
        /// </summary>
        /// <param name="go">GameObject for which to read the renderers and materials</param>
        /// <param name="data">Object that will be passed through to the get/set/reset events</param>
        /// <param name="filter">Comma separated list of text to filter the results</param>
        protected void PopulateList(GameObject go, object data, string filter = null)
        {
            CancelPendingRefreshes();
            PopulateListCore(go, data, filter, null, false);
        }

        private void PopulateListWithAnchor(
            GameObject go,
            object data,
            string filter,
            VirtualListRowAnchorResolver.TopRowAnchor topRowAnchor)
        {
            CancelPendingRefreshes();
            PopulateListCore(go, data, filter, topRowAnchor, false);
        }

        private void PopulateListCore(
            GameObject go,
            object data,
            string filter,
            VirtualListRowAnchorResolver.TopRowAnchor topRowAnchor,
            bool preserveRenamePanel,
            bool publishViewportAnchor = true)
        {
            var previousTarget = CurrentGameObject;
            var previousTargetWasDestroyed =
                !ReferenceEquals(previousTarget, null) && previousTarget == null;
            _transientContentReleased = false;
            if (!preserveRenamePanel)
                _selectionController.CloseRenamePanel();

            if (!ReferenceEquals(previousTarget, null)
                && !ReferenceEquals(previousTarget, go))
            {
                CloseTargetColorPalette();
                if (previousTargetWasDestroyed)
                {
                    ClearInterpolablesForTarget(previousTarget);
                    PruneDestroyedInterpolables();
                }
            }

            if (filter == null)
                filter = PersistFilter.Value ? CurrentFilter : string.Empty;

            _windowView.PrepareForDisplay(filter);
            if (go == null)
            {
                ReleaseRetainedTargetContext();
                Session.ClearTargetReferences();
                if (previousTargetWasDestroyed)
                    ClearInterpolablesForTarget(previousTarget);
                PruneDestroyedInterpolables();
                return;
            }

            var renderers = GetRendererList(go).ToList();
            var projectors = EditService.GetProjectorList(data, go).ToList();
            PopulateRendererList(go, data, renderers);

            CurrentGameObject = go;
            CurrentData = data;
            CurrentFilter = filter;

            _presentation = _presenter.BuildRows(go, data, filter, renderers, projectors);
            VirtualList.SetList(_presentation.Rows, false);
            if (topRowAnchor != null)
                VirtualList.RestoreTopRowAnchor(topRowAnchor, false);

            // Install the new presentation first, then publish its final anchor
            // once. Deferring the navigator update avoids a false no-categories
            // transition and two responsive layout passes during one rebuild.
            _windowView.SetPresentation(_presentation, true);
            if (publishViewportAnchor)
                VirtualList.PublishViewportAnchor();
        }

        private void NavigateToCategory(CategoryNavigationTarget target)
        {
            if (target == null)
                return;
            if (target.EnsureParentsExpanded())
            {
                RebuildAndScrollToCategory(target.SectionId, target.Id);
                return;
            }
            if (target.RowIndex >= 0)
                VirtualList.ScrollToIndex(target.RowIndex);
        }

        private void ToggleCategory(CategoryNavigationTarget target)
        {
            if (target == null)
                return;
            var sectionId = target.SectionId;
            var categoryId = target.Id;
            target.SetCollapsed(!target.Collapsed);
            RebuildAndScrollToCategory(sectionId, categoryId);
        }

        private void SetRendererCollapsed(
            RendererSectionPresentation section,
            bool collapsed)
        {
            var presentation = _presentation;
            if (presentation == null || VirtualList == null)
                return;

            int replaceStartIndex;
            int removeCount;
            IList<RowModel> replacementRows;
            if (!presentation.TrySetRendererCollapsed(
                    section,
                    collapsed,
                    out replaceStartIndex,
                    out removeCount,
                    out replacementRows))
                return;

            VirtualList.ReplaceRange(
                replaceStartIndex,
                removeCount,
                replacementRows,
                section.HeaderRowIndex);
            _windowView?.RefreshSectionCollapseState(presentation);
        }

        private void ToggleAllCategories()
        {
            if (_presentation == null)
                return;

            _presentation.SetAllCategoriesCollapsed(
                !_presentation.AllCategoriesCollapsed);
            PopulateList(CurrentGameObject, CurrentData, CurrentFilter);
        }

        private void ToggleAllSections()
        {
            if (_presentation == null
                || !_presentation.CanToggleSections)
                return;

            _presentation.SetAllSectionsCollapsed(
                !_presentation.AllSectionsCollapsed);
            PopulateList(CurrentGameObject, CurrentData, CurrentFilter);
        }

        private void RebuildAndScrollToCategory(
            string sectionId,
            string categoryId)
        {
            CancelPendingRefreshes();
            PopulateListCore(
                CurrentGameObject,
                CurrentData,
                CurrentFilter,
                null,
                false,
                false);
            var target = _presentation?.FindCategory(sectionId, categoryId);
            if (target != null && target.RowIndex >= 0)
                VirtualList.ScrollToIndex(target.RowIndex);
            else
                VirtualList.PublishViewportAnchor();
        }

        /// <summary>
        /// Obj export should be done in OnGUI or something similarly late so that finger rotation is exported properly
        /// </summary>
        private void OnGUI()
        {
            if (Session.TryTakeObjExport(out var renderer))
                Export.ExportObj(renderer);
        }

        /// <summary>
        /// Defers rebuilding the material list until the dropdown fade has completed.
        /// </summary>
        protected IEnumerator PopulateListCoroutine(
            GameObject go,
            object data,
            string filter = "") =>
            RefreshController.PopulateListCoroutine(go, data, filter);

        private void SchedulePopulateList(
            GameObject go,
            object data,
            string filter) =>
            RefreshController.SchedulePopulateList(go, data, filter);

        private string ResolveFilterForCurrentTarget(
            GameObject go,
            object data,
            string filter) =>
            RefreshController.ResolveFilterForCurrentTarget(go, data, filter);

        private void HandleFilterChanged(string filter) =>
            RefreshController.HandleFilterChanged(filter);

        private void HandleConditionChanged(
            MaterialConditionInvalidationHandle handle) =>
            RefreshController.HandleConditionChanged(handle);

        private void CancelPendingRefreshes() =>
            _refresh?.CancelPendingRefreshes();
        private void ReleaseTransientUiContent() =>
            TargetLifecycle.ReleaseTransientUiContent();

        private void ReleaseRetainedTargetContext() =>
            TargetLifecycle.ReleaseRetainedTargetContext();

        private void RestoreTransientUiContent() =>
            TargetLifecycle.RestoreTransientUiContent();

        internal static bool IsGameObjectWithin(
            GameObject candidate,
            GameObject root) =>
            MaterialEditorTargetLifecycle.IsGameObjectWithin(candidate, root);

        internal static bool IsCurrentTargetWithin(GameObject root) =>
            TargetLifecycle.IsCurrentTargetWithin(root);

        internal static void ReleaseCurrentTargetSelections() =>
            TargetLifecycle.ReleaseCurrentTargetSelections();

        internal static void InvalidateCurrentTarget() =>
            TargetLifecycle.InvalidateCurrentTarget();

        internal static void InvalidateAllTargetState() =>
            TargetLifecycle.InvalidateAllTargetState();

        internal static bool NotifyTargetDestroyed(GameObject root) =>
            TargetLifecycle.NotifyTargetDestroyed(root);

        internal static void PruneDestroyedInterpolables() =>
            TargetLifecycle.PruneDestroyedInterpolables();

        private static void ClearInterpolablesForTarget(GameObject root) =>
            TargetLifecycle.ClearInterpolablesForTarget(root);

        private void CloseTargetColorPalette() =>
            TargetLifecycle.CloseTargetColorPalette();
        internal void CancelAssetImportsForLifecycle()
        {
            _assetWorkflow?.Dispose();
            _assetWorkflow = null;
        }

        internal void ShutdownMaterialEditorUi()
        {
            if (!ReferenceEquals(ActiveUi, this))
                return;

            try
            {
                InvalidateAllTargetState();
            }
            finally
            {
                MaterialEditorExtensionRegistry.SetActiveEditService(null);
                _assetWorkflow?.Dispose();
                _assetWorkflow = null;
                ActiveView = null;
                ActiveUi = null;
                MaterialEditorWindow = null;
                MaterialEditorMainPanel = null;
                DragPanel = null;
                VirtualList = null;
                _refresh = null;
                _windowView = null;
                _selectionController = null;
                _presenter = null;
                ColorPalette = null;
            }
        }

        internal static void DisposeTexChangeWatcher()
        {
            MaterialEditorAssetWorkflow.DisposeTextureWatcher();
        }

        private void ImportTexture(
            TexturePropertyRowModel textureItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
            AssetWorkflow.ImportTexture(
                textureItem,
                gameObject,
                data,
                material,
                propertyName);
        }

        private void ImportCubemap(
            CubemapPropertyRowModel cubemapItem,
            GameObject gameObject,
            object data,
            Material material,
            string propertyName)
        {
            AssetWorkflow.ImportCubemap(
                cubemapItem,
                gameObject,
                data,
                material,
                propertyName);
        }

        internal virtual void ExportTexture(Material mat, string property)
        {
            AssetWorkflow.ExportTexture(mat, property);
        }

        internal void ExportCubemap(Material mat, string property)
        {
            AssetWorkflow.ExportCubemap(mat, property);
        }

        internal void ExportTextureOriginal(
            Material mat,
            string property,
            string ext,
            byte[] texData)
        {
            AssetWorkflow.ExportTextureOriginal(mat, property, ext, texData);
        }

        private void SetupColorPalette(
            object data,
            Material material,
            string title,
            Color value,
            Action<Color> onChanged,
            bool useAlpha) =>
            TargetLifecycle.SetupColorPalette(
                data,
                material,
                title,
                value,
                onChanged,
                useAlpha);

        private void SetColorToPalette(
            object data,
            Material material,
            string title,
            Color value) =>
            TargetLifecycle.SetColorToPalette(data, material, title, value);

        private void SelectInterpolableButtonOnClick(
            GameObject go,
            RowModel.RowItemType rowType,
            string materialName = "",
            string propertyName = "",
            string rendererName = "") =>
            InterpolableSelection.SelectMaterial(
                go,
                rowType,
                materialName,
                propertyName,
                rendererName);

        private void SelectProjectorInterpolableButtonOnClick(
            GameObject gameObject,
            ProjectorProperties property,
            string projectorName) =>
            InterpolableSelection.SelectProjector(
                gameObject,
                property,
                projectorName);
    }

}
