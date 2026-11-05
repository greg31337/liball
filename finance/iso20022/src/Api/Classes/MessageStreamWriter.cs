using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

/// <summary>Streams messages into business-file or caller-defined envelopes with explicit totals.</summary>
public class MessageStreamWriter : IMessageStreamWriter, IDisposable
{
    const int CountFieldWidth = 18;
    const int ControlTotalFieldWidth = 72;
    Stream? output;
    MessageEnvelope? envelopeHeader;
    StreamWriteOptions? options;
    XmlWriter? writer;
    MessageWriteResult? initialFailure;
    MessageWriteResult? completionResult;
    QualifiedNameData wrapperName = new QualifiedNameData();
    long outputStartPosition;
    long countPosition;
    long controlTotalPosition;
    long messageCount;
    decimal controlTotal;
    int controlTotalScale;
    bool initialized;
    bool hasCountPosition;
    bool hasControlTotalPosition;
    bool disposed;

    /// <summary>Initializes a stream writer over one caller-owned output and optional envelope header.</summary>
    /// <param name="output">The writable output, retained open after completion or disposal.</param>
    /// <param name="envelopeHeader">Generic envelope header content written before totals and messages.</param>
    /// <param name="options">The wrapper, message, and total-finalization options.</param>
    public MessageStreamWriter(
        Stream? output,
        MessageEnvelope? envelopeHeader,
        StreamWriteOptions? options)
    {
        this.output = output;
        this.envelopeHeader = envelopeHeader;
        this.options = options;
        if (output == null)
        {
            initialFailure = CreateFailure(
                "stream_writer.output_null",
                "A message sequence output stream is required.");
            return;
        }

        if (options == null)
        {
            initialFailure = CreateFailure(
                "stream_writer.options_null",
                "Message stream write options are required.");
            return;
        }

        if (!output.CanWrite)
        {
            initialFailure = CreateFailure(
                "stream_writer.output_not_writable",
                "The message sequence output stream is not writable.");
            return;
        }

        initialFailure = ValidateOptions(options, output);
    }

    /// <summary>Writes the next message without retaining its model.</summary>
    /// <param name="document">The next generic message document.</param>
    /// <returns>The outcome and number of bytes emitted by this write.</returns>
    public MessageWriteResult WriteNextMessage(MessageDocument? document)
    {
        MessageWriteResult? stateFailure = PrepareForWrite();
        if (stateFailure != null)
        {
            return stateFailure;
        }

        if (document == null)
        {
            return CreateFailure(
                "stream_writer.document_null",
                "A message document is required.");
        }

        if (completionResult != null)
        {
            return CreateFailure(
                "stream_writer.completed",
                "The message sequence has already been completed.");
        }

        if (options == null || writer == null)
        {
            return CreateFailure(
                "stream_writer.state_invalid",
                "The message sequence writer is not initialized.");
        }

        if (messageCount >= options.MessageOptions.Limits.MaxCollectionCount)
        {
            return CreateFailure(
                "stream_writer.item_limit",
                "The message sequence exceeds MaxCollectionCount.");
        }

        if (!options.UseDeferredTotals &&
            options.HasPrecomputedMessageCount &&
            messageCount >= options.PrecomputedMessageCount)
        {
            return CreateFailure(
                "stream_writer.message_count_exceeded",
                "More messages were supplied than the precomputed message count.");
        }

        DynamicXmlNode? tree = XmlMessageWriter.PrepareTree(document, options.MessageOptions);
        if (tree == null)
        {
            return CreateFailure(
                "stream_writer.content_missing",
                "The message document does not contain generic XML content.");
        }

        XmlMessageWriter.ValidateTreeLimits(tree, options.MessageOptions.Limits);
        IsoDecimalValue? amount = ResolvePrimaryAmount(document);
        if (amount != null && (amount.Scale < 0 || amount.Scale > 28 ||
            DecimalLexical.FormatDecimal(amount) == null))
        {
            return CreateFailure(
                "stream_writer.control_amount_invalid",
                "A primary amount cannot contribute to the control total.");
        }

        long startPosition = CaptureOutputPosition();
        if (options.EnvelopeKind == EnvelopeKind.BusinessFileEnvelope)
        {
            writer.WriteStartElement(
                wrapperName.Prefix,
                "Pyld",
                wrapperName.NamespaceUri);
            writer.WriteStartElement(
                wrapperName.Prefix,
                "BizCntt",
                wrapperName.NamespaceUri);
            if (document.Layout == EnvelopeKind.SiblingHeaderAndDocument)
            {
                writer.WriteStartElement(
                    string.Empty,
                    "BizMsgEnvlp",
                    Iso20022Namespaces.SchemaUrnPrefix + "nvlp.001.001.02");
            }
        }

        new GenericXmlNodeWriter(CreateItemOptions()).WriteNode(
            writer,
            tree,
            new NamespaceScopeTracker());
        if (options.EnvelopeKind == EnvelopeKind.BusinessFileEnvelope)
        {
            if (document.Layout == EnvelopeKind.SiblingHeaderAndDocument)
            {
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.Flush();
        messageCount++;
        if (amount != null)
        {
            controlTotal = controlTotal + amount.Value;
            if (amount.Scale > controlTotalScale)
            {
                controlTotalScale = amount.Scale;
            }
        }

        long byteCount = BytesSince(startPosition);
        EnsureTotalByteLimit();
        return CreateSuccess(byteCount);
    }

    /// <summary>Finalizes the wrapper and verifies or patches its count and control total.</summary>
    /// <returns>The finalization outcome and complete output byte count when known.</returns>
    public MessageWriteResult CompleteWriting()
    {
        if (completionResult != null)
        {
            return completionResult;
        }

        MessageWriteResult? stateFailure = PrepareForWrite();
        if (stateFailure != null)
        {
            completionResult = stateFailure;
            return completionResult;
        }

        if (writer == null || output == null || options == null)
        {
            completionResult = CreateFailure(
                "stream_writer.state_invalid",
                "The message sequence writer is not initialized.");
            return completionResult;
        }

        writer.WriteEndElement();
        if (options.MessageOptions.WriteXmlDeclaration)
        {
            writer.WriteEndDocument();
        }

        writer.Flush();
        long completedPosition = CaptureOutputPosition();
        if (options.UseDeferredTotals)
        {
            PatchDeferredTotals(completedPosition);
        }

        writer.Dispose();
        writer = null;
        EnsureTotalByteLimit();
        string errorCode = ValidateActualTotals();
        if (errorCode.Length > 0)
        {
            completionResult = CreateFailure(errorCode, TotalFailureMessage(errorCode));
            completionResult.BytesWritten = CompleteByteCount();
            return completionResult;
        }

        completionResult = CreateSuccess(CompleteByteCount());
        return completionResult;
    }

    /// <summary>Finalizes pending XML when possible and retains the caller-owned output stream.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (initialized && completionResult == null)
        {
            CompleteWriting();
        }

        disposed = true;
        if (writer != null)
        {
            writer.Dispose();
            writer = null;
        }
    }

    internal MessageWriteResult? PrepareForWrite()
    {
        if (initialFailure != null)
        {
            MessageWriteResult failure = initialFailure;
            initialFailure = null;
            return failure;
        }

        if (disposed)
        {
            return CreateFailure(
                "stream_writer.disposed",
                "The message sequence writer has been disposed.");
        }

        if (!initialized)
        {
            InitializeOutput();
        }

        return null;
    }

    internal void InitializeOutput()
    {
        initialized = true;
        if (output == null || options == null)
        {
            return;
        }

        if (output.CanSeek)
        {
            outputStartPosition = output.Position;
        }

        ResolveWrapperName();
        XmlWriterSettings settings = new GenericXmlNodeWriter(
            options.MessageOptions).CreateSettings(options.MessageOptions.WriteByteOrderMark);
        settings.CloseOutput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        writer = XmlWriter.Create(output, settings);
        if (options.MessageOptions.WriteXmlDeclaration)
        {
            writer.WriteStartDocument();
        }

        writer.WriteStartElement(
            wrapperName.Prefix,
            wrapperName.LocalName,
            wrapperName.NamespaceUri);
        WriteEnvelopeDeclarations();
        WriteHeaderContent();
        WriteTotals();
        writer.Flush();
        EnsureTotalByteLimit();
    }

    internal void ResolveWrapperName()
    {
        if (options == null)
        {
            return;
        }

        if (options.EnvelopeKind == EnvelopeKind.BusinessFileEnvelope)
        {
            wrapperName.LocalName = "BizFileEnvlp";
            wrapperName.NamespaceUri = Iso20022Namespaces.SchemaUrnPrefix + "nvlp.002.001.02";
            if (envelopeHeader != null && envelopeHeader.Name.Prefix.Length > 0)
            {
                wrapperName.Prefix = envelopeHeader.Name.Prefix;
            }

            return;
        }

        if (options.CustomWrapperName != null)
        {
            wrapperName = XmlPathTracker.CopyName(options.CustomWrapperName);
        }
    }

    internal void WriteEnvelopeDeclarations()
    {
        if (writer == null || envelopeHeader == null)
        {
            return;
        }

        int index = 0;
        while (index < envelopeHeader.NamespaceDeclarations.Count)
        {
            NamespaceDeclarationData declaration = envelopeHeader.NamespaceDeclarations[index];
            if (declaration.Prefix != wrapperName.Prefix)
            {
                if (declaration.Prefix.Length == 0)
                {
                    writer.WriteAttributeString(
                        string.Empty,
                        "xmlns",
                        Iso20022Namespaces.Xmlns,
                        declaration.NamespaceUri);
                }
                else
                {
                    writer.WriteAttributeString(
                        "xmlns",
                        declaration.Prefix,
                        Iso20022Namespaces.Xmlns,
                        declaration.NamespaceUri);
                }
            }

            index++;
        }
    }

    internal void WriteHeaderContent()
    {
        if (writer == null || envelopeHeader == null || options == null)
        {
            return;
        }

        DynamicXmlNode? content = envelopeHeader.GenericContent;
        if (content == null && envelopeHeader.RawContent != null)
        {
            content = RawXmlFragmentMaterializer.MaterializeDocument(
                envelopeHeader.RawContent,
                options.MessageOptions);
        }

        DynamicXmlNode headerTree = new DynamicXmlNode();
        headerTree.Kind = XmlNodeKind.Document;
        if (content != null && content.Kind == XmlNodeKind.Element &&
            content.Name.LocalName == wrapperName.LocalName &&
            content.Name.NamespaceUri == wrapperName.NamespaceUri)
        {
            int childIndex = 0;
            while (childIndex < content.Children.Count)
            {
                headerTree.Children.Add(content.Children[childIndex]);
                childIndex++;
            }
        }
        else if (content != null)
        {
            XmlMessageWriter.AddContentNode(headerTree, content);
        }

        if (envelopeHeader.Header != null)
        {
            XmlMessageWriter.AddHeaderContentNode(
                headerTree,
                envelopeHeader.Header,
                options.MessageOptions);
        }

        if (headerTree.Children.Count > 0)
        {
            XmlMessageWriter.ValidateTreeLimits(
                headerTree,
                options.MessageOptions.Limits);
            new GenericXmlNodeWriter(CreateItemOptions()).WriteNode(
                writer,
                headerTree,
                new NamespaceScopeTracker());
        }
    }

    internal void WriteTotals()
    {
        if (writer == null || options == null)
        {
            return;
        }

        if (options.EnvelopeKind == EnvelopeKind.BusinessFileEnvelope)
        {
            if (options.UseDeferredTotals || options.HasPrecomputedMessageCount)
            {
                writer.WriteStartElement(
                    wrapperName.Prefix,
                    "ApplSpcfcs",
                    wrapperName.NamespaceUri);
                WriteCountElement("TtlNbOfBizMsgs");
                writer.WriteEndElement();
            }

            return;
        }

        if (options.UseDeferredTotals || options.HasPrecomputedMessageCount ||
            options.HasPrecomputedControlTotal)
        {
            writer.WriteStartElement(
                wrapperName.Prefix,
                "StreamTotals",
                wrapperName.NamespaceUri);
            if (options.UseDeferredTotals || options.HasPrecomputedMessageCount)
            {
                WriteCountElement("MessageCount");
            }

            if (options.UseDeferredTotals || options.HasPrecomputedControlTotal)
            {
                WriteControlTotalElement();
            }

            writer.WriteEndElement();
        }
    }

    internal void WriteCountElement(string localName)
    {
        if (writer == null || options == null || output == null)
        {
            return;
        }

        writer.WriteStartElement(wrapperName.Prefix, localName, wrapperName.NamespaceUri);
        if (options.UseDeferredTotals)
        {
            writer.WriteString(new string('0', CountFieldWidth));
            writer.Flush();
            countPosition = output.Position - CountFieldWidth;
            hasCountPosition = true;
        }
        else
        {
            writer.WriteString(IntegerLexical.FormatInt32(options.PrecomputedMessageCount));
        }

        writer.WriteEndElement();
    }

    internal void WriteControlTotalElement()
    {
        if (writer == null || options == null || output == null)
        {
            return;
        }

        writer.WriteStartElement(
            wrapperName.Prefix,
            "ControlTotal",
            wrapperName.NamespaceUri);
        if (options.UseDeferredTotals)
        {
            writer.WriteString(new string('0', ControlTotalFieldWidth));
            writer.Flush();
            controlTotalPosition = output.Position - ControlTotalFieldWidth;
            hasControlTotalPosition = true;
        }
        else
        {
            string? value = DecimalLexical.FormatDecimal(options.PrecomputedControlTotal);
            if (value != null)
            {
                writer.WriteString(value);
            }
        }

        writer.WriteEndElement();
    }

    internal void PatchDeferredTotals(long completedPosition)
    {
        if (output == null)
        {
            return;
        }

        if (hasCountPosition)
        {
            output.Position = countPosition;
            WritePatch(FormatFixedCount(messageCount));
        }

        if (hasControlTotalPosition)
        {
            output.Position = controlTotalPosition;
            WritePatch(FormatFixedControlTotal());
        }

        output.Position = completedPosition;
    }

    internal void WritePatch(string text)
    {
        if (output == null)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        output.Write(bytes, 0, bytes.Length);
        output.Flush();
    }

    internal string FormatFixedControlTotal()
    {
        IsoDecimalValue value = new IsoDecimalValue();
        value.Value = controlTotal;
        value.Scale = controlTotalScale;
        string? lexical = DecimalLexical.FormatDecimal(value);
        if (lexical == null)
        {
            return new string('0', ControlTotalFieldWidth);
        }

        if (lexical[0] == '-')
        {
            return "-" + lexical.Substring(1).PadLeft(
                ControlTotalFieldWidth - 1,
                '0');
        }

        return lexical.PadLeft(ControlTotalFieldWidth, '0');
    }

    internal static string FormatFixedCount(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture).PadLeft(
            CountFieldWidth,
            '0');
    }

    internal IsoDecimalValue? ResolvePrimaryAmount(MessageDocument document)
    {
        if (document.Metadata.PrimaryAmount == null)
        {
            return null;
        }

        return document.Metadata.PrimaryAmount.Amount;
    }

    internal string ValidateActualTotals()
    {
        if (options == null || options.UseDeferredTotals)
        {
            return string.Empty;
        }

        if (options.HasPrecomputedMessageCount &&
            messageCount != options.PrecomputedMessageCount)
        {
            return "stream_writer.message_count_mismatch";
        }

        if (options.HasPrecomputedControlTotal &&
            controlTotal != options.PrecomputedControlTotal.Value)
        {
            return "stream_writer.control_total_mismatch";
        }

        return string.Empty;
    }

    internal static string TotalFailureMessage(string code)
    {
        if (code == "stream_writer.message_count_mismatch")
        {
            return "The written message count differs from the precomputed count.";
        }

        return "The written primary-amount sum differs from the precomputed control total.";
    }

    internal MessageWriteOptions CreateItemOptions()
    {
        MessageWriteOptions itemOptions = XmlMessageWriter.CreateEffectiveOptions(
            options == null ? new MessageWriteOptions() : options.MessageOptions);
        itemOptions.WriteXmlDeclaration = false;
        itemOptions.WriteByteOrderMark = false;
        return itemOptions;
    }

    internal long CaptureOutputPosition()
    {
        if (output != null && output.CanSeek)
        {
            return output.Position;
        }

        return -1;
    }

    internal long BytesSince(long position)
    {
        long current = CaptureOutputPosition();
        if (position >= 0 && current >= position)
        {
            return current - position;
        }

        return 0;
    }

    internal long CompleteByteCount()
    {
        long current = CaptureOutputPosition();
        if (current >= outputStartPosition)
        {
            return current - outputStartPosition;
        }

        return 0;
    }

    internal void EnsureTotalByteLimit()
    {
        if (options != null && output != null && output.CanSeek)
        {
            new ResourceLimitGuard(options.MessageOptions.Limits).EnsureInputByteCount(
                CompleteByteCount());
        }
    }

    internal static MessageWriteResult? ValidateOptions(
        StreamWriteOptions options,
        Stream output)
    {
        if (options.MessageOptions == null || options.MessageOptions.Limits == null)
        {
            return CreateFailure(
                "stream_writer.message_options_invalid",
                "Message and resource-limit options are required.");
        }

        if (options.EnvelopeKind != EnvelopeKind.BusinessFileEnvelope &&
            options.EnvelopeKind != EnvelopeKind.CustomWrapper)
        {
            return CreateFailure(
                "stream_writer.envelope_kind_invalid",
                "Only business-file and custom sequence wrappers are supported.");
        }

        if (options.EnvelopeKind == EnvelopeKind.CustomWrapper &&
            !IsValidCustomWrapperName(options.CustomWrapperName))
        {
            return CreateFailure(
                "stream_writer.wrapper_name_invalid",
                "A valid qualified custom wrapper name is required.");
        }

        if (options.UseDeferredTotals && !output.CanSeek)
        {
            return CreateFailure(
                "stream_writer.deferred_seek_required",
                "Deferred totals require a seekable output stream.");
        }

        if (options.HasPrecomputedMessageCount && options.PrecomputedMessageCount < 0)
        {
            return CreateFailure(
                "stream_writer.message_count_invalid",
                "A precomputed message count cannot be negative.");
        }

        if (options.HasPrecomputedControlTotal &&
            (options.PrecomputedControlTotal == null ||
                DecimalLexical.FormatDecimal(options.PrecomputedControlTotal) == null))
        {
            return CreateFailure(
                "stream_writer.control_total_invalid",
                "The precomputed control total is not a valid ISO decimal.");
        }

        return null;
    }

    internal static bool IsValidCustomWrapperName(QualifiedNameData? name)
    {
        if (name == null || !XmlNameLexical.IsNcName(name.LocalName.AsSpan()))
        {
            return false;
        }

        if (name.Prefix.Length > 0 &&
            (!XmlNameLexical.IsNcName(name.Prefix.AsSpan()) ||
                name.NamespaceUri.Length == 0))
        {
            return false;
        }

        return name.Prefix != "xml" && name.Prefix != "xmlns";
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
}
