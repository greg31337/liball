namespace raw;

internal class TiffPredictorParameters
{
    internal TiffPredictor Predictor;
    internal int Width;
    internal int RowCount;
    internal int SamplesPerPixel;
    internal int BitsPerSample;
    internal TiffSampleFormat SampleFormat;
    internal RawByteOrder ByteOrder;

    internal TiffPredictorParameters(TiffPredictor predictor, int width, int rowCount, int samplesPerPixel, int bitsPerSample, TiffSampleFormat sampleFormat, RawByteOrder byteOrder)
    {
        Predictor = predictor;
        Width = width;
        RowCount = rowCount;
        SamplesPerPixel = samplesPerPixel;
        BitsPerSample = bitsPerSample;
        SampleFormat = sampleFormat;
        ByteOrder = byteOrder;
    }
}

internal static class TiffPredictorCodec
{
    internal static int GetDecodedLength(TiffPredictorParameters parameters, RawLimits limits)
    {
        Validate(parameters);
        long sampleCount = CheckedMath.Multiply(parameters.Width, parameters.SamplesPerPixel);
        long rowBits = CheckedMath.Multiply(sampleCount, parameters.BitsPerSample);
        long rowBytes = CheckedMath.Add(rowBits, 7) / 8;
        long decodedBytes = CheckedMath.DecodedSize(rowBytes, parameters.RowCount, limits);
        CheckedMath.AllocationSize(decodedBytes, 1, limits);
        return CheckedMath.ToInt32(decodedBytes);
    }

    internal static void Encode(System.Span<byte> data, TiffPredictorParameters parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int expectedLength = GetDecodedLength(parameters, limits);
        if (data.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The TIFF predictor input length does not match its row geometry.", -1);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (parameters.Predictor == TiffPredictor.None)
        {
            return;
        }
        if (parameters.Predictor == TiffPredictor.Horizontal)
        {
            ApplyHorizontal(data, parameters, limits, true, cancellationToken);
            return;
        }
        ApplyFloatingPoint(data, parameters, limits, true, cancellationToken);
    }

    internal static void Decode(System.Span<byte> data, TiffPredictorParameters parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int expectedLength = GetDecodedLength(parameters, limits);
        if (data.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The TIFF predictor output length does not match its row geometry.", -1);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (parameters.Predictor == TiffPredictor.None)
        {
            return;
        }
        if (parameters.Predictor == TiffPredictor.Horizontal)
        {
            ApplyHorizontal(data, parameters, limits, false, cancellationToken);
            return;
        }
        ApplyFloatingPoint(data, parameters, limits, false, cancellationToken);
    }

    internal static void Validate(TiffPredictorParameters parameters)
    {
        if (parameters == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "TIFF predictor parameters are required.", -1);
        }
        if (parameters.Predictor != TiffPredictor.None && parameters.Predictor != TiffPredictor.Horizontal && parameters.Predictor != TiffPredictor.FloatingPoint)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The TIFF predictor is not supported.", -1);
        }
        if (parameters.Width <= 0 || parameters.RowCount <= 0 || parameters.SamplesPerPixel <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "TIFF predictor dimensions and sample counts must be greater than zero.", -1);
        }
        if (parameters.BitsPerSample <= 0 || parameters.BitsPerSample > 64)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "TIFF predictor sample widths must be between 1 and 64 bits.", -1);
        }
        if (parameters.ByteOrder != RawByteOrder.LittleEndian && parameters.ByteOrder != RawByteOrder.BigEndian)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The TIFF predictor byte order is not defined.", -1);
        }
        if (parameters.Predictor == TiffPredictor.Horizontal && parameters.BitsPerSample > 32 && parameters.BitsPerSample != 64)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Horizontal prediction supports sample widths through 32 bits and exactly 64 bits.", -1);
        }
        if (parameters.Predictor == TiffPredictor.FloatingPoint)
        {
            if (parameters.SampleFormat != TiffSampleFormat.FloatingPoint)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Floating-point prediction requires IEEE floating-point samples.", -1);
            }
            if (parameters.BitsPerSample != 16 && parameters.BitsPerSample != 24 && parameters.BitsPerSample != 32 && parameters.BitsPerSample != 64)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Floating-point prediction requires 16-, 24-, 32-, or 64-bit samples.", -1);
            }
        }
    }

    internal static void ApplyHorizontal(System.Span<byte> data, TiffPredictorParameters parameters, RawLimits limits, bool encoding, System.Threading.CancellationToken cancellationToken)
    {
        int samplesPerRow = CheckedMath.ToInt32(CheckedMath.Multiply(parameters.Width, parameters.SamplesPerPixel));
        int rowBytes = data.Length / parameters.RowCount;
        if (parameters.BitsPerSample == 8)
        {
            ApplyHorizontal8(data, rowBytes, parameters.RowCount, parameters.SamplesPerPixel, encoding, cancellationToken);
            return;
        }
        if (parameters.BitsPerSample == 16 || parameters.BitsPerSample == 32 || parameters.BitsPerSample == 64)
        {
            ApplyHorizontalWords(data, rowBytes, parameters.RowCount, parameters.SamplesPerPixel, parameters.BitsPerSample, parameters.ByteOrder, encoding, cancellationToken);
            return;
        }

        CheckedMath.AllocationSize(samplesPerRow, 4, limits);
        uint[] samples = new uint[samplesPerRow];
        uint mask = (uint)BitUtility.Mask(parameters.BitsPerSample);
        for (int row = 0; row < parameters.RowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            System.Span<byte> rowData = data.Slice(row * rowBytes, rowBytes);
            BitPacking.UnpackUnsigned(rowData, samplesPerRow, parameters.BitsPerSample, RawBitOrder.MostSignificantBitFirst, samples);
            if (encoding)
            {
                for (int index = samplesPerRow - 1; index >= parameters.SamplesPerPixel; index--)
                {
                    samples[index] = BitUtility.SubtractUInt32(samples[index], samples[index - parameters.SamplesPerPixel]) & mask;
                }
            }
            else
            {
                for (int index = parameters.SamplesPerPixel; index < samplesPerRow; index++)
                {
                    samples[index] = BitUtility.AddUInt32(samples[index], samples[index - parameters.SamplesPerPixel]) & mask;
                }
            }
            BitPacking.PackUnsigned(samples, parameters.BitsPerSample, RawBitOrder.MostSignificantBitFirst, rowData);
        }
    }

    internal static void ApplyHorizontal8(System.Span<byte> data, int rowBytes, int rowCount, int stride, bool encoding, System.Threading.CancellationToken cancellationToken)
    {
        for (int row = 0; row < rowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            System.Span<byte> rowData = data.Slice(row * rowBytes, rowBytes);
            if (encoding)
            {
                for (int index = rowData.Length - 1; index >= stride; index--)
                {
                    rowData[index] = BitUtility.WrapUInt8(rowData[index] - rowData[index - stride]);
                }
            }
            else
            {
                for (int index = stride; index < rowData.Length; index++)
                {
                    rowData[index] = BitUtility.WrapUInt8(rowData[index] + rowData[index - stride]);
                }
            }
        }
    }

    internal static void ApplyHorizontalWords(System.Span<byte> data, int rowBytes, int rowCount, int stride, int bitsPerSample, RawByteOrder byteOrder, bool encoding, System.Threading.CancellationToken cancellationToken)
    {
        int sampleBytes = bitsPerSample / 8;
        int samplesPerRow = rowBytes / sampleBytes;
        for (int row = 0; row < rowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            System.Span<byte> rowData = data.Slice(row * rowBytes, rowBytes);
            if (encoding)
            {
                for (int index = samplesPerRow - 1; index >= stride; index--)
                {
                    WriteDifference(rowData, index, index - stride, sampleBytes, byteOrder);
                }
            }
            else
            {
                for (int index = stride; index < samplesPerRow; index++)
                {
                    WriteSum(rowData, index, index - stride, sampleBytes, byteOrder);
                }
            }
        }
    }

    internal static void WriteDifference(System.Span<byte> rowData, int currentIndex, int previousIndex, int sampleBytes, RawByteOrder byteOrder)
    {
        int currentOffset = currentIndex * sampleBytes;
        int previousOffset = previousIndex * sampleBytes;
        if (sampleBytes == 2)
        {
            ushort current = EndianReader.ReadUInt16(rowData.Slice(currentOffset, 2), byteOrder);
            ushort previous = EndianReader.ReadUInt16(rowData.Slice(previousOffset, 2), byteOrder);
            EndianWriter.WriteUInt16(rowData.Slice(currentOffset, 2), BitUtility.SubtractUInt16(current, previous), byteOrder);
        }
        else if (sampleBytes == 4)
        {
            uint current = EndianReader.ReadUInt32(rowData.Slice(currentOffset, 4), byteOrder);
            uint previous = EndianReader.ReadUInt32(rowData.Slice(previousOffset, 4), byteOrder);
            EndianWriter.WriteUInt32(rowData.Slice(currentOffset, 4), BitUtility.SubtractUInt32(current, previous), byteOrder);
        }
        else
        {
            ulong current = EndianReader.ReadUInt64(rowData.Slice(currentOffset, 8), byteOrder);
            ulong previous = EndianReader.ReadUInt64(rowData.Slice(previousOffset, 8), byteOrder);
            EndianWriter.WriteUInt64(rowData.Slice(currentOffset, 8), BitUtility.SubtractUInt64(current, previous), byteOrder);
        }
    }

    internal static void WriteSum(System.Span<byte> rowData, int currentIndex, int previousIndex, int sampleBytes, RawByteOrder byteOrder)
    {
        int currentOffset = currentIndex * sampleBytes;
        int previousOffset = previousIndex * sampleBytes;
        if (sampleBytes == 2)
        {
            ushort current = EndianReader.ReadUInt16(rowData.Slice(currentOffset, 2), byteOrder);
            ushort previous = EndianReader.ReadUInt16(rowData.Slice(previousOffset, 2), byteOrder);
            EndianWriter.WriteUInt16(rowData.Slice(currentOffset, 2), BitUtility.AddUInt16(current, previous), byteOrder);
        }
        else if (sampleBytes == 4)
        {
            uint current = EndianReader.ReadUInt32(rowData.Slice(currentOffset, 4), byteOrder);
            uint previous = EndianReader.ReadUInt32(rowData.Slice(previousOffset, 4), byteOrder);
            EndianWriter.WriteUInt32(rowData.Slice(currentOffset, 4), BitUtility.AddUInt32(current, previous), byteOrder);
        }
        else
        {
            ulong current = EndianReader.ReadUInt64(rowData.Slice(currentOffset, 8), byteOrder);
            ulong previous = EndianReader.ReadUInt64(rowData.Slice(previousOffset, 8), byteOrder);
            EndianWriter.WriteUInt64(rowData.Slice(currentOffset, 8), BitUtility.AddUInt64(current, previous), byteOrder);
        }
    }

    internal static void ApplyFloatingPoint(System.Span<byte> data, TiffPredictorParameters parameters, RawLimits limits, bool encoding, System.Threading.CancellationToken cancellationToken)
    {
        int rowBytes = data.Length / parameters.RowCount;
        int bytesPerSample = parameters.BitsPerSample / 8;
        int sampleCount = rowBytes / bytesPerSample;
        CheckedMath.AllocationSize(rowBytes, 1, limits);
        byte[] temporary = new byte[rowBytes];
        for (int row = 0; row < parameters.RowCount; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            System.Span<byte> rowData = data.Slice(row * rowBytes, rowBytes);
            rowData.CopyTo(temporary);
            if (encoding)
            {
                ShuffleFloatingPoint(temporary, rowData, sampleCount, bytesPerSample, parameters.ByteOrder);
                for (int index = rowBytes - 1; index >= parameters.SamplesPerPixel; index--)
                {
                    rowData[index] = BitUtility.WrapUInt8(rowData[index] - rowData[index - parameters.SamplesPerPixel]);
                }
            }
            else
            {
                for (int index = parameters.SamplesPerPixel; index < rowBytes; index++)
                {
                    rowData[index] = BitUtility.WrapUInt8(rowData[index] + rowData[index - parameters.SamplesPerPixel]);
                }
                rowData.CopyTo(temporary);
                UnshuffleFloatingPoint(temporary, rowData, sampleCount, bytesPerSample, parameters.ByteOrder);
            }
        }
    }

    internal static void ShuffleFloatingPoint(System.ReadOnlySpan<byte> source, System.Span<byte> destination, int sampleCount, int bytesPerSample, RawByteOrder byteOrder)
    {
        for (int sample = 0; sample < sampleCount; sample++)
        {
            for (int plane = 0; plane < bytesPerSample; plane++)
            {
                int sourceByte = plane;
                if (byteOrder == RawByteOrder.LittleEndian)
                {
                    sourceByte = bytesPerSample - plane - 1;
                }
                destination[plane * sampleCount + sample] = source[sample * bytesPerSample + sourceByte];
            }
        }
    }

    internal static void UnshuffleFloatingPoint(System.ReadOnlySpan<byte> source, System.Span<byte> destination, int sampleCount, int bytesPerSample, RawByteOrder byteOrder)
    {
        for (int sample = 0; sample < sampleCount; sample++)
        {
            for (int destinationByte = 0; destinationByte < bytesPerSample; destinationByte++)
            {
                int plane = destinationByte;
                if (byteOrder == RawByteOrder.LittleEndian)
                {
                    plane = bytesPerSample - destinationByte - 1;
                }
                destination[sample * bytesPerSample + destinationByte] = source[plane * sampleCount + sample];
            }
        }
    }
}
