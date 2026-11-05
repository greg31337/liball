using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAdvancedInvoiceTests
{
    [Test]
    public void AdvancedInvoice_RendersStructuredLayoutsFormattingTaxAndPaymentDetails()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfInvoiceFormattingOptions formatting = new PdfInvoiceFormattingOptions();
        PdfParty seller = new PdfParty("Acme International");
        PdfParty buyer = new PdfParty("Contoso Europe");
        PdfParty shipping = new PdfParty("Contoso Warehouse");
        PdfInvoiceLine standardRate = new PdfInvoiceLine("Implementation", 2, 700);
        PdfInvoiceLine reducedRate = new PdfInvoiceLine("Printed guide", 1, 200);

        formatting.CultureName = "en-US";
        formatting.CurrencySymbol = "USD ";
        formatting.CurrencyDecimalDigits = 2;
        formatting.NumberDecimalDigits = 2;
        formatting.DateFormat = "dd MMM yyyy";
        seller.SetTaxIdentifier("US-SELLER-42");
        buyer.SetTaxIdentifier("EU-BUYER-84");
        shipping.AddLine("Loading Dock 7");
        standardRate.SetDiscount(100);
        standardRate.SetTaxRate(0.2);
        reducedRate.SetTaxRate(0.05);

        invoice.SetFormattingOptions(formatting);
        invoice.SetInvoiceDetailsWithDates("INV-2042", new DateTime(2026, 7, 31), new DateTime(2026, 8, 30));
        invoice.SetSeller(seller);
        invoice.SetBuyer(buyer);
        invoice.SetShipping(shipping);
        invoice.SetPartyLayout(PdfInvoicePartyLayout.ThreeColumn);
        invoice.AddLine(standardRate);
        invoice.AddLine(reducedRate);
        invoice.IncludeTaxBreakdown();
        invoice.SetPaymentTerms("Net 30");
        invoice.SetPaymentMethod("Bank transfer", "INV-2042");
        invoice.SetRemittanceInstructions("Use the invoice number as the transfer memo.");
        invoice.SetRemittanceAccount("Acme International", "US00-2042", "BANKUS42");
        PdfImage logo = invoice.GetDocument().AddImage(1, 1, new byte[] { 0, 0, 0 });
        invoice.SetLogo(logo, 24, 24);
        invoice.SetLogoPlacement(PdfInvoiceLogoPlacement.RightOfTitle);

        PdfDocument document = invoice.Build();

        string content = AllPageContent(document);
        Assert.That(invoice.GetDocument(), Is.SameAs(document));
        Assert.That(invoice.FormatInvoiceCurrency(1234.5), Is.EqualTo("USD 1,234.50"));
        Assert.That(invoice.FormatInvoiceNumber(1234.5), Is.EqualTo("1,234.50"));
        Assert.That(invoice.FormatInvoiceDate(new DateTime(2026, 7, 31)), Is.EqualTo("31 Jul 2026"));
        Assert.That(content, Does.Contain("(31 Jul 2026) Tj"));
        Assert.That(content, Does.Contain("(30 Aug 2026) Tj"));
        Assert.That(content, Does.Contain("(Seller) Tj"));
        Assert.That(content, Does.Contain("(Buyer) Tj"));
        Assert.That(content, Does.Contain("(Ship To) Tj"));
        Assert.That(TextX(content, "Seller"), Is.LessThan(TextX(content, "Buyer")));
        Assert.That(TextX(content, "Buyer"), Is.LessThan(TextX(content, "Ship To")));
        Assert.That(content, Does.Contain("(Tax ID: US-SELLER-42) Tj"));
        Assert.That(content, Does.Contain("(Tax ID: EU-BUYER-84) Tj"));
        Assert.That(content, Does.Contain("(USD 700.00) Tj"));
        Assert.That(content, Does.Contain("(USD 100.00) Tj"));
        Assert.That(content, Does.Contain("(Tax Breakdown) Tj"));
        Assert.That(content, Does.Contain("(5.00%) Tj"));
        Assert.That(content, Does.Contain("(20.00%) Tj"));
        Assert.That(content, Does.Contain("(USD 1,770.00) Tj"));
        Assert.That(content, Does.Contain("(Payment Details) Tj"));
        Assert.That(content, Does.Contain("(Terms: Net 30) Tj"));
        Assert.That(content, Does.Contain("(Method: Bank transfer) Tj"));
        Assert.That(content, Does.Contain("(Reference: INV-2042) Tj"));
        Assert.That(content, Does.Contain("(Remittance Details) Tj"));
        Assert.That(content, Does.Contain("(Payee: Acme International) Tj"));
        Assert.That(content, Does.Contain("(Account: US00-2042) Tj"));
        Assert.That(content, Does.Contain("(Bank: BANKUS42) Tj"));
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content.IndexOf("(INVOICE) Tj", StringComparison.Ordinal), Is.LessThan(content.IndexOf("/Im1 Do", StringComparison.Ordinal)));
    }

    [Test]
    public void TwoColumnParties_KeepShippingBelowAndSupportALeftLogo()
    {
        PdfInvoice invoice = new PdfInvoice();
        invoice.SetSeller(new PdfParty("Seller Name"));
        invoice.SetBuyer(new PdfParty("Buyer Name"));
        invoice.SetShipping(new PdfParty("Shipping Name"));
        invoice.SetPartyLayout(PdfInvoicePartyLayout.TwoColumn);
        PdfImage logo = invoice.GetDocument().AddImage(1, 1, new byte[] { 0, 0, 0 });
        invoice.SetLogo(logo, 20, 20);
        invoice.SetLogoPlacement(PdfInvoiceLogoPlacement.LeftOfTitle);

        string content = AllPageContent(invoice.Build());

        Assert.That(TextX(content, "Seller"), Is.LessThan(TextX(content, "Buyer")));
        Assert.That(TextY(content, "Ship To"), Is.LessThan(TextY(content, "Seller")));
        Assert.That(content.IndexOf("/Im1 Do", StringComparison.Ordinal), Is.LessThan(content.IndexOf("(INVOICE) Tj", StringComparison.Ordinal)));
    }

    [Test]
    public void TaxBreakdown_GroupsDiscountedTaxableAmountsInRateOrder()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfInvoiceLine firstStandard = new PdfInvoiceLine("First standard", 2, 50);
        PdfInvoiceLine reduced = new PdfInvoiceLine("Reduced", 1, 40);
        PdfInvoiceLine secondStandard = new PdfInvoiceLine("Second standard", 1, 25);

        firstStandard.SetDiscount(10);
        firstStandard.SetTaxRate(0.2);
        reduced.SetTaxRate(0.05);
        secondStandard.SetTaxRate(0.2);
        invoice.AddLine(firstStandard);
        invoice.AddLine(reduced);
        invoice.AddLine(secondStandard);

        IReadOnlyList<PdfInvoiceTaxBreakdown> breakdowns = invoice.GetTaxBreakdown();

        Assert.That(breakdowns.Count, Is.EqualTo(2));
        Assert.That(breakdowns[0].TaxRate, Is.EqualTo(0.05));
        Assert.That(breakdowns[0].TaxableAmount, Is.EqualTo(40));
        Assert.That(breakdowns[0].TaxAmount, Is.EqualTo(2));
        Assert.That(breakdowns[1].TaxRate, Is.EqualTo(0.2));
        Assert.That(breakdowns[1].TaxableAmount, Is.EqualTo(115));
        Assert.That(breakdowns[1].TaxAmount, Is.EqualTo(23));
    }

    [Test]
    public void FormattingOptions_AreCopiedAndUseBuiltInCultureRules()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfInvoiceFormattingOptions formatting = new PdfInvoiceFormattingOptions();

        formatting.CultureName = "de-DE";
        formatting.CurrencySymbol = "EUR";
        formatting.CurrencyDecimalDigits = 2;
        formatting.NumberDecimalDigits = 1;
        formatting.DateFormat = "dd.MM.yyyy";
        invoice.SetFormattingOptions(formatting);
        formatting.CultureName = "en-US";
        formatting.CurrencySymbol = "$";
        formatting.NumberDecimalDigits = 4;
        formatting.DateFormat = "yyyy";

        Assert.That(invoice.FormatInvoiceCurrency(1234.5), Is.EqualTo("1.234,50 EUR"));
        Assert.That(invoice.FormatInvoiceNumber(1234.5), Is.EqualTo("1.234,5"));
        Assert.That(invoice.FormatInvoiceDate(new DateTime(2026, 7, 31)), Is.EqualTo("31.07.2026"));
    }

    [Test]
    public void AdvancedInvoiceHelpers_RejectInvalidInputs()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfInvoiceFormattingOptions formatting = new PdfInvoiceFormattingOptions();
        PdfInvoiceLine line = new PdfInvoiceLine("Line", 1, 10);
        PdfDocument otherDocument = new PdfDocument();
        PdfImage foreignLogo = otherDocument.AddImage(1, 1, new byte[] { 0, 0, 0 });
        PdfImage invoiceLogo = invoice.GetDocument().AddImage(1, 1, new byte[] { 0, 0, 0 });

        Assert.Throws<ArgumentException>(() => invoice.SetPartyLayout((PdfInvoicePartyLayout)99));
        Assert.Throws<ArgumentException>(() => invoice.SetLogoPlacement((PdfInvoiceLogoPlacement)99));
        Assert.Throws<ArgumentNullException>(() => invoice.SetFormattingOptions(null!));
        formatting.CultureName = string.Empty;
        Assert.Throws<ArgumentException>(() => invoice.SetFormattingOptions(formatting));
        formatting.CultureName = "not-a-real-culture";
        Assert.Throws<System.Globalization.CultureNotFoundException>(() => invoice.SetFormattingOptions(formatting));
        formatting.CultureName = "en-US";
        formatting.CurrencyDecimalDigits = 7;
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.SetFormattingOptions(formatting));
        formatting.CurrencyDecimalDigits = 2;
        formatting.NumberDecimalDigits = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.SetFormattingOptions(formatting));
        formatting.NumberDecimalDigits = 2;
        formatting.DateFormat = string.Empty;
        Assert.Throws<ArgumentException>(() => invoice.SetFormattingOptions(formatting));
        Assert.Throws<ArgumentNullException>(() => invoice.SetInvoiceDetailsWithDates(null!, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)));
        Assert.Throws<ArgumentException>(() => invoice.SetInvoiceDetailsWithDates("INV", new DateTime(2026, 1, 2), new DateTime(2026, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.FormatInvoiceCurrency(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.FormatInvoiceNumber(double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => invoice.SetPaymentMethod(string.Empty, "reference"));
        Assert.Throws<ArgumentNullException>(() => invoice.SetPaymentMethod("Card", null!));
        Assert.Throws<ArgumentException>(() => invoice.SetRemittanceAccount(string.Empty, "account", "bank"));
        Assert.Throws<ArgumentException>(() => invoice.SetRemittanceAccount("Payee", string.Empty, "bank"));
        Assert.Throws<ArgumentNullException>(() => invoice.SetRemittanceAccount("Payee", "account", null!));
        Assert.Throws<ArgumentException>(() => invoice.SetLogo(foreignLogo, 10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.SetLogo(invoiceLogo, 504, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.SetDiscount(11));
        Assert.Throws<ArgumentOutOfRangeException>(() => line.SetTaxRate(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceLine("Line", double.PositiveInfinity, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceTotals(double.NaN, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfInvoiceTaxBreakdown(0.2, 1, double.PositiveInfinity));
    }

    static string AllPageContent(PdfDocument document)
    {
        string content = string.Empty;
        for (int index = 0; index < document.Pages.Count; index++)
        {
            content += document.Pages[index].Canvas.ToPdfString();
        }

        return content;
    }

    static double TextX(string content, string text)
    {
        string command = TextPositionCommand(content, text);
        int separator = command.IndexOf(' ');
        return double.Parse(command.Substring(0, separator), System.Globalization.CultureInfo.InvariantCulture);
    }

    static double TextY(string content, string text)
    {
        string command = TextPositionCommand(content, text);
        int separator = command.IndexOf(' ');
        return double.Parse(command.Substring(separator + 1), System.Globalization.CultureInfo.InvariantCulture);
    }

    static string TextPositionCommand(string content, string text)
    {
        int textIndex = content.IndexOf("(" + text + ") Tj", StringComparison.Ordinal);
        int commandEnd = content.LastIndexOf(" Td", textIndex, StringComparison.Ordinal);
        int commandStart = content.LastIndexOf('\n', commandEnd) + 1;
        return content.Substring(commandStart, commandEnd - commandStart);
    }
}
