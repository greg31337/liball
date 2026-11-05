using System.Text;

namespace odf.Tests;

public class TextChangeDocumentModelTests
{
    [Test]
    public void MaterializesStandardAndUnknownChangesWithBoundaries()
    {
        OdfDocument document = ReadFlat(CompleteChangesXml());
        OdfChange insertion = document.Changes[0];
        OdfChange deletion = document.Changes[1];
        OdfChange format = document.Changes[2];
        OdfChange unknown = document.Changes[3];

        Assert.Multiple(() =>
        {
            Assert.That(document.TrackTextChanges, Is.True);
            Assert.That(document.Changes, Has.Length.EqualTo(4));
            Assert.That(insertion.Kind, Is.EqualTo(OdfChangeKind.Insertion));
            Assert.That(insertion.Id, Is.EqualTo("insert"));
            Assert.That(insertion.XmlId, Is.EqualTo("insert"));
            Assert.That(insertion.Creator, Is.EqualTo("Alice"));
            Assert.That(insertion.Date,
                Is.EqualTo("2026-09-01T12:00:00Z"));
            Assert.That(insertion.IsDateValid, Is.True);
            Assert.That(insertion.Comment, Is.EqualTo("First\nSecond"));
            Assert.That(insertion.IsRange, Is.True);
            Assert.That(insertion.IsBoundaryResolved, Is.True);
            Assert.That(insertion.ChangedText, Is.EqualTo("new words"));
            Assert.That(insertion.StartBlock,
                Is.SameAs(insertion.EndBlock));
            Assert.That(deletion.Kind, Is.EqualTo(OdfChangeKind.Deletion));
            Assert.That(deletion.Id, Is.EqualTo("delete"));
            Assert.That(deletion.XmlId, Is.EqualTo("delete-xml"));
            Assert.That(deletion.DeletedText,
                Is.EqualTo("removed\ncontent"));
            Assert.That(deletion.DeletedTextBlocks, Has.Length.EqualTo(2));
            Assert.That(deletion.IsPoint, Is.True);
            Assert.That(deletion.IsBoundaryResolved, Is.True);
            Assert.That(format.Kind,
                Is.EqualTo(OdfChangeKind.FormatChange));
            Assert.That(format.ChangedText, Is.EqualTo("styled"));
            Assert.That(unknown.Kind, Is.EqualTo(OdfChangeKind.Unknown));
            Assert.That(unknown.OperationName, Is.Not.Null);
            Assert.That(unknown.OperationName!.NamespaceName,
                Is.EqualTo("urn:change-extension"));
            Assert.That(unknown.OperationName.LocalName,
                Is.EqualTo("replacement"));
        });
    }

    [Test]
    public void SynchronizesMetadataDeletionContentIdentityAndRemoval()
    {
        OdfDocument document = ReadFlat(CompleteChangesXml());
        OdfChange insertion = document.Changes[0];
        OdfChange deletion = document.Changes[1];
        insertion.Id = "insert-renamed";
        insertion.Creator = "Grace";
        insertion.Date = "2026-09-03";
        insertion.Comment = "Reviewed";
        deletion.DeletedText = "replacement deletion";
        document.TrackTextChanges = false;
        document.Changes = [insertion, deletion];

        byte[] bytes = Write(document);
        string xml = Encoding.UTF8.GetString(bytes);
        OdfDocument reread = ReadFlat(xml);

        Assert.Multiple(() =>
        {
            Assert.That(reread.TrackTextChanges, Is.False);
            Assert.That(reread.Changes, Has.Length.EqualTo(2));
            Assert.That(reread.Changes[0].Id,
                Is.EqualTo("insert-renamed"));
            Assert.That(reread.Changes[0].XmlId,
                Is.EqualTo("insert-renamed"));
            Assert.That(reread.Changes[0].Creator, Is.EqualTo("Grace"));
            Assert.That(reread.Changes[0].Date,
                Is.EqualTo("2026-09-03"));
            Assert.That(reread.Changes[0].Comment,
                Is.EqualTo("Reviewed"));
            Assert.That(reread.Changes[0].IsBoundaryResolved, Is.True);
            Assert.That(reread.Changes[0].ChangedText,
                Is.EqualTo("new words"));
            Assert.That(reread.Changes[1].DeletedText,
                Is.EqualTo("replacement deletion"));
            Assert.That(reread.Changes[1].DeletedTextBlocks,
                Has.Length.EqualTo(1));
            Assert.That(xml, Does.Contain(
                "text:change-id=\"insert-renamed\""));
            Assert.That(xml, Does.Not.Contain("format"));
            Assert.That(xml, Does.Not.Contain("unknown"));
        });
    }

    [Test]
    public void ReportsMissingReversedDuplicateAndMixedBoundaries()
    {
        OdfDocument document = ReadFlat(BrokenBoundariesXml());
        OdfChange missing = document.Changes[0];
        OdfChange reversed = document.Changes[1];
        OdfChange duplicate = document.Changes[2];
        OdfChange mixed = document.Changes[3];

        Assert.Multiple(() =>
        {
            Assert.That(missing.IsRange, Is.True);
            Assert.That(missing.IsBoundaryResolved, Is.False);
            Assert.That(missing.HasDuplicateBoundary, Is.False);
            Assert.That(reversed.IsReversed, Is.True);
            Assert.That(reversed.IsBoundaryResolved, Is.False);
            Assert.That(duplicate.HasDuplicateBoundary, Is.True);
            Assert.That(duplicate.IsBoundaryResolved, Is.False);
            Assert.That(mixed.IsPoint, Is.True);
            Assert.That(mixed.IsRange, Is.True);
            Assert.That(mixed.HasDuplicateBoundary, Is.True);
        });
    }

    [Test]
    public void CreatesNewDeletionDefinitionAndTrackingSetting()
    {
        OdfDocument document = ReadFlat(
            DocumentXml("<text:p>Visible</text:p>"));
        OdfChange change = new OdfChange();
        change.Id = "created-change";
        change.Kind = OdfChangeKind.Deletion;
        change.Creator = "Ada";
        change.Date = "2026-09-04T08:30:00Z";
        change.Comment = "Created by model";
        change.DeletedText = "removed text";
        document.Changes = [change];
        document.TrackTextChanges = true;

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));

        Assert.Multiple(() =>
        {
            Assert.That(reread.TrackTextChanges, Is.True);
            Assert.That(reread.Changes, Has.Length.EqualTo(1));
            Assert.That(reread.Changes[0].Id,
                Is.EqualTo("created-change"));
            Assert.That(reread.Changes[0].XmlId,
                Is.EqualTo("created-change"));
            Assert.That(reread.Changes[0].Kind,
                Is.EqualTo(OdfChangeKind.Deletion));
            Assert.That(reread.Changes[0].Creator, Is.EqualTo("Ada"));
            Assert.That(reread.Changes[0].Comment,
                Is.EqualTo("Created by model"));
            Assert.That(reread.Changes[0].DeletedText,
                Is.EqualTo("removed text"));
            Assert.That(reread.Changes[0].IsBoundaryResolved, Is.False);
        });
    }

    [Test]
    public void PreservesUnknownRegionWithoutAnOperationElement()
    {
        string body = "<text:tracked-changes>"
            + "<text:changed-region/>"
            + "</text:tracked-changes><text:p>Visible</text:p>";
        OdfDocument document = ReadFlat(DocumentXml(body));

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));

        Assert.Multiple(() =>
        {
            Assert.That(reread.Changes, Has.Length.EqualTo(1));
            Assert.That(reread.Changes[0].Kind,
                Is.EqualTo(OdfChangeKind.Unknown));
            Assert.That(reread.Changes[0].OperationNode, Is.Null);
            Assert.That(reread.Changes[0].Id, Is.Empty);
        });
    }

    static string CompleteChangesXml()
    {
        string info = "<office:change-info>"
            + "<dc:creator>Alice</dc:creator>"
            + "<dc:date>2026-09-01T12:00:00Z</dc:date>"
            + "<text:p>First</text:p><text:p>Second</text:p>"
            + "</office:change-info>";
        string changes = "<text:tracked-changes text:track-changes='true'>"
            + "<text:changed-region xml:id='insert'>"
            + "<text:insertion>" + info + "</text:insertion>"
            + "</text:changed-region>"
            + "<text:changed-region xml:id='delete-xml' text:id='delete'>"
            + "<text:deletion>" + info
            + "<text:p>removed</text:p><text:p>content</text:p>"
            + "</text:deletion></text:changed-region>"
            + "<text:changed-region xml:id='format'>"
            + "<text:format-change>" + info + "</text:format-change>"
            + "</text:changed-region>"
            + "<text:changed-region xml:id='unknown'>"
            + "<ext:replacement>" + info + "</ext:replacement>"
            + "</text:changed-region></text:tracked-changes>";
        string body = changes
            + "<text:p>Before <text:change-start text:change-id='insert'/>"
            + "new <text:span>words</text:span>"
            + "<text:change-end text:change-id='insert'/> after "
            + "<text:change text:change-id='delete'/></text:p>"
            + "<text:p><text:change-start text:change-id='format'/>"
            + "styled<text:change-end text:change-id='format'/></text:p>";
        return DocumentXml(body);
    }

    static string BrokenBoundariesXml()
    {
        string info = "<office:change-info><dc:creator>A</dc:creator>"
            + "<dc:date>2026-09-01</dc:date></office:change-info>";
        string changes = "<text:tracked-changes>"
            + Region("missing", info)
            + Region("reversed", info)
            + Region("duplicate", info)
            + Region("mixed", info)
            + "</text:tracked-changes>";
        string body = changes
            + "<text:p><text:change-start text:change-id='missing'/>"
            + "<text:change-end text:change-id='reversed'/>"
            + "x<text:change-start text:change-id='reversed'/>"
            + "<text:change-start text:change-id='duplicate'/>"
            + "<text:change-start text:change-id='duplicate'/>"
            + "x<text:change-end text:change-id='duplicate'/>"
            + "<text:change text:change-id='mixed'/>"
            + "<text:change-start text:change-id='mixed'/>"
            + "x<text:change-end text:change-id='mixed'/></text:p>";
        return DocumentXml(body);
    }

    static string Region(string id, string info)
    {
        return "<text:changed-region xml:id='" + id + "'>"
            + "<text:insertion>" + info + "</text:insertion>"
            + "</text:changed-region>";
    }

    static string DocumentXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' xmlns:ext='urn:change-extension'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "changes.fodt",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }
}
