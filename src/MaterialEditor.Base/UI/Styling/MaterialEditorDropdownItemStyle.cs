using System;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorDropdownItemStyle : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Text _text;
        private bool _listening;

        internal void Configure(Toggle toggle, Text text)
        {
            if (_listening && !ReferenceEquals(_toggle, toggle))
                Unbind();
            _toggle = toggle;
            _text = text;
            if (isActiveAndEnabled)
                Bind();
            Refresh();
        }

        private void OnEnable()
        {
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_listening || _toggle == null)
                return;
            _toggle.onValueChanged.AddListener(OnValueChanged);
            _listening = true;
        }

        private void Unbind()
        {
            if (!_listening)
                return;
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnValueChanged);
            _listening = false;
        }

        private void OnValueChanged(bool selected)
        {
            MaterialEditorStyles.ApplyDropdownItemState(_toggle, _text, selected);
        }

        internal void ReapplyTheme()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_toggle != null)
                MaterialEditorStyles.ApplyDropdownItemState(
                    _toggle,
                    _text,
                    _toggle.isOn);
        }
    }
}
