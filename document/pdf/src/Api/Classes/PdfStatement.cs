using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>Builds an account statement with a calculated running balance.</summary>
public class PdfStatement : IPdfStatement
{
    internal string AccountName = string.Empty;
    internal string AccountNumber = string.Empty;
    internal string PeriodStart = string.Empty;
    internal string PeriodEnd = string.Empty;
    internal string CurrencySymbol = "$";
    internal string Notes = string.Empty;
    internal double OpeningBalance;
    internal List<PdfStatementEntry> Entries = new List<PdfStatementEntry>();
    internal PdfDocument Document = new PdfDocument();
    internal bool Built;

    /// <summary>Initializes an account statement.</summary>
    /// <param name="accountName">The account holder name.</param>
    /// <param name="accountNumber">The displayed account number.</param>
    public PdfStatement(string accountName, string accountNumber)
    {
        ValidateText(accountName, "accountName");
        ValidateText(accountNumber, "accountNumber");
        AccountName = accountName;
        AccountNumber = accountNumber;
        Entries = new List<PdfStatementEntry>();
        Document = new PdfDocument();
        Built = false;
    }

    /// <summary>Sets the statement period.</summary>
    /// <param name="startText">The period start.</param>
    /// <param name="endText">The period end.</param>
    public void SetPeriod(string startText, string endText)
    {
        EnsureNotBuilt();
        ValidateText(startText, "startText");
        ValidateText(endText, "endText");
        PeriodStart = startText;
        PeriodEnd = endText;
    }

    /// <summary>Sets the opening balance.</summary>
    /// <param name="balance">The opening balance.</param>
    public void SetOpeningBalance(double balance)
    {
        EnsureNotBuilt();
        ValidateAmount(balance, "balance");
        OpeningBalance = balance;
    }

    /// <summary>Sets the displayed currency symbol.</summary>
    /// <param name="currencySymbol">The currency symbol.</param>
    public void SetCurrencySymbol(string currencySymbol)
    {
        EnsureNotBuilt();
        ValidateText(currencySymbol, "currencySymbol");
        CurrencySymbol = currencySymbol;
    }

    /// <summary>Adds a statement transaction.</summary>
    /// <param name="entry">The transaction to add.</param>
    public void AddEntry(PdfStatementEntry entry)
    {
        EnsureNotBuilt();
        if (entry == null) { throw new ArgumentNullException("entry"); }
        Entries.Add(entry);
    }

    /// <summary>Sets optional notes.</summary>
    /// <param name="notes">The notes text.</param>
    public void SetNotes(string notes)
    {
        EnsureNotBuilt();
        ValidateText(notes, "notes");
        Notes = notes;
    }

    /// <summary>Returns the calculated closing balance.</summary>
    /// <returns>The opening balance plus all entries.</returns>
    public double GetClosingBalance()
    {
        double balance = OpeningBalance;
        for (int index = 0; index < Entries.Count; index++) { balance += Entries[index].Amount; }
        return balance;
    }

    /// <summary>Builds the statement.</summary>
    /// <returns>The built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfLayoutDocument layout = new PdfLayoutDocument(Document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
            PdfParagraphStyle titleStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(20, PdfFont.Bold), 24, 0, 0, 0);
            PdfParagraphStyle bodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
            PdfParagraphStyle boldStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Bold), 14, 0, 0, 0);
            layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", bodyStyle, 12));
            layout.AddTextBlock(new PdfTextBlock("ACCOUNT STATEMENT", titleStyle));
            layout.AddTextBlock(new PdfTextBlock(AccountName + "   Account: " + AccountNumber, bodyStyle));
            if (PeriodStart.Length > 0 && PeriodEnd.Length > 0)
            {
                layout.AddTextBlock(new PdfTextBlock("Period: " + PeriodStart + " to " + PeriodEnd, bodyStyle));
            }

            layout.AddBlock(PdfBlock.WithSize(0, 14));
            PdfTableBlock table = new PdfTableBlock(new double[] { 82, 226, 80, 80 });
            PdfTableRow header = table.AddHeaderRow();
            header.AddTextBlock(new PdfTextBlock("Date", boldStyle));
            header.AddTextBlock(new PdfTextBlock("Description", boldStyle));
            header.AddTextBlock(new PdfTextBlock("Amount", boldStyle));
            header.AddTextBlock(new PdfTextBlock("Balance", boldStyle));
            double runningBalance = OpeningBalance;
            AddBalanceRow(table, "", "Opening balance", "", runningBalance, bodyStyle);
            for (int index = 0; index < Entries.Count; index++)
            {
                runningBalance += Entries[index].Amount;
                AddBalanceRow(table, Entries[index].DateText, Entries[index].Description, FormatAmount(Entries[index].Amount), runningBalance, bodyStyle);
            }

            layout.AddTableBlock(table);
            layout.AddBlock(PdfBlock.WithSize(0, 14));
            layout.AddTextBlock(new PdfTextBlock("Closing balance: " + FormatAmount(GetClosingBalance()), boldStyle));
            if (Notes.Length > 0) { layout.AddTextBlock(new PdfTextBlock(Notes, bodyStyle)); }
            layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void AddBalanceRow(PdfTableBlock table, string dateText, string description, string amount, double balance, PdfParagraphStyle style)
    {
        PdfTableRow row = table.AddRow();
        row.AddTextBlock(new PdfTextBlock(dateText, style));
        row.AddTextBlock(new PdfTextBlock(description, style));
        row.AddTextBlock(new PdfTextBlock(amount, style));
        row.AddTextBlock(new PdfTextBlock(FormatAmount(balance), style));
    }

    internal string FormatAmount(double amount)
    {
        return CurrencySymbol + amount.ToString("0.00", CultureInfo.InvariantCulture);
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null) { throw new ArgumentNullException(name); }
    }

    internal void ValidateAmount(double amount, string name)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount)) { throw new ArgumentOutOfRangeException(name, "Statement balance must be finite."); }
    }

    internal void EnsureNotBuilt()
    {
        if (Built) { throw new InvalidOperationException("Statement has already been built."); }
    }
}
