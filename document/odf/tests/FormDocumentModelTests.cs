using System.Text;

namespace odf.Tests;

public class FormDocumentModelTests
{
    [Test]
    public void MaterializesEveryStandardControlKindAndHierarchy()
    {
        OdfDocument document = ReadFlat(AllControlsXml());
        OdfFormControl form = Control(document, "MainForm");
        OdfFormControl grid = document.FormControls.Single(
            item => item.Kind == OdfFormControlKind.Grid);
        OdfFormControl column = document.FormControls.Single(
            item => item.Kind == OdfFormControlKind.Column);
        OdfFormControlKind[] kinds = document.FormControls
            .Select(item => item.Kind)
            .Distinct()
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Form));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Button));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Text));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.TextArea));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.FormattedText));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Number));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Date));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Time));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.ComboBox));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.ListBox));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.CheckBox));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Password));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.File));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Label));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Image));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.RadioButton));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Frame));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.ImageFrame));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Hidden));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Grid));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.ValueRange));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.GenericControl));
            Assert.That(kinds, Does.Contain(OdfFormControlKind.Column));
            Assert.That(grid.Parent, Is.SameAs(form));
            Assert.That(column.Parent, Is.SameAs(grid));
            Assert.That(column.Children, Has.Length.EqualTo(1));
            Assert.That(column.Children[0].Kind,
                Is.EqualTo(OdfFormControlKind.Text));
        });
    }

    [Test]
    public void MaterializesPropertiesBindingsEventsOptionsAndReferences()
    {
        OdfDocument document = ReadFlat(FeatureXml());
        OdfFormControl form = Control(document, "MainForm");
        OdfFormControl input = Control(document, "Input");
        OdfFormControl label = Control(document, "Caption");
        OdfFormControl list = Control(document, "Choices");
        OdfFormReference target = label.References.Single(
            item => item.TargetId == "input");
        OdfFormReference missing = label.References.Single(
            item => item.TargetId == "missing");
        OdfFormReference drawing = input.References.Single(
            item => item.Kind == OdfFormReferenceKind.DrawingControl);

        Assert.Multiple(() =>
        {
            Assert.That(form.Properties, Has.Length.EqualTo(2));
            Assert.That(form.Properties[0].Name, Is.EqualTo("Theme"));
            Assert.That(form.Properties[0].ValueType, Is.EqualTo("string"));
            Assert.That(form.Properties[0].Values[0].Value,
                Is.EqualTo("blue"));
            Assert.That(form.Properties[1].IsList, Is.True);
            Assert.That(form.Properties[1].Values
                .Select(item => item.Value), Is.EqualTo(new[] { "1.5", "2.5" }));
            Assert.That(input.Binding.LinkedCell, Is.EqualTo("Sheet1.A1"));
            Assert.That(input.Binding.SourceCellRange,
                Is.EqualTo("Sheet1.A1:Sheet1.A3"));
            Assert.That(input.Binding.DataField, Is.EqualTo("customer_name"));
            Assert.That(input.Binding.XFormsBind, Is.EqualTo("customer"));
            Assert.That(input.Validation.IsDisabled, Is.True);
            Assert.That(input.Validation.IsInputRequired, Is.True);
            Assert.That(input.Validation.IsReadOnly, Is.False);
            Assert.That(input.Validation.IsValidationEnabled, Is.True);
            Assert.That(input.Validation.MaxLength, Is.EqualTo("40"));
            Assert.That(input.Events, Has.Length.EqualTo(1));
            Assert.That(input.Events[0].MacroName,
                Is.EqualTo("Standard.Validate"));
            Assert.That(list.Options, Has.Length.EqualTo(2));
            Assert.That(list.Options[0].Label, Is.EqualTo("One"));
            Assert.That(list.Options[0].Value, Is.EqualTo("1"));
            Assert.That(list.Options[0].IsSelected, Is.True);
            Assert.That(list.Options[1].Text, Is.EqualTo("Two"));
            Assert.That(target.TargetControl, Is.SameAs(input));
            Assert.That(target.IsBroken, Is.False);
            Assert.That(missing.TargetControl, Is.Null);
            Assert.That(missing.IsBroken, Is.True);
            Assert.That(drawing.TargetControl, Is.SameAs(input));
            Assert.That(drawing.DrawingObject?.ControlId, Is.EqualTo("input"));
        });
    }

    [Test]
    public void WritesFormMetadataChangesAndRebuildsReferences()
    {
        OdfDocument document = ReadFlat(FeatureXml());
        OdfFormControl input = Control(document, "Input");
        OdfFormControl label = Control(document, "Caption");
        OdfFormControl list = Control(document, "Choices");
        OdfFormControl form = Control(document, "MainForm");
        input.Label = "Changed input";
        input.CurrentValue = "changed";
        input.Binding.LinkedCell = "Sheet1.B2";
        input.Validation.IsDisabled = false;
        input.Validation.MaxLength = "80";
        input.Events[0].MacroName = "Standard.Changed";
        label.For = "input";
        list.Options[0].Label = "First";
        list.Options[0].IsSelected = false;
        list.Options[1].Text = "Second";
        form.Properties[0].Values[0].Value = "green";
        form.Properties[1].Values[1].Value = "3.5";

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfFormControl changedInput = Control(reread, "Input");
        OdfFormControl changedLabel = Control(reread, "Caption");
        OdfFormControl changedList = Control(reread, "Choices");
        OdfFormControl changedForm = Control(reread, "MainForm");

        Assert.Multiple(() =>
        {
            Assert.That(changedInput.Label, Is.EqualTo("Changed input"));
            Assert.That(changedInput.CurrentValue, Is.EqualTo("changed"));
            Assert.That(changedInput.Binding.LinkedCell,
                Is.EqualTo("Sheet1.B2"));
            Assert.That(changedInput.Validation.IsDisabled, Is.False);
            Assert.That(changedInput.Validation.DisabledValue,
                Is.EqualTo("false"));
            Assert.That(changedInput.Validation.MaxLength, Is.EqualTo("80"));
            Assert.That(changedInput.Events[0].MacroName,
                Is.EqualTo("Standard.Changed"));
            Assert.That(changedLabel.References, Has.Length.EqualTo(1));
            Assert.That(changedLabel.References[0].TargetControl,
                Is.SameAs(changedInput));
            Assert.That(changedList.Options[0].Label, Is.EqualTo("First"));
            Assert.That(changedList.Options[0].IsSelected, Is.False);
            Assert.That(changedList.Options[1].Text, Is.EqualTo("Second"));
            Assert.That(changedForm.Properties[0].Values[0].Value,
                Is.EqualTo("green"));
            Assert.That(changedForm.Properties[1].Values[1].Value,
                Is.EqualTo("3.5"));
        });
    }

    [Test]
    public void PreservesAndReportsInvalidValidationLexicalValues()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:forms><form:form form:name='MainForm'>"
            + "<form:text form:name='Input' xml:id='input' "
            + "form:disabled='sometimes' form:input-required='required' "
            + "form:readonly='perhaps' form:validation='maybe'/>"
            + "</form:form></office:forms><text:p/>"));
        OdfFormValidation validation = Control(document, "Input").Validation;

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsDisabledValid, Is.False);
            Assert.That(validation.IsInputRequiredValid, Is.False);
            Assert.That(validation.IsReadOnlyValid, Is.False);
            Assert.That(validation.IsValidationValueValid, Is.False);
            Assert.That(validation.DisabledValue, Is.EqualTo("sometimes"));
        });
    }

    static OdfFormControl Control(OdfDocument document, string name)
    {
        return document.FormControls.Single(item => item.Name == name);
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

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "forms.fodt",
            new OdfLoadOptions());
    }

    static string FeatureXml()
    {
        return FlatXml(
            "<office:forms><form:form form:name='MainForm' form:id='form1'>"
            + "<form:properties>"
            + "<form:property form:property-name='Theme' "
            + "office:value-type='string' office:string-value='blue'/>"
            + "<form:list-property form:property-name='Thresholds' "
            + "office:value-type='float'><form:list-value office:value='1.5'/>"
            + "<form:list-value office:value='2.5'/></form:list-property>"
            + "</form:properties>"
            + "<form:text form:name='Input' form:id='input' xml:id='inputXml' "
            + "form:label='Input label' form:value='default' "
            + "form:current-value='current' form:min-value='a' form:max-value='z' "
            + "form:title='Input title' style:data-style-name='TextFormat' "
            + "form:linked-cell='Sheet1.A1' "
            + "form:source-cell-range='Sheet1.A1:Sheet1.A3' "
            + "form:data-field='customer_name' form:list-source='Customers' "
            + "form:list-source-type='table-fields' form:bound-column='1' "
            + "xforms:bind='customer' form:xforms-list-source='customer-list' "
            + "form:xforms-submission='submit-customer' form:disabled='true' "
            + "form:input-required='1' form:readonly='false' "
            + "form:validation='true' form:max-length='40'>"
            + "<office:event-listeners><script:event-listener "
            + "script:event-name='dom:change' script:language='ooo:script' "
            + "script:macro-name='Standard.Validate'/></office:event-listeners>"
            + "</form:text>"
            + "<form:fixed-text form:name='Caption' form:label='Customer' "
            + "form:for='input missing'/>"
            + "<form:listbox form:name='Choices' form:id='choices'>"
            + "<form:option form:label='One' form:value='1' "
            + "form:selected='true' form:current-selected='1'>One text</form:option>"
            + "<form:option form:label='Two' form:value='2'>Two</form:option>"
            + "</form:listbox></form:form></office:forms>"
            + "<text:p><draw:control draw:name='Input shape' "
            + "draw:control='input'/></text:p>");
    }

    static string AllControlsXml()
    {
        return FlatXml(
            "<office:forms><form:form form:name='MainForm'>"
            + "<form:button/><form:text/><form:textarea/><form:formatted-text/>"
            + "<form:number/><form:date/><form:time/><form:combobox/>"
            + "<form:listbox/><form:checkbox/><form:password/><form:file/>"
            + "<form:fixed-text/><form:image/><form:radio/><form:frame/>"
            + "<form:image-frame/><form:hidden/><form:value-range/>"
            + "<form:generic-control/><form:grid><form:column form:label='Column'>"
            + "<form:text/></form:column></form:grid>"
            + "</form:form></office:forms><text:p/>");
    }

    static string FlatXml(string content)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:form='" + OdfNamespaces.Form + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:script='" + OdfNamespaces.Script + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:xforms='" + OdfNamespaces.XForms + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " office:version='1.4'>"
            + "<office:body><office:text>" + content
            + "</office:text></office:body></office:document>";
    }
}
