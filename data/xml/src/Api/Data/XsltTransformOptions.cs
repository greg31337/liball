namespace xml;

/// <summary>
/// Carries XSLT transformation invocation context and destinations.
/// </summary>
public class XsltTransformOptions
{
    /// <summary>
    /// The executable to invoke.
    /// </summary>
    public XsltExecutable? Executable;
    /// <summary>
    /// The principal source document.
    /// </summary>
    public XmlDocument? SourceDocument;
    /// <summary>
    /// The global context item represented as an empty or singleton sequence.
    /// </summary>
    public XdmSequence? GlobalContextItem;
    /// <summary>
    /// The initial sequence supplied to apply-templates invocation.
    /// </summary>
    public XdmSequence? InitialMatchSelection;
    /// <summary>
    /// The initial mode name.
    /// </summary>
    public XmlName? InitialMode;
    /// <summary>
    /// The initial named template.
    /// </summary>
    public XmlName? InitialTemplate;
    /// <summary>
    /// The initial stylesheet function.
    /// </summary>
    public XmlName? InitialFunction;
    /// <summary>
    /// The arguments supplied to the initial stylesheet function.
    /// </summary>
    public XdmSequence[]? FunctionParameters;
    /// <summary>
    /// The supplied non-static parameters.
    /// </summary>
    public XsltParameter[]? Parameters;
    /// <summary>
    /// The non-tunnel and tunnel parameters supplied to the initial template invocation.
    /// </summary>
    public XsltParameter[]? InitialTemplateParameters;
    /// <summary>
    /// The principal output destination.
    /// </summary>
    public XmlOutput? PrincipalOutput;
    /// <summary>
    /// The principal serialization parameters.
    /// </summary>
    public XmlSerializationOptions? SerializationOptions;
    /// <summary>
    /// The resolver used to allocate transactional secondary result destinations.
    /// </summary>
    public IXsltResultDocumentResolver? ResultDocumentResolver;
    /// <summary>
    /// The receiver for xsl:message and assertion messages.
    /// </summary>
    public IXsltMessageReceiver? MessageReceiver;
    /// <summary>
    /// The receiver for XSLT execution trace events.
    /// </summary>
    public IXsltTraceReceiver? TraceReceiver;
    /// <summary>
    /// The base output URI used for principal and secondary result identification.
    /// </summary>
    public string? BaseOutputUri;
    /// <summary>
    /// The dynamic recovery policy.
    /// </summary>
    public XsltRecoveryPolicy RecoveryPolicy;
    /// <summary>
    /// The document and resource resolver.
    /// </summary>
    public IXmlResourceResolver? ResourceResolver;
    /// <summary>
    /// The collection resolver used by XPath collection functions.
    /// </summary>
    public IXPathCollectionResolver? CollectionResolver;
    /// <summary>
    /// The receiver for XPath expression trace events.
    /// </summary>
    public IXPathTraceReceiver? XPathTraceReceiver;
    /// <summary>
    /// The cooperative cancellation source for the transformation.
    /// </summary>
    public IXmlCancellation? Cancellation;
    /// <summary>
    /// The transformation security limits.
    /// </summary>
    public XmlSecurityLimits? SecurityLimits;
    /// <summary>
    /// The requested principal result disposition.
    /// </summary>
    public XsltResultDisposition ResultDisposition;
    /// <summary>
    /// Whether execution trace events are collected.
    /// </summary>
    public bool EnableTracing;
    /// <summary>
    /// Whether xsl:assert instructions are evaluated.
    /// </summary>
    public bool EnableAssertions;
}
