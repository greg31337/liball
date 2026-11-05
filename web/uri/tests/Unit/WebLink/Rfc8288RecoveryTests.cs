namespace uri.Tests;

public class Rfc8288RecoveryTests
{
    [Test]
    public void AppendixBReportsEveryRecoveryDiagnosticInSourceOrder()
    {
        string[] fieldValues = new string[]
        {
            "garbage",
            "<truncated",
            "<http://[bad]>",
            "<a><b>",
            "<c>; bad@name=value",
            "<d>; value=two words",
            "<e>; title=\"bad\rtext\"",
            "<f>; title=\"unterminated",
            "<g>; title=\"dangling\\"
        };

        WebLinkParseResult result = ParseFields(fieldValues);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[]
                {
                    "a",
                    "b",
                    "c",
                    "d",
                    "e",
                    "f",
                    "g"
                }));
            Assert.That(result.Diagnostics.Select(item => item.Message),
                Is.EqualTo(new string[]
                {
                    WebLinkRecoveryParserCore.DiscardedRemainderMessage,
                    WebLinkRecoveryParserCore.TruncatedTargetMessage,
                    WebLinkRecoveryParserCore.InvalidTargetMessage,
                    WebLinkRecoveryParserCore.MissingSeparatorMessage,
                    WebLinkRecoveryParserCore.InvalidParameterNameMessage,
                    WebLinkRecoveryParserCore.InvalidParameterValueMessage,
                    WebLinkRecoveryParserCore.InvalidQuotedContentMessage,
                    WebLinkRecoveryParserCore.UnterminatedQuotedStringMessage,
                    WebLinkRecoveryParserCore.IncompleteQuotedPairMessage
                }));
            Assert.That(result.Diagnostics.All(item =>
                item.Domain == DiagnosticDomain.WebLink), Is.True);
            Assert.That(result.Diagnostics.All(item =>
                item.Severity == DiagnosticSeverity.Warning), Is.True);
            Assert.That(result.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new UriDiagnosticCode[]
                {
                    UriDiagnosticCode.InvalidWebLink,
                    UriDiagnosticCode.InvalidLinkTarget,
                    UriDiagnosticCode.InvalidLinkTarget,
                    UriDiagnosticCode.InvalidWebLink,
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriDiagnosticCode.InvalidLinkParameter,
                    UriDiagnosticCode.InvalidLinkParameter
                }));
            Assert.That(result.LinkSet.Diagnostics,
                Is.SameAs(result.Diagnostics));
            Assert.That(result.LinkSet.OriginalFieldValues,
                Is.EqualTo(fieldValues));
        });
    }

    [Test]
    public void AppendixBRetainsMalformedParametersAndContinuesScanning()
    {
        const string fieldValue =
            ", , <one>; =unnamed; bad@name=ok; empty=; " +
            "spaced=two words; good=yes, , <two>, ,";

        WebLinkParseResult result = ParseField(fieldValue);
        WebLink first = result.LinkSet.Links[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "one", "two" }));
            Assert.That(first.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[]
                {
                    "",
                    "bad@name",
                    "empty",
                    "spaced",
                    "good"
                }));
            Assert.That(first.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new string[]
                {
                    "unnamed",
                    "ok",
                    "",
                    "two words",
                    "yes"
                }));
            Assert.That(result.Diagnostics.Select(item => item.Message),
                Is.EqualTo(new string[]
                {
                    WebLinkRecoveryParserCore.InvalidParameterNameMessage,
                    WebLinkRecoveryParserCore.InvalidParameterNameMessage,
                    WebLinkRecoveryParserCore.InvalidParameterValueMessage,
                    WebLinkRecoveryParserCore.InvalidParameterValueMessage
                }));
        });
    }

    [Test]
    public void AppendixBUnterminatedQuoteConsumesOnlyItsOwnFieldRemainder()
    {
        string[] fieldValues = new string[]
        {
            "<one>; title=\"unterminated, <not-a-link>",
            "<two>; title=complete",
            "<three>"
        };

        WebLinkParseResult result = ParseFields(fieldValues);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "one", "two", "three" }));
            Assert.That(result.LinkSet.Links[0].Title,
                Is.EqualTo("unterminated, <not-a-link>"));
            Assert.That(result.LinkSet.Links[1].Title,
                Is.EqualTo("complete"));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(
                    WebLinkRecoveryParserCore.UnterminatedQuotedStringMessage));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(fieldValues[0].Length));
            Assert.That(result.Diagnostics[0].Range.Length, Is.Zero);
        });
    }

    [Test]
    public void AppendixBDiscardsBadTargetsAndRetainsLaterValidLinks()
    {
        const string fieldValue =
            "<http://[bad]>,,, <valid>; rel=next, <also-valid>";

        WebLinkParseResult result = ParseField(fieldValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.LinkSet.Links.Select(link => link.Target),
                Is.EqualTo(new string[] { "valid", "also-valid" }));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(WebLinkRecoveryParserCore.InvalidTargetMessage));
        });
    }

    private static WebLinkParseResult ParseField(string fieldValue)
    {
        return WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
            fieldValue.AsSpan(),
            CreateOptions());
    }

    private static WebLinkParseResult ParseFields(string[] fieldValues)
    {
        return WebLinkRecoveryParserCore.ParseRecoveryFieldValues(
            fieldValues,
            CreateOptions());
    }

    private static WebLinkParseOptions CreateOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }
}
