using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorDropdownCaptionFitter : MonoBehaviour
    {
        private const string Ellipsis = "...";

        [SerializeField] private Dropdown _dropdown;
        private Text _caption;
        private string _lastFullText;
        private string _lastDisplayText;
        private float _lastWidth = -1f;

        internal static void Configure(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            var fitter =
                dropdown.GetComponent<MaterialEditorDropdownCaptionFitter>()
                ?? dropdown.gameObject
                    .AddComponent<MaterialEditorDropdownCaptionFitter>();
            fitter.ConfigureInternal(dropdown);
        }

        internal static void Refresh(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            Configure(dropdown);
        }

        internal static void Refresh(
            Dropdown dropdown,
            string fullText)
        {
            if (dropdown == null)
                return;

            var fitter =
                dropdown.GetComponent<MaterialEditorDropdownCaptionFitter>()
                ?? dropdown.gameObject
                    .AddComponent<MaterialEditorDropdownCaptionFitter>();
            fitter.ConfigureInternal(dropdown);
            fitter.RefreshText(true, fullText ?? string.Empty);
        }

        private void ConfigureInternal(Dropdown dropdown)
        {
            if (_dropdown != null)
                _dropdown.onValueChanged.RemoveListener(HandleValueChanged);

            _dropdown = dropdown;
            _caption = dropdown.captionText;
            _dropdown.onValueChanged.AddListener(HandleValueChanged);
            RefreshText(true);
        }

        private void OnEnable()
        {
            RefreshText(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshText(false);
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
                _dropdown.onValueChanged.RemoveListener(HandleValueChanged);
        }

        private void HandleValueChanged(int value)
        {
            RefreshText(true);
        }

        private void RefreshText(
            bool force,
            string explicitFullText = null)
        {
            if (_dropdown == null || _caption == null)
                return;

            var width = _caption.rectTransform.rect.width;
            if (width <= 0f)
                return;

            var fullText = explicitFullText ?? ResolveFullText();
            if (!force
                && string.Equals(
                    fullText,
                    _lastFullText,
                    StringComparison.Ordinal)
                && Mathf.Approximately(width, _lastWidth))
                return;

            var displayText = FitText(fullText, width);
            _lastFullText = fullText;
            _lastDisplayText = displayText;
            _lastWidth = width;
            if (!string.Equals(
                    _caption.text,
                    displayText,
                    StringComparison.Ordinal))
            {
                _caption.text = displayText;
            }
            MaterialEditorPanelTextStyles.RefreshTextRendering(_caption);
        }

        private string ResolveFullText()
        {
            var value = _dropdown.value;
            if (value >= 0 && value < _dropdown.options.Count)
            {
                var option = _dropdown.options[value];
                if (option != null)
                    return option.text ?? string.Empty;
            }

            var current = _caption.text ?? string.Empty;
            return string.Equals(
                       current,
                       _lastDisplayText,
                       StringComparison.Ordinal)
                   && _lastFullText != null
                ? _lastFullText
                : current;
        }

        private string FitText(string fullText, float availableWidth)
        {
            fullText = fullText ?? string.Empty;
            var fontSize = MaterialEditorLayout.DropdownFontSize;
            if (MaterialEditorTextFitting.MeasurePreferredWidth(
                    _caption,
                    fullText,
                    fontSize) <= availableWidth)
                return fullText;

            if (MaterialEditorTextFitting.MeasurePreferredWidth(
                    _caption,
                    Ellipsis,
                    fontSize) > availableWidth)
                return string.Empty;

            var low = 0;
            var high = fullText.Length;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                var safeMiddle = GetSafeSubstringLength(fullText, middle);
                var candidate =
                    fullText.Substring(0, safeMiddle).TrimEnd() + Ellipsis;
                if (MaterialEditorTextFitting.MeasurePreferredWidth(
                        _caption,
                        candidate,
                        fontSize) <= availableWidth)
                    low = middle;
                else
                    high = middle - 1;
            }

            var safeLength = GetSafeSubstringLength(fullText, low);
            return fullText.Substring(0, safeLength).TrimEnd() + Ellipsis;
        }

        private static int GetSafeSubstringLength(string value, int length)
        {
            var safeLength = Mathf.Clamp(length, 0, value.Length);
            if (safeLength > 0
                && safeLength < value.Length
                && char.IsHighSurrogate(value[safeLength - 1])
                && char.IsLowSurrogate(value[safeLength]))
            {
                safeLength--;
            }
            return safeLength;
        }
    }
}
