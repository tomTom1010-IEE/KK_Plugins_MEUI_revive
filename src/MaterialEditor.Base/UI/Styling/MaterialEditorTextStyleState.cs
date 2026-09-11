using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorTextStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorTextRole _role;
        [SerializeField] private bool _assigned;

        internal MaterialEditorTextRole Role => _role;
        internal bool Assigned => _assigned;

        internal void SetRole(MaterialEditorTextRole role)
        {
            _role = role;
            _assigned = true;
        }

        private void OnEnable()
        {
            var text = GetComponent<Text>();
            if (!_assigned)
            {
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
                return;
            }

            MaterialEditorPanelTextStyles.ApplyText(text, _role);
            var owner = GetComponentInParent<MaterialEditorControlStyleState>();
            if (owner != null)
                MaterialEditorStyles.ReapplyControlState(owner);
        }
    }
}
