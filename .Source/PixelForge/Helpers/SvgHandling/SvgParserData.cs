using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PixelForge.Helpers.SvgHandling
{
    internal static class SvgParserData
    {
        /// <summary>
        /// Extracts coordinates and dimensions of the viewBox from the root SVG element.
        /// If the viewBox is missing, it attempts to use the width/height attributes.
        /// </summary>
        internal static (double x, double y, double width, double height) ExtractViewBox(string svgCode)
        {
            try
            {
                using MemoryStream mStream = new(Encoding.UTF8.GetBytes(svgCode));
                XDocument doc = XDocument.Load(mStream);
                XElement? root = doc.Root;
                if (root == null)
                {
                    return (0, 0, 0, 0);
                }

                string? vb = root.Attribute("viewBox")?.Value;
                if (!string.IsNullOrEmpty(vb))
                {
                    string[] parts = vb.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&
                        double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double w) && double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
                    {
                        return (x, y, w, h);
                    }
                }

                if (ParseLengthAttribute(root.Attribute("width")?.Value) is double fw && ParseLengthAttribute(root.Attribute("height")?.Value) is double fh)
                {
                    return (0, 0, fw, fh);
                }
            }
            catch { }

            return (0, 0, 0, 0);
        }

        internal static double? ParseLengthAttribute(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string trimmed = raw.Trim();

            Match match = Regex.Match(trimmed, @"^(-?\d+\.?\d*)\s*(px|pt|em|ex|pc|mm|cm|in|%)?$", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return null;
            }

            if (string.Equals(match.Groups[2].Value, "%", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return value;
        }
    }
}
