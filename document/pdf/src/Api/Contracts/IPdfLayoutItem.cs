using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfLayoutItem"/>.
/// </summary>
public interface IPdfLayoutItem
{
    /// <summary>
    /// Sets the keep together used for subsequent PDF generation.
    /// </summary>
    void SetKeepTogether();

    /// <summary>
    /// Clears the current keep together.
    /// </summary>
    void ClearKeepTogether();

    /// <summary>
    /// Flows this layout item through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawWithCursor(PdfPageCursor cursor);

    /// <summary>
    /// Flows this layout item without applying its keep-together option.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawWithoutKeepTogether(PdfPageCursor cursor);
}
