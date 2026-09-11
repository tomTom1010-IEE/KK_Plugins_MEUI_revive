using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorDropdownStyles
    {
        internal static void ApplyDropdown(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            MaterialEditorControlStyleState.Assign(
                dropdown,
                MaterialEditorControlStyleRole.Dropdown);
            MaterialEditorScrollSelectableStyles.ApplySelectable(
                dropdown,
                MaterialEditorTheme.Colors.DropdownSurface,
                MaterialEditorTheme.Colors.ControlHover,
                MaterialEditorTheme.Colors.ControlPressed,
                MaterialEditorTheme.Colors.ControlDisabled);
            MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                dropdown.targetGraphic,
                MaterialEditorThemeColorRole.InputBorder);
            ApplyDropdownCaptionText(dropdown.captionText);
            ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);

            var arrow = dropdown.transform.Find("Arrow");
            if (arrow != null)
            {
                var arrowImage = arrow.GetComponent<Image>();
                if (arrowImage != null)
                    arrowImage.color = MaterialEditorTheme.Colors.SecondaryText;
            }

            if (dropdown.template != null)
            {
                var templateImage = dropdown.template.GetComponent<Image>();
                if (templateImage != null)
                {
                    templateImage.color = MaterialEditorTheme.Colors.PopupSurface;
                    templateImage.maskable = true;
                    MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                        templateImage,
                        MaterialEditorThemeColorRole.StrongBorder);
                }

                var templateScroll = dropdown.template.GetComponentInChildren<ScrollRect>(true);
                if (templateScroll != null)
                    MaterialEditorScrollSelectableStyles.ApplyDropdownScrollView(templateScroll);

                var popupStyle = dropdown.template.GetComponent<MaterialEditorDropdownPopupStyle>()
                                 ?? dropdown.template.gameObject.AddComponent<MaterialEditorDropdownPopupStyle>();
                popupStyle.Configure(dropdown);
            }

            if (dropdown.itemText != null)
            {
                var itemToggle = dropdown.itemText.GetComponentInParent<Toggle>();
                if (itemToggle != null)
                {
                    ApplyDropdownItem(itemToggle, dropdown.itemText);
                    ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);
                }
            }
            MaterialEditorPanelTextStyles.ApplyTypography(dropdown.gameObject);
            ApplyDropdownCaptionText(dropdown.captionText);
            ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);
            MaterialEditorDropdownCaptionFitter.Configure(dropdown);
        }

        internal static void ReapplyTheme(
            MaterialEditorControlStyleState state)
        {
            if (state == null
                || state.Role != MaterialEditorControlStyleRole.Dropdown)
                return;

            ApplyDropdown(state.GetComponent<Dropdown>());
        }

        internal static void ApplyDropdownPopup(
            Dropdown dropdown,
            Transform popupRoot,
            InputField filter,
            Button clearButton)
        {
            if (popupRoot == null)
                return;

            var popupImage = popupRoot.GetComponent<Image>();
            if (popupImage != null)
            {
                popupImage.color = MaterialEditorTheme.Colors.PopupSurface;
                popupImage.maskable = true;
                MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                    popupImage,
                    MaterialEditorThemeColorRole.StrongBorder);
            }

            var scroll = popupRoot.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null)
                MaterialEditorScrollSelectableStyles.ApplyDropdownScrollView(scroll);

            if (filter != null)
                MaterialEditorInputStyles.ApplyInputField(filter);
            if (clearButton != null)
                MaterialEditorSelectionStyles.ApplyButton(clearButton);

            if (dropdown != null)
            {
                ApplyDropdownCaptionText(dropdown.captionText);
                ApplyDropdownItemText(dropdown.itemText, dropdown.captionText);
            }

            var items = popupRoot.GetComponentsInChildren<Toggle>(true);
            for (var i = 0; i < items.Length; i++)
            {
                var itemText = items[i].GetComponentInChildren<Text>(true);
                ApplyDropdownItem(items[i], itemText);
                ApplyDropdownItemState(items[i], itemText, items[i].isOn);
            }
        }

        private static void ApplyDropdownCaptionText(Text text)
        {
            if (text == null)
                return;

            MaterialEditorPanelTextStyles.ApplyText(text, MaterialEditorTextRole.Input);
            MaterialEditorPanelTextStyles.ApplyTextRendering(text, null);
            text.maskable = true;
            text.raycastTarget = false;
            text.fontSize = MaterialEditorLayout.DropdownFontSize;
            text.resizeTextForBestFit = false;
            text.resizeTextMinSize = MaterialEditorLayout.DropdownFontSize;
            text.resizeTextMaxSize = MaterialEditorLayout.DropdownFontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ApplyDropdownTextInsets(text);
        }

        private static void ApplyDropdownItemText(
            Text text,
            Text renderingSource)
        {
            if (text == null)
                return;

            MaterialEditorPanelTextStyles.ApplyText(
                text,
                MaterialEditorTextRole.Input);
            MaterialEditorPanelTextStyles.ApplyTextRendering(
                text,
                renderingSource);
            text.maskable = true;
            text.raycastTarget = false;
            MaterialEditorTextFitting.ApplyAdaptiveSingleLine(
                text,
                MaterialEditorLayout.DropdownFontSize);
            ApplyDropdownTextInsets(text);
        }

        private static void ApplyDropdownTextInsets(Text text)
        {
            if (text == null)
                return;

            var rect = text.rectTransform;
            rect.offsetMin = new Vector2(
                rect.offsetMin.x,
                MaterialEditorLayout.DropdownTextVerticalInset);
            rect.offsetMax = new Vector2(
                rect.offsetMax.x,
                -MaterialEditorLayout.DropdownTextVerticalInset);

            text.SetVerticesDirty();
        }

        private static void ApplyDropdownItem(Toggle toggle, Text text)
        {
            if (toggle == null)
                return;

            if (text != null)
                TooltipManager.AddTooltip(toggle.gameObject, text.text);

            MaterialEditorScrollSelectableStyles.ApplySelectable(
                toggle,
                MaterialEditorTheme.Colors.PopupSurface,
                MaterialEditorTheme.Colors.Hover,
                MaterialEditorTheme.Colors.Pressed,
                MaterialEditorTheme.Colors.DisabledSurface);
            if (toggle.graphic != null)
            {
                toggle.graphic.color = MaterialEditorTheme.Colors.SelectedText;
            }
            ApplyDropdownItemText(text, null);

            var state = toggle.GetComponent<MaterialEditorDropdownItemStyle>()
                        ?? toggle.gameObject.AddComponent<MaterialEditorDropdownItemStyle>();
            state.Configure(toggle, text);
        }

        internal static void ApplyDropdownItemState(
            Toggle toggle,
            Text text,
            bool selected)
        {
            if (toggle == null)
                return;

            var colors = toggle.colors;
            colors.normalColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.PopupSurface;
            colors.highlightedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.Hover;
            colors.pressedColor = selected
                ? MaterialEditorTheme.Colors.Selected
                : MaterialEditorTheme.Colors.Pressed;
            colors.disabledColor = MaterialEditorTheme.Colors.DisabledSurface;
            colors.colorMultiplier = 1f;
            colors.fadeDuration =
                MaterialEditorTheme.States.SelectableFadeDuration;
            toggle.colors = colors;

            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(toggle);

            if (text != null)
            {
                if (!toggle.interactable
                    && MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
                {
                    text.color = MaterialEditorTheme.Colors.DisabledText;
                }
                else if (selected)
                {
                    text.color = MaterialEditorTheme.Colors.SelectedText;
                }
                else if (MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
                {
                    text.color = MaterialEditorTheme.Colors.PrimaryText;
                }
                else
                {
                    text.color = MaterialEditorPanelTextStyles.ResolveTextColor(
                        MaterialEditorPanelTextStyles.GetAssignedTextRole(text));
                }
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
            }
        }
    }
}
