using System.Collections.Immutable;
using MonorailCss.Css;

namespace MyLittleContentEngine.MonorailCss;

public static class ColorPaletteGenerator
{
    // Chroma and lightness values from https://evilmartians.com/chronicles/better-dynamic-themes-in-tailwind-with-oklch-color-magic
    private static readonly double[] ChromaLevels =
    [
        0.0108, 0.0321, 0.0609, 0.0908, 0.1398, 0.1472, 0.1299, 0.1067, 0.0898, 0.0726, 0.054
    ];
    
    private static readonly double[] LightnessLevels =
    [
        97.78, 93.56, 88.11, 82.67, 74.22, 64.78, 57.33, 46.89, 39.44, 32.00, 23.78
    ];
    
    // Keys for the palette
    private static readonly string[] PaletteKeys =
    [
        "50", "100", "200", "300", "400", "500", "600", "700", "800", "900", "950"
    ];
    
    /// <summary>
    /// Generates a primary and accent color palette based on a hue value in degrees (0-360)
    /// </summary>
    public static ImmutableDictionary<string, string> GenerateFromHue(double hue)
    {
        // Normalize the hue to 0-360 range
        hue = (hue % 360 + 360) % 360;
        
        // Generate a primary palette (with the exact hue)
        return GeneratePaletteFromHue(hue);
    }
    
    /// <summary>
    /// Generates a palette from a specific hue value
    /// </summary>
    private static ImmutableDictionary<string, string> GeneratePaletteFromHue(double hue)
    {
        var palette = new Dictionary<string, string>();
        
        // Generate colors for each step in the palette
        for (var i = 0; i < PaletteKeys.Length; i++)
        {
            var lightness = LightnessLevels[i] / 100.0; // Convert to 0-1 range for OKLCH
            var chroma = ChromaLevels[i];
            
            // Create the OKLCH color
            var oklchColor = $"oklch({lightness:F3} {chroma:F3} {hue:F3})";
            palette.Add(PaletteKeys[i],oklchColor );
        }
        
        return palette.ToImmutableDictionary();
    }
}