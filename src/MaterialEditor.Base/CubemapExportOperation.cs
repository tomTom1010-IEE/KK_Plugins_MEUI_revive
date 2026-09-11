using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static partial class MaterialEditorCubemapConversion
    {
        /// <summary>
        /// Snapshot, row-budgeted projection, then Unity encoding. All Unity work
        /// runs on the pumping thread. A reservation survives until writing completes.
        /// </summary>
        internal sealed class ExportOperation : IDisposable
        {
            private Cubemap _source;
            private readonly int _size;
            private Color[][] _faces;
            private Color32[] _output;
            private byte[] _data;
            private int _row;
            private bool _disposed;
            private MaterialEditorCubemapMemoryReservation _reservation;

            internal ExportOperation(Cubemap source, MaterialEditorCubemapMemoryReservation reservation)
            {
                _source = source;
                _size = source.width;
                _reservation = reservation;
                Stage = "Readback";
            }

            internal string Stage { get; private set; }
            internal bool IsComplete { get; private set; }

            internal bool Advance(out string error)
            {
                error = null;
                if (_disposed) { error = "Cubemap export was disposed."; return false; }
                if (IsComplete) return true;
                try
                {
                    if (_faces == null && _output == null)
                    {
                        // Capture all faces in one call, before yielding. External code
                        // can mutate a Cubemap in place without changing its reference;
                        // spreading capture over frames would mix source generations.
                        if (_source == null || !TryReadFaces(_source, out _faces, out error))
                        {
                            if (error == null) error = "The export source no longer exists.";
                            return false;
                        }
                        _source = null;
                        _output = new Color32[_size * 4 * _size * 2];
                        Stage = "Projection";
                        return true;
                    }
                    var width = _size * 4;
                    var height = _size * 2;
                    if (_row < height)
                    {
                        var budget = new MaterialWorkBudget(MaterialWorkBudget.DefaultMilliseconds, MaterialWorkBudget.DefaultRowLimit);
                        while (_row < height && budget.TryStartUnit())
                        {
                            ProjectRow(width, height);
                            _row++;
                        }
                        if (_row == height) { _faces = null; Stage = "Encoding"; }
                        return true;
                    }
                    Texture2D panorama = null;
                    try
                    {
                        panorama = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        panorama.SetPixels32(_output);
                        panorama.Apply(false, false);
                        _data = panorama.EncodeToPNG();
                    }
                    finally
                    {
                        if (panorama != null) UnityEngine.Object.Destroy(panorama);
                        _output = null;
                    }
                    if (_data == null || _data.Length == 0)
                    {
                        error = "Cubemap export failed because Unity returned no PNG data.";
                        return false;
                    }
                    IsComplete = true;
                    Stage = "Ready";
                    return true;
                }
                catch (Exception ex) { error = "Cubemap export failed: " + ex.Message; return false; }
            }

            private void ProjectRow(int width, int height)
            {
                var panoramaV = (_row + 0.5) / height;
                for (var x = 0; x < width; x++)
                {
                    var panoramaU = (x + 0.5) / width;
                    double directionX, directionY, directionZ;
                    MaterialEditorCubemapProjection.DirectionForEquirectangularUv(
                        panoramaU, panoramaV, out directionX, out directionY, out directionZ);
                    MaterialEditorCubemapFace face;
                    double faceU, faceV;
                    MaterialEditorCubemapProjection.FaceUvForDirection(
                        directionX, directionY, directionZ, out face, out faceU, out faceV);
                    _output[_row * width + x] = SampleCubemap(_faces, _size, face, faceU, faceV);
                }
            }

            internal byte[] TakeData()
            {
                if (!IsComplete || _disposed) return null;
                var data = _data;
                _data = null;
                return data;
            }

            internal IDisposable TakeReservation()
            {
                var reservation = _reservation;
                _reservation = null;
                return reservation;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _source = null;
                _faces = null;
                _output = null;
                _data = null;
                _reservation?.Dispose();
                _reservation = null;
            }
        }
    }
}
