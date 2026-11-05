using System.Text;
using NUnit.Framework;

namespace odf.Tests;

public class OdfAnnotationEditorTests
{
    [Test]
    public void CreatesFindsUpdatesAndChangesPointAnnotationState()
    {
        OdfDocument document = ReadFlat("<text:p>Hello world</text:p>");
        IOdfTextEditor textEditor = new OdfTextEditor();
        OdfTextCursor cursor = textEditor.CreateTextCursor(
            document, document.TextBlocks[0], 5);
        IOdfAnnotationEditor editor = new OdfAnnotationEditor();
        OdfAnnotation template = Annotation("", "Initial note");
        template.Creator = "Ada";
        template.Date = "2026-09-02T10:15:00Z";
        template.Initials = "AL";

        OdfAnnotation created = editor.CreateAnnotation(
            document, cursor, template);
        OdfAnnotation[] snapshot = editor.GetAnnotations(document);
        OdfAnnotation replacement = Annotation(created.Name, "Revised note");
        replacement.Creator = "Grace";
        replacement.Date = "2026-09-03";
        replacement.Initials = "GH";
        editor.ResolveAnnotation(document, created);
        Assert.That(created.State, Is.EqualTo(OdfAnnotationState.Resolved));
        editor.ReopenAnnotation(document, created);
        editor.UpdateAnnotation(document, created, replacement);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(created.Name, Is.EqualTo("__Annotation__1"));
            Assert.That(editor.FindAnnotation(document, created.Name),
                Is.SameAs(created));
            Assert.That(snapshot, Is.Not.SameAs(document.Annotations));
            Assert.That(snapshot, Is.EqualTo(new[] { created }));
            Assert.That(created.IsRange, Is.False);
            Assert.That(created.State, Is.EqualTo(OdfAnnotationState.Unknown));
            Assert.That(reread.Annotations, Has.Length.EqualTo(1));
            Assert.That(reread.Annotations[0].Name,
                Is.EqualTo("__Annotation__1"));
            Assert.That(reread.Annotations[0].Creator, Is.EqualTo("Grace"));
            Assert.That(reread.Annotations[0].Date, Is.EqualTo("2026-09-03"));
            Assert.That(reread.Annotations[0].Initials, Is.EqualTo("GH"));
            Assert.That(reread.Annotations[0].Text,
                Is.EqualTo("Revised note"));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("Hello world"));
        });
    }

    [Test]
    public void CreatesAndPersistsSelectedRangeAnnotation()
    {
        OdfDocument document = ReadFlat(
            "<text:p>Alpha selected omega</text:p>");
        IOdfTextEditor textEditor = new OdfTextEditor();
        OdfTextCursor cursor = textEditor.CreateTextCursor(
            document, document.TextBlocks[0], 14);
        textEditor.SetTextSelection(
            document, cursor, document.TextBlocks[0], 6);
        IOdfAnnotationEditor editor = new OdfAnnotationEditor();

        OdfAnnotation created = editor.CreateAnnotation(
            document, cursor, Annotation("selection", "Review this"));
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(created.IsRange, Is.True);
            Assert.That(created.IsRangeResolved, Is.True);
            Assert.That(created.AnnotatedText, Is.EqualTo("selected"));
            Assert.That(reread.Annotations, Has.Length.EqualTo(1));
            Assert.That(reread.Annotations[0].IsRangeResolved, Is.True);
            Assert.That(reread.Annotations[0].AnnotatedText,
                Is.EqualTo("selected"));
            Assert.That(reread.TextBlocks[0].Text,
                Is.EqualTo("Alpha selected omega"));
        });
    }

    [Test]
    public void CreatesRepliesAndPromotesThemWhenParentIsRemoved()
    {
        OdfDocument document = ReadFlat("<text:p>Question</text:p>");
        IOdfTextEditor textEditor = new OdfTextEditor();
        OdfTextCursor cursor = textEditor.CreateTextCursor(
            document, document.TextBlocks[0], 0);
        IOdfAnnotationEditor editor = new OdfAnnotationEditor();
        OdfAnnotation root = editor.CreateAnnotation(
            document, cursor, Annotation("root", "Question note"));
        OdfAnnotation reply = editor.CreateAnnotationReply(
            document, root, Annotation("reply", "Answer note"));
        editor.UpdateAnnotation(
            document, reply, Annotation("reply", "Updated answer"));
        Assert.That(reply.Parent, Is.SameAs(root));

        bool removed = editor.RemoveAnnotation(document, root);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(reply.Parent, Is.Null);
            Assert.That(reply.ParentName, Is.Empty);
            Assert.That(document.Annotations, Is.EqualTo(new[] { reply }));
            Assert.That(reread.Annotations, Has.Length.EqualTo(1));
            Assert.That(reread.Annotations[0].Name, Is.EqualTo("reply"));
            Assert.That(reread.Annotations[0].Parent, Is.Null);
            Assert.That(reread.Annotations[0].Text,
                Is.EqualTo("Updated answer"));
        });
    }

    [Test]
    public void RemovesAnEntireReplySubtree()
    {
        OdfDocument document = ReadFlat("<text:p>Thread</text:p>");
        IOdfTextEditor textEditor = new OdfTextEditor();
        OdfTextCursor cursor = textEditor.CreateTextCursor(
            document, document.TextBlocks[0], 6);
        IOdfAnnotationEditor editor = new OdfAnnotationEditor();
        OdfAnnotation root = editor.CreateAnnotation(
            document, cursor, Annotation("root", "Root"));
        OdfAnnotation reply = editor.CreateAnnotationReply(
            document, root, Annotation("reply", "Reply"));
        editor.CreateAnnotationReply(
            document, reply, Annotation("nested", "Nested"));

        int removed = editor.RemoveAnnotationThread(document, reply);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(document.Annotations, Is.EqualTo(new[] { root }));
            Assert.That(root.Replies, Is.Empty);
            Assert.That(reread.Annotations, Has.Length.EqualTo(1));
            Assert.That(reread.Annotations[0].Name, Is.EqualTo("root"));
            Assert.That(reread.Annotations[0].Replies, Is.Empty);
        });
    }

    [Test]
    public void RejectsDuplicateNamesAndInvalidDatesWithoutMutation()
    {
        OdfDocument document = ReadFlat("<text:p>Validation</text:p>");
        IOdfTextEditor textEditor = new OdfTextEditor();
        OdfTextCursor cursor = textEditor.CreateTextCursor(
            document, document.TextBlocks[0], 0);
        IOdfAnnotationEditor editor = new OdfAnnotationEditor();
        editor.CreateAnnotation(
            document, cursor, Annotation("existing", "Valid"));
        OdfAnnotation duplicate = Annotation("existing", "Duplicate");
        OdfAnnotation invalidDate = Annotation("new", "Invalid date");
        invalidDate.Date = "not-a-date";

        Assert.Multiple(() =>
        {
            Assert.That(
                () => editor.CreateAnnotation(document, cursor, duplicate),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.CreateAnnotation(document, cursor, invalidDate),
                Throws.TypeOf<OdfEditException>());
            Assert.That(document.Annotations, Has.Length.EqualTo(1));
        });
    }

    static OdfAnnotation Annotation(string name, string content)
    {
        OdfAnnotation result = new OdfAnnotation();
        result.Name = name;
        result.Text = content;
        return result;
    }

    static OdfDocument ReadFlat(string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:meta='" + OdfNamespaces.Metadata
            + "' xmlns:loext='" + OdfNamespaces.LibreOffice
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "annotation-editor.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document, new OdfSaveOptions());
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes, "annotation-editor.fodt", new OdfLoadOptions());
    }
}
