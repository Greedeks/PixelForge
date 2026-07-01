using System.Globalization;
using System.Text.RegularExpressions;

namespace PixelForge.Helpers.Formatters
{
    internal class SvgGeometryFormatter
    {
        internal static string RoundNum(double value) => Math.Round(value, 4).ToString("G", CultureInfo.InvariantCulture);

        internal static string RoundNumbersInGeometry(string geometry)
        {
            return Regex.Replace(geometry, @"-?\d+\.?\d*(?:E[+-]?\d+)?", match =>
            {
                if (double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    return Math.Round(value, 4).ToString("G", CultureInfo.InvariantCulture);
                }

                return match.Value;
            });
        }
    }
}
