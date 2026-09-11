using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    public partial class MaterialEditorPluginBase
    {
        private const string LightThemeSetting = "Light";
        private const string DarkThemeSetting = "Dark";

        /// <summary>
        /// Configuration entry for ME window scale
        /// </summary>
        public static ConfigEntry<float> UIScale { get; set; }
        /// <summary>
        /// Configuration entry for ME window width
        /// </summary>
        public static ConfigEntry<float> UIWidth { get; set; }
        /// <summary>
        /// Configuration entry for ME window height
        /// </summary>
        public static ConfigEntry<float> UIHeight { get; set; }
        /// <summary>
        /// Configuration entry for width of the renderer/materials lists to the side of the window
        /// </summary>
        public static ConfigEntry<float> UIListWidth { get; set; }
        /// <summary>
        /// Configuration entry for width of the Categories list beside the window
        /// </summary>
        internal static ConfigEntry<float> UICategoriesWidth { get; private set; }
        internal static ConfigEntry<string> UITheme { get; private set; }
        internal static ConfigEntry<bool> CategoriesPanelOpen { get; private set; }
        internal static ConfigEntry<bool> RenderersPanelOpen { get; private set; }
        internal static ConfigEntry<bool> MaterialsPanelOpen { get; private set; }
        /// <summary>
        /// Configuration entry for sensitivity of dragging labels to edit float values
        /// </summary>
        public static ConfigEntry<float> DragSensitivity { get; set; }
        /// <summary>
        /// Prevent dragging the ME window outside of the game window
        /// </summary>
        public static ConfigEntry<bool> PreventDragout { get; set; }
        /// <summary>
        /// Defines which visible part of the Material Editor window remains
        /// recoverable while dragging. It is synchronized with the public
        /// <see cref="PreventDragout"/> compatibility entry.
        /// </summary>
        internal static ConfigEntry<MaterialEditorWindowDragMode> WindowDragMode
        {
            get;
            private set;
        }
        private static bool _synchronizingWindowDragSettings;
        /// <summary>
        /// Configuration entry for watching for file changes and reloading textures on change
        /// </summary>
        public static ConfigEntry<bool> WatchTexChanges { get; set; }
        /// <summary>
        /// Replaces every loaded shader with the MaterialEditor copy of the shader
        /// </summary>
        public static ConfigEntry<bool> ShaderOptimization { get; set; }
        /// <summary>
        /// Skinned meshes will be exported in their current state with all customization applied as well as in the current pose
        /// </summary>
        public static ConfigEntry<bool> ExportBakedMesh { get; set; }
        /// <summary>
        /// When enabled, objects will be exported with their position changes intact so that, i.e. when exporting two objects they retain their position relative to each other
        /// </summary>
        public static ConfigEntry<bool> ExportBakedWorldPosition { get; set; }
        /// <summary>
        /// Textures and models will be exported to this folder. If empty, exports to {ExportPathDefault}
        /// </summary>
        internal static ConfigEntry<string> ConfigExportPath { get; private set; }
        /// <summary>
        /// Persist search filter across editor windows
        /// </summary>
        public static ConfigEntry<bool> PersistFilter { get; set; }
        /// <summary>
        /// Whether to show tooltips or not
        /// </summary>
        public static ConfigEntry<bool> Showtooltips { get; set; }
        /// <summary>
        /// Whether Shift-activated shader-authored hints are available
        /// </summary>
        internal static ConfigEntry<bool> EnableShaderHints { get; private set; }
        /// <summary>
        /// Whether to sort shader properties by their types
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByType { get; set; }
        /// <summary>
        /// Whether to sort shader properties by their names
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByName { get; set; }
        /// <summary>
        /// Whether to sort shader properties by their category
        /// </summary>
        public static ConfigEntry<bool> SortPropertiesByCategory { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorNearClipPlaneMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorFarClipPlaneMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorFieldOfViewMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorAspectRatioMax { get; set; }
        /// <summary>
        /// Controls the max value of the slider for this projector property
        /// </summary>
        public static ConfigEntry<float> ProjectorOrthographicSizeMax { get; set; }
        /// <summary>
        /// When enabled, normalmaps get converted from DXT5 compressed (red) normals back to normal OpenGL (blue/purple) normals
        /// </summary>
        public static ConfigEntry<bool> ConvertNormalmapsOnExport { get; set; }
        /// <summary>
        /// Optional path for reading version-2 local texture data. Empty uses {LocalTexturePathDefault}.
        /// </summary>
        internal static ConfigEntry<string> ConfigLocalTexturePath { get; set; }

        private static void HandleLegacyWindowDragSettingChanged(
            object sender,
            EventArgs eventArgs)
        {
            if (_synchronizingWindowDragSettings || WindowDragMode == null)
                return;

            _synchronizingWindowDragSettings = true;
            try
            {
                WindowDragMode.Value =
                    MaterialEditorWindowBoundsPolicy.FromLegacy(
                        PreventDragout.Value);
            }
            finally
            {
                _synchronizingWindowDragSettings = false;
            }
        }

        private static void HandleWindowDragModeChanged(
            object sender,
            EventArgs eventArgs)
        {
            SynchronizeLegacyWindowDragSetting();
            MaterialEditorUI.UISettingChanged(sender, eventArgs);
        }

        private static void SynchronizeLegacyWindowDragSetting()
        {
            if (_synchronizingWindowDragSettings
                || PreventDragout == null
                || WindowDragMode == null)
                return;

            _synchronizingWindowDragSettings = true;
            try
            {
                PreventDragout.Value =
                    MaterialEditorWindowBoundsPolicy.ToLegacyBoolean(
                        WindowDragMode.Value);
            }
            finally
            {
                _synchronizingWindowDragSettings = false;
            }
        }

        private void BindConfiguration()
        {
            UIScale = Config.Bind("Config", "UI Scale", MaterialEditorTheme.Metrics.UiScaleDefault, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.UiScaleMinimum, MaterialEditorTheme.Metrics.UiScaleMaximum), new ConfigurationManagerAttributes { Order = 8 }));
            UIWidth = Config.Bind("Config", "UI Width", MaterialEditorTheme.Metrics.WindowWidthDefault, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.WindowWidthMinimum, MaterialEditorTheme.Metrics.WindowWidthMaximum), new ConfigurationManagerAttributes { Order = 7, ShowRangeAsPercent = false }));
            UIHeight = Config.Bind("Config", "UI Height", MaterialEditorTheme.Metrics.WindowHeightDefault, new ConfigDescription("Controls the size of the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.WindowHeightMinimum, MaterialEditorTheme.Metrics.WindowHeightMaximum), new ConfigurationManagerAttributes { Order = 6, ShowRangeAsPercent = false }));
            UICategoriesWidth = Config.Bind("Config", "UI Categories Width", MaterialEditorTheme.Metrics.CategoryPanelDefaultWidth, new ConfigDescription("Controls the width of the Categories list beside the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.SidePanelMinimumWidth, MaterialEditorTheme.Metrics.SidePanelMaximumWidth), new ConfigurationManagerAttributes { Order = 5, ShowRangeAsPercent = false }));
            UIListWidth = Config.Bind("Config", "UI List Width", MaterialEditorTheme.Metrics.SidePanelDefaultWidth, new ConfigDescription("Controls the width of the renderer/material and Rename lists beside the window.", new AcceptableValueRange<float>(MaterialEditorTheme.Metrics.SidePanelMinimumWidth, MaterialEditorTheme.Metrics.SidePanelMaximumWidth), new ConfigurationManagerAttributes { Order = 4, ShowRangeAsPercent = false, DispName = "Renderer/Material List Width" }));
            UITheme = Config.Bind(
                "Config",
                "UI Theme",
                LightThemeSetting,
                new ConfigDescription(
                    "Selects the Light or Dark appearance. This changes visuals only.",
                    new AcceptableValueList<string>(
                        LightThemeSetting,
                        DarkThemeSetting)));
            MaterialEditorTheme.SetMode(GetConfiguredUITheme());
            CategoriesPanelOpen = Config.Bind(
                "UI State",
                "Categories Panel Open",
                false,
                new ConfigDescription(
                    "Remembers whether the Categories panel was left open.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            RenderersPanelOpen = Config.Bind(
                "UI State",
                "Renderers Panel Open",
                false,
                new ConfigDescription(
                    "Compatibility state for the jointly visible Renderers and Materials panels.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            MaterialsPanelOpen = Config.Bind(
                "UI State",
                "Materials Panel Open",
                false,
                new ConfigDescription(
                    "Compatibility state for the jointly visible Renderers and Materials panels.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            DragSensitivity = Config.Bind("Config", "Drag Sensitivity", 30f, new ConfigDescription("Controls the sensitivity of dragging labels to edit float values", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
            PreventDragout = Config.Bind(
                "Config",
                "Prevent Window Dragout",
                true,
                new ConfigDescription(
                    "Compatibility alias synchronized with Window Drag Limits: false selects NoLimits; true selects KeepHeaderInside.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));
            WindowDragMode = Config.Bind(
                "Config",
                "Window Drag Limits",
                MaterialEditorWindowBoundsPolicy.FromLegacy(
                    PreventDragout.Value),
                "Controls which visible part of the Material Editor window must remain inside the game window while dragging.");
            PreventDragout.SettingChanged += HandleLegacyWindowDragSettingChanged;
            WindowDragMode.SettingChanged += HandleWindowDragModeChanged;
            SynchronizeLegacyWindowDragSetting();
            WatchTexChanges = Config.Bind("Config", "Watch File Changes", true, new ConfigDescription("Watch for file changes and reload textures on change. Can be toggled in the UI.", null, new ConfigurationManagerAttributes { Order = 2 }));
            ShaderOptimization = Config.Bind("Config", "Shader Optimization", true, new ConfigDescription("Replaces every loaded shader with the MaterialEditor copy of the shader. Reduces the number of copies of shaders loaded which reduces RAM usage and improves performance.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ExportBakedMesh = Config.Bind("Config", "Export Baked Mesh", false, new ConfigDescription("When enabled, skinned meshes will be exported in their current state with all customization applied as well as in the current pose.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ExportBakedWorldPosition = Config.Bind("Config", "Export Baked World Position", false, new ConfigDescription("When enabled, objects will be exported with their position changes intact so that, i.e. when exporting two objects they retain their position relative to each other.\nOnly works when Export Baked Mesh is also enabled.", null, new ConfigurationManagerAttributes { Order = 1 }));
            ConfigExportPath = Config.Bind("Config", "Export Path Override", "", new ConfigDescription("Textures and models will be exported to this folder. If empty, exports to UserData\\MaterialEditor.", null, new ConfigurationManagerAttributes { Order = 1 }));
            PersistFilter = Config.Bind("Config", "Persist Filter", false, "Persist search filter across editor windows");
            Showtooltips = Config.Bind("Config", "Show Tooltips", true, "Whether to show tooltips or not");
            EnableShaderHints = Config.Bind(
                "Config",
                "Enable Shader Hints",
                true,
                "Show shader-authored hints while holding Shift. This is independent of the Show Tooltips setting.");
            SortPropertiesByType = Config.Bind("Config", "Sort Properties by Type", true, "Whether to sort shader properties by their types.");
            SortPropertiesByName = Config.Bind("Config", "Sort Properties by Name", true, "Whether to sort shader properties by their names.");
            SortPropertiesByCategory = Config.Bind("Config", "Sort Properties by Category", true, "Whether to sort shader properties by their category.");
            ConvertNormalmapsOnExport = Config.Bind("Config", "Convert Normalmaps On Export", true, new ConfigDescription("When enabled, normalmaps get converted from DXT5 compressed (red) normals back to normal OpenGL (blue/purple) normals"));

            // Everything in these games is 10x the size of KK/KKS
#if AI || HS2 || PH
            ProjectorNearClipPlaneMax = Config.Bind("Projector", "Max Near Clip Plane", 100f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 1000f), new ConfigurationManagerAttributes { Order = 5 }));
            ProjectorFarClipPlaneMax = Config.Bind("Projector", "Max Far Clip Plane", 1000f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 1000f), new ConfigurationManagerAttributes { Order = 4 }));
            ProjectorOrthographicSizeMax = Config.Bind("Projector", "Max Orthographic Size", 20f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 1000f), new ConfigurationManagerAttributes { Order = 1 }));
#else
            ProjectorNearClipPlaneMax = Config.Bind("Projector", "Max Near Clip Plane", 10f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 5 }));
            ProjectorFarClipPlaneMax = Config.Bind("Projector", "Max Far Clip Plane", 100f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 4 }));
            ProjectorOrthographicSizeMax = Config.Bind("Projector", "Max Orthographic Size", 2f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 1 }));
#endif
            ProjectorFieldOfViewMax = Config.Bind("Projector", "Max Field Of View", 180f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 180f), new ConfigurationManagerAttributes { Order = 3 }));
            ProjectorAspectRatioMax = Config.Bind("Projector", "Max Aspect Ratio", 2f, new ConfigDescription("Controls the max value of the slider for this projector property", new AcceptableValueRange<float>(0.01f, 100f), new ConfigurationManagerAttributes { Order = 2 }));

            UIScale.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIHeight.SettingChanged += MaterialEditorUI.UISettingChanged;
            UICategoriesWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            UIListWidth.SettingChanged += MaterialEditorUI.UISettingChanged;
            UITheme.SettingChanged += MaterialEditorUI.UIThemeSettingChanged;
            WatchTexChanges.SettingChanged += WatchTexChanges_SettingChanged;
            ShaderOptimization.SettingChanged += ShaderOptimization_SettingChanged;
            ConfigExportPath.SettingChanged += ConfigExportPath_SettingChanged;
            SortPropertiesByType.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SortPropertiesByName.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SortPropertiesByCategory.SettingChanged += (object sender, EventArgs e) => PropertyOrganizer.Refresh();
            SetExportPath();

        }

        internal static void ToggleUITheme()
        {
            if (UITheme == null)
                return;

            UITheme.Value = MaterialEditorTheme.ToggleMode
                            == MaterialEditorThemeMode.Dark
                ? DarkThemeSetting
                : LightThemeSetting;
        }

        internal static MaterialEditorThemeMode GetConfiguredUITheme()
        {
            return UITheme != null
                   && string.Equals(
                       UITheme.Value,
                       DarkThemeSetting,
                       StringComparison.OrdinalIgnoreCase)
                ? MaterialEditorThemeMode.Dark
                : MaterialEditorThemeMode.Legacy;
        }

        internal virtual void WatchTexChanges_SettingChanged(object sender, EventArgs e)
        {
            if (!WatchTexChanges.Value)
                MaterialEditorUI.DisposeTexChangeWatcher();
        }

        internal virtual void ShaderOptimization_SettingChanged(object sender, EventArgs e) { }

        internal virtual void ConfigExportPath_SettingChanged(object sender, EventArgs e)
        {
            SetExportPath();
        }

        private void SetExportPath()
        {
            if (ConfigExportPath.Value == "")
                ExportPath = ExportPathDefault;
            else
                ExportPath = ConfigExportPath.Value;
        }


    }
}
