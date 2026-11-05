namespace html;

/// <summary>Constructs public API values and initialized configuration presets.</summary>
public class HtmlFactory : IHtmlFactory
{
    /// <summary>Creates a mutable document with an empty document-node root.</summary>
    public HtmlDocument CreateDocument(string responseUrl, string baseUrl, string contentLanguage, HtmlSourceSpan sourceSpan)
    {
        HtmlDocument document = new HtmlDocument(responseUrl, baseUrl,
            HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0], sourceSpan);
        document.SetContentLanguageStorage(contentLanguage);
        return document;
    }
    /// <summary>Creates a detached document type owned by a document.</summary>
    public HtmlNode CreateDocumentTypeNode(HtmlDocument ownerDocument, string? name, string? publicIdentifier, string? systemIdentifier, bool forceQuirks, HtmlSourceSpan sourceSpan)
    {
        if (ownerDocument == null)
        {
            throw new System.ArgumentNullException("ownerDocument");
        }

        return HtmlNode.CreateDocumentTypeStorage(ownerDocument,
            name != null, name ?? "", publicIdentifier != null,
            publicIdentifier ?? "", systemIdentifier != null,
            systemIdentifier ?? "", forceQuirks, sourceSpan);
    }
    /// <summary>Creates a detached element owned by a document.</summary>
    public HtmlNode CreateElementNode(HtmlDocument ownerDocument, string namespaceUri, string qualifiedName, HtmlSourceSpan sourceSpan)
    {
        if (ownerDocument == null)
        {
            throw new System.ArgumentNullException("ownerDocument");
        }

        return ownerDocument.CreateElementWithSourceSpanStorage(namespaceUri,
            qualifiedName, sourceSpan);
    }
    /// <summary>Creates a detached text node owned by a document.</summary>
    public HtmlNode CreateTextNode(HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateTextStorage(ownerDocument, data, sourceSpan);
    }
    /// <summary>Creates a detached comment node owned by a document.</summary>
    public HtmlNode CreateCommentNode(HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateCommentStorage(ownerDocument, data, sourceSpan);
    }
    /// <summary>Creates an empty document fragment owned by a document.</summary>
    public HtmlNode CreateFragmentNode(HtmlDocument ownerDocument, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateFragmentStorage(ownerDocument, sourceSpan);
    }
    /// <summary>Creates general-purpose resource limits.</summary>
    public HtmlLimits CreateDefaultLimits() { return HtmlLimits.CreateDefault(); }
    /// <summary>Creates conservative resource limits for untrusted input.</summary>
    public HtmlLimits CreateUntrustedLimits() { return HtmlLimits.CreateUntrusted(); }
    /// <summary>Creates default parsing options.</summary>
    public HtmlParseOptions CreateParseOptions() { return HtmlParseOptions.CreateDefault(); }
    /// <summary>Creates a fragment context for an existing element.</summary>
    public HtmlFragmentContext CreateFragmentContext(HtmlNode element) { return HtmlFragmentContext.Create(element); }
    /// <summary>Creates a known source position.</summary>
    public HtmlSourcePosition CreateSourcePosition(int characterOffset, int line, int column) { return HtmlSourcePosition.Create(characterOffset, line, column); }
    /// <summary>Creates a source position with normalized and original offsets.</summary>
    public HtmlSourcePosition CreateMappedSourcePosition(int normalizedCharacterOffset, int characterOffset, int line, int column) { return HtmlSourcePosition.CreateMapped(normalizedCharacterOffset, characterOffset, line, column); }
    /// <summary>Creates a source position that also has a byte offset.</summary>
    public HtmlSourcePosition CreateByteSourcePosition(int characterOffset, int line, int column, long byteOffset) { return HtmlSourcePosition.CreateWithByteOffset(characterOffset, line, column, byteOffset); }
    /// <summary>Creates an unknown source position.</summary>
    public HtmlSourcePosition CreateUnknownSourcePosition() { return HtmlSourcePosition.CreateUnknown(); }
    /// <summary>Creates a half-open source span.</summary>
    public HtmlSourceSpan CreateSourceSpan(HtmlSourcePosition start, HtmlSourcePosition end) { return HtmlSourceSpan.Create(start, end); }
    /// <summary>Creates an unknown source span.</summary>
    public HtmlSourceSpan CreateUnknownSourceSpan() { return HtmlSourceSpan.CreateUnknown(); }
    /// <summary>Creates an unnamespaced HTML attribute.</summary>
    public HtmlAttribute CreateAttribute(string name, string value, HtmlSourceSpan sourceSpan) { return HtmlAttribute.Create(name, value, sourceSpan); }
    /// <summary>Creates an attribute with explicit namespace components.</summary>
    public HtmlAttribute CreateNamespacedAttribute(string name, string localName, string prefix, string namespaceUri, string value, HtmlSourceSpan sourceSpan) { return HtmlAttribute.CreateNamespaced(name, localName, prefix, namespaceUri, value, sourceSpan); }
    /// <summary>Creates default article-extraction options.</summary>
    public HtmlArticleOptions CreateArticleOptions() { return HtmlArticleOptions.CreateDefault(); }
    /// <summary>Creates a text-only sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateTextOnlySanitizerPolicy() { return HtmlSanitizerPolicy.CreateTextOnly(); }
    /// <summary>Creates a basic-formatting sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateBasicFormattingSanitizerPolicy() { return HtmlSanitizerPolicy.CreateBasicFormatting(); }
    /// <summary>Creates a rich-text sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateRichTextSanitizerPolicy() { return HtmlSanitizerPolicy.CreateRichText(); }
    /// <summary>Creates an HTML-only sanitizer policy.</summary>
    public HtmlSanitizerPolicy CreateHtmlOnlySanitizerPolicy() { return HtmlSanitizerPolicy.CreateHtmlOnly(); }
    /// <summary>Creates default serializer options.</summary>
    public HtmlSerializerOptions CreateSerializerOptions() { return HtmlSerializerOptions.CreateDefault(); }
    /// <summary>Creates source-preserving serializer options.</summary>
    public HtmlSerializerOptions CreatePreservingSerializerOptions() { return HtmlSerializerOptions.CreatePreserve(); }
    /// <summary>Creates compact serializer options.</summary>
    public HtmlSerializerOptions CreateCompactSerializerOptions() { return HtmlSerializerOptions.CreateCompact(); }
    /// <summary>Creates pretty-printing serializer options.</summary>
    public HtmlSerializerOptions CreatePrettySerializerOptions() { return HtmlSerializerOptions.CreatePretty(); }
    /// <summary>Creates default plain-text conversion options.</summary>
    public HtmlTextOptions CreateTextOptions() { return HtmlTextOptions.CreateDefault(); }
    /// <summary>Creates search-index plain-text options.</summary>
    public HtmlTextOptions CreateSearchTextOptions() { return HtmlTextOptions.CreateSearchIndex(); }
    /// <summary>Creates CommonMark-compatible Markdown options.</summary>
    public HtmlMarkdownOptions CreateCommonMarkOptions() { return HtmlMarkdownOptions.CreateCommonMark(); }
    /// <summary>Creates GitHub Flavored Markdown options.</summary>
    public HtmlMarkdownOptions CreateGitHubMarkdownOptions() { return HtmlMarkdownOptions.CreateGitHubFlavored(); }
    /// <summary>Creates readability-oriented Markdown options.</summary>
    public HtmlMarkdownOptions CreateReadableMarkdownOptions() { return HtmlMarkdownOptions.CreateReadable(); }
    /// <summary>Creates default tokenizer options.</summary>
    public HtmlTokenizerOptions CreateTokenizerOptions() { return HtmlTokenizerOptions.CreateDefault(); }
    /// <summary>Creates conservative tokenizer options.</summary>
    public HtmlTokenizerOptions CreateUntrustedTokenizerOptions() { return HtmlTokenizerOptions.CreateUntrusted(); }
    /// <summary>Creates untrusted-rich-text pipeline options.</summary>
    public HtmlProcessingOptions CreateRichTextProcessingOptions() { return HtmlProcessingOptions.CreateUntrustedRichText(); }
    /// <summary>Creates article pipeline options.</summary>
    public HtmlProcessingOptions CreateArticleProcessingOptions() { return HtmlProcessingOptions.CreateArticle(); }
    /// <summary>Creates search-index pipeline options.</summary>
    public HtmlProcessingOptions CreateSearchProcessingOptions() { return HtmlProcessingOptions.CreateSearchIndex(); }
    /// <summary>Creates LLM-ingestion pipeline options.</summary>
    public HtmlProcessingOptions CreateLlmProcessingOptions() { return HtmlProcessingOptions.CreateLlmIngestion(); }
    /// <summary>Creates an inclusive text conversion rule.</summary>
    public HtmlTextRule CreateIncludeTextRule(string selector) { return HtmlTextRule.CreateInclude(selector); }
    /// <summary>Creates an excluding text conversion rule.</summary>
    public HtmlTextRule CreateExcludeTextRule(string selector) { return HtmlTextRule.CreateExclude(selector); }
    /// <summary>Creates a replacing text conversion rule.</summary>
    public HtmlTextRule CreateReplaceTextRule(string selector, string replacement) { return HtmlTextRule.CreateReplace(selector, replacement); }
    /// <summary>Creates a default Markdown conversion rule.</summary>
    public HtmlMarkdownRule CreateDefaultMarkdownRule(string selector) { return HtmlMarkdownRule.CreateDefault(selector); }
    /// <summary>Creates a keep Markdown conversion rule.</summary>
    public HtmlMarkdownRule CreateKeepMarkdownRule(string selector) { return HtmlMarkdownRule.CreateKeep(selector); }
    /// <summary>Creates a remove Markdown conversion rule.</summary>
    public HtmlMarkdownRule CreateRemoveMarkdownRule(string selector) { return HtmlMarkdownRule.CreateRemove(selector); }
    /// <summary>Creates a URL-only source-set candidate.</summary>
    public HtmlSrcsetCandidate CreateSrcsetCandidate(string url) { return HtmlSrcsetCandidate.Create(url); }
    /// <summary>Creates a width source-set candidate.</summary>
    public HtmlSrcsetCandidate CreateWidthSrcsetCandidate(string url, int width) { return HtmlSrcsetCandidate.CreateWidth(url, width); }
    /// <summary>Creates a density source-set candidate.</summary>
    public HtmlSrcsetCandidate CreateDensitySrcsetCandidate(string url, double density) { return HtmlSrcsetCandidate.CreateDensity(url, density); }
}
