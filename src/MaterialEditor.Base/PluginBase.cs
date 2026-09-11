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
    /// <summary>
    /// MaterialEditor plugin base
    /// </summary>
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    public partial class MaterialEditorPluginBase : BaseUnityPlugin
    {

        /// <summary>
        /// Logger instance for the plugin
        /// </summary>
        public static new ManualLogSource Logger;
        /// <summary>
        /// Singleton instance of the plugin
        /// </summary>
        public static MaterialEditorPluginBase Instance;

        /// <summary>
        /// Default path where textures will be exported
        /// </summary>
        public static string ExportPathDefault = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor");
        /// <summary>
        /// Path where textures will be exported
        /// </summary>
        public static string ExportPath = ExportPathDefault;
        /// <summary>
        /// Default path where local textures will be exported to / imported from
        /// </summary>
        public static string LocalTexturePathDefault = Path.Combine(Paths.GameRootPath, @"UserData\MaterialEditor\_LocalTextures");
        /// <summary>
        /// Path where local textures will be exported to / imported from
        /// </summary>
        public static string LocalTexturePath = LocalTexturePathDefault;
        /// <summary>
        /// Saved material edits
        /// </summary>
        public static CopyContainer CopyData = new CopyContainer();

        /// <summary>
        /// Dictionary of loaded shaders
        /// </summary>
        public static Dictionary<string, ShaderData> LoadedShaders = new Dictionary<string, ShaderData>();
        /// <summary>
        /// Sorted dictionary of XML shader properties
        /// </summary>
        public static SortedDictionary<string, Dictionary<string, ShaderPropertyData>> XMLShaderProperties = new SortedDictionary<string, Dictionary<string, ShaderPropertyData>>();
        internal static readonly ShaderPropertyFallbackMergeState
            ShaderPropertyFallbacks = new ShaderPropertyFallbackMergeState();

        /// <summary>
        /// Init logic, do not call
        /// </summary>
        public virtual void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Directory.CreateDirectory(ExportPath);

            BindConfiguration();

            ResourceRedirection.RegisterAssetLoadedHook(HookBehaviour.OneCallbackPerResourceLoaded, AssetLoadedHook);
            ShaderDefinitionLoader.LoadDefaults();
        }

        /// <summary>
        /// Every time an asset is loaded, swap its shader for the one loaded by MaterialEditor. This reduces the number of instances of a shader once they are cleaned up by garbage collection
        /// which reduce RAM usage, etc. Also fixes KK mods in EC by swapping them to the equivalent EC shader.
        /// </summary>
        protected virtual void AssetLoadedHook(AssetLoadedContext context)
        {
            if (!ShaderOptimization.Value) return;

            if (context.Asset is GameObject go)
            {
                var renderers = go.GetComponentsInChildren<Renderer>();
                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    for (var j = 0; j < renderer.materials.Length; j++)
                    {
                        var material = renderer.materials[j];
                        if (LoadedShaders.TryGetValue(material.shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                        {
                            int renderQueue = material.renderQueue;
                            material.shader = shaderData.Shader;
                            material.renderQueue = renderQueue;
                        }
                    }
                }
            }
            else if (context.Asset is Material mat)
            {
                if (LoadedShaders.TryGetValue(mat.shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                {
                    int renderQueue = mat.renderQueue;
                    mat.shader = shaderData.Shader;
                    mat.renderQueue = renderQueue;
                }
            }
            else if (context.Asset is Shader shader)
            {
                if (LoadedShaders.TryGetValue(shader.name, out var shaderData) && shaderData.Shader != null && shaderData.ShaderOptimization)
                    context.Asset = shaderData.Shader;
            }
        }

        /// <summary>
        /// Always returns false, i.e. does nothing. Override to prevent certain materials from showing in the UI.
        /// </summary>
        /// <param name="materialName">Name of the material</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns></returns>
        public virtual bool CheckBlacklist(string materialName, string propertyName) => false;

        /// <summary>
        /// Refreshes the property organization, which groups shader properties by their categories and sorts them based on the configuration settings.
        /// </summary>
        protected static void RefreshPropertyOrganization()
        {
            PropertyOrganizer.Refresh();
        }

        internal static Texture2D GetT2D(RenderTexture renderTexture) =>
            MaterialTextureExport.GetT2D(renderTexture);

        internal static void SaveTexR(RenderTexture renderTexture, string path) =>
            MaterialTextureExport.SaveTexR(renderTexture, path);

        internal static byte[] EncodeTextureToPng(Texture2D texture) =>
            MaterialTextureExport.EncodeTextureToPng(texture);

        internal static void SaveTex(Texture tex, string path, RenderTextureFormat rtf = RenderTextureFormat.Default, RenderTextureReadWrite cs = RenderTextureReadWrite.Default) =>
            MaterialTextureExport.SaveTex(tex, path, rtf, cs);
    }
}
