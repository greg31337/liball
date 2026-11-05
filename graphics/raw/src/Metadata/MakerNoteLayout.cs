using System;

namespace raw;

internal static class MakerNoteLayout
{
    internal static RawMakerNoteWritePlan Plan(RawMakerNote note, long plannedOffset, RawLimits limits)
    {
        RawValidationReport validation = Validate(note, limits);
        if (!validation.IsValid() || plannedOffset < 0)
        {
            return Reject(note, plannedOffset, "A valid preserved maker note and nonnegative planned offset are required.");
        }
        long delta;
        if (plannedOffset >= note.Offset)
        {
            delta = plannedOffset - note.Offset;
        }
        else
        {
            delta = -(note.Offset - plannedOffset);
        }
        if (delta == 0)
        {
            byte[] fixedBytes = note.CopyBytes();
            RawWriteChange change = new RawWriteChange(RawWriteChangeKind.Copied, note.IfdPath + "/MakerNote", "Copy the maker note byte for byte at its required fixed anchor.", note.Offset, (note.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, plannedOffset, fixedBytes.Length, false);
            return new RawMakerNoteWritePlan(true, RawMakerNoteWriteStrategy.FixedAnchor, note.Offset, plannedOffset, 0, fixedBytes, new RawWriteChange[] { change }, null);
        }
        if (!note.RelocationSupported)
        {
            return Reject(note, plannedOffset, "The maker-note offset base is not recognized, so relocation would require guessing.");
        }
        byte[] relocated = note.CopyBytes();
        RawMakerNoteWriteStrategy strategy = RawMakerNoteWriteStrategy.RelativeRelocation;
        int index = 0;
        while (index < (note.OffsetReferences ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawMakerNoteOffsetReference reference = note.OffsetReferences[index];
            if (reference.AdjustOnRelocation)
            {
                if (!CanAdjust(reference.Value, delta))
                {
                    return Reject(note, plannedOffset, "A relocated maker-note offset would overflow its unsigned encoded value.");
                }
                ulong adjusted = Adjust(reference.Value, delta);
                if (!Fits(adjusted, reference.Width))
                {
                    return Reject(note, plannedOffset, "A relocated maker-note offset does not fit its original encoded width.");
                }
                Write(relocated, reference.Position, reference.Width, adjusted, note.ByteOrder);
                strategy = RawMakerNoteWriteStrategy.PatchedRelocation;
            }
            index++;
        }
        string description = strategy == RawMakerNoteWriteStrategy.PatchedRelocation ? "Relocate the recognized maker note and adjust every proven TIFF-relative offset." : "Relocate the recognized self-relative maker note without changing its bytes.";
        RawWriteChange relocatedChange = new RawWriteChange(RawWriteChangeKind.Relocated, note.IfdPath + "/MakerNote", description, note.Offset, (note.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length, plannedOffset, relocated.Length, false);
        return new RawMakerNoteWritePlan(true, strategy, note.Offset, plannedOffset, delta, relocated, new RawWriteChange[] { relocatedChange }, null);
    }

    internal static RawValidationReport Validate(RawMakerNote note, RawLimits limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (limits == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "MakerNote.Limits", "Maker-note validation requires resource limits.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (note == null || note.RawBytes == null || note.RawBytes.Length == 0 || note.Offset < 0 || note.EntryOffset < -1 || note.IfdPath == null || note.Manufacturer == null || note.Signature == null || note.OffsetReferences == null || note.CanonTags == null || note.NikonTags == null || note.NikonVersion == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "MakerNote", "A maker note requires exact bytes, source context, and offset evidence collections.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (note.RawBytes.LongLength > limits.MaxTagValueBytes || note.RawBytes.LongLength > limits.MaxMetadataBytes || note.RawBytes.LongLength > limits.MaxAllocationBytes)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "MakerNote.RawBytes", "The maker-note payload exceeds configured resource limits.");
        }
        bool byteOrderValid = note.ByteOrder >= RawByteOrder.LittleEndian && note.ByteOrder <= RawByteOrder.BigEndian;
        bool offsetBaseValid = note.OffsetBase >= RawMakerNoteOffsetBase.Unknown && note.OffsetBase <= RawMakerNoteOffsetBase.AbsoluteFile;
        if (!byteOrderValid || !offsetBaseValid)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "MakerNote.OffsetBase", "The maker-note byte order or offset base is not defined.");
        }
        if (note.RelocationSupported && (note.OffsetBase == RawMakerNoteOffsetBase.Unknown || note.OffsetBaseAbsolute < 0))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "MakerNote.Relocation", "Relocation support requires a recognized absolute offset base.");
        }
        if (!note.RelocationSupported && note.OffsetReferences.Length != 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "MakerNote.Relocation", "Unknown maker-note layouts cannot claim proven offset references.");
        }
        if (!byteOrderValid || !offsetBaseValid)
        {
            return new RawValidationReport(true, messages.ToArray());
        }
        int index = 0;
        while (index < note.OffsetReferences.Length)
        {
            RawMakerNoteOffsetReference reference = note.OffsetReferences[index];
            if (reference == null || reference.Position < 0 || reference.Width != 4 && reference.Width != 8 || reference.Position > note.RawBytes.Length - reference.Width || reference.TargetPosition < 0 || reference.TargetPosition >= note.RawBytes.Length)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "MakerNote.OffsetReferences", "Every maker-note offset reference must identify bounded storage and a bounded target.");
                break;
            }
            ulong stored = Read(note.RawBytes, reference.Position, reference.Width, note.ByteOrder);
            bool adjusts = note.OffsetBase == RawMakerNoteOffsetBase.TiffHeader || note.OffsetBase == RawMakerNoteOffsetBase.AbsoluteFile;
            long targetAbsolute = ReferenceTarget(note, reference.Value);
            long targetPosition = targetAbsolute < 0 ? -1 : targetAbsolute - note.Offset;
            if (stored != reference.Value || reference.AdjustOnRelocation != adjusts || targetPosition != reference.TargetPosition)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidMetadata, "MakerNote.OffsetReferences", "Maker-note offset evidence must match the preserved bytes, offset base, and bounded target.");
                break;
            }
            index++;
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static RawMakerNoteWritePlan Reject(RawMakerNote note, long plannedOffset, string message)
    {
        long originalOffset = note == null ? -1 : note.Offset;
        RawDiagnostic diagnostic = new RawDiagnostic(RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, RawFormat.Unknown, originalOffset, note == null ? string.Empty : note.IfdPath, 37500, "MakerNoteLayout", message, false);
        return new RawMakerNoteWritePlan(false, RawMakerNoteWriteStrategy.Reject, originalOffset, plannedOffset, 0, null, null, new RawDiagnostic[] { diagnostic });
    }

    internal static ulong Adjust(ulong value, long delta)
    {
        if (delta >= 0)
        {
            return value + (ulong)delta;
        }
        ulong subtraction = (ulong)(-(delta + 1)) + 1;
        return value - subtraction;
    }

    internal static bool CanAdjust(ulong value, long delta)
    {
        if (delta >= 0)
        {
            return value <= ulong.MaxValue - (ulong)delta;
        }
        ulong subtraction = (ulong)(-(delta + 1)) + 1;
        return value >= subtraction;
    }

    internal static bool Fits(ulong value, int width)
    {
        return width == 8 || value <= uint.MaxValue;
    }

    internal static long ReferenceTarget(RawMakerNote note, ulong value)
    {
        if (value > long.MaxValue)
        {
            return -1;
        }
        if (note.OffsetBase == RawMakerNoteOffsetBase.AbsoluteFile)
        {
            return (long)value;
        }
        if (note.OffsetBaseAbsolute > long.MaxValue - (long)value)
        {
            return -1;
        }
        return note.OffsetBaseAbsolute + (long)value;
    }

    internal static ulong Read(byte[] bytes, int position, int width, RawByteOrder byteOrder)
    {
        if (width == 4)
        {
            return EndianReader.ReadUInt32(bytes.AsSpan(position, 4), byteOrder);
        }
        return EndianReader.ReadUInt64(bytes.AsSpan(position, 8), byteOrder);
    }

    internal static void Write(byte[] bytes, int position, int width, ulong value, RawByteOrder byteOrder)
    {
        if (width == 4)
        {
            EndianWriter.WriteUInt32(bytes.AsSpan(position, 4), (uint)value, byteOrder);
        }
        else
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(position, 8), value, byteOrder);
        }
    }
}
