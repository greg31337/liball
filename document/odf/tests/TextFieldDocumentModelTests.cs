using System.Text;

namespace odf.Tests;

public class TextFieldDocumentModelTests
{
    [Test]
    public void ClassifiesEveryStandardFieldFamilyAndRetainsElementNames()
    {
        OdfDocument document = ReadFlat(
            "",
            "<text:p>"
            + "<text:page-number>1</text:page-number>"
            + "<text:page-count>2</text:page-count>"
            + "<text:date>2026-01-01</text:date>"
            + "<text:time>10:30</text:time>"
            + "<text:author-name>Author</text:author-name>"
            + "<text:sender-email>sender@example.test</text:sender-email>"
            + "<text:file-name>file.odt</text:file-name>"
            + "<text:template-name>Template</text:template-name>"
            + "<text:chapter>Chapter</text:chapter>"
            + "<text:word-count>10</text:word-count>"
            + "<text:character-count>20</text:character-count>"
            + "<text:paragraph-count>3</text:paragraph-count>"
            + "<text:table-count>4</text:table-count>"
            + "<text:image-count>5</text:image-count>"
            + "<text:object-count>6</text:object-count>"
            + "<text:variable-get text:name='V'>Variable</text:variable-get>"
            + "<text:user-field-get text:name='U'>User</text:user-field-get>"
            + "<text:sequence text:name='S'>1</text:sequence>"
            + "<text:expression text:formula='of:=1+1'>2</text:expression>"
            + "<text:placeholder text:placeholder-type='text'>Placeholder</text:placeholder>"
            + "<text:text-input text:description='Input'>Value</text:text-input>"
            + "<text:conditional-text text:condition='of:=TRUE()' "
            + "text:string-value-if-true='Yes' text:string-value-if-false='No' "
            + "text:current-value='true'>Yes</text:conditional-text>"
            + "<text:hidden-text text:condition='of:=TRUE()' text:is-hidden='true'/>"
            + "<text:hidden-paragraph text:condition='of:=TRUE()' text:is-hidden='true'/>"
            + "<text:database-display text:database-name='Db' text:table-name='People' "
            + "text:column-name='Name'>Ada</text:database-display>"
            + "<text:bookmark-ref text:ref-name='Target'>Target</text:bookmark-ref>"
            + "<text:sheet-name>Sheet1</text:sheet-name>"
            + "<text:cell-address>Sheet1.A1</text:cell-address>"
            + "<text:meta-field>Metadata</text:meta-field>"
            + "</text:p>");

        Assert.Multiple(() =>
        {
            Assert.That(document.TextFields, Has.Length.EqualTo(29));
            AssertField(document, "page-number", OdfTextFieldKind.PageNumber);
            AssertField(document, "page-count", OdfTextFieldKind.PageCount);
            AssertField(document, "date", OdfTextFieldKind.Date);
            AssertField(document, "time", OdfTextFieldKind.Time);
            AssertField(document, "author-name", OdfTextFieldKind.AuthorName);
            AssertField(document, "sender-email", OdfTextFieldKind.Sender);
            AssertField(document, "file-name", OdfTextFieldKind.FileName);
            AssertField(document, "template-name", OdfTextFieldKind.TemplateName);
            AssertField(document, "chapter", OdfTextFieldKind.Chapter);
            AssertField(document, "word-count", OdfTextFieldKind.WordCount);
            AssertField(document, "character-count", OdfTextFieldKind.CharacterCount);
            AssertField(document, "paragraph-count", OdfTextFieldKind.ParagraphCount);
            AssertField(document, "table-count", OdfTextFieldKind.TableCount);
            AssertField(document, "image-count", OdfTextFieldKind.ImageCount);
            AssertField(document, "object-count", OdfTextFieldKind.ObjectCount);
            AssertField(document, "variable-get", OdfTextFieldKind.Variable);
            AssertField(document, "user-field-get", OdfTextFieldKind.UserField);
            AssertField(document, "sequence", OdfTextFieldKind.Sequence);
            AssertField(document, "expression", OdfTextFieldKind.Expression);
            AssertField(document, "placeholder", OdfTextFieldKind.Placeholder);
            AssertField(document, "text-input", OdfTextFieldKind.Input);
            AssertField(document, "conditional-text", OdfTextFieldKind.ConditionalText);
            AssertField(document, "hidden-text", OdfTextFieldKind.HiddenText);
            AssertField(document, "hidden-paragraph", OdfTextFieldKind.HiddenParagraph);
            AssertField(document, "database-display", OdfTextFieldKind.Database);
            AssertField(document, "bookmark-ref", OdfTextFieldKind.Reference);
            AssertField(document, "sheet-name", OdfTextFieldKind.SheetName);
            AssertField(document, "cell-address", OdfTextFieldKind.CellAddress);
            AssertField(document, "meta-field", OdfTextFieldKind.Metadata);
        });
    }

    [Test]
    public void MaterializesDeclarationsAndResolvesTheirFieldReferences()
    {
        OdfDocument document = ReadFlat(
            "<text:variable-decls><text:variable-decl text:name='Variable' "
            + "office:value-type='float' office:value='12.5'/></text:variable-decls>"
            + "<text:user-field-decls><text:user-field-decl text:name='User' "
            + "office:value-type='string' office:string-value='User value'/>"
            + "</text:user-field-decls>"
            + "<text:sequence-decls><text:sequence-decl text:name='Figure' "
            + "text:display-outline-level='2' text:separation-character='.'/></text:sequence-decls>"
            + "<text:dde-connection-decls><text:dde-connection-decl office:name='Prices' "
            + "office:dde-application='Calc' office:dde-topic='Sheet1' "
            + "office:dde-item='A1' office:automatic-update='true'/>"
            + "</text:dde-connection-decls>",
            "<text:p><text:variable-get text:name='Variable'>12.5</text:variable-get>"
            + "<text:user-field-get text:name='User'>User value</text:user-field-get>"
            + "<text:sequence text:name='Figure'>1</text:sequence>"
            + "<text:dde-connection office:name='Prices'>42</text:dde-connection>"
            + "</text:p>");
        OdfTextField variable = Field(document, "variable-get");
        OdfTextField user = Field(document, "user-field-get");
        OdfTextField sequence = Field(document, "sequence");
        OdfTextField dde = Field(document, "dde-connection");

        Assert.Multiple(() =>
        {
            Assert.That(document.TextFieldDeclarations, Has.Length.EqualTo(4));
            Assert.That(variable.Declaration?.Kind, Is.EqualTo(OdfTextFieldDeclarationKind.Variable));
            Assert.That(variable.Declaration?.Value, Is.EqualTo("12.5"));
            Assert.That(user.Declaration?.StringValue, Is.EqualTo("User value"));
            Assert.That(sequence.Declaration?.DisplayOutlineLevel, Is.EqualTo("2"));
            Assert.That(sequence.Declaration?.SeparationCharacter, Is.EqualTo("."));
            Assert.That(dde.Declaration?.DdeApplication, Is.EqualTo("Calc"));
            Assert.That(dde.Declaration?.DdeTopic, Is.EqualTo("Sheet1"));
            Assert.That(dde.Declaration?.DdeItem, Is.EqualTo("A1"));
            Assert.That(dde.Declaration?.AutomaticUpdate, Is.EqualTo("true"));
            Assert.That(variable.TextBlock, Is.SameAs(document.TextBlocks[0]));
            Assert.That(variable.Run, Is.Not.Null);
        });
    }

    [Test]
    public void ExposesConditionalHiddenPlaceholderAndDatabaseDetails()
    {
        OdfDocument document = ReadFlat(
            "",
            "<text:p><text:conditional-text text:condition='of:=[.A1]&gt;0' "
            + "text:string-value-if-true='Positive' text:string-value-if-false='Other' "
            + "text:current-value='false'>Other</text:conditional-text>"
            + "<text:hidden-text text:condition='of:=TRUE()' text:is-hidden='true' "
            + "text:string-value='Secret'>Secret</text:hidden-text>"
            + "<text:placeholder text:placeholder-type='table' text:description='Insert table' "
            + "text:style-name='PlaceholderStyle'>Table</text:placeholder>"
            + "<text:database-display text:database-name='Contacts' text:table-name='People' "
            + "text:table-type='table' text:column-name='Email' text:row-number='7' "
            + "style:data-style-name='MailFormat'>mail@example.test</text:database-display>"
            + "</text:p>");
        OdfTextField conditional = Field(document, "conditional-text");
        OdfTextField hidden = Field(document, "hidden-text");
        OdfTextField placeholder = Field(document, "placeholder");
        OdfTextField database = Field(document, "database-display");

        Assert.Multiple(() =>
        {
            Assert.That(conditional.Condition, Is.EqualTo("of:=[.A1]>0"));
            Assert.That(conditional.SelectedText, Is.EqualTo("Other"));
            Assert.That(hidden.IsHidden, Is.True);
            Assert.That(hidden.StringValue, Is.EqualTo("Secret"));
            Assert.That(placeholder.PlaceholderType, Is.EqualTo("table"));
            Assert.That(placeholder.Description, Is.EqualTo("Insert table"));
            Assert.That(placeholder.TextStyleName, Is.EqualTo("PlaceholderStyle"));
            Assert.That(database.DatabaseName, Is.EqualTo("Contacts"));
            Assert.That(database.TableName, Is.EqualTo("People"));
            Assert.That(database.TableType, Is.EqualTo("table"));
            Assert.That(database.ColumnName, Is.EqualTo("Email"));
            Assert.That(database.RowNumber, Is.EqualTo("7"));
            Assert.That(database.DataStyleName, Is.EqualTo("MailFormat"));
        });
    }

    [Test]
    public void WritesDeclarationFieldConditionalAndDatabaseChanges()
    {
        OdfDocument document = ReadFlat(
            "<text:variable-decls><text:variable-decl text:name='Old' "
            + "office:value-type='float' office:value='1'/></text:variable-decls>",
            "<text:p><text:variable-get text:name='Old' text:style-name='FieldStyle'>1</text:variable-get>"
            + "<text:conditional-text text:string-value-if-true='Yes' "
            + "text:string-value-if-false='No' text:current-value='false'>No</text:conditional-text>"
            + "<text:database-display text:database-name='OldDb' text:table-name='OldTable' "
            + "text:column-name='OldColumn'>Old</text:database-display></text:p>");
        OdfTextFieldDeclaration declaration =
            document.TextFieldDeclarations[0];
        OdfTextField variable = Field(document, "variable-get");
        OdfTextField conditional = Field(document, "conditional-text");
        OdfTextField database = Field(document, "database-display");

        TextFieldNavigator.RenameDeclaration(
            document,
            declaration,
            "Renamed");
        declaration.Value = "9";
        variable.DisplayText = "Nine";
        variable.TextStyleName = "ChangedStyle";
        conditional.CurrentValue = "true";
        conditional.DisplayText = "Yes";
        database.DatabaseName = "NewDb";
        database.TableName = "NewTable";
        database.ColumnName = "NewColumn";
        database.RowNumber = "12";
        database.DisplayText = "New value";
        OdfDocument reread = WriteAndRead(document);
        OdfTextField rereadVariable = Field(reread, "variable-get");
        OdfTextField rereadConditional = Field(reread, "conditional-text");
        OdfTextField rereadDatabase = Field(reread, "database-display");

        Assert.Multiple(() =>
        {
            Assert.That(reread.TextFieldDeclarations[0].Name, Is.EqualTo("Renamed"));
            Assert.That(reread.TextFieldDeclarations[0].Value, Is.EqualTo("9"));
            Assert.That(rereadVariable.Name, Is.EqualTo("Renamed"));
            Assert.That(rereadVariable.Declaration, Is.SameAs(reread.TextFieldDeclarations[0]));
            Assert.That(rereadVariable.DisplayText, Is.EqualTo("Nine"));
            Assert.That(rereadVariable.TextStyleName, Is.EqualTo("ChangedStyle"));
            Assert.That(rereadConditional.SelectedText, Is.EqualTo("Yes"));
            Assert.That(rereadConditional.DisplayText, Is.EqualTo("Yes"));
            Assert.That(rereadDatabase.DatabaseName, Is.EqualTo("NewDb"));
            Assert.That(rereadDatabase.TableName, Is.EqualTo("NewTable"));
            Assert.That(rereadDatabase.ColumnName, Is.EqualTo("NewColumn"));
            Assert.That(rereadDatabase.RowNumber, Is.EqualTo("12"));
            Assert.That(rereadDatabase.DisplayText, Is.EqualTo("New value"));
        });
    }

    [Test]
    public void AddsRemovesAndRejectsAmbiguousDeclarations()
    {
        OdfDocument document = ReadFlat(
            "<text:variable-decls><text:variable-decl text:name='Remove'/></text:variable-decls>"
            + "<text:user-field-decls><text:user-field-decl text:name='Duplicate'/>"
            + "<text:user-field-decl text:name='Duplicate'/></text:user-field-decls>",
            "<text:p><text:user-field-get text:name='Duplicate'>Value</text:user-field-get></text:p>");
        OdfTextField ambiguous = Field(document, "user-field-get");
        Assert.That(ambiguous.Declaration, Is.Null);
        Assert.That(ambiguous.HasAmbiguousDeclaration, Is.True);
        OdfTextFieldDeclaration added = new OdfTextFieldDeclaration
        {
            Kind = OdfTextFieldDeclarationKind.Sequence,
            Name = "AddedSequence",
            DisplayOutlineLevel = "3",
            SeparationCharacter = ":"
        };
        document.TextFieldDeclarations = [added];

        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.TextFieldDeclarations, Has.Length.EqualTo(1));
            Assert.That(reread.TextFieldDeclarations[0].Kind, Is.EqualTo(OdfTextFieldDeclarationKind.Sequence));
            Assert.That(reread.TextFieldDeclarations[0].Name, Is.EqualTo("AddedSequence"));
            Assert.That(reread.TextFieldDeclarations[0].DisplayOutlineLevel, Is.EqualTo("3"));
            Assert.That(reread.TextFieldDeclarations[0].SeparationCharacter, Is.EqualTo(":"));
            Assert.That(reread.TextFieldDeclarations[0].Node, Is.Not.Null);
            Assert.That(
                TextFieldNavigator.FindDeclaration(
                    reread,
                    OdfTextFieldDeclarationKind.Variable,
                    "Remove"),
                Is.Null);
        });
    }

    private static void AssertField(
        OdfDocument document,
        string elementName,
        OdfTextFieldKind kind)
    {
        Assert.That(Field(document, elementName).Kind, Is.EqualTo(kind));
    }

    private static OdfTextField Field(
        OdfDocument document,
        string elementName)
    {
        return document.TextFields.Single(
            item => item.ElementName == elementName);
    }

    private static OdfDocument ReadFlat(
        string declarations,
        string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles/>"
            + "<office:body><office:text>"
            + declarations
            + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "text-fields.fodt",
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
            "text-fields.fodt",
            new OdfLoadOptions());
    }
}
