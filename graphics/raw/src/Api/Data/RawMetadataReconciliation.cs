using System;

namespace raw;

/// <summary>Contains one normalized metadata field together with its source location.</summary>
public class RawMetadataField
{
    /// <summary>The stable unified field name.</summary>
    public string Name;
    /// <summary>The normalized ordered values.</summary>
    public string[] Values;
    /// <summary>The language, or an empty string for an unqualified or default-language value.</summary>
    public string Language;
    /// <summary>The representation that supplied the values.</summary>
    public RawMetadataSource Source;
    /// <summary>The absolute source offset, or <c>-1</c> when no exact byte offset applies.</summary>
    public long Offset;
    /// <summary>The stable IFD or XML path, or an empty string.</summary>
    public string Path;
    /// <summary>The TIFF or IPTC tag number, or <c>-1</c> when no numeric tag applies.</summary>
    public int Tag;

    /// <summary>Creates a source-attributed unified metadata field.</summary>
    /// <param name="name">The stable field name.</param>
    /// <param name="values">The normalized ordered values.</param>
    /// <param name="language">The language, or an empty string.</param>
    /// <param name="source">The source representation.</param>
    /// <param name="offset">The absolute source offset, or <c>-1</c>.</param>
    /// <param name="path">The source path.</param>
    /// <param name="tag">The source tag number, or <c>-1</c>.</param>
    public RawMetadataField(string name, string[] values, string? language, RawMetadataSource source, long offset, string? path, int tag)
    {
        Name = name == null ? string.Empty : name;
        Values = Copy(values);
        Language = language == null || language == "x-default" ? string.Empty : language;
        Source = source;
        Offset = offset;
        Path = path == null ? string.Empty : path;
        Tag = tag;
    }

    /// <summary>Returns the first value, or an empty string when the field has no values.</summary>
    /// <returns>The first normalized value.</returns>
    internal string GetValue()
    {
        return Values == null || Values.Length == 0 ? string.Empty : Values[0];
    }

    internal static string[] Copy(string[]? values)
    {
        if (values == null)
        {
            return System.Array.Empty<string>();
        }
        string[] result = new string[values.Length];
        values.AsSpan().CopyTo(result);
        return result;
    }
}

/// <summary>Reports differing values found for one field across metadata representations.</summary>
public class RawMetadataConflict
{
    /// <summary>The stable unified field name.</summary>
    public string Name;
    /// <summary>The language, or an empty string.</summary>
    public string Language;
    /// <summary>The distinct source candidates in deterministic source order.</summary>
    public RawMetadataField[] Candidates;
    /// <summary>Whether an explicit precedence selected a candidate.</summary>
    public bool Resolved;
    /// <summary>The selected source, or <see cref="RawMetadataSource.None"/>.</summary>
    public RawMetadataSource SelectedSource;
    /// <summary>The selected normalized values, or an empty array when unresolved.</summary>
    public string[] SelectedValues;

    /// <summary>Creates a metadata conflict report.</summary>
    /// <param name="name">The stable field name.</param>
    /// <param name="language">The language, or an empty string.</param>
    /// <param name="candidates">The distinct source candidates.</param>
    /// <param name="resolved">Whether precedence resolved the conflict.</param>
    /// <param name="selectedSource">The selected source.</param>
    /// <param name="selectedValues">The selected normalized values.</param>
    public RawMetadataConflict(string name, string language, RawMetadataField[] candidates, bool resolved, RawMetadataSource selectedSource, string[]? selectedValues)
    {
        Name = name == null ? string.Empty : name;
        Language = language == null ? string.Empty : language;
        Candidates = candidates == null ? System.Array.Empty<RawMetadataField>() : candidates;
        Resolved = resolved;
        SelectedSource = selectedSource;
        SelectedValues = RawMetadataField.Copy(selectedValues);
    }
}
