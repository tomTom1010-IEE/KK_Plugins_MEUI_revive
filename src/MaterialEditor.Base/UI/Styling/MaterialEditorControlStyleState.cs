using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorControlStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorControlStyleRole _role;
        [SerializeField] private bool _logicalState;
        [SerializeField] private bool _available = true;
        [SerializeField] private MaterialEditorControlAvailabilityMode _availabilityMode;
        [SerializeField] private bool _assigned;
        private bool _applying;

        internal MaterialEditorControlStyleRole Role => _role;
        internal bool LogicalState => _logicalState;
        internal bool Available => _available;
        internal MaterialEditorControlAvailabilityMode AvailabilityMode =>
            _availabilityMode;

        internal static MaterialEditorControlStyleState Assign(
            Selectable selectable,
            MaterialEditorControlStyleRole role)
        {
            if (selectable == null)
                return null;

            var state = selectable.GetComponent<MaterialEditorControlStyleState>();
            if (state == null)
                state = selectable.gameObject.AddComponent<MaterialEditorControlStyleState>();
            if (!state._assigned)
            {
                state._available = true;
                state._availabilityMode =
                    MaterialEditorControlAvailabilityMode.Disabled;
            }
            state._role = role;
            state._assigned = true;
            return state;
        }

        internal void SetLogicalState(bool value)
        {
            if (_logicalState == value)
                return;
            _logicalState = value;
            if (_assigned && !_applying)
                MaterialEditorStyles.ReapplyControlState(this);
        }

        internal void SetAvailability(
            bool available,
            MaterialEditorControlAvailabilityMode mode)
        {
            _available = available;
            _availabilityMode = mode;
        }

        internal bool BeginApply()
        {
            if (_applying)
                return false;
            _applying = true;
            return true;
        }

        internal void EndApply()
        {
            _applying = false;
        }

        private void OnEnable()
        {
            // AddComponent invokes OnEnable before Assign can set the role.
            // Existing pooled controls, however, already own complete semantic
            // state and must restore it synchronously when reactivated.
            if (_assigned && !_applying)
                MaterialEditorStyles.ReapplyControlState(this);
        }
    }
}
