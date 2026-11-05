using System;

namespace html;

internal static class HtmlTreeClone
{
    internal static HtmlNode Clone(HtmlNode source, HtmlDocument ownerDocument,
        bool deep)
    {
        if (source.GetKindStorage() == HtmlNodeKind.Document)
        {
            return CloneDocument(source, deep);
        }

        HtmlNode result = CloneSingle(source, ownerDocument);
        if (!deep)
        {
            return result;
        }

        HtmlNode? sourceContainer = GetCloneContainer(source);
        HtmlNode? resultContainer = GetCloneContainer(result);
        if (sourceContainer == null || resultContainer == null)
        {
            return result;
        }

        HtmlNode[] sourceChildren = sourceContainer.GetChildrenStorage();
        HtmlNode[] resultChildren = new HtmlNode[sourceChildren.Length];
        int index = 0;
        while (index < sourceChildren.Length)
        {
            resultChildren[index] = Clone(sourceChildren[index], ownerDocument,
                true);
            index = index + 1;
        }

        resultContainer.InitializeChildrenStorage(resultChildren);
        return result;
    }

    internal static HtmlNode CloneDocument(HtmlNode source, bool deep)
    {
        HtmlDocument sourceDocument = source.GetOwnerDocumentStorage();
        HtmlDocument resultDocument = new HtmlDocument(
            sourceDocument.GetUrlStorage(), sourceDocument.GetBaseUrlStorage(),
            sourceDocument.GetModeStorage(),
            sourceDocument.GetDiagnosticsStorage(),
            source.GetSourceSpanStorage());
        resultDocument.SetContentLanguageStorage(
            sourceDocument.GetContentLanguage());
        resultDocument.SetEncodingMetadataStorage(
            sourceDocument.GetDetectedEncoding(),
            sourceDocument.GetDetectedEncodingLabel(),
            sourceDocument.GetEncodingDetection());
        HtmlNode result = resultDocument.GetRootStorage();
        result.SetSourceBackedStorage(source.IsSourceBackedStorage());
        if (!deep)
        {
            return result;
        }

        HtmlNode[] sourceChildren = source.GetChildrenStorage();
        HtmlNode[] resultChildren = new HtmlNode[sourceChildren.Length];
        int index = 0;
        while (index < sourceChildren.Length)
        {
            resultChildren[index] = Clone(sourceChildren[index],
                resultDocument, true);
            index = index + 1;
        }

        result.InitializeChildrenStorage(resultChildren);
        return result;
    }

    internal static HtmlNode CloneSingle(HtmlNode source, HtmlDocument ownerDocument)
    {
        HtmlSourceSpan span = source.GetSourceSpanStorage();
        HtmlNode result;
        switch (source.GetKindStorage())
        {
            case HtmlNodeKind.Document:
                result = HtmlNode.CreateDocumentStorage(ownerDocument, span);
                break;
            case HtmlNodeKind.Fragment:
                result = HtmlNode.CreateFragmentStorage(ownerDocument, span);
                break;
            case HtmlNodeKind.DocumentType:
                result = HtmlNode.CreateDocumentTypeStorage(ownerDocument,
                    source.HasDoctypeNameStorage(), source.GetNameStorage(),
                    source.HasPublicIdentifierStorage(),
                    source.GetPublicIdentifierStorage(),
                    source.HasSystemIdentifierStorage(),
                    source.GetSystemIdentifierStorage(),
                    source.IsForceQuirksStorage(), span);
                break;
            case HtmlNodeKind.Element:
                result = HtmlNode.CreateElementStorage(ownerDocument,
                    source.GetNamespaceStorage(),
                    source.GetNamespaceUriStorage(), source.GetNameStorage(),
                    source.GetLocalNameStorage(), source.GetPrefixStorage(),
                    CloneAttributes(source.GetAttributesStorage()),
                    source.GetTemplateContentStorage() != null, span);
                break;
            case HtmlNodeKind.Text:
                result = HtmlNode.CreateTextStorage(ownerDocument,
                    source.GetDataStorage(), span);
                break;
            case HtmlNodeKind.Comment:
                result = HtmlNode.CreateCommentStorage(ownerDocument,
                    source.GetDataStorage(), span);
                break;
            default:
                throw new InvalidOperationException("Unknown stored node kind.");
        }

        result.SetSourceBackedStorage(source.IsSourceBackedStorage());
        return result;
    }

    internal static HtmlNode? GetCloneContainer(HtmlNode node)
    {
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind != HtmlNodeKind.Document
            && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            return null;
        }

        return HtmlTreeMutation.GetMutationTarget(node);
    }

    internal static HtmlAttribute[] CloneAttributes(HtmlAttribute[] attributes)
    {
        HtmlAttribute[] result = new HtmlAttribute[attributes.Length];
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute attribute = attributes[index];
            result[index] = HtmlAttribute.CreateNamespaced(
                attribute.GetName(), attribute.GetLocalName(),
                attribute.GetPrefix(), attribute.GetNamespaceUri(),
                attribute.GetValue(), attribute.GetSourceSpan());
            index = index + 1;
        }

        return result;
    }
}
