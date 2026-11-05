using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a table element in the paginated PDF layout system.
/// </summary>
public class PdfTableBlock : IPdfLayoutElement, IPdfTableBlock
{
    /// <summary>
    /// Exposes the column widths currently stored by this object.
    /// </summary>
    internal double[] ColumnWidths = new double[0];
    internal PdfTableColumnDefinition[] ColumnDefinitions = new PdfTableColumnDefinition[0];
    internal double[] ResolvedColumnWidths = new double[0];
    internal bool UsesColumnDefinitions;
    internal PdfTableRowSplitPolicy RowSplitPolicy;
    /// <summary>
    /// Stores the padding used by this object.
    /// </summary>
    internal PdfInsets Padding = PdfInsets.None();
    /// <summary>
    /// Stores the border used by this object.
    /// </summary>
    internal PdfInsets Border = PdfInsets.None();
    /// <summary>
    /// Stores the margin used by this object.
    /// </summary>
    internal PdfInsets Margin = PdfInsets.None();
    internal PdfElementStyle Style = new PdfElementStyle();
    internal bool HasTableStyle;
    internal PdfTableStyle TableStyle = new PdfTableStyle();
    internal List<PdfTableRow> _headerRows = new List<PdfTableRow>();
    internal List<PdfTableRow> _rows = new List<PdfTableRow>();
    internal List<PdfTableRow> _footerRows = new List<PdfTableRow>();
    /// <summary>
    /// Exposes the header rows currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfTableRow> HeaderRows = new List<PdfTableRow>();
    /// <summary>
    /// Exposes the rows currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfTableRow> Rows = new List<PdfTableRow>();
    /// <summary>
    /// Exposes the footer rows currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfTableRow> FooterRows = new List<PdfTableRow>();

    /// <summary>
    /// Initializes a new table block.
    /// </summary>
    /// <param name="columnWidths">The column widths to use for this operation.</param>
    public PdfTableBlock(double[] columnWidths)
    {
        Initialize(columnWidths);
    }

    /// <summary>
    /// Creates a single-column table block with unit width.
    /// </summary>
    /// <returns>The resulting table block.</returns>
    internal static PdfTableBlock Default()
    {
        return new PdfTableBlock(new double[] { 1 });
    }

    /// <summary>
    /// Adds a row to this table block and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableRow AddRow()
    {
        PdfTableRow row = new PdfTableRow();
        _rows.Add(row);
        return row;
    }

    /// <summary>
    /// Adds a header row to this table block and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableRow AddHeaderRow()
    {
        PdfTableRow row = new PdfTableRow();
        _headerRows.Add(row);
        return row;
    }

    /// <summary>
    /// Adds a footer row to this table block and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableRow AddFooterRow()
    {
        PdfTableRow row = new PdfTableRow();
        _footerRows.Add(row);
        return row;
    }

    /// <summary>
    /// Adds a header row to this table block.
    /// </summary>
    /// <param name="row">The row to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddExistingHeaderRow(PdfTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        _headerRows.Add(row);
    }

    /// <summary>
    /// Adds a row to this table block.
    /// </summary>
    /// <param name="row">The row to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddExistingRow(PdfTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        _rows.Add(row);
    }

    /// <summary>
    /// Adds a footer row to this table block.
    /// </summary>
    /// <param name="row">The row to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddExistingFooterRow(PdfTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        _footerRows.Add(row);
    }

    /// <summary>
    /// Sets the padding used for subsequent PDF generation.
    /// </summary>
    /// <param name="padding">The padding to use for this operation.</param>
    public void SetPadding(PdfInsets padding)
    {
        ValidateInsets(padding, "padding");
        Padding = padding;
    }

    /// <summary>
    /// Sets the border used for subsequent PDF generation.
    /// </summary>
    /// <param name="border">The border to use for this operation.</param>
    public void SetBorder(PdfInsets border)
    {
        ValidateInsets(border, "border");
        Border = border;
    }

    /// <summary>
    /// Sets the margin used for subsequent PDF generation.
    /// </summary>
    /// <param name="margin">The margin to use for this operation.</param>
    public void SetMargin(PdfInsets margin)
    {
        ValidateInsets(margin, "margin");
        Margin = margin;
    }

    /// <summary>
    /// Applies a defensive copy of a reusable element style to the table container.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    public void SetStyle(PdfElementStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        Style = style.Copy();
        Padding = PdfElementStyle.CopyInsets(Style.Padding);
        Border = PdfElementStyle.CopyInsets(Style.Border);
        Margin = PdfElementStyle.CopyInsets(Style.Margin);
    }

    /// <summary>
    /// Applies reusable table and header, body, and footer cell styles.
    /// </summary>
    /// <param name="style">The table style to apply.</param>
    public void SetTableStyle(PdfTableStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        TableStyle = style.Copy();
        HasTableStyle = true;
        SetStyle(TableStyle.Table);
        PrepareTableStyles();
    }

    /// <summary>
    /// Replaces fixed widths with reusable column sizing definitions.
    /// </summary>
    /// <param name="definitions">One definition for each table column.</param>
    public void SetColumnDefinitions(PdfTableColumnDefinition[] definitions)
    {
        if (definitions == null)
        {
            throw new ArgumentNullException("definitions");
        }

        if (definitions.Length == 0)
        {
            throw new ArgumentException("Table must have at least one column definition.");
        }

        PdfTableColumnDefinition[] copied = new PdfTableColumnDefinition[definitions.Length];
        for (int index = 0; index < definitions.Length; index++)
        {
            if (definitions[index] == null)
            {
                throw new ArgumentNullException("definitions");
            }

            PdfTableColumnDefinition.Validate(definitions[index].Sizing, definitions[index].Value);
            copied[index] = definitions[index].Copy();
        }

        ColumnDefinitions = copied;
        ColumnWidths = new double[copied.Length];
        ResolvedColumnWidths = new double[copied.Length];
        for (int index = 0; index < copied.Length; index++)
        {
            double initial = copied[index].Value;
            if (initial <= 0)
            {
                initial = 1;
            }

            ColumnWidths[index] = initial;
            ResolvedColumnWidths[index] = initial;
        }

        UsesColumnDefinitions = true;
    }

    /// <summary>
    /// Sets whether pagination may break row-span groups.
    /// </summary>
    /// <param name="policy">The row split policy.</param>
    public void SetRowSplitPolicy(PdfTableRowSplitPolicy policy)
    {
        if (policy != PdfTableRowSplitPolicy.Prevent && policy != PdfTableRowSplitPolicy.Allow)
        {
            throw new ArgumentOutOfRangeException("policy", "Table row split policy is not supported.");
        }

        RowSplitPolicy = policy;
    }

    /// <summary>
    /// Returns the most recently resolved width for a column.
    /// </summary>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <returns>The resolved width in PDF points.</returns>
    public double GetResolvedColumnWidth(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= ResolvedColumnWidths.Length)
        {
            throw new ArgumentOutOfRangeException("columnIndex", "Table column index is outside the resolved width range.");
        }

        return ResolvedColumnWidths[columnIndex];
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        return MeasureRows(context, 0, _rows.Count);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        return DrawRows(context, 0, _rows.Count);
    }

    /// <summary>
    /// Flows this layout element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawTableBlock(this);
    }

    internal PdfBox CreateMeasuredBox(double contentWidth, double contentHeight)
    {
        PdfRectangle contentRectangle = new PdfRectangle(0, 0, contentWidth, contentHeight);
        return new PdfBox(contentRectangle, Padding, Border, Margin);
    }

    internal PdfLayoutResult MeasureRows(PdfLayoutContext context, int startRowIndex, int rowCount)
    {
        PdfBlock.ValidateContext(context);
        ValidateRowRange(startRowIndex, rowCount);
        PrepareTableStyles();
        ResolveColumnWidths(context);

        double contentAvailableHeight = GetContentAvailableHeight(context);
        double contentWidth = GetContentWidth();
        PdfTableGroupLayout headerLayout = CreateGroupLayout(_headerRows, contentAvailableHeight);
        PdfTableGroupLayout bodyLayout = CreateGroupLayout(_rows, contentAvailableHeight);
        PdfTableGroupLayout footerLayout = CreateGroupLayout(_footerRows, contentAvailableHeight);
        double contentHeight = SumRowHeights(headerLayout, 0, headerLayout.RowHeights.Length);
        contentHeight += SumRowHeights(bodyLayout, startRowIndex, rowCount);
        contentHeight += SumRowHeights(footerLayout, 0, footerLayout.RowHeights.Length);
        bool childrenFit = headerLayout.Fits && footerLayout.Fits && RangeFits(bodyLayout, startRowIndex, rowCount);

        PdfBox box = CreateMeasuredBox(contentWidth, contentHeight);
        PdfLayoutSize size = box.GetOuterSize();
        bool fits = PdfBlock.SizeFits(size, context) && childrenFit;

        return new PdfLayoutResult(size, box, fits);
    }

    internal PdfLayoutResult DrawRows(PdfLayoutDrawContext context, int startRowIndex, int rowCount)
    {
        PdfBlock.ValidateDrawContext(context);
        ValidateRowRange(startRowIndex, rowCount);

        PdfLayoutResult result = MeasureRows(context.ToLayoutContext(), startRowIndex, rowCount);
        Style.DrawBox(context, result, Margin, Border);
        double contentX = context.X + Margin.Left + Border.Left + Padding.Left;
        double contentY = context.Y + Margin.Bottom + Border.Bottom + Padding.Bottom;
        double currentTop = contentY + result.Box.ContentRectangle.Height;
        double availableHeight = GetContentAvailableHeight(context.ToLayoutContext());
        PdfTableGroupLayout headerLayout = CreateGroupLayout(_headerRows, availableHeight);
        PdfTableGroupLayout bodyLayout = CreateGroupLayout(_rows, availableHeight);
        PdfTableGroupLayout footerLayout = CreateGroupLayout(_footerRows, availableHeight);

        currentTop = DrawGroupRange(context.Page, headerLayout, contentX, currentTop, 0, headerLayout.RowHeights.Length);
        currentTop = DrawGroupRange(context.Page, bodyLayout, contentX, currentTop, startRowIndex, rowCount);
        currentTop = DrawGroupRange(context.Page, footerLayout, contentX, currentTop, 0, footerLayout.RowHeights.Length);

        return result;
    }

    /// <summary>
    /// Returns the row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int GetRowCount()
    {
        return _rows.Count;
    }

    /// <summary>
    /// Returns the header row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int GetHeaderRowCount()
    {
        return _headerRows.Count;
    }

    /// <summary>
    /// Returns the footer row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int GetFooterRowCount()
    {
        return _footerRows.Count;
    }

    internal double MeasureRowHeight(int rowIndex, double availableHeight)
    {
        ValidateRowIndex(rowIndex);
        PdfTableGroupLayout layout = CreateGroupLayout(_rows, availableHeight);
        return layout.RowHeights[rowIndex];
    }

    /// <summary>
    /// Measures a header rows height without emitting drawing commands.
    /// </summary>
    /// <param name="availableHeight">The available height in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double MeasureHeaderRowsHeight(double availableHeight)
    {
        PdfTableGroupLayout layout = CreateGroupLayout(_headerRows, availableHeight);
        return SumRowHeights(layout, 0, layout.RowHeights.Length);
    }

    /// <summary>
    /// Measures a footer rows height without emitting drawing commands.
    /// </summary>
    /// <param name="availableHeight">The available height in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double MeasureFooterRowsHeight(double availableHeight)
    {
        PdfTableGroupLayout layout = CreateGroupLayout(_footerRows, availableHeight);
        return SumRowHeights(layout, 0, layout.RowHeights.Length);
    }

    internal double MeasureBodyRowsHeight(int startRowIndex, int rowCount, double availableHeight)
    {
        ValidateRowRange(startRowIndex, rowCount);
        PdfTableGroupLayout layout = CreateGroupLayout(_rows, availableHeight);
        return SumRowHeights(layout, startRowIndex, rowCount);
    }

    internal int GetPaginationRowCount(int startRowIndex)
    {
        ValidateRowIndex(startRowIndex);
        if (RowSplitPolicy == PdfTableRowSplitPolicy.Allow)
        {
            return 1;
        }

        PdfTableGroupLayout layout = CreateGroupLayout(_rows, double.MaxValue);
        int endRow = startRowIndex + 1;
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (PdfTableCellPlacement placement in layout.Placements)
            {
                int placementEnd = placement.RowIndex + placement.RowSpan;
                if (placement.RowIndex < endRow && placementEnd > startRowIndex && placementEnd > endRow)
                {
                    endRow = placementEnd;
                    changed = true;
                }
            }
        }

        return endRow - startRowIndex;
    }

    internal double GetOuterVertical()
    {
        return Padding.GetVertical() + Border.GetVertical() + Margin.GetVertical();
    }

    internal void ResolveColumnWidths(PdfLayoutContext context)
    {
        if (!UsesColumnDefinitions)
        {
            ResolvedColumnWidths = new double[ColumnWidths.Length];
            for (int index = 0; index < ColumnWidths.Length; index++)
            {
                ResolvedColumnWidths[index] = ColumnWidths[index];
            }
            return;
        }

        double availableWidth = context.AvailableWidth - Padding.GetHorizontal() - Border.GetHorizontal() - Margin.GetHorizontal();
        if (availableWidth < 0)
        {
            availableWidth = 0;
        }

        double[] naturalWidths = new double[ColumnDefinitions.Length];
        for (int index = 0; index < naturalWidths.Length; index++)
        {
            naturalWidths[index] = 1;
        }

        UpdateNaturalWidths(_headerRows, naturalWidths, availableWidth, context.AvailableHeight);
        UpdateNaturalWidths(_rows, naturalWidths, availableWidth, context.AvailableHeight);
        UpdateNaturalWidths(_footerRows, naturalWidths, availableWidth, context.AvailableHeight);

        ResolvedColumnWidths = new double[ColumnDefinitions.Length];
        double fixedWidth = 0;
        double totalWeight = 0;
        for (int index = 0; index < ColumnDefinitions.Length; index++)
        {
            PdfTableColumnDefinition definition = ColumnDefinitions[index];
            double width = naturalWidths[index];
            if (definition.Sizing == PdfTableColumnSizing.Fixed)
            {
                width = definition.Value;
            }
            else if (definition.Sizing == PdfTableColumnSizing.Minimum && width < definition.Value)
            {
                width = definition.Value;
            }
            else if (definition.Sizing == PdfTableColumnSizing.Maximum && width > definition.Value)
            {
                width = definition.Value;
            }
            else if (definition.Sizing == PdfTableColumnSizing.Proportional)
            {
                width = 0;
                totalWeight += definition.Value;
            }

            ResolvedColumnWidths[index] = width;
            fixedWidth += width;
        }

        double remainingWidth = availableWidth - fixedWidth;
        if (remainingWidth < totalWeight)
        {
            remainingWidth = totalWeight;
        }

        if (totalWeight > 0)
        {
            for (int index = 0; index < ColumnDefinitions.Length; index++)
            {
                if (ColumnDefinitions[index].Sizing == PdfTableColumnSizing.Proportional)
                {
                    ResolvedColumnWidths[index] = remainingWidth * ColumnDefinitions[index].Value / totalWeight;
                }
            }
        }

        ExpandColumnsForSpans(_headerRows, availableWidth, context.AvailableHeight);
        ExpandColumnsForSpans(_rows, availableWidth, context.AvailableHeight);
        ExpandColumnsForSpans(_footerRows, availableWidth, context.AvailableHeight);
    }

    internal void UpdateNaturalWidths(List<PdfTableRow> rows, double[] naturalWidths, double availableWidth, double availableHeight)
    {
        List<PdfTableCellPlacement> placements = BuildPlacements(rows);
        foreach (PdfTableCellPlacement placement in placements)
        {
            if (placement.ColumnSpan == 1)
            {
                double natural = placement.Cell.MeasureNaturalWidth(availableWidth, availableHeight);
                if (natural > naturalWidths[placement.ColumnIndex])
                {
                    naturalWidths[placement.ColumnIndex] = natural;
                }
            }
        }
    }

    internal void ExpandColumnsForSpans(List<PdfTableRow> rows, double availableWidth, double availableHeight)
    {
        List<PdfTableCellPlacement> placements = BuildPlacements(rows);
        foreach (PdfTableCellPlacement placement in placements)
        {
            if (placement.ColumnSpan > 1)
            {
                double natural = placement.Cell.MeasureNaturalWidth(availableWidth, availableHeight);
                double current = SumColumnWidths(placement.ColumnIndex, placement.ColumnSpan);
                if (natural > current)
                {
                    int endColumn = placement.ColumnIndex + placement.ColumnSpan;
                    int flexibleColumns = 0;
                    for (int columnIndex = placement.ColumnIndex; columnIndex < endColumn; columnIndex++)
                    {
                        if (ColumnDefinitions[columnIndex].Sizing != PdfTableColumnSizing.Fixed &&
                            ColumnDefinitions[columnIndex].Sizing != PdfTableColumnSizing.Maximum)
                        {
                            flexibleColumns++;
                        }
                    }

                    double extra = 0;
                    if (flexibleColumns > 0)
                    {
                        extra = (natural - current) / flexibleColumns;
                    }

                    for (int columnIndex = placement.ColumnIndex; columnIndex < endColumn; columnIndex++)
                    {
                        if (ColumnDefinitions[columnIndex].Sizing != PdfTableColumnSizing.Fixed &&
                            ColumnDefinitions[columnIndex].Sizing != PdfTableColumnSizing.Maximum)
                        {
                            ResolvedColumnWidths[columnIndex] += extra;
                        }
                    }
                }
            }
        }
    }

    internal List<PdfTableCellPlacement> BuildPlacements(List<PdfTableRow> rows)
    {
        bool[][] occupied = new bool[rows.Count][];
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            occupied[rowIndex] = new bool[ResolvedColumnWidths.Length];
        }

        List<PdfTableCellPlacement> placements = new List<PdfTableCellPlacement>();
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            PdfTableRow row = rows[rowIndex];
            ValidateRow(row);
            int columnIndex = 0;
            foreach (PdfTableCell cell in row.Cells)
            {
                while (columnIndex < ResolvedColumnWidths.Length && occupied[rowIndex][columnIndex])
                {
                    columnIndex++;
                }

                if (columnIndex >= ResolvedColumnWidths.Length || columnIndex + cell.ColumnSpan > ResolvedColumnWidths.Length)
                {
                    throw new ArgumentException("Table cell column span exceeds the available columns.");
                }

                if (rowIndex + cell.RowSpan > rows.Count)
                {
                    throw new ArgumentException("Table cell row span exceeds its header, body, or footer row group.");
                }

                int endRow = rowIndex + cell.RowSpan;
                int endColumn = columnIndex + cell.ColumnSpan;
                for (int occupiedRow = rowIndex; occupiedRow < endRow; occupiedRow++)
                {
                    for (int occupiedColumn = columnIndex; occupiedColumn < endColumn; occupiedColumn++)
                    {
                        if (occupied[occupiedRow][occupiedColumn])
                        {
                            throw new ArgumentException("Table cell spans overlap.");
                        }
                    }
                }

                for (int occupiedRow = rowIndex; occupiedRow < endRow; occupiedRow++)
                {
                    for (int occupiedColumn = columnIndex; occupiedColumn < endColumn; occupiedColumn++)
                    {
                        occupied[occupiedRow][occupiedColumn] = true;
                    }
                }

                placements.Add(new PdfTableCellPlacement(cell, rowIndex, columnIndex, cell.RowSpan, cell.ColumnSpan));
                columnIndex = endColumn;
            }
        }

        return placements;
    }

    internal PdfTableGroupLayout CreateGroupLayout(List<PdfTableRow> rows, double availableHeight)
    {
        PdfTableGroupLayout layout = new PdfTableGroupLayout(rows.Count);
        layout.Placements = BuildPlacements(rows);
        double cellAvailableHeight = availableHeight;
        if (cellAvailableHeight < 0)
        {
            cellAvailableHeight = 0;
        }

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            if (rows[rowIndex].HasExplicitHeight)
            {
                layout.RowHeights[rowIndex] = rows[rowIndex].ExplicitHeight;
            }
        }

        foreach (PdfTableCellPlacement placement in layout.Placements)
        {
            if (placement.RowSpan == 1)
            {
                double width = SumColumnWidths(placement.ColumnIndex, placement.ColumnSpan);
                PdfLayoutResult cellResult = placement.Cell.Measure(new PdfLayoutContext(width, cellAvailableHeight));
                if (!rows[placement.RowIndex].HasExplicitHeight && cellResult.Size.Height > layout.RowHeights[placement.RowIndex])
                {
                    layout.RowHeights[placement.RowIndex] = cellResult.Size.Height;
                }
            }
        }

        foreach (PdfTableCellPlacement placement in layout.Placements)
        {
            if (placement.RowSpan > 1)
            {
                double width = SumColumnWidths(placement.ColumnIndex, placement.ColumnSpan);
                PdfLayoutResult cellResult = placement.Cell.Measure(new PdfLayoutContext(width, cellAvailableHeight));
                double currentHeight = SumValues(layout.RowHeights, placement.RowIndex, placement.RowSpan);
                if (cellResult.Size.Height > currentHeight)
                {
                    int flexibleRows = 0;
                    int endRow = placement.RowIndex + placement.RowSpan;
                    for (int rowIndex = placement.RowIndex; rowIndex < endRow; rowIndex++)
                    {
                        if (!rows[rowIndex].HasExplicitHeight)
                        {
                            flexibleRows++;
                        }
                    }

                    if (flexibleRows > 0)
                    {
                        double extra = (cellResult.Size.Height - currentHeight) / flexibleRows;
                        for (int rowIndex = placement.RowIndex; rowIndex < endRow; rowIndex++)
                        {
                            if (!rows[rowIndex].HasExplicitHeight)
                            {
                                layout.RowHeights[rowIndex] += extra;
                            }
                        }
                    }
                }
            }
        }

        foreach (PdfTableCellPlacement placement in layout.Placements)
        {
            double width = SumColumnWidths(placement.ColumnIndex, placement.ColumnSpan);
            double height = SumValues(layout.RowHeights, placement.RowIndex, placement.RowSpan);
            PdfLayoutResult cellResult = placement.Cell.Measure(new PdfLayoutContext(width, height));
            if (!cellResult.Fits)
            {
                layout.Fits = false;
            }
        }

        return layout;
    }

    internal double DrawGroupRange(PdfPage page, PdfTableGroupLayout layout, double contentX, double currentTop, int startRowIndex, int rowCount)
    {
        int endRow = startRowIndex + rowCount;
        foreach (PdfTableCellPlacement placement in layout.Placements)
        {
            if (placement.RowIndex >= startRowIndex && placement.RowIndex < endRow)
            {
                int placementEnd = placement.RowIndex + placement.RowSpan;
                if (placementEnd > endRow)
                {
                    placementEnd = endRow;
                }

                double x = contentX + SumColumnWidths(0, placement.ColumnIndex);
                double width = SumColumnWidths(placement.ColumnIndex, placement.ColumnSpan);
                double topOffset = SumValues(layout.RowHeights, startRowIndex, placement.RowIndex - startRowIndex);
                double height = SumValues(layout.RowHeights, placement.RowIndex, placementEnd - placement.RowIndex);
                PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(page, x, currentTop - topOffset - height, width, height);
                placement.Cell.Draw(childContext);
            }
        }

        return currentTop - SumValues(layout.RowHeights, startRowIndex, rowCount);
    }

    internal bool RangeFits(PdfTableGroupLayout layout, int startRowIndex, int rowCount)
    {
        int endRow = startRowIndex + rowCount;
        foreach (PdfTableCellPlacement placement in layout.Placements)
        {
            int placementEnd = placement.RowIndex + placement.RowSpan;
            if (placement.RowIndex < endRow && placementEnd > startRowIndex)
            {
                if (placement.RowIndex < startRowIndex || placementEnd > endRow)
                {
                    if (RowSplitPolicy == PdfTableRowSplitPolicy.Prevent)
                    {
                        return false;
                    }

                    if (placement.RowIndex < startRowIndex)
                    {
                        continue;
                    }

                    placementEnd = endRow;
                }

                double width = SumColumnWidths(placement.ColumnIndex, placement.ColumnSpan);
                double height = SumValues(layout.RowHeights, placement.RowIndex, placementEnd - placement.RowIndex);
                if (!placement.Cell.Measure(new PdfLayoutContext(width, height)).Fits)
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal double SumColumnWidths(int startColumnIndex, int columnCount)
    {
        return SumValues(ResolvedColumnWidths, startColumnIndex, columnCount);
    }

    internal static double SumValues(double[] values, int startIndex, int count)
    {
        double sum = 0;
        int endIndex = startIndex + count;
        for (int index = startIndex; index < endIndex; index++)
        {
            sum += values[index];
        }

        return sum;
    }

    internal static double SumRowHeights(PdfTableGroupLayout layout, int startRowIndex, int rowCount)
    {
        return SumValues(layout.RowHeights, startRowIndex, rowCount);
    }

    internal double GetContentWidth()
    {
        double width = 0;
        for (int index = 0; index < ResolvedColumnWidths.Length; index++)
        {
            width += ResolvedColumnWidths[index];
        }

        return width;
    }

    internal double GetContentAvailableHeight(PdfLayoutContext context)
    {
        double height = context.AvailableHeight - Padding.GetVertical() - Border.GetVertical() - Margin.GetVertical();
        if (height < 0)
        {
            return 0;
        }

        return height;
    }

    internal void ValidateRow(PdfTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

    }

    internal void ValidateRowRange(int startRowIndex, int rowCount)
    {
        if (startRowIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startRowIndex", "Table row start cannot be negative.");
        }

        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException("rowCount", "Table row count cannot be negative.");
        }

        if (startRowIndex > _rows.Count)
        {
            throw new ArgumentOutOfRangeException("startRowIndex", "Table row start cannot exceed row count.");
        }

        if (rowCount > _rows.Count - startRowIndex)
        {
            throw new ArgumentOutOfRangeException("rowCount", "Table row count exceeds available rows.");
        }
    }

    internal void ValidateRowIndex(int rowIndex)
    {
        if (rowIndex < 0)
        {
            throw new ArgumentOutOfRangeException("rowIndex", "Table row index cannot be negative.");
        }

        if (rowIndex >= _rows.Count)
        {
            throw new ArgumentOutOfRangeException("rowIndex", "Table row index exceeds row count.");
        }
    }

    internal void Initialize(double[] columnWidths)
    {
        if (columnWidths == null)
        {
            throw new ArgumentNullException("columnWidths");
        }

        if (columnWidths.Length == 0)
        {
            throw new ArgumentException("Table must have at least one column.");
        }

        ColumnWidths = new double[columnWidths.Length];
        ColumnDefinitions = new PdfTableColumnDefinition[columnWidths.Length];
        ResolvedColumnWidths = new double[columnWidths.Length];
        for (int index = 0; index < columnWidths.Length; index++)
        {
            if (double.IsNaN(columnWidths[index]) || double.IsInfinity(columnWidths[index]) || columnWidths[index] <= 0)
            {
                throw new ArgumentOutOfRangeException("columnWidths", "Table column widths must be positive.");
            }

            ColumnWidths[index] = columnWidths[index];
            ColumnDefinitions[index] = new PdfTableColumnDefinition(PdfTableColumnSizing.Fixed, columnWidths[index]);
            ResolvedColumnWidths[index] = columnWidths[index];
        }

        UsesColumnDefinitions = false;
        RowSplitPolicy = PdfTableRowSplitPolicy.Prevent;

        Padding = PdfInsets.None();
        Border = PdfInsets.None();
        Margin = PdfInsets.None();
        Style = new PdfElementStyle();
        HasTableStyle = false;
        TableStyle = new PdfTableStyle();
        _headerRows = new List<PdfTableRow>();
        _rows = new List<PdfTableRow>();
        _footerRows = new List<PdfTableRow>();
        HeaderRows = _headerRows;
        Rows = _rows;
        FooterRows = _footerRows;
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal void PrepareTableStyles()
    {
        if (!HasTableStyle)
        {
            return;
        }

        ApplyCellStyle(_headerRows, TableStyle.HeaderCell);
        ApplyCellStyle(_rows, TableStyle.BodyCell);
        ApplyCellStyle(_footerRows, TableStyle.FooterCell);
    }

    internal static void ApplyCellStyle(List<PdfTableRow> rows, PdfElementStyle style)
    {
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            PdfTableRow row = rows[rowIndex];
            for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
            {
                row.Cells[cellIndex].SetStyle(style);
            }
        }
    }
}
