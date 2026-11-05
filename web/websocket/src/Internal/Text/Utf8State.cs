using System;

namespace websocket;

internal class Utf8State
{
    internal bool IsValid = true;
    internal bool IsComplete;
    internal int RemainingContinuationCount;
    internal int ContinuationMinimum = 128;
    internal int ContinuationMaximum = 191;

    internal void Reset()
    {
        IsValid = true;
        IsComplete = false;
        RemainingContinuationCount = 0;
        ContinuationMinimum = 128;
        ContinuationMaximum = 191;
    }

    internal bool Validate(ReadOnlySpan<byte> source)
    {
        if (!IsValid || IsComplete)
        {
            return false;
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            int value = source[sourceIndex];
            if (RemainingContinuationCount == 0)
            {
                if (value <= 127)
                {
                    sourceIndex = sourceIndex + 1;
                    continue;
                }

                if (value >= 194 && value <= 223)
                {
                    BeginSequence(1, 128, 191);
                }
                else if (value == 224)
                {
                    BeginSequence(2, 160, 191);
                }
                else if (value >= 225 && value <= 236)
                {
                    BeginSequence(2, 128, 191);
                }
                else if (value == 237)
                {
                    BeginSequence(2, 128, 159);
                }
                else if (value >= 238 && value <= 239)
                {
                    BeginSequence(2, 128, 191);
                }
                else if (value == 240)
                {
                    BeginSequence(3, 144, 191);
                }
                else if (value >= 241 && value <= 243)
                {
                    BeginSequence(3, 128, 191);
                }
                else if (value == 244)
                {
                    BeginSequence(3, 128, 143);
                }
                else
                {
                    IsValid = false;
                    return false;
                }
            }
            else
            {
                if (value < ContinuationMinimum || value > ContinuationMaximum)
                {
                    IsValid = false;
                    return false;
                }

                RemainingContinuationCount = RemainingContinuationCount - 1;
                ContinuationMinimum = 128;
                ContinuationMaximum = 191;
            }

            sourceIndex = sourceIndex + 1;
        }

        return true;
    }

    internal bool Complete()
    {
        if (!IsComplete)
        {
            if (RemainingContinuationCount != 0)
            {
                IsValid = false;
            }

            IsComplete = true;
        }

        return IsValid;
    }

    internal void BeginSequence(int continuationCount, int minimum, int maximum)
    {
        RemainingContinuationCount = continuationCount;
        ContinuationMinimum = minimum;
        ContinuationMaximum = maximum;
    }
}
