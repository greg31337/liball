using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfDocumentTemplateTests
{
    [Test]
    public void Certificate_BuildRendersLandscapeCertificate()
    {
        PdfCertificate certificate = new PdfCertificate("Jane Doe", "Certificate of Completion");

        certificate.SetDescription("For completing the PDF template track.");
        certificate.SetIssuer("OpenAI Academy");
        certificate.SetDate("July 31, 2026");
        PdfDocument document = certificate.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages[0].Width, Is.EqualTo(792));
        Assert.That(document.Pages[0].Height, Is.EqualTo(612));
        Assert.That(content, Does.Contain("(Certificate of Completion) Tj"));
        Assert.That(content, Does.Contain("(Presented to) Tj"));
        Assert.That(content, Does.Contain("(Jane Doe) Tj"));
        Assert.That(content, Does.Contain("(For completing the PDF template track.) Tj"));
        Assert.That(content, Does.Contain("(OpenAI Academy) Tj"));
        Assert.That(content, Does.Contain("(July 31, 2026) Tj"));
        Assert.That(content, Does.Contain("48 48 696 516 re S"));
    }

    [Test]
    public void Letter_BuildRendersBusinessLetter()
    {
        PdfLetter letter = new PdfLetter();

        letter.SetSender("Acme LLC");
        letter.SetDate("July 31, 2026");
        letter.SetRecipient("Buyer Co");
        letter.SetSubject("Project update");
        letter.SetBody("The project remains on schedule.");
        letter.SetClosing("Sincerely,");
        letter.SetSignature("Ada");
        PdfDocument document = letter.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("(Acme LLC) Tj"));
        Assert.That(content, Does.Contain("(July 31, 2026) Tj"));
        Assert.That(content, Does.Contain("(Buyer Co) Tj"));
        Assert.That(content, Does.Contain("(Project update) Tj"));
        Assert.That(content, Does.Contain("(The project remains on schedule.) Tj"));
        Assert.That(content, Does.Contain("(Sincerely,) Tj"));
        Assert.That(content, Does.Contain("(Ada) Tj"));
    }

    [Test]
    public void Receipt_BuildRendersReceiptSizedDocumentWithTotals()
    {
        PdfReceipt receipt = new PdfReceipt("Corner Store");

        receipt.SetReceiptDetails("R-100", "2026-07-31");
        receipt.AddLine("Coffee", 2, 3.5);
        receipt.AddLine("Bagel", 1, 4);
        receipt.SetNotes("Thank you.");
        PdfDocument document = receipt.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages[0].Width, Is.EqualTo(PdfPageSize.Receipt80Width));
        Assert.That(content, Does.Contain("(Corner Store) Tj"));
        Assert.That(content, Does.Contain("(Receipt R-100) Tj"));
        Assert.That(content, Does.Contain("(Coffee) Tj"));
        Assert.That(content, Does.Contain("($7.00) Tj"));
        Assert.That(content, Does.Contain("(Total $11.00) Tj"));
        Assert.That(content, Does.Contain("(Thank you.) Tj"));
    }

    [Test]
    public void LabelSheet_BuildRendersLabelsInGrid()
    {
        PdfLabelSheet labels = new PdfLabelSheet(2, 2);

        labels.AddLabel("Alpha");
        labels.AddLabel("Beta");
        labels.AddLabel("Gamma");
        PdfDocument document = labels.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(Alpha) Tj"));
        Assert.That(content, Does.Contain("(Beta) Tj"));
        Assert.That(content, Does.Contain("(Gamma) Tj"));
        Assert.That(content, Does.Contain("36 396 270 360 re S"));
        Assert.That(content, Does.Contain("306 396 270 360 re S"));
    }

    [Test]
    public void LabelSheet_CapacityHandlesLargeDimensions()
    {
        PdfLabelSheet labels = new PdfLabelSheet(int.MaxValue, int.MaxValue);

        labels.AddLabel("first");

        Assert.That(labels.Labels.Count, Is.EqualTo(1));
    }

    [Test]
    public void DocumentTemplatesRejectInvalidInputs()
    {
        PdfCertificate certificate = new PdfCertificate("Jane", "Award");
        PdfLetter letter = new PdfLetter();
        PdfReceipt receipt = new PdfReceipt("Store");
        PdfLabelSheet labels = new PdfLabelSheet(1, 1);

        Assert.Throws<ArgumentNullException>(() => new PdfCertificate(null!, "Award"));
        Assert.Throws<ArgumentNullException>(() => new PdfCertificate("Jane", null!));
        Assert.Throws<ArgumentNullException>(() => certificate.SetDescription(null!));
        Assert.Throws<ArgumentNullException>(() => certificate.SetIssuer(null!));
        Assert.Throws<ArgumentNullException>(() => certificate.SetDate(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetSender(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetRecipient(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetDate(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetSubject(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetBody(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetClosing(null!));
        Assert.Throws<ArgumentNullException>(() => letter.SetSignature(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfReceipt(null!));
        Assert.Throws<ArgumentNullException>(() => receipt.SetReceiptDetails(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => receipt.SetReceiptDetails("x", null!));
        Assert.Throws<ArgumentNullException>(() => receipt.SetCurrencySymbol(null!));
        Assert.Throws<ArgumentNullException>(() => receipt.SetNotes(null!));
        Assert.Throws<ArgumentNullException>(() => receipt.AddLine(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => receipt.AddLine("x", -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => receipt.AddLine("x", 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLabelSheet(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfLabelSheet(1, 0));
        Assert.Throws<ArgumentNullException>(() => labels.AddLabel(null!));

        labels.AddLabel("Only");
        Assert.Throws<InvalidOperationException>(() => labels.AddLabel("Too many"));
    }

    [Test]
    public void DocumentTemplatesRejectChangesAfterBuild()
    {
        PdfCertificate certificate = new PdfCertificate("Jane", "Award");
        PdfLetter letter = new PdfLetter();
        PdfReceipt receipt = new PdfReceipt("Store");
        PdfLabelSheet labels = new PdfLabelSheet(1, 1);

        certificate.Build();
        letter.Build();
        receipt.Build();
        labels.Build();

        Assert.Throws<InvalidOperationException>(() => certificate.SetIssuer("late"));
        Assert.Throws<InvalidOperationException>(() => letter.SetBody("late"));
        Assert.Throws<InvalidOperationException>(() => receipt.AddLine("late", 1, 1));
        Assert.Throws<InvalidOperationException>(() => labels.AddLabel("late"));
    }
}
