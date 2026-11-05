using System;

namespace tiff;

/// <summary>Parses and formats the invariant date/time representation used by TIFF and EXIF metadata.</summary>
public class TiffDateTimeService : ITiffDateTimeService
{
    /// <summary>Creates a TIFF date/time service.</summary>
    public TiffDateTimeService()
    {
    }

    /// <summary>Parses a TIFF date/time value in <c>YYYY:MM:DD HH:MM:SS</c> form.</summary>
    /// <param name="text">The TIFF date/time text.</param>
    /// <returns>The parsed value and diagnostics.</returns>
    public TiffDateTimeParseResult ParseTiffDateTime(string text)
    {
        return TiffDateTimeCodec.Parse(text);
    }

    /// <summary>Formats a date/time value in TIFF <c>YYYY:MM:DD HH:MM:SS</c> form.</summary>
    /// <param name="value">The date/time value.</param>
    /// <returns>The invariant TIFF date/time text.</returns>
    public string FormatTiffDateTime(DateTime value)
    {
        return TiffDateTimeCodec.Format(value);
    }
}
