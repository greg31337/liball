using System.Text;

namespace odf.Tests;

public class ChangeProcessingModelTests
{
    [Test]
    public void ProjectsCurrentOriginalAndCombinedVisibleText()
    {
        OdfDocument document = ReadText(TextChangesXml());

        string current = ChangeProcessingModel.ProjectVisibleText(
            document, true, false);
        string original = ChangeProcessingModel.ProjectVisibleText(
            document, false, true);
        string combined = ChangeProcessingModel.ProjectVisibleText(
            document, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(current, Is.EqualTo("Before new words after \nstyled"));
            Assert.That(original,
                Is.EqualTo("Before  after removed\ncontent\nstyled"));
            Assert.That(combined,
                Is.EqualTo("Before new words after removed\ncontent\nstyled"));
        });
    }

    [Test]
    public void FiltersTextAndSpreadsheetChangesByMetadata()
    {
        OdfDocument text = ReadText(TextChangesXml());
        OdfDocument spreadsheet = ReadSpreadsheet(SpreadsheetChangesXml());

        OdfChange[] textMatches = ChangeProcessingModel.FilterTextChanges(
            text, "bob", "2026-09-02", "REMOVE", false);
        OdfSpreadsheetChange[] sheetMatches =
            ChangeProcessingModel.FilterSpreadsheetChanges(
                spreadsheet, "alice", "2026-09", "value", false);

        Assert.Multiple(() =>
        {
            Assert.That(textMatches, Has.Length.EqualTo(1));
            Assert.That(textMatches[0].Kind,
                Is.EqualTo(OdfChangeKind.Deletion));
            Assert.That(sheetMatches, Has.Length.EqualTo(1));
            Assert.That(sheetMatches[0].Id, Is.EqualTo("cell-change"));
        });
    }

    [Test]
    public void AcceptsOneAndSelectedTextChangesWithoutChangingContent()
    {
        OdfDocument document = ReadText(TextChangesXml());
        OdfChange insertion = document.Changes[0];
        OdfChange format = document.Changes[2];

        bool accepted = ChangeProcessingModel.AcceptTextChange(
            document, insertion);
        OdfChange refreshedFormat = document.Changes.Single(
            item => item.Id == format.Id);
        int selected = ChangeProcessingModel.AcceptSelectedTextChanges(
            document, [refreshedFormat]);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(selected, Is.EqualTo(1));
            Assert.That(document.Changes, Has.Length.EqualTo(1));
            Assert.That(document.Changes[0].Kind,
                Is.EqualTo(OdfChangeKind.Deletion));
            Assert.That(ChangeProcessingModel.ProjectVisibleText(
                document, true, false),
                Is.EqualTo("Before new words after \nstyled"));
        });
    }

    [Test]
    public void RejectsAllTextChangesAndRestoresDeletedBlocks()
    {
        OdfDocument document = ReadText(TextChangesXml());

        int rejected = ChangeProcessingModel.RejectAllTextChanges(document);
        string xml = Encoding.UTF8.GetString(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(rejected, Is.EqualTo(3));
            Assert.That(document.Changes, Is.Empty);
            Assert.That(ChangeProcessingModel.ProjectVisibleText(
                document, true, false),
                Is.EqualTo("Before  after removed\ncontent\nstyled"));
            Assert.That(xml, Does.Not.Contain("text:change-start"));
            Assert.That(xml, Does.Not.Contain("text:change-end"));
            Assert.That(xml, Does.Not.Contain("text:change-id"));
        });
    }

    [Test]
    public void LeavesAnUnresolvedChangeUntouched()
    {
        string body = "<text:tracked-changes><text:changed-region xml:id='x'>"
            + "<text:insertion/></text:changed-region></text:tracked-changes>"
            + "<text:p><text:change-start text:change-id='x'/>x</text:p>";
        OdfDocument document = ReadText(TextDocumentXml(body));

        bool result = ChangeProcessingModel.RejectTextChange(
            document, document.Changes[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(document.Changes, Has.Length.EqualTo(1));
            Assert.That(document.Changes[0].IsBoundaryResolved, Is.False);
        });
    }

    [Test]
    public void MaterializesAndRoundTripsSpreadsheetChangeStructures()
    {
        OdfDocument document = ReadSpreadsheet(SpreadsheetChangesXml());
        OdfSpreadsheetChange cell = document.SpreadsheetChanges[0];
        OdfSpreadsheetChange deletion = document.SpreadsheetChanges[1];
        cell.PreviousCell!.Text = "older";
        deletion.CutOffs[1].EndPosition = 9;
        ChangeProcessingModel.AcceptSpreadsheetChange(document, cell);
        ChangeProcessingModel.RejectSpreadsheetChange(
            document, deletion, "review-1");

        OdfDocument reread = ReadSpreadsheet(
            Encoding.UTF8.GetString(Write(document)));
        OdfSpreadsheetChange rereadCell = reread.SpreadsheetChanges[0];
        OdfSpreadsheetChange rereadDeletion = reread.SpreadsheetChanges[1];

        Assert.Multiple(() =>
        {
            Assert.That(cell.Dependencies, Has.Length.EqualTo(1));
            Assert.That(cell.Dependencies[0].Id, Is.EqualTo("dependency-1"));
            Assert.That(cell.Deletions, Has.Length.EqualTo(1));
            Assert.That(cell.Deletions[0].HasCellAddress, Is.True);
            Assert.That(cell.Deletions[0].Cell!.Value, Is.EqualTo("new"));
            Assert.That(cell.PreviousId, Is.EqualTo("prior-1"));
            Assert.That(cell.PreviousCell!.ValueType, Is.EqualTo("string"));
            Assert.That(rereadCell.AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(rereadCell.PreviousCell!.Text, Is.EqualTo("older"));
            Assert.That(rereadDeletion.AcceptanceState,
                Is.EqualTo("rejected"));
            Assert.That(rereadDeletion.RejectingChangeId,
                Is.EqualTo("review-1"));
            Assert.That(rereadDeletion.CutOffs, Has.Length.EqualTo(2));
            Assert.That(rereadDeletion.CutOffs[0].Kind,
                Is.EqualTo("insertion"));
            Assert.That(rereadDeletion.CutOffs[1].HasRange, Is.True);
            Assert.That(rereadDeletion.CutOffs[1].EndPosition, Is.EqualTo(9));
        });
    }

    [Test]
    public void AcceptsAndRejectsSelectedAndAllSpreadsheetChanges()
    {
        OdfDocument document = ReadSpreadsheet(SpreadsheetChangesXml());

        int selected = ChangeProcessingModel.AcceptSelectedSpreadsheetChanges(
            document, [document.SpreadsheetChanges[0]]);
        int all = ChangeProcessingModel.RejectAllSpreadsheetChanges(
            document, "review-all");

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.EqualTo(1));
            Assert.That(all, Is.EqualTo(2));
            Assert.That(document.SpreadsheetChanges[0].AcceptanceState,
                Is.EqualTo("rejected"));
            Assert.That(document.SpreadsheetChanges[1].RejectingChangeId,
                Is.EqualTo("review-all"));
        });
    }

    static string TextChangesXml()
    {
        string alice = ChangeInfo(
            "Alice", "2026-09-01T12:00:00Z", "Inserted");
        string bob = ChangeInfo(
            "Bob", "2026-09-02T12:00:00Z", "Remove this");
        string changes = "<text:tracked-changes>"
            + Region("insert", "insertion", alice, "")
            + Region(
                "delete",
                "deletion",
                bob,
                "<text:p>removed</text:p><text:p>content</text:p>")
            + Region("format", "format-change", alice, "")
            + "</text:tracked-changes>";
        string body = changes
            + "<text:p>Before <text:change-start text:change-id='insert'/>"
            + "new <text:span>words</text:span>"
            + "<text:change-end text:change-id='insert'/> after "
            + "<text:change text:change-id='delete'/></text:p>"
            + "<text:p><text:change-start text:change-id='format'/>"
            + "styled<text:change-end text:change-id='format'/></text:p>";
        return TextDocumentXml(body);
    }

    static string Region(
        string id,
        string kind,
        string info,
        string content)
    {
        return "<text:changed-region xml:id='" + id + "'><text:"
            + kind + ">" + info + content + "</text:" + kind
            + "></text:changed-region>";
    }

    static string ChangeInfo(
        string creator,
        string date,
        string comment)
    {
        return "<office:change-info><dc:creator>" + creator
            + "</dc:creator><dc:date>" + date + "</dc:date><text:p>"
            + comment + "</text:p></office:change-info>";
    }

    static string TextDocumentXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + body + "</office:text></office:body></office:document>";
    }

    static string SpreadsheetChangesXml()
    {
        string info = ChangeInfo(
            "Alice", "2026-09-01T12:00:00Z", "Changed value");
        string deletionInfo = ChangeInfo(
            "Bob", "2026-09-02T12:00:00Z", "Deleted row");
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:table='" + OdfNamespaces.Table
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:dc='" + OdfNamespaces.DublinCore
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Spreadsheet + "'><office:body>"
            + "<office:spreadsheet><table:tracked-changes>"
            + "<table:cell-content-change table:id='cell-change'>"
            + "<table:cell-address table:column='1' table:row='2' table:table='0'/>"
            + info
            + "<table:dependencies><table:dependency table:id='dependency-1'/>"
            + "</table:dependencies><table:deletions>"
            + "<table:cell-content-deletion table:id='deleted-1'>"
            + "<table:cell-address table:column='1' table:row='2' table:table='0'/>"
            + "<table:change-track-table-cell office:value-type='string' "
            + "office:string-value='new'><text:p>new</text:p>"
            + "</table:change-track-table-cell></table:cell-content-deletion>"
            + "</table:deletions><table:previous table:id='prior-1'>"
            + "<table:change-track-table-cell office:value-type='string' "
            + "office:string-value='old'><text:p>old</text:p>"
            + "</table:change-track-table-cell></table:previous>"
            + "</table:cell-content-change>"
            + "<table:deletion table:id='delete-change' table:type='row' "
            + "table:position='3'>" + deletionInfo + "<table:cut-offs>"
            + "<table:insertion-cut-off table:id='insert-1' table:position='2'/>"
            + "<table:movement-cut-off table:start-position='4' "
            + "table:end-position='7'/></table:cut-offs></table:deletion>"
            + "</table:tracked-changes><table:table table:name='Sheet1'/>"
            + "</office:spreadsheet></office:body></office:document>";
    }

    static OdfDocument ReadText(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "changes.fodt",
            new OdfLoadOptions());
    }

    static OdfDocument ReadSpreadsheet(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "changes.fods",
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
