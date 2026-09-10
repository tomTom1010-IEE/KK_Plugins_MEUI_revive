using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Class containing material data, used for copy and paste of material edits
    /// </summary>
    public class CopyContainer
    {
        /// <summary>
        /// List of float property edits
        /// </summary>
        public List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        /// <summary>
        /// List of keyword property edits
        /// </summary>
        public List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        /// <summary>
        /// List of color property edits
        /// </summary>
        public List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        public List<MaterialVectorProperty> MaterialVectorPropertyList = new List<MaterialVectorProperty>();
        /// <summary>
        /// List of texture property edits
        /// </summary>
        public List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        public List<MaterialCubemapProperty> MaterialCubemapPropertyList = new List<MaterialCubemapProperty>();
        /// <summary>
        /// List of shader edits
        /// </summary>
        public List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        /// <summary>
        /// List of projector edits
        /// </summary>
        public List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();

        /// <summary>
        /// Gets a value indicating whether the container is empty (contains no copied edits)
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return !HasAny(MaterialFloatPropertyList)
                       && !HasAny(MaterialKeywordPropertyList)
                       && !HasAny(MaterialColorPropertyList)
                       && !HasAny(MaterialVectorPropertyList)
                       && !HasAny(MaterialTexturePropertyList)
                       && !HasAny(MaterialCubemapPropertyList)
                       && !HasAny(MaterialShaderList)
                       && !HasAny(ProjectorPropertyList);
            }
        }

        private static bool HasAny<T>(IList<T> values) where T : class
        {
            if (values == null)
                return false;
            for (var index = 0; index < values.Count; index++)
                if (values[index] != null)
                    return true;
            return false;
        }

        /// <summary>
        /// Clears all copied edits from the container
        /// </summary>
        public void ClearAll()
        {
            MaterialFloatPropertyList = new List<MaterialFloatProperty>();
            MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
            MaterialColorPropertyList = new List<MaterialColorProperty>();
            MaterialVectorPropertyList = new List<MaterialVectorProperty>();
            MaterialTexturePropertyList = new List<MaterialTextureProperty>();
            MaterialCubemapPropertyList = new List<MaterialCubemapProperty>();
            MaterialShaderList = new List<MaterialShader>();
            ProjectorPropertyList = new List<ProjectorProperty>();
        }

        /// <summary>
        /// Data storage class for float properties
        /// </summary>
        public class MaterialFloatProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public float Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialFloatProperty"/> class with the specified property name and value.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialFloatProperty(string property, float value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for keyword properties
        /// </summary>
        public class MaterialKeywordProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public bool Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialKeywordProperty"/> class with the specified property name and value.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialKeywordProperty(string property, bool value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for color properties
        /// </summary>
        public class MaterialColorProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public Color Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialColorProperty"/> class with the specified property name and value.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialColorProperty(string property, Color value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for vector properties
        /// </summary>
        public class MaterialVectorProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Value
            /// </summary>
            public Vector4 Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialVectorProperty"/> class with the specified property name and value.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public MaterialVectorProperty(string property, Vector4 value)
            {
                Property = property;
                Value = value;
            }
        }

        /// <summary>
        /// Data storage class for texture properties
        /// </summary>
        public class MaterialTextureProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// ID of the texture as stored in the texture dictionary
            /// </summary>
            public byte[] Data;
            /// <summary>
            /// Texture offset value
            /// </summary>
            public Vector2? Offset;
            /// <summary>
            /// Texture scale value
            /// </summary>
            public Vector2? Scale;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialTextureProperty"/> class with the specified property name, texture data, offset and scale.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="data">Byte array containing the texture</param>
            /// <param name="offset">Texture offset value</param>
            /// <param name="scale">Texture scale value</param>
            public MaterialTextureProperty(string property, byte[] data = null, Vector2? offset = null, Vector2? scale = null)
            {
                Property = property;
                Data = data;
                Offset = offset;
                Scale = scale;
            }
        }

        /// <summary>
        /// Data storage class for cubemap properties
        /// </summary>
        public class MaterialCubemapProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public string Property;
            /// <summary>
            /// Encoded PNG or Radiance HDR Cubemap source data.
            /// </summary>
            public byte[] Data;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialCubemapProperty"/> class with the specified property name and data.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="data">Byte array containing the cubemap data</param>
            public MaterialCubemapProperty(string property, byte[] data = null)
            {
                Property = property;
                Data = data;
            }
        }

        /// <summary>
        /// Data storage class for shader data
        /// </summary>
        public class MaterialShader
        {
            /// <summary>
            /// Name of the shader
            /// </summary>
            public string ShaderName;
            /// <summary>
            /// Render queue
            /// </summary>
            public int? RenderQueue;

            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialShader"/> class with the specified shader name and render queue.
            /// </summary>
            /// <param name="shaderName">Name of the shader</param>
            /// <param name="renderQueue">Render queue</param>
            public MaterialShader(string shaderName, int? renderQueue)
            {
                ShaderName = shaderName;
                RenderQueue = renderQueue;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialShader"/> class with the specified shader name.
            /// </summary>
            /// <param name="shaderName">Name of the shader</param>
            public MaterialShader(string shaderName)
            {
                ShaderName = shaderName;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="MaterialShader"/> class with the specified render queue.
            /// </summary>
            /// <param name="renderQueue">Render queue</param>
            public MaterialShader(int? renderQueue)
            {
                RenderQueue = renderQueue;
            }

            /// <summary>
            /// Checks if the shader name and render queue are both null. Safe to delete this data if true.
            /// </summary>
            /// <returns>True if both ShaderName and RenderQueue are null; otherwise false.</returns>
            public bool NullCheck() => ShaderName.IsNullOrEmpty() && RenderQueue == null;
        }

        /// <summary>
        /// Data storage class for projector properties
        /// </summary>
        public class ProjectorProperty
        {
            /// <summary>
            /// Name of the property
            /// </summary>
            public MaterialAPI.ProjectorProperties Property;
            /// <summary>
            /// Value
            /// </summary>
            public float Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="ProjectorProperty"/> class with the specified property and value.
            /// </summary>
            /// <param name="property">Name of the property</param>
            /// <param name="value">Value</param>
            public ProjectorProperty(MaterialAPI.ProjectorProperties property, float value)
            {
                Property = property;
                Value = value;
            }
        }
    }
}
