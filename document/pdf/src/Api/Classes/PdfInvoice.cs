using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Builds a printable invoice from parties, line items, totals, payment details, and optional branding.
/// </summary>
public class PdfInvoice : IPdfInvoice
{
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();
    /// <summary>
    /// Stores the layout used by this object.
    /// </summary>
    internal PdfLayoutDocument Layout = PdfLayoutDocument.Default(new PdfDocument());
    /// <summary>
    /// Stores the page size used by this object.
    /// </summary>
    internal PdfPageDimensions PageSize = new PdfPageSizeFactory().Letter();
    /// <summary>
    /// Stores the margins used by this object.
    /// </summary>
    internal PdfPageMargins Margins = new PdfPageMargins(54, 54, 54, 54);
    /// <summary>
    /// Specifies the content width in PDF points.
    /// </summary>
    internal double ContentWidth;
    /// <summary>
    /// Indicates whether this object has seller.
    /// </summary>
    internal bool HasSeller;
    /// <summary>
    /// Stores the seller used by this object.
    /// </summary>
    internal PdfParty Seller = new PdfParty(string.Empty);
    /// <summary>
    /// Indicates whether this object has buyer.
    /// </summary>
    internal bool HasBuyer;
    /// <summary>
    /// Stores the buyer used by this object.
    /// </summary>
    internal PdfParty Buyer = new PdfParty(string.Empty);
    /// <summary>
    /// Indicates whether this object has shipping.
    /// </summary>
    internal bool HasShipping;
    /// <summary>
    /// Stores the shipping used by this object.
    /// </summary>
    internal PdfParty Shipping = new PdfParty(string.Empty);
    /// <summary>
    /// Stores the invoice number used by this object.
    /// </summary>
    internal string InvoiceNumber = string.Empty;
    /// <summary>
    /// Stores the invoice date used by this object.
    /// </summary>
    internal string InvoiceDate = string.Empty;
    /// <summary>
    /// Stores the due date used by this object.
    /// </summary>
    internal string DueDate = string.Empty;
    /// <summary>
    /// Stores the payment terms used by this object.
    /// </summary>
    internal string PaymentTerms = string.Empty;
    /// <summary>
    /// Stores the remittance instructions used by this object.
    /// </summary>
    internal string RemittanceInstructions = string.Empty;
    /// <summary>
    /// Stores the notes used by this object.
    /// </summary>
    internal string Notes = string.Empty;
    /// <summary>
    /// Stores the currency symbol used by this object.
    /// </summary>
    internal string CurrencySymbol = "$";
    /// <summary>
    /// Indicates whether this object has totals.
    /// </summary>
    internal bool HasTotals;
    /// <summary>
    /// Stores the totals used by this object.
    /// </summary>
    internal PdfInvoiceTotals Totals = new PdfInvoiceTotals(0, 0, 0, 0);
    /// <summary>
    /// Indicates whether this object has logo.
    /// </summary>
    internal bool HasLogo;
    /// <summary>
    /// Stores the logo used by this object.
    /// </summary>
    internal PdfImage Logo = new PdfImage("Im0", 1, 1, new byte[] { 0, 0, 0 });
    /// <summary>
    /// Specifies the logo width in PDF points.
    /// </summary>
    internal double LogoWidth;
    /// <summary>
    /// Specifies the logo height in PDF points.
    /// </summary>
    internal double LogoHeight;
    internal PdfInvoicePartyLayout PartyLayout;
    internal PdfInvoiceLogoPlacement LogoPlacement;
    internal bool HasFormattingOptions;
    internal PdfInvoiceFormattingOptions FormattingOptions = new PdfInvoiceFormattingOptions();
    internal CultureInfo FormattingCulture = CultureInfo.InvariantCulture;
    internal bool HasTypedDates;
    internal DateTime InvoiceDateValue;
    internal DateTime DueDateValue;
    internal bool HasTaxBreakdown;
    internal bool HasPaymentMethod;
    internal string PaymentMethod = string.Empty;
    internal string PaymentReference = string.Empty;
    internal bool HasRemittanceAccount;
    internal string RemittancePayee = string.Empty;
    internal string RemittanceAccountIdentifier = string.Empty;
    internal string RemittanceBankIdentifier = string.Empty;
    internal PdfInvoiceDocumentType DocumentType;
    internal bool HasCreditNoteReference;
    internal string OriginalInvoiceNumber = string.Empty;
    internal string CreditReason = string.Empty;
    internal bool HasEmbeddedFont;
    internal bool HasFacturXData;
    internal PdfAPreparationOptions FacturXPdfAOptions = new PdfAPreparationOptions();
    internal List<PdfInvoiceLine> _lines = new List<PdfInvoiceLine>();
    /// <summary>
    /// Exposes the lines currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfInvoiceLine> Lines = new List<PdfInvoiceLine>();
    /// <summary>
    /// Stores the title style used by this object.
    /// </summary>
    internal PdfParagraphStyle TitleStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the label style used by this object.
    /// </summary>
    internal PdfParagraphStyle LabelStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the body style used by this object.
    /// </summary>
    internal PdfParagraphStyle BodyStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the table header style used by this object.
    /// </summary>
    internal PdfParagraphStyle TableHeaderStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the table cell style used by this object.
    /// </summary>
    internal PdfParagraphStyle TableCellStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the footer style used by this object.
    /// </summary>
    internal PdfParagraphStyle FooterStyle = new PdfParagraphStyle();
    internal bool Built;

    /// <summary>
    /// Initializes a new invoice.
    /// </summary>
    public PdfInvoice()
    {
        Document = new PdfDocument();
        PageSize = new PdfPageSizeFactory().Letter();
        Margins = new PdfPageMargins(54, 54, 54, 54);
        ContentWidth = PageSize.Width - Margins.Left - Margins.Right;
        Layout = new PdfLayoutDocument(Document, PageSize, Margins);
        _lines = new List<PdfInvoiceLine>();
        Lines = _lines;
        InitializeStyles();
        ApplyPageFurniture();
        Built = false;
        HasSeller = false;
        HasBuyer = false;
        HasShipping = false;
        HasTotals = false;
        HasLogo = false;
        LogoWidth = 0;
        LogoHeight = 0;
        PartyLayout = PdfInvoicePartyLayout.Stacked;
        LogoPlacement = PdfInvoiceLogoPlacement.AboveTitle;
        HasFormattingOptions = false;
        FormattingOptions = new PdfInvoiceFormattingOptions();
        FormattingCulture = CultureInfo.InvariantCulture;
        HasTypedDates = false;
        InvoiceDateValue = default(DateTime);
        DueDateValue = default(DateTime);
        HasTaxBreakdown = false;
        HasPaymentMethod = false;
        PaymentMethod = string.Empty;
        PaymentReference = string.Empty;
        HasRemittanceAccount = false;
        RemittancePayee = string.Empty;
        RemittanceAccountIdentifier = string.Empty;
        RemittanceBankIdentifier = string.Empty;
        DocumentType = PdfInvoiceDocumentType.Invoice;
        HasCreditNoteReference = false;
        OriginalInvoiceNumber = string.Empty;
        CreditReason = string.Empty;
        HasEmbeddedFont = false;
        HasFacturXData = false;
        FacturXPdfAOptions = new PdfAPreparationOptions();
    }

    /// <summary>Returns the underlying invoice document for resource and metadata configuration.</summary>
    /// <returns>The invoice document.</returns>
    public PdfDocument GetDocument()
    {
        return Document;
    }

    /// <summary>Selects invoice or credit-note rendering.</summary>
    /// <param name="documentType">The business document type.</param>
    public void SetDocumentType(PdfInvoiceDocumentType documentType)
    {
        EnsureNotBuilt();
        if (documentType != PdfInvoiceDocumentType.Invoice && documentType != PdfInvoiceDocumentType.CreditNote)
        {
            throw new ArgumentException("Invoice document type is not supported.", "documentType");
        }

        DocumentType = documentType;
        ApplyPageFurniture();
    }

    /// <summary>Sets the original invoice reference and reason displayed by a credit note.</summary>
    /// <param name="originalInvoiceNumber">The credited invoice number.</param>
    /// <param name="reason">The reason for issuing the credit.</param>
    public void SetCreditNoteReference(string originalInvoiceNumber, string reason)
    {
        EnsureNotBuilt();
        ValidateRequiredText(originalInvoiceNumber, "originalInvoiceNumber", "Original invoice number cannot be empty.");
        ValidateRequiredText(reason, "reason", "Credit-note reason cannot be empty.");
        OriginalInvoiceNumber = originalInvoiceNumber;
        CreditReason = reason;
        HasCreditNoteReference = true;
        DocumentType = PdfInvoiceDocumentType.CreditNote;
        ApplyPageFurniture();
    }

    /// <summary>Uses a registered embeddable TrueType font for all invoice text.</summary>
    /// <param name="font">The font registered with the invoice document.</param>
    public void SetEmbeddedFont(PdfTrueTypeFont font)
    {
        EnsureNotBuilt();
        if (font == null)
        {
            throw new ArgumentNullException("font");
        }

        if (!Document.FontRegistry.HasTrueTypeFont(font.Identity) || Document.FontRegistry.GetTrueTypeFont(font.Identity) != font)
        {
            throw new ArgumentException("Invoice font must be registered with the invoice document.", "font");
        }

        PdfCanvas.ValidateTrueTypeFont(font);
        ApplyEmbeddedFont(TitleStyle, font);
        ApplyEmbeddedFont(LabelStyle, font);
        ApplyEmbeddedFont(BodyStyle, font);
        ApplyEmbeddedFont(TableHeaderStyle, font);
        ApplyEmbeddedFont(TableCellStyle, font);
        ApplyEmbeddedFont(FooterStyle, font);
        HasEmbeddedFont = true;
    }

    /// <summary>Configures caller-supplied Factur-X/ZUGFeRD XML and automatic PDF/A-3b preparation.</summary>
    /// <param name="metadata">The standardized hybrid-invoice profile metadata.</param>
    /// <param name="xmlData">The complete caller-supplied UN/CEFACT CII XML bytes.</param>
    /// <param name="pdfAOptions">PDF/A-3b output-intent options with a complete ICC profile.</param>
    public void SetFacturXData(PdfFacturXMetadata metadata, byte[] xmlData, PdfAPreparationOptions pdfAOptions)
    {
        EnsureNotBuilt();
        PdfAConfiguration.ValidateOptions(pdfAOptions);
        if (pdfAOptions.Conformance != PdfAConformance.PdfA3B)
        {
            throw new ArgumentException("Factur-X/ZUGFeRD output requires PDF/A-3b conformance.", "pdfAOptions");
        }

        PdfFacturXConfiguration.Configure(Document, metadata, xmlData);
        FacturXPdfAOptions = pdfAOptions.Copy();
        HasFacturXData = true;
    }

    /// <summary>
    /// Sets the seller used for subsequent PDF generation.
    /// </summary>
    /// <param name="party">The party to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSeller(PdfParty party)
    {
        EnsureNotBuilt();
        if (party == null)
        {
            throw new ArgumentNullException("party");
        }

        Seller = party;
        HasSeller = true;
    }

    /// <summary>
    /// Sets the buyer used for subsequent PDF generation.
    /// </summary>
    /// <param name="party">The party to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetBuyer(PdfParty party)
    {
        EnsureNotBuilt();
        if (party == null)
        {
            throw new ArgumentNullException("party");
        }

        Buyer = party;
        HasBuyer = true;
    }

    /// <summary>
    /// Sets the shipping used for subsequent PDF generation.
    /// </summary>
    /// <param name="party">The party to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetShipping(PdfParty party)
    {
        EnsureNotBuilt();
        if (party == null)
        {
            throw new ArgumentNullException("party");
        }

        Shipping = party;
        HasShipping = true;
    }

    /// <summary>Selects the seller, buyer, and shipping layout.</summary>
    /// <param name="layout">The party layout.</param>
    public void SetPartyLayout(PdfInvoicePartyLayout layout)
    {
        EnsureNotBuilt();
        if (layout != PdfInvoicePartyLayout.Stacked && layout != PdfInvoicePartyLayout.TwoColumn && layout != PdfInvoicePartyLayout.ThreeColumn)
        {
            throw new ArgumentException("Invoice party layout is not supported.", "layout");
        }

        PartyLayout = layout;
    }

    /// <summary>
    /// Sets the invoice details used for subsequent PDF generation.
    /// </summary>
    /// <param name="invoiceNumber">The invoice number to use for this operation.</param>
    /// <param name="invoiceDate">The invoice date to use for this operation.</param>
    /// <param name="dueDate">The due date to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetInvoiceDetails(string invoiceNumber, string invoiceDate, string dueDate)
    {
        EnsureNotBuilt();
        if (invoiceNumber == null)
        {
            throw new ArgumentNullException("invoiceNumber");
        }

        if (invoiceDate == null)
        {
            throw new ArgumentNullException("invoiceDate");
        }

        if (dueDate == null)
        {
            throw new ArgumentNullException("dueDate");
        }

        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        HasTypedDates = false;
    }

    /// <summary>Sets invoice details using typed dates formatted at build time.</summary>
    /// <param name="invoiceNumber">The invoice number.</param>
    /// <param name="invoiceDate">The invoice date.</param>
    /// <param name="dueDate">The due date.</param>
    public void SetInvoiceDetailsWithDates(string invoiceNumber, DateTime invoiceDate, DateTime dueDate)
    {
        EnsureNotBuilt();
        if (invoiceNumber == null)
        {
            throw new ArgumentNullException("invoiceNumber");
        }

        if (dueDate < invoiceDate)
        {
            throw new ArgumentException("Invoice due date cannot precede the invoice date.", "dueDate");
        }

        InvoiceNumber = invoiceNumber;
        InvoiceDateValue = invoiceDate;
        DueDateValue = dueDate;
        HasTypedDates = true;
    }

    /// <summary>
    /// Sets the currency symbol used for subsequent PDF generation.
    /// </summary>
    /// <param name="currencySymbol">The currency symbol to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetCurrencySymbol(string currencySymbol)
    {
        EnsureNotBuilt();
        if (currencySymbol == null)
        {
            throw new ArgumentNullException("currencySymbol");
        }

        CurrencySymbol = currencySymbol;
        if (HasFormattingOptions)
        {
            FormattingOptions.CurrencySymbol = currencySymbol;
            ConfigureFormattingCulture();
        }
    }

    /// <summary>Sets deterministic locale-aware formatting options.</summary>
    /// <param name="options">The formatting options copied by the invoice.</param>
    public void SetFormattingOptions(PdfInvoiceFormattingOptions options)
    {
        EnsureNotBuilt();
        ValidateFormattingOptions(options);
        FormattingOptions = CopyFormattingOptions(options);
        FormattingCulture = CreateFormattingCulture(FormattingOptions);
        CurrencySymbol = FormattingOptions.CurrencySymbol;
        HasFormattingOptions = true;
    }

    /// <summary>Formats a currency value using the current invoice configuration.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>The formatted currency text.</returns>
    public string FormatInvoiceCurrency(double value)
    {
        ValidateFinite(value, "value");
        if (!HasFormattingOptions)
        {
            return CurrencySymbol + value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        string format = "C" + FormattingOptions.CurrencyDecimalDigits.ToString(CultureInfo.InvariantCulture);
        return value.ToString(format, FormattingCulture);
    }

    /// <summary>Formats an ordinary number using the current invoice configuration.</summary>
    /// <param name="value">The finite value.</param>
    /// <returns>The formatted number text.</returns>
    public string FormatInvoiceNumber(double value)
    {
        ValidateFinite(value, "value");
        if (!HasFormattingOptions)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        string format = "N" + FormattingOptions.NumberDecimalDigits.ToString(CultureInfo.InvariantCulture);
        return value.ToString(format, FormattingCulture);
    }

    /// <summary>Formats a date using the current invoice configuration.</summary>
    /// <param name="value">The date value.</param>
    /// <returns>The formatted date text.</returns>
    public string FormatInvoiceDate(DateTime value)
    {
        if (!HasFormattingOptions)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return value.ToString(FormattingOptions.DateFormat, FormattingCulture);
    }

    /// <summary>
    /// Adds a line to this invoice.
    /// </summary>
    /// <param name="line">The line to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddLine(PdfInvoiceLine line)
    {
        EnsureNotBuilt();
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        _lines.Add(line);
    }

    /// <summary>
    /// Sets the totals used for subsequent PDF generation.
    /// </summary>
    /// <param name="totals">The totals to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetTotals(PdfInvoiceTotals totals)
    {
        EnsureNotBuilt();
        if (totals == null)
        {
            throw new ArgumentNullException("totals");
        }

        Totals = totals;
        HasTotals = true;
    }

    /// <summary>Enables an itemized tax-rate breakdown in the rendered invoice.</summary>
    public void IncludeTaxBreakdown()
    {
        EnsureNotBuilt();
        HasTaxBreakdown = true;
    }

    /// <summary>Returns tax-rate subtotals calculated from current invoice lines.</summary>
    /// <returns>The tax-rate subtotals in ascending rate order.</returns>
    public IReadOnlyList<PdfInvoiceTaxBreakdown> GetTaxBreakdown()
    {
        List<PdfInvoiceTaxBreakdown> breakdowns = new List<PdfInvoiceTaxBreakdown>();
        foreach (PdfInvoiceLine line in _lines)
        {
            double taxableAmount = line.GetSubtotal() - line.Discount;
            if (taxableAmount < 0)
            {
                taxableAmount = 0;
            }

            int existingIndex = FindTaxBreakdownIndex(breakdowns, line.TaxRate);
            if (existingIndex >= 0)
            {
                breakdowns[existingIndex].TaxableAmount += taxableAmount;
                breakdowns[existingIndex].TaxAmount += line.GetTaxAmount();
            }
            else
            {
                PdfInvoiceTaxBreakdown breakdown = new PdfInvoiceTaxBreakdown(line.TaxRate, taxableAmount, line.GetTaxAmount());
                int insertIndex = FindTaxBreakdownInsertIndex(breakdowns, line.TaxRate);
                breakdowns.Insert(insertIndex, breakdown);
            }
        }

        return breakdowns;
    }

    /// <summary>
    /// Sets the payment terms used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetPaymentTerms(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        PaymentTerms = text;
    }

    /// <summary>Sets structured payment method and reference details.</summary>
    /// <param name="method">The payment method.</param>
    /// <param name="reference">The payment reference.</param>
    public void SetPaymentMethod(string method, string reference)
    {
        EnsureNotBuilt();
        ValidateRequiredText(method, "method", "Payment method cannot be empty.");
        ValidateText(reference, "reference");
        PaymentMethod = method;
        PaymentReference = reference;
        HasPaymentMethod = true;
    }

    /// <summary>
    /// Sets the remittance instructions used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetRemittanceInstructions(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        RemittanceInstructions = text;
    }

    /// <summary>Sets structured remittance account details.</summary>
    /// <param name="payee">The remittance payee.</param>
    /// <param name="accountIdentifier">The account identifier.</param>
    /// <param name="bankIdentifier">The bank or routing identifier.</param>
    public void SetRemittanceAccount(string payee, string accountIdentifier, string bankIdentifier)
    {
        EnsureNotBuilt();
        ValidateRequiredText(payee, "payee", "Remittance payee cannot be empty.");
        ValidateRequiredText(accountIdentifier, "accountIdentifier", "Remittance account identifier cannot be empty.");
        ValidateText(bankIdentifier, "bankIdentifier");
        RemittancePayee = payee;
        RemittanceAccountIdentifier = accountIdentifier;
        RemittanceBankIdentifier = bankIdentifier;
        HasRemittanceAccount = true;
    }

    /// <summary>
    /// Sets the notes used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetNotes(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Notes = text;
    }

    /// <summary>
    /// Sets the logo used for subsequent PDF generation.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLogo(PdfImage image, double width, double height)
    {
        EnsureNotBuilt();
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (!HasDocumentImage(image))
        {
            throw new ArgumentException("Invoice logo image must be registered with the invoice document.", "image");
        }

        ValidatePositive(width, "width", "Logo width must be positive.");
        ValidatePositive(height, "height", "Logo height must be positive.");
        if (width >= ContentWidth)
        {
            throw new ArgumentOutOfRangeException("width", "Logo width must leave space within the invoice content area.");
        }

        Logo = image;
        LogoWidth = width;
        LogoHeight = height;
        HasLogo = true;
    }

    /// <summary>Selects the logo position relative to the title.</summary>
    /// <param name="placement">The logo placement.</param>
    public void SetLogoPlacement(PdfInvoiceLogoPlacement placement)
    {
        EnsureNotBuilt();
        if (placement != PdfInvoiceLogoPlacement.AboveTitle && placement != PdfInvoiceLogoPlacement.LeftOfTitle && placement != PdfInvoiceLogoPlacement.RightOfTitle)
        {
            throw new ArgumentException("Invoice logo placement is not supported.", "placement");
        }

        LogoPlacement = placement;
    }

    internal string FormatCurrency(double value)
    {
        return FormatInvoiceCurrency(value);
    }

    /// <summary>
    /// Builds an invoice from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            if (HasFacturXData && !HasEmbeddedFont)
            {
                throw new InvalidOperationException("Factur-X/ZUGFeRD invoice output requires a registered embedded font.");
            }

            Compose();
            Layout.Render();
            if (HasFacturXData)
            {
                Document.PrepareForPdfA(FacturXPdfAOptions);
            }

            Built = true;
        }

        return Document;
    }

    internal void Compose()
    {
        AddTitleBlock();
        AddPartyBlocks();
        AddLineTable();
        if (HasTaxBreakdown)
        {
            AddTaxBreakdownBlock();
        }

        AddTotalsBlock(GetTotalsForBuild());
        if (HasPaymentMethod)
        {
            AddPaymentDetailsBlock();
        }
        else
        {
            AddOptionalText("Payment Terms", PaymentTerms);
        }

        if (HasRemittanceAccount)
        {
            AddRemittanceDetailsBlock();
        }
        else
        {
            AddOptionalText("Remittance", RemittanceInstructions);
        }

        AddOptionalText("Notes", Notes);
    }

    internal void AddTitleBlock()
    {
        if (HasLogo && LogoPlacement == PdfInvoiceLogoPlacement.AboveTitle)
        {
            Layout.AddImageBlock(new PdfImageBlock(Logo, LogoWidth, LogoHeight));
            Layout.AddBlock(PdfBlock.WithSize(0, 8));
        }

        if (HasLogo && LogoPlacement != PdfInvoiceLogoPlacement.AboveTitle)
        {
            AddSideLogoTitleBlock();
            return;
        }

        Layout.AddTextBlock(new PdfTextBlock(GetDocumentTitle(), TitleStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 8));
        PdfTableBlock details = new PdfTableBlock(new double[] { 120, 200 });
        AddDetailRow(details, GetDocumentNumberLabel(), InvoiceNumber);
        AddDetailRow(details, GetDocumentDateLabel(), GetInvoiceDateText());
        AddDetailRow(details, "Due Date", GetDueDateText());
        AddCreditNoteReferenceRows(details);
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(details));
        Layout.AddBlock(PdfBlock.WithSize(0, 14));
    }

    internal void AddPartyBlocks()
    {
        if (PartyLayout == PdfInvoicePartyLayout.TwoColumn)
        {
            AddTwoColumnPartyBlocks();
            return;
        }

        if (PartyLayout == PdfInvoicePartyLayout.ThreeColumn)
        {
            AddThreeColumnPartyBlocks();
            return;
        }

        if (HasSeller)
        {
            Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(Seller.CreateBlock("Seller", LabelStyle, BodyStyle)));
            Layout.AddBlock(PdfBlock.WithSize(0, 10));
        }

        if (HasBuyer)
        {
            Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(Buyer.CreateBlock("Buyer", LabelStyle, BodyStyle)));
            Layout.AddBlock(PdfBlock.WithSize(0, 10));
        }

        if (HasShipping)
        {
            Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(Shipping.CreateBlock("Ship To", LabelStyle, BodyStyle)));
            Layout.AddBlock(PdfBlock.WithSize(0, 10));
        }
    }

    internal void AddLineTable()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 184, 48, 64, 64, 50, 94 });
        PdfTableRow header = table.AddHeaderRow();
        AddHeaderCell(header, "Description");
        AddHeaderCell(header, "Qty");
        AddHeaderCell(header, "Unit");
        AddHeaderCell(header, "Discount");
        AddHeaderCell(header, "Tax");
        AddHeaderCell(header, "Total");

        for (int index = 0; index < _lines.Count; index++)
        {
            PdfInvoiceLine line = _lines[index];
            PdfTableRow row = table.AddRow();
            AddBodyCell(row, line.Description);
            AddBodyCell(row, FormatInvoiceNumber(line.Quantity));
            AddBodyCell(row, FormatCurrency(line.UnitPrice));
            AddBodyCell(row, FormatCurrency(line.Discount));
            AddBodyCell(row, FormatInvoiceNumber(line.TaxRate * 100) + "%");
            AddBodyCell(row, FormatCurrency(line.GetTotal()));
        }

        Layout.AddTableBlock(table);
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    internal void AddTaxBreakdownBlock()
    {
        IReadOnlyList<PdfInvoiceTaxBreakdown> breakdowns = GetTaxBreakdown();
        if (breakdowns.Count == 0)
        {
            return;
        }

        PdfStackBlock section = PdfStackBlock.WithSpacing(5);
        section.AddTextBlock(new PdfTextBlock("Tax Breakdown", LabelStyle));
        PdfTableBlock table = new PdfTableBlock(new double[] { 80, 100, 100 });
        PdfTableRow header = table.AddHeaderRow();
        AddHeaderCell(header, "Rate");
        AddHeaderCell(header, "Taxable");
        AddHeaderCell(header, "Tax");
        foreach (PdfInvoiceTaxBreakdown breakdown in breakdowns)
        {
            PdfTableRow row = table.AddRow();
            AddBodyCell(row, FormatInvoiceNumber(breakdown.TaxRate * 100) + "%");
            AddBodyCell(row, FormatCurrency(breakdown.TaxableAmount));
            AddBodyCell(row, FormatCurrency(breakdown.TaxAmount));
        }

        section.AddTableBlock(table);
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(section));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    internal void AddTotalsBlock(PdfInvoiceTotals totals)
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 160, 120 });
        AddTotalRow(table, "Subtotal", FormatCurrency(totals.Subtotal));
        AddTotalRow(table, "Discount", FormatCurrency(totals.DiscountTotal));
        AddTotalRow(table, "Tax", FormatCurrency(totals.TaxTotal));
        AddTotalRow(table, GetGrandTotalLabel(), FormatCurrency(totals.GrandTotal));
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(table));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    internal void AddOptionalText(string heading, string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        PdfStackBlock stack = PdfStackBlock.WithSpacing(4);
        stack.AddTextBlock(new PdfTextBlock(heading, LabelStyle));
        stack.AddTextBlock(new PdfTextBlock(text, BodyStyle));
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(stack));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    internal void AddPaymentDetailsBlock()
    {
        PdfStackBlock stack = PdfStackBlock.WithSpacing(3);
        stack.AddTextBlock(new PdfTextBlock("Payment Details", LabelStyle));
        if (PaymentTerms.Length > 0)
        {
            stack.AddTextBlock(new PdfTextBlock("Terms: " + PaymentTerms, BodyStyle));
        }

        stack.AddTextBlock(new PdfTextBlock("Method: " + PaymentMethod, BodyStyle));
        if (PaymentReference.Length > 0)
        {
            stack.AddTextBlock(new PdfTextBlock("Reference: " + PaymentReference, BodyStyle));
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(stack));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    internal void AddRemittanceDetailsBlock()
    {
        PdfStackBlock stack = PdfStackBlock.WithSpacing(3);
        stack.AddTextBlock(new PdfTextBlock("Remittance Details", LabelStyle));
        if (RemittanceInstructions.Length > 0)
        {
            stack.AddTextBlock(new PdfTextBlock(RemittanceInstructions, BodyStyle));
        }

        stack.AddTextBlock(new PdfTextBlock("Payee: " + RemittancePayee, BodyStyle));
        stack.AddTextBlock(new PdfTextBlock("Account: " + RemittanceAccountIdentifier, BodyStyle));
        if (RemittanceBankIdentifier.Length > 0)
        {
            stack.AddTextBlock(new PdfTextBlock("Bank: " + RemittanceBankIdentifier, BodyStyle));
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(stack));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    internal void AddSideLogoTitleBlock()
    {
        double titleWidth = ContentWidth - LogoWidth;
        PdfTableBlock details = new PdfTableBlock(new double[] { titleWidth * 0.4, titleWidth * 0.6 });
        AddDetailRow(details, GetDocumentNumberLabel(), InvoiceNumber);
        AddDetailRow(details, GetDocumentDateLabel(), GetInvoiceDateText());
        AddDetailRow(details, "Due Date", GetDueDateText());
        AddCreditNoteReferenceRows(details);
        PdfStackBlock title = PdfStackBlock.WithSpacing(8);
        title.AddTextBlock(new PdfTextBlock(GetDocumentTitle(), TitleStyle));
        title.AddTableBlock(details);
        PdfTableBlock titleTable;
        PdfTableRow row;
        if (LogoPlacement == PdfInvoiceLogoPlacement.LeftOfTitle)
        {
            titleTable = new PdfTableBlock(new double[] { LogoWidth, titleWidth });
            row = titleTable.AddRow();
            row.AddElement(new PdfImageBlock(Logo, LogoWidth, LogoHeight));
            row.AddStackBlock(title);
        }
        else
        {
            titleTable = new PdfTableBlock(new double[] { titleWidth, LogoWidth });
            row = titleTable.AddRow();
            row.AddStackBlock(title);
            row.AddElement(new PdfImageBlock(Logo, LogoWidth, LogoHeight));
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(titleTable));
        Layout.AddBlock(PdfBlock.WithSize(0, 14));
    }

    internal void AddTwoColumnPartyBlocks()
    {
        if (HasSeller || HasBuyer)
        {
            PdfTableBlock table = new PdfTableBlock(new double[] { ContentWidth / 2, ContentWidth / 2 });
            PdfTableRow row = table.AddRow();
            AddPartyCell(row, HasSeller, Seller, "Seller");
            AddPartyCell(row, HasBuyer, Buyer, "Buyer");
            Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(table));
            Layout.AddBlock(PdfBlock.WithSize(0, 10));
        }

        if (HasShipping)
        {
            Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(Shipping.CreateBlock("Ship To", LabelStyle, BodyStyle)));
            Layout.AddBlock(PdfBlock.WithSize(0, 10));
        }
    }

    internal void AddThreeColumnPartyBlocks()
    {
        if (!HasSeller && !HasBuyer && !HasShipping)
        {
            return;
        }

        double columnWidth = ContentWidth / 3;
        PdfTableBlock table = new PdfTableBlock(new double[] { columnWidth, columnWidth, columnWidth });
        PdfTableRow row = table.AddRow();
        AddPartyCell(row, HasSeller, Seller, "Seller");
        AddPartyCell(row, HasBuyer, Buyer, "Buyer");
        AddPartyCell(row, HasShipping, Shipping, "Ship To");
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(table));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    internal void AddPartyCell(PdfTableRow row, bool hasParty, PdfParty party, string label)
    {
        PdfTableCell cell;
        if (hasParty)
        {
            cell = row.AddStackBlock(party.CreateBlock(label, LabelStyle, BodyStyle));
        }
        else
        {
            cell = row.AddBlock(new PdfBlock());
        }

        cell.SetPadding(PdfInsets.Uniform(6));
        cell.SetBorder(new PdfInsets(0, 0, 0, 1));
        cell.SetBorderColor(PdfColor.Gray(0.86));
    }

    internal PdfInvoiceTotals GetTotalsForBuild()
    {
        if (HasTotals)
        {
            return Totals;
        }

        return PdfInvoiceTotals.FromLines(Lines);
    }

    internal void AddDetailRow(PdfTableBlock table, string label, string value)
    {
        PdfTableRow row = table.AddRow();
        AddBodyCell(row, label);
        AddBodyCell(row, value);
    }

    internal void AddTotalRow(PdfTableBlock table, string label, string value)
    {
        PdfTableRow row = table.AddRow();
        AddBodyCell(row, label);
        AddBodyCell(row, value);
    }

    internal void AddHeaderCell(PdfTableRow row, string text)
    {
        PdfTableCell cell = row.AddTextBlock(new PdfTextBlock(text, TableHeaderStyle));
        cell.SetPadding(PdfInsets.Uniform(4));
        cell.SetBackground(PdfColor.Gray(0.88));
        cell.SetBorder(new PdfInsets(0, 0, 0, 1));
        cell.SetBorderColor(PdfColor.Gray(0.7));
    }

    internal void AddBodyCell(PdfTableRow row, string text)
    {
        PdfTableCell cell = row.AddTextBlock(new PdfTextBlock(text, TableCellStyle));
        cell.SetPadding(PdfInsets.Uniform(4));
        cell.SetBorder(new PdfInsets(0, 0, 0, 1));
        cell.SetBorderColor(PdfColor.Gray(0.82));
    }

    internal void InitializeStyles()
    {
        TitleStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(24, PdfFont.Bold), 29, 0, 0, 0);
        LabelStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(9, PdfFont.Bold, PdfColor.Gray(0.28)), 11, 0, 0, 0);
        BodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(9, PdfFont.Regular), 12, 0, 0, 0);
        TableHeaderStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(8, PdfFont.Bold), 11, 0, 0, 0);
        TableCellStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(8, PdfFont.Regular), 11, 0, 0, 0);
        FooterStyle = PdfParagraphStyle.Create(PdfTextStyle.WithColor(8, PdfFont.Regular, PdfColor.Gray(0.35)), 10, 0, 0, 0, PdfTextAlignment.Right);
    }

    internal void ApplyPageFurniture()
    {
        Layout.SetHeaderTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock(GetPageHeaderText(), LabelStyle), 12));
        Layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", FooterStyle, 12));
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Invoice has already been built.");
        }
    }

    internal string GetInvoiceDateText()
    {
        if (HasTypedDates)
        {
            return FormatInvoiceDate(InvoiceDateValue);
        }

        return InvoiceDate;
    }

    internal string GetDocumentTitle()
    {
        if (DocumentType == PdfInvoiceDocumentType.CreditNote)
        {
            return "CREDIT NOTE";
        }

        return "INVOICE";
    }

    internal string GetDocumentNumberLabel()
    {
        if (DocumentType == PdfInvoiceDocumentType.CreditNote)
        {
            return "Credit Note #";
        }

        return "Invoice #";
    }

    internal string GetDocumentDateLabel()
    {
        if (DocumentType == PdfInvoiceDocumentType.CreditNote)
        {
            return "Credit Note Date";
        }

        return "Invoice Date";
    }

    internal string GetGrandTotalLabel()
    {
        if (DocumentType == PdfInvoiceDocumentType.CreditNote)
        {
            return "Credit Total";
        }

        return "Grand Total";
    }

    internal string GetPageHeaderText()
    {
        if (DocumentType == PdfInvoiceDocumentType.CreditNote)
        {
            return "Credit Note";
        }

        return "Invoice";
    }

    internal void AddCreditNoteReferenceRows(PdfTableBlock details)
    {
        if (DocumentType != PdfInvoiceDocumentType.CreditNote || !HasCreditNoteReference)
        {
            return;
        }

        AddDetailRow(details, "Original Invoice", OriginalInvoiceNumber);
        AddDetailRow(details, "Credit Reason", CreditReason);
    }

    internal string GetDueDateText()
    {
        if (HasTypedDates)
        {
            return FormatInvoiceDate(DueDateValue);
        }

        return DueDate;
    }

    internal bool HasDocumentImage(PdfImage image)
    {
        foreach (PdfImage registered in Document.Images)
        {
            if (registered == image)
            {
                return true;
            }
        }

        return false;
    }

    internal static int FindTaxBreakdownIndex(List<PdfInvoiceTaxBreakdown> breakdowns, double taxRate)
    {
        for (int index = 0; index < breakdowns.Count; index++)
        {
            if (breakdowns[index].TaxRate == taxRate)
            {
                return index;
            }
        }

        return -1;
    }

    internal static int FindTaxBreakdownInsertIndex(List<PdfInvoiceTaxBreakdown> breakdowns, double taxRate)
    {
        int index = 0;
        while (index < breakdowns.Count && breakdowns[index].TaxRate < taxRate)
        {
            index++;
        }

        return index;
    }

    internal void ConfigureFormattingCulture()
    {
        FormattingCulture = CreateFormattingCulture(FormattingOptions);
    }

    internal static PdfInvoiceFormattingOptions CopyFormattingOptions(PdfInvoiceFormattingOptions options)
    {
        PdfInvoiceFormattingOptions copy = new PdfInvoiceFormattingOptions();
        copy.CultureName = options.CultureName;
        copy.CurrencySymbol = options.CurrencySymbol;
        copy.CurrencyDecimalDigits = options.CurrencyDecimalDigits;
        copy.NumberDecimalDigits = options.NumberDecimalDigits;
        copy.DateFormat = options.DateFormat;
        return copy;
    }

    internal static CultureInfo CreateFormattingCulture(PdfInvoiceFormattingOptions options)
    {
        CultureInfo culture = (CultureInfo)CultureInfo.GetCultureInfo(options.CultureName).Clone();
        culture.NumberFormat.CurrencySymbol = options.CurrencySymbol;
        culture.NumberFormat.CurrencyDecimalDigits = options.CurrencyDecimalDigits;
        return culture;
    }

    internal static void ValidateFormattingOptions(PdfInvoiceFormattingOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        ValidateRequiredText(options.CultureName, "CultureName", "Invoice formatting culture name cannot be empty.");
        if (!IsBuiltInCultureName(options.CultureName))
        {
            throw new CultureNotFoundException("CultureName", options.CultureName, "Invoice formatting culture must be a built-in .NET culture.");
        }

        ValidateText(options.CurrencySymbol, "CurrencySymbol");
        if (options.CurrencyDecimalDigits < 0 || options.CurrencyDecimalDigits > 6)
        {
            throw new ArgumentOutOfRangeException("CurrencyDecimalDigits", "Currency decimal digits must be from zero through six.");
        }

        if (options.NumberDecimalDigits < 0 || options.NumberDecimalDigits > 6)
        {
            throw new ArgumentOutOfRangeException("NumberDecimalDigits", "Number decimal digits must be from zero through six.");
        }

        ValidateRequiredText(options.DateFormat, "DateFormat", "Invoice date format cannot be empty.");
        CultureInfo culture = CreateFormattingCulture(options);
        default(DateTime).ToString(options.DateFormat, culture);
    }

    internal static bool IsBuiltInCultureName(string cultureName)
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        for (int index = 0; index < cultures.Length; index++)
        {
            if (string.Equals(cultures[index].Name, cultureName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static void ApplyEmbeddedFont(PdfParagraphStyle style, PdfTrueTypeFont font)
    {
        style.TextStyle.TrueTypeFont = font;
    }

    internal static void ValidateText(string value, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal static void ValidateRequiredText(string value, string name, string message)
    {
        ValidateText(value, name);
        if (value.Length == 0)
        {
            throw new ArgumentException(message, name);
        }
    }

    internal static void ValidateFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Invoice formatting values must be finite.");
        }
    }

    internal static void ValidatePositive(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
