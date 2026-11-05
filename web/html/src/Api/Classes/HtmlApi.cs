using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the Html operations.</summary>
public class HtmlApi : IHtmlApi
{
    /// <summary>Executes the ParseString operation.</summary>
    public HtmlDocument ParseString(string html)
    {
        return Html.Parse(html);
    }

    /// <summary>Executes the ParseCharacters operation.</summary>
    public HtmlDocument ParseCharacters(ReadOnlySpan<char> html)
    {
        return Html.Parse(html);
    }

    /// <summary>Executes the ParseStringWithOptions operation.</summary>
    public HtmlParseResult ParseStringWithOptions(string html, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Parse(html, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Parses a character span with copied options and cancellation.</summary>
    public HtmlParseResult ParseCharactersWithOptions(ReadOnlySpan<char> html, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Parse(html, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Executes the ParseBytesWithOptions operation.</summary>
    public HtmlParseResult ParseBytesWithOptions(byte[] html, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Parse(html, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Executes the ParseByteSpanWithOptions operation.</summary>
    public HtmlParseResult ParseByteSpanWithOptions(ReadOnlySpan<byte> html, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Parse(html, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Executes the ParseStream operation.</summary>
    public HtmlParseResult ParseStream(Stream stream, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Parse(stream, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Executes the ParseReader operation.</summary>
    public HtmlParseResult ParseReader(TextReader reader, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Parse(reader, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Executes the ParseFragment operation.</summary>
    public HtmlParseResult ParseFragment(string html, string contextTag)
    {
        return Html.ParseFragment(html, contextTag);
    }

    /// <summary>Executes the ParseFragmentWithOptions operation.</summary>
    public HtmlParseResult ParseFragmentWithOptions(string html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ParseFragment(html, context, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Parses a character span as a context-sensitive fragment.</summary>
    public HtmlParseResult ParseFragmentCharactersWithOptions(ReadOnlySpan<char> html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ParseFragment(html, context, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Parses a byte array as a context-sensitive fragment.</summary>
    public HtmlParseResult ParseFragmentBytesWithOptions(byte[] html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ParseFragment(html, context, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Parses a byte span as a context-sensitive fragment.</summary>
    public HtmlParseResult ParseFragmentByteSpanWithOptions(ReadOnlySpan<byte> html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ParseFragment(html, context, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Parses an encoded stream as a context-sensitive fragment.</summary>
    public HtmlParseResult ParseFragmentStream(Stream stream, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ParseFragment(stream, context, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Parses a text reader as a context-sensitive fragment.</summary>
    public HtmlParseResult ParseFragmentReader(TextReader reader, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ParseFragment(reader, context, CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Executes the Sanitize operation.</summary>
    public string Sanitize(string html)
    {
        return Html.Sanitize(html);
    }

    /// <summary>Executes the SanitizeWithOptions operation.</summary>
    public HtmlSanitizeResult SanitizeWithOptions(string html, HtmlSanitizerPolicy policy, IHtmlParseConfiguration parseOptions, CancellationToken cancellationToken)
    {
        return Html.Sanitize(html, policy, CreateParseOptions(parseOptions), cancellationToken);
    }

    /// <summary>Executes the ToMarkdown operation.</summary>
    public string ToMarkdown(string html)
    {
        return Html.ToMarkdown(html);
    }

    /// <summary>Executes the ToMarkdownWithOptions operation.</summary>
    public HtmlMarkdownResult ToMarkdownWithOptions(string html, IHtmlMarkdownOptions options, CancellationToken cancellationToken)
    {
        return Html.ToMarkdown(html, options, cancellationToken);
    }

    /// <summary>Executes the ToText operation.</summary>
    public string ToText(string html)
    {
        return Html.ToText(html);
    }

    /// <summary>Executes the ToTextWithOptions operation.</summary>
    public HtmlTextResult ToTextWithOptions(string html, IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        return Html.ToText(html, options, cancellationToken);
    }

    /// <summary>Executes the ExtractMetadata operation.</summary>
    public HtmlMetadata ExtractMetadata(string html, IHtmlMetadataOptions options, CancellationToken cancellationToken)
    {
        return Html.ExtractMetadata(html, options, cancellationToken);
    }

    /// <summary>Executes the ExtractArticle operation.</summary>
    public HtmlArticleResult ExtractArticle(string html, IHtmlArticleConfiguration options, CancellationToken cancellationToken)
    {
        return Html.ExtractArticle(html, CreateArticleOptions(options), cancellationToken);
    }

    /// <summary>Executes the ProcessString operation.</summary>
    public HtmlProcessingResult ProcessString(string html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Process(html, options, cancellationToken);
    }

    /// <summary>Executes the ProcessCharacters operation.</summary>
    public HtmlProcessingResult ProcessCharacters(ReadOnlySpan<char> html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessCharacters(html, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessBytes operation.</summary>
    public HtmlProcessingResult ProcessBytes(byte[] html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return Html.Process(html, options, cancellationToken);
    }

    /// <summary>Executes the ProcessByteSpan operation.</summary>
    public HtmlProcessingResult ProcessByteSpan(ReadOnlySpan<byte> html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessByteSpan(html, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessStream operation.</summary>
    public HtmlProcessingResult ProcessStream(Stream stream, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessStream(stream, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessReader operation.</summary>
    public HtmlProcessingResult ProcessReader(TextReader reader, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessReader(reader, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessDocument operation.</summary>
    public HtmlProcessingResult ProcessDocument(HtmlDocument document, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessDocument(document, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessFragment operation.</summary>
    public HtmlProcessingResult ProcessFragment(HtmlNode fragment, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessFragment(fragment, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessNode operation.</summary>
    public HtmlProcessingResult ProcessNode(HtmlNode node, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessNode(node, options,
            cancellationToken);
    }

    internal static HtmlParseOptions CreateParseOptions(
        IHtmlParseConfiguration options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return options.Compile().CreateOptions();
    }

    internal static HtmlArticleOptions CreateArticleOptions(
        IHtmlArticleConfiguration options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return options.Compile().CreateOptions();
    }
}
