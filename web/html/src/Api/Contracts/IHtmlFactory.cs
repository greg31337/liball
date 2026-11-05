namespace html;

/// <summary>Defines construction and preset operations for public API values.</summary>
public interface IHtmlFactory
{
    /// <summary>Creates a mutable document with an empty document-node root.</summary>
    public HtmlDocument CreateDocument(string responseUrl, string baseUrl, string contentLanguage, HtmlSourceSpan sourceSpan);
    /// <summary>Creates a detached document type owned by a document.</summary>
    public HtmlNode CreateDocumentTypeNode(HtmlDocument ownerDocument, string? name, string? publicIdentifier, string? systemIdentifier, bool forceQuirks, HtmlSourceSpan sourceSpan);
    /// <summary>Creates a detached element owned by a document.</summary>
    public HtmlNode CreateElementNode(HtmlDocument ownerDocument, string namespaceUri, string qualifiedName, HtmlSourceSpan sourceSpan);
    /// <summary>Creates a detached text node owned by a document.</summary>
    public HtmlNode CreateTextNode(HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan);
    /// <summary>Creates a detached comment node owned by a document.</summary>
    public HtmlNode CreateCommentNode(HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan);
    /// <summary>Creates an empty document fragment owned by a document.</summary>
    public HtmlNode CreateFragmentNode(HtmlDocument ownerDocument, HtmlSourceSpan sourceSpan);
    /// <summary>Creates general-purpose resource limits.</summary>
    public HtmlLimits CreateDefaultLimits();
    /// <summary>Creates conservative resource limits for untrusted input.</summary>
    public HtmlLimits CreateUntrustedLimits();
    /// <summary>Creates default parsing options.</summary>
    public HtmlParseOptions CreateParseOptions();
    /// <summary>Creates a fragment context for an existing element.</summary>
    public HtmlFragmentContext CreateFragmentContext(HtmlNode element);
    /// <summary>Creates a known source position.</summary>
    public HtmlSourcePosition CreateSourcePosition(int characterOffset, int line, int column);
    /// <summary>Creates a source position with normalized and original offsets.</summary>
    public HtmlSourcePosition CreateMappedSourcePosition(int normalizedCharacterOffset, int characterOffset, int line, int column);
    /// <summary>Creates a source position that also has a byte offset.</summary>
    public HtmlSourcePosition CreateByteSourcePosition(int characterOffset, int line, int column, long byteOffset);
    /// <summary>Creates an unknown source position.</summary>
    public HtmlSourcePosition CreateUnknownSourcePosition();
    /// <summary>Creates a half-open source span.</summary>
    public HtmlSourceSpan CreateSourceSpan(HtmlSourcePosition start, HtmlSourcePosition end);
    /// <summary>Creates an unknown source span.</summary>
    public HtmlSourceSpan CreateUnknownSourceSpan();
    /// <summary>Creates an unnamespaced HTML attribute.</summary>
    public HtmlAttribute CreateAttribute(string name, string value, HtmlSourceSpan sourceSpan);
    /// <summary>Creates an attribute with explicit namespace components.</summary>
    public HtmlAttribute CreateNamespacedAttribute(string name, string localName, string prefix, string namespaceUri, string value, HtmlSourceSpan sourceSpan);
    /// <summary>Creates default article-extraction options.</summary>
    public HtmlArticleOptions CreateArticleOptions();
    /// <summary>Creates a text-only sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateTextOnlySanitizerPolicy();
    /// <summary>Creates a basic-formatting sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateBasicFormattingSanitizerPolicy();
    /// <summary>Creates a rich-text sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateRichTextSanitizerPolicy();
    /// <summary>Creates an HTML-only sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateHtmlOnlySanitizerPolicy();
    /// <summary>Creates default serializer options.</summary>
    public HtmlSerializerOptions CreateSerializerOptions();
    /// <summary>Creates source-preserving serializer options.</summary>
    public HtmlSerializerOptions CreatePreservingSerializerOptions();
    /// <summary>Creates compact serializer options.</summary>
    public HtmlSerializerOptions CreateCompactSerializerOptions();
    /// <summary>Creates pretty-printing serializer options.</summary>
    public HtmlSerializerOptions CreatePrettySerializerOptions();
    /// <summary>Creates default plain-text conversion options.</summary>
    public HtmlTextOptions CreateTextOptions();
    /// <summary>Creates search-index plain-text options.</summary>
    public HtmlTextOptions CreateSearchTextOptions();
    /// <summary>Creates CommonMark-compatible Markdown options.</summary>
    public HtmlMarkdownOptions CreateCommonMarkOptions();
    /// <summary>Creates GitHub Flavored Markdown options.</summary>
    public HtmlMarkdownOptions CreateGitHubMarkdownOptions();
    /// <summary>Creates readability-oriented Markdown options.</summary>
    public HtmlMarkdownOptions CreateReadableMarkdownOptions();
    /// <summary>Creates default tokenizer options.</summary>
    public HtmlTokenizerOptions CreateTokenizerOptions();
    /// <summary>Creates conservative tokenizer options.</summary>
    public HtmlTokenizerOptions CreateUntrustedTokenizerOptions();
    /// <summary>Creates untrusted-rich-text pipeline options.</summary>
    public HtmlProcessingOptions CreateRichTextProcessingOptions();
    /// <summary>Creates article pipeline options.</summary>
    public HtmlProcessingOptions CreateArticleProcessingOptions();
    /// <summary>Creates search-index pipeline options.</summary>
    public HtmlProcessingOptions CreateSearchProcessingOptions();
    /// <summary>Creates LLM-ingestion pipeline options.</summary>
    public HtmlProcessingOptions CreateLlmProcessingOptions();
    /// <summary>Creates an inclusive text conversion rule.</summary>
    public HtmlTextRule CreateIncludeTextRule(string selector);
    /// <summary>Creates an excluding text conversion rule.</summary>
    public HtmlTextRule CreateExcludeTextRule(string selector);
    /// <summary>Creates a replacing text conversion rule.</summary>
    public HtmlTextRule CreateReplaceTextRule(string selector, string replacement);
    /// <summary>Creates a default Markdown conversion rule.</summary>
    public HtmlMarkdownRule CreateDefaultMarkdownRule(string selector);
    /// <summary>Creates a keep Markdown conversion rule.</summary>
    public HtmlMarkdownRule CreateKeepMarkdownRule(string selector);
    /// <summary>Creates a remove Markdown conversion rule.</summary>
    public HtmlMarkdownRule CreateRemoveMarkdownRule(string selector);
    /// <summary>Creates a URL-only source-set candidate.</summary>
    public HtmlSrcsetCandidate CreateSrcsetCandidate(string url);
    /// <summary>Creates a width source-set candidate.</summary>
    public HtmlSrcsetCandidate CreateWidthSrcsetCandidate(string url, int width);
    /// <summary>Creates a density source-set candidate.</summary>
    public HtmlSrcsetCandidate CreateDensitySrcsetCandidate(string url, double density);
}
