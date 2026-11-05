using System;
using System.Threading;

namespace html;
/// <summary>Configures resource ceilings that bound parsing, traversal, sanitization, and output work.</summary>
public class HtmlLimits : IHtmlLimits
{
    internal int maximumInputBytes;
    internal int maximumInputCharacters;
    internal int maximumTokens;
    internal int maximumAttributesPerTag;
    internal int maximumTokenCharacters;
    internal int maximumNameCharacters;
    internal int maximumAttributeValueCharacters;
    internal int maximumNodes;
    internal int maximumDepth;
    internal int maximumSelectorComplexity;
    internal int maximumSelectorResults;
    internal int maximumHasWork;
    internal int maximumCssTokens;
    internal int maximumCssDeclarations;
    internal int maximumUrlCharacters;
    internal int maximumDataUriBytes;
    internal int maximumTableCells;
    internal int maximumJsonDepth;
    internal int maximumJsonProperties;
    internal int maximumVisits;
    internal int maximumDiagnostics;
    internal int maximumOutputCharacters;
    /// <summary>Creates resource limits initialized to the general-purpose defaults.</summary>
    public HtmlLimits()
    {
        this.maximumInputBytes = 67108864;
        this.maximumInputCharacters = 33554432;
        this.maximumTokens = 2000000;
        this.maximumAttributesPerTag = 1024;
        this.maximumTokenCharacters = 4194304;
        this.maximumNameCharacters = 4096;
        this.maximumAttributeValueCharacters = 1048576;
        this.maximumNodes = 1000000;
        this.maximumDepth = 1024;
        this.maximumSelectorComplexity = 4096;
        this.maximumSelectorResults = 1000000;
        this.maximumHasWork = 10000000;
        this.maximumCssTokens = 100000;
        this.maximumCssDeclarations = 10000;
        this.maximumUrlCharacters = 1048576;
        this.maximumDataUriBytes = 16777216;
        this.maximumTableCells = 1000000;
        this.maximumJsonDepth = 128;
        this.maximumJsonProperties = 100000;
        this.maximumVisits = 10000000;
        this.maximumDiagnostics = 10000;
        this.maximumOutputCharacters = 67108864;
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <returns>A new independently mutable limits object containing the general-purpose thresholds.</returns>
    internal static HtmlLimits CreateDefault()
    {
        HtmlLimits limits = new HtmlLimits();
        limits.maximumInputBytes = 67108864;
        limits.maximumInputCharacters = 33554432;
        limits.maximumTokens = 2000000;
        limits.maximumAttributesPerTag = 1024;
        limits.maximumTokenCharacters = 4194304;
        limits.maximumNameCharacters = 4096;
        limits.maximumAttributeValueCharacters = 1048576;
        limits.maximumNodes = 1000000;
        limits.maximumDepth = 1024;
        limits.maximumSelectorComplexity = 4096;
        limits.maximumSelectorResults = 1000000;
        limits.maximumHasWork = 10000000;
        limits.maximumCssTokens = 100000;
        limits.maximumCssDeclarations = 10000;
        limits.maximumUrlCharacters = 1048576;
        limits.maximumDataUriBytes = 16777216;
        limits.maximumTableCells = 1000000;
        limits.maximumJsonDepth = 128;
        limits.maximumJsonProperties = 100000;
        limits.maximumVisits = 10000000;
        limits.maximumDiagnostics = 10000;
        limits.maximumOutputCharacters = 67108864;
        return limits;
    }

    /// <summary>Creates conservative resource limits for processing untrusted input.</summary>
    /// <returns>A new independently mutable limits object containing the lower untrusted-input thresholds.</returns>
    internal static HtmlLimits CreateUntrusted()
    {
        HtmlLimits limits = new HtmlLimits();
        limits.maximumInputBytes = 8388608;
        limits.maximumInputCharacters = 4194304;
        limits.maximumTokens = 250000;
        limits.maximumAttributesPerTag = 256;
        limits.maximumTokenCharacters = 262144;
        limits.maximumNameCharacters = 1024;
        limits.maximumAttributeValueCharacters = 262144;
        limits.maximumNodes = 100000;
        limits.maximumDepth = 256;
        limits.maximumSelectorComplexity = 1024;
        limits.maximumSelectorResults = 100000;
        limits.maximumHasWork = 250000;
        limits.maximumCssTokens = 25000;
        limits.maximumCssDeclarations = 2000;
        limits.maximumUrlCharacters = 16384;
        limits.maximumDataUriBytes = 1048576;
        limits.maximumTableCells = 100000;
        limits.maximumJsonDepth = 64;
        limits.maximumJsonProperties = 10000;
        limits.maximumVisits = 1000000;
        limits.maximumDiagnostics = 1000;
        limits.maximumOutputCharacters = 8388608;
        return limits;
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent limits object containing the same thresholds.</returns>
    public HtmlLimits Copy()
    {
        HtmlLimits copy = new HtmlLimits();
        copy.maximumInputBytes = this.maximumInputBytes;
        copy.maximumInputCharacters = this.maximumInputCharacters;
        copy.maximumTokens = this.maximumTokens;
        copy.maximumAttributesPerTag = this.maximumAttributesPerTag;
        copy.maximumTokenCharacters = this.maximumTokenCharacters;
        copy.maximumNameCharacters = this.maximumNameCharacters;
        copy.maximumAttributeValueCharacters = this.maximumAttributeValueCharacters;
        copy.maximumNodes = this.maximumNodes;
        copy.maximumDepth = this.maximumDepth;
        copy.maximumSelectorComplexity = this.maximumSelectorComplexity;
        copy.maximumSelectorResults = this.maximumSelectorResults;
        copy.maximumHasWork = this.maximumHasWork;
        copy.maximumCssTokens = this.maximumCssTokens;
        copy.maximumCssDeclarations = this.maximumCssDeclarations;
        copy.maximumUrlCharacters = this.maximumUrlCharacters;
        copy.maximumDataUriBytes = this.maximumDataUriBytes;
        copy.maximumTableCells = this.maximumTableCells;
        copy.maximumJsonDepth = this.maximumJsonDepth;
        copy.maximumJsonProperties = this.maximumJsonProperties;
        copy.maximumVisits = this.maximumVisits;
        copy.maximumDiagnostics = this.maximumDiagnostics;
        copy.maximumOutputCharacters = this.maximumOutputCharacters;
        return copy;
    }

    internal void Validate()
    {
        ValidatePositive(this.maximumInputBytes, "MaximumInputBytes");
        ValidatePositive(this.maximumInputCharacters, "MaximumInputCharacters");
        ValidatePositive(this.maximumTokens, "MaximumTokens");
        ValidatePositive(this.maximumAttributesPerTag, "MaximumAttributesPerTag");
        ValidatePositive(this.maximumTokenCharacters, "MaximumTokenCharacters");
        ValidatePositive(this.maximumNameCharacters, "MaximumNameCharacters");
        ValidatePositive(this.maximumAttributeValueCharacters, "MaximumAttributeValueCharacters");
        ValidatePositive(this.maximumNodes, "MaximumNodes");
        ValidatePositive(this.maximumDepth, "MaximumDepth");
        ValidatePositive(this.maximumSelectorComplexity, "MaximumSelectorComplexity");
        ValidatePositive(this.maximumSelectorResults, "MaximumSelectorResults");
        ValidatePositive(this.maximumHasWork, "MaximumHasWork");
        ValidatePositive(this.maximumCssTokens, "MaximumCssTokens");
        ValidatePositive(this.maximumCssDeclarations, "MaximumCssDeclarations");
        ValidatePositive(this.maximumUrlCharacters, "MaximumUrlCharacters");
        ValidatePositive(this.maximumDataUriBytes, "MaximumDataUriBytes");
        ValidatePositive(this.maximumTableCells, "MaximumTableCells");
        ValidatePositive(this.maximumJsonDepth, "MaximumJsonDepth");
        ValidatePositive(this.maximumJsonProperties, "MaximumJsonProperties");
        ValidatePositive(this.maximumVisits, "MaximumVisits");
        ValidatePositive(this.maximumDiagnostics, "MaximumDiagnostics");
        ValidatePositive(this.maximumOutputCharacters, "MaximumOutputCharacters");
    }

    /// <summary>Gets the largest byte input accepted before decoding stops.</summary>
    /// <returns>The positive byte-input ceiling.</returns>
    public int GetMaximumInputBytes()
    {
        return this.maximumInputBytes;
    }

    /// <summary>Sets the maximum input bytes used by subsequent operations.</summary>
    /// <param name = "value">The positive byte-input ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumInputBytes(int value)
    {
        this.maximumInputBytes = ValidatePositive(value, "value");
    }

    /// <summary>Gets the largest normalized character input accepted by parsing and conversion.</summary>
    /// <returns>The positive character-input ceiling.</returns>
    public int GetMaximumInputCharacters()
    {
        return this.maximumInputCharacters;
    }

    /// <summary>Sets the maximum input characters used by subsequent operations.</summary>
    /// <param name = "value">The positive normalized-character ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumInputCharacters(int value)
    {
        this.maximumInputCharacters = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many HTML tokenizer tokens one operation may emit.</summary>
    /// <returns>The positive HTML token ceiling.</returns>
    public int GetMaximumTokens()
    {
        return this.maximumTokens;
    }

    /// <summary>Sets the maximum tokens used by subsequent operations.</summary>
    /// <param name = "value">The positive HTML token ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumTokens(int value)
    {
        this.maximumTokens = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many attributes one start tag may retain.</summary>
    /// <returns>The positive per-tag attribute ceiling.</returns>
    public int GetMaximumAttributesPerTag()
    {
        return this.maximumAttributesPerTag;
    }

    /// <summary>Sets the maximum attributes per tag used by subsequent operations.</summary>
    /// <param name = "value">The positive per-tag attribute ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumAttributesPerTag(int value)
    {
        this.maximumAttributesPerTag = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many characters one HTML token may accumulate.</summary>
    /// <returns>The positive per-token character ceiling.</returns>
    public int GetMaximumTokenCharacters()
    {
        return this.maximumTokenCharacters;
    }

    /// <summary>Sets the maximum token characters used by subsequent operations.</summary>
    /// <param name = "value">The positive per-token character ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumTokenCharacters(int value)
    {
        this.maximumTokenCharacters = ValidatePositive(value, "value");
    }

    /// <summary>Gets the longest element, attribute, or similar parsed name that may be retained.</summary>
    /// <returns>The positive name-length ceiling.</returns>
    public int GetMaximumNameCharacters()
    {
        return this.maximumNameCharacters;
    }

    /// <summary>Sets the maximum name characters used by subsequent operations.</summary>
    /// <param name = "value">The positive name-length ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumNameCharacters(int value)
    {
        this.maximumNameCharacters = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many decoded characters one attribute value may contain.</summary>
    /// <returns>The positive per-attribute value ceiling.</returns>
    public int GetMaximumAttributeValueCharacters()
    {
        return this.maximumAttributeValueCharacters;
    }

    /// <summary>Sets the maximum attribute value characters used by subsequent operations.</summary>
    /// <param name = "value">The positive per-attribute value ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumAttributeValueCharacters(int value)
    {
        this.maximumAttributeValueCharacters = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many DOM nodes tree construction may allocate.</summary>
    /// <returns>The positive DOM node ceiling.</returns>
    public int GetMaximumNodes()
    {
        return this.maximumNodes;
    }

    /// <summary>Sets the maximum nodes used by subsequent operations.</summary>
    /// <param name = "value">The positive DOM node ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumNodes(int value)
    {
        this.maximumNodes = ValidatePositive(value, "value");
    }

    /// <summary>Gets the deepest tree, traversal, or nested-processing level an operation may enter.</summary>
    /// <returns>The positive nesting-depth ceiling.</returns>
    public int GetMaximumDepth()
    {
        return this.maximumDepth;
    }

    /// <summary>Sets the maximum depth used by subsequent operations.</summary>
    /// <param name = "value">The positive nesting-depth ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumDepth(int value)
    {
        this.maximumDepth = ValidatePositive(value, "value");
    }

    /// <summary>Gets the compilation budget for selector components and nested selector arguments.</summary>
    /// <returns>The positive selector-complexity budget.</returns>
    public int GetMaximumSelectorComplexity()
    {
        return this.maximumSelectorComplexity;
    }

    /// <summary>Sets the maximum selector complexity used by subsequent operations.</summary>
    /// <param name = "value">The positive selector-complexity budget.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumSelectorComplexity(int value)
    {
        this.maximumSelectorComplexity = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many nodes a selector query may return.</summary>
    /// <returns>The positive selector-result ceiling.</returns>
    public int GetMaximumSelectorResults()
    {
        return this.maximumSelectorResults;
    }

    /// <summary>Sets the maximum selector results used by subsequent operations.</summary>
    /// <param name = "value">The positive selector-result ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumSelectorResults(int value)
    {
        this.maximumSelectorResults = ValidatePositive(value, "value");
    }

    /// <summary>Gets the relationship-check budget used while evaluating selector <c>:has()</c> arguments.</summary>
    /// <returns>The positive <c>:has()</c> work budget.</returns>
    public int GetMaximumHasWork()
    {
        return this.maximumHasWork;
    }

    /// <summary>Sets the maximum has work used by subsequent operations.</summary>
    /// <param name = "value">The positive <c>:has()</c> work budget.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumHasWork(int value)
    {
        this.maximumHasWork = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many CSS tokens inline-style processing may produce.</summary>
    /// <returns>The positive CSS token ceiling.</returns>
    public int GetMaximumCssTokens()
    {
        return this.maximumCssTokens;
    }

    /// <summary>Sets the maximum CSS tokens used by subsequent operations.</summary>
    /// <param name = "value">The positive CSS token ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumCssTokens(int value)
    {
        this.maximumCssTokens = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many declarations one inline style may retain for policy evaluation.</summary>
    /// <returns>The positive CSS declaration ceiling.</returns>
    public int GetMaximumCssDeclarations()
    {
        return this.maximumCssDeclarations;
    }

    /// <summary>Sets the maximum CSS declarations used by subsequent operations.</summary>
    /// <param name = "value">The positive CSS declaration ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumCssDeclarations(int value)
    {
        this.maximumCssDeclarations = ValidatePositive(value, "value");
    }

    /// <summary>Gets the longest URL text accepted for normalization or policy checks.</summary>
    /// <returns>The positive URL-character ceiling.</returns>
    public int GetMaximumUrlCharacters()
    {
        return this.maximumUrlCharacters;
    }

    /// <summary>Sets the maximum URL characters used by subsequent operations.</summary>
    /// <param name = "value">The positive URL-character ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumUrlCharacters(int value)
    {
        this.maximumUrlCharacters = ValidatePositive(value, "value");
    }

    /// <summary>Gets the decoded payload ceiling for an allowed <c>data:</c> URL.</summary>
    /// <returns>The positive decoded data-URL byte ceiling.</returns>
    public int GetMaximumDataUriBytes()
    {
        return this.maximumDataUriBytes;
    }

    /// <summary>Sets the maximum data URI bytes used by subsequent operations.</summary>
    /// <param name = "value">The positive decoded data-URL byte ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumDataUriBytes(int value)
    {
        this.maximumDataUriBytes = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many logical cells table conversion may create after expanding row and column spans.</summary>
    /// <returns>The positive expanded-cell ceiling.</returns>
    public int GetMaximumTableCells()
    {
        return this.maximumTableCells;
    }

    /// <summary>Sets the maximum table cells used by subsequent operations.</summary>
    /// <param name = "value">The positive expanded-cell ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumTableCells(int value)
    {
        this.maximumTableCells = ValidatePositive(value, "value");
    }

    /// <summary>Gets the deepest JSON-LD array or object nesting accepted during metadata extraction.</summary>
    /// <returns>The positive JSON nesting ceiling.</returns>
    public int GetMaximumJsonDepth()
    {
        return this.maximumJsonDepth;
    }

    /// <summary>Sets the maximum JSON depth used by subsequent operations.</summary>
    /// <param name = "value">The positive JSON nesting ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumJsonDepth(int value)
    {
        this.maximumJsonDepth = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many JSON-LD properties metadata extraction may inspect.</summary>
    /// <returns>The positive JSON property ceiling.</returns>
    public int GetMaximumJsonProperties()
    {
        return this.maximumJsonProperties;
    }

    /// <summary>Sets the maximum JSON properties used by subsequent operations.</summary>
    /// <param name = "value">The positive JSON property ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumJsonProperties(int value)
    {
        this.maximumJsonProperties = ValidatePositive(value, "value");
    }

    /// <summary>Gets the shared ceiling for nodes or entries examined by bounded traversal operations.</summary>
    /// <returns>The positive traversal-visit ceiling.</returns>
    public int GetMaximumVisits()
    {
        return this.maximumVisits;
    }

    /// <summary>Sets the maximum visits used by subsequent operations.</summary>
    /// <param name = "value">The positive traversal-visit ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumVisits(int value)
    {
        this.maximumVisits = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many diagnostic objects an operation may retain.</summary>
    /// <returns>The positive retained-diagnostic ceiling.</returns>
    public int GetMaximumDiagnostics()
    {
        return this.maximumDiagnostics;
    }

    /// <summary>Sets the maximum diagnostics used by subsequent operations.</summary>
    /// <param name = "value">The positive retained-diagnostic ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumDiagnostics(int value)
    {
        this.maximumDiagnostics = ValidatePositive(value, "value");
    }

    /// <summary>Gets how many characters serialization or conversion may emit.</summary>
    /// <returns>The positive output-character ceiling.</returns>
    public int GetMaximumOutputCharacters()
    {
        return this.maximumOutputCharacters;
    }

    /// <summary>Sets the maximum output characters used by subsequent operations.</summary>
    /// <param name = "value">The positive output-character ceiling.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when <c>value</c> is not positive.</exception>
    public void SetMaximumOutputCharacters(int value)
    {
        this.maximumOutputCharacters = ValidatePositive(value, "value");
    }

    internal static int ValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "HTML limits must be greater than zero.");
        }

        return value;
    }
}
