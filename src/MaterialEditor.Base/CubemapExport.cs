using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal static partial class MaterialEditorCubemapConversion
    {
        // Synchronous compatibility entry point; interactive UI pumps the same operation.
        internal static bool TryExport(Cubemap cubemap, out byte[] pngData, out string error)
        {
            pngData = null;
            ExportOperation operation;
            if (!TryBeginExport(cubemap, out operation, out error)) return false;
            using (operation)
            {
                while (!operation.IsComplete)
                    if (!operation.Advance(out error)) return false;
                pngData = operation.TakeData();
                return pngData != null;
            }
        }

        internal static bool TryBeginExport(Cubemap cubemap, out ExportOperation operation, out string error)
        {
            operation = null;
            error = null;
            if (cubemap == null) { error = "No Cubemap is currently assigned."; return false; }
            if (cubemap.width > MaximumExportFaceSize)
            {
                error = "Cubemap export supports face sizes up to " + MaximumExportFaceSize
                    + " pixels to keep runtime readback memory bounded.";
                return false;
            }
            long peak;
            if (!MaterialEditorCubemapMemoryBudget.TryValidateExport(cubemap.width, out peak, out error))
                return false;
            MaterialEditorCubemapMemoryReservation reservation;
            if (!MaterialEditorCubemapMemoryBudget.TryReserveConversion(peak, out reservation, out error))
                return false;
            operation = new ExportOperation(cubemap, reservation);
            return true;
        }

        private static Color SampleCubemap(
            Color[][] faces,
            int size,
            MaterialEditorCubemapFace face,
            double u,
            double v)
        {
            var sourceX = u * size - 0.5;
            var sourceY = v * size - 0.5;
            var rawX0 = FloorToInt(sourceX);
            var rawY0 = FloorToInt(sourceY);
            var fractionX = (float)(sourceX - Math.Floor(sourceX));
            var fractionY = (float)(sourceY - Math.Floor(sourceY));
            var bottom = Lerp(
                SampleFaceTexel(faces, size, face, rawX0, rawY0),
                SampleFaceTexel(faces, size, face, rawX0 + 1, rawY0),
                fractionX);
            var top = Lerp(
                SampleFaceTexel(faces, size, face, rawX0, rawY0 + 1),
                SampleFaceTexel(faces, size, face, rawX0 + 1, rawY0 + 1),
                fractionX);
            return Lerp(bottom, top, fractionY);
        }

        private static Color SampleFaceTexel(
            Color[][] faces,
            int size,
            MaterialEditorCubemapFace face,
            int x,
            int y)
        {
            if (x >= 0 && x < size && y >= 0 && y < size)
                return faces[(int)face][y * size + x];

            double directionX;
            double directionY;
            double directionZ;
            MaterialEditorCubemapProjection.DirectionForFaceUv(
                face,
                (x + 0.5) / size,
                (y + 0.5) / size,
                out directionX,
                out directionY,
                out directionZ);
            MaterialEditorCubemapFace adjacentFace;
            double adjacentU;
            double adjacentV;
            MaterialEditorCubemapProjection.FaceUvForDirection(
                directionX,
                directionY,
                directionZ,
                out adjacentFace,
                out adjacentU,
                out adjacentV);
            var adjacentX = Clamp(FloorToInt(adjacentU * size), 0, size - 1);
            var adjacentY = Clamp(FloorToInt(adjacentV * size), 0, size - 1);
            return faces[(int)adjacentFace][adjacentY * size + adjacentX];
        }
    }
}
