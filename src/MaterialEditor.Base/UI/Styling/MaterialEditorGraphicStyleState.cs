using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorGraphicStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorThemeColorRole _role;

        internal MaterialEditorThemeColorRole Role => _role;

        internal static MaterialEditorGraphicStyleState Assign(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return null;
            var state = graphic.GetComponent<MaterialEditorGraphicStyleState>()
                        ?? graphic.gameObject.AddComponent<MaterialEditorGraphicStyleState>();
            state._role = role;
            return state;
        }
    }
}
