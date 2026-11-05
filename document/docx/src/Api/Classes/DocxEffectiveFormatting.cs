namespace docx;

/// <summary>
/// Calculates direct, inherited, and effective formatting in document context.
/// </summary>
public class DocxEffectiveFormatting : IDocxEffectiveFormatting
{
    /// <summary>
    /// Creates an effective-formatting service.
    /// </summary>
    public DocxEffectiveFormatting()
    {
    }

    /// <summary>
    /// Calculates formatting for a run in its paragraph context.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="paragraph">The containing paragraph.</param>
    /// <param name="run">The run to inspect.</param>
    /// <returns>The separated run formatting.</returns>
    public DocxEffectiveRunFormatting GetRunFormatting(DocxDocument document, DocxParagraph paragraph, DocxRun run)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        DocxStyles styles = document.GetStyles();
        List<string> inheritedParts = new List<string>();

        if (styles.DocumentDefaults.HasRunFormat())
        {
            inheritedParts.Add(styles.DocumentDefaults.RunFormat.WriteXml());
        }

        List<DocxStyle> paragraphChain = GetStyleChain(styles, paragraph.GetFormat().StyleId, DocxStyle.TypeParagraph);

        foreach (DocxStyle style in paragraphChain)
        {
            inheritedParts.Add(style.RunFormat.WriteXml());
        }

        DocxRunFormat paragraphInherited = DocxRunFormat.ParseXml(DocxFormatMerger.Merge("rPr", inheritedParts));
        List<DocxStyle> characterChain = GetStyleChain(styles, run.GetFormat().StyleId, DocxStyle.TypeCharacter);

        foreach (DocxStyle style in characterChain)
        {
            inheritedParts.Add(style.RunFormat.WriteXml());
        }

        DocxEffectiveRunFormatting result = new DocxEffectiveRunFormatting();
        result.Direct = DocxRunFormat.ParseXml(run.GetFormat().WriteXml());
        result.Inherited = DocxRunFormat.ParseXml(DocxFormatMerger.Merge("rPr", inheritedParts));
        ApplyCharacterStyleToggles(result.Inherited, paragraphInherited, characterChain);

        List<string> effectiveParts = new List<string>();
        effectiveParts.Add(result.Inherited.WriteXml());
        effectiveParts.Add(result.Direct.WriteXml());
        result.Effective = DocxRunFormat.ParseXml(DocxFormatMerger.Merge("rPr", effectiveParts));

        DocxThemeResolver theme = CreateThemeResolver(document);
        theme.ApplyRun(result.Inherited);
        theme.ApplyRun(result.Effective);
        return result;
    }

    internal void ApplyCharacterStyleToggles(DocxRunFormat target, DocxRunFormat paragraphFormatting, List<DocxStyle> characterChain)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (paragraphFormatting == null)
        {
            throw new ArgumentNullException("paragraphFormatting");
        }

        if (characterChain == null)
        {
            throw new ArgumentNullException("characterChain");
        }

        target.HasBold = paragraphFormatting.HasBold;
        target.Bold = paragraphFormatting.Bold;
        target.HasItalic = paragraphFormatting.HasItalic;
        target.Italic = paragraphFormatting.Italic;
        target.HasStrike = paragraphFormatting.HasStrike;
        target.Strike = paragraphFormatting.Strike;
        target.HasCaps = paragraphFormatting.HasCaps;
        target.Caps = paragraphFormatting.Caps;
        target.HasSmallCaps = paragraphFormatting.HasSmallCaps;
        target.SmallCaps = paragraphFormatting.SmallCaps;
        target.HasVanish = paragraphFormatting.HasVanish;
        target.Vanish = paragraphFormatting.Vanish;
        target.HasEmboss = paragraphFormatting.HasEmboss;
        target.Emboss = paragraphFormatting.Emboss;
        target.HasImprint = paragraphFormatting.HasImprint;
        target.Imprint = paragraphFormatting.Imprint;
        target.HasOutline = paragraphFormatting.HasOutline;
        target.Outline = paragraphFormatting.Outline;
        target.HasShadow = paragraphFormatting.HasShadow;
        target.Shadow = paragraphFormatting.Shadow;
        target.HasComplexScriptBold = paragraphFormatting.HasComplexScriptBold;
        target.ComplexScriptBold = paragraphFormatting.ComplexScriptBold;
        target.HasComplexScriptItalic = paragraphFormatting.HasComplexScriptItalic;
        target.ComplexScriptItalic = paragraphFormatting.ComplexScriptItalic;

        foreach (DocxStyle style in characterChain)
        {
            DocxRunFormat format = style.RunFormat;

            if (format.HasBold)
            {
                target.HasBold = true;

                if (format.Bold)
                {
                    target.Bold = !target.Bold;
                }
            }

            if (format.HasItalic)
            {
                target.HasItalic = true;

                if (format.Italic)
                {
                    target.Italic = !target.Italic;
                }
            }

            if (format.HasStrike)
            {
                target.HasStrike = true;

                if (format.Strike)
                {
                    target.Strike = !target.Strike;
                }
            }

            if (format.HasCaps)
            {
                target.HasCaps = true;

                if (format.Caps)
                {
                    target.Caps = !target.Caps;
                }
            }

            if (format.HasSmallCaps)
            {
                target.HasSmallCaps = true;

                if (format.SmallCaps)
                {
                    target.SmallCaps = !target.SmallCaps;
                }
            }

            if (format.HasVanish)
            {
                target.HasVanish = true;

                if (format.Vanish)
                {
                    target.Vanish = !target.Vanish;
                }
            }

            if (format.HasEmboss)
            {
                target.HasEmboss = true;

                if (format.Emboss)
                {
                    target.Emboss = !target.Emboss;
                }
            }

            if (format.HasImprint)
            {
                target.HasImprint = true;

                if (format.Imprint)
                {
                    target.Imprint = !target.Imprint;
                }
            }

            if (format.HasOutline)
            {
                target.HasOutline = true;

                if (format.Outline)
                {
                    target.Outline = !target.Outline;
                }
            }

            if (format.HasShadow)
            {
                target.HasShadow = true;

                if (format.Shadow)
                {
                    target.Shadow = !target.Shadow;
                }
            }

            if (format.HasComplexScriptBold)
            {
                target.HasComplexScriptBold = true;

                if (format.ComplexScriptBold)
                {
                    target.ComplexScriptBold = !target.ComplexScriptBold;
                }
            }

            if (format.HasComplexScriptItalic)
            {
                target.HasComplexScriptItalic = true;

                if (format.ComplexScriptItalic)
                {
                    target.ComplexScriptItalic = !target.ComplexScriptItalic;
                }
            }
        }
    }

    /// <summary>
    /// Calculates formatting for a paragraph.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The separated paragraph formatting.</returns>
    public DocxEffectiveParagraphFormatting GetParagraphFormatting(DocxDocument document, DocxParagraph paragraph)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        DocxStyles styles = document.GetStyles();
        List<string> inheritedParts = new List<string>();

        if (styles.DocumentDefaults.HasParagraphFormat())
        {
            inheritedParts.Add(styles.DocumentDefaults.ParagraphFormat.WriteXml());
        }

        List<DocxStyle> styleChain = GetStyleChain(styles, paragraph.GetFormat().StyleId, DocxStyle.TypeParagraph);

        foreach (DocxStyle style in styleChain)
        {
            inheritedParts.Add(style.ParagraphFormat.WriteXml());
        }

        DocxEffectiveParagraphFormatting result = new DocxEffectiveParagraphFormatting();
        result.Direct = DocxParagraphFormat.ParseXml(paragraph.GetFormat().WriteXml());
        result.Inherited = DocxParagraphFormat.ParseXml(DocxFormatMerger.Merge("pPr", inheritedParts));

        List<string> effectiveParts = new List<string>();
        effectiveParts.Add(result.Inherited.WriteXml());
        effectiveParts.Add(result.Direct.WriteXml());
        result.Effective = DocxParagraphFormat.ParseXml(DocxFormatMerger.Merge("pPr", effectiveParts));

        DocxThemeResolver theme = CreateThemeResolver(document);
        theme.ApplyParagraph(result.Inherited);
        theme.ApplyParagraph(result.Effective);
        return result;
    }

    /// <summary>
    /// Calculates formatting for a table.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The separated table formatting.</returns>
    public DocxEffectiveTableFormatting GetTableFormatting(DocxDocument document, DocxTable table)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        DocxStyles styles = document.GetStyles();
        List<string> inheritedParts = new List<string>();
        List<DocxStyle> styleChain = GetStyleChain(styles, table.GetFormat().StyleId, DocxStyle.TypeTable);

        foreach (DocxStyle style in styleChain)
        {
            inheritedParts.Add(style.TableFormat.WriteXml());
        }

        DocxEffectiveTableFormatting result = new DocxEffectiveTableFormatting();
        result.Direct = DocxTableFormat.ParseXml(table.GetFormat().WriteXml());
        result.Inherited = DocxTableFormat.ParseXml(DocxFormatMerger.Merge("tblPr", inheritedParts));

        List<string> effectiveParts = new List<string>();
        effectiveParts.Add(result.Inherited.WriteXml());
        effectiveParts.Add(result.Direct.WriteXml());
        result.Effective = DocxTableFormat.ParseXml(DocxFormatMerger.Merge("tblPr", effectiveParts));

        DocxThemeResolver theme = CreateThemeResolver(document);
        theme.ApplyTable(result.Inherited);
        theme.ApplyTable(result.Effective);
        return result;
    }

    /// <summary>
    /// Calculates formatting for a row in its table context.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="table">The containing table.</param>
    /// <param name="row">The row to inspect.</param>
    /// <returns>The separated row formatting.</returns>
    public DocxEffectiveTableRowFormatting GetRowFormatting(DocxDocument document, DocxTable table, DocxTableRow row)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        DocxEffectiveTableRowFormatting result = new DocxEffectiveTableRowFormatting();
        result.Direct = CopyRowFormat(row);
        result.Inherited = new DocxTableRowFormat();

        List<DocxStyle> styleChain = GetStyleChain(document.GetStyles(), table.GetFormat().StyleId, DocxStyle.TypeTable);

        foreach (DocxStyle style in styleChain)
        {
            string rowXml = DocxFormatMerger.GetDirectChildXml(style.Xml, "trPr");

            if (rowXml != "")
            {
                OverlayRowFormat(result.Inherited, ParseRowFormat(rowXml));
            }
        }

        result.Effective = CopyRowFormatFromModel(result.Inherited);
        OverlayRowFormat(result.Effective, result.Direct);
        return result;
    }

    /// <summary>
    /// Calculates formatting for a cell in its table context.
    /// </summary>
    /// <param name="document">The containing document.</param>
    /// <param name="table">The containing table.</param>
    /// <param name="rowIndex">The zero-based row position.</param>
    /// <param name="cellIndex">The zero-based cell position.</param>
    /// <returns>The separated cell formatting.</returns>
    public DocxEffectiveTableCellFormatting GetCellFormatting(DocxDocument document, DocxTable table, int rowIndex, int cellIndex)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        DocxTableCell cell = table.GetRow(rowIndex).GetCell(cellIndex);
        DocxEffectiveTableCellFormatting result = new DocxEffectiveTableCellFormatting();
        result.Direct = cell.CreateFormat();
        result.Inherited = new DocxTableCellFormat();

        DocxEffectiveTableFormatting tableFormatting = GetTableFormatting(document, table);
        ApplyTableFormatToCell(result.Inherited, tableFormatting.Effective, table, cell);

        List<DocxStyle> styleChain = GetStyleChain(document.GetStyles(), table.GetFormat().StyleId, DocxStyle.TypeTable);

        foreach (DocxStyle style in styleChain)
        {
            string cellXml = DocxFormatMerger.GetDirectChildXml(style.Xml, "tcPr");

            if (cellXml != "")
            {
                OverlayCellFormat(result.Inherited, ParseCellFormat(cellXml));
            }
        }

        result.Effective = CopyCellFormat(result.Inherited);
        OverlayCellFormat(result.Effective, result.Direct);

        DocxThemeResolver theme = CreateThemeResolver(document);
        theme.ApplyCell(result.Inherited);
        theme.ApplyCell(result.Effective);
        return result;
    }

    internal List<DocxStyle> GetStyleChain(DocxStyles styles, string styleId, string expectedType)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        if (expectedType == null)
        {
            throw new ArgumentNullException("expectedType");
        }

        List<DocxStyle> chain = new List<DocxStyle>();
        HashSet<string> visited = new HashSet<string>();
        DocxStyle? current = styleId == "" ? FindDefaultStyle(styles, expectedType) : styles.FindById(styleId);

        while (current != null && current.Type == expectedType && !visited.Contains(current.StyleId))
        {
            chain.Insert(0, current);
            visited.Add(current.StyleId);

            if (current.BasedOn == "")
            {
                current = null;
            }
            else
            {
                current = styles.FindById(current.BasedOn);
            }
        }

        return chain;
    }

    internal DocxStyle? FindDefaultStyle(DocxStyles styles, string expectedType)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        if (expectedType == null)
        {
            throw new ArgumentNullException("expectedType");
        }

        foreach (DocxStyle style in styles.Styles)
        {
            if (style.Type == expectedType && style.HasDefault && style.Default)
            {
                return style;
            }
        }

        return null;
    }

    internal DocxThemeResolver CreateThemeResolver(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        DocxPart? themePart = document.GetThemePart();

        if (themePart == null)
        {
            return new DocxThemeResolver("");
        }

        return new DocxThemeResolver(themePart.GetText());
    }

    internal DocxTableRowFormat CopyRowFormat(DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        DocxTableRowFormat format = new DocxTableRowFormat();
        format.Height = row.Height;
        format.HeightRule = row.HeightRule;
        format.HasCantSplit = row.HasCantSplit;
        format.CantSplit = row.CantSplit;
        format.HasRepeatHeader = row.HasTableHeader;
        format.RepeatHeader = row.TableHeader;
        return format;
    }

    internal DocxTableRowFormat CopyRowFormatFromModel(DocxTableRowFormat source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        DocxTableRowFormat format = new DocxTableRowFormat();
        OverlayRowFormat(format, source);
        return format;
    }

    internal void OverlayRowFormat(DocxTableRowFormat target, DocxTableRowFormat source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (source.Height != "")
        {
            target.Height = source.Height;
        }

        if (source.HeightRule != "")
        {
            target.HeightRule = source.HeightRule;
        }

        if (source.HasCantSplit)
        {
            target.HasCantSplit = true;
            target.CantSplit = source.CantSplit;
        }

        if (source.HasRepeatHeader)
        {
            target.HasRepeatHeader = true;
            target.RepeatHeader = source.RepeatHeader;
        }
    }

    internal DocxTableRowFormat ParseRowFormat(string propertiesXml)
    {
        if (propertiesXml == null)
        {
            throw new ArgumentNullException("propertiesXml");
        }

        string xml = "<w:tr xmlns:w=\"" + DocxConstants.NamespaceWordprocessingMl + "\">" + propertiesXml + "</w:tr>";
        return CopyRowFormat(new DocxTableRow(xml));
    }

    internal DocxTableCellFormat ParseCellFormat(string propertiesXml)
    {
        if (propertiesXml == null)
        {
            throw new ArgumentNullException("propertiesXml");
        }

        string xml = "<w:tc xmlns:w=\"" + DocxConstants.NamespaceWordprocessingMl + "\">" + propertiesXml + "</w:tc>";
        return new DocxTableCell(xml).CreateFormat();
    }

    internal DocxTableCellFormat CopyCellFormat(DocxTableCellFormat source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        DocxTableCellFormat format = new DocxTableCellFormat();
        OverlayCellFormat(format, source);
        return format;
    }

    internal void OverlayCellFormat(DocxTableCellFormat target, DocxTableCellFormat source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        target.Width = SelectString(source.Width, target.Width);
        target.WidthType = SelectString(source.WidthType, target.WidthType);
        target.GridSpan = SelectString(source.GridSpan, target.GridSpan);
        target.HorizontalMerge = SelectString(source.HorizontalMerge, target.HorizontalMerge);
        target.VerticalMerge = SelectString(source.VerticalMerge, target.VerticalMerge);
        target.VerticalAlignment = SelectString(source.VerticalAlignment, target.VerticalAlignment);
        target.TextDirection = SelectString(source.TextDirection, target.TextDirection);
        target.MarginTopWidth = SelectString(source.MarginTopWidth, target.MarginTopWidth);
        target.MarginTopType = SelectString(source.MarginTopType, target.MarginTopType);
        target.MarginLeftWidth = SelectString(source.MarginLeftWidth, target.MarginLeftWidth);
        target.MarginLeftType = SelectString(source.MarginLeftType, target.MarginLeftType);
        target.MarginBottomWidth = SelectString(source.MarginBottomWidth, target.MarginBottomWidth);
        target.MarginBottomType = SelectString(source.MarginBottomType, target.MarginBottomType);
        target.MarginRightWidth = SelectString(source.MarginRightWidth, target.MarginRightWidth);
        target.MarginRightType = SelectString(source.MarginRightType, target.MarginRightType);
        OverlayBorders(target.Borders, source.Borders);
        OverlayShading(target.Shading, source.Shading);
    }

    internal void ApplyTableFormatToCell(DocxTableCellFormat target, DocxTableFormat tableFormat, DocxTable table, DocxTableCell cell)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (tableFormat == null)
        {
            throw new ArgumentNullException("tableFormat");
        }

        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        target.MarginTopWidth = tableFormat.CellMarginTopWidth;
        target.MarginTopType = tableFormat.CellMarginTopType;
        target.MarginLeftWidth = tableFormat.CellMarginLeftWidth;
        target.MarginLeftType = tableFormat.CellMarginLeftType;
        target.MarginBottomWidth = tableFormat.CellMarginBottomWidth;
        target.MarginBottomType = tableFormat.CellMarginBottomType;
        target.MarginRightWidth = tableFormat.CellMarginRightWidth;
        target.MarginRightType = tableFormat.CellMarginRightType;
        OverlayShading(target.Shading, tableFormat.Shading);

        int rowIndex = FindCellRowIndex(table, cell);
        int cellIndex = FindCellColumnIndex(table, cell);

        if (rowIndex >= 0 && cellIndex >= 0)
        {
            bool firstRow = rowIndex == 0;
            bool lastRow = rowIndex == table.Rows.Count - 1;
            bool firstCell = cellIndex == 0;
            bool lastCell = cellIndex == table.Rows[rowIndex].Cells.Count - 1;
            ApplyCellBorderTop(target.Borders, tableFormat.Borders, firstRow);
            ApplyCellBorderBottom(target.Borders, tableFormat.Borders, lastRow);
            ApplyCellBorderLeft(target.Borders, tableFormat.Borders, firstCell);
            ApplyCellBorderRight(target.Borders, tableFormat.Borders, lastCell);
        }
    }

    internal int FindCellRowIndex(DocxTable table, DocxTableCell cell)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        int rowIndex = 0;

        while (rowIndex < table.Rows.Count)
        {
            if (table.Rows[rowIndex].Cells.Contains(cell))
            {
                return rowIndex;
            }

            rowIndex = rowIndex + 1;
        }

        return -1;
    }

    internal int FindCellColumnIndex(DocxTable table, DocxTableCell cell)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        int rowIndex = FindCellRowIndex(table, cell);

        if (rowIndex < 0)
        {
            return -1;
        }

        return table.Rows[rowIndex].Cells.IndexOf(cell);
    }

    internal void ApplyCellBorderTop(DocxBorders target, DocxBorders source, bool outside)
    {
        target.TopVal = outside ? source.TopVal : source.InsideHorizontalVal;
        target.TopColor = outside ? source.TopColor : source.InsideHorizontalColor;
        target.TopSize = outside ? source.TopSize : source.InsideHorizontalSize;
        target.TopSpace = outside ? source.TopSpace : source.InsideHorizontalSpace;
    }

    internal void ApplyCellBorderBottom(DocxBorders target, DocxBorders source, bool outside)
    {
        target.BottomVal = outside ? source.BottomVal : source.InsideHorizontalVal;
        target.BottomColor = outside ? source.BottomColor : source.InsideHorizontalColor;
        target.BottomSize = outside ? source.BottomSize : source.InsideHorizontalSize;
        target.BottomSpace = outside ? source.BottomSpace : source.InsideHorizontalSpace;
    }

    internal void ApplyCellBorderLeft(DocxBorders target, DocxBorders source, bool outside)
    {
        target.LeftVal = outside ? source.LeftVal : source.InsideVerticalVal;
        target.LeftColor = outside ? source.LeftColor : source.InsideVerticalColor;
        target.LeftSize = outside ? source.LeftSize : source.InsideVerticalSize;
        target.LeftSpace = outside ? source.LeftSpace : source.InsideVerticalSpace;
    }

    internal void ApplyCellBorderRight(DocxBorders target, DocxBorders source, bool outside)
    {
        target.RightVal = outside ? source.RightVal : source.InsideVerticalVal;
        target.RightColor = outside ? source.RightColor : source.InsideVerticalColor;
        target.RightSize = outside ? source.RightSize : source.InsideVerticalSize;
        target.RightSpace = outside ? source.RightSpace : source.InsideVerticalSpace;
    }

    internal void OverlayBorders(DocxBorders target, DocxBorders source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        target.SingleVal = SelectString(source.SingleVal, target.SingleVal);
        target.SingleColor = SelectString(source.SingleColor, target.SingleColor);
        target.SingleSize = SelectString(source.SingleSize, target.SingleSize);
        target.SingleSpace = SelectString(source.SingleSpace, target.SingleSpace);
        target.TopVal = SelectString(source.TopVal, target.TopVal);
        target.TopColor = SelectString(source.TopColor, target.TopColor);
        target.TopSize = SelectString(source.TopSize, target.TopSize);
        target.TopSpace = SelectString(source.TopSpace, target.TopSpace);
        target.LeftVal = SelectString(source.LeftVal, target.LeftVal);
        target.LeftColor = SelectString(source.LeftColor, target.LeftColor);
        target.LeftSize = SelectString(source.LeftSize, target.LeftSize);
        target.LeftSpace = SelectString(source.LeftSpace, target.LeftSpace);
        target.BottomVal = SelectString(source.BottomVal, target.BottomVal);
        target.BottomColor = SelectString(source.BottomColor, target.BottomColor);
        target.BottomSize = SelectString(source.BottomSize, target.BottomSize);
        target.BottomSpace = SelectString(source.BottomSpace, target.BottomSpace);
        target.RightVal = SelectString(source.RightVal, target.RightVal);
        target.RightColor = SelectString(source.RightColor, target.RightColor);
        target.RightSize = SelectString(source.RightSize, target.RightSize);
        target.RightSpace = SelectString(source.RightSpace, target.RightSpace);
        target.InsideHorizontalVal = SelectString(source.InsideHorizontalVal, target.InsideHorizontalVal);
        target.InsideHorizontalColor = SelectString(source.InsideHorizontalColor, target.InsideHorizontalColor);
        target.InsideHorizontalSize = SelectString(source.InsideHorizontalSize, target.InsideHorizontalSize);
        target.InsideHorizontalSpace = SelectString(source.InsideHorizontalSpace, target.InsideHorizontalSpace);
        target.InsideVerticalVal = SelectString(source.InsideVerticalVal, target.InsideVerticalVal);
        target.InsideVerticalColor = SelectString(source.InsideVerticalColor, target.InsideVerticalColor);
        target.InsideVerticalSize = SelectString(source.InsideVerticalSize, target.InsideVerticalSize);
        target.InsideVerticalSpace = SelectString(source.InsideVerticalSpace, target.InsideVerticalSpace);
        target.BetweenVal = SelectString(source.BetweenVal, target.BetweenVal);
        target.BetweenColor = SelectString(source.BetweenColor, target.BetweenColor);
        target.BetweenSize = SelectString(source.BetweenSize, target.BetweenSize);
        target.BetweenSpace = SelectString(source.BetweenSpace, target.BetweenSpace);
        target.BarVal = SelectString(source.BarVal, target.BarVal);
        target.BarColor = SelectString(source.BarColor, target.BarColor);
        target.BarSize = SelectString(source.BarSize, target.BarSize);
        target.BarSpace = SelectString(source.BarSpace, target.BarSpace);
    }

    internal void OverlayShading(DocxShading target, DocxShading source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        target.Val = SelectString(source.Val, target.Val);

        if (source.Color != "")
        {
            target.Color = source.Color;

            if (source.ThemeColor == "")
            {
                target.ThemeColor = "";
                target.ThemeTint = "";
                target.ThemeShade = "";
            }
        }

        if (source.ThemeColor != "")
        {
            target.ThemeColor = source.ThemeColor;
            target.Color = source.Color;
            target.ThemeTint = source.ThemeTint;
            target.ThemeShade = source.ThemeShade;
        }
        else
        {
            target.ThemeTint = SelectString(source.ThemeTint, target.ThemeTint);
            target.ThemeShade = SelectString(source.ThemeShade, target.ThemeShade);
        }

        if (source.Fill != "")
        {
            target.Fill = source.Fill;

            if (source.ThemeFill == "")
            {
                target.ThemeFill = "";
                target.ThemeFillTint = "";
                target.ThemeFillShade = "";
            }
        }

        if (source.ThemeFill != "")
        {
            target.ThemeFill = source.ThemeFill;
            target.Fill = source.Fill;
            target.ThemeFillTint = source.ThemeFillTint;
            target.ThemeFillShade = source.ThemeFillShade;
        }
        else
        {
            target.ThemeFillTint = SelectString(source.ThemeFillTint, target.ThemeFillTint);
            target.ThemeFillShade = SelectString(source.ThemeFillShade, target.ThemeFillShade);
        }
    }

    internal string SelectString(string preferred, string fallback)
    {
        if (preferred == null)
        {
            throw new ArgumentNullException("preferred");
        }

        if (fallback == null)
        {
            throw new ArgumentNullException("fallback");
        }

        return preferred == "" ? fallback : preferred;
    }
}
