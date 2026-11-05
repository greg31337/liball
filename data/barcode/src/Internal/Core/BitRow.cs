namespace barcode;

internal class BitRow
{
    internal int Length;
    internal uint[] Words;

    internal BitRow(int length)
    {
        Length = CheckedMath.ValidateDimension(length);
        int wordCount = ((length - 1) / 32) + 1;
        Words = new uint[wordCount];
    }

    internal bool GetBit(int index)
    {
        CheckedMath.ValidateIndex(index, Length);
        return ReadBitUnchecked(index);
    }

    internal void SetBit(int index, bool value)
    {
        CheckedMath.ValidateIndex(index, Length);
        WriteBitUnchecked(index, value);
    }

    internal void ClearBits()
    {
        int index = 0;
        while (index < Words.Length)
        {
            Words[index] = 0u;
            index = index + 1;
        }
    }

    internal void SetAllBits(bool value)
    {
        uint word = value ? uint.MaxValue : 0u;
        int index = 0;
        while (index < Words.Length)
        {
            Words[index] = word;
            index = index + 1;
        }

        int remainder = Length & 31;
        if (value && remainder != 0)
        {
            uint mask = (1u << remainder) - 1u;
            Words[Words.Length - 1] = Words[Words.Length - 1] & mask;
        }
    }

    internal void LoadBits(bool[] bits)
    {
        if (bits == null)
        {
            throw new BarcodeValidationException("Bit source must not be null.");
        }

        if (bits.Length != Length)
        {
            throw new BarcodeValidationException("Bit source length does not match the row length.");
        }

        ClearBits();
        int index = 0;
        while (index < Length)
        {
            if (bits[index])
            {
                WriteBitUnchecked(index, true);
            }

            index = index + 1;
        }
    }

    internal bool[] CopyBits()
    {
        bool[] bits = new bool[Length];
        int index = 0;
        while (index < Length)
        {
            bits[index] = ReadBitUnchecked(index);
            index = index + 1;
        }

        return bits;
    }

    internal uint[] CopyPackedWords()
    {
        uint[] words = new uint[Words.Length];
        int index = 0;
        while (index < Words.Length)
        {
            words[index] = Words[index];
            index = index + 1;
        }

        return words;
    }

    internal void ReverseBits()
    {
        int left = 0;
        int right = Length - 1;
        while (left < right)
        {
            bool leftValue = ReadBitUnchecked(left);
            bool rightValue = ReadBitUnchecked(right);
            if (leftValue != rightValue)
            {
                WriteBitUnchecked(left, rightValue);
                WriteBitUnchecked(right, leftValue);
            }

            left = left + 1;
            right = right - 1;
        }
    }

    internal int CountDarkBits()
    {
        int count = 0;
        int index = 0;
        while (index < Length)
        {
            if (ReadBitUnchecked(index))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal bool ReadBitUnchecked(int index)
    {
        int wordIndex = index >> 5;
        int bitIndex = index & 31;
        uint mask = 1u << bitIndex;
        return (Words[wordIndex] & mask) != 0u;
    }

    internal void WriteBitUnchecked(int index, bool value)
    {
        int wordIndex = index >> 5;
        int bitIndex = index & 31;
        uint mask = 1u << bitIndex;
        if (value)
        {
            Words[wordIndex] = Words[wordIndex] | mask;
        }
        else
        {
            Words[wordIndex] = Words[wordIndex] & ~mask;
        }
    }
}
