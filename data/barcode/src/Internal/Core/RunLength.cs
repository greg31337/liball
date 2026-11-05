namespace barcode;

internal class RunIterator
{
    internal BitRow Row;
    internal int Position;
    internal int Direction;
    internal bool Finished;

    internal RunIterator(BitRow row, int start, int direction)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Run source must not be null.");
        }

        if (direction != 1 && direction != -1)
        {
            throw new BarcodeValidationException("Run direction must be forward or reverse.");
        }

        CheckedMath.ValidateIndex(start, row.Length);
        Row = row;
        Position = start;
        Direction = direction;
        Finished = false;
    }

    internal bool HasNextRun()
    {
        return !Finished;
    }

    internal bool GetRunColor()
    {
        if (Finished)
        {
            throw new BarcodeValidationException("Run iterator is exhausted.");
        }

        return Row.ReadBitUnchecked(Position);
    }

    internal int ReadNextRun()
    {
        if (Finished)
        {
            throw new BarcodeValidationException("Run iterator is exhausted.");
        }

        bool color = Row.ReadBitUnchecked(Position);
        int length = 0;
        while (Position >= 0 && Position < Row.Length && Row.ReadBitUnchecked(Position) == color)
        {
            length = CheckedMath.Add(length, 1);
            Position = CheckedMath.Add(Position, Direction);
        }

        if (Position < 0 || Position >= Row.Length)
        {
            Finished = true;
        }

        return length;
    }

    internal int GetPosition()
    {
        return Position;
    }
}

internal static class RunLength
{
    internal static int[] ExtractForward(BitRow row, int start, int maximumRuns)
    {
        if (maximumRuns <= 0)
        {
            throw new BarcodeValidationException("Maximum run count must be greater than zero.");
        }

        RunIterator iterator = new RunIterator(row, start, 1);
        return ReadRuns(iterator, maximumRuns);
    }

    internal static int[] ExtractReverse(BitRow row, int start, int maximumRuns)
    {
        if (maximumRuns <= 0)
        {
            throw new BarcodeValidationException("Maximum run count must be greater than zero.");
        }

        RunIterator iterator = new RunIterator(row, start, -1);
        return ReadRuns(iterator, maximumRuns);
    }

    internal static int[] ExtractAll(BitRow row)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Run source must not be null.");
        }

        return ExtractForward(row, 0, row.Length);
    }

    internal static BitRow ExpandRuns(int[] runs, bool startsDark)
    {
        if (runs == null)
        {
            throw new BarcodeValidationException("Run lengths must not be null.");
        }

        if (runs.Length == 0)
        {
            throw new BarcodeValidationException("At least one run length is required.");
        }

        int totalLength = 0;
        int runIndex = 0;
        while (runIndex < runs.Length)
        {
            if (runs[runIndex] <= 0)
            {
                throw new BarcodeValidationException("Run lengths must be greater than zero.");
            }

            totalLength = CheckedMath.Add(totalLength, runs[runIndex]);
            runIndex = runIndex + 1;
        }

        BitRow row = new BitRow(totalLength);
        bool color = startsDark;
        int position = 0;
        runIndex = 0;
        while (runIndex < runs.Length)
        {
            int remaining = runs[runIndex];
            while (remaining > 0)
            {
                if (color)
                {
                    row.WriteBitUnchecked(position, true);
                }

                position = position + 1;
                remaining = remaining - 1;
            }

            color = !color;
            runIndex = runIndex + 1;
        }

        return row;
    }

    internal static int[] ReadRuns(RunIterator iterator, int maximumRuns)
    {
        int[] buffer = new int[maximumRuns];
        int count = 0;
        while (count < maximumRuns && iterator.HasNextRun())
        {
            buffer[count] = iterator.ReadNextRun();
            count = count + 1;
        }

        int[] result = new int[count];
        int index = 0;
        while (index < count)
        {
            result[index] = buffer[index];
            index = index + 1;
        }

        return result;
    }
}
