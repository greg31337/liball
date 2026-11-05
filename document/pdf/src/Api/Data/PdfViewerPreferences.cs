namespace pdf;

/// <summary>
/// Stores viewer-window and full-screen presentation preferences for a PDF document.
/// </summary>
public class PdfViewerPreferences
{
    /// <summary>
    /// Indicates whether the viewer should hide its toolbars.
    /// </summary>
    public bool HideToolbar;
    /// <summary>
    /// Indicates whether the viewer should hide its menu bar.
    /// </summary>
    public bool HideMenubar;
    /// <summary>
    /// Indicates whether the viewer should hide window controls such as scroll bars.
    /// </summary>
    public bool HideWindowUi;
    /// <summary>
    /// Indicates whether the viewer should resize its window to the first displayed page.
    /// </summary>
    public bool FitWindow;
    /// <summary>
    /// Indicates whether the viewer should center its window on the screen.
    /// </summary>
    public bool CenterWindow;
    /// <summary>
    /// Indicates whether the title bar should use the document metadata title instead of the file name.
    /// </summary>
    public bool DisplayDocumentTitle;
    /// <summary>
    /// Specifies the panel shown after leaving full-screen mode.
    /// </summary>
    public PdfPageMode NonFullScreenPageMode;
    /// <summary>
    /// Specifies the predominant reading direction for side-by-side pages.
    /// </summary>
    public PdfReadingDirection ReadingDirection;

    /// <summary>
    /// Initializes viewer preferences with the PDF defaults.
    /// </summary>
    public PdfViewerPreferences()
    {
        NonFullScreenPageMode = PdfPageMode.UseNone;
        ReadingDirection = PdfReadingDirection.LeftToRight;
    }

    internal PdfViewerPreferences Copy()
    {
        PdfViewerPreferences copied = new PdfViewerPreferences();
        copied.HideToolbar = HideToolbar;
        copied.HideMenubar = HideMenubar;
        copied.HideWindowUi = HideWindowUi;
        copied.FitWindow = FitWindow;
        copied.CenterWindow = CenterWindow;
        copied.DisplayDocumentTitle = DisplayDocumentTitle;
        copied.NonFullScreenPageMode = NonFullScreenPageMode;
        copied.ReadingDirection = ReadingDirection;
        return copied;
    }
}
