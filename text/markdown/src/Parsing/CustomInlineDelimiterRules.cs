using System;

namespace markdown;

internal class CustomInlineDelimiterRules
{
    internal const string CustomInlineDelimiterRuleName = "custom-inline.delimiter";

    internal static void RegisterCustomInlineDelimiterRules(MarkdownRuleList inlineRules)
    {
        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (inlineRules.Contains(CustomInlineDelimiterRuleName))
        {
            return;
        }

        int textIndex = inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (textIndex < 0)
        {
            inlineRules.Add(CustomInlineDelimiterRuleName, CreateCustomInlineDelimiterRule());
        }
        else
        {
            inlineRules.Insert(textIndex, CustomInlineDelimiterRuleName, CreateCustomInlineDelimiterRule());
        }
    }

    internal static MarkdownInlineRule CreateCustomInlineDelimiterRule()
    {
        return new MarkdownInlineRule(CustomInlineDelimiterRuleName, TryParseCustomInlineDelimiter);
    }

    internal static bool TryParseCustomInlineDelimiter(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownCustomInlineDelimiterRegistration? registration = FindRegistrationAt(context.ParseContext.Pipeline, context.Source, context.Position);
        if (registration == null)
        {
            return false;
        }

        int contentStart = context.Position + registration.OpeningDelimiter.Length;
        int closingStart = FindClosingDelimiter(context.Source, contentStart, registration.ClosingDelimiter);
        if (closingStart <= contentStart)
        {
            return false;
        }

        MarkdownNode custom = MarkdownNode.Create(MarkdownNodeKind.CustomInline);
        custom.SetAttribute("name", registration.Name);
        custom.SetAttribute("openingDelimiter", registration.OpeningDelimiter);
        custom.SetAttribute("closingDelimiter", registration.ClosingDelimiter);
        MarkdownInlineParseContext innerContext = MarkdownInlineParseContext.Create(
            context.ParseContext,
            custom,
            context.Source.Substring(contentStart, closingStart - contentStart));
        innerContext.InLink = context.InLink;
        InlineParser.Run(innerContext);
        context.AppendInline(custom);
        context.AdvanceBy(closingStart + registration.ClosingDelimiter.Length - context.Position);
        return true;
    }

    internal static MarkdownCustomInlineDelimiterRegistration? FindRegistrationAt(MarkdownPipeline pipeline, string source, int index)
    {
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (index < 0 || index > source.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        MarkdownCustomInlineDelimiterRegistration[] registrations = pipeline.GetOptions().CustomInlineDelimiters.GetRegistrations();
        int registrationIndex = 0;
        while (registrationIndex < registrations.Length)
        {
            if (TextFormattingInlineRules.StartsWithAt(source, index, registrations[registrationIndex].OpeningDelimiter))
            {
                return registrations[registrationIndex];
            }

            registrationIndex = registrationIndex + 1;
        }

        return null;
    }

    internal static bool IsPotentialDelimiterStart(MarkdownPipeline pipeline, string source, int index)
    {
        return FindRegistrationAt(pipeline, source, index) != null;
    }

    internal static int FindClosingDelimiter(string source, int startIndex, string delimiter)
    {
        return TextFormattingInlineRules.FindClosingDelimiter(source, startIndex, delimiter);
    }
}
