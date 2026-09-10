using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Set the initial scroll position of the dropdown to the position of the selected item.
    /// </summary>
    internal class AutoScrollToSelectionWithDropdown : MonoBehaviour
    {
        public static void Setup(Dropdown dropdown)
        {
            var scrollbar = dropdown.GetComponentInChildren<Scrollbar>(true);

            if (scrollbar == null)
                return;

            var assComp = scrollbar.GetComponent<AutoScrollToSelectionWithDropdown>();
            if (assComp == null)
                assComp = scrollbar.gameObject.AddComponent<AutoScrollToSelectionWithDropdown>();
            assComp._target = dropdown;
            assComp.enabled = true;
        }

        [SerializeField] private Dropdown _target;

        private bool _autoScrolled;

        private void OnEnable()
        {
            //No scrolling until LateUpdate when internal setup is complete
            _autoScrolled = false;
        }

        private void LateUpdate()
        {
            if (_autoScrolled)
                return;

            _autoScrolled = true;
            AutoScroll();
            // Runtime dropdown templates are cloned per opening. After the
            // single post-layout pass there is no reason to poll every frame.
            enabled = false;
        }

        private void AutoScroll()
        {
            if (_target == null)
                return;

            var selectedIndex = _target.value;
            var items = _target.options.Count;
            var filter = GetComponentInParent<DropdownFilter>();
            if (filter != null
                && !filter.TryGetVisibleOptionPosition(
                    selectedIndex,
                    out selectedIndex,
                    out items))
            {
                // The selected option is filtered out. Keep the filter-owned
                // scroll position instead of centering an invisible full-list
                // index into the shortened content.
                return;
            }

            if (items <= 1)
                return;

            var scrollbar = GetComponent<Scrollbar>();

            if (scrollbar == null)
                return;

            //x = 0, y = 1
            int axis = (scrollbar.direction < Scrollbar.Direction.BottomToTop ? 0 : 1);

            var scrollRect = _target.template.GetComponent<ScrollRect>();

            float viewSize = scrollRect.viewport.rect.size[axis];
            float itemSize = 20f;

            if (_target.itemText != null)
            {
                var itemRect = (RectTransform)_target.itemText.transform.parent;
                itemSize = itemRect.rect.size[axis];
            }
            else if (_target.itemImage != null)
            {
                var itemRect = (RectTransform)_target.itemImage.transform.parent;
                itemSize = itemRect.rect.size[axis];
            }

            float viewAreaRatio = (viewSize / itemSize) / items;

            float scroll = (float)selectedIndex / items - viewAreaRatio * 0.5f;
            scroll = Mathf.Clamp(scroll, 0f, 1f - viewAreaRatio);
            scroll = Mathf.InverseLerp(0, 1f - viewAreaRatio, scroll);

            scrollbar.value = Mathf.Clamp01(1.0f - scroll);
        }
    }
}
