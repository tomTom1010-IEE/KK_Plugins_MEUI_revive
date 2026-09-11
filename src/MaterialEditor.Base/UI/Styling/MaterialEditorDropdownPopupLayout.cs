using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    // Geometry only; the popup component retains registration and activation ownership.
    internal static class MaterialEditorDropdownPopupLayout
    {
        internal static void Fit(Dropdown dropdown, Transform popup)
        {
            if (popup.gameObject.name != "Dropdown List" || dropdown == null)
                return;

            var popupRect = popup as RectTransform;
            var dropdownRect = dropdown.transform as RectTransform;
            var measurementText = dropdown.itemText ?? dropdown.captionText;
            if (popupRect == null || measurementText == null)
                return;

            Canvas.ForceUpdateCanvases();

            var preferredTextWidth = 0f;
            var options = dropdown.options;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                    continue;
                preferredTextWidth = Mathf.Max(
                    preferredTextWidth,
                    MaterialEditorTextFitting.MeasurePreferredWidth(
                        measurementText,
                        option.text,
                        MaterialEditorLayout.DropdownFontSize));
            }

            var currentWidth = dropdownRect != null
                               && dropdownRect.rect.width > 0f
                ? dropdownRect.rect.width
                : popupRect.rect.width;
            var desiredWidth = Mathf.Max(
                currentWidth,
                preferredTextWidth
                + MaterialEditorTheme.Metrics.DropdownPopupHorizontalPadding);
            desiredWidth = Mathf.Min(
                desiredWidth,
                MaterialEditorTheme.Metrics.DropdownPopupMaximumWidth);

            var canvas = popup.GetComponentInParent<Canvas>();
            var canvasRect = canvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (canvasRect != null)
            {
                var availableWidth = canvasRect.rect.width
                                     - MaterialEditorTheme.Metrics
                                         .DropdownPopupScreenMargin * 2f;
                if (availableWidth <= 0f)
                    return;
                desiredWidth = Mathf.Min(desiredWidth, availableWidth);
            }

            popupRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                desiredWidth);
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            Canvas.ForceUpdateCanvases();

            if (canvasRect != null)
            {
                AlignRightEdgeToDropdown(
                    popupRect,
                    dropdownRect,
                    canvasRect);
                ClampHorizontallyToCanvas(popupRect, canvasRect);
            }

            RefreshAdaptiveItemText(popup);
        }

        private static void ClampHorizontallyToCanvas(
            RectTransform popupRect,
            RectTransform canvasRect)
        {
            var bounds = GetHorizontalBoundsInCanvas(popupRect, canvasRect);
            var minimum = canvasRect.rect.xMin
                          + MaterialEditorTheme.Metrics.DropdownPopupScreenMargin;
            var maximum = canvasRect.rect.xMax
                          - MaterialEditorTheme.Metrics.DropdownPopupScreenMargin;
            var shift = bounds.x < minimum
                ? minimum - bounds.x
                : bounds.y > maximum
                    ? maximum - bounds.y
                    : 0f;
            if (Mathf.Approximately(shift, 0f) || popupRect.parent == null)
                return;

            MoveHorizontallyInCanvasUnits(popupRect, canvasRect, shift);
        }

        private static void AlignRightEdgeToDropdown(
            RectTransform popupRect,
            RectTransform dropdownRect,
            RectTransform canvasRect)
        {
            if (dropdownRect == null || popupRect.parent == null)
                return;

            var dropdownBounds =
                GetHorizontalBoundsInCanvas(dropdownRect, canvasRect);
            var popupBounds =
                GetHorizontalBoundsInCanvas(popupRect, canvasRect);
            var shift = dropdownBounds.y - popupBounds.y;
            if (Mathf.Approximately(shift, 0f))
                return;

            MoveHorizontallyInCanvasUnits(popupRect, canvasRect, shift);
            Canvas.ForceUpdateCanvases();
        }

        private static void RefreshAdaptiveItemText(Transform popup)
        {
            var fitters =
                popup.GetComponentsInChildren<MaterialEditorAdaptiveTextFitter>(true);
            for (var i = 0; i < fitters.Length; i++)
                fitters[i].RefreshNow();

            var popupRect = popup as RectTransform;
            if (popupRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
        }

        private static void MoveHorizontallyInCanvasUnits(
            RectTransform popupRect,
            RectTransform canvasRect,
            float shift)
        {
            if (popupRect == null
                || canvasRect == null
                || popupRect.parent == null)
                return;

            var worldShift = canvasRect.TransformVector(new Vector3(shift, 0f, 0f));
            var parentShift = popupRect.parent.InverseTransformVector(worldShift);
            popupRect.anchoredPosition +=
                new Vector2(parentShift.x, parentShift.y);
        }

        private static Vector2 GetHorizontalBoundsInCanvas(
            RectTransform rectTransform,
            RectTransform canvasRect)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            for (var i = 0; i < corners.Length; i++)
            {
                var canvasPoint = canvasRect.InverseTransformPoint(corners[i]);
                minimum = Mathf.Min(minimum, canvasPoint.x);
                maximum = Mathf.Max(maximum, canvasPoint.x);
            }

            return new Vector2(minimum, maximum);
        }
    }
}
