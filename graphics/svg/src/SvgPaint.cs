using System;

namespace svg;

/// <summary>Validates mutable and programmatic SVG paint values.</summary>
internal static class SvgPaintRules
{
    /// <summary>Determines whether a paint payload and flat fallback chain are consistent.</summary>
    /// <param name="kind">The paint kind.</param>
    /// <param name="color">The optional color.</param>
    /// <param name="url">The optional URL.</param>
    /// <param name="fallbacks">The fallback collection.</param>
    /// <param name="requireValidFlag">Whether nested paint validity flags are required.</param>
    /// <returns><c>true</c> for a consistent paint value.</returns>
    public static bool Valid(SvgPaintKind kind, SvgColor? color, string? url, System.Collections.Generic.List<SvgPaint>? fallbacks, bool requireValidFlag)
    {
        if (fallbacks == null || url == null || !KnownKind(kind)) return false;
        if (kind == SvgPaintKind.Color)
        {
            if (!ValidColor(color) || url.Length != 0 || fallbacks.Count != 0) return false;
        }
        else if (kind == SvgPaintKind.Url)
        {
            if (color != null || !ValidUrl(url)) return false;
        }
        else if (color != null || url.Length != 0 || fallbacks.Count != 0)
        {
            return false;
        }
        bool terminal = false;
        int index = 0;
        while (index < fallbacks.Count)
        {
            SvgPaint fallback = fallbacks[index];
            if (kind != SvgPaintKind.Url || fallback == null || requireValidFlag && !fallback.IsValid || fallback.Fallbacks == null || fallback.Fallbacks.Count != 0 || fallback.Kind == SvgPaintKind.ContextFill || fallback.Kind == SvgPaintKind.ContextStroke || terminal)
            {
                return false;
            }
            if (!Valid(fallback.Kind, fallback.Color, fallback.Url, fallback.Fallbacks, requireValidFlag)) return false;
            terminal = fallback.Kind != SvgPaintKind.Url;
            index = index + 1;
        }
        return true;
    }

    static bool KnownKind(SvgPaintKind kind)
    {
        return kind == SvgPaintKind.None || kind == SvgPaintKind.Color || kind == SvgPaintKind.Url || kind == SvgPaintKind.ContextFill || kind == SvgPaintKind.ContextStroke;
    }

    static bool ValidColor(SvgColor? color)
    {
        return color != null && color.IsValid && SvgColorMath.IsKnownSpace(color.Space) && SvgFiniteMath.IsFinite(color.Red) && SvgFiniteMath.IsFinite(color.Green) && SvgFiniteMath.IsFinite(color.Blue) && SvgFiniteMath.IsFinite(color.Alpha) && color.Alpha >= 0 && color.Alpha <= 1;
    }

    static bool ValidUrl(string value)
    {
        if (value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\0' || current == '\n' || current == '\r' || current == '\f') return false;
            index = index + 1;
        }
        return true;
    }
}

/// <summary>Formats valid SVG paint values.</summary>
internal static class SvgPaintFormatter
{
    /// <summary>Formats a paint and its flat fallback chain.</summary>
    /// <param name="paint">The paint to format.</param>
    /// <returns>Canonical text, or an empty string for invalid state.</returns>
    public static string Format(SvgPaint paint)
    {
        if (paint == null || !paint.IsValid || !SvgPaintRules.Valid(paint.Kind, paint.Color, paint.Url, paint.Fallbacks, true)) return "";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        Append(builder, paint);
        int index = 0;
        while (index < paint.Fallbacks.Count)
        {
            builder.Append(' ');
            Append(builder, paint.Fallbacks[index]);
            index = index + 1;
        }
        return builder.ToString();
    }

    static void Append(System.Text.StringBuilder builder, SvgPaint paint)
    {
        if (paint.Kind == SvgPaintKind.None) builder.Append("none");
        else if (paint.Kind == SvgPaintKind.Color) builder.Append((paint.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText());
        else if (paint.Kind == SvgPaintKind.ContextFill) builder.Append("context-fill");
        else if (paint.Kind == SvgPaintKind.ContextStroke) builder.Append("context-stroke");
        else
        {
            builder.Append("url(\"");
            int index = 0;
            while (index < paint.Url.Length)
            {
                char current = paint.Url[index];
                if (current == '\\' || current == '"') builder.Append('\\');
                builder.Append(current);
                index = index + 1;
            }
            builder.Append("\")");
        }
    }
}

/// <summary>Contains one internal paint atom parse result.</summary>
internal class SvgPaintAtomResult
{
    /// <summary>Whether the atom parsed.</summary>
    public bool Success;
    /// <summary>The atom failure kind.</summary>
    public SvgPaintParseError Error;
    /// <summary>The exact atom failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The parsed atom paint.</summary>
    public SvgPaint? Paint;

    /// <summary>Creates a paint atom result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="paint">The parsed paint.</param>
    public SvgPaintAtomResult(bool success, SvgPaintParseError error, int errorOffset, SvgPaint? paint)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        Paint = paint;
    }
}

/// <summary>Contains one decoded paint keyword result.</summary>
internal class SvgPaintNameResult
{
    /// <summary>Whether decoding succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgPaintParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The decoded name.</summary>
    public string Name;

    /// <summary>Creates a decoded name result.</summary>
    /// <param name="success">Whether decoding succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="name">The decoded name.</param>
    public SvgPaintNameResult(bool success, SvgPaintParseError error, int errorOffset, string name)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        Name = name;
    }
}

/// <summary>Contains one decoded URL escape result.</summary>
internal class SvgPaintEscapeResult
{
    /// <summary>Whether decoding succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgPaintParseError Error;
    /// <summary>The exact failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The exclusive ending offset.</summary>
    public int End;
    /// <summary>The decoded escape text.</summary>
    public string Value;

    /// <summary>Creates a URL escape result.</summary>
    /// <param name="success">Whether decoding succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="end">The exclusive ending offset.</param>
    /// <param name="value">The decoded text.</param>
    public SvgPaintEscapeResult(bool success, SvgPaintParseError error, int errorOffset, int end, string value)
    {
        Success = success;
        Error = error;
        ErrorOffset = errorOffset;
        End = end;
        Value = value;
    }
}
