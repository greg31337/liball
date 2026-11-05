namespace xml;

/// <summary>
/// Carries the dynamic context used to evaluate a compiled XPath expression.
/// </summary>
public class XPathDynamicContext
{
    internal XmlExtensionRuntimeState? ExtensionState;
    internal bool XsltCurrentGroupAvailable;
    internal XdmSequence? XsltCurrentGroup;
    internal bool XsltCurrentGroupingKeyAvailable;
    internal XdmSequence? XsltCurrentGroupingKey;
    internal bool XsltCurrentMergeGroupAvailable;
    internal XdmSequence? XsltCurrentMergeGroup;
    internal string[]? XsltCurrentMergeSourceNames;
    internal XdmSequence[]? XsltCurrentMergeSourceGroups;
    internal bool XsltCurrentMergeKeyAvailable;
    internal XdmSequence? XsltCurrentMergeKey;
    internal XsltKeyRuntime? XsltKeys;
    internal XsltAccumulatorRuntime? XsltAccumulators;
    internal string?[]? XsltRegexGroups;
    internal XsltFunctionRuntime? XsltFunctions;
    internal XsltComponent? XsltCurrentComponent;
    internal XdmSequence? XsltCurrentItem;
    internal string? XsltCurrentOutputUri;
    internal XPathResourceRegistry? XsltDocuments;
    internal XsltStreamedNodeRuntime? XsltStreamedNodes;
    /// <summary>
    /// The context item represented as an empty or singleton sequence.
    /// </summary>
    public XdmSequence? ContextItem;
    /// <summary>
    /// The focus position.
    /// </summary>
    public int Position;
    /// <summary>
    /// The focus size.
    /// </summary>
    public int Size;
    /// <summary>
    /// The dynamic variable bindings.
    /// </summary>
    public XPathVariableBinding[]? Variables;
    /// <summary>
    /// The resolver used when an external variable has no directly supplied value.
    /// </summary>
    public IXPathVariableResolver? VariableResolver;
    /// <summary>
    /// The dynamic base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The default collection URI.
    /// </summary>
    public string? DefaultCollectionUri;
    /// <summary>
    /// The implicit timezone offset in minutes.
    /// </summary>
    public int ImplicitTimezoneMinutes;
    /// <summary>
    /// The default language used by language-sensitive XPath functions.
    /// </summary>
    public string? DefaultLanguage;
    /// <summary>
    /// The stable current date and time snapshot.
    /// </summary>
    public System.DateTimeOffset CurrentDateTime;
    /// <summary>
    /// The permitted environment variable names.
    /// </summary>
    public string[]? EnvironmentNames;
    /// <summary>
    /// The corresponding deterministic environment values.
    /// </summary>
    public string[]? EnvironmentValues;
    /// <summary>
    /// The dynamic resource policy.
    /// </summary>
    public XmlResolverPolicy? ResolverPolicy;
    /// <summary>
    /// The document and collection resolver used during this evaluation.
    /// </summary>
    public IXPathCollectionResolver? CollectionResolver;
    /// <summary>
    /// The resolver used for unparsed text resources during this evaluation.
    /// </summary>
    public IXmlResourceResolver? ResourceResolver;
    /// <summary>
    /// The receiver for XPath trace values.
    /// </summary>
    public IXPathTraceReceiver? TraceReceiver;
    /// <summary>
    /// The caller-controlled cancellation source.
    /// </summary>
    public IXmlCancellation? Cancellation;
    /// <summary>
    /// The evaluation security limits.
    /// </summary>
    public XmlSecurityLimits? SecurityLimits;
    /// <summary>
    /// The maximum dynamic operation count.
    /// </summary>
    public long MaximumOperations;
}
