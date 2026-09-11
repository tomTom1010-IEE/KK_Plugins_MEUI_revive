using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class VirtualListCachePolicy
    {
        internal static int MaximumViewCount
        {
            get
            {
                var maximumMainHeight =
                    MaterialEditorTheme.Metrics.CanvasReferenceHeight
                    * (1f - 2f
                        * MaterialEditorTheme.Metrics.ResponsiveOuterMarginFraction);
                var maximumViewportHeight = Math.Max(
                    0f,
                    maximumMainHeight
                    - MaterialEditorTheme.Metrics.TopBarHeight
                    - MaterialEditorTheme.Metrics.Margin * 1.5f);
                return UnboundedViewCount(
                    maximumViewportHeight,
                    MaterialEditorTheme.Metrics.RowHeight);
            }
        }

        internal static int RequiredViewCount(
            float viewportHeight,
            float rowHeight,
            int modelCount)
        {
            if (modelCount <= 0
                || viewportHeight <= 0f
                || rowHeight <= 0f
                || float.IsNaN(viewportHeight)
                || float.IsInfinity(viewportHeight)
                || float.IsNaN(rowHeight)
                || float.IsInfinity(rowHeight))
                return 0;

            return Math.Min(
                modelCount,
                Math.Min(
                    MaximumViewCount,
                    UnboundedViewCount(viewportHeight, rowHeight)));
        }

        private static int UnboundedViewCount(
            float viewportHeight,
            float rowHeight)
        {
            return (int)Math.Ceiling(viewportHeight / rowHeight) + 1;
        }
    }
}
