using System.Collections.Generic;

namespace xml;

/// <summary>Provides the baseline transactional XSLT result-document collector.</summary>
public class XsltResultDocumentCollector : IXsltResultDocumentResolver
{
    internal XmlDiagnostic? Diagnostic;
    internal List<XsltSecondaryResult> Results;

    /// <summary>Creates an XSLT result-document collector.</summary>
    public XsltResultDocumentCollector()
    {
        Results = new List<XsltSecondaryResult>();
    }

    /// <summary>Allocates a secondary result destination.</summary>
    public XsltSecondaryResult AllocateXsltResultDocument(string uri, XmlSerializationOptions options)
    {
        if (uri == null) throw new System.ArgumentNullException("uri");
        if (options == null) throw new System.ArgumentNullException("options");
        XsltSecondaryResult result = new XsltSecondaryResult();
        result.Uri = uri;
        result.SerializationOptions = options;
        result.Disposition = XsltResultDisposition.Pending;
        lock (Results)
        {
            Results.Add(result);
        }

        return result;
    }

    /// <summary>Commits a secondary result.</summary>
    public void CommitXsltResultDocument(XsltSecondaryResult result)
    {
        if (result == null) throw new System.ArgumentNullException("result");
        lock (Results)
        {
            result.Disposition = XsltResultDisposition.Commit;
            result.Succeeded = true;
        }
    }

    /// <summary>Rejects a secondary result.</summary>
    public void RejectXsltResultDocument(XsltSecondaryResult result, XmlDiagnostic diagnostic)
    {
        if (result == null) throw new System.ArgumentNullException("result");
        if (diagnostic == null) throw new System.ArgumentNullException("diagnostic");
        lock (Results)
        {
            result.Disposition = XsltResultDisposition.Reject;
            result.Succeeded = false;
            Diagnostic = diagnostic;
        }
    }

    /// <summary>Rolls back a secondary result.</summary>
    public void RollbackXsltResultDocument(XsltSecondaryResult result)
    {
        if (result == null) throw new System.ArgumentNullException("result");
        lock (Results)
        {
            result.Disposition = XsltResultDisposition.Rollback;
            result.Succeeded = false;
        }
    }

    internal XsltSecondaryResult[] SnapshotXsltResultDocuments()
    {
        lock (Results)
        {
            return Results.ToArray();
        }
    }
}
