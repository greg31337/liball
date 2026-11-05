using System;

namespace pdf;

/// <summary>Builds a compact printable identification badge.</summary>
public class PdfBadge : IPdfBadge
{
    internal string Name = string.Empty;
    internal string Title = string.Empty;
    internal string Organization = string.Empty;
    internal string Identifier = string.Empty;
    internal PdfDocument Document = new PdfDocument();
    internal bool Built;

    /// <summary>Initializes a badge.</summary>
    /// <param name="name">The prominently displayed name.</param>
    public PdfBadge(string name)
    {
        ValidateText(name, "name");
        Name = name;
        Document = new PdfDocument();
        Built = false;
    }

    /// <summary>Sets the badge title.</summary>
    /// <param name="title">The title text.</param>
    public void SetTitle(string title)
    {
        EnsureNotBuilt();
        ValidateText(title, "title");
        Title = title;
    }

    /// <summary>Sets the represented organization.</summary>
    /// <param name="organization">The organization text.</param>
    public void SetOrganization(string organization)
    {
        EnsureNotBuilt();
        ValidateText(organization, "organization");
        Organization = organization;
    }

    /// <summary>Sets the badge identifier.</summary>
    /// <param name="identifier">The identifier text.</param>
    public void SetIdentifier(string identifier)
    {
        EnsureNotBuilt();
        ValidateText(identifier, "identifier");
        Identifier = identifier;
    }

    /// <summary>Builds the badge.</summary>
    /// <returns>The built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            PdfPage page = Document.AddPageWithDimensions(new PdfPageSizeFactory().Custom(252, 144));
            PdfTextStyle organizationStyle = PdfTextStyle.WithColor(11, PdfFont.Bold, PdfColor.Gray(1));
            PdfTextStyle nameStyle = PdfTextStyle.WithFont(20, PdfFont.Bold);
            PdfTextStyle detailStyle = PdfTextStyle.WithColor(10, PdfFont.Regular, PdfColor.Gray(0.3));
            page.Canvas.SetFillColorValue(PdfColor.Gray(0.18));
            page.Canvas.FillRectangle(0, 112, 252, 32);
            page.Canvas.SetStrokeColorValue(PdfColor.Gray(0.45));
            page.Canvas.SetLineWidth(1);
            page.Canvas.StrokeRectangle(1, 1, 250, 142);
            if (Organization.Length > 0)
            {
                page.Canvas.DrawAlignedStyledText(Organization, 12, 121, 228, organizationStyle, PdfTextAlignment.Center);
            }

            page.Canvas.DrawAlignedStyledText(Name, 12, 75, 228, nameStyle, PdfTextAlignment.Center);
            if (Title.Length > 0)
            {
                page.Canvas.DrawAlignedStyledText(Title, 12, 51, 228, detailStyle, PdfTextAlignment.Center);
            }

            if (Identifier.Length > 0)
            {
                page.Canvas.DrawAlignedStyledText(Identifier, 12, 18, 228, detailStyle, PdfTextAlignment.Center);
            }

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
        if (Built) { throw new InvalidOperationException("Badge has already been built."); }
    }
}
