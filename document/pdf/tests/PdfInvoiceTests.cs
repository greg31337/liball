using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfInvoiceTests
{
    [Test]
    public void Invoice_BuildRendersPartiesLinesTotalsTermsAndFurniture()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfParty seller = new PdfParty("Acme LLC");
        PdfParty buyer = new PdfParty("Buyer Co");
        PdfInvoiceLine line = new PdfInvoiceLine("Consulting services", 2, 100);

        seller.AddLine("1 Market Street");
        seller.SetTaxIdentifier("US-123");
        buyer.AddLine("9 Main Road");
        line.SetDiscount(10);
        line.SetTaxRate(0.10);
        invoice.SetSeller(seller);
        invoice.SetBuyer(buyer);
        invoice.SetInvoiceDetails("INV-1001", "2026-07-31", "2026-08-30");
        invoice.AddLine(line);
        invoice.SetPaymentTerms("Net 30");
        invoice.SetRemittanceInstructions("Wire to account ending 4242.");
        PdfDocument document = invoice.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(INVOICE) Tj"));
        Assert.That(content, Does.Contain("(INV-1001) Tj"));
        Assert.That(content, Does.Contain("(2026-08-30) Tj"));
        Assert.That(content, Does.Contain("(Acme LLC) Tj"));
        Assert.That(content, Does.Contain("(Tax ID: US-123) Tj"));
        Assert.That(content, Does.Contain("(Buyer Co) Tj"));
        Assert.That(content, Does.Contain("(Consulting services) Tj"));
        Assert.That(content, Does.Contain("($100.00) Tj"));
        Assert.That(content, Does.Contain("($10.00) Tj"));
        Assert.That(content, Does.Contain("(10%) Tj"));
        Assert.That(content, Does.Contain("($209.00) Tj"));
        Assert.That(content, Does.Contain("(Payment Terms) Tj"));
        Assert.That(content, Does.Contain("(Net 30) Tj"));
        Assert.That(content, Does.Contain("(Remittance) Tj"));
        Assert.That(content, Does.Contain("(Page 1 of 1) Tj"));
    }

    [Test]
    public void Invoice_UsesExplicitTotalsAndCurrencySymbol()
    {
        PdfInvoice invoice = new PdfInvoice();

        invoice.SetCurrencySymbol("EUR ");
        invoice.AddLine(new PdfInvoiceLine("Subscription", 1, 90));
        invoice.SetTotals(new PdfInvoiceTotals(100, 5, 10, 105));
        PdfDocument document = invoice.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(invoice.FormatCurrency(12.5), Is.EqualTo("EUR 12.50"));
        Assert.That(content, Does.Contain("(EUR 100.00) Tj"));
        Assert.That(content, Does.Contain("(EUR 5.00) Tj"));
        Assert.That(content, Does.Contain("(EUR 10.00) Tj"));
        Assert.That(content, Does.Contain("(EUR 105.00) Tj"));
    }

    [Test]
    public void Invoice_LogoAndShippingAndNotesRenderWhenProvided()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfParty shipping = new PdfParty("Warehouse");
        PdfImage logo = invoice.Document.AddImage(1, 1, new byte[] { 0, 0, 0 });

        shipping.AddLine("Dock 4");
        invoice.SetLogo(logo, 24, 24);
        invoice.SetShipping(shipping);
        invoice.SetNotes("Thank you for your business.");
        invoice.Build();

        string content = invoice.Document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content, Does.Contain("(Ship To) Tj"));
        Assert.That(content, Does.Contain("(Warehouse) Tj"));
        Assert.That(content, Does.Contain("(Notes) Tj"));
        Assert.That(content, Does.Contain("(Thank you for your business.) Tj"));
    }

    [Test]
    public void InvoiceTotals_ComputesFromLines()
    {
        PdfInvoiceLine first = new PdfInvoiceLine("First", 2, 50);
        PdfInvoiceLine second = new PdfInvoiceLine("Second", 1, 30);
        List<PdfInvoiceLine> lines = new List<PdfInvoiceLine>();

        first.SetDiscount(10);
        first.SetTaxRate(0.1);
        second.SetTaxRate(0.2);
        lines.Add(first);
        lines.Add(second);
        PdfInvoiceTotals totals = PdfInvoiceTotals.FromLines(lines);

        Assert.That(totals.Subtotal, Is.EqualTo(130));
        Assert.That(totals.DiscountTotal, Is.EqualTo(10));
        Assert.That(totals.TaxTotal, Is.EqualTo(15));
        Assert.That(totals.GrandTotal, Is.EqualTo(135));
    }

    [Test]
    public void InvoiceHelpersRejectInvalidInputs()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfParty party = new PdfParty("Party");
        PdfInvoiceLine line = new PdfInvoiceLine("Line", 1, 2);
        PdfImage image = invoice.Document.AddImage(1, 1, new byte[] { 0, 0, 0 });

        Assert.Throws<ArgumentNullException>(() => new PdfParty(null!));
        Assert.Throws<ArgumentNullException>(() => party.AddLine(null!));
        Assert.Throws<ArgumentNullException>(() => party.SetTaxIdentifier(null!));
        Assert.Throws<ArgumentNullException>(() => party.SetEmail(null!));
        Assert.Throws<ArgumentNullException>(() => party.CreateBlock(null!, BodyStyle(), BodyStyle()));
        Assert.Throws<ArgumentNullException>(() => party.CreateBlock("x", null!, BodyStyle()));
        Assert.Throws<ArgumentNullException>(() => party.CreateBlock("x", BodyStyle(), null!));
        Assert.Throws<ArgumentNullException>(() => new PdfInvoiceLine(null!, 1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceLine("x", -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceLine("x", 1, -2));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.SetDiscount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.SetTaxRate(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceTotals(-1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceTotals(0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceTotals(0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceTotals(0, 0, 0, -1));
        Assert.Throws<ArgumentNullException>(() => PdfInvoiceTotals.FromLines(null!));
        Assert.Throws<ArgumentNullException>(() => PdfInvoiceTotals.FromLines(new List<PdfInvoiceLine> { null! }));
        Assert.Throws<ArgumentNullException>(() => invoice.SetSeller(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetBuyer(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetShipping(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetInvoiceDetails(null!, "x", "x"));
        Assert.Throws<ArgumentNullException>(() => invoice.SetInvoiceDetails("x", null!, "x"));
        Assert.Throws<ArgumentNullException>(() => invoice.SetInvoiceDetails("x", "x", null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetCurrencySymbol(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.AddLine(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetTotals(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetPaymentTerms(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetRemittanceInstructions(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetNotes(null!));
        Assert.Throws<ArgumentNullException>(() => invoice.SetLogo(null!, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.SetLogo(image, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.SetLogo(image, 10, 0));
    }

    [Test]
    public void InvoiceRejectsChangesAfterBuild()
    {
        PdfInvoice invoice = new PdfInvoice();

        invoice.Build();

        Assert.Throws<InvalidOperationException>(() => invoice.AddLine(new PdfInvoiceLine("late", 1, 1)));
    }

    static PdfParagraphStyle BodyStyle()
    {
        return PdfParagraphStyle.Create(PdfTextStyle.Create(10, PdfFont.Regular), 12, 0, 0, 0);
    }
}
