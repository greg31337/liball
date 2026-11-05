using System.Text;

namespace odf.Tests;

public class TextSearchModelTests
{
    [Test]
    public void FindsLiteralTextInDocumentOrderWithLocationsAndContext()
    {
        OdfDocument document = ReadFlat(
            "<text:p>Before Alpha after alpha.</text:p>"
            + "<text:p>ALPHA ending</text:p>");

        OdfSearchMatch[] sensitive = TextSearchModel.SearchLiteral(
            document,
            "alpha",
            true);
        OdfSearchMatch[] insensitive = TextSearchModel.SearchLiteral(
            document,
            "alpha",
            false);

        Assert.Multiple(() =>
        {
            Assert.That(sensitive, Has.Length.EqualTo(1));
            Assert.That(sensitive[0].Location.TextOffset, Is.EqualTo(19));
            Assert.That(sensitive[0].Location.TextLength, Is.EqualTo(5));
            Assert.That(sensitive[0].MatchedValue, Is.EqualTo("alpha"));
            Assert.That(sensitive[0].Node, Is.SameAs(document.TextBlocks[0].Node));
            Assert.That(insensitive, Has.Length.EqualTo(3));
            Assert.That(
                insensitive.Select(item => item.MatchedValue),
                Is.EqualTo(new[] { "Alpha", "alpha", "ALPHA" }));
            Assert.That(insensitive[0].IsExact, Is.False);
            Assert.That(insensitive[1].IsExact, Is.True);
            Assert.That(insensitive[0].Context, Does.Contain("Before Alpha after"));
        });
    }

    [Test]
    public void ReplacesRegularExpressionsWithCapturesAndTracksCursors()
    {
        OdfDocument document = ReadFlat("<text:p>A12 B345</text:p>");
        OdfTextCursor cursor = TextEditingModel.CreateCursor(
            document,
            document.TextBlocks[0],
            8);

        OdfSearchMatch[] matches = TextSearchModel.SearchRegularExpression(
            document,
            "([A-Z])(\\d+)",
            true);
        OdfEditResult result = TextSearchModel.ReplaceRegularExpression(
            document,
            "([A-Z])(\\d+)",
            "$1[$2]",
            true);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(matches, Has.Length.EqualTo(2));
            Assert.That(matches[0].MatchedValue, Is.EqualTo("A12"));
            Assert.That(matches[1].Location.TextOffset, Is.EqualTo(4));
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.Changed, Is.True);
            Assert.That(result.AffectedCount, Is.EqualTo(2));
            Assert.That(result.SearchMatches, Has.Length.EqualTo(2));
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("A[12] B[345]"));
            Assert.That(cursor.Offset, Is.EqualTo(12));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("A[12] B[345]"));
        });
    }

    [Test]
    public void ReplacesEveryLiteralMatchAndReportsNoChangeForEqualText()
    {
        OdfDocument document = ReadFlat(
            "<text:p>cat cat</text:p><text:p>Cat</text:p>");

        OdfEditResult replaced = TextSearchModel.ReplaceLiteral(
            document,
            "cat",
            "dog",
            false);
        OdfEditResult unchanged = TextSearchModel.ReplaceLiteral(
            document,
            "dog",
            "dog",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(replaced.AffectedCount, Is.EqualTo(3));
            Assert.That(
                document.TextBlocks.Select(item => item.Text),
                Is.EqualTo(new[] { "dog dog", "dog" }));
            Assert.That(unchanged.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(unchanged.Changed, Is.False);
            Assert.That(unchanged.AffectedCount, Is.Zero);
            Assert.That(unchanged.SearchMatches, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void FindsAndReplacesTextByStyleWhileRetainingInlineStyle()
    {
        OdfDocument document = ReadFlat(
            "<text:p>cold <text:span text:style-name='Emphasis'>hot</text:span></text:p>"
            + "<text:p text:style-name='Emphasis'>whole</text:p>");

        OdfSearchMatch[] matches = TextSearchModel.SearchStyle(
            document,
            "Emphasis");
        OdfEditResult result = TextSearchModel.ReplaceStyle(
            document,
            "Emphasis",
            "warm");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(matches, Has.Length.EqualTo(2));
            Assert.That(
                matches.Select(item => item.MatchedValue),
                Is.EqualTo(new[] { "hot", "whole" }));
            Assert.That(matches[0].Location.TextOffset, Is.EqualTo(5));
            Assert.That(result.AffectedCount, Is.EqualTo(2));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("cold warm"));
            Assert.That(reread.TextBlocks[0].Runs[1].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(reread.TextBlocks[0].Runs[1].StyleName, Is.EqualTo("Emphasis"));
            Assert.That(reread.TextBlocks[0].Runs[1].Text, Is.EqualTo("warm"));
            Assert.That(reread.TextBlocks[1].Text, Is.EqualTo("warm"));
            Assert.That(reread.TextBlocks[1].StyleName, Is.EqualTo("Emphasis"));
        });
    }

    [Test]
    public void FindsAndReplacesQualifiedTextElements()
    {
        OdfDocument document = ReadFlat(
            "<text:p>one <text:span>two</text:span> three</text:p>"
            + "<text:p><text:span>four</text:span></text:p>");
        OdfQualifiedName spanName = new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = "span",
            Prefix = "ignored"
        };

        OdfSearchMatch[] matches = TextSearchModel.SearchElement(
            document,
            spanName);
        OdfEditResult result = TextSearchModel.ReplaceElement(
            document,
            spanName,
            "X");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(matches, Has.Length.EqualTo(2));
            Assert.That(
                matches.Select(item => item.MatchedValue),
                Is.EqualTo(new[] { "two", "four" }));
            Assert.That(result.AffectedCount, Is.EqualTo(2));
            Assert.That(
                reread.TextBlocks.Select(item => item.Text),
                Is.EqualTo(new[] { "one X three", "X" }));
            Assert.That(reread.TextBlocks[0].Runs[1].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(reread.TextBlocks[0].Runs[1].Text, Is.EqualTo("X"));
            Assert.That(reread.TextBlocks[1].Runs[0].Kind, Is.EqualTo(OdfTextRunKind.Span));
        });
    }

    private static OdfDocument ReadFlat(string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles/>"
            + "<office:body><office:text>" + body + "</office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-search.fodt",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "text-search.fodt",
            new OdfLoadOptions());
    }
}
