using System;

namespace pdf;

internal class PdfSemanticLayoutElement
{
    internal IPdfLayoutElement Content = new PdfBlock();
    internal PdfElementStyle Style = new PdfElementStyle();
    internal string Role = "Div";
    internal string Description = string.Empty;
    internal bool KeepTogether;

    internal PdfSemanticLayoutElement(IPdfLayoutElement content, string role, string description)
    {
        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        if (role == null)
        {
            throw new ArgumentNullException("role");
        }

        if (role.Length == 0)
        {
            throw new ArgumentException("Semantic layout role cannot be empty.", "role");
        }

        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        Content = content;
        Style = new PdfElementStyle();
        Role = role;
        Description = description;
        KeepTogether = false;
    }

    internal void SetStyle(PdfElementStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        Style = style.Copy();
    }

    internal void SetKeepTogether()
    {
        KeepTogether = true;
    }

    internal void ClearKeepTogether()
    {
        KeepTogether = false;
    }

    internal bool IsKeepTogether()
    {
        return KeepTogether;
    }

    internal void SetAccessibilityDescription(string description)
    {
        if (description == null)
        {
            throw new ArgumentNullException("description");
        }

        Description = description;
    }

    internal string GetAccessibilityRole()
    {
        return Role;
    }

    internal string GetAccessibilityDescription()
    {
        return Description;
    }

    internal PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        double insetWidth = Style.Padding.GetHorizontal() + Style.Border.GetHorizontal() + Style.Margin.GetHorizontal();
        double insetHeight = Style.Padding.GetVertical() + Style.Border.GetVertical() + Style.Margin.GetVertical();
        double availableWidth = context.AvailableWidth - insetWidth;
        double availableHeight = context.AvailableHeight - insetHeight;
        if (availableWidth <= 0)
        {
            throw new ArgumentException("Semantic layout insets must leave positive content width.");
        }

        if (availableHeight < 0)
        {
            availableHeight = 0;
        }

        PdfLayoutResult contentResult = Content.Measure(new PdfLayoutContext(availableWidth, availableHeight));
        PdfBox box = new PdfBox(
            new PdfRectangle(0, 0, contentResult.Size.Width, contentResult.Size.Height),
            Style.Padding,
            Style.Border,
            Style.Margin);
        PdfLayoutSize size = box.GetOuterSize();
        bool fits = contentResult.Fits && PdfBlock.SizeFits(size, context);
        return new PdfLayoutResult(size, box, fits);
    }

    internal PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);

        PdfLayoutResult result = Measure(context.ToLayoutContext());
        Style.DrawBox(context, result, Style.Margin, Style.Border);
        double contentX = context.X + Style.Margin.Left + Style.Border.Left + Style.Padding.Left;
        double contentY = context.Y + Style.Margin.Bottom + Style.Border.Bottom + Style.Padding.Bottom;
        PdfLayoutDrawContext contentContext = new PdfLayoutDrawContext(
            context.Page,
            contentX,
            contentY,
            result.Box.ContentRectangle.Width,
            result.Box.ContentRectangle.Height);
        if (context.Page.OwnerDocument != null)
        {
            RegisterAccessibility(context.Page.OwnerDocument, context.Page);
        }

        Content.Draw(contentContext);
        return result;
    }

    internal PdfLayoutResult Flow(PdfPageCursor cursor, IPdfLayoutElement owner)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException("cursor");
        }

        if (owner == null)
        {
            throw new ArgumentNullException("owner");
        }

        if (KeepTogether)
        {
            PdfLayoutResult measured = owner.Measure(cursor.GetMeasureContext());
            if (measured.Size.Height > cursor.GetRemainingHeight() && cursor.HasContentOnPage)
            {
                cursor.MoveToNextFlowArea();
            }
        }

        return cursor.DrawItem(owner);
    }

    internal void RegisterAccessibility(PdfDocument document, PdfPage page)
    {
        document.AddAccessibilityNodeWithDescription(page, Role, Description);
    }
}
