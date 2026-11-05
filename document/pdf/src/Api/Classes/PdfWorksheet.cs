using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>Builds a printable worksheet with prompts and ruled response areas.</summary>
public class PdfWorksheet : IPdfWorksheet
{
    internal string Title = string.Empty;
    internal string Instructions = string.Empty;
    internal string Footer = string.Empty;
    internal List<PdfWorksheetQuestion> Questions = new List<PdfWorksheetQuestion>();
    internal PdfDocument Document = new PdfDocument();
    internal bool Built;

    /// <summary>Initializes a worksheet.</summary>
    /// <param name="title">The worksheet title.</param>
    public PdfWorksheet(string title)
    {
        ValidateText(title, "title");
        Title = title;
        Questions = new List<PdfWorksheetQuestion>();
        Document = new PdfDocument();
        Built = false;
    }

    /// <summary>Sets worksheet instructions.</summary>
    /// <param name="instructions">The instructions text.</param>
    public void SetInstructions(string instructions)
    {
        EnsureNotBuilt();
        ValidateText(instructions, "instructions");
        Instructions = instructions;
    }

    /// <summary>Adds a worksheet question.</summary>
    /// <param name="question">The question to add.</param>
    public void AddQuestion(PdfWorksheetQuestion question)
    {
        EnsureNotBuilt();
        if (question == null) { throw new ArgumentNullException("question"); }
        Questions.Add(question);
    }

    /// <summary>Sets footer text.</summary>
    /// <param name="footer">The footer text.</param>
    public void SetFooter(string footer)
    {
        EnsureNotBuilt();
        ValidateText(footer, "footer");
        Footer = footer;
    }

    /// <summary>Builds the worksheet.</summary>
    /// <returns>The built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfLayoutDocument layout = new PdfLayoutDocument(Document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(54, 54, 54, 54));
            PdfParagraphStyle titleStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(19, PdfFont.Bold), 24, 0, 0, 0);
            PdfParagraphStyle bodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
            PdfParagraphStyle questionStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(11, PdfFont.Bold), 15, 0, 0, 0);
            if (Footer.Length > 0) { layout.SetFooterTemplate(PdfPageTemplate.FromPageText(Footer + "   {page}/{total}", bodyStyle, 12)); }
            layout.AddTextBlock(new PdfTextBlock(Title, titleStyle));
            layout.AddTextBlock(new PdfTextBlock("Name: __________________________   Date: ______________", bodyStyle));
            if (Instructions.Length > 0) { layout.AddTextBlock(new PdfTextBlock(Instructions, bodyStyle)); }
            layout.AddBlock(PdfBlock.WithSize(0, 14));
            for (int questionIndex = 0; questionIndex < Questions.Count; questionIndex++)
            {
                layout.AddTextBlock(new PdfTextBlock((questionIndex + 1) + ". " + Questions[questionIndex].Prompt, questionStyle));
                for (int lineIndex = 0; lineIndex < Questions[questionIndex].ResponseLines; lineIndex++)
                {
                    layout.AddTextBlock(new PdfTextBlock("________________________________________________________________________", bodyStyle));
                }

                layout.AddBlock(PdfBlock.WithSize(0, 10));
            }

            layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void ValidateText(string text, string name)
    {
        if (text == null) { throw new ArgumentNullException(name); }
    }

    internal void EnsureNotBuilt()
    {
        if (Built) { throw new InvalidOperationException("Worksheet has already been built."); }
    }
}
