using System;

namespace html;
/// <summary>Configures deterministic serialization, escaping, formatting, source preservation, and resource limits.</summary>
public class HtmlSerializerOptions : IHtmlSerializerOptions, IHtmlSerializerConfiguration
{
    internal HtmlSerializerNameMode nameMode;
    internal HtmlSerializerQuoteMode quoteMode;
    internal bool asciiOnly;
    internal bool sortAttributes;
    internal bool selfCloseForeignElements;
    internal bool pretty;
    internal bool omitOptionalTags;
    internal string indentation;
    internal string newLine;
    internal HtmlLimits limits;
    /// <summary>Creates serializer options initialized for canonical output.</summary>
    public HtmlSerializerOptions()
    {
        this.nameMode = HtmlSerializerNameMode.Canonical;
        this.quoteMode = HtmlSerializerQuoteMode.Double;
        this.asciiOnly = false;
        this.sortAttributes = false;
        this.selfCloseForeignElements = false;
        this.pretty = false;
        this.omitOptionalTags = false;
        this.indentation = "  ";
        this.newLine = "\n";
        this.limits = HtmlLimits.CreateDefault();
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <returns>A new independently mutable serializer-options object initialized with the library defaults.</returns>
    internal static HtmlSerializerOptions CreateDefault()
    {
        return new HtmlSerializerOptions();
    }

    /// <summary>Creates deterministic canonical serialization settings.</summary>
    /// <returns>A new serializer-options object for stable canonical output.</returns>
    internal static HtmlSerializerOptions CreateCanonical()
    {
        return new HtmlSerializerOptions();
    }

    /// <summary>Creates settings that preserve source-oriented spelling choices where possible.</summary>
    /// <returns>A new serializer-options object that preserves stored name casing and attribute order where possible.</returns>
    internal static HtmlSerializerOptions CreatePreserve()
    {
        HtmlSerializerOptions options = new HtmlSerializerOptions();
        options.nameMode = HtmlSerializerNameMode.Preserve;
        return options;
    }

    /// <summary>Creates settings for compact serialization without pretty printing.</summary>
    /// <returns>A new serializer-options object that emits compact output.</returns>
    internal static HtmlSerializerOptions CreateCompact()
    {
        HtmlSerializerOptions options = new HtmlSerializerOptions();
        options.omitOptionalTags = true;
        return options;
    }

    /// <summary>Creates settings for indented, human-readable serialization.</summary>
    /// <returns>A new serializer-options object that adds indentation and line breaks around block content.</returns>
    internal static HtmlSerializerOptions CreatePretty()
    {
        HtmlSerializerOptions options = new HtmlSerializerOptions();
        options.pretty = true;
        return options;
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent serializer-options object containing the same settings.</returns>
    public HtmlSerializerOptions Copy()
    {
        HtmlSerializerOptions copy = new HtmlSerializerOptions();
        copy.nameMode = this.nameMode;
        copy.quoteMode = this.quoteMode;
        copy.asciiOnly = this.asciiOnly;
        copy.sortAttributes = this.sortAttributes;
        copy.selfCloseForeignElements = this.selfCloseForeignElements;
        copy.pretty = this.pretty;
        copy.omitOptionalTags = this.omitOptionalTags;
        copy.indentation = this.indentation;
        copy.newLine = this.newLine;
        copy.limits = this.limits.Copy();
        return copy;
    }

    /// <summary>Creates a validated immutable snapshot for concurrent reuse.</summary>
    /// <returns>An immutable compiled serializer configuration.</returns>
    public HtmlCompiledSerializerOptions Compile()
    {
        return new HtmlCompiledSerializerOptions(this);
    }

    internal void Validate()
    {
        ValidateNameMode(this.nameMode);
        ValidateQuoteMode(this.quoteMode);
        if (this.indentation == null || this.newLine == null || this.limits == null)
        {
            throw new InvalidOperationException("Serializer options contain a missing required value.");
        }

        this.limits.Validate();
    }

    /// <summary>Gets how HTML element and attribute names are cased during serialization.</summary>
    /// <returns>The configured canonical or preserve-case name mode.</returns>
    public HtmlSerializerNameMode GetNameMode()
    {
        return this.nameMode;
    }

    /// <summary>Sets the name mode used by subsequent operations.</summary>
    /// <param name = "value">The name-casing mode to validate and use.</param>
    public void SetNameMode(HtmlSerializerNameMode value)
    {
        ValidateNameMode(value);
        this.nameMode = value;
    }

    /// <summary>Gets how the serializer chooses quotation marks for attribute values.</summary>
    /// <returns>The configured double, single, or shortest-safe quote mode.</returns>
    public HtmlSerializerQuoteMode GetQuoteMode()
    {
        return this.quoteMode;
    }

    /// <summary>Sets the quote mode used by subsequent operations.</summary>
    /// <param name = "value">The attribute quotation mode to validate and use.</param>
    public void SetQuoteMode(HtmlSerializerQuoteMode value)
    {
        ValidateQuoteMode(value);
        this.quoteMode = value;
    }

    /// <summary>Reports whether non-ASCII characters are emitted as character references.</summary>
    /// <returns><c>true</c> when non-ASCII characters are escaped; otherwise, <c>false</c>.</returns>
    public bool GetAsciiOnly()
    {
        return this.asciiOnly;
    }

    /// <summary>Controls whether non-ASCII characters are emitted as character references.</summary>
    /// <param name = "value"><c>true</c> to encode non-ASCII scalars as numeric character references; otherwise, <c>false</c> to emit Unicode directly.</param>
    public void SetAsciiOnly(bool value)
    {
        this.asciiOnly = value;
    }

    /// <summary>Reports whether serialization sorts attributes instead of preserving DOM order.</summary>
    /// <returns><c>true</c> when attributes are sorted before serialization; otherwise, <c>false</c>.</returns>
    public bool GetSortAttributes()
    {
        return this.sortAttributes;
    }

    /// <summary>Controls whether attributes are sorted for deterministic serialization.</summary>
    /// <param name = "value"><c>true</c> to sort attributes deterministically; otherwise, <c>false</c> to retain DOM order.</param>
    public void SetSortAttributes(bool value)
    {
        this.sortAttributes = value;
    }

    /// <summary>Reports whether empty foreign elements use self-closing syntax.</summary>
    /// <returns><c>true</c> when empty foreign elements use self-closing syntax; otherwise, <c>false</c>.</returns>
    public bool GetSelfCloseForeignElements()
    {
        return this.selfCloseForeignElements;
    }

    /// <summary>Controls whether empty foreign elements use self-closing syntax.</summary>
    /// <param name = "value"><c>true</c> to use self-closing syntax for empty SVG, MathML, and other foreign elements.</param>
    public void SetSelfCloseForeignElements(bool value)
    {
        this.selfCloseForeignElements = value;
    }

    /// <summary>Reports whether safe indentation and line breaks are added for readability.</summary>
    /// <returns><c>true</c> when pretty-printing is enabled; otherwise, <c>false</c>.</returns>
    public bool GetPretty()
    {
        return this.pretty;
    }

    /// <summary>Enables or disables indented, human-readable output.</summary>
    /// <param name = "value"><c>true</c> to add safe indentation and line breaks outside whitespace-sensitive content.</param>
    public void SetPretty(bool value)
    {
        this.pretty = value;
    }

    /// <summary>Reports whether optional HTML tags may be omitted.</summary>
    /// <returns><c>true</c> when optional HTML tags may be omitted; otherwise, <c>false</c>.</returns>
    public bool GetOmitOptionalTags()
    {
        return this.omitOptionalTags;
    }

    /// <summary>Controls whether optional HTML tags may be omitted.</summary>
    /// <param name = "value"><c>true</c> to omit HTML start or end tags only where HTML parsing rules make omission unambiguous.</param>
    public void SetOmitOptionalTags(bool value)
    {
        this.omitOptionalTags = value;
    }

    /// <summary>Gets the text repeated once per nesting level when pretty printing is enabled.</summary>
    /// <returns>The indentation unit; an empty string disables visible indentation.</returns>
    public string GetIndentation()
    {
        return this.indentation;
    }

    /// <summary>Sets the indentation used by subsequent operations.</summary>
    /// <param name = "value">The indentation unit to copy; an empty string is allowed.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetIndentation(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.indentation = value;
    }

    /// <summary>Gets the line-ending sequence used when pretty printing is enabled.</summary>
    /// <returns>The configured line ending, which may be empty to keep pretty-printed breaks on one line.</returns>
    public string GetNewLine()
    {
        return this.newLine;
    }

    /// <summary>Sets the new line used by subsequent operations.</summary>
    /// <param name = "value">The line-ending sequence to copy; an empty string is allowed.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetNewLine(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.newLine = value;
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    /// <summary>Sets the limits used by subsequent operations.</summary>
    /// <param name = "value">The validated limits to copy; later changes to the supplied object do not affect these options.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLimits(HtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.limits = value.Copy();
    }

    internal static void ValidateNameMode(HtmlSerializerNameMode value)
    {
        if ((int)value < (int)HtmlSerializerNameMode.Canonical || (int)value > (int)HtmlSerializerNameMode.Preserve)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown serializer name mode.");
        }
    }

    internal static void ValidateQuoteMode(HtmlSerializerQuoteMode value)
    {
        if ((int)value < (int)HtmlSerializerQuoteMode.Double || (int)value > (int)HtmlSerializerQuoteMode.Shortest)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown serializer quote mode.");
        }
    }
}
