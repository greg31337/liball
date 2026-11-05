using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlResultTests
{
    [Test]
    public void SourceSpan_StoresBoundsAndReportsLengthContainmentAndCopies()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(3, 2, 4, 8, 2, 9);

        Assert.That(span.StartIndex, Is.EqualTo(3));
        Assert.That(span.StartLine, Is.EqualTo(2));
        Assert.That(span.StartColumn, Is.EqualTo(4));
        Assert.That(span.EndIndex, Is.EqualTo(8));
        Assert.That(span.EndLine, Is.EqualTo(2));
        Assert.That(span.EndColumn, Is.EqualTo(9));
        Assert.That(span.Length(), Is.EqualTo(5));
        Assert.That(span.ContainsIndex(3), Is.True);
        Assert.That(span.ContainsIndex(7), Is.True);
        Assert.That(span.ContainsIndex(8), Is.False);
        Assert.That(span.Format(), Is.EqualTo("2:4-2:9"));

        global::yaml.YamlSourceSpan copy = span.Copy();
        Assert.That(copy, Is.Not.SameAs(span));
        Assert.That(copy.StartIndex, Is.EqualTo(span.StartIndex));
        Assert.That(copy.EndColumn, Is.EqualTo(span.EndColumn));

        copy.CopyFrom(null);
        Assert.That(copy.IsEmpty(), Is.True);
    }

    [Test]
    public void SourceSpan_Cover_ReturnsBoundsCoveringBothSpans()
    {
        global::yaml.YamlSourceSpan first = global::yaml.YamlSourceSpan.FromBounds(5, 2, 3, 9, 2, 7);
        global::yaml.YamlSourceSpan second = global::yaml.YamlSourceSpan.FromBounds(1, 1, 1, 12, 3, 2);

        global::yaml.YamlSourceSpan cover = global::yaml.YamlSourceSpan.Cover(first, second);

        Assert.That(cover.StartIndex, Is.EqualTo(1));
        Assert.That(cover.StartLine, Is.EqualTo(1));
        Assert.That(cover.StartColumn, Is.EqualTo(1));
        Assert.That(cover.EndIndex, Is.EqualTo(12));
        Assert.That(cover.EndLine, Is.EqualTo(3));
        Assert.That(cover.EndColumn, Is.EqualTo(2));
    }

    [Test]
    public void SourceSpan_Length_SaturatesWhenBoundsExceedIntegerRange()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(int.MinValue, 1, 1, int.MaxValue, 1, 1);

        Assert.That(span.Length(), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void DiagnosticFactories_SetSeverityCodeMessageAndSpan()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(0, 1, 1, 5, 1, 6);

        global::yaml.YamlDiagnostic error = global::yaml.YamlDiagnostic.Error("YAML001", "bad token", span);
        global::yaml.YamlDiagnostic warning = global::yaml.YamlDiagnostic.Warning("YAML002", "duplicate key", span);
        global::yaml.YamlDiagnostic info = global::yaml.YamlDiagnostic.Info("YAML003", "note", span);

        Assert.That(error.IsError(), Is.True);
        Assert.That(error.Code, Is.EqualTo("YAML001"));
        Assert.That(error.Message, Is.EqualTo("bad token"));
        Assert.That(error.Span, Is.SameAs(span));
        Assert.That(error.Format(), Is.EqualTo("Error YAML001 at 1:1-1:6: bad token"));
        Assert.That(warning.IsWarning(), Is.True);
        Assert.That(info.IsInfo(), Is.True);
    }

    [Test]
    public void Result_TracksDiagnosticsAndSuccessState()
    {
        global::yaml.YamlResult result = new global::yaml.YamlResult();
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.Empty();

        result.AddWarning("YAML010", "warning", span);

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.WarningCount(), Is.EqualTo(1));

        result.AddError("YAML011", "error", span);

        Assert.That(result.Success, Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.FormatDiagnostics(), Does.Contain("Warning YAML010"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("Error YAML011"));

        result.Reset();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(0));
    }

    [Test]
    public void ParseResult_StoresNullableValueAndTracksErrors()
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();
        object value = "parsed";

        Assert.That(result.Value, Is.Null);

        result.SetValue(value);
        result.AddError("YAML020", "parse failed", global::yaml.YamlSourceSpan.FromBounds(1, 1, 2, 2, 1, 3));

        Assert.That(result.Value, Is.SameAs(value));
        Assert.That(result.Success, Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.ErrorCount(), Is.EqualTo(1));
        Assert.That(result.FormatDiagnostics(), Is.EqualTo("Error YAML020 at 1:2-1:3: parse failed"));

        result.Reset();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(0));
    }
}
