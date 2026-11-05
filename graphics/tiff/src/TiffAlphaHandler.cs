using System;

namespace tiff;

internal class TiffAlphaHandler
{
    internal static byte[] NormalizeRgba8(TiffImageInfo info, ReadOnlySpan<byte> rgba, TiffDiagnostics diagnostics)
    {
        if (!ValidateAlphaRequest(info, rgba.Length, diagnostics))
        {
            return new byte[0];
        }

        byte[] destination = TiffIfdEntry.CopyBytes(rgba);
        TiffExtraSample alphaKind = GetPrimaryExtraSample(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (alphaKind != TiffExtraSample.AssociatedAlpha)
        {
            return destination;
        }

        int offset = 0;
        while (offset < destination.Length)
        {
            byte alpha = destination[offset + 3];
            destination[offset] = UnassociateByteChannel(destination[offset], alpha);
            destination[offset + 1] = UnassociateByteChannel(destination[offset + 1], alpha);
            destination[offset + 2] = UnassociateByteChannel(destination[offset + 2], alpha);
            offset = offset + 4;
        }

        return destination;
    }

    internal static ushort[] NormalizeRgba16(TiffImageInfo info, ReadOnlySpan<ushort> rgba, TiffDiagnostics diagnostics)
    {
        if (!ValidateAlphaRequest(info, rgba.Length, diagnostics))
        {
            return new ushort[0];
        }

        ushort[] destination = CopyUShorts(rgba);
        TiffExtraSample alphaKind = GetPrimaryExtraSample(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        if (alphaKind != TiffExtraSample.AssociatedAlpha)
        {
            return destination;
        }

        int offset = 0;
        while (offset < destination.Length)
        {
            ushort alpha = destination[offset + 3];
            destination[offset] = UnassociateUShortChannel(destination[offset], alpha);
            destination[offset + 1] = UnassociateUShortChannel(destination[offset + 1], alpha);
            destination[offset + 2] = UnassociateUShortChannel(destination[offset + 2], alpha);
            offset = offset + 4;
        }

        return destination;
    }

    internal static bool ValidateAlphaRequest(TiffImageInfo info, int valueCount, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for alpha handling.");
            return false;
        }

        if (info.SamplesPerPixel < 4)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Alpha handling requires RGBA samples.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return false;
        }

        if (valueCount % 4 != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA alpha data must contain complete four-channel pixels.");
            return false;
        }

        return true;
    }

    internal static TiffExtraSample GetPrimaryExtraSample(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info.ExtraSamples == null || info.ExtraSamples.Length == 0)
        {
            return TiffExtraSample.Unspecified;
        }

        TiffExtraSample extraSample = info.ExtraSamples[0];
        if (extraSample == TiffExtraSample.Unspecified || extraSample == TiffExtraSample.AssociatedAlpha || extraSample == TiffExtraSample.UnassociatedAlpha)
        {
            return extraSample;
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ExtraSamples contains an unsupported alpha value.", info.ImageIndex, TiffTagId.ExtraSamples);
        return TiffExtraSample.Unspecified;
    }

    internal static byte UnassociateByteChannel(byte channel, byte alpha)
    {
        if (alpha == 0)
        {
            return 0;
        }

        int value = ((int)channel * 255 + alpha / 2) / alpha;
        if (value > 255)
        {
            value = 255;
        }

        return (byte)value;
    }

    internal static ushort UnassociateUShortChannel(ushort channel, ushort alpha)
    {
        if (alpha == 0)
        {
            return 0;
        }

        ulong value = ((ulong)channel * 65535UL + (ulong)(alpha / 2)) / (ulong)alpha;
        if (value > 65535UL)
        {
            value = 65535UL;
        }

        return (ushort)value;
    }

    internal static byte[] AssociateRgba8(ReadOnlySpan<byte> rgba)
    {
        byte[] destination = TiffIfdEntry.CopyBytes(rgba);
        int offset = 0;
        while (offset < destination.Length)
        {
            int alpha = destination[offset + 3];
            destination[offset] = (byte)(((int)destination[offset] * alpha + 127) / 255);
            destination[offset + 1] = (byte)(((int)destination[offset + 1] * alpha + 127) / 255);
            destination[offset + 2] = (byte)(((int)destination[offset + 2] * alpha + 127) / 255);
            offset = offset + 4;
        }

        return destination;
    }

    internal static ushort[] AssociateRgba16(ReadOnlySpan<ushort> rgba)
    {
        ushort[] destination = CopyUShorts(rgba);
        int offset = 0;
        while (offset < destination.Length)
        {
            ulong alpha = destination[offset + 3];
            destination[offset] = (ushort)(((ulong)destination[offset] * alpha + 32767UL) / 65535UL);
            destination[offset + 1] = (ushort)(((ulong)destination[offset + 1] * alpha + 32767UL) / 65535UL);
            destination[offset + 2] = (ushort)(((ulong)destination[offset + 2] * alpha + 32767UL) / 65535UL);
            offset = offset + 4;
        }

        return destination;
    }

    internal static ushort[] CopyUShorts(ReadOnlySpan<ushort> source)
    {
        ushort[] destination = new ushort[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            destination[index] = source[index];
            index = index + 1;
        }

        return destination;
    }
}
