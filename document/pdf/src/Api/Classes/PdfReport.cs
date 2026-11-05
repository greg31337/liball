using System;

namespace pdf;

/// <summary>
/// Builds a paginated business report from titles, sections, key metrics, tables, and figures.
/// </summary>
public class PdfReport : IPdfReport
{
    /// <summary>
    /// Stores the title used by this object.
    /// </summary>
    internal string Title = string.Empty;
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();
    /// <summary>
    /// Stores the layout used by this object.
    /// </summary>
    internal PdfLayoutDocument Layout = PdfLayoutDocument.Default(new PdfDocument());
    /// <summary>
    /// Stores the page size used by this object.
    /// </summary>
    internal PdfPageDimensions PageSize = new PdfPageSizeFactory().Letter();
    /// <summary>
    /// Stores the margins used by this object.
    /// </summary>
    internal PdfPageMargins Margins = new PdfPageMargins(64, 64, 64, 54);
    /// <summary>
    /// Specifies the content width in PDF points.
    /// </summary>
    internal double ContentWidth;
    /// <summary>
    /// Stores the title style used by this object.
    /// </summary>
    internal PdfParagraphStyle TitleStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the subtitle style used by this object.
    /// </summary>
    internal PdfParagraphStyle SubtitleStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the heading style used by this object.
    /// </summary>
    internal PdfParagraphStyle HeadingStyle = new PdfParagraphStyle();
    internal PdfParagraphStyle SubheadingStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the body style used by this object.
    /// </summary>
    internal PdfParagraphStyle BodyStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the kpi label style used by this object.
    /// </summary>
    internal PdfParagraphStyle KpiLabelStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the kpi value style used by this object.
    /// </summary>
    internal PdfParagraphStyle KpiValueStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the table header style used by this object.
    /// </summary>
    internal PdfParagraphStyle TableHeaderStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the table cell style used by this object.
    /// </summary>
    internal PdfParagraphStyle TableCellStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the caption style used by this object.
    /// </summary>
    internal PdfParagraphStyle CaptionStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the header style used by this object.
    /// </summary>
    internal PdfParagraphStyle HeaderStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the footer style used by this object.
    /// </summary>
    internal PdfParagraphStyle FooterStyle = new PdfParagraphStyle();
    internal bool Built;
    internal bool HasContent;
    internal bool PendingCoverPageBreak;

    /// <summary>
    /// Initializes a new report.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfReport(string title)
    {
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        Title = title;
        Document = new PdfDocument();
        PageSize = new PdfPageSizeFactory().Letter();
        Margins = new PdfPageMargins(64, 64, 64, 54);
        ContentWidth = PageSize.Width - Margins.Left - Margins.Right;
        Layout = new PdfLayoutDocument(Document, PageSize, Margins);
        InitializeStyles();
        ApplyPageFurniture();
        Built = false;
        HasContent = false;
        PendingCoverPageBreak = false;
    }

    /// <summary>Returns the underlying document for resource and metadata configuration.</summary>
    /// <returns>The report document.</returns>
    public PdfDocument GetDocument()
    {
        return Document;
    }

    /// <summary>Adds a dedicated first-page report cover.</summary>
    /// <param name="title">The cover title.</param>
    /// <param name="subtitle">The cover subtitle.</param>
    /// <param name="author">The author or organization line.</param>
    /// <param name="reportingPeriod">The reporting-period line.</param>
    public void AddCoverPage(string title, string subtitle, string author, string reportingPeriod)
    {
        EnsureNotBuilt();
        ValidateRequiredText(title, "title", "Report cover title cannot be empty.");
        ValidateText(subtitle, "subtitle");
        ValidateText(author, "author");
        ValidateText(reportingPeriod, "reportingPeriod");
        if (HasContent)
        {
            throw new InvalidOperationException("A report cover page must be added before other report content.");
        }

        Layout.SetFirstPageHeaderTemplate(CreateEmptyPageTemplate());
        Layout.SetFirstPageFooterTemplate(CreateEmptyPageTemplate());
        Layout.AddBlock(PdfBlock.WithSize(0, 170));
        PdfStackBlock cover = PdfStackBlock.WithSpacing(10);
        cover.AddTextBlock(new PdfTextBlock(title, TitleStyle));
        if (subtitle.Length > 0)
        {
            cover.AddTextBlock(new PdfTextBlock(subtitle, SubtitleStyle));
        }

        if (author.Length > 0)
        {
            cover.AddTextBlock(new PdfTextBlock(author, BodyStyle));
        }

        if (reportingPeriod.Length > 0)
        {
            cover.AddTextBlock(new PdfTextBlock(reportingPeriod, BodyStyle));
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(cover));
        HasContent = true;
        PendingCoverPageBreak = true;
    }

    /// <summary>
    /// Adds a title to this report.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <param name="subtitle">The subtitle to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddTitle(string title, string subtitle)
    {
        EnsureNotBuilt();
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        if (subtitle == null)
        {
            throw new ArgumentNullException("subtitle");
        }

        PrepareForContent();

        PdfStackBlock stack = PdfStackBlock.WithSpacing(6);
        stack.AddTextBlock(new PdfTextBlock(title, TitleStyle));
        stack.AddTextBlock(new PdfTextBlock(subtitle, SubtitleStyle));
        Layout.AddItem(PdfLayoutItem.FromStackBlock(stack));
        Layout.AddBlock(PdfBlock.WithSize(0, 18));
    }

    /// <summary>
    /// Adds a section to this report.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    public void AddSection(string heading, string text)
    {
        EnsureNotBuilt();
        PdfReportSection section = new PdfReportSection(heading, text);
        PrepareForContent();
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(section.CreateBlock(HeadingStyle, BodyStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>
    /// Adds a section to this report.
    /// </summary>
    /// <param name="section">The section to add to the report.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddReportSection(PdfReportSection section)
    {
        EnsureNotBuilt();
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        PrepareForContent();

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(section.CreateBlock(HeadingStyle, BodyStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>Adds a styled executive-summary panel.</summary>
    /// <param name="text">The executive-summary text.</param>
    public void AddExecutiveSummary(string text)
    {
        EnsureNotBuilt();
        ValidateText(text, "text");
        PrepareForContent();
        PdfStackBlock stack = PdfStackBlock.WithSpacing(6);
        stack.AddTextBlock(new PdfTextBlock("Executive Summary", HeadingStyle));
        stack.AddTextBlock(new PdfTextBlock(text, BodyStyle));
        PdfCallout callout = new PdfCallout(stack);
        PdfElementStyle style = new PdfElementStyle();
        style.Padding = PdfInsets.Uniform(12);
        style.Border = new PdfInsets(3, 0, 0, 0);
        style.HasBackground = true;
        style.BackgroundColor = PdfColor.Gray(0.95);
        style.BorderColor = PdfColor.Gray(0.45);
        callout.SetStyle(style);
        callout.SetKeepTogether();
        callout.SetAccessibilityDescription("Executive Summary");
        Layout.AddCallout(callout);
        Layout.AddBlock(PdfBlock.WithSize(0, 14));
    }

    /// <summary>Adds a subsection heading and body.</summary>
    /// <param name="heading">The subsection heading.</param>
    /// <param name="text">The subsection body.</param>
    public void AddSubsection(string heading, string text)
    {
        EnsureNotBuilt();
        PdfReportSection subsection = new PdfReportSection(heading, text);
        PrepareForContent();
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(subsection.CreateBlock(SubheadingStyle, BodyStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    /// <summary>
    /// Adds a paragraph to this report.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddParagraph(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        PrepareForContent();

        Layout.AddTextBlock(new PdfTextBlock(text, BodyStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 8));
    }

    /// <summary>
    /// Adds a KPI to this report.
    /// </summary>
    /// <param name="label">The label to use for this operation.</param>
    /// <param name="value">The value to use for this operation.</param>
    public void AddKpi(string label, string value)
    {
        EnsureNotBuilt();
        PdfKpi kpi = new PdfKpi(label, value);
        PrepareForContent();
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(kpi.CreateBlock(ContentWidth, KpiLabelStyle, KpiValueStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    /// <summary>
    /// Adds a KPI to this report.
    /// </summary>
    /// <param name="kpi">The KPI to add to the report.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddReportKpi(PdfKpi kpi)
    {
        EnsureNotBuilt();
        if (kpi == null)
        {
            throw new ArgumentNullException("kpi");
        }

        PrepareForContent();

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTableBlock(kpi.CreateBlock(ContentWidth, KpiLabelStyle, KpiValueStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    /// <summary>Adds KPI cards in a multi-column layout.</summary>
    /// <param name="kpis">The KPI cards.</param>
    /// <param name="columnCount">The positive column count.</param>
    public void AddKpiLayout(PdfKpi[] kpis, int columnCount)
    {
        EnsureNotBuilt();
        if (kpis == null)
        {
            throw new ArgumentNullException("kpis");
        }

        if (kpis.Length == 0)
        {
            throw new ArgumentException("A KPI layout requires at least one KPI.", "kpis");
        }

        if (columnCount <= 0 || columnCount > kpis.Length)
        {
            throw new ArgumentOutOfRangeException("columnCount", "KPI layout column count must be positive and cannot exceed the KPI count.");
        }

        for (int index = 0; index < kpis.Length; index++)
        {
            if (kpis[index] == null)
            {
                throw new ArgumentNullException("kpis");
            }
        }

        PrepareForContent();
        const double gap = 10;
        double cardWidth = (ContentWidth - gap * (columnCount - 1)) / columnCount;
        PdfGridBlock grid = PdfGridBlock.Create(columnCount, gap, gap);
        foreach (PdfKpi kpi in kpis)
        {
            grid.AddTableBlock(kpi.CreateBlock(cardWidth, KpiLabelStyle, KpiValueStyle));
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromGridBlock(grid));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>
    /// Adds a table to this report.
    /// </summary>
    /// <param name="table">The table to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddTable(PdfReportTable table)
    {
        EnsureNotBuilt();
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        PrepareForContent();

        Layout.AddTableBlock(table.CreateBlock(ContentWidth, TableHeaderStyle, TableCellStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>
    /// Adds a figure to this report.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="caption">The caption to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddFigure(PdfImage image, string caption)
    {
        EnsureNotBuilt();
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (caption == null)
        {
            throw new ArgumentNullException("caption");
        }

        PrepareForContent();

        double imageWidth = image.Width;
        double imageHeight = image.Height;
        if (imageWidth > ContentWidth)
        {
            double scale = ContentWidth / imageWidth;
            imageWidth = ContentWidth;
            imageHeight = imageHeight * scale;
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromImageBlock(new PdfImageBlock(image, imageWidth, imageHeight)));
        Layout.AddTextBlock(new PdfTextBlock(caption, CaptionStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>Adds a chart placeholder with a semantic caption.</summary>
    /// <param name="label">The chart label.</param>
    /// <param name="caption">The chart caption.</param>
    /// <param name="height">The positive chart height.</param>
    public void AddChartFigure(string label, string caption, double height)
    {
        EnsureNotBuilt();
        ValidateRequiredText(label, "label", "Chart label cannot be empty.");
        ValidateText(caption, "caption");
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Chart height must be positive and finite.");
        }

        PrepareForContent();
        PdfChartPlaceholder chart = new PdfChartPlaceholder(label, ContentWidth, height);
        chart.SetTextStyle(CaptionStyle.TextStyle);
        PdfCaption chartCaption = new PdfCaption(new PdfTextBlock(caption, CaptionStyle));
        PdfFigure figure = new PdfFigure(chart, chartCaption);
        figure.SetCaptionSpacing(5);
        figure.SetAccessibilityDescription(caption);
        figure.SetKeepTogether();
        Layout.AddFigure(figure);
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>Starts and adds an appendix section.</summary>
    /// <param name="heading">The appendix heading.</param>
    /// <param name="text">The appendix body.</param>
    public void AddAppendix(string heading, string text)
    {
        EnsureNotBuilt();
        ValidateRequiredText(heading, "heading", "Appendix heading cannot be empty.");
        ValidateText(text, "text");
        PdfReportSection appendix = new PdfReportSection("Appendix: " + heading, text);
        PrepareForNewPageContent();
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromStackBlock(appendix.CreateBlock(HeadingStyle, BodyStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 12));
    }

    /// <summary>Configures the report header and footer text.</summary>
    /// <param name="reportName">The running report name.</param>
    /// <param name="footerText">The footer prefix placed before page numbering.</param>
    public void SetReportPageFurniture(string reportName, string footerText)
    {
        EnsureNotBuilt();
        ValidateRequiredText(reportName, "reportName", "Report furniture name cannot be empty.");
        ValidateText(footerText, "footerText");
        Layout.SetHeaderTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock(reportName, HeaderStyle), 12));
        string pageText = "Page {page} of {total}";
        if (footerText.Length > 0)
        {
            pageText = footerText + " | " + pageText;
        }

        Layout.SetFooterTemplate(PdfPageTemplate.FromPageText(pageText, FooterStyle, 12));
        if (PendingCoverPageBreak)
        {
            Layout.SetFirstPageHeaderTemplate(CreateEmptyPageTemplate());
            Layout.SetFirstPageFooterTemplate(CreateEmptyPageTemplate());
        }
    }

    /// <summary>
    /// Builds a report from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            Layout.Render();
            Built = true;
        }

        return Document;
    }

    internal void InitializeStyles()
    {
        TitleStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(26, PdfFont.Bold), 31, 0, 0, 0);
        SubtitleStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(11, PdfFont.Regular, PdfColor.Gray(0.28)), 15, 0, 0, 0);
        HeadingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(15, PdfFont.Bold), 19, 0, 0, 0);
        SubheadingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(12, PdfFont.Bold), 16, 0, 0, 0);
        BodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
        KpiLabelStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(9, PdfFont.Regular, PdfColor.Gray(0.32)), 11, 0, 0, 0);
        KpiValueStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(18, PdfFont.Bold), 22, 0, 0, 0);
        TableHeaderStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(9, PdfFont.Bold), 12, 0, 0, 0);
        TableCellStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(9, PdfFont.Regular), 12, 0, 0, 0);
        CaptionStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(8, PdfFont.Regular, PdfColor.Gray(0.34)), 11, 0, 0, 0);
        HeaderStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(8, PdfFont.Bold, PdfColor.Gray(0.26)), 10, 0, 0, 0);
        FooterStyle = PdfParagraphStyle.Create(PdfTextStyle.WithColor(8, PdfFont.Regular, PdfColor.Gray(0.35)), 10, 0, 0, 0, PdfTextAlignment.Right);
    }

    internal void ApplyPageFurniture()
    {
        Layout.SetHeaderTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock(Title, HeaderStyle), 12));
        Layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", FooterStyle, 12));
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Report has already been built.");
        }
    }

    internal void PrepareForContent()
    {
        if (PendingCoverPageBreak)
        {
            Layout.AddPageBreak();
            PendingCoverPageBreak = false;
        }

        HasContent = true;
    }

    internal void PrepareForNewPageContent()
    {
        if (PendingCoverPageBreak)
        {
            Layout.AddPageBreak();
            PendingCoverPageBreak = false;
        }
        else if (HasContent)
        {
            Layout.AddPageBreak();
        }

        HasContent = true;
    }

    internal static PdfPageTemplate CreateEmptyPageTemplate()
    {
        return new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    }

    internal static void ValidateText(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    internal static void ValidateRequiredText(string value, string parameterName, string message)
    {
        ValidateText(value, parameterName);
        if (value.Length == 0)
        {
            throw new ArgumentException(message, parameterName);
        }
    }
}
