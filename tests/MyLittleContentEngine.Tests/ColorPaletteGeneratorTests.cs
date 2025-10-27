using MyLittleContentEngine.MonorailCss;
using Shouldly;

namespace MyLittleContentEngine.Tests;

public class ColorPaletteGeneratorTests
{
    // Tailwind v4 actual red values (starting at hue ~17°)
    private static readonly Dictionary<string, OklchColor> TailwindRed = new()
    {
        ["50"] = new(0.971, 0.013, 17.38),
        ["100"] = new(0.936, 0.032, 17.717),
        ["200"] = new(0.885, 0.062, 18.334),
        ["300"] = new(0.808, 0.114, 19.571),
        ["400"] = new(0.704, 0.191, 22.216),
        ["500"] = new(0.637, 0.237, 25.331),
        ["600"] = new(0.577, 0.245, 27.325),
        ["700"] = new(0.505, 0.213, 27.518),
        ["800"] = new(0.444, 0.177, 26.899),
        ["900"] = new(0.396, 0.141, 25.723),
        ["950"] = new(0.258, 0.092, 26.042)
    };

    // Tailwind v4 actual blue values (starting at hue ~255°)
    private static readonly Dictionary<string, OklchColor> TailwindBlue = new()
    {
        ["50"] = new(0.970, 0.014, 254.604),
        ["500"] = new(0.623, 0.214, 259.815),
        ["950"] = new(0.282, 0.091, 267.935)
    };

    // Tailwind v4 actual yellow values (starting at hue ~102°)
    private static readonly Dictionary<string, OklchColor> TailwindYellow = new()
    {
        ["50"] = new(0.987, 0.026, 102.212),
        ["500"] = new(0.795, 0.184, 86.047),
        ["950"] = new(0.286, 0.066, 53.813)
    };

    [Fact]
    public void GenerateFromHue_WithRedHue17_ShouldMatchTailwindRed()
    {
        // Arrange: Use Tailwind's red starting hue
        const double redHue = 17.0;

        // Act
        var palette = ColorPaletteGenerator.GenerateFromHue(redHue);

        // Assert: Test key shades
        AssertOklchMatch(palette["50"], TailwindRed["50"], "red-50");
        AssertOklchMatch(palette["500"], TailwindRed["500"], "red-500");
        AssertOklchMatch(palette["700"], TailwindRed["700"], "red-700");
        AssertOklchMatch(palette["950"], TailwindRed["950"], "red-950");
    }

    [Fact]
    public void GenerateFromHue_WithBlueHue255_ShouldMatchTailwindBlue()
    {
        // Arrange: Use Tailwind's blue starting hue
        const double blueHue = 255.0;

        // Act
        var palette = ColorPaletteGenerator.GenerateFromHue(blueHue);

        // Assert: Test key shades
        AssertOklchMatch(palette["50"], TailwindBlue["50"], "blue-50");
        AssertOklchMatch(palette["500"], TailwindBlue["500"], "blue-500");
        AssertOklchMatch(palette["950"], TailwindBlue["950"], "blue-950");
    }

    [Fact]
    public void GenerateFromHue_WithYellowHue100_ShouldMatchTailwindYellow()
    {
        // Arrange: Use Tailwind's yellow starting hue
        const double yellowHue = 100.0;

        // Act
        var palette = ColorPaletteGenerator.GenerateFromHue(yellowHue);

        // Assert: Test key shades
        AssertOklchMatch(palette["50"], TailwindYellow["50"], "yellow-50");
        AssertOklchMatch(palette["500"], TailwindYellow["500"], "yellow-500");
        AssertOklchMatch(palette["950"], TailwindYellow["950"], "yellow-950");
    }

    [Fact]
    public void GenerateFromHue_ShouldHaveGaussianChromaDistribution()
    {
        // Arrange
        const double hue = 200.0;

        // Act
        var palette = ColorPaletteGenerator.GenerateFromHue(hue);

        // Assert: Chroma should peak around 500-600
        var chroma50 = ParseOklch(palette["50"]).Chroma;
        var chroma500 = ParseOklch(palette["500"]).Chroma;
        var chroma600 = ParseOklch(palette["600"]).Chroma;
        var chroma950 = ParseOklch(palette["950"]).Chroma;

        // Peak should be higher than extremes
        chroma500.ShouldBeGreaterThan(chroma50);
        chroma600.ShouldBeGreaterThan(chroma50);
        chroma500.ShouldBeGreaterThan(chroma950);
        chroma600.ShouldBeGreaterThan(chroma950);

        // 500 or 600 should have highest chroma
        var maxChroma = Math.Max(chroma500, chroma600);
        maxChroma.ShouldBeGreaterThanOrEqualTo(0.15); // Should be reasonably vibrant
    }

    [Fact]
    public void GenerateFromHue_WithRedHue_ShouldShiftTowardOrange()
    {
        // Arrange: Pure red at 0°
        const double pureRedHue = 0.0;

        // Act
        var palette = ColorPaletteGenerator.GenerateFromHue(pureRedHue);

        // Assert: Darker shades should shift toward orange (positive hue shift)
        var hue50 = ParseOklch(palette["50"]).Hue;
        var hue950 = ParseOklch(palette["950"]).Hue;

        hue950.ShouldBeGreaterThan(hue50, "Dark red should shift toward orange");
        hue950.ShouldBeGreaterThanOrEqualTo(5.0, "Should shift at least +5° by shade 950");
    }

    [Fact]
    public void GenerateFromHue_ShouldReturnAllElevenShades()
    {
        // Act
        var palette = ColorPaletteGenerator.GenerateFromHue(180.0);

        // Assert
        palette.Count.ShouldBe(11);
        palette.Keys.ShouldContain("50");
        palette.Keys.ShouldContain("500");
        palette.Keys.ShouldContain("950");
    }

    // Helper methods
    private static void AssertOklchMatch(string actual, OklchColor expected, string shadeName)
    {
        var parsed = ParseOklch(actual);

        parsed.Lightness.ShouldBe(expected.Lightness, 0.02, $"{shadeName} lightness mismatch");
        parsed.Chroma.ShouldBe(expected.Chroma, 0.03, $"{shadeName} chroma mismatch");
        parsed.Hue.ShouldBe(expected.Hue, 5.0, $"{shadeName} hue mismatch");
    }

    private static OklchColor ParseOklch(string oklchString)
    {
        // Parse "oklch(0.971 0.013 17.38)" format
        var values = oklchString
            .Replace("oklch(", "")
            .Replace(")", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(double.Parse)
            .ToArray();

        return new OklchColor(values[0], values[1], values[2]);
    }

    private record OklchColor(double Lightness, double Chroma, double Hue);
}
