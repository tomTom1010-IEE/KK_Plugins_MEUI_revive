using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;

using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal static class ShaderDefinitionLoader
    {
        internal static void LoadDefaults()
        {
            XMLShaderProperties["default"] = new Dictionary<string, ShaderPropertyData>();
            ShaderPropertyFallbacks.Reset();

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(MaterialEditorAPI)}.Resources.default.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);
                        XmlElement materialEditorElement = doc.DocumentElement;
                        Action<string> metadataWarning = message =>
                            MaterialEditorPluginBase.Logger?.LogWarning("Material Editor default metadata: " + message);
                        var schemaVersion = ShaderPropertyMetadataParser.ReadSchemaVersion(
                            materialEditorElement,
                            metadataWarning);

                        var shaderElements = materialEditorElement.GetElementsByTagName("Shader");
                        foreach (var shaderElementObj in shaderElements)
                        {
                            if (shaderElementObj != null)
                            {
                                var shaderElement = (XmlElement)shaderElementObj;
                                {
                                    string shaderName = shaderElement.GetAttribute("Name");

                                    XMLShaderProperties[shaderName] = new Dictionary<string, ShaderPropertyData>();

                                    var shaderPropertyElements = shaderElement.GetElementsByTagName("Property");
                                    var declarationOrder = 0;
                                    foreach (var shaderPropertyElementObj in shaderPropertyElements)
                                    {
                                        if (shaderPropertyElementObj != null)
                                        {
                                            var shaderPropertyElement = (XmlElement)shaderPropertyElementObj;
                                            {
                                                ShaderPropertyData shaderPropertyData;
                                                if (!ShaderPropertyData.TryParse(
                                                        shaderPropertyElement,
                                                        metadataWarning,
                                                        out shaderPropertyData,
                                                        schemaVersion))
                                                {
                                                    declarationOrder++;
                                                    continue;
                                                }

                                                shaderPropertyData.DeclarationOrder = declarationOrder++;
                                                ShaderPropertyFallbacks.MergeInto(
                                                    XMLShaderProperties["default"],
                                                    shaderPropertyData,
                                                    "MaterialEditor.API default",
                                                    message => MaterialEditorPluginBase.Logger?.LogWarning(
                                                        "Material Editor fallback: " + message));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
        }
    }
}
