using System;

namespace tiff;

internal class TiffOmeMetadataWriter
{
    internal static TiffOmeMetadataWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, string omeXml, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, omeXml, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffOmeMetadataWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, string omeXml, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, omeXml, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffOmeMetadataWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffImageDescriptionWriteResult writeResult = TiffImageDescriptionWriter.WriteClassic(destination, values, omeXml, byteOrder, directoryOffset, nextIfdOffset, options);
        diagnostics.Merge(writeResult.Diagnostics);
        return TiffOmeMetadataWriteResult.Create(writeResult.DirectoryOffset, writeResult.BytesWritten, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, string omeXml)
    {
        if (!TiffOmeMetadataReader.IsOmeXml(omeXml))
        {
            return 0;
        }

        return TiffImageDescriptionWriter.CalculateClassicWriteByteCount(values, omeXml);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, string omeXml, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        string actualXml = TiffOmeMetadata.NormalizeString(omeXml);
        if (!TiffOmeMetadataReader.IsOmeXml(actualXml))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "OME-TIFF writer requires OME-XML in ImageDescription.", TiffDiagnostic.NoImageIndex, TiffTagId.ImageDescription);
            return false;
        }

        return TiffImageDescriptionWriter.ValidateClassicRequest(destinationLength, values, actualXml, byteOrder, directoryOffset, nextIfdOffset, diagnostics);
    }

    internal static TiffTagValue CreateValue(string omeXml)
    {
        return TiffTagValue.CreateAscii(TiffTagId.ImageDescription, TiffOmeMetadata.NormalizeString(omeXml));
    }
}
