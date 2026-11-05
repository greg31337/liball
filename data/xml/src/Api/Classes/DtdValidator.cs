namespace xml;

/// <summary>Provides the public DTD validator implementation.</summary>
public class DtdValidator : IDtdValidator
{
    /// <summary>Creates a DTD validator.</summary>
    public DtdValidator()
    {
    }

    /// <summary>Validates a document against its DTD.</summary>
    public XmlDiagnostic[] ValidateXmlDocumentDtd(XmlDocument document, XmlParserOptions options)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(options, "options");
        XmlDocumentType? documentType = document.DocumentType;
        if (documentType == null)
        {
            throw new System.ArgumentException("The XML document does not retain a document type definition.", "document");
        }

        DtdRetainedValidator validator = new DtdRetainedValidator(documentType, options, document.Standalone == XmlStandalone.Yes);
        return validator.ValidateRetainedDtdTree(document);
    }

    /// <summary>Validates events against a DTD.</summary>
    public XmlDiagnostic[] ValidateXmlEventDtd(XmlEventBatch events, XmlDocumentType documentType, XmlParserOptions options)
    {
        Guard.NotNull(events, "events");
        Guard.NotNull(documentType, "documentType");
        Guard.NotNull(options, "options");
        DtdRetainedValidator validator = new DtdRetainedValidator(documentType, options, false);
        return validator.ValidateRetainedDtdEventBatch(events);
    }
}
