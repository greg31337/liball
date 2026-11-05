using System;
using System.IO;

namespace avif;

/// <summary>Contains encoded AVIF or AV1 output from a completed operation.</summary>
public class AvifEncodeResult : IAvifEncodeResult, IAvifDiagnosticSource
{
    AvifResultCode code;
    byte[] data;

    /// <summary>Creates an encode result with an independent copy of caller-supplied bytes.</summary>
    /// <param name="code">The encode outcome.</param>
    /// <param name="data">The encoded bytes, or null for no output.</param>
    /// <remarks>This constructor is primarily useful for implementations of <see cref="IAvifCodec"/>.</remarks>
    public AvifEncodeResult(AvifResultCode code, byte[]? data)
    {
        this.code = code;
        this.data = code != AvifResultCode.Success || data == null
            ? System.Array.Empty<byte>() : (byte[])data.Clone();
    }

    /// <summary>Returns the encode outcome.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns an independent copy of the encoded AVIF bytes.</summary>
    /// <returns>The encoded bytes, or an empty array on failure.</returns>
    public byte[] GetData()
    {
        return (byte[])data.Clone();
    }

    /// <summary>Returns the encoded byte count.</summary>
    /// <returns>The output length, or zero on failure.</returns>
    public int GetDataLength()
    {
        return data.Length;
    }

    /// <summary>Copies encoded AVIF or AV1 bytes into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer, which may be larger than the output.</param>
    /// <returns>Success, BufferTooSmall, or the encode failure code.</returns>
    public AvifResultCode CopyDataTo(Span<byte> destination)
    {
        return code == AvifResultCode.Success
            ? CallerOutput.Copy(data, destination) : code;
    }

    /// <summary>Writes encoded AVIF or AV1 bytes without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidArgument, or the encode failure code.</returns>
    public AvifResultCode WriteDataTo(Stream destination)
    {
        return code == AvifResultCode.Success
            ? CallerOutput.Write(data, destination) : code;
    }

    /// <summary>Returns diagnostics produced while encoding or writing the output.</summary>
    /// <returns>The structured encoding diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Encoding,
            "The encode operation did not complete successfully.");
    }
}
