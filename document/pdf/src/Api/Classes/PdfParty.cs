using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a party used by the PDF generation API.
/// </summary>
public class PdfParty : IPdfParty
{
    /// <summary>
    /// Stores the name used by this object.
    /// </summary>
    internal string Name = string.Empty;
    internal List<string> _lines = new List<string>();
    /// <summary>
    /// Exposes the lines currently stored by this object.
    /// </summary>
    internal IReadOnlyList<string> Lines = new List<string>();
    /// <summary>
    /// Stores the tax identifier used by this object.
    /// </summary>
    internal string TaxIdentifier = string.Empty;
    /// <summary>
    /// Stores the email used by this object.
    /// </summary>
    internal string Email = string.Empty;

    /// <summary>
    /// Initializes a new party.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfParty(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        Name = name;
        _lines = new List<string>();
        Lines = _lines;
        TaxIdentifier = string.Empty;
        Email = string.Empty;
    }

    /// <summary>
    /// Adds a line to this party.
    /// </summary>
    /// <param name="line">The line to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddLine(string line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        _lines.Add(line);
    }

    /// <summary>
    /// Sets the tax identifier used for subsequent PDF generation.
    /// </summary>
    /// <param name="taxIdentifier">The tax identifier to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetTaxIdentifier(string taxIdentifier)
    {
        if (taxIdentifier == null)
        {
            throw new ArgumentNullException("taxIdentifier");
        }

        TaxIdentifier = taxIdentifier;
    }

    /// <summary>
    /// Sets the email used for subsequent PDF generation.
    /// </summary>
    /// <param name="email">The email to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetEmail(string email)
    {
        if (email == null)
        {
            throw new ArgumentNullException("email");
        }

        Email = email;
    }

    internal PdfStackBlock CreateBlock(string label, PdfParagraphStyle labelStyle, PdfParagraphStyle bodyStyle)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (labelStyle == null)
        {
            throw new ArgumentNullException("labelStyle");
        }

        if (bodyStyle == null)
        {
            throw new ArgumentNullException("bodyStyle");
        }

        PdfStackBlock stack = PdfStackBlock.WithSpacing(2);
        stack.AddTextBlock(new PdfTextBlock(label, labelStyle));
        stack.AddTextBlock(new PdfTextBlock(Name, bodyStyle));
        for (int index = 0; index < _lines.Count; index++)
        {
            stack.AddTextBlock(new PdfTextBlock(_lines[index], bodyStyle));
        }

        if (TaxIdentifier.Length > 0)
        {
            stack.AddTextBlock(new PdfTextBlock("Tax ID: " + TaxIdentifier, bodyStyle));
        }

        if (Email.Length > 0)
        {
            stack.AddTextBlock(new PdfTextBlock(Email, bodyStyle));
        }

        return stack;
    }
}
