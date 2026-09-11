using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorPanelStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorPanelRole _role;

        internal MaterialEditorPanelRole Role => _role;

        internal void SetRole(MaterialEditorPanelRole role)
        {
            _role = role;
        }
    }
}
