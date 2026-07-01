using System.Windows;
using System.Windows.Media;

namespace PixelForge.Helpers.SvgHandling
{
    internal static class SvgDrawingHelper
    {
        /// <summary>
        /// Calculates the total bounding box of all geometries inside the provided Drawing by recursively traversing all DrawingGroups.
        /// </summary>
        internal static Rect CalculateContentBounds(Drawing drawing)
        {
            Rect bounds = Rect.Empty;

            void VisitDrawing(Drawing currentDrawing)
            {
                if (currentDrawing is DrawingGroup drawingGroup)
                {
                    foreach (Drawing child in drawingGroup.Children)
                    {
                        VisitDrawing(child);
                    }
                }
                else if (currentDrawing is GeometryDrawing geometryDrawing && geometryDrawing.Geometry != null)
                {
                    Rect geometryBounds = geometryDrawing.Geometry.Bounds;

                    if (!geometryBounds.IsEmpty)
                    {
                        bounds = bounds.IsEmpty ? geometryBounds : Rect.Union(bounds, geometryBounds);
                    }
                }
            }

            VisitDrawing(drawing);

            return bounds;
        }

        /// <summary>
        /// Determines whether the content is predominantly located within the viewBox bounds.
        /// It is considered "inside" if the intersection area is at least 10% of the content area, or if one of the rectangles is empty.
        /// </summary>
        internal static bool IsContentInsideViewBox(Rect viewBox, Rect content)
        {
            if (viewBox.IsEmpty || content.IsEmpty)
            {
                return true;
            }

            Rect intersection = Rect.Intersect(viewBox, content);
            if (intersection.IsEmpty)
            {
                return false;
            }

            double contentArea = content.Width * content.Height;
            double intersectionArea = intersection.Width * intersection.Height;
            return contentArea <= 0 || (intersectionArea / contentArea) >= 0.1;
        }

        /// <summary>
        /// Recursively searches for the first nested DrawingGroup that has a ClipGeometry defined.
        /// </summary>
        internal static DrawingGroup? FindGroupWithClip(DrawingGroup drawingGroup)
        {
            foreach (DrawingGroup child in drawingGroup.Children.OfType<DrawingGroup>())
            {
                if (child.ClipGeometry != null)
                {
                    return child;
                }

                DrawingGroup? found = FindGroupWithClip(child);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the size (Size) from the ClipGeometry of the provided DrawingGroup.
        /// If the group itself has no clip, it looks for it in the first child group with a defined ClipGeometry.
        /// </summary>
        internal static Size? GetSizeFromDrawingGroup(DrawingGroup drawingGroup)
        {
            if (drawingGroup != null)
            {
                if (drawingGroup.ClipGeometry != null)
                {
                    return drawingGroup.ClipGeometry.Bounds.Size;
                }

                DrawingGroup? subGroup = drawingGroup.Children.OfType<DrawingGroup>().FirstOrDefault(c => c.ClipGeometry != null);
                return subGroup?.ClipGeometry.Bounds.Size;
            }

            return null;
        }

        /// <summary>
        /// Recursively collects all PathGeometry objects contained within the provided Drawing (including nested DrawingGroups).
        /// </summary>
        internal static IEnumerable<PathGeometry> EnumeratePathGeometries(Drawing drawing)
        {
            List<PathGeometry> geometries = [];

            static void VisitDrawing(Drawing drawing, List<PathGeometry> geometries)
            {
                if (drawing is DrawingGroup drawingGroup)
                {
                    foreach (Drawing child in drawingGroup.Children)
                    {
                        VisitDrawing(child, geometries);
                    }
                }

                if (drawing is GeometryDrawing geometryDrawing &&
                    geometryDrawing.Geometry is PathGeometry pathGeometry)
                {
                    geometries.Add(pathGeometry);
                }
            }

            VisitDrawing(drawing, geometries);

            return geometries;
        }
    }
}