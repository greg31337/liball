using System.Text;

namespace odf.Tests;

public class SpreadsheetFormattingModelTests
{
    [Test]
    public void MaterializesValidationMessagesCellAssignmentsAndConditionalStyles()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        OdfContentValidation validation = document.ContentValidations[0];
        OdfConditionalStyle conditional = document.ConditionalStyles[0];
        OdfCell cell = document.Tables[0].Rows[0].Cells[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.ContentValidations, Has.Length.EqualTo(2));
            Assert.That(validation.Name, Is.EqualTo("Positive"));
            Assert.That(validation.Condition,
                Is.EqualTo("of:cell-content-is-between([Sheet1.A2];[Sheet1.A4])"));
            Assert.That(validation.BaseCellAddress, Is.EqualTo("Sheet1.A2"));
            Assert.That(validation.AllowEmptyCell, Is.False);
            Assert.That(validation.DisplayList, Is.EqualTo("sort-ascending"));
            Assert.That(validation.HasHelpMessage, Is.True);
            Assert.That(validation.DisplayHelp, Is.True);
            Assert.That(validation.HelpTitle, Is.EqualTo("Input"));
            Assert.That(validation.HelpText, Is.EqualTo("Enter a positive value."));
            Assert.That(validation.HasErrorMessage, Is.True);
            Assert.That(validation.DisplayError, Is.True);
            Assert.That(validation.ErrorMessageType, Is.EqualTo("warning"));
            Assert.That(validation.ErrorText, Is.EqualTo("Value is outside the range."));
            Assert.That(cell.ValidationName, Is.EqualTo("Positive"));
            Assert.That(cell.Validation, Is.SameAs(validation));
            Assert.That(document.ConditionalStyles, Has.Length.EqualTo(1));
            Assert.That(conditional.OwnerStyleName, Is.EqualTo("InputCell"));
            Assert.That(conditional.OwnerStyleFamily, Is.EqualTo("table-cell"));
            Assert.That(conditional.Condition, Is.EqualTo("cell-content()<0"));
            Assert.That(conditional.ApplyStyleName, Is.EqualTo("Negative"));
            Assert.That(conditional.BaseCellAddress, Is.EqualTo("Sheet1.A2"));
            Assert.That(conditional.OwnerStyle, Is.Not.Null);
            Assert.That(conditional.ApplyStyle, Is.Not.Null);
        });
    }

    [Test]
    public void SynchronizesValidationAndConditionalStyleChanges()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        OdfContentValidation validation = document.ContentValidations[0];
        validation.DisplayList = "unsorted";
        validation.AllowEmptyCell = true;
        validation.HelpTitle = "Updated input";
        validation.HelpText = "Choose a listed value.";
        validation.ErrorMessageType = "information";
        validation.ErrorText = "Check the selected value.";
        OdfContentValidation added = new OdfContentValidation();
        added.Name = "Added";
        added.Condition = "of:cell-content-is-decimal-number()";
        added.BaseCellAddress = "Sheet1.B2";
        added.DisplayList = "none";
        added.HasErrorMessage = true;
        added.DisplayError = true;
        added.ErrorTitle = "Decimal required";
        added.ErrorText = "Enter a decimal number.";
        added.ErrorMessageType = "stop";
        document.ContentValidations = [validation, added];
        document.Tables[0].Rows[0].Cells[0].ValidationName = "Added";
        OdfConditionalStyle replacement = new OdfConditionalStyle();
        replacement.OwnerStyleName = "InputCell";
        replacement.OwnerStyleFamily = "table-cell";
        replacement.PartPath = "";
        replacement.IsAutomatic = true;
        replacement.Condition = "cell-content()>100";
        replacement.ApplyStyleName = "High";
        replacement.BaseCellAddress = "Sheet1.B2";
        document.ConditionalStyles = [replacement];

        OdfDocument reread = WriteAndRead(document);
        OdfContentValidation updated = reread.ContentValidations.Single(
            item => item.Name == "Positive");
        OdfContentValidation addedResult = reread.ContentValidations.Single(
            item => item.Name == "Added");
        OdfConditionalStyle conditional = reread.ConditionalStyles.Single();
        OdfCell cell = reread.Tables[0].Rows[0].Cells[0];

        Assert.Multiple(() =>
        {
            Assert.That(reread.ContentValidations, Has.Length.EqualTo(2));
            Assert.That(updated.DisplayList, Is.EqualTo("unsorted"));
            Assert.That(updated.AllowEmptyCell, Is.True);
            Assert.That(updated.HelpTitle, Is.EqualTo("Updated input"));
            Assert.That(updated.HelpText, Is.EqualTo("Choose a listed value."));
            Assert.That(updated.ErrorMessageType, Is.EqualTo("information"));
            Assert.That(updated.ErrorText, Is.EqualTo("Check the selected value."));
            Assert.That(reread.ContentValidations.Any(
                item => item.Name == "RemoveMe"), Is.False);
            Assert.That(addedResult.ErrorTitle, Is.EqualTo("Decimal required"));
            Assert.That(addedResult.ErrorText, Is.EqualTo("Enter a decimal number."));
            Assert.That(cell.ValidationName, Is.EqualTo("Added"));
            Assert.That(cell.Validation, Is.SameAs(addedResult));
            Assert.That(conditional.Condition, Is.EqualTo("cell-content()>100"));
            Assert.That(conditional.ApplyStyleName, Is.EqualTo("High"));
            Assert.That(conditional.ApplyStyle, Is.Not.Null);
        });
    }

    [Test]
    public void StructuralEditsUpdateValidationAndConditionalStyleReferences()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());

        TableStructureEditor.InsertRows(
            document,
            document.Tables[0],
            1,
            2);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.ContentValidations[0].BaseCellAddress,
                Is.EqualTo("Sheet1.A4"));
            Assert.That(document.ContentValidations[0].Condition,
                Is.EqualTo("of:cell-content-is-between([Sheet1.A4];[Sheet1.A6])"));
            Assert.That(document.ConditionalStyles[0].BaseCellAddress,
                Is.EqualTo("Sheet1.A4"));
            Assert.That(reread.ContentValidations[0].BaseCellAddress,
                Is.EqualTo("Sheet1.A4"));
        });
    }

    [Test]
    public void RejectsConditionalStyleWithoutResolvableOwner()
    {
        OdfDocument document = ReadSpreadsheet(FeatureXml());
        OdfConditionalStyle invalid = new OdfConditionalStyle();
        invalid.OwnerStyleName = "Missing";
        invalid.OwnerStyleFamily = "table-cell";
        invalid.IsAutomatic = true;
        invalid.Condition = "cell-content()>0";
        invalid.ApplyStyleName = "High";
        document.ConditionalStyles = [invalid];

        Assert.That(
            () => new OdfDocumentWriter().WriteDocumentBytes(
                document,
                new OdfSaveOptions()),
            Throws.TypeOf<OdfDocumentException>());
    }

    private static string FeatureXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:automatic-styles>"
            + "<style:style style:name='InputCell' style:family='table-cell'>"
            + "<style:table-cell-properties/>"
            + "<style:map style:condition='cell-content()&lt;0' "
            + "style:apply-style-name='Negative' "
            + "style:base-cell-address='Sheet1.A2'/></style:style>"
            + "<style:style style:name='Negative' style:family='table-cell'/>"
            + "<style:style style:name='High' style:family='table-cell'/>"
            + "</office:automatic-styles><office:body><office:spreadsheet>"
            + "<table:content-validations>"
            + "<table:content-validation table:name='Positive' "
            + "table:condition='of:cell-content-is-between([Sheet1.A2];[Sheet1.A4])' "
            + "table:base-cell-address='Sheet1.A2' "
            + "table:allow-empty-cell='false' table:display-list='sort-ascending'>"
            + "<table:help-message table:title='Input' table:display='true'>"
            + "<text:p>Enter a positive value.</text:p></table:help-message>"
            + "<table:error-message table:title='Invalid' table:display='true' "
            + "table:message-type='warning'><text:p>Value is outside the range.</text:p>"
            + "</table:error-message></table:content-validation>"
            + "<table:content-validation table:name='RemoveMe'/>"
            + "</table:content-validations>"
            + "<table:table table:name='Sheet1'><table:table-row "
            + "table:number-rows-repeated='4'><table:table-cell "
            + "table:number-columns-repeated='3' table:style-name='InputCell' "
            + "table:content-validation-name='Positive'/></table:table-row>"
            + "</table:table></office:spreadsheet></office:body></office:document>";
    }

    private static OdfDocument ReadSpreadsheet(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "spreadsheet-formatting.fods",
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
        return ReadSpreadsheet(Encoding.UTF8.GetString(bytes));
    }
}
