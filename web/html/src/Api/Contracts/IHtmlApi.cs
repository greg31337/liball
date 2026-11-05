using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the Html operations.</summary>
public interface IHtmlApi
{
    /// <summary>Executes the ParseString operation.</summary>
    public HtmlDocument ParseString(string html);
    /// <summary>Executes the ParseCharacters operation.</summary>
    public HtmlDocument ParseCharacters(ReadOnlySpan<char> html);
    /// <summary>Executes the ParseStringWithOptions operation.</summary>
    public HtmlParseResult ParseStringWithOptions(string html, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseCharactersWithOptions operation.</summary>
    public HtmlParseResult ParseCharactersWithOptions(ReadOnlySpan<char> html, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseBytesWithOptions operation.</summary>
    public HtmlParseResult ParseBytesWithOptions(byte[] html, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseByteSpanWithOptions operation.</summary>
    public HtmlParseResult ParseByteSpanWithOptions(ReadOnlySpan<byte> html, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseStream operation.</summary>
    public HtmlParseResult ParseStream(Stream stream, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseReader operation.</summary>
    public HtmlParseResult ParseReader(TextReader reader, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseFragment operation.</summary>
    public HtmlParseResult ParseFragment(string html, string contextTag);
    /// <summary>Executes the ParseFragmentWithOptions operation.</summary>
    public HtmlParseResult ParseFragmentWithOptions(string html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseFragmentCharactersWithOptions operation.</summary>
    public HtmlParseResult ParseFragmentCharactersWithOptions(ReadOnlySpan<char> html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseFragmentBytesWithOptions operation.</summary>
    public HtmlParseResult ParseFragmentBytesWithOptions(byte[] html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseFragmentByteSpanWithOptions operation.</summary>
    public HtmlParseResult ParseFragmentByteSpanWithOptions(ReadOnlySpan<byte> html, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseFragmentStream operation.</summary>
    public HtmlParseResult ParseFragmentStream(Stream stream, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ParseFragmentReader operation.</summary>
    public HtmlParseResult ParseFragmentReader(TextReader reader, HtmlFragmentContext context, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the Sanitize operation.</summary>
    public string Sanitize(string html);
    /// <summary>Executes the SanitizeWithOptions operation.</summary>
    public HtmlSanitizeResult SanitizeWithOptions(string html, HtmlSanitizerPolicy policy, IHtmlParseConfiguration parseOptions, CancellationToken cancellationToken);
    /// <summary>Executes the ToMarkdown operation.</summary>
    public string ToMarkdown(string html);
    /// <summary>Executes the ToMarkdownWithOptions operation.</summary>
    public HtmlMarkdownResult ToMarkdownWithOptions(string html, IHtmlMarkdownOptions options, CancellationToken cancellationToken);
    /// <summary>Executes the ToText operation.</summary>
    public string ToText(string html);
    /// <summary>Executes the ToTextWithOptions operation.</summary>
    public HtmlTextResult ToTextWithOptions(string html, IHtmlTextOptions options, CancellationToken cancellationToken);
    /// <summary>Executes the ExtractMetadata operation.</summary>
    public HtmlMetadata ExtractMetadata(string html, IHtmlMetadataOptions options, CancellationToken cancellationToken);
    /// <summary>Executes the ExtractArticle operation.</summary>
    public HtmlArticleResult ExtractArticle(string html, IHtmlArticleConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessString operation.</summary>
    public HtmlProcessingResult ProcessString(string html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessCharacters operation.</summary>
    public HtmlProcessingResult ProcessCharacters(ReadOnlySpan<char> html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessBytes operation.</summary>
    public HtmlProcessingResult ProcessBytes(byte[] html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessByteSpan operation.</summary>
    public HtmlProcessingResult ProcessByteSpan(ReadOnlySpan<byte> html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessStream operation.</summary>
    public HtmlProcessingResult ProcessStream(Stream stream, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessReader operation.</summary>
    public HtmlProcessingResult ProcessReader(TextReader reader, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessDocument operation.</summary>
    public HtmlProcessingResult ProcessDocument(HtmlDocument document, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessFragment operation.</summary>
    public HtmlProcessingResult ProcessFragment(HtmlNode fragment, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the ProcessNode operation.</summary>
    public HtmlProcessingResult ProcessNode(HtmlNode node, IHtmlProcessingConfiguration options, CancellationToken cancellationToken);
}
