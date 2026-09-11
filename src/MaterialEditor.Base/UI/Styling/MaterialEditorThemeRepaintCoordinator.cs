using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal sealed class MaterialEditorThemeRepaintCoordinator : MonoBehaviour
    {
        private Coroutine _routine;
        private int _generation;
        private bool _pending;

        internal void RequestRepaint()
        {
            _pending = true;
            _generation++;
            SynchronizeActiveText();

            if (isActiveAndEnabled)
                Restart(_generation);
        }

        private void OnEnable()
        {
            if (_pending)
                Restart(_generation);
        }

        private void OnDisable()
        {
            _generation++;
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = null;
        }

        private void Restart(int generation)
        {
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(RepaintAfterActivation(generation));
        }

        private IEnumerator RepaintAfterActivation(int generation)
        {
            // One frame lets parent activation and virtual-row binding finish.
            yield return null;
            if (!CanContinue(generation))
                yield break;
            SynchronizeActiveText();

            // A second frame catches dropdown clones and the final ColorTint
            // tick without rebuilding presentation data or the row layout.
            yield return null;
            if (!CanContinue(generation))
                yield break;
            SynchronizeActiveText();

            _pending = false;
            _routine = null;
        }

        private bool CanContinue(int generation)
        {
            return generation == _generation && isActiveAndEnabled;
        }

        private void SynchronizeActiveText()
        {
            MaterialEditorStyles.RefreshTextRendering(gameObject, false);
        }
    }
}
