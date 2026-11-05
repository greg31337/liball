using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace iso20022;

/// <summary>Writes generic message documents in compact, indented, preserved, or canonical form.</summary>
public class XmlMessageWriter : IXmlMessageWriter
{
    /// <summary>Initializes an XML message writer.</summary>
    public XmlMessageWriter()
    {
    }

    /// <summary>Writes a generic message to a byte stream.</summary>
    /// <param name="document">The message document.</param><param name="output">The output stream.</param><param name="options">The write options.</param><returns>The write result.</returns>
    public MessageWriteResult WriteGenericStream(MessageDocument? document, Stream? output, MessageWriteOptions? options)
    {
        if (document == null || output == null || options == null)
        {
            return CreateInputFailure(document == null, output == null, options == null);
        }

        DynamicXmlNode? tree = PrepareTree(document, options);
        if (tree == null)
        {
            return CreateFailure("writer.content_missing", "The message document does not contain generic XML content.");
        }

        MessageWriteOptions effectiveOptions = CreateEffectiveOptions(options);
        ValidateTreeLimits(tree, effectiveOptions.Limits);
        byte[] bytes = new GenericXmlNodeWriter(effectiveOptions).WriteBytes(tree);
        new ResourceLimitGuard(effectiveOptions.Limits).EnsureInputByteCount(bytes.LongLength);
        output.Write(bytes, 0, bytes.Length);
        MessageWriteResult result = CreateSuccess(bytes.LongLength);
        return result;
    }

    /// <summary>Writes a generic message through a text writer.</summary>
    /// <param name="document">The message document.</param><param name="output">The text writer.</param><param name="options">The write options.</param><returns>The write result.</returns>
    public MessageWriteResult WriteGenericTextWriter(MessageDocument? document, TextWriter? output, MessageWriteOptions? options)
    {
        if (document == null || output == null || options == null)
        {
            return CreateInputFailure(document == null, output == null, options == null);
        }

        DynamicXmlNode? tree = PrepareTree(document, options);
        if (tree == null)
        {
            return CreateFailure("writer.content_missing", "The message document does not contain generic XML content.");
        }

        MessageWriteOptions effectiveOptions = CreateEffectiveOptions(options);
        ValidateTreeLimits(tree, effectiveOptions.Limits);
        string text = new GenericXmlNodeWriter(effectiveOptions).WriteText(tree);
        if (effectiveOptions.WriteXmlDeclaration)
        {
            text = text.Replace("encoding=\"utf-8\"", "encoding=\"" + output.Encoding.WebName + "\"", StringComparison.Ordinal);
        }

        int byteCount = output.Encoding.GetByteCount(text);
        new ResourceLimitGuard(effectiveOptions.Limits).EnsureInputByteCount(byteCount);
        output.Write(text);
        return CreateSuccess(byteCount);
    }

    /// <summary>Writes a generic message to a byte buffer writer.</summary>
    /// <param name="document">The message document.</param><param name="output">The byte buffer writer.</param><param name="options">The write options.</param><returns>The write result.</returns>
    public MessageWriteResult WriteGenericBuffer(MessageDocument? document, IBufferWriter<byte>? output, MessageWriteOptions? options)
    {
        if (document == null || output == null || options == null)
        {
            return CreateInputFailure(document == null, output == null, options == null);
        }

        DynamicXmlNode? tree = PrepareTree(document, options);
        if (tree == null)
        {
            return CreateFailure("writer.content_missing", "The message document does not contain generic XML content.");
        }

        MessageWriteOptions effectiveOptions = CreateEffectiveOptions(options);
        ValidateTreeLimits(tree, effectiveOptions.Limits);
        byte[] bytes = new GenericXmlNodeWriter(effectiveOptions).WriteBytes(tree);
        new ResourceLimitGuard(effectiveOptions.Limits).EnsureInputByteCount(bytes.LongLength);
        Memory<byte> destination = output.GetMemory(bytes.Length);
        bytes.AsSpan().CopyTo(destination.Span);
        output.Advance(bytes.Length);
        MessageWriteResult result = CreateSuccess(bytes.LongLength);
        result.Bytes = bytes;
        return result;
    }

    /// <summary>Writes a generic message to a returned string.</summary>
    /// <param name="document">The message document.</param><param name="options">The write options.</param><returns>The write result.</returns>
    public MessageWriteResult WriteGenericString(MessageDocument? document, MessageWriteOptions? options)
    {
        if (document == null || options == null)
        {
            return CreateInputFailure(document == null, false, options == null);
        }

        DynamicXmlNode? tree = PrepareTree(document, options);
        if (tree == null)
        {
            return CreateFailure("writer.content_missing", "The message document does not contain generic XML content.");
        }

        MessageWriteOptions effectiveOptions = CreateEffectiveOptions(options);
        ValidateTreeLimits(tree, effectiveOptions.Limits);
        string text = new GenericXmlNodeWriter(effectiveOptions).WriteText(tree);
        int byteCount = Encoding.UTF8.GetByteCount(text);
        new ResourceLimitGuard(effectiveOptions.Limits).EnsureInputByteCount(byteCount);
        MessageWriteResult result = CreateSuccess(byteCount);
        result.Text = text;
        return result;
    }

    internal static DynamicXmlNode? PrepareTree(MessageDocument document, MessageWriteOptions options)
    {
        DynamicXmlNode? source = BuildDocumentTree(document, options);
        if (source == null)
        {
            return null;
        }

        DynamicXmlNode tree = CanonicalXmlNodeNormalizer.CloneNode(source);
        ApplyDeclarationsToRoots(tree, document.NamespaceDeclarations);
        ApplyDeclarationsToRoots(tree, options.NamespaceDeclarations);
        if (options.FormattingMode == XmlFormattingMode.Canonical)
        {
            return CanonicalXmlNodeNormalizer.NormalizeDocument(tree);
        }

        return tree;
    }

    internal static DynamicXmlNode? BuildDocumentTree(MessageDocument document, MessageWriteOptions options)
    {
        DynamicXmlNode tree = new DynamicXmlNode();
        tree.Kind = XmlNodeKind.Document;
        if (document.Envelope != null && document.Envelope.GenericContent != null)
        {
            AddContentNode(tree, document.Envelope.GenericContent);
            return tree;
        }

        if (document.Envelope != null && document.Envelope.RawContent != null)
        {
            AddRawContentNode(tree, document.Envelope.RawContent, options);
            if (tree.Children.Count > 0)
            {
                return tree;
            }
        }

        if (document.Envelope != null && document.Envelope.Name.LocalName.Length > 0)
        {
            DynamicXmlNode envelope = new DynamicXmlNode();
            envelope.Kind = XmlNodeKind.Element;
            envelope.Name = XmlPathTracker.CopyName(document.Envelope.Name);
            envelope.NamespaceDeclarations = GenericXmlNodeWriter.CopyDeclarations(document.Envelope.NamespaceDeclarations);
            if (document.Header != null && document.Header.GenericContent != null)
            {
                AddContentNode(envelope, document.Header.GenericContent);
            }
            else if (document.Header != null && document.Header.RawContent != null)
            {
                AddRawContentNode(envelope, document.Header.RawContent, options);
            }

            int envelopePayloadIndex = 0;
            while (envelopePayloadIndex < document.Payloads.Count)
            {
                DynamicXmlNode? content = document.Payloads[envelopePayloadIndex].GenericContent;
                if (content != null)
                {
                    AddContentNode(envelope, content);
                }
                else
                {
                    RawXmlFragment? rawContent = document.Payloads[envelopePayloadIndex].RawContent;
                    if (rawContent != null)
                    {
                        AddRawContentNode(envelope, rawContent, options);
                    }
                }

                envelopePayloadIndex++;
            }

            tree.Children.Add(envelope);
            return tree;
        }

        if (document.Layout == EnvelopeKind.SiblingHeaderAndDocument && !document.HeaderPrecedesPayload)
        {
            AddPayloadContentNodes(tree, document.Payloads, options);
            AddHeaderContentNode(tree, document.Header, options);
        }
        else
        {
            AddHeaderContentNode(tree, document.Header, options);
            AddPayloadContentNodes(tree, document.Payloads, options);
        }

        if (tree.Children.Count == 0)
        {
            return null;
        }

        return tree;
    }

    internal static void AddHeaderContentNode(DynamicXmlNode parent, MessageHeader? header, MessageWriteOptions options)
    {
        if (header == null)
        {
            return;
        }

        if (header.GenericContent != null)
        {
            AddContentNode(parent, header.GenericContent);
        }
        else if (header.RawContent != null)
        {
            AddRawContentNode(parent, header.RawContent, options);
        }
    }

    internal static void AddPayloadContentNodes(DynamicXmlNode parent, List<MessagePayload> payloads, MessageWriteOptions options)
    {
        int payloadIndex = 0;
        while (payloadIndex < payloads.Count)
        {
            DynamicXmlNode? content = payloads[payloadIndex].GenericContent;
            if (content != null)
            {
                AddContentNode(parent, content);
            }
            else
            {
                RawXmlFragment? rawContent = payloads[payloadIndex].RawContent;
                if (rawContent != null)
                {
                    AddRawContentNode(parent, rawContent, options);
                }
            }

            payloadIndex++;
        }
    }

    internal static void AddRawContentNode(DynamicXmlNode parent, RawXmlFragment rawContent, MessageWriteOptions options)
    {
        if (options.UnknownContentMode == UnknownContentMode.Ignore)
        {
            return;
        }

        if (options.UnknownContentMode == UnknownContentMode.Reject)
        {
            throw new System.Xml.XmlException("The write options reject preserved raw XML content.");
        }

        DynamicXmlNode content = RawXmlFragmentMaterializer.MaterializeDocument(rawContent, options);
        AddContentNode(parent, content);
    }

    internal static void AddContentNode(DynamicXmlNode parent, DynamicXmlNode content)
    {
        if (content.Kind == XmlNodeKind.Document)
        {
            int index = 0;
            while (index < content.Children.Count)
            {
                parent.Children.Add(content.Children[index]);
                index++;
            }
        }
        else
        {
            parent.Children.Add(content);
        }
    }

    internal static void ApplyDeclarationsToRoots(DynamicXmlNode tree, List<NamespaceDeclarationData> declarations)
    {
        int childIndex = 0;
        while (childIndex < tree.Children.Count)
        {
            DynamicXmlNode child = tree.Children[childIndex];
            if (child.Kind == XmlNodeKind.Element)
            {
                int declarationIndex = 0;
                while (declarationIndex < declarations.Count)
                {
                    NamespaceDeclarationData declaration = declarations[declarationIndex];
                    if (!HasDeclarationPrefix(child.NamespaceDeclarations, declaration.Prefix))
                    {
                        child.NamespaceDeclarations.Add(NamespaceScopeTracker.CopyDeclaration(declaration));
                    }

                    declarationIndex++;
                }
            }

            childIndex++;
        }
    }

    internal static bool HasDeclarationPrefix(List<NamespaceDeclarationData> declarations, string prefix)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Prefix == prefix)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static MessageWriteOptions CreateEffectiveOptions(MessageWriteOptions source)
    {
        MessageWriteOptions options = new MessageWriteOptions();
        options.FormattingMode = source.FormattingMode;
        options.UnknownContentMode = source.UnknownContentMode;
        options.Limits = source.Limits;
        options.WriteXmlDeclaration = source.WriteXmlDeclaration;
        options.WriteByteOrderMark = source.WriteByteOrderMark;
        options.PreservePrefixes = source.PreservePrefixes;
        options.PreserveLexicalValues = source.PreserveLexicalValues;
        options.IndentText = source.IndentText;
        options.NewLineText = source.NewLineText;
        options.NamespaceDeclarations = source.NamespaceDeclarations;
        if (source.FormattingMode == XmlFormattingMode.Canonical)
        {
            options.FormattingMode = XmlFormattingMode.Compact;
            options.WriteXmlDeclaration = false;
            options.WriteByteOrderMark = false;
            options.PreservePrefixes = false;
            options.PreserveLexicalValues = false;
        }

        return options;
    }

    internal static void ValidateTreeLimits(DynamicXmlNode tree, ResourceLimits limits)
    {
        ResourceLimitGuard guard = new ResourceLimitGuard(limits);
        int index = 0;
        while (index < tree.Children.Count)
        {
            ValidateNodeLimits(tree.Children[index], guard, 1);
            index++;
        }
    }

    internal static void ValidateNodeLimits(DynamicXmlNode node, ResourceLimitGuard guard, int depth)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            guard.RecordElement(depth, node.Attributes.Count + node.NamespaceDeclarations.Count);
            guard.EnsureCollectionCount(node.Children.Count);
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                guard.EnsureTextLength(node.Attributes[attributeIndex].Value.Length);
                attributeIndex++;
            }
        }
        else
        {
            guard.EnsureTextLength(node.Value.Length);
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            ValidateNodeLimits(node.Children[childIndex], guard, depth + 1);
            childIndex++;
        }
    }

    internal static MessageWriteResult CreateSuccess(long byteCount)
    {
        MessageWriteResult result = new MessageWriteResult();
        result.Success = true;
        result.BytesWritten = byteCount;
        return result;
    }

    internal static MessageWriteResult CreateFailure(string code, string message)
    {
        MessageWriteResult result = new MessageWriteResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static MessageWriteResult CreateInputFailure(bool documentNull, bool outputNull, bool optionsNull)
    {
        if (documentNull)
        {
            return CreateFailure("writer.document_null", "A message document is required.");
        }

        if (outputNull)
        {
            return CreateFailure("writer.output_null", "A message output target is required.");
        }

        if (optionsNull)
        {
            return CreateFailure("writer.options_null", "Message write options are required.");
        }

        return CreateFailure("writer.input", "The message write input is invalid.");
    }
}
