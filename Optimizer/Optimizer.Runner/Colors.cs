using System.Drawing;

namespace Optimizer.Runner;

public class Colors
{
    public static Dictionary<int, Color> GenerateColorPalette(int n)
    {
        var palette = new Dictionary<int, Color>();
        var rgbValues = new[,]
        {
            { 255, 255, 255 }, // White
            { 255, 0, 0 }, // Red
            { 0, 255, 0 }, // Green
            { 0, 0, 255 }, // Blue
            { 255, 255, 0 }, // Yellow
            { 255, 0, 255 }, // Magenta
            { 0, 255, 255 }, // Cyan
            { 128, 0, 0 }, // Maroon
            { 0, 128, 0 }, // Green
            { 0, 0, 128 }, // Navy
            { 128, 128, 0 }, // Olive
            { 128, 0, 128 }, // Purple
            { 0, 128, 128 }, // Teal
            { 192, 192, 192 }, // Silver
            { 128, 128, 128 }, // Gray
            { 255, 165, 0 }, // Orange
            { 128, 0, 64 }, // Burgundy
            { 255, 192, 203 }, // Pink
            { 0, 255, 128 }, // Spring Green
            { 255, 69, 0 }, // Red-Orange
            { 0, 255, 255 }, // Sky Blue
            { 255, 20, 147 }, // Deep Pink
            { 255, 215, 0 }, // Gold
            { 255, 99, 71 }, // Tomato
            { 0, 255, 0 }, // Lime
            { 0, 0, 205 }, // Medium Blue
            { 255, 140, 0 }, // Dark Orange
            { 127, 255, 212 }, // Aquamarine
            { 218, 112, 214 }, // Orchid
            { 0, 128, 128 }, // Dark Cyan
            { 64, 64, 64 },
            { 32, 32, 32 },
            { 200, 200, 200 }
        };

        for (var i = 0; i < rgbValues.GetLength(0); i++)
            palette[i ] = Color.FromArgb(rgbValues[i, 0], rgbValues[i, 1], rgbValues[i, 2]);

        return palette;
    }
}