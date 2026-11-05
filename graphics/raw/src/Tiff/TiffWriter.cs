using System;

namespace raw;

internal class TiffWriteOutput
{
    internal System.IO.Stream Stream;
    internal long StartPosition;
    internal long Written;
    internal long MaximumLength;
    internal byte[] PaddingBuffer;

    internal TiffWriteOutput(System.IO.Stream stream, long maximumLength)
    {
        Stream = stream;
        StartPosition = stream.CanSeek ? stream.Position : 0;
        Written = 0;
        MaximumLength = maximumLength;
        PaddingBuffer = new byte[4096];
    }

    internal void Write(byte[]? bytes, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (bytes == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "TIFF output bytes are required.", Written);
        }
        long next = CheckedMath.Add(Written, bytes.Length);
        if (next > MaximumLength)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "TIFF output exceeds its planned length.", Written);
        }
        Stream.Write(bytes, 0, bytes.Length);
        Written = next;
    }

    internal void AdvanceTo(ulong offset, System.Threading.CancellationToken cancellationToken)
    {
        if (offset > long.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A planned TIFF offset exceeds the stream position range.", Written);
        }
        long target = (long)offset;
        if (target > MaximumLength)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidOffset, "A planned TIFF region starts beyond the predicted output length.", target);
        }
        if (target < Written)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidOffset, "Planned TIFF regions overlap or are out of order.", target);
        }
        long remaining = target - Written;
        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = PaddingBuffer.Length;
            if (remaining < count)
            {
                count = (int)remaining;
            }
            Stream.Write(PaddingBuffer, 0, count);
            Written += count;
            remaining -= count;
        }
    }
}

internal static class TiffWriter
{
    internal static void Write(System.IO.Stream? destination, TiffLayoutPlan? plan, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        Validate(destination, plan, limits);
        cancellationToken.ThrowIfCancellationRequested();
        TiffWriteOutput output = new TiffWriteOutput((destination ?? throw new System.InvalidOperationException("A required value is unavailable.")), (plan ?? throw new System.InvalidOperationException("A required value is unavailable.")).PredictedSize);
        WriteHeader(output, plan, cancellationToken);
        WriteDirectories(output, plan, cancellationToken);
        WriteValues(output, plan, cancellationToken);
        WriteBlocks(output, plan, cancellationToken);
        output.AdvanceTo((ulong)plan.PredictedSize, cancellationToken);
        if (output.Written != plan.PredictedSize)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "TIFF output does not match its predicted size.", output.Written);
        }
        if (destination.CanSeek && destination.Position - output.StartPosition != plan.PredictedSize)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The seekable TIFF destination advanced by an unexpected length.", destination.Position);
        }
    }

    internal static byte[] WriteToArray(TiffLayoutPlan? plan, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        CheckedMath.AllocationSize(plan == null ? 0 : plan.PredictedSize, 1, limits);
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            Write(output, plan, limits, cancellationToken);
            return output.ToArray();
        }
    }

    internal static void Validate(System.IO.Stream? destination, TiffLayoutPlan? plan, RawLimits limits)
    {
        if (destination == null || !destination.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable TIFF destination stream is required.", -1);
        }
        if (plan == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A TIFF layout plan is required.", -1);
        }
        CheckedMath.FileSize(plan.PredictedSize, limits);
        if (plan.PredictedSize < plan.HeaderLength || plan.HeaderExtraBytes == null || plan.Ifds == null || plan.Ifds.Length == 0 || plan.Blocks == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "The TIFF layout plan is incomplete.", -1);
        }
        if (plan.ByteOrder != RawByteOrder.LittleEndian && plan.ByteOrder != RawByteOrder.BigEndian)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "The TIFF output byte order is not defined.", -1);
        }
        int baseHeaderLength = plan.BigTiff ? 16 : 8;
        if (plan.HeaderLength != baseHeaderLength + plan.HeaderExtraBytes.Length || plan.BigTiff && plan.Alignment != 8 || !plan.BigTiff && plan.Alignment != 2)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "The TIFF layout format dimensions are inconsistent.", -1);
        }
        if (!plan.BigTiff && plan.PredictedSize > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "Classic TIFF output exceeds the 32-bit address space.", plan.PredictedSize);
        }
    }

    internal static void WriteHeader(TiffWriteOutput output, TiffLayoutPlan plan, System.Threading.CancellationToken cancellationToken)
    {
        byte[] header = new byte[plan.HeaderLength];
        if (plan.ByteOrder == RawByteOrder.LittleEndian)
        {
            header[0] = 0x49;
            header[1] = 0x49;
        }
        else
        {
            header[0] = 0x4d;
            header[1] = 0x4d;
        }
        if (plan.BigTiff)
        {
            EndianWriter.WriteUInt16(header.AsSpan(2, 2), 43, plan.ByteOrder);
            EndianWriter.WriteUInt16(header.AsSpan(4, 2), 8, plan.ByteOrder);
            EndianWriter.WriteUInt16(header.AsSpan(6, 2), 0, plan.ByteOrder);
            EndianWriter.WriteUInt64(header.AsSpan(8, 8), plan.FirstIfdOffset, plan.ByteOrder);
        }
        else
        {
            if (plan.FirstIfdOffset > uint.MaxValue)
            {
                throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "The classic TIFF first-directory offset exceeds 32 bits.", -1);
            }
            EndianWriter.WriteUInt16(header.AsSpan(2, 2), 42, plan.ByteOrder);
            EndianWriter.WriteUInt32(header.AsSpan(4, 4), (uint)plan.FirstIfdOffset, plan.ByteOrder);
        }
        plan.HeaderExtraBytes.AsSpan().CopyTo(header.AsSpan(plan.BigTiff ? 16 : 8));
        output.Write(header, cancellationToken);
    }

    internal static void WriteDirectories(TiffWriteOutput output, TiffLayoutPlan plan, System.Threading.CancellationToken cancellationToken)
    {
        int directoryIndex = 0;
        while (directoryIndex < (plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TiffLayoutIfdPlan directory = plan.Ifds[directoryIndex];
            if (directory == null || directory.Entries == null)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "A planned TIFF directory is incomplete.", -1);
            }
            output.AdvanceTo(directory.Offset, cancellationToken);
            long start = output.Written;
            WriteDirectoryCount(output, plan, directory.Entries.Length, cancellationToken);
            int entryIndex = 0;
            while (entryIndex < directory.Entries.Length)
            {
                TiffLayoutEntryPlan entry = directory.Entries[entryIndex];
                if (entry == null || entry.EntryOffset != output.Written)
                {
                    throw new RawWriteException(RawDiagnosticCode.InvalidOffset, "A planned TIFF entry offset is inconsistent.", output.Written);
                }
                WriteEntry(output, plan, entry, cancellationToken);
                entryIndex++;
            }
            WriteOffset(output, plan, directory.NextIfdOffset, cancellationToken);
            if (output.Written - start != directory.Length)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidLength, "A TIFF directory does not match its planned length.", start);
            }
            directoryIndex++;
        }
    }

    internal static void WriteDirectoryCount(TiffWriteOutput output, TiffLayoutPlan plan, int count, System.Threading.CancellationToken cancellationToken)
    {
        if (plan.BigTiff)
        {
            byte[] bytes = new byte[8];
            EndianWriter.WriteUInt64(bytes, (ulong)count, plan.ByteOrder);
            output.Write(bytes, cancellationToken);
            return;
        }
        if (count > ushort.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF directory contains too many entries.", output.Written);
        }
        byte[] classic = new byte[2];
        EndianWriter.WriteUInt16(classic, (ushort)count, plan.ByteOrder);
        output.Write(classic, cancellationToken);
    }

    internal static void WriteEntry(TiffWriteOutput output, TiffLayoutPlan plan, TiffLayoutEntryPlan entry, System.Threading.CancellationToken cancellationToken)
    {
        int size = plan.BigTiff ? 20 : 12;
        int slotOffset = plan.BigTiff ? 12 : 8;
        int slotSize = plan.BigTiff ? 8 : 4;
        byte[] bytes = new byte[size];
        EndianWriter.WriteUInt16(bytes.AsSpan(0, 2), entry.Tag, plan.ByteOrder);
        EndianWriter.WriteUInt16(bytes.AsSpan(2, 2), (ushort)entry.FieldType, plan.ByteOrder);
        if (plan.BigTiff)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(4, 8), entry.Count, plan.ByteOrder);
        }
        else
        {
            if (entry.Count > uint.MaxValue)
            {
                throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF entry count exceeds 32 bits.", entry.EntryOffset);
            }
            EndianWriter.WriteUInt32(bytes.AsSpan(4, 4), (uint)entry.Count, plan.ByteOrder);
        }
        if (entry.ValueBytes == null || entry.ValueBytes.Length != entry.ValueLength)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "A TIFF entry value does not match its planned length.", entry.EntryOffset);
        }
        if (entry.Inline)
        {
            if (entry.ValueBytes.Length > slotSize)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidLength, "An inline TIFF value exceeds its value slot.", entry.EntryOffset);
            }
            entry.ValueBytes.AsSpan().CopyTo(bytes.AsSpan(slotOffset, entry.ValueBytes.Length));
        }
        else
        {
            WriteOffset(bytes.AsSpan(slotOffset, slotSize), plan, entry.ValueOffset);
        }
        output.Write(bytes, cancellationToken);
    }

    internal static void WriteValues(TiffWriteOutput output, TiffLayoutPlan plan, System.Threading.CancellationToken cancellationToken)
    {
        int directoryIndex = 0;
        while (directoryIndex < (plan.Ifds ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            TiffLayoutEntryPlan[]? entries = plan.Ifds[directoryIndex].Entries;
            int entryIndex = 0;
            while (entryIndex < (entries ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
            {
                TiffLayoutEntryPlan entry = entries[entryIndex];
                if (!entry.Inline)
                {
                    output.AdvanceTo(entry.ValueOffset, cancellationToken);
                    output.Write(entry.ValueBytes, cancellationToken);
                }
                entryIndex++;
            }
            directoryIndex++;
        }
    }

    internal static void WriteBlocks(TiffWriteOutput output, TiffLayoutPlan plan, System.Threading.CancellationToken cancellationToken)
    {
        int index = 0;
        while (index < (plan.Blocks ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TiffLayoutBlockPlan block = plan.Blocks[index];
            if (block == null || block.Bytes == null || block.Bytes.Length != block.Length)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidLength, "A TIFF data block does not have its planned bytes.", -1);
            }
            output.AdvanceTo(block.Offset, cancellationToken);
            output.Write(block.Bytes, cancellationToken);
            index++;
        }
    }

    internal static void WriteOffset(TiffWriteOutput output, TiffLayoutPlan plan, ulong value, System.Threading.CancellationToken cancellationToken)
    {
        int size = plan.BigTiff ? 8 : 4;
        byte[] bytes = new byte[size];
        WriteOffset(bytes, plan, value);
        output.Write(bytes, cancellationToken);
    }

    internal static void WriteOffset(System.Span<byte> destination, TiffLayoutPlan plan, ulong value)
    {
        if (plan.BigTiff)
        {
            EndianWriter.WriteUInt64(destination, value, plan.ByteOrder);
            return;
        }
        if (value > uint.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "A classic TIFF offset exceeds 32 bits.", -1);
        }
        EndianWriter.WriteUInt32(destination, (uint)value, plan.ByteOrder);
    }
}
