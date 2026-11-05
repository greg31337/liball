using System;
using System.Collections.Generic;

namespace avif;

internal class Av1MotionVector
{
    internal int Row;
    internal int Column;

    internal Av1MotionVector(int row, int column)
    {
        Row = row;
        Column = column;
    }
}

internal class Av1MotionVectorCandidate
{
    internal Av1MotionVector First;
    internal Av1MotionVector Second;
    internal int Weight;
    internal bool Compound;
    internal bool Temporal;
    internal bool Global;
    internal int Order;

    internal Av1MotionVectorCandidate(
        Av1MotionVector first,
        Av1MotionVector second,
        int weight,
        bool compound,
        bool temporal,
        bool global,
        int order)
    {
        First = first;
        Second = second;
        Weight = weight;
        Compound = compound;
        Temporal = temporal;
        Global = global;
        Order = order;
    }
}

internal class Av1MotionVectorStack
{
    internal AvifResultCode Code;
    internal List<Av1MotionVectorCandidate> Candidates;
    internal int NewMotionVectorContext;
    internal int ZeroMotionVectorContext;
    internal int ReferenceMotionVectorContext;

    internal Av1MotionVectorStack(AvifResultCode code)
    {
        Code = code;
        Candidates = new List<Av1MotionVectorCandidate>();
    }
}

internal class Av1MotionVectorStackRequest
{
    internal ParsedAv1SequenceHeader? Sequence;
    internal ParsedAv1FrameHeader? Header;
    internal Av1BlockMotionField? CurrentMotionField;
    internal Av1DecodedFrame[]? ReferenceFrames;
    internal int MiRow;
    internal int MiColumn;
    internal Av1BlockSize BlockSize;
    internal Av1ReferenceFrame FirstReference;
    internal Av1ReferenceFrame SecondReference;
}

// AV1 specification sections 7.10.2 through 7.10.6 and 7.10.18.
internal static class Av1MotionVectorStackBuilder
{
    internal static Av1MotionVectorStack Build(Av1MotionVectorStackRequest request)
    {
        AvifResultCode validation = Validate(request);
        Av1MotionVectorStack stack = new Av1MotionVectorStack(validation);
        if (validation != AvifResultCode.Success)
        {
            return stack;
        }

        int width = Av1ConversionTables.GetBlockMiWidth(request.BlockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(request.BlockSize);
        bool compound = request.SecondReference > Av1ReferenceFrame.Intra;
        int order = 0;

        ScanRow(request, stack, request.MiRow - 1, request.MiColumn,
            width, 4, compound, order);
        order += width;
        ScanColumn(request, stack, request.MiColumn - 1, request.MiRow,
            height, 4, compound, order);
        order += height;
        AddSpatial(request, stack, request.MiRow - 1,
            request.MiColumn + width, 4, compound, order++);
        AddSpatial(request, stack, request.MiRow + height,
            request.MiColumn - 1, 2, compound, order++);

        int distance = 2;
        while (distance <= 3
            && stack.Candidates.Count < Av1SyntaxConstants.MaxReferenceMotionVectorStackSize)
        {
            ScanRow(request, stack, request.MiRow - distance, request.MiColumn,
                width, 2, compound, order);
            order += width;
            ScanColumn(request, stack, request.MiColumn - distance, request.MiRow,
                height, 2, compound, order);
            order += height;
            distance++;
        }

        if ((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).UseReferenceFrameMotionVectors)
        {
            AddTemporal(request, stack, request.FirstReference, compound, order++);
            if (compound)
            {
                AddTemporal(request, stack, request.SecondReference, compound, order++);
            }
        }

        Av1MotionVector? firstGlobal = GetGlobalMotionVector(
            request.Header,
            request.FirstReference,
            request.MiRow,
            request.MiColumn,
            request.BlockSize);
        Av1MotionVector? secondGlobal = compound
            ? GetGlobalMotionVector(
                request.Header,
                request.SecondReference,
                request.MiRow,
                request.MiColumn,
                request.BlockSize)
            : new Av1MotionVector(0, 0);
        AddCandidate(stack, firstGlobal, secondGlobal, 1, compound, false, true, order++);

        SortAndTrim(stack.Candidates);
        while (stack.Candidates.Count < 2)
        {
            Av1MotionVector zero = new Av1MotionVector(0, 0);
            AddCandidate(stack, zero, zero, 0, compound, false, true, order++);
            if (stack.Candidates.Count == 1
                && stack.Candidates[0].First.Row == 0
                && stack.Candidates[0].First.Column == 0
                && (!compound || stack.Candidates[0].Second.Row == 0
                    && stack.Candidates[0].Second.Column == 0))
            {
                stack.Candidates.Add(new Av1MotionVectorCandidate(
                    new Av1MotionVector(0, 0),
                    new Av1MotionVector(0, 0),
                    0,
                    compound,
                    false,
                    true,
                    order++));
            }
        }

        DeriveContexts(stack);
        return stack;
    }

    internal static Av1MotionVector? Project(
        Av1MotionVector motionVector,
        int numerator,
        int denominator)
    {
        if (motionVector == null || denominator == 0)
        {
            return null;
        }

        int clippedNumerator = Clip(-31, 31, numerator);
        int clippedDenominator = Clip(-31, 31, denominator);
        if (clippedDenominator == 0)
        {
            return null;
        }

        int row = DivideRound((long)motionVector.Row * clippedNumerator,
            clippedDenominator);
        int column = DivideRound((long)motionVector.Column * clippedNumerator,
            clippedDenominator);
        return new Av1MotionVector(
            Clip(-Av1SyntaxConstants.ReferenceMotionVectorLimit,
                Av1SyntaxConstants.ReferenceMotionVectorLimit, row),
            Clip(-Av1SyntaxConstants.ReferenceMotionVectorLimit,
                Av1SyntaxConstants.ReferenceMotionVectorLimit, column));
    }

    internal static Av1MotionVector? LowerPrecision(
        Av1MotionVector motionVector,
        bool forceInteger,
        bool allowHighPrecision)
    {
        if (motionVector == null)
        {
            return null;
        }

        int multiple = forceInteger ? 8 : (allowHighPrecision ? 1 : 2);
        return new Av1MotionVector(
            RoundToMultiple(motionVector.Row, multiple),
            RoundToMultiple(motionVector.Column, multiple));
    }

    internal static Av1MotionVector? Clamp(
        Av1MotionVector? motionVector,
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        int miRows,
        int miColumns)
    {
        if (motionVector == null || miRow < 0 || miColumn < 0
            || miRows <= 0 || miColumns <= 0)
        {
            return null;
        }

        int width = Av1ConversionTables.GetBlockMiWidth(blockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(blockSize);
        if (width <= 0 || height <= 0 || miRow > miRows - height
            || miColumn > miColumns - width)
        {
            return null;
        }

        int border = Av1SyntaxConstants.MotionVectorBorder;
        int minimumRow = -(miRow + height) * 32 - border;
        int maximumRow = (miRows - miRow) * 32 + border;
        int minimumColumn = -(miColumn + width) * 32 - border;
        int maximumColumn = (miColumns - miColumn) * 32 + border;
        return new Av1MotionVector(
            Clip(minimumRow, maximumRow, motionVector.Row),
            Clip(minimumColumn, maximumColumn, motionVector.Column));
    }

    internal static Av1MotionVector? GetGlobalMotionVector(
        ParsedAv1FrameHeader header,
        Av1ReferenceFrame referenceFrame,
        int miRow,
        int miColumn,
        Av1BlockSize blockSize)
    {
        if (header == null || header.GlobalMotion == null
            || header.GlobalMotion.Types == null
            || header.GlobalMotion.Parameters == null
            || referenceFrame <= Av1ReferenceFrame.Intra
            || (int)referenceFrame >= header.GlobalMotion.Types.Length
            || (int)referenceFrame >= header.GlobalMotion.Parameters.Length)
        {
            return new Av1MotionVector(0, 0);
        }

        int[] parameters = header.GlobalMotion.Parameters[(int)referenceFrame];
        if (parameters == null || parameters.Length < 6
            || header.GlobalMotion.Types[(int)referenceFrame] == Av1WarpModelType.Identity)
        {
            return new Av1MotionVector(0, 0);
        }

        long column;
        long row;
        if (header.GlobalMotion.Types[(int)referenceFrame]
            == Av1WarpModelType.Translation)
        {
            column = RoundShift(parameters[0], 13);
            row = RoundShift(parameters[1], 13);
        }
        else
        {
            int centerX = miColumn * 4
                + (Av1ConversionTables.GetBlockWidth(blockSize) >> 1) - 1;
            int centerY = miRow * 4
                + (Av1ConversionTables.GetBlockHeight(blockSize) >> 1) - 1;
            long identity = 1L << Av1SyntaxConstants.WarpedModelPrecisionBits;
            column = (long)parameters[2] * centerX
                + (long)parameters[3] * centerY + parameters[0]
                - identity * centerX;
            row = (long)parameters[4] * centerX
                + (long)parameters[5] * centerY + parameters[1]
                - identity * centerY;
            if (header.AllowHighPrecisionMotionVectors)
            {
                column = RoundShift(column, 13);
                row = RoundShift(row, 13);
            }
            else
            {
                column = RoundShift(column, 14) * 2;
                row = RoundShift(row, 14) * 2;
            }
        }

        Av1MotionVector? vector = new Av1MotionVector(
            ClipLong(-Av1SyntaxConstants.ReferenceMotionVectorLimit,
                Av1SyntaxConstants.ReferenceMotionVectorLimit, row),
            ClipLong(-Av1SyntaxConstants.ReferenceMotionVectorLimit,
                Av1SyntaxConstants.ReferenceMotionVectorLimit, column));
        vector = LowerPrecision(
            vector,
            header.ForceIntegerMotionVectors,
            header.AllowHighPrecisionMotionVectors);
        if (header.Size != null)
        {
            vector = Clamp(vector, miRow, miColumn, blockSize,
                header.Size.MiRows, header.Size.MiColumns);
        }

        return vector;
    }

    internal static int RelativeDistance(int first, int second, int orderHintBits)
    {
        if (orderHintBits <= 0 || orderHintBits > 30)
        {
            return 0;
        }

        int mask = (1 << orderHintBits) - 1;
        int difference = (first - second) & mask;
        int sign = 1 << (orderHintBits - 1);
        return (difference & sign) != 0 ? difference - (1 << orderHintBits) : difference;
    }

    static void ScanRow(
        Av1MotionVectorStackRequest request,
        Av1MotionVectorStack stack,
        int row,
        int columnStart,
        int count,
        int weight,
        bool compound,
        int order)
    {
        int offset = 0;
        while (offset < count)
        {
            AddSpatial(request, stack, row, columnStart + offset,
                weight, compound, order + offset);
            offset++;
        }
    }

    static void ScanColumn(
        Av1MotionVectorStackRequest request,
        Av1MotionVectorStack stack,
        int column,
        int rowStart,
        int count,
        int weight,
        bool compound,
        int order)
    {
        int offset = 0;
        while (offset < count)
        {
            AddSpatial(request, stack, rowStart + offset, column,
                weight, compound, order + offset);
            offset++;
        }
    }

    static void AddSpatial(
        Av1MotionVectorStackRequest request,
        Av1MotionVectorStack stack,
        int row,
        int column,
        int weight,
        bool compound,
        int order)
    {
        if (row < 0 || row >= (request.CurrentMotionField ?? throw new InvalidOperationException("Invalid internal state.")).MiRows
            || column < 0 || column >= request.CurrentMotionField.MiColumns)
        {
            return;
        }

        int index = row * request.CurrentMotionField.MiColumns + column;
        Av1MotionVector? first = FindSpatialVector(
            request.CurrentMotionField, index, request.FirstReference);
        if (first == null)
        {
            return;
        }

        Av1MotionVector? second = new Av1MotionVector(0, 0);
        if (compound)
        {
            second = FindSpatialVector(
                request.CurrentMotionField, index, request.SecondReference);
            if (second == null)
            {
                return;
            }
        }

        first = PrepareVector(request, first);
        second = compound ? PrepareVector(request, second) : second;
        AddCandidate(stack, first, second, weight, compound, false, false, order);
    }

    static Av1MotionVector? FindSpatialVector(
        Av1BlockMotionField field,
        int index,
        Av1ReferenceFrame referenceFrame)
    {
        int list = 0;
        while (list < 2)
        {
            if (field.GetReferenceFrame(index, list) == (int)referenceFrame)
            {
                return new Av1MotionVector(
                    field.GetMotionVectorRow(index, list),
                    field.GetMotionVectorColumn(index, list));
            }

            list++;
        }

        return null;
    }

    static void AddTemporal(
        Av1MotionVectorStackRequest request,
        Av1MotionVectorStack stack,
        Av1ReferenceFrame targetReference,
        bool compound,
        int order)
    {
        Av1DecodedFrame? frame = GetReferenceFrame(request, targetReference);
        if (frame == null || frame.MotionField == null || frame.ReferenceOrderHints == null)
        {
            return;
        }

        int row = request.MiRow
            + (Av1ConversionTables.GetBlockMiHeight(request.BlockSize) >> 1);
        int column = request.MiColumn
            + (Av1ConversionTables.GetBlockMiWidth(request.BlockSize) >> 1);
        Av1ReferenceFrame sourceReference = frame.MotionField.GetReferenceFrame(row, column);
        int sourceIndex = (int)sourceReference - (int)Av1ReferenceFrame.Last;
        if (sourceIndex < 0 || sourceIndex >= frame.ReferenceOrderHints.Length)
        {
            return;
        }

        int bits = (request.Sequence ?? throw new InvalidOperationException("Invalid internal state.")).GetOrderHintBits();
        int numerator = RelativeDistance((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).OrderHint, frame.OrderHint, bits);
        int denominator = RelativeDistance(
            frame.ReferenceOrderHints[sourceIndex], frame.OrderHint, bits);
        Av1MotionVector? projected = Project(
            new Av1MotionVector(
                frame.MotionField.GetMotionVectorRow(row, column),
                frame.MotionField.GetMotionVectorColumn(row, column)),
            numerator,
            denominator);
        if (projected == null)
        {
            return;
        }

        projected = PrepareVector(request, projected);
        Av1MotionVector? second = new Av1MotionVector(0, 0);
        if (compound)
        {
            Av1ReferenceFrame other = targetReference == request.FirstReference
                ? request.SecondReference
                : request.FirstReference;
            second = GetGlobalMotionVector(request.Header, other,
                request.MiRow, request.MiColumn, request.BlockSize);
            if (targetReference == request.SecondReference)
            {
                Av1MotionVector? swap = projected;
                projected = second;
                second = swap;
            }
        }

        AddCandidate(stack, projected, second, 2, compound, true, false, order);
    }

    static Av1DecodedFrame? GetReferenceFrame(
        Av1MotionVectorStackRequest request,
        Av1ReferenceFrame referenceFrame)
    {
        int index = (int)referenceFrame - (int)Av1ReferenceFrame.Last;
        if (index < 0 || index >= (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).ReferenceFrameIndexes.Length)
        {
            return null;
        }

        int slot = request.Header.ReferenceFrameIndexes[index];
        return slot >= 0 && slot < (request.ReferenceFrames ?? throw new InvalidOperationException("Invalid internal state.")).Length
            ? request.ReferenceFrames[slot]
            : null;
    }

    static Av1MotionVector? PrepareVector(
        Av1MotionVectorStackRequest request,
        Av1MotionVector vector)
    {
        Av1MotionVector? result = LowerPrecision(vector,
            (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).ForceIntegerMotionVectors,
            request.Header.AllowHighPrecisionMotionVectors);
        return Clamp(result, request.MiRow, request.MiColumn, request.BlockSize,
            (request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows, request.Header.Size.MiColumns);
    }

    static void AddCandidate(
        Av1MotionVectorStack stack,
        Av1MotionVector? first,
        Av1MotionVector? second,
        int weight,
        bool compound,
        bool temporal,
        bool global,
        int order)
    {
        if (first == null || second == null)
        {
            return;
        }

        int index = 0;
        while (index < stack.Candidates.Count)
        {
            Av1MotionVectorCandidate candidate = stack.Candidates[index];
            if (candidate.First.Row == first.Row
                && candidate.First.Column == first.Column
                && (!compound || candidate.Second.Row == second.Row
                    && candidate.Second.Column == second.Column))
            {
                candidate.Weight += weight;
                candidate.Temporal |= temporal;
                candidate.Global &= global;
                return;
            }

            index++;
        }

        stack.Candidates.Add(new Av1MotionVectorCandidate(
            first, second, weight, compound, temporal, global, order));
    }

    static void SortAndTrim(List<Av1MotionVectorCandidate> candidates)
    {
        int index = 1;
        while (index < candidates.Count)
        {
            Av1MotionVectorCandidate value = candidates[index];
            int insertion = index;
            while (insertion > 0
                && ComesBefore(value, candidates[insertion - 1]))
            {
                candidates[insertion] = candidates[insertion - 1];
                insertion--;
            }

            candidates[insertion] = value;
            index++;
        }

        while (candidates.Count > Av1SyntaxConstants.MaxReferenceMotionVectorStackSize)
        {
            candidates.RemoveAt(candidates.Count - 1);
        }
    }

    static bool ComesBefore(
        Av1MotionVectorCandidate first,
        Av1MotionVectorCandidate second)
    {
        return first.Weight > second.Weight
            || first.Weight == second.Weight && first.Order < second.Order;
    }

    static void DeriveContexts(Av1MotionVectorStack stack)
    {
        int nearestWeight = stack.Candidates.Count > 0
            ? stack.Candidates[0].Weight
            : 0;
        int secondWeight = stack.Candidates.Count > 1
            ? stack.Candidates[1].Weight
            : 0;
        stack.NewMotionVectorContext = Clip(0,
            Av1SyntaxConstants.NewMotionVectorContexts - 1,
            nearestWeight == 0 ? 0 : (nearestWeight >= 8 ? 5 : 3));
        stack.ZeroMotionVectorContext = nearestWeight >= 4 ? 1 : 0;
        stack.ReferenceMotionVectorContext = Clip(0,
            Av1SyntaxConstants.ReferenceMotionVectorContexts - 1,
            BooleanValue(nearestWeight >= 4) + BooleanValue(secondWeight >= 4) * 2
                + BooleanValue(stack.Candidates.Count > 2));
    }

    static AvifResultCode Validate(Av1MotionVectorStackRequest request)
    {
        if (request == null || request.Sequence == null || request.Header == null
            || request.Header.Size == null || request.CurrentMotionField == null
            || request.CurrentMotionField.Code != AvifResultCode.Success
            || request.ReferenceFrames == null
            || request.ReferenceFrames.Length != Av1SyntaxConstants.StoredReferenceFrames
            || request.Header.ReferenceFrameIndexes == null
            || request.Header.ReferenceFrameIndexes.Length != Av1SyntaxConstants.RefsPerFrame
            || request.FirstReference < Av1ReferenceFrame.Last
            || request.FirstReference > Av1ReferenceFrame.Alternate
            || request.SecondReference < Av1ReferenceFrame.None
            || request.SecondReference > Av1ReferenceFrame.Alternate
            || request.MiRow < 0 || request.MiColumn < 0
            || request.CurrentMotionField.MiRows != request.Header.Size.MiRows
            || request.CurrentMotionField.MiColumns != request.Header.Size.MiColumns)
        {
            return AvifResultCode.InvalidArgument;
        }

        int width = Av1ConversionTables.GetBlockMiWidth(request.BlockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(request.BlockSize);
        if (width <= 0 || height <= 0
            || request.MiRow > request.Header.Size.MiRows - height
            || request.MiColumn > request.Header.Size.MiColumns - width
            || request.SecondReference == Av1ReferenceFrame.Intra
            || request.SecondReference == request.FirstReference)
        {
            return AvifResultCode.InvalidArgument;
        }

        return AvifResultCode.Success;
    }

    static int RoundToMultiple(int value, int multiple)
    {
        if (multiple == 1)
        {
            return value;
        }

        if (multiple == 2)
        {
            return (value & 1) == 0
                ? value
                : value + (value > 0 ? -1 : 1);
        }

        int remainder = value % multiple;
        int rounded = value - remainder;
        if (Absolute(remainder) > multiple >> 1)
        {
            rounded += remainder > 0 ? multiple : -multiple;
        }

        return rounded;
    }

    static int DivideRound(long numerator, int denominator)
    {
        bool negative = numerator < 0 != denominator < 0;
        long magnitude = numerator < 0 ? -numerator : numerator;
        int divisor = denominator < 0 ? -denominator : denominator;
        int result = (int)((magnitude + (divisor >> 1)) / divisor);
        return negative ? -result : result;
    }

    static long RoundShift(long value, int bits)
    {
        if (value < 0)
        {
            return -((-value + (1L << (bits - 1))) >> bits);
        }

        return (value + (1L << (bits - 1))) >> bits;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    static int BooleanValue(bool value)
    {
        return value ? 1 : 0;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static int ClipLong(int minimum, int maximum, long value)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : (int)value;
    }
}
