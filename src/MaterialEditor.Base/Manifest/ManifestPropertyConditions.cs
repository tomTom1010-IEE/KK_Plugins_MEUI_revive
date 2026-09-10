using System;

namespace MaterialEditorAPI
{
    /// <summary>Comparison performed by a property visibility condition.</summary>
    public enum MaterialEditorConditionComparison
    {
        /// <summary>Tests for equality.</summary>
        Equal,
        /// <summary>Tests for inequality.</summary>
        NotEqual,
        /// <summary>Tests if the value is greater than the comparison value.</summary>
        GreaterThan,
        /// <summary>Tests if the value is greater than or equal to the comparison value.</summary>
        GreaterThanOrEqual,
        /// <summary>Tests if the value is less than the comparison value.</summary>
        LessThan,
        /// <summary>Tests if the value is less than or equal to the comparison value.</summary>
        LessThanOrEqual
    }

    /// <summary>A simple numeric condition referencing another shader property.</summary>
    public sealed class MaterialEditorPropertyCondition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEditorPropertyCondition"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the shader property to reference, without a leading underscore.</param>
        /// <param name="comparison">The comparison operation to perform.</param>
        /// <param name="value">Expected numeric value for the comparison.</param>
        public MaterialEditorPropertyCondition(
            string propertyName,
            MaterialEditorConditionComparison comparison,
            float value)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException(
                    "A condition property name is required.",
                    nameof(propertyName));

            PropertyName = NormalizePropertyName(propertyName);
            Comparison = comparison;
            Value = value;
        }

        /// <summary>Referenced shader property name, without a leading underscore.</summary>
        public string PropertyName { get; }
        /// <summary>The comparison operation to perform.</summary>
        public MaterialEditorConditionComparison Comparison { get; }
        /// <summary>Expected numeric value.</summary>
        public float Value { get; }

        public bool Evaluate(float currentValue)
        {
            switch (Comparison)
            {
                case MaterialEditorConditionComparison.Equal:
                    return currentValue == Value;
                case MaterialEditorConditionComparison.NotEqual:
                    return currentValue != Value;
                case MaterialEditorConditionComparison.GreaterThan:
                    return currentValue > Value;
                case MaterialEditorConditionComparison.GreaterThanOrEqual:
                    return currentValue >= Value;
                case MaterialEditorConditionComparison.LessThan:
                    return currentValue < Value;
                case MaterialEditorConditionComparison.LessThanOrEqual:
                    return currentValue <= Value;
                default:
                    return true;
            }
        }

        private static string NormalizePropertyName(string propertyName)
        {
            var normalized = propertyName.Trim();
            while (normalized.StartsWith("_", StringComparison.Ordinal))
                normalized = normalized.Substring(1);
            if (normalized.Length == 0)
            {
                throw new ArgumentException(
                    "A condition property name is required.",
                    nameof(propertyName));
            }
            foreach (var character in normalized)
            {
                if (!char.IsLetterOrDigit(character)
                    && character != '_'
                    && character != '.')
                {
                    throw new ArgumentException(
                        "A condition property name contains unsupported characters.",
                        nameof(propertyName));
                }
            }
            return normalized;
        }
    }
}
