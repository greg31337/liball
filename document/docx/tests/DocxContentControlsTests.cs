using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxContentControlsTests
{
    [Test]
    public void Parse_CapturesPropertiesContentAndTypedChildren()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl(CreateBlockContentControlXml());

        Assert.That(contentControl.HasXml(), Is.True);
        Assert.That(contentControl.HasProperties(), Is.True);
        Assert.That(contentControl.GetPropertiesXml(), Does.Contain("sdtPr"));
        Assert.That(contentControl.HasContent(), Is.True);
        Assert.That(contentControl.GetContentXml(), Does.Contain("sdtContent"));
        Assert.That(contentControl.ContentChildCount(), Is.EqualTo(2));
        Assert.That(contentControl.BlockCount(), Is.EqualTo(2));
        Assert.That(contentControl.ParagraphCount(), Is.EqualTo(1));
        Assert.That(contentControl.TableCount(), Is.EqualTo(1));
        Assert.That(contentControl.RunCount(), Is.EqualTo(0));
        Assert.That(contentControl.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(contentControl.GetBlock(1).IsTable(), Is.True);
        Assert.That(contentControl.GetText(), Is.EqualTo("InsideCell"));
        Assert.That(contentControl.GetValue(), Is.EqualTo("InsideCell"));
        Assert.That(contentControl.HasValue(), Is.True);
        Assert.That(contentControl.WriteXml(), Is.EqualTo(CreateBlockContentControlXml()));
    }

    [Test]
    public void Parse_CapturesRunLevelContentControlChildren()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl(CreateRunContentControlXml("Inline"));

        Assert.That(contentControl.ContentChildCount(), Is.EqualTo(1));
        Assert.That(contentControl.BlockCount(), Is.EqualTo(0));
        Assert.That(contentControl.RunCount(), Is.EqualTo(1));
        Assert.That(contentControl.GetRun(0).GetText(), Is.EqualTo("Inline"));
        Assert.That(contentControl.GetText(), Is.EqualTo("Inline"));
        Assert.That(contentControl.GetValue(), Is.EqualTo("Inline"));
    }

    [Test]
    public void Parse_ReadsAliasTagTypeAndDataBindingFromProperties()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl(CreateBoundContentControlXml("Customer"));

        Assert.That(contentControl.HasAlias(), Is.True);
        Assert.That(contentControl.Alias, Is.EqualTo("Customer Name"));
        Assert.That(contentControl.HasTag(), Is.True);
        Assert.That(contentControl.Tag, Is.EqualTo("customer.name"));
        Assert.That(contentControl.HasType(), Is.True);
        Assert.That(contentControl.Type, Is.EqualTo("text"));
        Assert.That(contentControl.HasDataBinding(), Is.True);
        Assert.That(contentControl.DataBindingStoreItemId, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(contentControl.DataBindingXPath, Is.EqualTo("/root/customer/name"));
        Assert.That(contentControl.DataBindingPrefixMappings, Is.EqualTo("xmlns:ns='urn:test'"));
        Assert.That(contentControl.GetText(), Is.EqualTo("Customer"));
        Assert.That(contentControl.GetValue(), Is.EqualTo("Customer"));
        Assert.That(contentControl.GetValueKey(), Is.EqualTo("customer.name"));
        Assert.That(contentControl.HasValueKey(), Is.True);
        Assert.That(contentControl.WriteXml(), Is.EqualTo(CreateBoundContentControlXml("Customer")));
    }

    [Test]
    public void GetValue_ExtractsVisibleContentFromRunBlockAndNestedControls()
    {
        global::docx.DocxContentControl runControl = ParseContentControl(CreateRunContentControlXml("Inline"));
        global::docx.DocxContentControl blockControl = ParseContentControl(CreateBlockContentControlXml());
        global::docx.DocxContentControl nestedControl = ParseContentControl("<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:alias w:val=\"Nested Alias\" /></w:sdtPr><w:sdtContent>" + CreateRunContentControlXml("Nested") + "</w:sdtContent></w:sdt>");
        global::docx.DocxContentControl emptyControl = new global::docx.DocxContentControl("");

        Assert.That(runControl.GetValue(), Is.EqualTo("Inline"));
        Assert.That(blockControl.GetValue(), Is.EqualTo("InsideCell"));
        Assert.That(nestedControl.GetValue(), Is.EqualTo("Nested"));
        Assert.That(nestedControl.GetValueKey(), Is.EqualTo("Nested Alias"));
        Assert.That(emptyControl.GetValue(), Is.EqualTo(""));
        Assert.That(emptyControl.HasValue(), Is.False);
        Assert.That(emptyControl.HasValueKey(), Is.False);
    }

    [Test]
    public void SetValue_ReplacesContentControlValueAndEscapesText()
    {
        global::docx.DocxContentControl runControl = ParseContentControl(CreateRunContentControlXml("Inline"));
        global::docx.DocxContentControl blockControl = ParseContentControl(CreateBlockContentControlXml());

        runControl.SetValue(" Updated & <value> ");
        blockControl.SetValue("Block Value");

        Assert.That(runControl.GetValue(), Is.EqualTo(" Updated & <value> "));
        Assert.That(runControl.RunCount(), Is.EqualTo(1));
        Assert.That(runControl.BlockCount(), Is.EqualTo(0));
        Assert.That(runControl.WriteXml(), Does.Contain("xml:space=\"preserve\""));
        Assert.That(runControl.WriteXml(), Does.Contain("&amp;"));
        Assert.That(runControl.WriteXml(), Does.Contain("&lt;value&gt;"));
        Assert.That(blockControl.GetValue(), Is.EqualTo("Block Value"));
        Assert.That(blockControl.BlockCount(), Is.EqualTo(1));
        Assert.That(blockControl.ParagraphCount(), Is.EqualTo(1));
    }

    [Test]
    public void FillContentControls_ReplacesParagraphControlsByTagAndAlias()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateBoundContentControlXml("Customer") + CreateAliasContentControlXml("City", "Original City") + "</w:p>");
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("customer.name", "Alice");
        values.Add("City", "Paris");

        int count = paragraph.FillContentControls(values);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(paragraph.ContentControlCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetContentControl(0).GetValue(), Is.EqualTo("Alice"));
        Assert.That(paragraph.GetContentControl(1).GetValue(), Is.EqualTo("Paris"));
        Assert.That(paragraph.WriteXml(), Does.Contain("Alice"));
        Assert.That(paragraph.WriteXml(), Does.Contain("Paris"));
        Assert.That(paragraph.WriteXml(), Does.Not.Contain("Original City"));
    }

    [Test]
    public void FillContentControls_UsesTagBeforeAliasWhenBothArePresent()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl(CreateBoundContentControlXml("Original"));
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("Customer Name", "Alias Value");

        Assert.That(contentControl.GetValueKey(), Is.EqualTo("customer.name"));
        Assert.That(contentControl.FillContentControls(values), Is.EqualTo(0));
        Assert.That(contentControl.GetValue(), Is.EqualTo("Original"));

        values.Add("customer.name", "Tag Value");

        Assert.That(contentControl.FillContentControls(values), Is.EqualTo(1));
        Assert.That(contentControl.GetValue(), Is.EqualTo("Tag Value"));
    }

    [Test]
    public void FillContentControls_RecursesThroughBodyBlocksAndTables()
    {
        global::docx.DocxBody body = ParseBody(CreateBodyXml(new string[]
        {
            CreateBoundContentControlXml("Original Customer"),
            "<w:tbl><w:tr><w:tc><w:p>" + CreateAliasContentControlXml("City", "Original City") + "</w:p></w:tc></w:tr></w:tbl>"
        }));
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("customer.name", "Alice");
        values.Add("City", "Paris");

        int count = body.FillContentControls(values);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(body.GetBlock(0).ContentControl!.GetValue(), Is.EqualTo("Alice"));
        Assert.That(body.GetBlock(1).Table!.GetRow(0).GetCell(0).GetParagraph(0).GetContentControl(0).GetValue(), Is.EqualTo("Paris"));
        Assert.That(body.WriteXml(), Does.Contain("Alice"));
        Assert.That(body.WriteXml(), Does.Contain("Paris"));
        Assert.That(body.WriteXml(), Does.Not.Contain("Original Customer"));
        Assert.That(body.WriteXml(), Does.Not.Contain("Original City"));
    }

    [Test]
    public void FillContentControls_RecursesThroughNestedContentControls()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl("<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:alias w:val=\"Outer\" /></w:sdtPr><w:sdtContent>" + CreateAliasContentControlXml("Inner", "Original Inner") + "</w:sdtContent></w:sdt>");
        Dictionary<string, string> values = new Dictionary<string, string>();

        values.Add("Inner", "Nested Value");

        int count = contentControl.FillContentControls(values);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(contentControl.GetContentControl(0).GetValue(), Is.EqualTo("Nested Value"));
        Assert.That(contentControl.GetValue(), Is.EqualTo("Nested Value"));
        Assert.That(contentControl.WriteXml(), Does.Not.Contain("Original Inner"));
    }

    [Test]
    public void Parse_ReadsFirstKnownContentControlTypeMarker()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl("<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:showingPlcHdr /><w:picture /><w:checkbox /></w:sdtPr><w:sdtContent><w:r><w:t>Picture</w:t></w:r></w:sdtContent></w:sdt>");
        global::docx.DocxXmlReader pictureReader = global::docx.DocxXmlReader.Create("<w:picture xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxXmlReader placeholderReader = global::docx.DocxXmlReader.Create("<w:showingPlcHdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.That(contentControl.Type, Is.EqualTo("picture"));
        Assert.That(pictureReader.MoveToContent(), Is.True);
        Assert.That(placeholderReader.MoveToContent(), Is.True);
        Assert.That(global::docx.DocxContentControl.IsTypeElement(pictureReader), Is.True);
        Assert.That(global::docx.DocxContentControl.IsTypeElement(placeholderReader), Is.False);
    }

    [Test]
    public void Body_ParseRecordsStructuredDocumentTagBlocksInSourceOrder()
    {
        global::docx.DocxBody body = ParseBody(CreateBodyXml(new string[]
        {
            "<w:p marker=\"first\" />",
            CreateBlockContentControlXml(),
            "<w:tbl marker=\"table\"><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl>"
        }));

        Assert.That(body.BlockCount(), Is.EqualTo(3));
        Assert.That(body.ParagraphCount(), Is.EqualTo(1));
        Assert.That(body.TableCount(), Is.EqualTo(1));
        Assert.That(body.ContentControlCount(), Is.EqualTo(1));
        Assert.That(body.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(body.GetBlock(1).IsContentControl(), Is.True);
        Assert.That(body.GetBlock(1).ContentControl, Is.Not.Null);
        Assert.That(body.GetBlock(1).ContentControl!.ParagraphCount(), Is.EqualTo(1));
        Assert.That(body.GetBlock(2).IsTable(), Is.True);
        Assert.That(body.WriteXml(), Does.Contain("w:sdt"));
    }

    [Test]
    public void Paragraph_ParseRecordsRunLevelStructuredDocumentTags()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Before</w:t></w:r>" + CreateRunContentControlXml("Inside") + "<w:r><w:t>After</w:t></w:r></w:p>");

        Assert.That(paragraph.ChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.RunCount(), Is.EqualTo(2));
        Assert.That(paragraph.ContentControlElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.ContentControlCount(), Is.EqualTo(1));
        Assert.That(paragraph.UnknownElementCount(), Is.EqualTo(0));
        Assert.That(paragraph.GetContentControl(0).RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetContentControl(0).GetText(), Is.EqualTo("Inside"));
        Assert.That(paragraph.GetText(), Is.EqualTo("BeforeInsideAfter"));
        Assert.That(paragraph.WriteXml(), Does.Contain("w:sdt"));
    }

    [Test]
    public void Paragraph_ExposesContentControlMetadata()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateBoundContentControlXml("Customer") + "</w:p>");
        global::docx.DocxContentControl contentControl = paragraph.GetContentControl(0);

        Assert.That(contentControl.Alias, Is.EqualTo("Customer Name"));
        Assert.That(contentControl.Tag, Is.EqualTo("customer.name"));
        Assert.That(contentControl.Type, Is.EqualTo("text"));
        Assert.That(contentControl.DataBindingXPath, Is.EqualTo("/root/customer/name"));
    }

    [Test]
    public void NestedStructuredDocumentTags_AreParsedAsNestedContentControls()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl("<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtContent>" + CreateRunContentControlXml("Nested") + "</w:sdtContent></w:sdt>");

        Assert.That(contentControl.ContentControlCount(), Is.EqualTo(1));
        Assert.That(contentControl.BlockCount(), Is.EqualTo(1));
        Assert.That(contentControl.GetBlock(0).IsContentControl(), Is.True);
        Assert.That(contentControl.GetContentControl(0).RunCount(), Is.EqualTo(1));
        Assert.That(contentControl.GetText(), Is.EqualTo("Nested"));
    }

    [Test]
    public void LoadFromXml_ResetsContentControlMetadataWhenPropertiesAreMissing()
    {
        global::docx.DocxContentControl contentControl = ParseContentControl(CreateBoundContentControlXml("Customer"));

        contentControl.Xml = "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtContent><w:r><w:t>Plain</w:t></w:r></w:sdtContent></w:sdt>";
        contentControl.LoadFromXml();

        Assert.That(contentControl.HasAlias(), Is.False);
        Assert.That(contentControl.HasTag(), Is.False);
        Assert.That(contentControl.HasType(), Is.False);
        Assert.That(contentControl.HasDataBinding(), Is.False);
        Assert.That(contentControl.Alias, Is.EqualTo(""));
        Assert.That(contentControl.Tag, Is.EqualTo(""));
        Assert.That(contentControl.Type, Is.EqualTo(""));
        Assert.That(contentControl.DataBindingStoreItemId, Is.EqualTo(""));
        Assert.That(contentControl.DataBindingXPath, Is.EqualTo(""));
        Assert.That(contentControl.DataBindingPrefixMappings, Is.EqualTo(""));
    }

    [Test]
    public void PublicContentControlManagement_EditsMetadataBindingAndPreservesOtherProperties()
    {
        global::docx.DocxContentControlManagement management = new global::docx.DocxContentControlManagement();
        global::docx.DocxContentControl contentControl = ParseContentControl("<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:lock w:val=\"sdtLocked\" /><w:text /></w:sdtPr><w:sdtContent><w:r><w:t>Original</w:t></w:r></w:sdtContent></w:sdt>");

        management.SetContentControlAlias(contentControl, "Customer Name");
        management.SetContentControlTag(contentControl, "customer.name");
        management.SetContentControlDataBinding(contentControl, "{11111111-1111-1111-1111-111111111111}", "/root/name", "xmlns:x='urn:test'");
        management.SetContentControlValue(contentControl, "Alice");

        global::docx.DocxContentControlInfo info = management.GetContentControlInfo(contentControl);

        Assert.That(info.Kind, Is.EqualTo(global::docx.DocxContentControlKind.PlainText));
        Assert.That(info.Alias, Is.EqualTo("Customer Name"));
        Assert.That(info.Tag, Is.EqualTo("customer.name"));
        Assert.That(info.DataBindingXPath, Is.EqualTo("/root/name"));
        Assert.That(management.GetContentControlValue(contentControl), Is.EqualTo("Alice"));
        Assert.That(contentControl.WriteXml(), Does.Contain("sdtLocked"));

        management.ClearContentControlDataBinding(contentControl);
        Assert.That(management.GetContentControlInfo(contentControl).DataBindingXPath, Is.EqualTo(""));
        Assert.That(contentControl.WriteXml(), Does.Contain("sdtLocked"));
    }

    [Test]
    public void PublicContentControlManagement_CreatesAndUpdatesEverySupportedTypedControl()
    {
        global::docx.DocxContentControlManagement management = new global::docx.DocxContentControlManagement();
        global::docx.DocxContentControl plain = management.CreatePlainTextControl("Name", "name", "Alice");
        List<global::docx.DocxBlock> blocks = new List<global::docx.DocxBlock>();
        blocks.Add(global::docx.DocxBlock.FromParagraph(ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Rich</w:t></w:r></w:p>")));
        global::docx.DocxContentControl rich = management.CreateRichTextControl("Biography", "bio", blocks);
        global::docx.DocxContentControl checkbox = management.CreateCheckboxControl("Approved", "approved", false);
        List<global::docx.DocxContentControlListItem> items = new List<global::docx.DocxContentControlListItem>();
        items.Add(new global::docx.DocxContentControlListItem("Red", "r"));
        items.Add(new global::docx.DocxContentControlListItem("Blue", "b"));
        global::docx.DocxContentControl combo = management.CreateComboBoxControl("Color", "combo", items, "r");
        global::docx.DocxContentControl dropDown = management.CreateDropDownListControl("Color", "drop", items, "b");
        global::docx.DocxContentControl date = management.CreateDatePickerControl("Due", "due", "2026-08-25T00:00:00Z", "August 25, 2026", "MMMM d, yyyy", "en-US");
        global::docx.DocxRun pictureRun = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:drawing><w:object /></w:drawing></w:r>");
        global::docx.DocxContentControl picture = management.CreatePictureControl("Photo", "photo", pictureRun);
        global::docx.DocxContentControl repeating = management.CreateRepeatingSectionControl("People", "people", new List<string> { "Alice", "Bob" });

        Assert.That(management.GetContentControlInfo(plain).Kind, Is.EqualTo(global::docx.DocxContentControlKind.PlainText));
        Assert.That(management.GetContentControlInfo(rich).Kind, Is.EqualTo(global::docx.DocxContentControlKind.RichText));
        Assert.That(rich.GetText(), Is.EqualTo("Rich"));
        Assert.That(management.GetContentControlInfo(checkbox).CheckboxValue, Is.False);
        management.SetCheckboxValue(checkbox, true);
        Assert.That(management.GetContentControlInfo(checkbox).CheckboxValue, Is.True);
        Assert.That(checkbox.GetValue(), Is.EqualTo("☒"));
        Assert.That(management.GetContentControlInfo(combo).Kind, Is.EqualTo(global::docx.DocxContentControlKind.ComboBox));
        Assert.That(management.GetContentControlInfo(combo).ListItems, Has.Count.EqualTo(2));
        management.SetListSelection(combo, "b");
        Assert.That(combo.GetValue(), Is.EqualTo("Blue"));
        Assert.That(management.GetContentControlInfo(dropDown).Kind, Is.EqualTo(global::docx.DocxContentControlKind.DropDownList));
        Assert.That(dropDown.GetValue(), Is.EqualTo("Blue"));
        Assert.That(management.GetContentControlInfo(date).DateFormat, Is.EqualTo("MMMM d, yyyy"));
        management.SetDatePickerValue(date, "2027-01-02T00:00:00Z", "January 2, 2027");
        Assert.That(management.GetContentControlInfo(date).DateValue, Is.EqualTo("2027-01-02T00:00:00Z"));
        Assert.That(date.GetValue(), Is.EqualTo("January 2, 2027"));
        Assert.That(management.GetContentControlInfo(picture).Kind, Is.EqualTo(global::docx.DocxContentControlKind.Picture));
        Assert.That(picture.WriteXml(), Does.Contain("w:drawing"));
        Assert.That(management.GetContentControlInfo(repeating).Kind, Is.EqualTo(global::docx.DocxContentControlKind.RepeatingSection));
        Assert.That(management.GetDescendantContentControls(repeating), Has.Count.EqualTo(2));
        Assert.That(management.GetDescendantContentControls(repeating)[0].Kind, Is.EqualTo(global::docx.DocxContentControlKind.RepeatingSectionItem));
        management.AddRepeatingSectionItem(repeating, "Carol");
        Assert.That(management.GetDescendantContentControls(repeating), Has.Count.EqualTo(3));
        Assert.That(repeating.GetText(), Is.EqualTo("AliceBobCarol"));
    }

    [Test]
    public void PublicContentControlManagement_TraversesFindsAndFillsNestedDocumentControls()
    {
        global::docx.DocxContentControlManagement management = new global::docx.DocxContentControlManagement();
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        string inner = CreateBoundContentControlXml("Original Customer");
        string outer = "<w:sdt><w:sdtPr><w:alias w:val=\"Outer\" /><w:richText /></w:sdtPr><w:sdtContent><w:p>" + inner + "</w:p></w:sdtContent></w:sdt>";
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + outer + CreateAliasContentControlXml("City", "Original City") + "</w:body></w:document>");
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(management.GetDocumentContentControls(document), Has.Count.EqualTo(3));
        Assert.That(management.FindContentControlByTag(document, "customer.name"), Is.Not.Null);
        Assert.That(management.FindContentControlByAlias(document, "City"), Is.Not.Null);
        Dictionary<string, string> values = new Dictionary<string, string>();
        values.Add("customer.name", "Alice");
        values.Add("City", "Paris");

        Assert.That(management.FillDocumentContentControls(document, values), Is.EqualTo(2));
        Assert.That(document.GetBody().GetText(), Is.EqualTo("Alice\nParis"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Contain("Alice"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Contain("Paris"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("Original Customer"));
    }

    [Test]
    public void ContentControlParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxContentControl contentControl = new global::docx.DocxContentControl("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxContentControl(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.ReadPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.LoadContentFromXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.IsTypeElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.DocxParagraphFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.DocxTableFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.DocxRunFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.DocxContentControlFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.GetValueWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.SetValue(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.FillContentControls(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.CreateRunValueContentXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.CreateParagraphValueContentXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.CreateContentXmlFromChildren(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.CreateValueRun(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxContentControl.CreateValueParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => contentControl.WriteContentTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBlock.FromContentControl(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxContentControlFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxParagraph("").FillContentControls(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBlock("raw", "").FillContentControls(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBody.Create().FillContentControls(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTable("").FillContentControls(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableRow.Create().FillContentControls(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.Create().FillContentControls(null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxContentControl.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxContentControl.ParseXml("<root />"));
    }

    static global::docx.DocxContentControl ParseContentControl(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("sdt", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxContentControl.Parse(reader);
    }

    static global::docx.DocxBody ParseBody(string bodyXml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(bodyXml);

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxBody.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string paragraphXml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(paragraphXml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string runXml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(runXml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        return global::docx.DocxRun.Parse(reader);
    }

    static string CreateBodyXml(string[] children)
    {
        return "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:body>";
    }

    static string CreateBlockContentControlXml()
    {
        return "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:id w:val=\"1\" /></w:sdtPr><w:sdtContent><w:p><w:r><w:t>Inside</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>Cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:sdtContent></w:sdt>";
    }

    static string CreateRunContentControlXml(string text)
    {
        return "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:id w:val=\"2\" /></w:sdtPr><w:sdtContent><w:r><w:t>" + text + "</w:t></w:r></w:sdtContent></w:sdt>";
    }

    static string CreateBoundContentControlXml(string text)
    {
        return "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:alias w:val=\"Customer Name\" /><w:tag w:val=\"customer.name\" /><w:text /><w:dataBinding w:storeItemID=\"{11111111-1111-1111-1111-111111111111}\" w:xpath=\"/root/customer/name\" w:prefixMappings=\"xmlns:ns='urn:test'\" /></w:sdtPr><w:sdtContent><w:r><w:t>" + text + "</w:t></w:r></w:sdtContent></w:sdt>";
    }

    static string CreateAliasContentControlXml(string alias, string text)
    {
        return "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtPr><w:alias w:val=\"" + alias + "\" /><w:text /></w:sdtPr><w:sdtContent><w:r><w:t>" + text + "</w:t></w:r></w:sdtContent></w:sdt>";
    }
}
