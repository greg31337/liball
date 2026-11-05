using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxBookmarkTests
{
    [Test]
    public void Parse_ReadsBookmarkStartAttributes()
    {
        global::docx.DocxBookmark bookmark = ParseBookmark("<w:bookmarkStart xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"42\" w:name=\"Customer\" w:colFirst=\"1\" w:colLast=\"3\" />");

        Assert.That(bookmark.Kind, Is.EqualTo(global::docx.DocxBookmark.KindStart));
        Assert.That(bookmark.IsStart(), Is.True);
        Assert.That(bookmark.IsEnd(), Is.False);
        Assert.That(bookmark.Id, Is.EqualTo("42"));
        Assert.That(bookmark.Name, Is.EqualTo("Customer"));
        Assert.That(bookmark.ColumnFirst, Is.EqualTo("1"));
        Assert.That(bookmark.ColumnLast, Is.EqualTo("3"));
        Assert.That(bookmark.GetXml(), Does.Contain("bookmarkStart"));
    }

    [Test]
    public void Parse_ReadsBookmarkEndAttributes()
    {
        global::docx.DocxBookmark bookmark = ParseBookmark("<w:bookmarkEnd xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"42\" />");

        Assert.That(bookmark.Kind, Is.EqualTo(global::docx.DocxBookmark.KindEnd));
        Assert.That(bookmark.IsStart(), Is.False);
        Assert.That(bookmark.IsEnd(), Is.True);
        Assert.That(bookmark.Id, Is.EqualTo("42"));
        Assert.That(bookmark.Name, Is.EqualTo(""));
    }

    [Test]
    public void ParagraphParsing_ExposesDirectBookmarksInOrder()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:bookmarkStart w:id=\"42\" w:name=\"Customer\" /><w:r><w:t>Text</w:t></w:r><w:bookmarkEnd w:id=\"42\" /><w:smartTag><w:bookmarkStart w:id=\"99\" w:name=\"Nested\" /></w:smartTag></w:p>");

        Assert.That(paragraph.ChildCount(), Is.EqualTo(4));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.BookmarkCount(), Is.EqualTo(2));
        Assert.That(paragraph.BookmarkStartCount(), Is.EqualTo(1));
        Assert.That(paragraph.BookmarkEndCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetBookmark(0).Name, Is.EqualTo("Customer"));
        Assert.That(paragraph.GetBookmark(1).Id, Is.EqualTo("42"));
        Assert.That(paragraph.FindBookmarkStartByName("Customer"), Is.SameAs(paragraph.GetBookmark(0)));
        Assert.That(paragraph.FindBookmarkStartByName("Nested"), Is.Null);
        Assert.That(paragraph.FindBookmarkEndById("42"), Is.SameAs(paragraph.GetBookmark(1)));
    }

    [Test]
    public void ParagraphWriting_PreservesParsedBookmarks()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:bookmarkStart w:id=\"7\" w:name=\"Place\" /><w:r><w:t>Here</w:t></w:r><w:bookmarkEnd w:id=\"7\" /></w:p>");

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(written.BookmarkCount(), Is.EqualTo(2));
        Assert.That(written.GetBookmark(0).Name, Is.EqualTo("Place"));
        Assert.That(written.GetBookmark(1).Id, Is.EqualTo("7"));
    }

    [Test]
    public void CreateStartAndEnd_WriteXmlRoundTrips()
    {
        global::docx.DocxBookmark start = global::docx.DocxBookmark.CreateStart("12", "Place");
        global::docx.DocxBookmark end = global::docx.DocxBookmark.CreateEnd("12");

        start.SetColumnRange("1", "2");

        global::docx.DocxBookmark parsedStart = ParseBookmark(start.WriteXml());
        global::docx.DocxBookmark parsedEnd = ParseBookmark(end.WriteXml());

        Assert.That(parsedStart.IsStart(), Is.True);
        Assert.That(parsedStart.Id, Is.EqualTo("12"));
        Assert.That(parsedStart.Name, Is.EqualTo("Place"));
        Assert.That(parsedStart.ColumnFirst, Is.EqualTo("1"));
        Assert.That(parsedStart.ColumnLast, Is.EqualTo("2"));
        Assert.That(parsedEnd.IsEnd(), Is.True);
        Assert.That(parsedEnd.Id, Is.EqualTo("12"));
    }

    [Test]
    public void ParagraphCreation_AllocatesFirstUnusedBookmarkIdAndWritesPair()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:bookmarkStart w:id=\"0\" w:name=\"First\" /><w:bookmarkEnd w:id=\"0\" /><w:bookmarkStart w:id=\"2\" w:name=\"Third\" /><w:bookmarkEnd w:id=\"2\" /></w:p>");

        string id = paragraph.AddBookmarkPair("Second");

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(id, Is.EqualTo("1"));
        Assert.That(written.BookmarkCount(), Is.EqualTo(6));
        Assert.That(written.GetBookmark(4).IsStart(), Is.True);
        Assert.That(written.GetBookmark(4).Id, Is.EqualTo("1"));
        Assert.That(written.GetBookmark(4).Name, Is.EqualTo("Second"));
        Assert.That(written.GetBookmark(5).IsEnd(), Is.True);
        Assert.That(written.GetBookmark(5).Id, Is.EqualTo("1"));
    }

    [Test]
    public void ParagraphCreation_AddsBookmarkStartAndEndSeparately()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        global::docx.DocxBookmark start = paragraph.AddBookmarkStart("StartOnly");
        global::docx.DocxBookmark end = paragraph.AddBookmarkEnd(start.Id);

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(end.Id, Is.EqualTo(start.Id));
        Assert.That(written.BookmarkStartCount(), Is.EqualTo(1));
        Assert.That(written.BookmarkEndCount(), Is.EqualTo(1));
        Assert.That(written.FindBookmarkStartByName("StartOnly"), Is.Not.Null);
        Assert.That(written.FindBookmarkEndById(start.Id), Is.Not.Null);
    }

    [Test]
    public void ParagraphCreation_AddsMultipleBookmarkPairsWithStableLookupAfterWrite()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        string firstId = paragraph.AddBookmarkPair("First");
        string secondId = paragraph.AddBookmarkPair("Second");

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(firstId, Is.EqualTo("0"));
        Assert.That(secondId, Is.EqualTo("1"));
        Assert.That(written.BookmarkCount(), Is.EqualTo(4));
        Assert.That(written.FindBookmarkStartByName("First")!.Id, Is.EqualTo("0"));
        Assert.That(written.FindBookmarkStartByName("Second")!.Id, Is.EqualTo("1"));
        Assert.That(written.FindBookmarkEndById("0"), Is.Not.Null);
        Assert.That(written.FindBookmarkEndById("1"), Is.Not.Null);
    }

    [Test]
    public void BookmarkParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxBookmark bookmark = global::docx.DocxBookmark.CreateStart("1", "Name");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBookmark(null!, "start", "1", "Name", "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBookmark("", null!, "1", "Name", "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBookmark("", "start", null!, "Name", "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBookmark("", "start", "1", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBookmark("", "start", "1", "Name", null!, ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBookmark("", "start", "1", "Name", "", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxBookmark("", "", "1", "Name", "", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.CreateStart(null!, "Name"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.CreateStart("1", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxBookmark.CreateStart("", "Name"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxBookmark.CreateStart("1", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.CreateEnd(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxBookmark.CreateEnd(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.ReadRequiredAttribute(null!, "id"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBookmark.ReadRequiredAttribute(ParseReader("<w:bookmarkEnd xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\" />"), null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxBookmark.ReadRequiredAttribute(ParseReader("<w:bookmarkEnd xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\" />"), ""));
        Assert.Throws<ArgumentNullException>(() => bookmark.SetId(null!));
        Assert.Throws<ArgumentNullException>(() => bookmark.SetName(null!));
        Assert.Throws<ArgumentNullException>(() => bookmark.SetColumnRange(null!, ""));
        Assert.Throws<ArgumentNullException>(() => bookmark.SetColumnRange("", null!));
        Assert.Throws<ArgumentNullException>(() => bookmark.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => bookmark.WriteStartTo(null!));
        Assert.Throws<ArgumentNullException>(() => bookmark.WriteEndTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddBookmark(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.CountBookmarks(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.FindBookmarkStartByName(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.FindBookmarkEndById(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ContainsBookmarkId(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddBookmarkStart(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddBookmarkEnd(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddBookmarkPair(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxBookmarkFromBuffer(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxBookmark.ParseXml("<root />"));
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxBookmark("", "other", "1", "Name", "", "").WriteXml());
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxBookmark("", global::docx.DocxBookmark.KindStart, "", "Name", "", "").WriteXml());
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxBookmark("", global::docx.DocxBookmark.KindStart, "1", "", "", "").WriteXml());
        Assert.Throws<InvalidOperationException>(() => new global::docx.DocxBookmark("", global::docx.DocxBookmark.KindEnd, "", "", "", "").WriteXml());
        Assert.Throws<InvalidDataException>(() => global::docx.DocxBookmark.ParseXml("<w:bookmarkStart xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:name=\"Name\" />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxBookmark.ParseXml("<w:bookmarkStart xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\" />"));
        Assert.Throws<InvalidDataException>(() => global::docx.DocxBookmark.ParseXml("<w:bookmarkEnd xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.That(global::docx.DocxBookmark.IsSupported(reader), Is.False);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxBookmark.Parse(reader));
    }

    static global::docx.DocxBookmark ParseBookmark(string xml)
    {
        global::docx.DocxXmlReader reader = ParseReader(xml);

        return global::docx.DocxBookmark.Parse(reader);
    }

    static global::docx.DocxXmlReader ParseReader(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.MoveToContent(), Is.True);

        return reader;
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}
