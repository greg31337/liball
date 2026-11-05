using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Keeps an ordered group of layout elements in the same page or column area when the complete group fits there.
/// </summary>
/// <remarks>
/// If the complete group is taller than a fresh flow area, its items paginate normally so splittable content can still make progress.
/// </remarks>
public class PdfKeepWithNextGroup : IPdfLayoutElement, IPdfKeepWithNextGroup
{
    internal List<IPdfLayoutElement> Items = new List<IPdfLayoutElement>();

    /// <summary>
    /// Creates an empty keep-with-next group.
    /// </summary>
    public PdfKeepWithNextGroup()
    {
    }

    /// <summary>
    /// Adds an item to the end of this keep-with-next group.
    /// </summary>
    /// <param name="item">The layout element to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the item is null.</exception>
    public void AddItem(IPdfLayoutElement item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        Items.Add(item);
    }

    /// <summary>
    /// Returns the number of items in this keep-with-next group.
    /// </summary>
    /// <returns>The number of grouped layout elements.</returns>
    public int GetItemCount()
    {
        return Items.Count;
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured size and combined fit status of the grouped items.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);
        double width = 0;
        double height = 0;
        bool fits = true;

        foreach (IPdfLayoutElement item in Items)
        {
            double remainingHeight = context.AvailableHeight - height;
            if (remainingHeight < 0)
            {
                remainingHeight = 0;
            }

            PdfLayoutResult result = item.Measure(new PdfLayoutContext(context.AvailableWidth, remainingHeight));
            if (result.Size.Width > width)
            {
                width = result.Size.Width;
            }

            height += result.Size.Height;
            if (!result.Fits)
            {
                fits = false;
            }
        }

        if (width > context.AvailableWidth || height > context.AvailableHeight)
        {
            fits = false;
        }

        return new PdfLayoutResult(new PdfLayoutSize(width, height), PdfBox.FromCoordinates(0, 0, width, height), fits);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured size and combined fit status of the grouped items.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);
        PdfLayoutResult groupResult = Measure(context.ToLayoutContext());
        double usedHeight = 0;

        foreach (IPdfLayoutElement item in Items)
        {
            double remainingHeight = context.AvailableHeight - usedHeight;
            if (remainingHeight < 0)
            {
                remainingHeight = 0;
            }

            PdfLayoutResult result = item.Measure(new PdfLayoutContext(context.AvailableWidth, remainingHeight));
            PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(context.Page, context.X, context.Y + groupResult.Size.Height - usedHeight - result.Size.Height, context.AvailableWidth, remainingHeight);
            item.Draw(childContext);
            usedHeight += result.Size.Height;
        }

        return groupResult;
    }

    /// <summary>
    /// Flows this group through the supplied page cursor while keeping its first item with all following group items when possible.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The size measured for the group at its selected starting area.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the cursor is null.</exception>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException("cursor");
        }

        PdfLayoutResult result = Measure(cursor.GetMeasureContext());
        if (result.Size.Height > cursor.GetRemainingHeight() && cursor.HasContentOnPage)
        {
            cursor.MoveToNextFlowArea();
            result = Measure(cursor.GetMeasureContext());
        }

        foreach (IPdfLayoutElement item in Items)
        {
            item.Flow(cursor);
        }

        return result;
    }
}
