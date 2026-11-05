using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>Builds a paginated contract with parties, clauses, and signature lines.</summary>
public class PdfContract : IPdfContract
{
    internal string Title = string.Empty;
    internal string FirstParty = string.Empty;
    internal string SecondParty = string.Empty;
    internal string EffectiveDate = string.Empty;
    internal List<PdfContractClause> Clauses = new List<PdfContractClause>();
    internal List<PdfContractSignature> Signatures = new List<PdfContractSignature>();
    internal PdfDocument Document = new PdfDocument();
    internal bool Built;

    /// <summary>Initializes a contract.</summary>
    /// <param name="title">The contract title.</param>
    public PdfContract(string title)
    {
        ValidateText(title, "title");
        Title = title;
        Clauses = new List<PdfContractClause>();
        Signatures = new List<PdfContractSignature>();
        Document = new PdfDocument();
        Built = false;
    }

    /// <summary>Sets the two contracting parties.</summary>
    /// <param name="firstParty">The first party.</param>
    /// <param name="secondParty">The second party.</param>
    public void SetParties(string firstParty, string secondParty)
    {
        EnsureNotBuilt();
        ValidateText(firstParty, "firstParty");
        ValidateText(secondParty, "secondParty");
        FirstParty = firstParty;
        SecondParty = secondParty;
    }

    /// <summary>Sets the displayed effective date.</summary>
    /// <param name="dateText">The effective date.</param>
    public void SetEffectiveDate(string dateText)
    {
        EnsureNotBuilt();
        ValidateText(dateText, "dateText");
        EffectiveDate = dateText;
    }

    /// <summary>Adds a numbered clause.</summary>
    /// <param name="clause">The clause to add.</param>
    public void AddClause(PdfContractClause clause)
    {
        EnsureNotBuilt();
        if (clause == null) { throw new ArgumentNullException("clause"); }
        Clauses.Add(clause);
    }

    /// <summary>Adds a signature line.</summary>
    /// <param name="signature">The signature line to add.</param>
    public void AddSignature(PdfContractSignature signature)
    {
        EnsureNotBuilt();
        if (signature == null) { throw new ArgumentNullException("signature"); }
        Signatures.Add(signature);
    }

    /// <summary>Builds the contract document.</summary>
    /// <returns>The built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfLayoutDocument layout = new PdfLayoutDocument(Document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
            PdfParagraphStyle titleStyle = PdfParagraphStyle.Create(PdfTextStyle.WithFont(20, PdfFont.Bold), 24, 0, 0, 0, PdfTextAlignment.Center);
            PdfParagraphStyle headingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(11, PdfFont.Bold), 15, 0, 0, 0);
            PdfParagraphStyle bodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
            layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", bodyStyle, 12));
            layout.AddTextBlock(new PdfTextBlock(Title, titleStyle));
            layout.AddBlock(PdfBlock.WithSize(0, 18));
            if (FirstParty.Length > 0 && SecondParty.Length > 0)
            {
                AddOptionalLine(layout, "Between " + FirstParty + " and " + SecondParty, bodyStyle);
            }

            if (EffectiveDate.Length > 0)
            {
                AddOptionalLine(layout, "Effective date: " + EffectiveDate, bodyStyle);
            }

            layout.AddBlock(PdfBlock.WithSize(0, 12));
            for (int index = 0; index < Clauses.Count; index++)
            {
                layout.AddTextBlock(new PdfTextBlock((index + 1) + ". " + Clauses[index].Heading, headingStyle));
                layout.AddTextBlock(new PdfTextBlock(Clauses[index].Text, bodyStyle));
                layout.AddBlock(PdfBlock.WithSize(0, 12));
            }

            for (int index = 0; index < Signatures.Count; index++)
            {
                layout.AddTextBlock(new PdfTextBlock("______________________________", bodyStyle));
                layout.AddTextBlock(new PdfTextBlock(Signatures[index].Signer + ", " + Signatures[index].Party, bodyStyle));
                layout.AddBlock(PdfBlock.WithSize(0, 16));
            }

            layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void AddOptionalLine(PdfLayoutDocument layout, string text, PdfParagraphStyle style)
    {
        if (text.Length > 0) { layout.AddTextBlock(new PdfTextBlock(text, style)); }
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null) { throw new ArgumentNullException(name); }
    }

    internal void EnsureNotBuilt()
    {
        if (Built) { throw new InvalidOperationException("Contract has already been built."); }
    }
}
