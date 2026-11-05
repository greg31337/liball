using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAdditionalDocumentTemplateTests
{
    [Test]
    public void ContractBuildsPartiesClausesAndSignatures()
    {
        PdfContract contract = new PdfContract("Services Agreement");
        contract.SetParties("Acme LLC", "Buyer Co");
        contract.SetEffectiveDate("August 26, 2026");
        contract.AddClause(new PdfContractClause("Services", "Acme will provide document services."));
        contract.AddClause(new PdfContractClause("Payment", "Buyer will pay within thirty days."));
        contract.AddSignature(new PdfContractSignature("Acme LLC", "Ada Lovelace"));

        PdfDocument document = contract.Build();
        string content = document.Pages[0].Canvas.ToPdfString();

        Assert.That(content, Does.Contain("(Services Agreement) Tj"));
        Assert.That(content, Does.Contain("(Between Acme LLC and Buyer Co) Tj"));
        Assert.That(content, Does.Contain("(1. Services) Tj"));
        Assert.That(content, Does.Contain("(Buyer will pay within thirty days.) Tj"));
        Assert.That(content, Does.Contain("(Ada Lovelace, Acme LLC) Tj"));
    }

    [Test]
    public void PackingSlipBuildsShipmentAndQuantityTable()
    {
        PdfPackingSlip slip = new PdfPackingSlip("PS-104");
        slip.SetSender("North Warehouse");
        slip.SetRecipient("Buyer Co");
        slip.SetShipmentDetails("2026-08-26", "Parcel Express", "TRACK-9");
        slip.AddItem(new PdfPackingSlipItem("SKU-1", "Blue widgets", 4));
        slip.SetNotes("Two cartons.");

        string content = slip.Build().Pages[0].Canvas.ToPdfString();

        Assert.That(content, Does.Contain("(PACKING SLIP PS-104) Tj"));
        Assert.That(content, Does.Contain("(From: North Warehouse) Tj"));
        Assert.That(content, Does.Contain("(Tracking: TRACK-9) Tj"));
        Assert.That(content, Does.Contain("(Blue widgets) Tj"));
        Assert.That(content, Does.Contain("(4) Tj"));
        Assert.That(content, Does.Contain("(Notes: Two cartons.) Tj"));
    }

    [Test]
    public void StatementCalculatesAndRendersClosingBalance()
    {
        PdfStatement statement = new PdfStatement("Ada Lovelace", "A-100");
        statement.SetPeriod("2026-08-01", "2026-08-31");
        statement.SetOpeningBalance(100);
        statement.SetCurrencySymbol("EUR ");
        statement.AddEntry(new PdfStatementEntry("2026-08-10", "Payment", -25));
        statement.AddEntry(new PdfStatementEntry("2026-08-20", "Service", 12.5));

        PdfDocument document = statement.Build();
        string content = document.Pages[0].Canvas.ToPdfString();

        Assert.That(statement.GetClosingBalance(), Is.EqualTo(87.5));
        Assert.That(content, Does.Contain("(ACCOUNT STATEMENT) Tj"));
        Assert.That(content, Does.Contain("(Payment) Tj"));
        Assert.That(content, Does.Contain("(EUR -25.00) Tj"));
        Assert.That(content, Does.Contain("(Closing balance: EUR 87.50) Tj"));
    }

    [Test]
    public void WorksheetBuildsPromptsResponseLinesAndFooter()
    {
        PdfWorksheet worksheet = new PdfWorksheet("Geometry Review");
        worksheet.SetInstructions("Show your work.");
        worksheet.AddQuestion(new PdfWorksheetQuestion("What is the area of the square?", 2));
        worksheet.SetFooter("Geometry 101");

        string content = worksheet.Build().Pages[0].Canvas.ToPdfString();

        Assert.That(content, Does.Contain("(Geometry Review) Tj"));
        Assert.That(content, Does.Contain("(Name: __________________________ Date: ______________) Tj"));
        Assert.That(content, Does.Contain("(1. What is the area of the square?) Tj"));
        Assert.That(content, Does.Contain("(Geometry 101 1/1) Tj"));
    }

    [Test]
    public void FormTemplateBuildsLabeledInteractiveFieldKinds()
    {
        PdfFormTemplate form = new PdfFormTemplate("Enrollment Form");
        form.SetInstructions("Complete every field.");
        form.AddField(new PdfFormTemplateField("full_name", "Full name", PdfFormTemplateFieldKind.Text));
        form.AddField(new PdfFormTemplateField("accepted", "Accept terms", PdfFormTemplateFieldKind.Checkbox));
        form.AddField(new PdfFormTemplateField("approval", "Approval signature", PdfFormTemplateFieldKind.Signature));

        PdfDocument document = form.Build();
        string content = document.Pages[0].Canvas.ToPdfString();

        Assert.That(content, Does.Contain("(Enrollment Form) Tj"));
        Assert.That(content, Does.Contain("(Full name) Tj"));
        Assert.That(document.AcroForm.GetFieldCount(), Is.EqualTo(3));
        Assert.That(document.AcroForm.GetField(0).GetFieldType(), Is.EqualTo("Tx"));
        Assert.That(document.AcroForm.GetField(1).GetFieldType(), Is.EqualTo("Btn"));
        Assert.That(document.AcroForm.GetField(2).GetFieldType(), Is.EqualTo("Sig"));
    }

    [Test]
    public void BadgeBuildsCompactIdentificationCard()
    {
        PdfBadge badge = new PdfBadge("Grace Hopper");
        badge.SetTitle("Keynote Speaker");
        badge.SetOrganization("Computing Conference");
        badge.SetIdentifier("BADGE-42");

        PdfDocument document = badge.Build();
        string content = document.Pages[0].Canvas.ToPdfString();

        Assert.That(document.Pages[0].Width, Is.EqualTo(252));
        Assert.That(document.Pages[0].Height, Is.EqualTo(144));
        Assert.That(content, Does.Contain("(Grace Hopper) Tj"));
        Assert.That(content, Does.Contain("(Keynote Speaker) Tj"));
        Assert.That(content, Does.Contain("(Computing Conference) Tj"));
        Assert.That(content, Does.Contain("(BADGE-42) Tj"));
    }

    [Test]
    public void AdditionalTemplatesRejectInvalidValuesAndLateChanges()
    {
        PdfContract contract = new PdfContract("Contract");
        PdfPackingSlip slip = new PdfPackingSlip("PS-1");
        PdfStatement statement = new PdfStatement("Name", "Account");
        PdfWorksheet worksheet = new PdfWorksheet("Worksheet");
        PdfFormTemplate form = new PdfFormTemplate("Form");
        PdfBadge badge = new PdfBadge("Name");

        Assert.Throws<ArgumentNullException>(() => new PdfContract(null!));
        Assert.Throws<ArgumentNullException>(() => contract.AddClause(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfPackingSlipItem("x", "y", 0));
        Assert.Throws<ArgumentNullException>(() => slip.SetRecipient(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfStatementEntry("date", "entry", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => statement.SetOpeningBalance(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfWorksheetQuestion("Question", 0));
        Assert.Throws<ArgumentNullException>(() => worksheet.AddQuestion(null!));
        Assert.Throws<ArgumentException>(() => form.AddField(new PdfFormTemplateField("bad", "Bad", (PdfFormTemplateFieldKind)99)));
        form.AddField(new PdfFormTemplateField("name", "Name", PdfFormTemplateFieldKind.Text));
        Assert.Throws<ArgumentException>(() => form.AddField(new PdfFormTemplateField("name", "Other", PdfFormTemplateFieldKind.Text)));
        Assert.Throws<ArgumentNullException>(() => badge.SetTitle(null!));

        contract.Build();
        slip.Build();
        statement.Build();
        worksheet.Build();
        form.Build();
        badge.Build();

        Assert.Throws<InvalidOperationException>(() => contract.SetEffectiveDate("late"));
        Assert.Throws<InvalidOperationException>(() => slip.SetNotes("late"));
        Assert.Throws<InvalidOperationException>(() => statement.SetNotes("late"));
        Assert.Throws<InvalidOperationException>(() => worksheet.SetFooter("late"));
        Assert.Throws<InvalidOperationException>(() => form.SetInstructions("late"));
        Assert.Throws<InvalidOperationException>(() => badge.SetIdentifier("late"));
    }
}
