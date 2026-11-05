using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ColorResolverTests
{
    [Test]
    public void Resolve_RgbAndTint_NormalizesArgbAndAppliesSpreadsheetTint()
    {
        XlsxSpreadsheetColor blackTint = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor whiteShade = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor argb = new XlsxSpreadsheetColor();

        blackTint.Rgb = "000000";
        blackTint.HasRgb = true;
        blackTint.Tint = 0.5d;
        blackTint.HasTint = true;
        whiteShade.Rgb = "FFFFFFFF";
        whiteShade.HasRgb = true;
        whiteShade.Tint = -0.5d;
        whiteShade.HasTint = true;
        argb.Rgb = "80336699";
        argb.HasRgb = true;

        XlsxSpreadsheetColorResolver resolver = new XlsxSpreadsheetColorResolver();
        XlsxSpreadsheetColorResolutionResult? blackTintResult = resolver.Resolve(blackTint, null!);
        XlsxSpreadsheetColorResolutionResult? whiteShadeResult = resolver.Resolve(whiteShade, null!);
        XlsxSpreadsheetColorResolutionResult? argbResult = resolver.Resolve(argb, null!);

        Assert.That(blackTintResult.IsValid, Is.True);
        Assert.That(blackTintResult.Argb, Is.EqualTo("FF808080"));
        Assert.That(blackTintResult.Rgb, Is.EqualTo("808080"));
        Assert.That(whiteShadeResult.IsValid, Is.True);
        Assert.That(whiteShadeResult.Argb, Is.EqualTo("FF808080"));
        Assert.That(argbResult.IsValid, Is.True);
        Assert.That(argbResult.Argb, Is.EqualTo("80336699"));
        Assert.That(argbResult.Alpha, Is.EqualTo(128));
    }

    [Test]
    public void Resolve_IndexedAndAutomatic_UsesDefaultPaletteAndAutomaticFallback()
    {
        XlsxSpreadsheetColor indexed = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor automatic = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor indexedAuto = new XlsxSpreadsheetColor();

        indexed.Indexed = 10;
        indexed.HasIndexed = true;
        automatic.IsAuto = true;
        indexedAuto.Indexed = 64;
        indexedAuto.HasIndexed = true;

        XlsxSpreadsheetColorResolver resolver = new XlsxSpreadsheetColorResolver();
        XlsxSpreadsheetColorResolutionResult? indexedResult = resolver.Resolve(indexed, null!);
        XlsxSpreadsheetColorResolutionResult? automaticResult = resolver.Resolve(automatic, null!);
        XlsxSpreadsheetColorResolutionResult? indexedAutoResult = resolver.Resolve(indexedAuto, null!);

        Assert.That(indexedResult.IsValid, Is.True);
        Assert.That(indexedResult.Argb, Is.EqualTo("FFFF0000"));
        Assert.That(indexedResult.Source, Is.EqualTo("indexed"));
        Assert.That(automaticResult.IsValid, Is.True);
        Assert.That(automaticResult.Argb, Is.EqualTo("FF000000"));
        Assert.That(automaticResult.Source, Is.EqualTo("automatic"));
        Assert.That(indexedAutoResult.IsValid, Is.True);
        Assert.That(indexedAutoResult.Argb, Is.EqualTo("FF000000"));
        Assert.That(indexedAutoResult.Source, Is.EqualTo("indexed"));
    }

    [Test]
    public void Resolve_ThemeAndSystemColors_UsesThemeSlotsAndDrawingTransforms()
    {
        XlsxThemePart theme = XlsxThemePartWriter.CreateDefaultOfficeTheme();
        XlsxSpreadsheetColor themeAccent = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor themeSystem = new XlsxSpreadsheetColor();
        XlsxThemeColor transformed = new XlsxThemeColor();

        themeAccent.Theme = 4;
        themeAccent.HasTheme = true;
        themeSystem.Theme = 0;
        themeSystem.HasTheme = true;
        transformed.Kind = "srgbClr";
        transformed.Value = "000000";
        transformed.Tint = 50000;
        transformed.HasTint = true;
        transformed.LuminanceOffset = 10000;
        transformed.HasLuminanceOffset = true;

        XlsxSpreadsheetColorResolver resolver = new XlsxSpreadsheetColorResolver();
        XlsxSpreadsheetColorResolutionResult accentResult = resolver.Resolve(themeAccent, theme);
        XlsxSpreadsheetColorResolutionResult systemResult = resolver.Resolve(themeSystem, theme);
        XlsxSpreadsheetColorResolutionResult transformedResult = XlsxSpreadsheetColorResolver.ResolveThemeColor(transformed);
        XlsxSpreadsheetColorResolutionResult directSystemResult = XlsxSpreadsheetColorResolver.ResolveSystemColor("windowText", "");

        Assert.That(accentResult.IsValid, Is.True);
        Assert.That(accentResult.Argb, Is.EqualTo("FF4472C4"));
        Assert.That(accentResult.Source, Is.EqualTo("theme"));
        Assert.That(systemResult.IsValid, Is.True);
        Assert.That(systemResult.Argb, Is.EqualTo("FFFFFFFF"));
        Assert.That(transformedResult.IsValid, Is.True);
        Assert.That(transformedResult.Argb, Is.EqualTo("FF9A9A9A"));
        Assert.That(directSystemResult.IsValid, Is.True);
        Assert.That(directSystemResult.Argb, Is.EqualTo("FF000000"));
    }

    [Test]
    public void Resolve_ReportsInvalidColorInputs()
    {
        XlsxSpreadsheetColor noValue = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor badTint = new XlsxSpreadsheetColor();
        XlsxSpreadsheetColor missingTheme = new XlsxSpreadsheetColor();

        badTint.Rgb = "000000";
        badTint.HasRgb = true;
        badTint.Tint = 2d;
        badTint.HasTint = true;
        missingTheme.Theme = 99;
        missingTheme.HasTheme = true;

        XlsxSpreadsheetColorResolver resolver = new XlsxSpreadsheetColorResolver();
        XlsxSpreadsheetColorResolutionResult? nullResult = resolver.Resolve(null!, null!);
        XlsxSpreadsheetColorResolutionResult? noValueResult = resolver.Resolve(noValue, null!);
        XlsxSpreadsheetColorResolutionResult badRgbResult = XlsxSpreadsheetColorResolver.ResolveRgb("bad");
        XlsxSpreadsheetColorResolutionResult badIndexedResult = XlsxSpreadsheetColorResolver.ResolveIndexed(99);
        XlsxSpreadsheetColorResolutionResult? badTintResult = resolver.Resolve(badTint, null!);
        XlsxSpreadsheetColorResolutionResult missingThemeResult = resolver.Resolve(missingTheme, XlsxThemePartWriter.CreateDefaultOfficeTheme());

        Assert.That(nullResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullResult, "color.null"), Is.True);
        Assert.That(noValueResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(noValueResult, "color.unresolved"), Is.True);
        Assert.That(badRgbResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badRgbResult, "color.rgbInvalid"), Is.True);
        Assert.That(badIndexedResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badIndexedResult, "color.indexedRange"), Is.True);
        Assert.That(badTintResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badTintResult, "color.tintRange"), Is.True);
        Assert.That(missingThemeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingThemeResult, "color.themeRange"), Is.True);
    }

    static bool HasDiagnostic(XlsxSpreadsheetColorResolutionResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
