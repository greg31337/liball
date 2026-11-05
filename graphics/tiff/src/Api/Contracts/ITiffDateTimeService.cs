using System;

namespace tiff;

/// <summary>Defines parsing and formatting operations for TIFF and EXIF date/time text.</summary>
public interface ITiffDateTimeService
{
    /// <summary>Parses a TIFF date/time value in <c>YYYY:MM:DD HH:MM:SS</c> form.</summary>
    /// <param name="text">The TIFF date/time text.</param>
    /// <returns>The parsed value and diagnostics.</returns>
    TiffDateTimeParseResult ParseTiffDateTime(string text);
    /// <summary>Formats a date/time value in TIFF <c>YYYY:MM:DD HH:MM:SS</c> form.</summary>
    /// <param name="value">The date/time value.</param>
    /// <returns>The invariant TIFF date/time text.</returns>
    string FormatTiffDateTime(DateTime value);
}
