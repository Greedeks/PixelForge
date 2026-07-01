using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using PixelForge.Core.Model;
using PixelForge.Core.Services;
using PixelForge.Helpers.Formatters;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace PixelForge.Helpers.SvgHandling
{
    internal static class SvgConverter
    {
        private static readonly WpfDrawingSettings DrawingSettings = new()
        {
            IncludeRuntime = false,
            TextAsGeometry = true,
            OptimizePath = true
        };

        internal static SvgConversionResult ConvertSvgText(string svgCode, string fileName)
        {
            try
            {
                (double vbX, double vbY, double vbWidth, double vbHeight) = SvgParserData.ExtractViewBox(svgCode);

                DrawingGroup drawingGroup = CreateDrawingGroup(svgCode);

                FlattenTransforms(drawingGroup);

                if (vbWidth > 0 && vbHeight > 0)
                {
                    EnsureRootClipGeometry(drawingGroup, vbX, vbY, vbWidth, vbHeight);
                }

                InjectGeometrySize(drawingGroup);
                RemoveObjectNames(drawingGroup);

                string xaml = ConvertDrawingToXaml(drawingGroup, Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_").Replace("-", "_").Replace(".", "_"));

                DrawingImage drawingImage = new(drawingGroup);
                drawingImage.Freeze();

                BitmapSource thumbnail = ImageThumbnailService.RenderThumbnailFromSvgText(svgCode, vbX, vbY, vbWidth, vbHeight);

                return new SvgConversionResult
                {
                    DrawingImage = drawingImage,
                    Thumbnail = thumbnail,
                    XamlCode = xaml,
                    SvgCode = FormatSvgToReadableText(svgCode),
                    ViewBoxX = vbX,
                    ViewBoxY = vbY,
                    ViewBoxWidth = vbWidth,
                    ViewBoxHeight = vbHeight,
                    IsError = false
                };
            }
            catch
            {
                return new SvgConversionResult()
                {
                    IsError = true
                };
            }
        }

        /// <summary>
        /// Parses SVG text via SharpVectors and builds a WPF DrawingGroup from it.
        /// Before parsing, it normalizes the document's id attributes (see <see cref="NormalizeIds"/>).
        /// </summary>
        private static DrawingGroup CreateDrawingGroup(string svgCode)
        {
            using MemoryStream mStream = new(Encoding.UTF8.GetBytes(svgCode));

            XDocument document = XDocument.Load(mStream);
            NormalizeIds(document.Root);

            using MemoryStream normalizedStream = new();
            document.Save(normalizedStream);
            normalizedStream.Position = 0;

            FileSvgReader svgReader = new(DrawingSettings);
            svgReader.Read(normalizedStream);

            return svgReader.Drawing ?? throw new InvalidOperationException("Unsupported or invalid SVG file structure.");
        }

        /// <summary>
        /// Ensures that the root DrawingGroup has a valid clip geometry set.
        /// If a clip already exists, it does nothing. If a clip is found in a nested group,
        /// it promotes it to the top level. Otherwise, it calculates the clip based on content bounds
        /// or the viewBox (if the content is inside the viewBox).
        /// </summary>
        private static void EnsureRootClipGeometry(DrawingGroup drawingGroup, double x, double y, double width, double height)
        {
            if (drawingGroup.ClipGeometry != null)
            {
                return;
            }

            DrawingGroup? groupWithClip = SvgDrawingHelper.FindGroupWithClip(drawingGroup);
            if (groupWithClip != null)
            {
                drawingGroup.ClipGeometry = groupWithClip.ClipGeometry;
                groupWithClip.ClipGeometry = null;
                return;
            }

            Rect viewBoxRect = new(x, y, width, height);

            Rect contentBounds = SvgDrawingHelper.CalculateContentBounds(drawingGroup);

            if (contentBounds.IsEmpty || SvgDrawingHelper.IsContentInsideViewBox(viewBoxRect, contentBounds))
            {
                drawingGroup.ClipGeometry = new RectangleGeometry(viewBoxRect);
            }
            else
            {
                drawingGroup.ClipGeometry = new RectangleGeometry(contentBounds);
                return;
            }
        }

        /// <summary>
        /// Adds hidden marker points to all path geometries, encoding the final image size
        /// (retrieved via <see cref="SvgDrawingHelper.GetSizeFromDrawingGroup"/>).
        /// Used to embed the size directly into the geometry (see <see cref="SizeGeometry"/>).
        /// </summary>
        private static void InjectGeometrySize(DrawingGroup drawingGroup)
        {
            Size? size = SvgDrawingHelper.GetSizeFromDrawingGroup(drawingGroup);
            if (size.HasValue)
            {
                foreach (PathGeometry pathGeometry in SvgDrawingHelper.EnumeratePathGeometries(drawingGroup))
                {
                    SizeGeometry(pathGeometry, size.Value);
                }
            }
        }

        /// <summary>
        /// Adds two additional point figures to the start of the provided PathGeometry
        /// (at (0,0) and (width,height)), which serve as hidden size markers
        /// and do not visually affect the rendering (as they are degenerate figures).
        /// </summary>
        private static void SizeGeometry(PathGeometry pathGeometry, Size size)
        {
            if (size.Width > 0 && size.Height > 0)
            {
                PathFigure[] markerFigures =
                [
                    new(new Point(size.Width, size.Height), [], true),
                    new(new Point(0, 0), [], true)
                ];

                PathGeometry combinedGeometry = new(markerFigures.Concat(pathGeometry.Figures), pathGeometry.FillRule, null);

                pathGeometry.Clear();
                pathGeometry.AddGeometry(combinedGeometry);
            }
        }

        /// <summary>
        /// Entry point for "flattening" transformations: recursively traverses the DrawingGroup,
        /// accumulating parent transformation matrices, and applies them directly
        /// to the geometry of leaf elements (see <see cref="FlattenDrawingGroup"/>).
        /// </summary>
        private static void FlattenTransforms(DrawingGroup drawingGroup) => FlattenDrawingGroup(drawingGroup, Matrix.Identity);

        /// <summary>
        /// Recursively applies the accumulated transformation matrix to the geometry of each
        /// GeometryDrawing and removes the Transform property from the DrawingGroups themselves,
        /// ensuring the resulting hierarchy contains no nested transformations (all coordinates are "baked" into the geometry).
        /// </summary>
        private static void FlattenDrawingGroup(DrawingGroup drawingGroup, Matrix parentMatrix)
        {
            Matrix current = parentMatrix;
            if (drawingGroup.Transform != null && !drawingGroup.Transform.Value.IsIdentity)
            {
                current = Matrix.Multiply(current, drawingGroup.Transform.Value);
                drawingGroup.Transform = null;
            }

            foreach (Drawing? child in drawingGroup.Children)
            {
                if (child is DrawingGroup childGroup)
                {
                    FlattenDrawingGroup(childGroup, current);
                }
                else if (child is GeometryDrawing geomDrawing && !current.IsIdentity && geomDrawing.Geometry != null)
                {
                    Geometry cloned = geomDrawing.Geometry.Clone();
                    cloned.Transform = new MatrixTransform(current);
                    geomDrawing.Geometry = PathGeometry.CreateFromGeometry(cloned);
                }
            }
        }

        /// <summary>
        /// Recursively clears the <see cref="FrameworkElement.NameProperty"/> value
        /// from the provided DrawingGroup and all its child elements, ensuring the final Drawing
        /// does not contain internal names inherited from SVG id attributes.
        /// </summary>
        private static void RemoveObjectNames(DrawingGroup drawingGroup)
        {
            if (drawingGroup.GetValue(FrameworkElement.NameProperty) != null)
            {
                drawingGroup.SetValue(FrameworkElement.NameProperty, null);
            }

            foreach (DependencyObject child in drawingGroup.Children.OfType<DependencyObject>())
            {
                if (child.GetValue(FrameworkElement.NameProperty) != null)
                {
                    child.SetValue(FrameworkElement.NameProperty, null);
                }

                if (child is DrawingGroup childGroup)
                {
                    RemoveObjectNames(childGroup);
                }
            }
        }

        /// <summary>
        /// Generates the final XAML markup for a DrawingImage with a specified x:Key based on the provided DrawingGroup, recursively writing it via <see cref="WriteDrawing"/>.
        /// </summary>
        private static string ConvertDrawingToXaml(DrawingGroup drawingGroup, string keyName)
        {
            StringBuilder sb = new();
            XmlWriterSettings settings = new()
            {
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = true
            };

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("DrawingImage");
                writer.WriteAttributeString("x", "Key", "http://schemas.microsoft.com/winfx/2006/xaml", $"{keyName}");
                writer.WriteStartElement("DrawingImage.Drawing");
                WriteDrawing(writer, drawingGroup);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return sb.ToString().Replace(" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"", "");
        }

        /// <summary>
        /// Recursively writes a Drawing (DrawingGroup or GeometryDrawing) as XAML elements.
        /// "Transparent" DrawingGroups with a single child, no clip, no transform,
        /// and full opacity are collapsed (only their single child is written).
        /// </summary>
        private static void WriteDrawing(XmlWriter writer, Drawing drawing)
        {
            if (drawing is DrawingGroup group)
            {
                if (group.Children.Count == 0 && group.ClipGeometry == null && group.Transform == null)
                {
                    return;
                }

                bool hasNoClip = group.ClipGeometry == null;
                bool hasNoTransform = group.Transform == null || group.Transform.Value.IsIdentity;
                bool hasFullOpacity = group.Opacity >= 1.0;

                if (hasNoClip && hasNoTransform && hasFullOpacity)
                {
                    foreach (Drawing? child in group.Children)
                    {
                        WriteDrawing(writer, child);
                    }
                    return;
                }

                writer.WriteStartElement("DrawingGroup");

                if (group.ClipGeometry != null)
                {
                    writer.WriteAttributeString("ClipGeometry", GetClipGeometryString(group.ClipGeometry));
                }

                if (group.Opacity < 1.0)
                {
                    writer.WriteAttributeString("Opacity", Math.Round(group.Opacity, 4).ToString(CultureInfo.InvariantCulture));
                }

                if (group.Transform != null && !group.Transform.Value.IsIdentity)
                {
                    writer.WriteStartElement("DrawingGroup.Transform");
                    if (group.Transform is ScaleTransform scale)
                    {
                        writer.WriteStartElement("ScaleTransform");
                        writer.WriteAttributeString("ScaleX", scale.ScaleX.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("ScaleY", scale.ScaleY.ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement("MatrixTransform");
                        writer.WriteAttributeString("Matrix", group.Transform.Value.ToString(CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                foreach (Drawing? child in group.Children)
                {
                    WriteDrawing(writer, child);
                }

                writer.WriteEndElement();
            }
            else if (drawing is GeometryDrawing geom)
            {
                writer.WriteStartElement("GeometryDrawing");

                if (geom.Brush is SolidColorBrush sb)
                {
                    writer.WriteAttributeString("Brush", sb.Color.ToString(CultureInfo.InvariantCulture));
                }
                else if (geom.Brush != null)
                {
                    Color fallback = GetApproximateSolidColor(geom.Brush);
                    writer.WriteAttributeString("Brush", fallback.ToString(CultureInfo.InvariantCulture));
                }

                if (geom.Geometry != null)
                {
                    writer.WriteAttributeString("Geometry", GetCompactGeometryString(geom.Geometry));
                }

                if (geom.Pen != null)
                {
                    WritePen(writer, geom.Pen);
                }

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Writes a <see cref="GeometryDrawing.Pen"/> as a property element with a nested Pen element to the XAML markup.
        /// </summary>
        private static void WritePen(XmlWriter writer, Pen pen)
        {
            writer.WriteStartElement("GeometryDrawing.Pen");
            writer.WriteStartElement("Pen");

            if (pen.Brush is SolidColorBrush pb)
            {
                writer.WriteAttributeString("Brush", pb.Color.ToString(CultureInfo.InvariantCulture));
            }
            else if (pen.Brush != null)
            {
                Color fallback = GetApproximateSolidColor(pen.Brush);
                writer.WriteAttributeString("Brush", fallback.ToString(CultureInfo.InvariantCulture));
            }

            if (pen.Thickness != 1.0)
            {
                writer.WriteAttributeString("Thickness", pen.Thickness.ToString(CultureInfo.InvariantCulture));
            }

            if (pen.StartLineCap != PenLineCap.Flat)
            {
                writer.WriteAttributeString("StartLineCap", pen.StartLineCap.ToString());
            }

            if (pen.EndLineCap != PenLineCap.Flat)
            {
                writer.WriteAttributeString("EndLineCap", pen.EndLineCap.ToString());
            }

            if (pen.DashCap != PenLineCap.Flat)
            {
                writer.WriteAttributeString("DashCap", pen.DashCap.ToString());
            }

            if (pen.LineJoin != PenLineJoin.Miter)
            {
                writer.WriteAttributeString("LineJoin", pen.LineJoin.ToString());
            }

            if (pen.MiterLimit != 10.0)
            {
                writer.WriteAttributeString("MiterLimit", pen.MiterLimit.ToString(CultureInfo.InvariantCulture));
            }

            if (pen.DashStyle != null && pen.DashStyle.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                string dashes = string.Join(" ", pen.DashStyle.Dashes.Select(d => d.ToString(CultureInfo.InvariantCulture)));

                if (pen.DashStyle.Offset == 0.0)
                {
                    writer.WriteAttributeString("DashArray", dashes);
                }
                else
                {
                    writer.WriteStartElement("Pen.DashStyle");
                    writer.WriteStartElement("DashStyle");
                    writer.WriteAttributeString("Dashes", dashes);
                    writer.WriteAttributeString("Offset", pen.DashStyle.Offset.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
            else
            {
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static Color GetApproximateSolidColor(Brush brush)
        {
            return brush switch
            {
                LinearGradientBrush lgb when lgb.GradientStops.Count > 0 => lgb.GradientStops[0].Color,
                RadialGradientBrush rgb when rgb.GradientStops.Count > 0 => rgb.GradientStops[0].Color,
                GradientBrush gb when gb.GradientStops.Count > 0 => gb.GradientStops[0].Color,
                _ => Colors.Transparent,
            };
        }

        /// <summary>
        /// Builds a compact clip geometry string as a rectangular path ("M.. V.. H.. V.. H.. Z") based on the bounds of the provided geometry. 
        /// </summary>
        private static string GetClipGeometryString(Geometry geometry)
        {
            if (geometry == null)
            {
                return string.Empty;
            }

            Rect bounds = geometry.Bounds;
            if (bounds.IsEmpty)
            {
                return GetCompactGeometryString(geometry);
            }

            return string.Format(CultureInfo.InvariantCulture, "M{0},{1} V{2} H{3} V{1} H{0} Z", SvgGeometryFormatter.RoundNum(bounds.Left), SvgGeometryFormatter.RoundNum(bounds.Top), SvgGeometryFormatter.RoundNum(bounds.Bottom), SvgGeometryFormatter.RoundNum(bounds.Right));
        }

        /// <summary>
        /// Converts geometry into a compact path string (WPF mini-language format) with rounded
        /// numbers, correctly inserting a space after the fill rule marker (F0/F1) if it is missing.
        /// </summary>
        private static string GetCompactGeometryString(Geometry geometry)
        {
            if (geometry == null)
            {
                return string.Empty;
            }

            string raw = PathGeometry.CreateFromGeometry(geometry).ToString(CultureInfo.InvariantCulture);

            if (raw.Length > 2 && (raw.StartsWith("F0") || raw.StartsWith("F1")) && raw[2] != ' ')
            {
                raw = raw[..2] + " " + raw[2..];
            }

            return SvgGeometryFormatter.RoundNumbersInGeometry(raw);
        }

        /// <summary>
        /// Normalizes SVG document id attributes: prepends an "_" to ids starting with a digit,
        /// and replaces "/" in ids with "_" (as such characters are invalid for x:Name in WPF/XAML).
        /// All references to modified ids (href, fill/stroke/clip-path/mask/filter with url(#id),
        /// and rules inside &lt;style&gt;) are updated accordingly.
        /// </summary>
        private static void NormalizeIds(XElement? root)
        {
            if (root != null)
            {
                Dictionary<string, string> renames = [];

                foreach (XAttribute? attr in root.DescendantsAndSelf().SelectMany(e => e.Attributes()).Where(a => string.Equals(a.Name.LocalName, "id", StringComparison.OrdinalIgnoreCase)))
                {
                    string oldVal = attr.Value;
                    if (string.IsNullOrEmpty(oldVal))
                    {
                        continue;
                    }

                    string newVal = oldVal;
                    if (char.IsDigit(newVal[0]))
                    {
                        newVal = "_" + newVal;
                    }

                    newVal = newVal.Replace("/", "_");

                    if (newVal != oldVal)
                    {
                        renames[oldVal] = newVal;
                        attr.Value = newVal;
                    }
                }

                if (renames.Count == 0)
                {
                    return;
                }

                foreach (XElement elem in root.DescendantsAndSelf())
                {
                    foreach (XAttribute? attr in elem.Attributes().ToList())
                    {
                        bool isHref = string.Equals(attr.Name.LocalName, "href", StringComparison.OrdinalIgnoreCase);
                        bool isStyleRef = attr.Name.LocalName is "fill" or "stroke" or "clip-path" or "mask" or "filter";

                        if (!isHref && !isStyleRef)
                        {
                            continue;
                        }

                        string val = attr.Value;
                        string updated = val;

                        foreach (KeyValuePair<string, string> kv in renames)
                        {
                            if (isHref)
                            {
                                updated = Regex.Replace(updated, $@"^#{Regex.Escape(kv.Key)}$", $"#{kv.Value}");
                            }
                            else
                            {
                                updated = Regex.Replace(updated, $@"url\(\s*#{Regex.Escape(kv.Key)}\s*\)", $"url(#{kv.Value})");
                            }
                        }

                        if (updated != val)
                        {
                            attr.Value = updated;
                        }
                    }
                }

                foreach (XElement? styleElem in root.DescendantsAndSelf().Where(e => string.Equals(e.Name.LocalName, "style", StringComparison.OrdinalIgnoreCase)))
                {
                    string css = styleElem.Value;
                    string updatedCss = css;
                    foreach (KeyValuePair<string, string> kv in renames)
                    {
                        updatedCss = Regex.Replace(updatedCss, $@"url\(\s*#{Regex.Escape(kv.Key)}\s*\)", $"url(#{kv.Value})");
                    }
                    if (updatedCss != css)
                    {
                        styleElem.Value = updatedCss;
                    }
                }
            }
        }

        /// <summary>
        /// Formats the SVG XML text with clean indentation.
        /// </summary>
        private static string FormatSvgToReadableText(string svgCode)
        {
            try
            {
                XDocument document = XDocument.Parse(svgCode, LoadOptions.PreserveWhitespace);

                StringBuilder sb = new();
                XmlWriterSettings writerSettings = new()
                {
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = document.Declaration == null,
                    NewLineOnAttributes = false
                };

                using (XmlWriter writer = XmlWriter.Create(sb, writerSettings))
                {
                    document.Save(writer);
                }

                return sb.ToString();
            }
            catch (XmlException)
            {
                return svgCode;
            }
        }
    }
}