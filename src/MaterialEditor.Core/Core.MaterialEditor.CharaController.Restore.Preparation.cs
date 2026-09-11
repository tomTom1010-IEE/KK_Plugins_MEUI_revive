using MaterialEditorAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        private readonly List<MaterialEditorCubemapImportCoordinator> _restorePreparations =
            new List<MaterialEditorCubemapImportCoordinator>();
        private int _restoreGeneration;
        private Coroutine _restoreRoutine;
        private MonoBehaviour _restoreHost;
        private bool _restorePending;
        private bool _restoreClothes, _restoreAccessories, _restoreHair, _restoreBody;

        // Only built-in callers use this owner. Public LoadData keeps its original
        // non-preparing execution contract. Coalescing preserves all requested scopes.
        internal void RequestPreparedRestore(bool clothes, bool accessories, bool hair, bool body = true)
        {
            _restoreClothes |= clothes;
            _restoreAccessories |= accessories;
            _restoreHair |= hair;
            _restoreBody |= body;
            _restorePending = true;
            CancelRestorePreparations();
            ResumePreparedRestore();
        }

        private void ResumePreparedRestore()
        {
            if (!_restorePending || _restoreRoutine != null || !isActiveAndEnabled || ChaControl == null
                || !ChaControl.gameObject.activeInHierarchy) return;
            _restoreHost = ChaControl;
            _restoreRoutine = _restoreHost.StartCoroutine(RunPreparedRestore());
        }

        private IEnumerator RunPreparedRestore()
        {
            var generation = _restoreGeneration;
            var load = LoadDataCore(_restoreClothes, _restoreAccessories, _restoreHair, _restoreBody, true);
            try
            {
                while (AdvanceRestore(load)) yield return load.Current;
            }
            finally
            {
                (load as IDisposable)?.Dispose();
                CompletePreparedRestore(generation);
            }
        }

        private static bool AdvanceRestore(IEnumerator load)
        {
            try { return load.MoveNext(); }
            catch (Exception ex)
            {
                MaterialEditorPluginBase.Logger?.LogError("Material restore failed: " + ex);
                return false;
            }
        }

        private void CompletePreparedRestore(int generation)
        {
            if (generation != _restoreGeneration) return;
            _restorePending = false;
            _restoreClothes = _restoreAccessories = _restoreHair = _restoreBody = false;
            _restoreRoutine = null;
            _restoreHost = null;
        }

        private void CancelRestorePreparations()
        {
            _restoreGeneration++;
            if (_restoreHost != null && _restoreRoutine != null) _restoreHost.StopCoroutine(_restoreRoutine);
            _restoreRoutine = null;
            _restoreHost = null;
            foreach (var preparation in _restorePreparations.ToArray()) preparation.Dispose();
            _restorePreparations.Clear();
        }

        private void OnDisable()
        {
            _textureImports.CancelAll();
            CancelRestorePreparations();
        }
        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            ResumePreparedRestore();
        }

        private IEnumerator PrepareRestoreCubemaps(CharacterRestoreScope scope, int generation)
        {
            var visited = new HashSet<int>();
            foreach (var property in MaterialCubemapPropertyList.ToArray())
            {
                if (generation != _restoreGeneration || this == null) yield break;
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)
                    || (property.ObjectType == ObjectType.Character && !scope.Body)
                    || !property.TexID.HasValue
                    || MaterialEditorPluginBase.Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                var id = property.TexID.Value;
                if (!visited.Add(id) || CubemapLeases.HasLiveLease(id)) continue;
                TextureContainer source;
                if (!TextureDictionary.TryGetValue(id, out source)) continue;
                var bytes = source.Data;
                var preparation = new MaterialEditorCubemapImportCoordinator(bytes);
                _restorePreparations.Add(preparation);
                try
                {
                    while (!preparation.IsTerminal)
                    {
                        if (generation != _restoreGeneration || this == null) yield break;
                        preparation.AdvanceOnMainThread();
                        if (!preparation.IsTerminal) yield return null;
                    }
                    if (generation != _restoreGeneration) yield break;
                    if (preparation.State == MaterialEditorCubemapImportState.Ready)
                    {
                        var lease = preparation.TakePreparedLease();
                        // Recheck live storage after every yield. A replaced resource
                        // must not inherit a cache lease prepared for its predecessor.
                        try
                        {
                            TextureContainer current;
                            if (TextureDictionary.TryGetValue(id, out current)
                                && ReferenceEquals(current, source) && ReferenceEquals(current.Data, bytes)
                                && MaterialCubemapPropertyList.Any(x => x.TexID == id))
                            {
                                CubemapLeases.Store(id, lease);
                                lease = null;
                            }
                        }
                        finally { lease?.Dispose(); }
                    }
                    else if (preparation.State == MaterialEditorCubemapImportState.Failed)
                    {
                        // Do not drop the record. The unchanged application path
                        // remains the compatibility fallback for admission failures.
                        MaterialEditorPluginBase.Logger?.LogWarning("Cubemap preparation: " + preparation.Error);
                    }
                }
                finally
                {
                    preparation.Dispose();
                    _restorePreparations.Remove(preparation);
                }
            }
        }
    }
}
