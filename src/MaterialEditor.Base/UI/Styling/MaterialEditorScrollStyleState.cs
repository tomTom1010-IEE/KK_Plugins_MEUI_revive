using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorScrollStyleState : MonoBehaviour
    {
        [SerializeField] private bool _popup;

        internal bool Popup => _popup;
        internal bool SideList { get; set; }

        internal static MaterialEditorScrollStyleState Assign(
            ScrollRect scrollRect,
            bool popup)
        {
            if (scrollRect == null)
                return null;

            var state = scrollRect.GetComponent<MaterialEditorScrollStyleState>()
                        ?? scrollRect.gameObject.AddComponent<MaterialEditorScrollStyleState>();
            state._popup = popup;
            return state;
        }
    }
}
