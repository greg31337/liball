using System;

namespace html;

internal static class BufferCapacity
{
    internal static void Validate(int initialCapacity, int maximumCapacity)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException("initialCapacity", initialCapacity, "An initial buffer capacity cannot be negative.");
        }

        if (maximumCapacity < 0)
        {
            throw new ArgumentOutOfRangeException("maximumCapacity", maximumCapacity, "A maximum buffer capacity cannot be negative.");
        }

        if (initialCapacity > maximumCapacity)
        {
            throw new ArgumentException("The initial buffer capacity cannot exceed the maximum capacity.");
        }
    }

    internal static int GetNext(int currentCapacity, int requiredCapacity, int maximumCapacity)
    {
        if (currentCapacity < 0 || requiredCapacity < 0 || maximumCapacity < 0)
        {
            throw new ArgumentOutOfRangeException("requiredCapacity", requiredCapacity, "Buffer capacities cannot be negative.");
        }

        if (currentCapacity > maximumCapacity || requiredCapacity > maximumCapacity)
        {
            throw new ArgumentException("A buffer capacity cannot exceed its configured maximum.");
        }

        if (requiredCapacity <= currentCapacity)
        {
            return currentCapacity;
        }

        int capacity = currentCapacity;
        if (capacity < 16)
        {
            capacity = 16;
            if (capacity > maximumCapacity)
            {
                capacity = maximumCapacity;
            }
        }

        while (capacity < requiredCapacity)
        {
            if (capacity > maximumCapacity / 2)
            {
                capacity = maximumCapacity;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        return capacity;
    }
}

internal class CharacterBuffer
{
    internal char[] characters;
    internal int length;
    internal int maximumCapacity;
    internal HtmlBudget? budget;
    internal bool usesOutputBudget;
    internal bool limitExceeded;

    internal CharacterBuffer(int initialCapacity, int maximumCapacity, HtmlBudget? budget, bool usesOutputBudget)
    {
        BufferCapacity.Validate(initialCapacity, maximumCapacity);
        if (usesOutputBudget && budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        this.characters = new char[initialCapacity];
        this.length = 0;
        this.maximumCapacity = maximumCapacity;
        this.budget = budget;
        this.usesOutputBudget = usesOutputBudget;
        this.limitExceeded = false;
    }

    internal static CharacterBuffer Create(int initialCapacity, int maximumCapacity)
    {
        return new CharacterBuffer(initialCapacity, maximumCapacity, null, false);
    }

    internal static CharacterBuffer CreateOutput(HtmlBudget budget, int initialCapacity)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        int maximum = budget.GetLimits().GetMaximumOutputCharacters();
        return new CharacterBuffer(initialCapacity, maximum, budget, true);
    }

    internal int GetLength()
    {
        return this.length;
    }

    internal int GetCapacity()
    {
        return this.characters.Length;
    }

    internal int GetMaximumCapacity()
    {
        return this.maximumCapacity;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    internal char GetCharacter(int index)
    {
        if (index < 0 || index >= this.length)
        {
            throw new ArgumentOutOfRangeException("index", index, "The character index is outside the buffer.");
        }

        return this.characters[index];
    }

    internal ReadOnlySpan<char> GetSpan()
    {
        return new ReadOnlySpan<char>(this.characters, 0, this.length);
    }

    internal string GetString()
    {
        return new string(this.characters, 0, this.length);
    }

    internal char[] GetCharacters()
    {
        char[] result = new char[this.length];
        Array.Copy(this.characters, result, this.length);
        return result;
    }

    internal bool TryAppend(char value)
    {
        if (!this.PrepareAppend(1))
        {
            return false;
        }

        this.characters[this.length] = value;
        this.length = this.length + 1;
        return true;
    }

    internal bool TryAppendString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return this.TryAppendSpan(value.AsSpan());
    }

    internal bool TryAppendSpan(ReadOnlySpan<char> value)
    {
        int amount = value.Length;
        if (!this.PrepareAppend(amount))
        {
            return false;
        }

        value.CopyTo(new Span<char>(this.characters, this.length, amount));
        this.length = this.length + amount;
        return true;
    }

    internal bool TryAppendRepeated(char value, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", count, "A repetition count cannot be negative.");
        }

        if (!this.PrepareAppend(count))
        {
            return false;
        }

        int end = this.length + count;
        int index = this.length;
        while (index < end)
        {
            this.characters[index] = value;
            index = index + 1;
        }

        this.length = end;
        return true;
    }

    internal bool TryAppendScalar(int value)
    {
        int scalar = UnicodeTools.GetScalarOrReplacement(value);
        int amount = scalar <= 65535 ? 1 : 2;
        if (!this.PrepareAppend(amount))
        {
            return false;
        }

        if (amount == 1)
        {
            this.characters[this.length] = (char)scalar;
        }
        else
        {
            this.characters[this.length] = UnicodeTools.GetHighSurrogate(scalar);
            this.characters[this.length + 1] = UnicodeTools.GetLowSurrogate(scalar);
        }

        this.length = this.length + amount;
        return true;
    }

    internal void Clear()
    {
        this.length = 0;
    }

    internal void Truncate(int length)
    {
        if (length < 0 || length > this.length)
        {
            throw new ArgumentOutOfRangeException("length", length, "A truncation length must be within the current buffer.");
        }

        this.length = length;
    }

    internal bool PrepareAppend(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException("amount", amount, "An append length cannot be negative.");
        }

        HtmlBudget? optionalBudget = this.budget;
        if (this.usesOutputBudget && optionalBudget == null)
        {
            throw new InvalidOperationException(
                "An output-budgeted buffer requires a budget.");
        }

        if (amount > this.maximumCapacity - this.length)
        {
            this.limitExceeded = true;
            if (this.usesOutputBudget && optionalBudget != null)
            {
                optionalBudget.TryConsume(HtmlLimitKind.OutputCharacters,
                    amount);
            }

            return false;
        }

        if (this.usesOutputBudget && optionalBudget != null
            && !optionalBudget.TryConsume(HtmlLimitKind.OutputCharacters,
                amount))
        {
            this.limitExceeded = true;
            return false;
        }

        int required = this.length + amount;
        if (required > this.characters.Length)
        {
            int capacity = BufferCapacity.GetNext(this.characters.Length, required, this.maximumCapacity);
            char[] expanded = new char[capacity];
            Array.Copy(this.characters, expanded, this.length);
            this.characters = expanded;
        }

        return true;
    }
}

internal class ByteBuffer
{
    internal byte[] bytes;
    internal int length;
    internal int maximumCapacity;
    internal HtmlBudget? budget;
    internal bool usesInputBudget;
    internal bool limitExceeded;

    internal ByteBuffer(int initialCapacity, int maximumCapacity, HtmlBudget? budget, bool usesInputBudget)
    {
        BufferCapacity.Validate(initialCapacity, maximumCapacity);
        if (usesInputBudget && budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        this.bytes = new byte[initialCapacity];
        this.length = 0;
        this.maximumCapacity = maximumCapacity;
        this.budget = budget;
        this.usesInputBudget = usesInputBudget;
        this.limitExceeded = false;
    }

    internal static ByteBuffer Create(int initialCapacity, int maximumCapacity)
    {
        return new ByteBuffer(initialCapacity, maximumCapacity, null, false);
    }

    internal static ByteBuffer CreateInput(HtmlBudget budget, int initialCapacity)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        int maximum = budget.GetLimits().GetMaximumInputBytes();
        return new ByteBuffer(initialCapacity, maximum, budget, true);
    }

    internal int GetLength()
    {
        return this.length;
    }

    internal int GetCapacity()
    {
        return this.bytes.Length;
    }

    internal int GetMaximumCapacity()
    {
        return this.maximumCapacity;
    }

    internal bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }

    internal byte GetByte(int index)
    {
        if (index < 0 || index >= this.length)
        {
            throw new ArgumentOutOfRangeException("index", index, "The byte index is outside the buffer.");
        }

        return this.bytes[index];
    }

    internal ReadOnlySpan<byte> GetSpan()
    {
        return new ReadOnlySpan<byte>(this.bytes, 0, this.length);
    }

    internal byte[] GetBytes()
    {
        byte[] result = new byte[this.length];
        Array.Copy(this.bytes, result, this.length);
        return result;
    }

    internal bool TryAppend(byte value)
    {
        if (!this.PrepareAppend(1))
        {
            return false;
        }

        this.bytes[this.length] = value;
        this.length = this.length + 1;
        return true;
    }

    internal bool TryAppendBytes(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return this.TryAppendSpan(value.AsSpan());
    }

    internal bool TryAppendSpan(ReadOnlySpan<byte> value)
    {
        int amount = value.Length;
        if (!this.PrepareAppend(amount))
        {
            return false;
        }

        value.CopyTo(new Span<byte>(this.bytes, this.length, amount));
        this.length = this.length + amount;
        return true;
    }

    internal void Clear()
    {
        this.length = 0;
    }

    internal void Truncate(int length)
    {
        if (length < 0 || length > this.length)
        {
            throw new ArgumentOutOfRangeException("length", length, "A truncation length must be within the current buffer.");
        }

        this.length = length;
    }

    internal bool PrepareAppend(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException("amount", amount, "An append length cannot be negative.");
        }

        HtmlBudget? optionalBudget = this.budget;
        if (this.usesInputBudget && optionalBudget == null)
        {
            throw new InvalidOperationException(
                "An input-budgeted buffer requires a budget.");
        }

        if (amount > this.maximumCapacity - this.length)
        {
            this.limitExceeded = true;
            if (this.usesInputBudget && optionalBudget != null)
            {
                optionalBudget.TryConsume(HtmlLimitKind.InputBytes, amount);
            }

            return false;
        }

        if (this.usesInputBudget && optionalBudget != null
            && !optionalBudget.TryConsume(HtmlLimitKind.InputBytes, amount))
        {
            this.limitExceeded = true;
            return false;
        }

        int required = this.length + amount;
        if (required > this.bytes.Length)
        {
            int capacity = BufferCapacity.GetNext(this.bytes.Length, required, this.maximumCapacity);
            byte[] expanded = new byte[capacity];
            Array.Copy(this.bytes, expanded, this.length);
            this.bytes = expanded;
        }

        return true;
    }
}
