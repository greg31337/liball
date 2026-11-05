using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfRadioButtonAndAppearanceTests
{
    [Test]
    public void RadioGroupMaintainsOneCheckedOptionAndPublicState()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfRadioButtonGroup group = document.AddRadioButtonGroup("contact");
        PdfAcroFormField email = group.AddOption("Email", page, 10, 60, 12, 12);
        PdfAcroFormField phone = group.AddOptionInRectangle("Phone", page, new PdfRectangle(10, 40, 12, 12));

        group.SelectOption("Email");
        Assert.That(group.GetSelectedValue(), Is.EqualTo("Email"));
        Assert.That(email.IsChecked(), Is.True);
        Assert.That(phone.IsChecked(), Is.False);

        phone.SetChecked(true);
        Assert.That(group.GetSelectedValue(), Is.EqualTo("Phone"));
        Assert.That(email.IsChecked(), Is.False);
        Assert.That(phone.IsChecked(), Is.True);

        phone.SetChecked(false);
        Assert.That(group.GetSelectedValue(), Is.Empty);
        Assert.That(email.IsChecked(), Is.False);
        Assert.That(phone.IsChecked(), Is.False);
        Assert.That(group.GetGroupName(), Is.EqualTo("contact"));
        Assert.That(group.GetOptionCount(), Is.EqualTo(2));
        Assert.That(group.GetOption(1), Is.SameAs(phone));
        Assert.That(email.IsRadioButtonOption(), Is.True);
        Assert.That(email.GetFieldName(), Is.EqualTo("contact"));
        Assert.That(email.GetExportValue(), Is.EqualTo("Email"));
        Assert.That(document.AcroForm.GetRadioButtonGroupCount(), Is.EqualTo(1));
        Assert.That(document.AcroForm.GetRadioButtonGroup(0), Is.SameAs(group));
    }

    [Test]
    public void SaveWritesRadioParentWidgetsStatesAndOwnedAppearances()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfRadioButtonGroup group = document.AddRadioButtonGroup("contact");
        PdfAcroFormField email = group.AddOption("Email option", page, 10, 60, 12, 12);
        group.AddOption("Phone", page, 10, 40, 12, 12);
        group.SetAccessibilityName("Preferred contact method");
        email.SetAccessibilityName("Contact by email");
        group.SelectOption("Email option");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(GetObjectBody(pdf, 5), Does.Contain("/Fields [6 0 R ]"));
        Assert.That(GetObjectBody(pdf, 5), Does.Not.Contain("/NeedAppearances true"));
        Assert.That(GetObjectBody(pdf, 6), Does.Contain("/FT /Btn /Ff 32768 /T (contact) /TU (Preferred contact method) /V /Email#20option /Kids [7 0 R 10 0 R ]"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/Parent 6 0 R /F 4 /TU (Contact by email) /AS /Email#20option"));
        Assert.That(GetObjectBody(pdf, 7), Does.Contain("/AP << /N << /Off 9 0 R /Email#20option 8 0 R >> >>"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("/Subtype /Form"));
        Assert.That(GetObjectBody(pdf, 8), Does.Contain("\nf\n"));
        Assert.That(GetObjectBody(pdf, 9), Does.Not.Contain("\nf\n"));
        Assert.That(GetObjectBody(pdf, 10), Does.Contain("/AS /Off"));
        Assert.That(GetObjectBody(pdf, 13), Does.Contain("/Annots [7 0 R 10 0 R ]"));
    }

    [Test]
    public void FieldAppearancesAndAccessibleNamesAreWrittenForEveryTerminalType()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(240, 160);
        PdfAcroFormField text = document.AddTextField("name", page, 10, 120, 100, 20);
        PdfAcroFormField checkbox = document.AddCheckbox("agree", page, 10, 90, 12, 12);
        PdfAcroFormField signature = document.AddSignaturePlaceholderWithLength("signature", page, 10, 40, 120, 30, 4);
        text.SetValue("Ada");
        text.SetAccessibilityName("Full name");
        checkbox.SetChecked(true);
        checkbox.SetAccessibilityName("Accept terms");
        signature.SetAccessibilityName("Author signature");

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(pdf, Does.Contain("/T (name) /F 4 /TU (Full name) /V (Ada)"));
        Assert.That(pdf, Does.Contain("/T (agree) /F 4 /TU (Accept terms) /V /Yes"));
        Assert.That(pdf, Does.Contain("/T (signature) /F 4 /TU (Author signature) /V << /Type /Sig"));
        Assert.That(pdf, Does.Contain("(Ada) Tj"));
        Assert.That(pdf, Does.Contain("(Author signature) Tj"));
        Assert.That(pdf, Does.Contain("/AP << /N"));
    }

    [Test]
    public void FlattenFormDrawsSelectedRadioAndSignatureAppearanceThenRemovesWidgets()
    {
        PdfDocument document = new PdfDocument();
        PdfPage selectedPage = document.AddPage(200, 100);
        PdfPage unselectedPage = document.AddPage(200, 100);
        PdfRadioButtonGroup group = document.AddRadioButtonGroup("choice");
        group.AddOption("One", selectedPage, 10, 60, 12, 12);
        group.AddOption("Two", unselectedPage, 10, 60, 12, 12);
        group.SelectOption("One");
        PdfAcroFormField signature = document.AddSignaturePlaceholderWithLength("signature", selectedPage, 30, 30, 100, 20, 4);
        signature.SetAccessibilityName("Approval signature");

        document.FlattenForm();

        string selectedContent = selectedPage.GetCanvas().ToPdfString();
        string unselectedContent = unselectedPage.GetCanvas().ToPdfString();
        Assert.That(document.AcroForm.GetRadioButtonGroupCount(), Is.EqualTo(0));
        Assert.That(document.AcroForm.GetFieldCount(), Is.EqualTo(0));
        Assert.That(selectedContent, Does.Contain("\nf\n"));
        Assert.That(unselectedContent, Does.Not.Contain("\nf\n"));
        Assert.That(selectedContent, Does.Contain("(Approval signature) Tj"));
        string pdf = Encoding.ASCII.GetString(document.ToBytes());
        Assert.That(pdf, Does.Not.Contain("/Subtype /Widget"));
        Assert.That(pdf, Does.Not.Contain("/AcroForm"));
    }

    [Test]
    public void AccessibleFieldNamesSatisfyFormPreparationRequirement()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfMetadata metadata = new PdfMetadata();
        metadata.Title = "Accessible form";
        document.SetMetadata(metadata);
        document.SetLanguage("en");
        document.AddAccessibilityNode(page, "P");
        PdfAcroFormField text = document.AddTextField("name", page, 10, 70, 80, 14);
        PdfAcroFormField checkbox = document.AddCheckbox("agree", page, 10, 50, 12, 12);
        PdfAcroFormField signature = document.AddSignaturePlaceholderWithLength("signature", page, 10, 10, 100, 20, 4);
        PdfRadioButtonGroup group = document.AddRadioButtonGroup("choice");
        group.AddOption("One", page, 120, 50, 12, 12);
        text.SetAccessibilityName("Full name");
        checkbox.SetAccessibilityName("Agree to terms");
        signature.SetAccessibilityName("Signature");
        group.SetAccessibilityName("Preferred choice");

        PdfAccessibilityPreparationResult result = document.CheckAccessibilityPreparation();

        Assert.That(result.HasIssue("ACCESSIBILITY_FORM_FIELD_DESCRIPTIONS_REQUIRED"), Is.False);
        Assert.That(result.IsReady(), Is.True);
    }

    [Test]
    public void FormFieldAndRadioValidationRejectInvalidNamesStatesAndOwnership()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfPage otherPage = new PdfPage(200, 100);
        PdfAcroFormField text = document.AddTextField("name", page, 10, 70, 80, 14);
        PdfAcroFormField checkbox = document.AddCheckbox("agree", page, 10, 50, 12, 12);

        Assert.Throws<ArgumentException>(() => document.AddCheckbox("section.field", page, 1, 1, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddCheckbox("finite", page, double.NaN, 1, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfAcroFormField("Tx", "direct", page, new PdfRectangle(0, 0, double.PositiveInfinity, 10)));
        Assert.Throws<ArgumentException>(() => document.AddRadioButtonGroup("name"));
        Assert.Throws<InvalidOperationException>(() => text.SetChecked(true));
        Assert.Throws<InvalidOperationException>(() => text.IsChecked());
        Assert.Throws<InvalidOperationException>(() => checkbox.SetValue("yes"));
        Assert.Throws<InvalidOperationException>(() => text.ConfigureSignaturePlaceholder(4));
        Assert.Throws<InvalidOperationException>(() => text.SetSignatureReason("reason"));
        Assert.Throws<ArgumentException>(() => text.SetAccessibilityName(string.Empty));

        PdfRadioButtonGroup group = document.AddRadioButtonGroup("choice");
        PdfAcroFormField first = group.AddOption("One", page, 10, 20, 12, 12);
        Assert.Throws<ArgumentException>(() => group.AddOption("One", page, 30, 20, 12, 12));
        Assert.Throws<ArgumentException>(() => group.AddOption("Two", otherPage, 30, 20, 12, 12));
        group.SelectOption("One");
        Assert.Throws<ArgumentException>(() => group.SelectOption("Missing"));
        Assert.That(group.GetSelectedValue(), Is.EqualTo("One"));
        Assert.That(first.IsChecked(), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => group.GetOption(2));

        PdfDocument emptyGroupDocument = new PdfDocument();
        emptyGroupDocument.AddPage(100, 100);
        emptyGroupDocument.AddRadioButtonGroup("empty");
        Assert.Throws<InvalidOperationException>(() => emptyGroupDocument.ToBytes());

        PdfDocument directSignatureDocument = new PdfDocument();
        PdfPage directPage = directSignatureDocument.AddPage(100, 100);
        PdfAcroForm directForm = new PdfAcroForm();
        directForm.AddField(new PdfAcroFormField("Sig", "direct signature", directPage, new PdfRectangle(0, 0, 20, 10)));
        directSignatureDocument.SetAcroForm(directForm);
        Assert.Throws<InvalidOperationException>(() => directSignatureDocument.ToBytes());

        PdfDocument secondDocument = new PdfDocument();
        secondDocument.AddPage(100, 100);
        Assert.Throws<InvalidOperationException>(() => secondDocument.SetAcroForm(directForm));
    }

    static string GetObjectBody(string pdf, int objectId)
    {
        Match match = Regex.Match(pdf, objectId.ToString() + @" 0 obj\n(?<body>.*?)\nendobj\n", RegexOptions.Singleline);
        Assert.That(match.Success, Is.True, "object " + objectId.ToString() + " should exist");
        return match.Groups["body"].Value;
    }
}
