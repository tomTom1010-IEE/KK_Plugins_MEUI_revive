using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorDropdownPopupStyle : MonoBehaviour
    {
        private static int _openPopupCount;

        [SerializeField] private Dropdown _dropdown;
        private bool _started;
        private bool _registeredOpen;

        internal static bool AnyPopupOpen => _openPopupCount > 0;

        internal void Configure(Dropdown dropdown)
        {
            _dropdown = dropdown;
            if (_started && isActiveAndEnabled)
                Apply();
        }

        private void OnEnable()
        {
            RegisterRuntimePopup();
            if (_started)
                Apply();
        }

        private void Start()
        {
            _started = true;
            RegisterRuntimePopup();
            Apply();
        }

        private void OnDisable()
        {
            UnregisterRuntimePopup();
        }

        private void OnDestroy()
        {
            UnregisterRuntimePopup();
        }

        private void RegisterRuntimePopup()
        {
            // uGUI renames the cloned template root before activating it. The
            // original disabled template remains named "Template" and is never
            // counted, while every Material Editor dropdown clone is tracked.
            if (_registeredOpen || gameObject.name != "Dropdown List")
                return;
            _registeredOpen = true;
            _openPopupCount++;
        }

        private void UnregisterRuntimePopup()
        {
            if (!_registeredOpen)
                return;
            _registeredOpen = false;
            if (_openPopupCount > 0)
                _openPopupCount--;
        }

        internal void ReapplyTheme()
        {
            Apply();
        }

        private void Apply()
        {
            MaterialEditorStyles.ApplyDropdownPopup(
                _dropdown,
                transform,
                null,
                null);
            MaterialEditorDropdownPopupLayout.Fit(_dropdown, transform);
        }

    }
}
