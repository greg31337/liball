using System.Collections.Generic;

namespace xml;

/// <summary>Provides the baseline XSLT message collector.</summary>
public class XsltMessageCollector : IXsltMessageReceiver
{
    internal XsltMessage? Message;
    internal List<XsltMessage> Messages;

    /// <summary>Creates an XSLT message collector.</summary>
    public XsltMessageCollector()
    {
        Messages = new List<XsltMessage>();
    }

    /// <summary>Receives an XSLT message.</summary>
    public void ReceiveXsltMessage(XsltMessage message)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        XsltMessage copy = CopyXsltCollectedMessage(message);
        lock (Messages)
        {
            Message = copy;
            Messages.Add(copy);
        }
    }

    internal XsltMessage[] SnapshotXsltMessages()
    {
        lock (Messages)
        {
            XsltMessage[] result = new XsltMessage[Messages.Count];
            int index = 0;
            while (index < Messages.Count)
            {
                result[index] = CopyXsltCollectedMessage(Messages[index]);
                index = index + 1;
            }

            return result;
        }
    }

    internal XsltMessage CopyXsltCollectedMessage(XsltMessage source)
    {
        XsltMessage result = new XsltMessage();
        result.Text = source.Text;
        result.Code = CopyXsltCollectedName(source.Code);
        result.Terminate = source.Terminate;
        result.Range = CopyXsltCollectedRange(source.Range);
        result.Content = source.Content == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(source.Content);
        return result;
    }

    internal XmlName? CopyXsltCollectedName(XmlName? source)
    {
        if (source == null) return null;
        XmlName result = new XmlName();
        result.Prefix = source.Prefix;
        result.LocalName = source.LocalName;
        result.NamespaceUri = source.NamespaceUri;
        result.QualifiedName = source.QualifiedName;
        return result;
    }

    internal SourceRange? CopyXsltCollectedRange(SourceRange? source)
    {
        if (source == null) return null;
        SourceRange result = new SourceRange();
        result.Start = CopyXsltCollectedLocation(source.Start);
        result.End = CopyXsltCollectedLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXsltCollectedLocation(SourceLocation? source)
    {
        if (source == null) return null;
        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
