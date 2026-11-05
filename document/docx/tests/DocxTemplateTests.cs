using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTemplateTests
{
    [Test]
    public void ReplacePlaceholders_ReplacesDocumentBodyTablesAndContentControlsAndUpdatesPackage()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:p><w:r><w:t>Hello {{Na</w:t></w:r><w:r><w:t>me}}</w:t></w:r></w:p>",
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("{{City}}") + "</w:tc></w:tr></w:tbl>",
            CreateContentControlXml("{{Role}}")
        }));
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("Name", "Ada");
        values.Add("City", "Paris");
        values.Add("Role", "Engineer");

        int count = document.ReplacePlaceholders(values);

        Assert.That(count, Is.EqualTo(3));
        Assert.That(document.GetText(), Is.EqualTo("Hello Ada\nParis\nEngineer"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Hello Ada"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Paris"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Engineer"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{Name}}"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{City}}"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{Role}}"));
    }

    [Test]
    public void ReplacePlaceholders_UsesCustomDelimitersAndLeavesMissingValues()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[[One]] {{Two}}</w:t></w:r></w:p>");
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("One", "1");

        int count = paragraph.ReplacePlaceholdersWithDelimiters(values, "[[", "]]");

        Assert.That(count, Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("1 {{Two}}"));
        Assert.That(global::docx.DocxTemplate.CreatePlaceholder("One", "[[", "]]"), Is.EqualTo("[[One]]"));
    }

    [Test]
    public void ReplacePlaceholders_ReturnsZeroWhenNoPlaceholderMatches()
    {
        global::docx.DocxBody body = new global::docx.DocxBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateParagraphXml("No tokens") + "</w:body>");
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("Name", "Ada");

        Assert.That(body.ReplacePlaceholders(values), Is.EqualTo(0));
        Assert.That(body.GetText(), Is.EqualTo("No tokens"));
    }

    [Test]
    public void ReplacePlaceholders_ReplacesNestedTablePlaceholdersAndUpdatesPackage()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("Outer {{Outer}}") + "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("Nested {{Nested}}") + "</w:tc></w:tr></w:tbl></w:tc></w:tr></w:tbl>"
        }));
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("Outer", "A");
        values.Add("Nested", "B");

        int count = document.ReplacePlaceholders(values);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(document.GetText(), Is.EqualTo("Outer A\nNested B"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Outer A"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Nested B"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{Nested}}"));
    }

    [Test]
    public void RepeatTableRow_ReplacesTemplateRowWithFilledRows()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc>" + CreateParagraphXml("Item") + "</w:tc><w:tc>" + CreateParagraphXml("Qty") + "</w:tc></w:tr><w:tr><w:trPr><w:tblHeader w:val=\"0\" /></w:trPr><w:tc>" + CreateParagraphXml("{{Item}}") + "</w:tc><w:tc>" + CreateParagraphXml("{{Qty}}") + "</w:tc></w:tr></w:tbl>");
        global::docx.DocxTableData data = new global::docx.DocxTableData();

        data.AddRowValues(CreateRow("Item", "Apples", "Qty", "2"));
        data.AddRowValues(CreateRow("Item", "Pears", "Qty", "4"));

        int count = table.RepeatRow(1, data);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(table.RowCount(), Is.EqualTo(3));
        Assert.That(table.GetText(), Is.EqualTo("Item\tQty\nApples\t2\nPears\t4"));
        Assert.That(table.GetRow(1).HasTableHeader, Is.True);
        Assert.That(table.GetRow(2).HasTableHeader, Is.True);
        Assert.That(table.WriteXml(), Does.Contain("Apples"));
        Assert.That(table.WriteXml(), Does.Contain("Pears"));
        Assert.That(table.WriteXml(), Does.Not.Contain("{{Item}}"));
        Assert.That(table.WriteXml(), Does.Not.Contain("{{Qty}}"));
    }

    [Test]
    public void RepeatTableRow_UsesCustomDelimitersAndRemovesTemplateForEmptyData()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc>" + CreateParagraphXml("[[Name]]") + "</w:tc></w:tr></w:tbl>");
        global::docx.DocxTableData data = new global::docx.DocxTableData();

        data.AddRowValues(CreateRow("Name", "Ada", "Extra", "Ignored"));

        Assert.That(global::docx.DocxTemplate.RepeatTableRow(table, 0, data, "[[", "]]"), Is.EqualTo(1));
        Assert.That(table.RowCount(), Is.EqualTo(1));
        Assert.That(table.GetText(), Is.EqualTo("Ada"));

        Assert.That(global::docx.DocxTemplate.RepeatTableRow(table, 0, new global::docx.DocxTableData()), Is.EqualTo(0));
        Assert.That(table.RowCount(), Is.EqualTo(0));
        Assert.That(table.GetText(), Is.EqualTo(""));
    }

    [Test]
    public void TableData_ClonesInputRows()
    {
        Dictionary<string, string> source = CreateRow("Name", "Ada", "Role", "Engineer");
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        rows.Add(source);

        global::docx.DocxTableData data = global::docx.DocxTableData.Create(rows);
        source["Name"] = "Changed";

        Assert.That(data.RowCount(), Is.EqualTo(1));
        Assert.That(data.GetRow(0)["Name"], Is.EqualTo("Ada"));
        Assert.That(data.AddRow().Count, Is.EqualTo(0));
        Assert.That(data.RowCount(), Is.EqualTo(2));
    }

    [Test]
    public void RemoveBlock_RemovesMarkedBodyBlocksAndUpdatesPackage()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Keep"),
            CreateParagraphXml("Remove {{Details}}"),
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("Table {{Table}}") + "</w:tc></w:tr></w:tbl>",
            CreateParagraphXml("Last")
        }));

        Assert.That(document.RemoveBlock("Details"), Is.EqualTo(1));
        Assert.That(document.GetText(), Is.EqualTo("Keep\nTable {{Table}}\nLast"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{Details}}"));

        Assert.That(document.RemoveBlock("Table"), Is.EqualTo(1));
        Assert.That(document.GetText(), Is.EqualTo("Keep\nLast"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{Table}}"));
    }

    [Test]
    public void RemoveBlock_UsesCustomDelimitersAndReturnsZeroWhenMissing()
    {
        global::docx.DocxBody body = new global::docx.DocxBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateParagraphXml("Keep") + CreateParagraphXml("Remove [[Flag]]") + "</w:body>");

        Assert.That(body.RemoveBlockWithDelimiters("Missing", "[[", "]]"), Is.EqualTo(0));
        Assert.That(body.GetText(), Is.EqualTo("Keep\nRemove [[Flag]]"));
        Assert.That(global::docx.DocxTemplate.RemoveBlock(body, "Flag", "[[", "]]"), Is.EqualTo(1));
        Assert.That(body.GetText(), Is.EqualTo("Keep"));
    }

    [Test]
    public void PublicTemplateManagement_DiscoversAndReplacesDocumentPlaceholders()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:p><w:r><w:t>{{Na</w:t></w:r><w:r><w:t>me}} from {{City}}</w:t></w:r></w:p>",
            CreateParagraphXml("Again {{Name}}"),
            CreateParagraphXml("{{#Rows}}"),
            CreateParagraphXml("{{/Rows}}")
        }));
        global::docx.DocxTemplateManagement management = new global::docx.DocxTemplateManagement();

        List<global::docx.DocxTemplatePlaceholder> placeholders = management.GetDocumentPlaceholders(
            document,
            global::docx.DocxConstants.DefaultTemplatePlaceholderPrefix,
            global::docx.DocxConstants.DefaultTemplatePlaceholderSuffix);

        Assert.That(placeholders, Has.Count.EqualTo(2));
        Assert.That(placeholders[0].Key, Is.EqualTo("Name"));
        Assert.That(placeholders[0].Token, Is.EqualTo("{{Name}}"));
        Assert.That(placeholders[0].Count, Is.EqualTo(2));
        Assert.That(placeholders[1].Key, Is.EqualTo("City"));

        Dictionary<string, string> values = new Dictionary<string, string>();
        values.Add("Name", "Ada");
        values.Add("City", "London");

        Assert.That(management.ReplaceDocumentPlaceholders(document, values, "{{", "}}"), Is.EqualTo(3));
        Assert.That(document.GetText(), Does.StartWith("Ada from London\nAgain Ada"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain(">Ada<"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain(" from London"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{Name}}"));
    }

    [Test]
    public void PublicTemplateManagement_ExpandsRepeatingAndConditionalBodyBlocks()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Before"),
            CreateParagraphXml("{{#People}}"),
            CreateParagraphXml("Name: {{Name}}"),
            CreateParagraphXml("Role: {{Role}}"),
            CreateParagraphXml("{{/People}}"),
            CreateParagraphXml("{{?Details}}"),
            CreateParagraphXml("Private details"),
            CreateParagraphXml("{{/Details}}"),
            CreateParagraphXml("After")
        }));
        global::docx.DocxTemplateManagement management = new global::docx.DocxTemplateManagement();
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        rows.Add(CreateRow("Name", "Ada", "Role", "Engineer"));
        rows.Add(CreateRow("Name", "Grace", "Role", "Admiral"));

        Assert.That(management.ExpandRepeatingBlock(document, "People", rows, "{{", "}}"), Is.EqualTo(2));
        Assert.That(management.ApplyConditionalBlock(document, "Details", false, "{{", "}}"), Is.EqualTo(1));
        Assert.That(document.GetText(), Is.EqualTo("Before\nName: Ada\nRole: Engineer\nName: Grace\nRole: Admiral\nAfter"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("{{#People}}"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("Private details"));

        global::docx.DocxDocument included = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("{{?Details}}"),
            CreateParagraphXml("Visible"),
            CreateParagraphXml("{{/Details}}")
        }));

        Assert.That(management.ApplyConditionalBlock(included, "Details", true, "{{", "}}"), Is.EqualTo(1));
        Assert.That(included.GetText(), Is.EqualTo("Visible"));
    }

    [Test]
    public void PublicTemplateManagement_ExpandsTableRowsAndMergesContentControls()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("{{Item}}") + "</w:tc><w:tc>" + CreateParagraphXml("{{Quantity}}") + "</w:tc></w:tr></w:tbl>",
            "<w:sdt><w:sdtPr><w:tag w:val=\"Customer\" /><w:text /></w:sdtPr><w:sdtContent>" + CreateParagraphXml("Original") + "</w:sdtContent></w:sdt>"
        }));
        global::docx.DocxTemplateManagement management = new global::docx.DocxTemplateManagement();
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        rows.Add(CreateRow("Item", "Apples", "Quantity", "2"));
        rows.Add(CreateRow("Item", "Pears", "Quantity", "4"));

        Assert.That(management.ExpandTableRow(document, 0, 0, rows, "{{", "}}"), Is.EqualTo(2));
        Dictionary<string, string> values = new Dictionary<string, string>();
        values.Add("Customer", "Alice");
        Assert.That(management.MergeContentControls(document, values), Is.EqualTo(1));
        Assert.That(document.GetText(), Is.EqualTo("Apples\t2\nPears\t4\nAlice"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Pears"));
        Assert.That(document.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Alice"));
    }

    [Test]
    public void PublicTemplateManagement_InsertsDocumentAtBookmark()
    {
        global::docx.DocxDocument target = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            "<w:p><w:bookmarkStart w:id=\"1\" w:name=\"InsertHere\" /><w:r><w:t>Anchor</w:t></w:r><w:bookmarkEnd w:id=\"1\" /></w:p>",
            CreateParagraphXml("After")
        }));
        global::docx.DocxDocument source = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("First inserted"),
            CreateParagraphXml("Second inserted")
        }));
        global::docx.DocxTemplateManagement management = new global::docx.DocxTemplateManagement();

        Assert.That(management.InsertDocumentAtBookmark(target, "InsertHere", source), Is.EqualTo(2));
        Assert.That(target.GetText(), Is.EqualTo("Anchor\nFirst inserted\nSecond inserted\nAfter"));
        Assert.That(target.Package.GetMainDocumentPart()!.GetText(), Does.Contain("Second inserted"));
    }

    [Test]
    public void ReplacePlaceholders_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxBody body = global::docx.DocxBody.Create();
        global::docx.DocxBlock block = global::docx.DocxBlock.FromParagraph(new global::docx.DocxParagraph(""));
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableRow row = global::docx.DocxTableRow.Create();
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();
        global::docx.DocxContentControl contentControl = new global::docx.DocxContentControl("");
        global::docx.DocxTableData data = new global::docx.DocxTableData();
        Dictionary<string, string> values = new Dictionary<string, string>();
        List<global::docx.DocxXmlBuffer> children = new List<global::docx.DocxXmlBuffer>();

        values.Add("Name", "Ada");
        table.AddRowWithCells(1);

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxDocument)null!, values));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxBody)null!, values));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxBlock)null!, values, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxParagraph)null!, values));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(paragraph, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxRun)null!, values, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(run, null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxTable)null!, values, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(table, null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxTableRow)null!, values, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(row, null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxTableCell)null!, values, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(cell, null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders((global::docx.DocxContentControl)null!, values, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplacePlaceholders(contentControl, null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => document.ReplacePlaceholders(null!));
        Assert.Throws<ArgumentNullException>(() => body.ReplacePlaceholders(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplacePlaceholders(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.CreatePlaceholder(null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.CreatePlaceholder("Name", null!, "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.CreatePlaceholder("Name", "{{", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTemplate.CreatePlaceholder("", "{{", "}}"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTemplate.CreatePlaceholder("Name", "", "}}"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTemplate.CreatePlaceholder("Name", "{{", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidatePlaceholderArguments(null!, "{{", "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidatePlaceholderArguments(values, null!, "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidatePlaceholderArguments(values, "{{", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidatePlaceholderValue(null!, "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidatePlaceholderValue("key", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTemplate.ValidatePlaceholderValue("", "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidateDelimiter(null!, "{{"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidateDelimiter("prefix", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTemplate.ValidateDelimiter("prefix", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.AddReplacedChild(null!, new global::docx.DocxXmlBuffer("<root />"), "<root />", 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.AddReplacedChild(children, null!, "<root />", 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.AddReplacedChild(children, new global::docx.DocxXmlBuffer("<root />"), null!, 1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.UpdateMainDocumentBody(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplaceBodyXml(null!, "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ReplaceBodyXml("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableData.Create(null!));
        Assert.Throws<ArgumentNullException>(() => data.AddRowValues(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableData.CloneRow(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RepeatTableRow(null!, 0, data));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RepeatTableRow(table, 0, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RepeatTableRow(table, 0, data, null!, "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RepeatTableRow(table, 0, data, "{{", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTemplate.RepeatTableRow(table, -1, data));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTemplate.RepeatTableRow(table, 1, data));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.ValidateTableRowIndex(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTemplate.ValidateTableRowIndex(table, 1));
        Assert.Throws<ArgumentNullException>(() => table.RepeatRow(0, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RemoveBlock((global::docx.DocxDocument)null!, "key"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RemoveBlock(document, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RemoveBlock((global::docx.DocxBody)null!, "key"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RemoveBlock(body, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RemoveBlock(body, "key", null!, "}}"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.RemoveBlock(body, "key", "{{", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.BlockContainsText(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTemplate.BlockContainsText(block, null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTemplate.BlockContainsText(block, ""));
        Assert.Throws<ArgumentNullException>(() => document.RemoveBlock(null!));
        Assert.Throws<ArgumentNullException>(() => body.RemoveBlock(null!));
    }

    static global::docx.DocxPackage CreatePackageWithBodyChildren(string[] children)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentXml(children));
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        return package;
    }

    static string CreateDocumentXml(string[] bodyChildren)
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + string.Join("", bodyChildren) + "</w:body></w:document>";
    }

    static string CreateParagraphXml(string text)
    {
        return "<w:p><w:r><w:t>" + text + "</w:t></w:r></w:p>";
    }

    static string CreateContentControlXml(string text)
    {
        return "<w:sdt><w:sdtContent>" + CreateParagraphXml(text) + "</w:sdtContent></w:sdt>";
    }

    static Dictionary<string, string> CreateRow(string firstKey, string firstValue, string secondKey, string secondValue)
    {
        Dictionary<string, string> row = new Dictionary<string, string>();

        row.Add(firstKey, firstValue);
        row.Add(secondKey, secondValue);

        return row;
    }

    static global::docx.DocxTable ParseTable(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tbl", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTable.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}
