using MaterialEditorAPI;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Add a float property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialFloatProperty(int slot, ObjectType objectType, Material material, string propertyName, float value, GameObject go, bool setProperty = true)
        {
            var materialProperty = FloatPropertyQuery.First(MaterialFloatPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
            if (materialProperty == null)
            {
                float valueOriginal = material.GetFloat($"_{propertyName}");
                MaterialFloatPropertyList.Add(new MaterialFloatProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value.ToString(CultureInfo.InvariantCulture), valueOriginal.ToString(CultureInfo.InvariantCulture)));
            }
            else
            {
                if (value.ToString(CultureInfo.InvariantCulture) == materialProperty.ValueOriginal)
                    RemoveMaterialFloatProperty(slot, objectType, material, propertyName, go, false);
                else
                    materialProperty.Value = value.ToString(CultureInfo.InvariantCulture);
            }
            if (setProperty)
                SetFloat(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var value = FloatPropertyQuery.First(MaterialFloatPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName))?.Value;
            if (value.IsNullOrEmpty())
                return null;
            float parsedValue;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : (float?)null;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public float? GetMaterialFloatPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            var valueOriginal = FloatPropertyQuery.First(MaterialFloatPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName))?.ValueOriginal;
            if (valueOriginal.IsNullOrEmpty())
                return null;
            float parsedValue;
            return float.TryParse(valueOriginal, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                ? parsedValue
                : (float?)null;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialFloatProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialFloatPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetFloat(go, material.NameFormatted(), propertyName, (float)original);
            }
            FloatPropertyQuery.RemoveAll(MaterialFloatPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
        }

        /// <summary>
        /// Add a keyword property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialKeywordProperty(int slot, ObjectType objectType, Material material, string propertyName, bool value, GameObject go, bool setProperty = true)
        {
            var materialProperty = KeywordPropertyQuery.First(MaterialKeywordPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
            if (materialProperty == null)
            {
                bool valueOriginal = material.IsKeywordEnabled($"_{propertyName}");
                MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == materialProperty.ValueOriginal)
                    RemoveMaterialKeywordProperty(slot, objectType, material, propertyName, go, false);
                else
                    materialProperty.Value = value;
            }
            if (setProperty)
                SetKeyword(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public bool? GetMaterialKeywordPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return KeywordPropertyQuery.First(MaterialKeywordPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName))?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public bool? GetMaterialKeywordPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return KeywordPropertyQuery.First(MaterialKeywordPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName))?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialKeywordProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialKeywordPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetKeyword(go, material.NameFormatted(), propertyName, (bool)original);
            }
            KeywordPropertyQuery.RemoveAll(MaterialKeywordPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
        }

        /// <summary>
        /// Add a color property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="value">Value</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void SetMaterialColorProperty(int slot, ObjectType objectType, Material material, string propertyName, Color value, GameObject go, bool setProperty = true)
        {
            var colorProperty = ColorPropertyQuery.First(MaterialColorPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
            if (colorProperty == null)
            {
                Color valueOriginal = material.GetColor($"_{propertyName}");
                MaterialColorPropertyList.Add(new MaterialColorProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == colorProperty.ValueOriginal)
                    RemoveMaterialColorProperty(slot, objectType, material, propertyName, go, false);
                else
                    colorProperty.Value = value;
            }
            if (setProperty)
                SetColor(go, material.NameFormatted(), propertyName, value);
        }
        /// <summary>
        /// Get the saved material property value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return ColorPropertyQuery.First(MaterialColorPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName))?.Value;
        }
        /// <summary>
        /// Get the saved material property's original value or null if none is saved
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <returns>Saved material property's original value or null if none is saved</returns>
        public Color? GetMaterialColorPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return ColorPropertyQuery.First(MaterialColorPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName))?.ValueOriginal;
        }
        /// <summary>
        /// Remove the saved material property value if one is saved and optionally also update the materials
        /// </summary>
        /// <param name="slot">Slot of the clothing (0=tops, 1=bottoms, etc.), the hair (0=back, 1=front, etc.), or of the accessory. Ignored for other object types.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="material">Material being modified. Also modifies all other materials of the same name.</param>
        /// <param name="propertyName">Property of the material without the leading underscore</param>
        /// <param name="go">GameObject the material belongs to</param>
        /// <param name="setProperty">Whether to also apply the value to the materials</param>
        public void RemoveMaterialColorProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialColorPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetColor(go, material.NameFormatted(), propertyName, (Color)original);
            }
            ColorPropertyQuery.RemoveAll(MaterialColorPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
        }

        /// <summary>
        /// Add a vector property to be saved and loaded with the card and optionally also update the materials.
        /// </summary>
        public void SetMaterialVectorProperty(int slot, ObjectType objectType, Material material, string propertyName, Vector4 value, GameObject go, bool setProperty = true)
        {
            var vectorProperty = MigrateLegacyMaterialVectorProperty(slot, objectType, material.NameFormatted(), propertyName, go);
            if (vectorProperty == null)
            {
                Vector4 valueOriginal = material.GetVector($"_{propertyName}");
                if (value != valueOriginal)
                    MaterialVectorPropertyList.Add(new MaterialVectorProperty(objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName, value, valueOriginal));
            }
            else
            {
                if (value == vectorProperty.ValueOriginal)
                    RemoveMaterialVectorProperty(slot, objectType, material, propertyName, go, false);
                else
                    vectorProperty.Value = value;
            }
            if (setProperty)
                SetVector(go, material.NameFormatted(), propertyName, value);
        }

        /// <summary>
        /// Get the saved material vector value or null if none is saved.
        /// </summary>
        public Vector4? GetMaterialVectorPropertyValue(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MigrateLegacyMaterialVectorProperty(slot, objectType, material.NameFormatted(), propertyName, go)?.Value;
        }

        /// <summary>
        /// Get the saved material vector property's original value or null if none is saved.
        /// </summary>
        public Vector4? GetMaterialVectorPropertyValueOriginal(int slot, ObjectType objectType, Material material, string propertyName, GameObject go)
        {
            return MigrateLegacyMaterialVectorProperty(slot, objectType, material.NameFormatted(), propertyName, go)?.ValueOriginal;
        }

        /// <summary>
        /// Remove the saved material vector value and optionally restore the original value.
        /// </summary>
        public void RemoveMaterialVectorProperty(int slot, ObjectType objectType, Material material, string propertyName, GameObject go, bool setProperty = true)
        {
            if (setProperty)
            {
                var original = GetMaterialVectorPropertyValueOriginal(slot, objectType, material, propertyName, go);
                if (original != null)
                    SetVector(go, material.NameFormatted(), propertyName, original.Value);
            }
            VectorPropertyQuery.RemoveAll(MaterialVectorPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName));
            ColorPropertyQuery.RemoveAll(MaterialColorPropertyList, new MaterialPropertyRecordKey((int)objectType, GetCoordinateIndex(objectType), slot, material.NameFormatted(), propertyName), x => IsVectorProperty(go, x.MaterialName, x.Property));
        }

        /// <summary>
        /// Migrates a Color-backed vector override to native Vector storage in memory.
        /// Native Vector data takes precedence when both representations exist.
        /// </summary>
        private MaterialVectorProperty MigrateLegacyMaterialVectorProperty(int slot, ObjectType objectType, string materialName, string propertyName, GameObject go)
        {
            int coordinateIndex = GetCoordinateIndex(objectType);
            var vectorProperty = VectorPropertyQuery.First(MaterialVectorPropertyList, new MaterialPropertyRecordKey((int)objectType, coordinateIndex, slot, materialName, propertyName));
            if (!IsVectorProperty(go, materialName, propertyName))
                return vectorProperty;

            var legacyColorProperty = ColorPropertyQuery.First(MaterialColorPropertyList, new MaterialPropertyRecordKey((int)objectType, coordinateIndex, slot, materialName, propertyName));
            if (vectorProperty != null)
            {
                if (legacyColorProperty != null)
                    MaterialColorPropertyList.Remove(legacyColorProperty);
                if (vectorProperty.Value == vectorProperty.ValueOriginal)
                {
                    MaterialVectorPropertyList.Remove(vectorProperty);
                    return null;
                }
                return vectorProperty;
            }
            if (legacyColorProperty == null)
                return null;

            var legacyValue = new Vector4(legacyColorProperty.Value.r, legacyColorProperty.Value.g, legacyColorProperty.Value.b, legacyColorProperty.Value.a);
            var legacyValueOriginal = new Vector4(legacyColorProperty.ValueOriginal.r, legacyColorProperty.ValueOriginal.g, legacyColorProperty.ValueOriginal.b, legacyColorProperty.ValueOriginal.a);
            if (legacyValue != legacyValueOriginal)
            {
                vectorProperty = new MaterialVectorProperty(
                    legacyColorProperty.ObjectType,
                    legacyColorProperty.CoordinateIndex,
                    legacyColorProperty.Slot,
                    legacyColorProperty.MaterialName,
                    legacyColorProperty.Property,
                    legacyValue,
                    legacyValueOriginal);
                MaterialVectorPropertyList.Add(vectorProperty);
            }

            MaterialColorPropertyList.Remove(legacyColorProperty);
            return vectorProperty;
        }

        private void RemoveLegacyMaterialVectorDuplicates()
        {
            MaterialVectorPropertyList.RemoveAll(vector => vector.Value == vector.ValueOriginal);
            // Color-backed Vector entries are removed by MigrateLegacyMaterialVectorProperty
            // only after the active shader manifest confirms that the property is a
            // Vector. Keeping Color entries here prevents a stale Vector entry from
            // discarding a newer Color edit when the manifest metadata changes.
        }

    }
}
