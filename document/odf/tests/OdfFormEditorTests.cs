using System.Text;

namespace odf.Tests;

public class OdfFormEditorTests
{
    [Test]
    public void CreatesEverySupportedFormControlKind()
    {
        OdfDocument document = Read(EmptyTextXml());
        IOdfFormEditor editor = new OdfFormEditor();
        OdfFormControl form = editor.CreateFormControl(
            document,
            null,
            OdfFormControlKind.Form,
            "AllControls");
        OdfFormControlKind[] directKinds =
        [
            OdfFormControlKind.Button,
            OdfFormControlKind.Text,
            OdfFormControlKind.TextArea,
            OdfFormControlKind.CheckBox,
            OdfFormControlKind.RadioButton,
            OdfFormControlKind.ListBox,
            OdfFormControlKind.ComboBox,
            OdfFormControlKind.ValueRange,
            OdfFormControlKind.Image,
            OdfFormControlKind.File,
            OdfFormControlKind.Date,
            OdfFormControlKind.Time,
            OdfFormControlKind.Frame,
            OdfFormControlKind.Hidden,
            OdfFormControlKind.Label,
            OdfFormControlKind.FormattedText,
            OdfFormControlKind.Number,
            OdfFormControlKind.Password,
            OdfFormControlKind.ImageFrame,
            OdfFormControlKind.Grid,
            OdfFormControlKind.GenericControl
        ];
        OdfFormControl? grid = null;
        int index = 0;
        while (index < directKinds.Length)
        {
            OdfFormControl created = editor.CreateFormControl(
                document,
                form,
                directKinds[index],
                "Control" + index);
            if (created.Kind == OdfFormControlKind.Grid)
            {
                grid = created;
            }
            index = index + 1;
        }
        Assert.That(grid, Is.Not.Null);
        OdfFormControl column = editor.CreateFormControl(
            document,
            grid,
            OdfFormControlKind.Column,
            "GridColumn");
        editor.CreateFormControl(
            document,
            column,
            OdfFormControlKind.Text,
            "ColumnText");

        OdfDocument reread = Read(Write(document));
        OdfFormControlKind[] actualKinds = new OdfFormEditor()
            .GetFormControls(reread)
            .Select(item => item.Kind)
            .Distinct()
            .OrderBy(item => (int)item)
            .ToArray();
        OdfFormControlKind[] expectedKinds = Enum
            .GetValues<OdfFormControlKind>()
            .Where(item => item != OdfFormControlKind.Unknown)
            .OrderBy(item => (int)item)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actualKinds, Is.EqualTo(expectedKinds));
            Assert.That(reread.FormControls.Single(item =>
                    item.Kind == OdfFormControlKind.Column).Parent?.Kind,
                Is.EqualTo(OdfFormControlKind.Grid));
            Assert.That(() => editor.CreateFormControl(
                    document,
                    form,
                    OdfFormControlKind.Unknown,
                    "Unknown"),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void RoundTripsEveryFormFieldPropertyOptionAndEventShape()
    {
        OdfDocument document = Read(EmptyTextXml());
        IOdfFormEditor editor = new OdfFormEditor();
        OdfFormControl form = editor.CreateFormControl(
            document, null, OdfFormControlKind.Form, "Main");
        OdfFormControl input = editor.CreateFormControl(
            document, form, OdfFormControlKind.Text, "Input");
        OdfFormControl list = editor.CreateFormControl(
            document, form, OdfFormControlKind.ListBox, "List");
        OdfFormControl combo = editor.CreateFormControl(
            document, form, OdfFormControlKind.ComboBox, "Combo");

        input.Id = "input-id";
        input.XmlId = "input-xml";
        input.ControlImplementation = "app:input";
        input.Label = "Input label";
        input.For = "target-id";
        input.Value = "default";
        input.CurrentValue = "current";
        input.MinValue = "minimum";
        input.MaxValue = "maximum";
        input.Title = "Input title";
        input.DataStyleName = "InputStyle";
        input.Binding.LinkedCell = "Sheet1.A1";
        input.Binding.SourceCellRange = "Sheet1.A1:Sheet1.A4";
        input.Binding.DataField = "customer_name";
        input.Binding.ListSource = "Customers";
        input.Binding.ListSourceType = "table-fields";
        input.Binding.BoundColumn = "1";
        input.Binding.XFormsBind = "customer-bind";
        input.Binding.XFormsListSource = "customer-list";
        input.Binding.XFormsSubmission = "customer-submit";
        input.Validation.IsDisabled = true;
        input.Validation.IsInputRequired = true;
        input.Validation.IsReadOnly = true;
        input.Validation.IsValidationEnabled = true;
        input.Validation.MaxLength = "80";

        AddScalarProperty(editor, document, input,
            "Boolean", "boolean", "true", "");
        AddScalarProperty(editor, document, input,
            "Date", "date", "2026-09-02", "");
        AddScalarProperty(editor, document, input,
            "Time", "time", "PT12H30M", "");
        AddScalarProperty(editor, document, input,
            "String", "string", "text", "");
        AddScalarProperty(editor, document, input,
            "Float", "float", "12.5", "");
        AddScalarProperty(editor, document, input,
            "Currency", "currency", "19.95", "USD");
        editor.AddFormProperty(
            document,
            input,
            new OdfFormProperty
            {
                Name = "ListValues",
                ValueType = "float",
                IsList = true,
                Values =
                [
                    new OdfFormPropertyValue { Value = "1.5" },
                    new OdfFormPropertyValue { Value = "2.5" }
                ]
            });
        editor.AddFormOption(
            document,
            list,
            new OdfFormOption
            {
                Label = "List option",
                Value = "list-value",
                Text = "List text",
                IsSelected = true,
                IsCurrentSelected = true
            });
        editor.AddFormOption(
            document,
            combo,
            new OdfFormOption
            {
                Label = "Combo item",
                Value = "combo-value",
                Text = "Combo text"
            });
        editor.AddFormEvent(
            document,
            input,
            new OdfDrawingEvent
            {
                Kind = OdfDrawingEventKind.Script,
                EventName = "dom:change",
                Language = "ooo:script",
                MacroName = "Standard.Module.Validate",
                Href = "#validation-script",
                Show = "embed",
                Actuate = "onRequest"
            });
        editor.AddFormEvent(
            document,
            input,
            new OdfDrawingEvent
            {
                Kind = OdfDrawingEventKind.Presentation,
                EventName = "dom:click",
                Action = "show",
                Effect = "fade",
                Direction = "from-left",
                Speed = "medium",
                StartScale = "75%",
                Verb = "open"
            });
        editor.ApplyFormChanges(document);

        OdfDocument reread = Read(Write(document));
        IOdfFormEditor rereadEditor = new OdfFormEditor();
        OdfFormControl changed = rereadEditor.FindFormControl(
            reread, "Input")!;
        OdfFormControl changedList = rereadEditor.FindFormControl(
            reread, "List")!;
        OdfFormControl changedCombo = rereadEditor.FindFormControl(
            reread, "Combo")!;

        Assert.Multiple(() =>
        {
            Assert.That(changed.Id, Is.EqualTo("input-id"));
            Assert.That(changed.XmlId, Is.EqualTo("input-xml"));
            Assert.That(changed.ControlImplementation,
                Is.EqualTo("app:input"));
            Assert.That(changed.Label, Is.EqualTo("Input label"));
            Assert.That(changed.For, Is.EqualTo("target-id"));
            Assert.That(changed.Value, Is.EqualTo("default"));
            Assert.That(changed.CurrentValue, Is.EqualTo("current"));
            Assert.That(changed.MinValue, Is.EqualTo("minimum"));
            Assert.That(changed.MaxValue, Is.EqualTo("maximum"));
            Assert.That(changed.Title, Is.EqualTo("Input title"));
            Assert.That(changed.DataStyleName, Is.EqualTo("InputStyle"));
            Assert.That(changed.Binding.LinkedCell, Is.EqualTo("Sheet1.A1"));
            Assert.That(changed.Binding.SourceCellRange,
                Is.EqualTo("Sheet1.A1:Sheet1.A4"));
            Assert.That(changed.Binding.DataField,
                Is.EqualTo("customer_name"));
            Assert.That(changed.Binding.ListSource, Is.EqualTo("Customers"));
            Assert.That(changed.Binding.ListSourceType,
                Is.EqualTo("table-fields"));
            Assert.That(changed.Binding.BoundColumn, Is.EqualTo("1"));
            Assert.That(changed.Binding.XFormsBind,
                Is.EqualTo("customer-bind"));
            Assert.That(changed.Binding.XFormsListSource,
                Is.EqualTo("customer-list"));
            Assert.That(changed.Binding.XFormsSubmission,
                Is.EqualTo("customer-submit"));
            Assert.That(changed.Binding.IsXFormsBindBroken, Is.True);
            Assert.That(changed.Binding.IsXFormsListSourceBroken, Is.True);
            Assert.That(changed.Binding.IsXFormsSubmissionBroken, Is.True);
            Assert.That(changed.Validation.IsDisabled, Is.True);
            Assert.That(changed.Validation.IsInputRequired, Is.True);
            Assert.That(changed.Validation.IsReadOnly, Is.True);
            Assert.That(changed.Validation.IsValidationEnabled, Is.True);
            Assert.That(changed.Validation.MaxLength, Is.EqualTo("80"));
            Assert.That(changed.Properties.Select(item => item.ValueType),
                Is.EqualTo(new[]
                {
                    "boolean", "date", "time", "string",
                    "float", "currency", "float"
                }));
            Assert.That(changed.Properties.Single(item =>
                    item.Name == "Currency").Values[0].Currency,
                Is.EqualTo("USD"));
            Assert.That(changed.Properties.Single(item =>
                    item.Name == "ListValues").Values.Select(item => item.Value),
                Is.EqualTo(new[] { "1.5", "2.5" }));
            Assert.That(changedList.Options[0].IsOption, Is.True);
            Assert.That(changedList.Options[0].IsSelected, Is.True);
            Assert.That(changedList.Options[0].IsCurrentSelected, Is.True);
            Assert.That(changedCombo.Options[0].IsOption, Is.False);
            Assert.That(changedCombo.Options[0].Text,
                Is.EqualTo("Combo text"));
            Assert.That(changed.Events.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfDrawingEventKind.Script,
                    OdfDrawingEventKind.Presentation
                }));
            Assert.That(changed.Events[0].MacroName,
                Is.EqualTo("Standard.Module.Validate"));
            Assert.That(changed.Events[1].Action, Is.EqualTo("show"));
            Assert.That(changed.Events[1].Verb, Is.EqualTo("open"));
        });
    }

    [Test]
    public void CreatesEditsAndDeletesFormContent()
    {
        OdfDocument document = Read(EmptyTextXml());
        IOdfFormEditor editor = new OdfFormEditor();
        OdfFormControl[] emptySnapshot = editor.GetFormControls(document);

        OdfFormControl form = editor.CreateFormControl(
            document,
            null,
            OdfFormControlKind.Form,
            "MainForm");
        OdfFormControl list = editor.CreateFormControl(
            document,
            form,
            OdfFormControlKind.ListBox,
            "Choice");
        list.Id = "choice-id";
        list.Label = "Choose";
        list.Binding.LinkedCell = "Sheet1.A1";
        list.Validation.IsInputRequired = true;
        OdfFormProperty property = editor.AddFormProperty(
            document,
            list,
            new OdfFormProperty
            {
                Name = "Theme",
                ValueType = "string",
                Values =
                [
                    new OdfFormPropertyValue { Value = "blue" }
                ]
            });
        OdfFormOption option = editor.AddFormOption(
            document,
            list,
            new OdfFormOption
            {
                Label = "First",
                Value = "one",
                Text = "First item",
                IsSelected = true
            });
        OdfDrawingEvent eventDescription = editor.AddFormEvent(
            document,
            list,
            new OdfDrawingEvent
            {
                Kind = OdfDrawingEventKind.Script,
                EventName = "dom:click",
                Language = "ooo:script",
                MacroName = "Standard.Module1.Run"
            });
        property.Values[0].Value = "green";
        option.Text = "Updated item";
        eventDescription.MacroName = "Standard.Module1.Updated";
        editor.ApplyFormChanges(document);

        OdfDocument reread = Read(Write(document));
        OdfFormControl changed = new OdfFormEditor()
            .FindFormControl(reread, "Choice")!;
        Assert.Multiple(() =>
        {
            Assert.That(emptySnapshot, Is.Empty);
            Assert.That(editor.GetFormControls(document),
                Is.Not.SameAs(document.FormControls));
            Assert.That(document.FormControls, Has.Length.EqualTo(2));
            Assert.That(form.Children.Single(), Is.SameAs(list));
            Assert.That(changed.Kind, Is.EqualTo(OdfFormControlKind.ListBox));
            Assert.That(changed.Id, Is.EqualTo("choice-id"));
            Assert.That(changed.Label, Is.EqualTo("Choose"));
            Assert.That(changed.Binding.LinkedCell, Is.EqualTo("Sheet1.A1"));
            Assert.That(changed.Validation.IsInputRequired, Is.True);
            Assert.That(changed.Properties.Single().Values.Single().Value,
                Is.EqualTo("green"));
            Assert.That(changed.Options.Single().Text,
                Is.EqualTo("Updated item"));
            Assert.That(changed.Options.Single().IsSelected, Is.True);
            Assert.That(changed.Events.Single().MacroName,
                Is.EqualTo("Standard.Module1.Updated"));
        });

        IOdfFormEditor deleteEditor = new OdfFormEditor();
        OdfFormControl deleteList = deleteEditor.FindFormControl(
            reread,
            "Choice")!;
        deleteEditor.DeleteFormProperty(
            reread,
            deleteList,
            deleteList.Properties[0]);
        deleteEditor.DeleteFormOption(
            reread,
            deleteList,
            deleteList.Options[0]);
        deleteEditor.DeleteFormEvent(
            reread,
            deleteList,
            deleteList.Events[0]);
        deleteEditor.DeleteFormControl(reread, deleteList);
        OdfDocument deleted = Read(Write(reread));
        Assert.Multiple(() =>
        {
            Assert.That(deleteEditor.FindFormControl(reread, "Choice"),
                Is.Null);
            Assert.That(deleted.FormControls, Has.Length.EqualTo(1));
            Assert.That(deleted.FormControls[0].Children, Is.Empty);
        });
    }

    [Test]
    public void EnforcesControlHierarchyAndAmbiguousNameLookup()
    {
        OdfDocument document = Read(EmptyTextXml());
        IOdfFormEditor editor = new OdfFormEditor();
        OdfFormControl form = editor.CreateFormControl(
            document,
            null,
            OdfFormControlKind.Form,
            "Main");
        editor.CreateFormControl(
            document,
            form,
            OdfFormControlKind.Text,
            "Duplicate");
        editor.CreateFormControl(
            document,
            form,
            OdfFormControlKind.Button,
            "Duplicate");

        Assert.Multiple(() =>
        {
            Assert.That(editor.FindFormControl(document, "Duplicate"),
                Is.Null);
            Assert.That(() => editor.CreateFormControl(
                    document,
                    null,
                    OdfFormControlKind.Button,
                    "Bad"),
                Throws.TypeOf<OdfEditException>());
            Assert.That(() => editor.CreateFormControl(
                    document,
                    form,
                    OdfFormControlKind.Column,
                    "BadColumn"),
                Throws.TypeOf<OdfEditException>());
            Assert.That(() => editor.AddFormOption(
                    document,
                    form,
                    new OdfFormOption()),
                Throws.TypeOf<OdfEditException>());
        });
    }

    [Test]
    public void KeepsTheFlatControlCollectionInDocumentOrder()
    {
        OdfDocument document = Read(EmptyTextXml());
        IOdfFormEditor editor = new OdfFormEditor();
        OdfFormControl first = editor.CreateFormControl(
            document,
            null,
            OdfFormControlKind.Form,
            "First");
        editor.CreateFormControl(
            document,
            null,
            OdfFormControlKind.Form,
            "Second");
        editor.CreateFormControl(
            document,
            first,
            OdfFormControlKind.Text,
            "FirstChild");

        Assert.That(editor.GetFormControls(document).Select(item => item.Name),
            Is.EqualTo(new[] { "First", "FirstChild", "Second" }));
    }

    static OdfDocument Read(string xml)
    {
        return Read(Encoding.UTF8.GetBytes(xml));
    }

    static void AddScalarProperty(
        IOdfFormEditor editor,
        OdfDocument document,
        OdfFormControl control,
        string name,
        string valueType,
        string value,
        string currency)
    {
        editor.AddFormProperty(
            document,
            control,
            new OdfFormProperty
            {
                Name = name,
                ValueType = valueType,
                Values =
                [
                    new OdfFormPropertyValue
                    {
                        Value = value,
                        Currency = currency
                    }
                ]
            });
    }

    static OdfDocument Read(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "forms.fodt",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions { Deterministic = true });
    }

    static string EmptyTextXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text/>"
            + "</office:body></office:document>";
    }
}
