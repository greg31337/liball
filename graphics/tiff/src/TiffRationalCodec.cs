using System;

namespace tiff;

internal class TiffRationalCodec
{
    internal static TiffRationalCodecResult Decode(byte[] bytes, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRationalDataType(dataType, diagnostics))
        {
            return TiffRationalCodecResult.Create(new TiffRational[0], new byte[0], diagnostics);
        }

        if (!ValidateByteOrder(byteOrder, diagnostics))
        {
            return TiffRationalCodecResult.Create(new TiffRational[0], new byte[0], diagnostics);
        }

        if (bytes == null)
        {
            diagnostics.AddError(TiffErrorCode.InvalidRationalValue, "Rational byte buffer is required.");
            return TiffRationalCodecResult.Create(new TiffRational[0], new byte[0], diagnostics);
        }

        if (bytes.Length % 8 != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidRationalValue, "Rational byte buffer length must be a multiple of 8.");
        }

        int count = bytes.Length / 8;
        TiffRational[] values = new TiffRational[count];
        int index = 0;
        while (index < count)
        {
            int offset = index * 8;
            if (dataType == TiffDataType.Rational)
            {
                uint numerator = TiffBinaryReader.ReadUInt32(bytes.AsSpan(offset, 4), byteOrder);
                uint denominator = TiffBinaryReader.ReadUInt32(bytes.AsSpan(offset + 4, 4), byteOrder);
                values[index] = TiffRational.Create(numerator, denominator);
            }
            else
            {
                int numerator = TiffBinaryReader.ReadInt32(bytes.AsSpan(offset, 4), byteOrder);
                int denominator = TiffBinaryReader.ReadInt32(bytes.AsSpan(offset + 4, 4), byteOrder);
                values[index] = TiffRational.Create(numerator, denominator);
            }

            ValidateDenominator(values[index], diagnostics);
            index = index + 1;
        }

        return TiffRationalCodecResult.Create(values, new byte[0], diagnostics);
    }

    internal static TiffRationalCodecResult Encode(TiffRational[] values, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateRationalDataType(dataType, diagnostics))
        {
            return TiffRationalCodecResult.Create(new TiffRational[0], new byte[0], diagnostics);
        }

        if (!ValidateByteOrder(byteOrder, diagnostics))
        {
            return TiffRationalCodecResult.Create(new TiffRational[0], new byte[0], diagnostics);
        }

        TiffRational[] normalized = TiffTagValue.CopyRationalArray(values);
        ValidateRationalValuesForEncoding(normalized, dataType, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRationalCodecResult.Create(normalized, new byte[0], diagnostics);
        }

        byte[] bytes = new byte[normalized.Length * 8];
        int index = 0;
        while (index < normalized.Length)
        {
            int offset = index * 8;
            if (dataType == TiffDataType.Rational)
            {
                TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset, 4), (uint)normalized[index].Numerator, byteOrder);
                TiffBinaryWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), (uint)normalized[index].Denominator, byteOrder);
            }
            else
            {
                TiffBinaryWriter.WriteInt32(bytes.AsSpan(offset, 4), (int)normalized[index].Numerator, byteOrder);
                TiffBinaryWriter.WriteInt32(bytes.AsSpan(offset + 4, 4), (int)normalized[index].Denominator, byteOrder);
            }

            index = index + 1;
        }

        return TiffRationalCodecResult.Create(normalized, bytes, diagnostics);
    }

    internal static void ValidateRationalValuesForEncoding(TiffRational[] values, TiffDataType dataType, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < values.Length)
        {
            ValidateDenominator(values[index], diagnostics);
            if (dataType == TiffDataType.Rational)
            {
                if (!FitsUnsignedRational(values[index]))
                {
                    diagnostics.AddError(TiffErrorCode.InvalidRationalValue, "Unsigned rational values must fit in two UInt32 fields.");
                }
            }
            else
            {
                if (!FitsSignedRational(values[index]))
                {
                    diagnostics.AddError(TiffErrorCode.InvalidRationalValue, "Signed rational values must fit in two Int32 fields.");
                }
            }

            index = index + 1;
        }
    }

    internal static bool ValidateRationalDataType(TiffDataType dataType, TiffDiagnostics diagnostics)
    {
        if (dataType == TiffDataType.Rational || dataType == TiffDataType.SRational)
        {
            return true;
        }

        diagnostics.AddError(TiffErrorCode.InvalidTagType, "Rational codec requires RATIONAL or SRATIONAL data type.");
        return false;
    }

    internal static bool ValidateByteOrder(TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (byteOrder == TiffByteOrder.LittleEndian || byteOrder == TiffByteOrder.BigEndian)
        {
            return true;
        }

        diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Rational codec requires a known byte order.");
        return false;
    }

    internal static void ValidateDenominator(TiffRational value, TiffDiagnostics diagnostics)
    {
        if (value == null || value.Denominator == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidRationalValue, "Rational denominator must not be zero.");
        }
    }

    internal static bool FitsUnsignedRational(TiffRational value)
    {
        if (value == null)
        {
            return false;
        }

        if (value.Numerator < 0 || value.Denominator < 0)
        {
            return false;
        }

        if (value.Numerator > uint.MaxValue || value.Denominator > uint.MaxValue)
        {
            return false;
        }

        return true;
    }

    internal static bool FitsSignedRational(TiffRational value)
    {
        if (value == null)
        {
            return false;
        }

        if (value.Numerator < int.MinValue || value.Numerator > int.MaxValue)
        {
            return false;
        }

        if (value.Denominator < int.MinValue || value.Denominator > int.MaxValue)
        {
            return false;
        }

        return true;
    }
}
