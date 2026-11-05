using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Stores built-in and custom syntax profiles and applies them consistently to parsing and option creation.
/// </summary>
public class TemplateProfileRegistry : ITemplateProfileRegistry
{
    internal Dictionary<string, TemplateProfile> Profiles;

    /// <summary>
    /// Initializes a registry containing the native, Mustache, Handlebars, Liquid, Jinja, and Django profiles.
    /// </summary>
    public TemplateProfileRegistry()
    {
        Profiles = new Dictionary<string, TemplateProfile>(StringComparer.Ordinal);
        AddBuiltIn(NativeProfile.Create());
        AddBuiltIn(MustacheProfile.Create());
        AddBuiltIn(HandlebarsProfile.Create());
        AddBuiltIn(LiquidProfile.Create());
        AddBuiltIn(JinjaProfile.Create());
        AddBuiltIn(DjangoProfile.Create());
        AddBuiltIn(ScribanProfile.Create());
        AddBuiltIn(NunjucksProfile.Create());
        AddBuiltIn(GoTextTemplateProfile.Create());
        AddBuiltIn(RazorInspiredProfile.Create());
    }

    /// <summary>
    /// Registers or replaces a custom syntax profile after validation.
    /// </summary>
    /// <param name="profile">The custom profile to register.</param>
    public void Register(TemplateProfile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException("profile");
        }

        profile.Validate();
        if (IsBuiltInName(profile.Name))
        {
            throw new InvalidOperationException("Built-in template syntax profiles cannot be replaced.");
        }

        Profiles[profile.Name] = profile.Clone();
    }

    /// <summary>
    /// Removes a custom syntax profile and reports whether it existed.
    /// </summary>
    /// <param name="name">The registered profile name.</param>
    /// <returns><c>true</c> when a custom profile was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string name)
    {
        RequireName(name);
        if (IsBuiltInName(name))
        {
            return false;
        }

        return Profiles.Remove(name);
    }

    /// <summary>
    /// Reports whether a syntax profile is registered.
    /// </summary>
    /// <param name="name">The profile name to find.</param>
    /// <returns><c>true</c> when the profile is registered; otherwise, <c>false</c>.</returns>
    public bool Contains(string name)
    {
        RequireName(name);
        return Profiles.ContainsKey(name);
    }

    /// <summary>
    /// Returns a defensive copy of the named profile.
    /// </summary>
    /// <param name="name">The registered profile name.</param>
    /// <returns>An independent copy of the profile.</returns>
    public TemplateProfile GetProfile(string name)
    {
        RequireName(name);
        if (!Profiles.ContainsKey(name))
        {
            throw new ArgumentException("Unknown template syntax profile '" + name + "'.", "name");
        }

        return Profiles[name].Clone();
    }

    /// <summary>
    /// Creates options populated from a registered profile and the requested compatibility mode.
    /// </summary>
    /// <param name="name">The registered profile name.</param>
    /// <param name="compatibilityMode">The strict or pragmatic compatibility behavior.</param>
    /// <returns>Options configured for the profile.</returns>
    public TemplateOptions CreateOptions(string name, TemplateCompatibilityMode compatibilityMode)
    {
        if (compatibilityMode != TemplateCompatibilityMode.Pragmatic && compatibilityMode != TemplateCompatibilityMode.Strict)
        {
            throw new ArgumentOutOfRangeException("compatibilityMode");
        }

        TemplateProfile profile = GetProfile(name);
        profile.CompatibilityMode = compatibilityMode;
        TemplateOptions options = profile.CreateOptions();
        if (compatibilityMode == TemplateCompatibilityMode.Strict)
        {
            options.SetStrictMode(true);
        }

        return options;
    }

    /// <summary>
    /// Returns the registered profile names.
    /// </summary>
    /// <returns>A defensive list of registered names.</returns>
    public List<string> GetProfileNames()
    {
        List<string> names = new List<string>();
        foreach (KeyValuePair<string, TemplateProfile> pair in Profiles)
        {
            names.Add(pair.Key);
        }

        return names;
    }

    /// <summary>
    /// Parses template text using the delimiters and compatibility conversion of a registered profile.
    /// </summary>
    /// <param name="templateText">The template source text to parse.</param>
    /// <param name="profileName">The registered profile name.</param>
    /// <returns>The parsed document and diagnostics.</returns>
    public TemplateParseResult Parse(string templateText, string profileName)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateSource source = TemplateSource.FromString(templateText);
        return ParseSource(source, profileName);
    }

    internal TemplateParseResult ParseSource(TemplateSource source, string profileName)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();
        TemplateProfile profile = GetProfile(profileName);
        if (IsBuiltInCompatibilityName(profile.Name))
        {
            TemplateSource nativeSource = ConvertSourceToNative(source, profile.Name);
            TemplateLexer nativeLexer = new TemplateLexer(nativeSource);
            TemplateLexerResult nativeLexerResult = nativeLexer.Lex();
            TemplateParser nativeParser = new TemplateParser(nativeLexerResult);
            nativeParser.ExpressionPrecedence = profile.ExpressionPrecedence;
            return nativeParser.Parse();
        }

        TemplateLexer lexer = TemplateLexer.Create(source, profile);
        TemplateLexerResult lexerResult = lexer.Lex();
        TemplateParser parser = new TemplateParser(lexerResult);
        parser.ExpressionPrecedence = profile.ExpressionPrecedence;
        return parser.Parse();
    }

    internal TemplateSource ConvertSourceToNative(TemplateSource source, string profileName)
    {
        TemplateProfile profile = GetProfile(profileName);
        string text = ConvertTextToNative(source.Text, profile.Name);
        if (text == source.Text)
        {
            return source;
        }

        TemplateSource converted = new TemplateSource(source.Name, source.Path, source.Version, source.ContentHash, text);
        converted.Timestamp = source.Timestamp;
        return converted;
    }

    internal string ConvertTextToNative(string templateText, string profileName)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateProfile profile = GetProfile(profileName);
        if (profile.Name == "mustache")
        {
            return MustacheTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "handlebars")
        {
            return HandlebarsTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "liquid")
        {
            return LiquidTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "jinja")
        {
            return JinjaTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "django")
        {
            return JinjaTemplateConverter.ConvertDjangoToNative(templateText);
        }

        if (profile.Name == "scriban")
        {
            return ScribanTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "nunjucks")
        {
            return JinjaTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "go-text-template")
        {
            return GoTextTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "razor-inspired")
        {
            return RazorInspiredTemplateConverter.ConvertToNative(templateText);
        }

        if (profile.Name == "native")
        {
            return templateText;
        }

        return ConvertCustomTextToNative(templateText, profile);
    }

    internal string ConvertCustomTextToNative(string templateText, TemplateProfile profile)
    {
        TemplateSource source = TemplateSource.FromString(templateText);
        TemplateLexer lexer = TemplateLexer.Create(source, profile);
        TemplateLexerResult result = lexer.Lex();
        StringBuilder builder = new StringBuilder();
        int position = 0;
        int index = 0;
        while (index < result.Tokens.Count)
        {
            TemplateToken token = result.Tokens[index];
            if (token.Span.Start > position)
            {
                builder.Append(templateText.Substring(position, token.Span.Start - position));
            }

            AppendNativeToken(builder, token, profile);
            position = token.Span.End;
            index = index + 1;
        }

        if (position < templateText.Length)
        {
            builder.Append(templateText.Substring(position));
        }

        return builder.ToString();
    }

    internal void AppendNativeToken(StringBuilder builder, TemplateToken token, TemplateProfile profile)
    {
        if (token.Kind == TemplateTokenKind.OutputStartDelimiter)
        {
            builder.Append("{{");
        }
        else if (token.Kind == TemplateTokenKind.OutputEndDelimiter)
        {
            builder.Append("}}");
        }
        else if (token.Kind == TemplateTokenKind.StatementStartDelimiter)
        {
            builder.Append("{%");
        }
        else if (token.Kind == TemplateTokenKind.StatementEndDelimiter)
        {
            builder.Append("%}");
        }
        else if (token.Kind == TemplateTokenKind.CommentStartDelimiter)
        {
            builder.Append("{#");
        }
        else if (token.Kind == TemplateTokenKind.CommentEndDelimiter)
        {
            builder.Append("#}");
        }
        else if (token.Kind == TemplateTokenKind.LeftTrimMarker || token.Kind == TemplateTokenKind.RightTrimMarker)
        {
            TemplateTrimKind trimKind = token.LeftTrimKind;
            if (token.Kind == TemplateTokenKind.RightTrimMarker)
            {
                trimKind = token.RightTrimKind;
            }

            if (trimKind == TemplateTrimKind.NonGreedy)
            {
                builder.Append('~');
            }
            else
            {
                builder.Append('-');
            }
        }
        else if (token.Kind == TemplateTokenKind.RawBlockStartDelimiter)
        {
            AppendNativeRawTag(builder, profile.RawBlockStartKeyword, token);
        }
        else if (token.Kind == TemplateTokenKind.RawBlockEndDelimiter)
        {
            AppendNativeRawTag(builder, profile.RawBlockEndKeyword, token);
        }
        else if (token.Kind != TemplateTokenKind.EndOfFile)
        {
            builder.Append(token.Span.Source.Text.Substring(token.Span.Start, token.Span.Length));
        }
    }

    internal void AppendNativeRawTag(StringBuilder builder, string keyword, TemplateToken token)
    {
        builder.Append("{%");
        if (token.LeftTrimKind == TemplateTrimKind.NonGreedy)
        {
            builder.Append('~');
        }
        else if (token.TrimLeft)
        {
            builder.Append('-');
        }

        builder.Append(" ");
        builder.Append(keyword);
        builder.Append(" ");
        if (token.RightTrimKind == TemplateTrimKind.NonGreedy)
        {
            builder.Append('~');
        }
        else if (token.TrimRight)
        {
            builder.Append('-');
        }

        builder.Append("%}");
    }

    internal void AppendProfileKey(StringBuilder builder, string profileName)
    {
        TemplateProfile profile = GetProfile(profileName);
        AppendKeyPart(builder, profile.Name);
        AppendKeyPart(builder, profile.OutputStartDelimiter);
        AppendKeyPart(builder, profile.OutputEndDelimiter);
        AppendKeyPart(builder, profile.StatementStartDelimiter);
        AppendKeyPart(builder, profile.StatementEndDelimiter);
        AppendKeyPart(builder, profile.CommentStartDelimiter);
        AppendKeyPart(builder, profile.CommentEndDelimiter);
        AppendKeyPart(builder, profile.RawBlockStartKeyword);
        AppendKeyPart(builder, profile.RawBlockEndKeyword);
        AppendKeyPart(builder, profile.SupportsWhitespaceTrim ? "1" : "0");
        AppendKeyPart(builder, profile.LeftTrimMarker.ToString());
        AppendKeyPart(builder, profile.RightTrimMarker.ToString());
        AppendKeyPart(builder, profile.NonGreedyLeftTrimMarker.ToString());
        AppendKeyPart(builder, profile.NonGreedyRightTrimMarker.ToString());
        AppendKeyPart(builder, profile.VerbatimBlockStartKeyword);
        AppendKeyPart(builder, profile.VerbatimBlockEndKeyword);
        AppendKeyPart(builder, profile.StandaloneStatementTrimming ? "1" : "0");
        AppendKeyPart(builder, profile.CaseSensitiveNames ? "1" : "0");
        AppendKeyPart(builder, profile.AllowsComplexExpressions ? "1" : "0");
        AppendKeyPart(builder, ((int)profile.ExpressionPrecedence).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal void AppendKeyPart(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(":");
        builder.Append(value);
        builder.Append("|");
    }

    internal bool IsBuiltInCompatibilityName(string name)
    {
        return name == "mustache" || name == "handlebars" || name == "liquid" || name == "jinja" || name == "django" || name == "scriban" || name == "nunjucks" || name == "go-text-template" || name == "razor-inspired";
    }

    internal bool IsBuiltInName(string name)
    {
        return name == "native" || IsBuiltInCompatibilityName(name);
    }

    internal void AddBuiltIn(TemplateProfile profile)
    {
        profile.Validate();
        Profiles[profile.Name] = profile.Clone();
    }

    internal void RequireName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Template syntax profile name is required.", "name");
        }
    }
}
