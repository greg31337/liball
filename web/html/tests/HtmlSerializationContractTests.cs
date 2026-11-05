using System.IO;
using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSerializationContractTests
{
    [Test]
    public void PublicFacadeSerializesEverySupportedScopeToStrings()
    {
        HtmlSerializerApi api = new HtmlSerializerApi();
        HtmlSerializerOptions options = api.CreateCanonicalOptions();
        HtmlDocument document = Html.Parse(
            "<!doctype html><html><head><title>T</title></head>"
            + "<body><p>one</p></body></html>");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode fragment = Html.ParseFragment(
            "<b>two</b>tail", "body").GetFragment();

        HtmlSerializationResult whole = api.SerializeDocument(document,
            options, CancellationToken.None);
        HtmlSerializationResult fragmentResult = api.SerializeFragment(
            fragment, options, CancellationToken.None);
        HtmlSerializationResult subtree = api.SerializeSubtree(paragraph,
            options, CancellationToken.None);
        HtmlSerializationResult inner = api.SerializeInnerHtml(paragraph,
            options, CancellationToken.None);
        HtmlSerializationResult outer = api.SerializeOuterHtml(paragraph,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(whole.GetString(), Is.EqualTo(
                "<!DOCTYPE html><html><head><title>T</title></head>"
                + "<body><p>one</p></body></html>"));
            Assert.That(fragmentResult.GetString(),
                Is.EqualTo("<b>two</b>tail"));
            Assert.That(subtree.GetString(), Is.EqualTo("<p>one</p>"));
            Assert.That(inner.GetString(), Is.EqualTo("one"));
            Assert.That(outer.GetString(), Is.EqualTo("<p>one</p>"));
            Assert.That(whole.GetStatus(),
                Is.EqualTo(HtmlSerializationStatus.Complete));
            Assert.That(whole.IsComplete(), Is.True);
            Assert.That(whole.WasLimitExceeded(), Is.False);
            Assert.That(whole.IsPartial(), Is.False);
            Assert.That(whole.HasString(), Is.True);
            Assert.That(whole.GetCharactersWritten(),
                Is.EqualTo(whole.GetString().Length));
            Assert.That(whole.GetFailureCode(),
                Is.EqualTo(HtmlDiagnosticCode.None));
        });
    }

    [Test]
    public void PublicFacadeWritesEverySupportedScopeWithCharacterCounts()
    {
        HtmlSerializerApi api = new HtmlSerializerApi();
        HtmlSerializerOptions options = api.CreateCanonicalOptions();
        HtmlDocument document = Html.Parse("<p>one</p>");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode fragment = Html.ParseFragment(
            "<b>two</b>tail", "body").GetFragment();
        StringWriter documentWriter = new StringWriter();
        StringWriter fragmentWriter = new StringWriter();
        StringWriter subtreeWriter = new StringWriter();
        StringWriter innerWriter = new StringWriter();
        StringWriter outerWriter = new StringWriter();

        HtmlSerializationResult whole = api.WriteDocument(document,
            documentWriter, options, CancellationToken.None);
        HtmlSerializationResult fragmentResult = api.WriteFragment(fragment,
            fragmentWriter, options, CancellationToken.None);
        HtmlSerializationResult subtree = api.WriteSubtree(paragraph,
            subtreeWriter, options, CancellationToken.None);
        HtmlSerializationResult inner = api.WriteInnerHtml(paragraph,
            innerWriter, options, CancellationToken.None);
        HtmlSerializationResult outer = api.WriteOuterHtml(paragraph,
            outerWriter, options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(documentWriter.ToString(), Is.EqualTo(
                "<html><head></head><body><p>one</p></body></html>"));
            Assert.That(fragmentWriter.ToString(),
                Is.EqualTo("<b>two</b>tail"));
            Assert.That(subtreeWriter.ToString(),
                Is.EqualTo("<p>one</p>"));
            Assert.That(innerWriter.ToString(), Is.EqualTo("one"));
            Assert.That(outerWriter.ToString(),
                Is.EqualTo("<p>one</p>"));
            Assert.That(whole.HasString(), Is.False);
            Assert.That(whole.GetString(), Is.Null);
            Assert.That(whole.GetCharactersWritten(),
                Is.EqualTo(documentWriter.ToString().Length));
            Assert.That(fragmentResult.GetCharactersWritten(),
                Is.EqualTo(fragmentWriter.ToString().Length));
            Assert.That(subtree.GetCharactersWritten(), Is.EqualTo(10));
            Assert.That(inner.GetCharactersWritten(), Is.EqualTo(3));
            Assert.That(outer.GetCharactersWritten(), Is.EqualTo(10));
        });
    }

    [Test]
    public void PublicPresetsAndOptionsControlSerializationModes()
    {
        HtmlSerializerApi api = new HtmlSerializerApi();
        HtmlNode fragment = Html.ParseFragment(
            "<DIV z=\"'\" a='\"'>é<svg><path></path></svg></DIV>",
            "body").GetFragment();
        HtmlSerializerOptions configured = api.CreateCanonicalOptions();
        configured.SetAsciiOnly(true);
        configured.SetQuoteMode(HtmlSerializerQuoteMode.Shortest);
        configured.SetSortAttributes(true);
        configured.SetSelfCloseForeignElements(true);

        string configuredHtml = api.SerializeFragment(fragment, configured,
            CancellationToken.None).GetString();

        Assert.Multiple(() =>
        {
            Assert.That(api.CreateCanonicalOptions().GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Canonical));
            Assert.That(api.CreatePreserveOptions().GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Preserve));
            Assert.That(api.CreateAsciiOnlyOptions().GetAsciiOnly(), Is.True);
            Assert.That(api.CreateCompactOptions().GetOmitOptionalTags(),
                Is.True);
            Assert.That(api.CreatePrettyOptions().GetPretty(), Is.True);
            Assert.That(configuredHtml, Is.EqualTo(
                "<div a='\"' z=\"'\">&eacute;<svg><path /></svg></div>"));
        });
    }

    [Test]
    public void BoundedResultsRetainSafePrefixesWithoutThrowing()
    {
        HtmlSerializerApi api = new HtmlSerializerApi();
        HtmlNode paragraph = Html.ParseFragment(
            "<p>x</p>", "body").GetFragment().GetFirstChild();
        HtmlSerializerOptions options = api.CreateCanonicalOptions();
        HtmlLimits limits = options.GetLimits();
        limits.SetMaximumOutputCharacters(7);
        options.SetLimits(limits);
        StringWriter writer = new StringWriter();

        HtmlSerializationResult text = api.SerializeOuterHtml(paragraph,
            options, CancellationToken.None);
        HtmlSerializationResult written = api.WriteOuterHtml(paragraph,
            writer, options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(text.GetString(), Is.EqualTo("<p>x</p"));
            Assert.That(text.GetStatus(),
                Is.EqualTo(HtmlSerializationStatus.LimitExceeded));
            Assert.That(text.IsComplete(), Is.False);
            Assert.That(text.WasLimitExceeded(), Is.True);
            Assert.That(text.IsPartial(), Is.True);
            Assert.That(text.GetCharactersWritten(), Is.EqualTo(7));
            Assert.That(text.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.SerializationLimitExceeded));
            Assert.That(writer.ToString(), Is.EqualTo("<p>x</p"));
            Assert.That(written.GetStatus(), Is.EqualTo(
                HtmlSerializationStatus.LimitExceeded));
            Assert.That(written.GetCharactersWritten(), Is.EqualTo(7));
        });
    }

    [Test]
    public void ScopeValidationAndCancellationProtectWriterState()
    {
        HtmlSerializerApi api = new HtmlSerializerApi();
        HtmlSerializerOptions options = api.CreateCanonicalOptions();
        HtmlNode fragment = Html.ParseFragment(
            "<p>x</p>", "body").GetFragment();
        HtmlNode paragraph = fragment.GetFirstChild();
        StringWriter writer = new StringWriter();
        writer.Write("existing");
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => api.SerializeFragment(
                paragraph, options, CancellationToken.None));
            Assert.Throws<ArgumentException>(() => api.SerializeOuterHtml(
                fragment, options, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => api.WriteSubtree(
                paragraph, writer, options, cancellation.Token));
            Assert.That(writer.ToString(), Is.EqualTo("existing"));
        });
    }
}
