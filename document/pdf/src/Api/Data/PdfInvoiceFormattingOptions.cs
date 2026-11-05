namespace pdf;

/// <summary>
/// Stores deterministic culture, currency, number, and date formatting choices for an invoice.
/// </summary>
public class PdfInvoiceFormattingOptions
{
    /// <summary>Stores the built-in .NET culture name used for separators and symbol placement.</summary>
    public string CultureName;
    /// <summary>Stores the currency symbol rendered by locale-aware currency formatting.</summary>
    public string CurrencySymbol;
    /// <summary>Stores the currency decimal digit count.</summary>
    public int CurrencyDecimalDigits;
    /// <summary>Stores the ordinary number decimal digit count.</summary>
    public int NumberDecimalDigits;
    /// <summary>Stores the explicit date format string.</summary>
    public string DateFormat;

    /// <summary>Creates US-English formatting with two decimal digits and ISO-style dates.</summary>
    public PdfInvoiceFormattingOptions()
    {
        CultureName = "en-US";
        CurrencySymbol = "$";
        CurrencyDecimalDigits = 2;
        NumberDecimalDigits = 2;
        DateFormat = "yyyy-MM-dd";
    }
}
