namespace xml;

/// <summary>Provides canonical XML output for documents and node selections.</summary>
public class XmlCanonicalizer : IXmlCanonicalizer
{
    /// <summary>Creates an XML canonicalizer.</summary>
    public XmlCanonicalizer()
    {
    }

    /// <summary>Canonicalizes XML data.</summary>
    public void CanonicalizeXml(XmlDocument document, XmlOutput output, XmlCanonicalizationOptions options)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        if (options.Method != XmlCanonicalizationMethod.CanonicalXml11 && options.Method != XmlCanonicalizationMethod.ExclusiveXml10 && options.Method != XmlCanonicalizationMethod.CanonicalXml20Experimental)
        {
            throw CreateXmlCanonicalizationError("The requested canonicalization method is not implemented.");
        }

        XmlOutput canonicalOutput = new XmlOutput();
        canonicalOutput.Kind = output.Kind;
        canonicalOutput.ByteStream = output.ByteStream;
        canonicalOutput.TextWriter = output.TextWriter;
        canonicalOutput.ByteSink = output.ByteSink;
        canonicalOutput.BaseUri = output.BaseUri;
        canonicalOutput.MediaType = output.MediaType;
        canonicalOutput.OwnsResource = output.OwnsResource;
        XmlOutputWriter writer = new XmlOutputWriter(canonicalOutput, "utf-8", false, 4096);
        new CanonicalXml11Writer(document, options, writer).WriteCanonicalDocument();
    }

    /// <summary>Canonicalizes normalized XML events without constructing a tree.</summary>
    public void CanonicalizeXmlEvents(XmlEventBatch events, XmlOutput output, XmlCanonicalizationOptions options)
    {
        Guard.NotNull(events, "events");
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        XmlEvent[] source = events.Events == null ? [] : events.Events;
        if (events.Offset < 0 || events.Count < 0 || events.Offset > source.Length - events.Count)
        {
            throw new System.ArgumentOutOfRangeException("events");
        }

        CanonicalXmlEventSink sink = new CanonicalXmlEventSink(output, options);
        XmlEventPipeline pipeline = new XmlEventPipeline([], sink);
        pipeline.ProcessXmlEventBatch(events);
        pipeline.CompleteXmlEventPipeline();
    }

    internal XmlCanonicalizationException CreateXmlCanonicalizationError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.CanonicalizationFailure;
        diagnostic.Category = XmlErrorCategory.Canonicalization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlCanonicalizationException(message, diagnostic, null);
    }
}
