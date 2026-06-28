using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PixelForge.Helpers.ImageOptimization
{
    /// <summary>
    /// SVG minifier approximating SVGO behavior for basic transforms:
    ///  - Removal of editor metadata (Inkscape/Illustrator/Sodipodi attributes and namespaces)
    ///  - Removal of metadata, title, desc, and comments
    ///  - Removal of empty defs and g containers without attributes or children
    ///  - Inlining simple translate() transforms directly into coordinates
    ///  - Rounding numeric precision in paths, coordinates, and transforms
    ///  - Compression of HEX and RGB colors
    ///  - Collapsing redundant whitespace in the output
    /// </summary>
    internal static class SvgOptimizer
    {
        private const int DefaultPrecision = 2;

        private static readonly HashSet<string> EditorNamespacePrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "inkscape", "sodipodi", "dc", "cc", "rdf", "xmlns:inkscape", "xmlns:sodipodi"
        };

        private static readonly HashSet<string> NumericAttributeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry",
            "width", "height", "viewBox", "points", "stroke-width", "fill-opacity", "opacity", "stroke-opacity"
        };

        private static readonly HashSet<string> WhitespaceNormalizedAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            "d", "points", "transform"
        };

        private static readonly HashSet<string> ColorAttributeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "fill", "stroke", "stop-color", "color"
        };

        private static readonly Regex MetadataBlockText = new(@"<metadata\b[^>]*>[\s\S]*?</metadata>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CommentText = new(@"", RegexOptions.Compiled);

        private static readonly Regex AttributeInnerWhitespace = new(@"\s+", RegexOptions.Compiled);

        private static readonly Regex NumberPattern = new(@"(?<![.\d])-?(?:\d+\.\d+|\.\d+)", RegexOptions.Compiled);

        private static readonly Regex HexLongColor = new(@"^#([0-9a-fA-F])\1([0-9a-fA-F])\2([0-9a-fA-F])\3$", RegexOptions.Compiled);

        private static readonly Regex RgbFunc = new(@"rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.Compiled);

        private static readonly Regex CrossTagWhitespace = new(@">\s+<", RegexOptions.Compiled);

        private static readonly Regex TranslateOnly = new(@"^\s*translate\(\s*(-?[\d.]+)(?:\s*,\s*(-?[\d.]+))?\s*\)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CoordPair = new(@"(-?(?:\d+\.?\d*|\.\d+))(\s*[,\s]\s*)(-?(?:\d+\.?\d*|\.\d+))", RegexOptions.Compiled);

        private static readonly Regex PathNumber = new(@"-?(?:\d+\.?\d*|\.\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Optimizes the provided SVG source string with a given decimal precision.
        /// </summary>
        internal static string Optimize(string sourceXml, int precision = DefaultPrecision)
        {
            XDocument? doc = TryParse(sourceXml);
            if (doc is null)
            {
                string sanitized = StripUnparsableBlocksAsText(sourceXml);
                doc = TryParse(sanitized);
            }

            return doc is null ? sourceXml : OptimizeDocument(doc, precision) ?? sourceXml;
        }

        /// <summary>
        /// Attempts to parse the XML string safely into an XDocument.
        /// </summary>
        private static XDocument? TryParse(string xml)
        {
            try { return XDocument.Parse(xml, LoadOptions.None); }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Removes metadata blocks and comments using regex fallback when XML parsing fails.
        /// </summary>
        private static string StripUnparsableBlocksAsText(string xml)
        {
            string result = CommentText.Replace(xml, string.Empty);
            result = MetadataBlockText.Replace(result, string.Empty);
            return result;
        }

        /// <summary>
        /// Orchestrates the XML-based optimization passes on the XDocument.
        /// </summary>
        private static string? OptimizeDocument(XDocument doc, int precision)
        {
            doc.DescendantNodes().OfType<XComment>().Remove();

            if (doc.Root is not null)
            {
                StripEditorMetadata(doc.Root);
                NormalizeAttributeWhitespace(doc.Root);
                InlineTranslates(doc.Root);
                RoundNumericAttributes(doc.Root, precision);
                CompressColors(doc.Root);
                RemoveEmptyContainers(doc.Root);

                string serialized = doc.Declaration is null ? doc.ToString(SaveOptions.DisableFormatting) : doc.Declaration + doc.ToString(SaveOptions.DisableFormatting);

                return CrossTagWhitespace.Replace(serialized, "><").Trim();
            }

            return null;
        }

        /// <summary>
        /// Removes editor-specific nodes and metadata attributes (Inkscape, Illustrator, Sodipodi).
        /// </summary>
        private static void StripEditorMetadata(XElement root)
        {
            root.Descendants().Where(e => e.Name.LocalName is "metadata" or "title" or "desc").ToList().ForEach(e => e.Remove());

            foreach (XElement? element in root.DescendantsAndSelf().ToList())
            {
                List<XAttribute> attrsToRemove = [.. element.Attributes().Where(a => IsEditorNamespaceAttribute(a))];

                foreach (XAttribute attr in attrsToRemove)
                {
                    attr.Remove();
                }
            }

            root.Descendants().Where(e => EditorNamespacePrefixes.Contains(e.Name.LocalName) || (e.Name.Namespace != XNamespace.None && EditorNamespacePrefixes.Contains(e.GetPrefixOfNamespace(e.Name.Namespace) ?? string.Empty))).ToList().ForEach(e => e.Remove());
        }

        /// <summary>
        /// Determines if an attribute belongs to an editor namespace or declaration.
        /// </summary>
        private static bool IsEditorNamespaceAttribute(XAttribute attr)
        {
            if (attr.IsNamespaceDeclaration)
            {
                return EditorNamespacePrefixes.Contains(attr.Name.LocalName);
            }

            XNamespace ns = attr.Name.Namespace;
            if (ns == XNamespace.None)
            {
                return false;
            }

            string? prefix = attr.Parent?.GetPrefixOfNamespace(ns);
            return prefix is not null && EditorNamespacePrefixes.Contains(prefix);
        }

        /// <summary>
        /// Recursively removes empty g and defs elements that have no visual impact.
        /// </summary>
        private static void RemoveEmptyContainers(XElement root)
        {
            bool removedAny;
            do
            {
                removedAny = false;
                foreach (XElement? element in root.Descendants().Where(e => e.Name.LocalName is "defs" or "g").ToList())
                {
                    bool hasMeaningfulAttrs = element.Attributes().Any(a => !a.IsNamespaceDeclaration && a.Name.LocalName != "id");

                    if (!element.HasElements && !hasMeaningfulAttrs && string.IsNullOrWhiteSpace(element.Value))
                    {
                        element.Remove();
                        removedAny = true;
                    }
                }
            }
            while (removedAny);
        }

        /// <summary>
        /// Collapses redundant whitespace inside path, points, and transform attributes.
        /// </summary>
        private static void NormalizeAttributeWhitespace(XElement root)
        {
            foreach (XElement element in root.DescendantsAndSelf())
            {
                foreach (XAttribute attr in element.Attributes())
                {
                    if (WhitespaceNormalizedAttributes.Contains(attr.Name.LocalName))
                    {
                        attr.Value = AttributeInnerWhitespace.Replace(attr.Value, " ").Trim();
                    }
                }
            }
        }

        /// <summary>
        /// Inlines simple translate() transforms directly into path coordinates or geometric attributes.
        /// </summary>
        private static void InlineTranslates(XElement root)
        {
            foreach (XElement element in root.DescendantsAndSelf().ToList())
            {
                XAttribute? transformAttr = element.Attribute("transform");
                if (transformAttr is null)
                {
                    continue;
                }

                Match m = TranslateOnly.Match(transformAttr.Value);
                if (!m.Success)
                {
                    continue;
                }

                double tx = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                double ty = m.Groups[2].Success ? double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0.0;

                if (tx == 0.0 && ty == 0.0)
                {
                    transformAttr.Remove();
                    continue;
                }

                XAttribute? dAttr = element.Attribute("d");
                if (dAttr is not null)
                {
                    dAttr.Value = ApplyTranslateToPath(dAttr.Value, tx, ty);
                    transformAttr.Remove();
                }
                else if (ApplyTranslateToNumericAttrs(element, tx, ty))
                {
                    transformAttr.Remove();
                }
            }
        }

        /// <summary>
        /// Parses path data segments and recalculates absolute coordinate positions based on translation deltas.
        /// </summary>
        private static string ApplyTranslateToPath(string d, double tx, double ty)
        {
            StringBuilder result = new();
            int i = 0;

            while (i < d.Length)
            {
                char ch = d[i];
                if (char.IsLetter(ch))
                {
                    result.Append(ch);
                    bool isAbsolute = char.IsUpper(ch);
                    char upper = char.ToUpperInvariant(ch);
                    i++;

                    int start = i;
                    while (i < d.Length && !char.IsLetter(d[i]))
                    {
                        i++;
                    }

                    string nums = d[start..i];

                    if (!isAbsolute || upper == 'Z')
                    {
                        result.Append(nums);
                    }
                    else if (upper == 'H')
                    {
                        result.Append(ShiftSingleCoords(nums, tx));
                    }
                    else if (upper == 'V')
                    {
                        result.Append(ShiftSingleCoords(nums, ty));
                    }
                    else if (upper == 'A')
                    {
                        result.Append(ShiftArcCoords(nums, tx, ty));
                    }
                    else
                    {
                        result.Append(ShiftCoordPairs(nums, tx, ty));
                    }
                }
                else
                {
                    result.Append(ch);
                    i++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Modifies single standalone axis coordinates (used for absolute H and V path commands).
        /// </summary>
        private static string ShiftSingleCoords(string nums, double delta)
        {
            return Regex.Replace(nums, @"-?(?:\d+\.?\d*|\.\d+)", m =>
            {
                double v = double.Parse(m.Value, CultureInfo.InvariantCulture) + delta;
                return FormatNum(v);
            });
        }

        /// <summary>
        /// Modifies absolute elliptical arc path coordinates (the 6th and 7th parameters in the arc parameter group) by adding translation deltas.
        /// </summary>
        private static string ShiftArcCoords(string nums, double tx, double ty)
        {
            MatchCollection matches = PathNumber.Matches(nums);

            int completeGroups = (matches.Count / 7) * 7;
            if (completeGroups == 0)
            {
                return nums;
            }

            StringBuilder sb = new(nums);
            int offset = 0;

            for (int i = 0; i < completeGroups; i++)
            {
                int posInGroup = i % 7;
                if (posInGroup != 5 && posInGroup != 6)
                {
                    continue;
                }

                double delta = posInGroup == 5 ? tx : ty;
                double val = double.Parse(matches[i].Value, CultureInfo.InvariantCulture) + delta;
                string newVal = FormatNum(val);

                int idx = matches[i].Index + offset;
                sb.Remove(idx, matches[i].Length);
                sb.Insert(idx, newVal);
                offset += newVal.Length - matches[i].Length;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Modifies grouped X and Y coordinate pairs within a segment by adding the translation factors.
        /// </summary>
        private static string ShiftCoordPairs(string nums, double tx, double ty)
        {
            int pairCount = 0;

            string replaced = CoordPair.Replace(nums, match =>
            {
                double x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) + tx;
                double y = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) + ty;
                pairCount++;
                return FormatNum(x) + match.Groups[2].Value + FormatNum(y);
            });

            return pairCount > 0 ? replaced : nums;
        }

        /// <summary>
        /// Updates standard numerical layout attributes (x, y, cx, cy) on elements with translation deltas.
        /// </summary>
        private static bool ApplyTranslateToNumericAttrs(XElement el, double tx, double ty)
        {
            bool any = false;
            foreach (string attr in new[] { "x", "cx" })
            {
                XAttribute? a = el.Attribute(attr);
                if (a is null || !double.TryParse(a.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    continue;
                }

                a.Value = FormatNum(v + tx);
                any = true;
            }
            foreach (string attr in new[] { "y", "cy" })
            {
                XAttribute? a = el.Attribute(attr);
                if (a is null || !double.TryParse(a.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                {
                    continue;
                }

                a.Value = FormatNum(v + ty);
                any = true;
            }
            return any;
        }

        /// <summary>
        /// Iterates through numerical attributes to apply rounding rules.
        /// </summary>
        private static void RoundNumericAttributes(XElement root, int precision)
        {
            foreach (XElement element in root.DescendantsAndSelf())
            {
                foreach (XAttribute? attr in element.Attributes().ToList())
                {
                    if (attr.Name.LocalName.Equals("d", StringComparison.OrdinalIgnoreCase))
                    {
                        attr.Value = RoundPathData(attr.Value, precision);
                    }
                    else if (NumericAttributeNames.Contains(attr.Name.LocalName))
                    {
                        attr.Value = RoundNumberList(attr.Value, precision);
                    }
                    else if (attr.Name.LocalName.Equals("transform", StringComparison.OrdinalIgnoreCase))
                    {
                        attr.Value = RoundTransform(attr.Value, precision);
                    }
                }
            }
        }

        /// <summary>
        /// Rounds lists of numbers found in standard attributes like x, y, viewBox, etc.
        /// </summary>
        private static string RoundNumberList(string value, int precision) => NumberPattern.Replace(value, m => RoundToken(m, value, precision));

        /// <summary>
        /// Rounds numeric coordinates specifically inside the "d" attribute of paths.
        /// </summary>
        private static string RoundPathData(string d, int precision) => NumberPattern.Replace(d, m => RoundToken(m, d, precision));

        /// <summary>
        /// Rounds numbers inside transform attributes (scale, translate, matrix, etc.).
        /// </summary>
        private static string RoundTransform(string transform, int precision) => NumberPattern.Replace(transform, m => RoundToken(m, transform, precision));

        /// <summary>
        /// Processes, rounds, and minifies an individual numeric token while ensuring SVG format validity.
        /// </summary>
        private static string RoundToken(Match match, string source, int precision)
        {
            string token = match.Value;

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
            {
                double rounded = Math.Round(num, precision, MidpointRounding.AwayFromZero);

                if (rounded == 0.0)
                {
                    return token;
                }

                string formatted = rounded.ToString("0." + new string('#', Math.Max(precision, 0)), CultureInfo.InvariantCulture);

                int nextCharPos = match.Index + match.Length;
                char nextChar = nextCharPos < source.Length ? source[nextCharPos] : '\0';
                if (!formatted.Contains('.') && nextChar == '.')
                {
                    formatted += ' ';
                }

                if (token.StartsWith('.'))
                {
                    if (formatted.StartsWith("0.", StringComparison.Ordinal))
                    {
                        formatted = formatted[1..];
                    }
                }
                else if (token.StartsWith("-."))
                {
                    if (formatted.StartsWith("-0.", StringComparison.Ordinal))
                    {
                        formatted = "-" + formatted[2..];
                    }
                }

                return formatted;
            }

            return token;
        }

        /// <summary>
        /// Formats a double value to a unified invariant string format with up to two decimal places.
        /// </summary>
        private static string FormatNum(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>
        /// Compresses color values in presentation attributes and style blocks.
        /// </summary>
        private static void CompressColors(XElement root)
        {
            foreach (XElement element in root.DescendantsAndSelf())
            {
                foreach (XAttribute? attr in element.Attributes().ToList())
                {
                    if (ColorAttributeNames.Contains(attr.Name.LocalName))
                    {
                        attr.Value = CompressColorValue(attr.Value);
                    }
                    else if (attr.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
                    {
                        attr.Value = CompressColorsInStyle(attr.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Parses style attribute strings to compress inline color declarations.
        /// </summary>
        private static string CompressColorsInStyle(string style)
        {
            string[] parts = style.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string[] kv = parts[i].Split(':', 2);
                if (kv.Length != 2)
                {
                    continue;
                }

                string prop = kv[0].Trim();
                if (ColorAttributeNames.Contains(prop))
                {
                    parts[i] = $"{kv[0]}:{CompressColorValue(kv[1].Trim())}";
                }
            }
            return string.Join(";", parts);
        }

        /// <summary>
        /// Converts rgb() or 6-digit hex colors to shorter formats where possible.
        /// </summary>
        private static string CompressColorValue(string value)
        {
            string trimmed = value.Trim();

            Match rgbMatch = RgbFunc.Match(trimmed);
            if (rgbMatch.Success)
            {
                int r = int.Parse(rgbMatch.Groups[1].Value);
                int g = int.Parse(rgbMatch.Groups[2].Value);
                int b = int.Parse(rgbMatch.Groups[3].Value);
                trimmed = $"#{r:X2}{g:X2}{b:X2}".ToLowerInvariant();
            }

            Match hexMatch = HexLongColor.Match(trimmed);
            if (hexMatch.Success)
            {
                trimmed = $"#{hexMatch.Groups[1].Value}{hexMatch.Groups[2].Value}{hexMatch.Groups[3].Value}".ToLowerInvariant();
            }

            return trimmed;
        }
    }
}
