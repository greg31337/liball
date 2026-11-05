namespace svg;

/// <summary>Represents one decoded IRI reference and its authored source span.</summary>
public class SvgIriReference
{
    /// <summary>The preserved or CSS-decoded reference text.</summary>
    public string Value;
    /// <summary>The fragment text without the number sign, or an empty string.</summary>
    public string Fragment;
    /// <summary>Whether the reference contains a fragment delimiter.</summary>
    public bool HasFragment;
    /// <summary>Whether the reference consists only of a fragment.</summary>
    public bool IsFragmentOnly;
    /// <summary>Whether the reference was authored with <c>url()</c>.</summary>
    public bool IsUrlFunction;
    /// <summary>The complete authored input.</summary>
    public string Source;
    /// <summary>The first character offset of the trimmed value.</summary>
    public int StartOffset;
    /// <summary>The exclusive ending offset of the trimmed value.</summary>
    public int EndOffset;
    /// <summary>Whether the mutable reference state is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a parsed IRI reference.</summary>
    /// <param name="value">The reference text.</param>
    /// <param name="isUrlFunction">Whether <c>url()</c> was authored.</param>
    /// <param name="source">The complete source text.</param>
    /// <param name="startOffset">The source starting offset.</param>
    /// <param name="endOffset">The exclusive source ending offset.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for an empty or inconsistent reference.</exception>
    public SvgIriReference(string value, bool isUrlFunction, string source, int startOffset, int endOffset)
    {
        if (value == null || value.Length == 0 || source == null || startOffset < 0 || endOffset < startOffset || endOffset > source.Length)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        Value = value;
        int fragment = value.IndexOf('#');
        HasFragment = fragment >= 0;
        Fragment = fragment >= 0 ? value.Substring(fragment + 1) : "";
        IsFragmentOnly = fragment == 0;
        IsUrlFunction = isUrlFunction;
        Source = source;
        StartOffset = startOffset;
        EndOffset = endOffset;
        IsValid = true;
    }

    /// <summary>Returns the decoded reference text.</summary>
    /// <returns>The reference text, or an empty string for invalid mutable state.</returns>
    internal string ToText()
    {
        if (!IsValid || Value == null || Value.Length == 0) return "";
        return Value;
    }
}

/// <summary>Contains a non-throwing IRI, fragment, or URL parse result.</summary>
public class SvgIriParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgIriParseError Error;
    /// <summary>The parsed reference, or <c>null</c>.</summary>
    public SvgIriReference? Reference;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates an IRI parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="reference">The parsed reference.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgIriParseResult(bool success, SvgIriParseError error, SvgIriReference? reference, int errorOffset)
    {
        Success = success;
        Error = error;
        Reference = reference;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one parsed function with decoded name and raw content.</summary>
public class SvgFunctionValue
{
    /// <summary>The decoded function name.</summary>
    public string Name;
    /// <summary>The raw content between the outer parentheses.</summary>
    public string Content;
    /// <summary>The complete authored input.</summary>
    public string Source;
    /// <summary>The function-name starting offset.</summary>
    public int StartOffset;
    /// <summary>The content starting offset.</summary>
    public int ContentOffset;
    /// <summary>The exclusive function ending offset.</summary>
    public int EndOffset;
    /// <summary>Whether the mutable function state is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a parsed function value.</summary>
    /// <param name="name">The decoded function name.</param>
    /// <param name="content">The raw function content.</param>
    /// <param name="source">The complete source text.</param>
    /// <param name="startOffset">The function starting offset.</param>
    /// <param name="contentOffset">The content starting offset.</param>
    /// <param name="endOffset">The exclusive function ending offset.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for inconsistent text or offsets.</exception>
    public SvgFunctionValue(string name, string content, string source, int startOffset, int contentOffset, int endOffset)
    {
        if (name == null || name.Length == 0 || content == null || source == null || startOffset < 0 || contentOffset <= startOffset || endOffset <= contentOffset || endOffset > source.Length)
        {
            throw new System.ArgumentOutOfRangeException("name");
        }
        Name = name;
        Content = content;
        Source = source;
        StartOffset = startOffset;
        ContentOffset = contentOffset;
        EndOffset = endOffset;
        IsValid = true;
    }

    /// <summary>Formats the decoded name and preserved function content.</summary>
    /// <returns>The function text, or an empty string for invalid state.</returns>
    internal string ToText()
    {
        if (!IsValid || Name == null || Name.Length == 0 || Content == null) return "";
        return Name + "(" + Content + ")";
    }
}

/// <summary>Contains a non-throwing function-value parse result.</summary>
public class SvgFunctionValueParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgIriParseError Error;
    /// <summary>The parsed function, or <c>null</c>.</summary>
    public SvgFunctionValue? Function;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a function-value parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="function">The parsed function.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgFunctionValueParseResult(bool success, SvgIriParseError error, SvgFunctionValue? function, int errorOffset)
    {
        Success = success;
        Error = error;
        Function = function;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one media type parameter and its source span.</summary>
public class SvgMediaTypeParameter
{
    /// <summary>The lowercase parameter name.</summary>
    public string Name;
    /// <summary>The decoded parameter value.</summary>
    public string Value;
    /// <summary>The parameter starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive parameter ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a media type parameter.</summary>
    /// <param name="name">The nonempty parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="startOffset">The source starting offset.</param>
    /// <param name="endOffset">The exclusive source ending offset.</param>
    public SvgMediaTypeParameter(string name, string value, int startOffset, int endOffset)
    {
        if (name == null || name.Length == 0 || value == null || startOffset < 0 || endOffset < startOffset) throw new System.ArgumentOutOfRangeException("name");
        Name = name;
        Value = value;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Represents a parsed RFC media type and ordered parameters.</summary>
public class SvgMediaType
{
    /// <summary>The lowercase top-level type.</summary>
    public string Type;
    /// <summary>The lowercase subtype.</summary>
    public string Subtype;
    /// <summary>The ordered decoded parameters.</summary>
    public System.Collections.Generic.List<SvgMediaTypeParameter> Parameters;
    /// <summary>The complete authored source.</summary>
    public string Source;
    /// <summary>The trimmed starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive trimmed ending offset.</summary>
    public int EndOffset;
    /// <summary>Whether the mutable media type is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a parsed media type and copies its parameters.</summary>
    /// <param name="type">The top-level type.</param>
    /// <param name="subtype">The subtype.</param>
    /// <param name="parameters">The ordered parameters.</param>
    /// <param name="source">The authored source.</param>
    /// <param name="startOffset">The trimmed starting offset.</param>
    /// <param name="endOffset">The exclusive ending offset.</param>
    public SvgMediaType(string type, string subtype, System.Collections.Generic.List<SvgMediaTypeParameter> parameters, string source, int startOffset, int endOffset)
    {
        if (type == null || type.Length == 0 || subtype == null || subtype.Length == 0 || parameters == null || source == null || startOffset < 0 || endOffset < startOffset || endOffset > source.Length) throw new System.ArgumentOutOfRangeException("type");
        Type = type.ToLowerInvariant();
        Subtype = subtype.ToLowerInvariant();
        Parameters = new System.Collections.Generic.List<SvgMediaTypeParameter>(parameters);
        Source = source;
        StartOffset = startOffset;
        EndOffset = endOffset;
        IsValid = true;
    }

    /// <summary>Formats the media type using lowercase names and minimal quoting.</summary>
    /// <returns>The canonical media type text.</returns>
    internal string ToText()
    {
        if (!IsValid || Type == null || Subtype == null || Parameters == null) return "";
        System.Text.StringBuilder builder = new System.Text.StringBuilder(Type + "/" + Subtype);
        int index = 0;
        while (index < Parameters.Count)
        {
            SvgMediaTypeParameter parameter = Parameters[index];
            builder.Append("; ");
            builder.Append(parameter.Name);
            builder.Append('=');
            if (SvgIriLexical.IsMimeToken(parameter.Value)) builder.Append(parameter.Value);
            else
            {
                builder.Append('"');
                int character = 0;
                while (character < parameter.Value.Length)
                {
                    char current = parameter.Value[character];
                    if (current == '\\' || current == '"') builder.Append('\\');
                    builder.Append(current);
                    character = character + 1;
                }
                builder.Append('"');
            }
            index = index + 1;
        }
        return builder.ToString();
    }
}

/// <summary>Contains a media type parse result.</summary>
public class SvgMediaTypeParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgIriParseError Error;
    /// <summary>The parsed media type, or <c>null</c>.</summary>
    public SvgMediaType? MediaType;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a media type parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="mediaType">The parsed media type.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgMediaTypeParseResult(bool success, SvgIriParseError error, SvgMediaType? mediaType, int errorOffset)
    {
        Success = success;
        Error = error;
        MediaType = mediaType;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Represents one list token and its source span.</summary>
public class SvgTextToken
{
    /// <summary>The normalized token value.</summary>
    public string Value;
    /// <summary>The exact authored token text.</summary>
    public string Source;
    /// <summary>The token starting offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive token ending offset.</summary>
    public int EndOffset;

    /// <summary>Creates a source-mapped text token.</summary>
    /// <param name="value">The normalized token.</param>
    /// <param name="source">The authored token.</param>
    /// <param name="startOffset">The token starting offset.</param>
    /// <param name="endOffset">The exclusive token ending offset.</param>
    public SvgTextToken(string value, string source, int startOffset, int endOffset)
    {
        if (value == null || value.Length == 0 || source == null || startOffset < 0 || endOffset < startOffset) throw new System.ArgumentOutOfRangeException("value");
        Value = value;
        Source = source;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Contains a source-mapped language or feature string list parse result.</summary>
public class SvgTextTokenListParseResult
{
    /// <summary>Whether the complete list parsed.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgIriParseError Error;
    /// <summary>The successfully parsed prefix.</summary>
    public System.Collections.Generic.List<SvgTextToken> Items;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a text-token list result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="items">The parsed prefix.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    public SvgTextTokenListParseResult(bool success, SvgIriParseError error, System.Collections.Generic.List<SvgTextToken> items, int errorOffset)
    {
        Success = success;
        Error = error;
        Items = items;
        ErrorOffset = errorOffset;
    }
}
