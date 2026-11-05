using System.Text;

namespace odf.Tests;

public class TemplateFieldFillerTests
{
    [Test]
    public void FillsRetainedPlaceholderAndPreservesNestedFormatting()
    {
        OdfDocument document = ReadFlat(
            "",
            "<text:p>Hello <text:placeholder text:description='CustomerName' "
            + "text:placeholder-type='text' text:style-name='FieldStyle'>"
            + "<text:span text:style-name='Strong'>Customer</text:span>"
            + "</text:placeholder>!</text:p>");
        OdfTextCursor cursor = TextEditingModel.CreateCursor(
            document,
            document.TextBlocks[0],
            document.TextBlocks[0].Text.Length);

        OdfEditResult result = TemplateFieldFiller.FillPlaceholders(
            document,
            "CustomerName",
            "Ada",
            true);
        OdfDocument reread = WriteAndRead(document);
        OdfTextField field = reread.TextFields[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(result.AffectedCount, Is.EqualTo(1));
            Assert.That(result.SearchMatches[0].Location.TextOffset, Is.EqualTo(6));
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("Hello Ada!"));
            Assert.That(cursor.Offset, Is.EqualTo(10));
            Assert.That(reread.TextFields, Has.Length.EqualTo(1));
            Assert.That(field.Kind, Is.EqualTo(OdfTextFieldKind.Placeholder));
            Assert.That(field.DisplayText, Is.EqualTo("Ada"));
            Assert.That(field.TextStyleName, Is.EqualTo("FieldStyle"));
            Assert.That(field.Run!.Runs[0].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(field.Run.Runs[0].StyleName, Is.EqualTo("Strong"));
            Assert.That(field.Run.Runs[0].Text, Is.EqualTo("Ada"));
        });
    }

    [Test]
    public void RemovesPlaceholderStructureButRetainsItsTextStyle()
    {
        OdfDocument document = ReadFlat(
            "",
            "<text:p>Dear <text:placeholder text:description='Recipient' "
            + "text:style-name='PlaceholderStyle'>Name</text:placeholder>.</text:p>");

        OdfEditResult result = TemplateFieldFiller.FillPlaceholders(
            document,
            "Recipient",
            "Grace",
            false);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Changed, Is.True);
            Assert.That(document.TextFields, Is.Empty);
            Assert.That(reread.TextFields, Is.Empty);
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("Dear Grace."));
            Assert.That(reread.TextBlocks[0].Runs[1].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(
                reread.TextBlocks[0].Runs[1].StyleName,
                Is.EqualTo("PlaceholderStyle"));
            Assert.That(reread.TextBlocks[0].Runs[1].Text, Is.EqualTo("Grace"));
        });
    }

    [Test]
    public void FillsEveryRetainedUserFieldAndUpdatesItsDeclaration()
    {
        OdfDocument document = ReadFlat(
            "<text:user-field-decls><text:user-field-decl text:name='Customer' "
            + "office:value-type='string' office:string-value='Old'/>"
            + "</text:user-field-decls>",
            "<text:p><text:user-field-get text:name='Customer'>"
            + "<text:span text:style-name='Strong'>Old</text:span>"
            + "</text:user-field-get> / "
            + "<text:user-field-input text:name='Customer' "
            + "text:style-name='InputStyle'>Old</text:user-field-input></text:p>");

        OdfEditResult result = TemplateFieldFiller.FillUserField(
            document,
            "Customer",
            "Ada",
            true);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.AffectedCount, Is.EqualTo(2));
            Assert.That(reread.TextFieldDeclarations, Has.Length.EqualTo(1));
            Assert.That(reread.TextFieldDeclarations[0].ValueType, Is.EqualTo("string"));
            Assert.That(reread.TextFieldDeclarations[0].StringValue, Is.EqualTo("Ada"));
            Assert.That(
                reread.TextFields.Select(item => item.DisplayText),
                Is.EqualTo(new[] { "Ada", "Ada" }));
            Assert.That(
                reread.TextFields[0].Run!.Runs[0].StyleName,
                Is.EqualTo("Strong"));
            Assert.That(reread.TextFields[1].TextStyleName, Is.EqualTo("InputStyle"));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("Ada / Ada"));
        });
    }

    [Test]
    public void RemovesFilledUserFieldsAndTheirUnusedDeclaration()
    {
        OdfDocument document = ReadFlat(
            "<text:user-field-decls><text:user-field-decl text:name='Customer' "
            + "office:value-type='string' office:string-value='Old'/>"
            + "<text:user-field-decl text:name='Other' "
            + "office:value-type='string' office:string-value='Keep'/>"
            + "</text:user-field-decls>",
            "<text:p>Value: <text:user-field-get text:name='Customer' "
            + "text:style-name='UserStyle'>Old</text:user-field-get>.</text:p>");

        OdfEditResult result = TemplateFieldFiller.FillUserField(
            document,
            "Customer",
            "Ada",
            false);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(document.TextFields, Is.Empty);
            Assert.That(document.TextFieldDeclarations, Has.Length.EqualTo(1));
            Assert.That(document.TextFieldDeclarations[0].Name, Is.EqualTo("Other"));
            Assert.That(reread.TextFields, Is.Empty);
            Assert.That(reread.TextFieldDeclarations, Has.Length.EqualTo(1));
            Assert.That(reread.TextFieldDeclarations[0].Name, Is.EqualTo("Other"));
            Assert.That(reread.TextBlocks[0].Text, Is.EqualTo("Value: Ada."));
            Assert.That(reread.TextBlocks[0].Runs[1].Kind, Is.EqualTo(OdfTextRunKind.Span));
            Assert.That(reread.TextBlocks[0].Runs[1].StyleName, Is.EqualTo("UserStyle"));
        });
    }

    [Test]
    public void ReportsNoChangeWhenNoTemplateValueNeedsFilling()
    {
        OdfDocument document = ReadFlat(
            "",
            "<text:p><text:placeholder text:description='Key'>Value</text:placeholder>"
            + "</text:p>");

        OdfEditResult equal = TemplateFieldFiller.FillPlaceholders(
            document,
            "Key",
            "Value",
            true);
        OdfEditResult missing = TemplateFieldFiller.FillUserField(
            document,
            "Missing",
            "Value",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(equal.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(equal.SearchMatches, Has.Length.EqualTo(1));
            Assert.That(missing.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(missing.SearchMatches, Is.Empty);
            Assert.That(document.TextBlocks[0].Text, Is.EqualTo("Value"));
        });
    }

    private static OdfDocument ReadFlat(
        string declarations,
        string body)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + "<office:styles/>"
            + "<office:body><office:text>"
            + declarations
            + body
            + "</office:text></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "template-fill.fodt",
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
            "template-fill.fodt",
            new OdfLoadOptions());
    }
}
