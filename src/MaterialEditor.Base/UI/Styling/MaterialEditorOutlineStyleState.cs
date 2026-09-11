using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorOutlineStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorThemeColorRole _role;

        internal MaterialEditorThemeColorRole Role => _role;

        internal static MaterialEditorOutlineStyleState Assign(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return null;
            var state = graphic.GetComponent<MaterialEditorOutlineStyleState>()
                        ?? graphic.gameObject.AddComponent<MaterialEditorOutlineStyleState>();
            state._role = role;
            return state;
        }
    }
}
