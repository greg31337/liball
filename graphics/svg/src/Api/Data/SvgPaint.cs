using System;

namespace svg;

/// <summary>Represents an SVG none, color, URL, or context paint value.</summary>
public class SvgPaint
{
    /// <summary>The paint value kind.</summary>
    public SvgPaintKind Kind;

    /// <summary>The solid color, or <c>null</c> for another kind.</summary>
    public SvgColor? Color;

    /// <summary>The decoded URL reference, or an empty string for another kind.</summary>
    public string Url;

    /// <summary>The ordered URL and terminal fallbacks for a URL paint.</summary>
    public System.Collections.Generic.List<SvgPaint> Fallbacks;

    /// <summary>The preserved authored text, or an empty string for a programmatic value.</summary>
    public string Source;

    /// <summary>Whether the mutable paint state is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a validated paint value and copies its fallback collection.</summary>
    /// <param name="kind">The paint kind.</param>
    /// <param name="color">The color for a color paint, or <c>null</c>.</param>
    /// <param name="url">The URL for a URL paint, or an empty string.</param>
    /// <param name="fallbacks">The flat URL fallback chain.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an inconsistent kind, payload, or fallback chain.</exception>
    public SvgPaint(SvgPaintKind kind, SvgColor? color, string url, System.Collections.Generic.List<SvgPaint> fallbacks)
    {
        if (!SvgPaintRules.Valid(kind, color, url, fallbacks, true))
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
        Kind = kind;
        Color = color;
        Url = url;
        Fallbacks = new System.Collections.Generic.List<SvgPaint>();
        int index = 0;
        while (index < fallbacks.Count)
        {
            Fallbacks.Add(fallbacks[index]);
            index = index + 1;
        }
        Source = "";
        IsValid = true;
    }

    /// <summary>Creates a no-paint value.</summary>
    /// <returns>A valid no-paint value.</returns>
    internal static SvgPaint NoPaint()
    {
        return new SvgPaint(SvgPaintKind.None, null, "", new System.Collections.Generic.List<SvgPaint>());
    }

    /// <summary>Creates a solid color paint.</summary>
    /// <param name="color">The valid color.</param>
    /// <returns>A valid solid paint.</returns>
    internal static SvgPaint Solid(SvgColor color)
    {
        return new SvgPaint(SvgPaintKind.Color, color, "", new System.Collections.Generic.List<SvgPaint>());
    }

    /// <summary>Creates a context fill or context stroke paint.</summary>
    /// <param name="kind">The context paint kind.</param>
    /// <returns>A valid context paint.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the kind is not a context paint.</exception>
    internal static SvgPaint Context(SvgPaintKind kind)
    {
        if (kind != SvgPaintKind.ContextFill && kind != SvgPaintKind.ContextStroke)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
        return new SvgPaint(kind, null, "", new System.Collections.Generic.List<SvgPaint>());
    }

    /// <summary>Creates a URL paint with a copied flat fallback chain.</summary>
    /// <param name="url">The nonempty decoded URL reference.</param>
    /// <param name="fallbacks">The ordered URL and terminal fallbacks.</param>
    /// <returns>A valid URL paint.</returns>
    internal static SvgPaint UrlReference(string url, System.Collections.Generic.List<SvgPaint> fallbacks)
    {
        return new SvgPaint(SvgPaintKind.Url, null, url, fallbacks);
    }

    /// <summary>Formats the paint and its fallbacks in canonical text.</summary>
    /// <returns>Canonical paint text, or an empty string for invalid mutable state.</returns>
    internal string ToText()
    {
        return SvgPaintFormatter.Format(this);
    }
}

/// <summary>Contains a strict, recovered, or failed SVG paint parse result.</summary>
public class SvgPaintParseResult
{
    /// <summary>Whether a complete or recovered paint value is available.</summary>
    public bool Success;

    /// <summary>Whether at least one malformed fallback was skipped.</summary>
    public bool Recovered;

    /// <summary>The first failure kind, or no failure after a strict success.</summary>
    public SvgPaintParseError Error;

    /// <summary>The parsed paint or valid URL prefix.</summary>
    public SvgPaint? Paint;

    /// <summary>The exact first failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates an SVG paint parse result.</summary>
    /// <param name="success">Whether a usable paint is available.</param>
    /// <param name="recovered">Whether malformed fallback syntax was skipped.</param>
    /// <param name="error">The first failure kind.</param>
    /// <param name="paint">The parsed paint or valid prefix.</param>
    /// <param name="errorOffset">The exact first failure offset.</param>
    public SvgPaintParseResult(bool success, bool recovered, SvgPaintParseError error, SvgPaint? paint, int errorOffset)
    {
        Success = success;
        Recovered = recovered;
        Error = error;
        Paint = paint;
        ErrorOffset = errorOffset;
    }
}
