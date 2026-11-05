using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XmlOutputOperation
{
    internal bool Succeeded;
    internal string? ErrorMessage;

    internal XmlOutputOperation(bool succeeded, string? errorMessage)
    {
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }
}

internal class XmlOutputEncodingOperation
{
    internal byte[]? Bytes;
    internal string? ErrorMessage;

    internal XmlOutputEncodingOperation(byte[]? bytes, string? errorMessage)
    {
        Bytes = bytes;
        ErrorMessage = errorMessage;
    }
}

internal class XmlOutputWriter
{
    internal XmlOutput Output;
    internal Encoding OutputEncoding;
    internal string EffectiveEncodingName;
    internal bool EmitByteOrderMark;
    internal int MaximumByteBlockSize;
    internal bool Started;
    internal bool Completed;
    internal bool Failed;
    internal string PendingHighSurrogate;

    internal XmlOutputWriter(XmlOutput output, string? requestedEncoding, bool emitByteOrderMark, int maximumByteBlockSize)
    {
        Guard.NotNull(output, "output");
        Guard.Positive(maximumByteBlockSize, "maximumByteBlockSize");
        ValidateXmlOutputDestination(output);
        Output = output;
        OutputEncoding = ResolveXmlOutputEncoding(output, requestedEncoding);
        EffectiveEncodingName = output.Kind == XmlOutputKind.TextWriter && output.TextWriter != null ? output.TextWriter.Encoding.WebName : OutputEncoding.WebName;
        EmitByteOrderMark = emitByteOrderMark && output.Kind != XmlOutputKind.TextWriter;
        MaximumByteBlockSize = maximumByteBlockSize;
        Started = false;
        Completed = false;
        Failed = false;
        PendingHighSurrogate = "";
    }

    internal void WriteXmlOutputText(string value)
    {
        Guard.NotNull(value, "value");
        EnsureXmlOutputOpen();
        StartXmlOutput();
        if (Output.Kind == XmlOutputKind.TextWriter)
        {
            TextWriter? writer = Output.TextWriter;
            if (writer == null)
            {
                FailXmlOutput();
                throw CreateXmlOutputError("The XML text-writer destination is unavailable.");
            }

            XmlOutputOperation written = RunXmlOutputOperation(() => writer.Write(value));
            if (!written.Succeeded)
            {
                FailXmlOutput();
                throw CreateXmlOutputError(written.ErrorMessage == null ? "The XML text writer rejected output." : written.ErrorMessage);
            }

            return;
        }

        string combined = PendingHighSurrogate.Length == 0 ? value : PendingHighSurrogate + value;
        PendingHighSurrogate = "";
        if (combined.Length > 0 && char.IsHighSurrogate(combined[combined.Length - 1]))
        {
            PendingHighSurrogate = combined.Substring(combined.Length - 1, 1);
            combined = combined.Substring(0, combined.Length - 1);
        }

        if (combined.Length > 0)
        {
            WriteEncodedXmlOutputText(combined);
        }
    }

    internal void FlushXmlOutput()
    {
        EnsureXmlOutputOpen();
        StartXmlOutput();
        XmlOutputOperation flushed;
        if (Output.Kind == XmlOutputKind.Stream && Output.ByteStream != null)
        {
            Stream stream = Output.ByteStream;
            flushed = RunXmlOutputOperation(() => stream.Flush());
        }
        else if (Output.Kind == XmlOutputKind.TextWriter && Output.TextWriter != null)
        {
            TextWriter writer = Output.TextWriter;
            flushed = RunXmlOutputOperation(() => writer.Flush());
        }
        else
        {
            flushed = new XmlOutputOperation(true, null);
        }

        if (!flushed.Succeeded)
        {
            FailXmlOutput();
            throw CreateXmlOutputError(flushed.ErrorMessage == null ? "The XML output could not be flushed." : flushed.ErrorMessage);
        }
    }

    internal void CompleteXmlOutput()
    {
        EnsureXmlOutputOpen();
        if (Output.Kind != XmlOutputKind.TextWriter && PendingHighSurrogate.Length > 0)
        {
            FailXmlOutput();
            throw CreateXmlOutputError("The XML output ends with an unpaired high surrogate.");
        }

        FlushXmlOutput();
        Completed = true;
        CloseOwnedXmlOutput();
    }

    internal void FailXmlOutput()
    {
        if (Completed || Failed)
        {
            return;
        }

        Failed = true;
        PendingHighSurrogate = "";
        CloseOwnedXmlOutput();
    }

    internal string InspectXmlOutputEncodingName()
    {
        return EffectiveEncodingName;
    }

    internal bool InspectXmlOutputCompletion()
    {
        return Completed;
    }

    internal bool InspectXmlOutputFailure()
    {
        return Failed;
    }

    internal void StartXmlOutput()
    {
        if (Started)
        {
            return;
        }

        Started = true;
        if (!EmitByteOrderMark)
        {
            return;
        }

        byte[] preamble = CreateXmlOutputPreamble(OutputEncoding);
        if (preamble.Length > 0)
        {
            WriteRawXmlOutputBytes(preamble);
        }
    }

    internal void WriteEncodedXmlOutputText(string value)
    {
        XmlOutputEncodingOperation encoded = RunXmlOutputEncoding(() => OutputEncoding.GetBytes(value));
        byte[]? optionalBytes = encoded.Bytes;
        if (optionalBytes == null)
        {
            FailXmlOutput();
            throw CreateXmlEncodingOutputError(encoded.ErrorMessage == null ? "The XML output contains a character that cannot be encoded." : encoded.ErrorMessage);
        }

        WriteRawXmlOutputBytes(optionalBytes);
    }

    internal void WriteRawXmlOutputBytes(byte[] bytes)
    {
        int offset = 0;
        while (offset < bytes.Length)
        {
            int remaining = bytes.Length - offset;
            int count = remaining < MaximumByteBlockSize ? remaining : MaximumByteBlockSize;
            XmlOutputOperation written;
            if (Output.Kind == XmlOutputKind.Stream && Output.ByteStream != null)
            {
                Stream stream = Output.ByteStream;
                int writeOffset = offset;
                int writeCount = count;
                written = RunXmlOutputOperation(() => stream.Write(bytes, writeOffset, writeCount));
            }
            else if (Output.Kind == XmlOutputKind.ByteSink && Output.ByteSink != null)
            {
                IXmlByteSink sink = Output.ByteSink;
                ReadOnlyMemory<byte> block = new ReadOnlyMemory<byte>(bytes, offset, count);
                written = RunXmlOutputOperation(() => sink.WriteXmlBytes(block));
            }
            else
            {
                FailXmlOutput();
                throw CreateXmlOutputError("The XML byte destination is unavailable.");
            }

            if (!written.Succeeded)
            {
                FailXmlOutput();
                throw CreateXmlOutputError(written.ErrorMessage == null ? "The XML byte destination rejected output." : written.ErrorMessage);
            }

            offset = offset + count;
        }
    }

    internal void CloseOwnedXmlOutput()
    {
        if (!Output.OwnsResource)
        {
            return;
        }

        XmlOutputOperation closed;
        if (Output.Kind == XmlOutputKind.Stream && Output.ByteStream != null)
        {
            Stream stream = Output.ByteStream;
            closed = RunXmlOutputOperation(() => stream.Dispose());
        }
        else if (Output.Kind == XmlOutputKind.TextWriter && Output.TextWriter != null)
        {
            TextWriter writer = Output.TextWriter;
            closed = RunXmlOutputOperation(() => writer.Dispose());
        }
        else
        {
            closed = new XmlOutputOperation(true, null);
        }

        if (!closed.Succeeded && !Failed)
        {
            Failed = true;
            Completed = false;
            throw CreateXmlOutputError(closed.ErrorMessage == null ? "The owned XML output could not be closed." : closed.ErrorMessage);
        }
    }

    internal void EnsureXmlOutputOpen()
    {
        if (Completed || Failed)
        {
            throw new ObjectDisposedException("XmlOutputWriter");
        }
    }

    internal void ValidateXmlOutputDestination(XmlOutput output)
    {
        if (output.Kind == XmlOutputKind.Stream)
        {
            if (output.ByteStream == null || !output.ByteStream.CanWrite)
            {
                throw new ArgumentException("A stream output requires a writable ByteStream.", "output");
            }

            return;
        }

        if (output.Kind == XmlOutputKind.TextWriter)
        {
            if (output.TextWriter == null)
            {
                throw new ArgumentException("A text-writer output requires TextWriter.", "output");
            }

            return;
        }

        if (output.Kind == XmlOutputKind.ByteSink)
        {
            if (output.ByteSink == null)
            {
                throw new ArgumentException("A byte-sink output requires ByteSink.", "output");
            }

            return;
        }

        throw new ArgumentOutOfRangeException("output.Kind");
    }

    internal Encoding ResolveXmlOutputEncoding(XmlOutput output, string? requestedEncoding)
    {
        if (output.Kind == XmlOutputKind.TextWriter && output.TextWriter != null)
        {
            return EncodingDetector.CreateStrictBuiltInEncoding(output.TextWriter.Encoding, EncodingPolicyKind.BuiltInText);
        }

        if (output.Encoding != null)
        {
            return EncodingDetector.CreateStrictBuiltInEncoding(output.Encoding, EncodingPolicyKind.BuiltInText);
        }

        if (output.EncodingName != null)
        {
            return EncodingDetector.ResolveXmlEncodingName(output.EncodingName, EncodingPolicyKind.BuiltInText);
        }

        if (requestedEncoding != null)
        {
            return EncodingDetector.ResolveXmlEncodingName(requestedEncoding, EncodingPolicyKind.BuiltInText);
        }

        return new UTF8Encoding(false, true);
    }

    internal byte[] CreateXmlOutputPreamble(Encoding encoding)
    {
        int codePage = encoding.CodePage;
        if (codePage == 65001)
        {
            return [0xEF, 0xBB, 0xBF];
        }

        if (codePage == 1200)
        {
            return [0xFF, 0xFE];
        }

        if (codePage == 1201)
        {
            return [0xFE, 0xFF];
        }

        if (codePage == 12000)
        {
            return [0xFF, 0xFE, 0x00, 0x00];
        }

        if (codePage == 12001)
        {
            return [0x00, 0x00, 0xFE, 0xFF];
        }

        return [];
    }

    internal XmlOutputOperation RunXmlOutputOperation(Action action)
    {
        Task pending = Task.Run(action);
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return new XmlOutputOperation(true, null);
        }

        AggregateException? aggregate = pending.Exception;
        string? message = aggregate == null || aggregate.InnerException == null ? null : aggregate.InnerException.Message;
        return new XmlOutputOperation(false, message);
    }

    internal XmlOutputEncodingOperation RunXmlOutputEncoding(Func<byte[]> action)
    {
        Task<byte[]> pending = Task.Run(action);
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion)
        {
            return new XmlOutputEncodingOperation(pending.Result, null);
        }

        AggregateException? aggregate = pending.Exception;
        string? message = aggregate == null || aggregate.InnerException == null ? null : aggregate.InnerException.Message;
        return new XmlOutputEncodingOperation(null, message);
    }

    internal XmlSerializationException CreateXmlOutputError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.GeneralFailure;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }

    internal XmlSerializationException CreateXmlEncodingOutputError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.SERE0008;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
