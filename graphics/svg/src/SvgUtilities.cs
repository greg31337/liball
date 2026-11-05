namespace svg;

/// <summary>Contains the result of a finite arithmetic operation.</summary>
internal class SvgFiniteResult
{
    /// <summary>Indicates whether the operation produced a finite value.</summary>
    public bool Success;

    /// <summary>The normalized finite value, or zero after failure.</summary>
    public double Value;

    /// <summary>An empty string after success or a stable failure description.</summary>
    public string Error;

    /// <summary>Creates a finite arithmetic result.</summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="value">The result value.</param>
    /// <param name="error">The failure description, or an empty string.</param>
    public SvgFiniteResult(bool success, double value, string error)
    {
        Success = success;
        Value = value;
        Error = error;
    }
}

/// <summary>Provides finite-only floating-point arithmetic helpers.</summary>
internal static class SvgFiniteMath
{
    /// <summary>Tests whether a value is neither NaN nor infinity.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns><c>true</c> for a finite value.</returns>
    public static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>Converts negative zero to positive zero.</summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>Positive zero for either signed zero; otherwise the original value.</returns>
    public static double NormalizeZero(double value)
    {
        if (value == 0)
        {
            return 0;
        }
        return value;
    }

    /// <summary>Adds two finite values and rejects non-finite input or output.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The finite result.</returns>
    public static SvgFiniteResult Add(double left, double right)
    {
        if (!IsFinite(left) || !IsFinite(right))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic operands must be finite.");
        }
        double value = left + right;
        if (!IsFinite(value))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic result is not finite.");
        }
        return new SvgFiniteResult(true, NormalizeZero(value), "");
    }

    /// <summary>Subtracts two finite values and rejects non-finite input or output.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The finite result.</returns>
    public static SvgFiniteResult Subtract(double left, double right)
    {
        if (!IsFinite(left) || !IsFinite(right))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic operands must be finite.");
        }
        double value = left - right;
        if (!IsFinite(value))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic result is not finite.");
        }
        return new SvgFiniteResult(true, NormalizeZero(value), "");
    }

    /// <summary>Multiplies two finite values and rejects non-finite input or output.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The finite result.</returns>
    public static SvgFiniteResult Multiply(double left, double right)
    {
        if (!IsFinite(left) || !IsFinite(right))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic operands must be finite.");
        }
        double value = left * right;
        if (!IsFinite(value))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic result is not finite.");
        }
        return new SvgFiniteResult(true, NormalizeZero(value), "");
    }

    /// <summary>Divides two finite values and rejects zero divisors and non-finite output.</summary>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    /// <returns>The finite result.</returns>
    public static SvgFiniteResult Divide(double left, double right)
    {
        if (!IsFinite(left) || !IsFinite(right))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic operands must be finite.");
        }
        if (right == 0)
        {
            return new SvgFiniteResult(false, 0, "Division by zero is not allowed.");
        }
        double value = left / right;
        if (!IsFinite(value))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic result is not finite.");
        }
        return new SvgFiniteResult(true, NormalizeZero(value), "");
    }

    /// <summary>Clamps a finite value to an ordered finite interval.</summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>The finite clamped result.</returns>
    public static SvgFiniteResult Clamp(double value, double minimum, double maximum)
    {
        if (!IsFinite(value) || !IsFinite(minimum) || !IsFinite(maximum))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic operands must be finite.");
        }
        if (minimum > maximum)
        {
            return new SvgFiniteResult(false, 0, "The minimum must not exceed the maximum.");
        }
        if (value < minimum)
        {
            return new SvgFiniteResult(true, NormalizeZero(minimum), "");
        }
        if (value > maximum)
        {
            return new SvgFiniteResult(true, NormalizeZero(maximum), "");
        }
        return new SvgFiniteResult(true, NormalizeZero(value), "");
    }

    /// <summary>Linearly interpolates finite endpoints by a finite amount.</summary>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="amount">The interpolation amount.</param>
    /// <returns>The finite interpolated result.</returns>
    public static SvgFiniteResult Lerp(double start, double end, double amount)
    {
        if (!IsFinite(start) || !IsFinite(end) || !IsFinite(amount))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic operands must be finite.");
        }
        if (amount == 0)
        {
            return new SvgFiniteResult(true, NormalizeZero(start), "");
        }
        if (amount == 1)
        {
            return new SvgFiniteResult(true, NormalizeZero(end), "");
        }
        double value = start * (1 - amount) + end * amount;
        if (!IsFinite(value))
        {
            return new SvgFiniteResult(false, 0, "Arithmetic result is not finite.");
        }
        return new SvgFiniteResult(true, NormalizeZero(value), "");
    }
}

/// <summary>Provides culture-independent ordinal name operations.</summary>
internal static class SvgOrdinalNames
{
    /// <summary>Tests two strings with ordinal case-sensitive semantics.</summary>
    /// <param name="left">The first string, or <c>null</c>.</param>
    /// <param name="right">The second string, or <c>null</c>.</param>
    /// <returns><c>true</c> when the strings are ordinally equal.</returns>
    public static bool Equal(string? left, string? right)
    {
        return string.Equals(left, right, System.StringComparison.Ordinal);
    }

    /// <summary>Compares two strings with ordinal case-sensitive semantics.</summary>
    /// <param name="left">The first string, or <c>null</c>.</param>
    /// <param name="right">The second string, or <c>null</c>.</param>
    /// <returns>A negative value, zero, or a positive value in ordinal order.</returns>
    public static int Compare(string? left, string? right)
    {
        return string.Compare(left, right, System.StringComparison.Ordinal);
    }

    /// <summary>Finds a name in an unsorted array by ordinal equality.</summary>
    /// <param name="names">The array to scan.</param>
    /// <param name="name">The non-null name to find.</param>
    /// <returns>The first matching index, or <c>-1</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public static int Find(string[] names, string name)
    {
        if (names == null)
        {
            throw new System.ArgumentNullException("names");
        }
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(names[index], name, System.StringComparison.Ordinal))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    /// <summary>Finds a name in an ordinally sorted array by binary search.</summary>
    /// <param name="sortedNames">The ordinally sorted array.</param>
    /// <param name="name">The non-null name to find.</param>
    /// <returns>The matching index, or <c>-1</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public static int FindSorted(string[] sortedNames, string name)
    {
        if (sortedNames == null)
        {
            throw new System.ArgumentNullException("sortedNames");
        }
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        int low = 0;
        int high = sortedNames.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int order = string.Compare(sortedNames[middle], name, System.StringComparison.Ordinal);
            if (order < 0)
            {
                low = middle + 1;
            }
            else if (order > 0)
            {
                high = middle - 1;
            }
            else
            {
                return middle;
            }
        }
        return -1;
    }
}

/// <summary>Provides explicit allocation-free scans for common primitive collections.</summary>
internal static class SvgCollectionScan
{
    /// <summary>Finds an integer in an array.</summary>
    /// <param name="values">The array to scan.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>The first matching index, or <c>-1</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is <c>null</c>.</exception>
    public static int IndexOfInt32(int[] values, int value)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    /// <summary>Finds a byte in a span.</summary>
    /// <param name="values">The span to scan.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>The first matching index, or <c>-1</c>.</returns>
    public static int IndexOfByte(System.ReadOnlySpan<byte> values, byte value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    /// <summary>Finds a character in a span.</summary>
    /// <param name="values">The span to scan.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>The first matching index, or <c>-1</c>.</returns>
    public static int IndexOfCharacter(System.ReadOnlySpan<char> values, char value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    /// <summary>Tests two byte spans for exact equality.</summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><c>true</c> when lengths and bytes are equal.</returns>
    public static bool EqualBytes(System.ReadOnlySpan<byte> left, System.ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    /// <summary>Tests two integer arrays for exact equality.</summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><c>true</c> when lengths and values are equal.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public static bool EqualInt32(int[] left, int[] right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }
        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    /// <summary>Finds an object by reference identity in an array.</summary>
    /// <param name="values">The array to scan.</param>
    /// <param name="value">The reference to find, including <c>null</c>.</param>
    /// <returns>The first reference-identical index, or <c>-1</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is <c>null</c>.</exception>
    public static int IndexOfReference(object[] values, object? value)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        int index = 0;
        while (index < values.Length)
        {
            if (object.ReferenceEquals(values[index], value))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }
}

/// <summary>Provides deterministic FNV-1a hashes without randomized runtime hashing.</summary>
internal static class SvgStableHash
{
    internal const ulong OffsetBasis = 14695981039346656037;
    internal const ulong Prime = 1099511628211;

    /// <summary>Hashes bytes using 64-bit FNV-1a.</summary>
    /// <param name="values">The bytes to hash.</param>
    /// <returns>The deterministic 64-bit hash.</returns>
    public static ulong HashBytes(System.ReadOnlySpan<byte> values)
    {
        ulong hash = OffsetBasis;
        int index = 0;
        while (index < values.Length)
        {
            hash = hash ^ values[index];
            hash = MultiplyPrime(hash);
            index = index + 1;
        }
        return hash;
    }

    /// <summary>Hashes UTF-16 code units in fixed little-endian byte order.</summary>
    /// <param name="value">The non-null string to hash.</param>
    /// <returns>The deterministic 64-bit hash.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>value</c> is <c>null</c>.</exception>
    public static ulong HashString(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        ulong hash = OffsetBasis;
        int index = 0;
        while (index < value.Length)
        {
            int unit = value[index];
            hash = hash ^ (byte)(unit & 255);
            hash = MultiplyPrime(hash);
            hash = hash ^ (byte)((unit >> 8) & 255);
            hash = MultiplyPrime(hash);
            index = index + 1;
        }
        return hash;
    }

    /// <summary>Hashes a signed 64-bit integer in fixed little-endian byte order.</summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The deterministic 64-bit hash.</returns>
    public static ulong HashInt64(long value)
    {
        ulong bits = value >= 0 ? (ulong)value : 9223372036854775808UL + (ulong)(value - long.MinValue);
        ulong hash = OffsetBasis;
        int index = 0;
        while (index < 8)
        {
            hash = hash ^ (byte)(bits & 255);
            hash = MultiplyPrime(hash);
            bits = bits >> 8;
            index = index + 1;
        }
        return hash;
    }

    /// <summary>Hashes a double after canonicalizing signed zero and NaN.</summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The deterministic 64-bit hash.</returns>
    public static ulong HashDouble(double value)
    {
        long bits;
        if (value == 0)
        {
            bits = 0;
        }
        else if (double.IsNaN(value))
        {
            bits = 9221120237041090560;
        }
        else
        {
            bits = System.BitConverter.DoubleToInt64Bits(value);
        }
        return HashInt64(bits);
    }

    /// <summary>Combines an existing hash with another 64-bit value.</summary>
    /// <param name="hash">The existing hash.</param>
    /// <param name="value">The value to append in little-endian order.</param>
    /// <returns>The combined deterministic hash.</returns>
    public static ulong Combine(ulong hash, ulong value)
    {
        int index = 0;
        while (index < 8)
        {
            hash = hash ^ (byte)(value & 255);
            hash = MultiplyPrime(hash);
            value = value >> 8;
            index = index + 1;
        }
        return hash;
    }

    static ulong MultiplyPrime(ulong value)
    {
        const ulong mask = 4294967295UL;
        ulong low = value & mask;
        ulong high = value >> 32;
        ulong lowProduct = low * 435;
        ulong crossProduct = low * 256 + high * 435;
        ulong resultHigh = (lowProduct >> 32) + (crossProduct & mask);
        return (lowProduct & mask) | (resultHigh & mask) << 32;
    }
}

/// <summary>Checks cancellation no later than a configured number of work units.</summary>
internal class SvgCancellationCheck
{
    /// <summary>The caller cancellation token.</summary>
    public System.Threading.CancellationToken Token;

    /// <summary>The positive number of work units between automatic checks.</summary>
    public int Interval;

    /// <summary>The work units remaining before the next automatic check.</summary>
    public int Remaining;

    /// <summary>The total work units observed since construction or reset.</summary>
    public long WorkCount;

    /// <summary>Creates a bounded-interval cancellation checker.</summary>
    /// <param name="token">The caller cancellation token.</param>
    /// <param name="interval">The positive work-unit interval.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>interval</c> is not positive.</exception>
    public SvgCancellationCheck(System.Threading.CancellationToken token, int interval)
    {
        if (interval <= 0)
        {
            throw new System.ArgumentOutOfRangeException("interval");
        }
        Token = token;
        Interval = interval;
        Remaining = interval;
        WorkCount = 0;
    }

    /// <summary>Records one work unit and checks the token when the interval expires.</summary>
    /// <returns><c>true</c> when this call checked the token; otherwise <c>false</c>.</returns>
    /// <exception cref="System.OperationCanceledException">Thrown when cancellation is requested at a check.</exception>
    public bool Tick()
    {
        if (WorkCount < long.MaxValue)
        {
            WorkCount = WorkCount + 1;
        }
        Remaining = Remaining - 1;
        if (Remaining > 0)
        {
            return false;
        }

        Remaining = Interval;
        Token.ThrowIfCancellationRequested();
        return true;
    }

    /// <summary>Checks the token immediately without changing interval state.</summary>
    /// <exception cref="System.OperationCanceledException">Thrown when cancellation is requested.</exception>
    public void CheckNow()
    {
        Token.ThrowIfCancellationRequested();
    }

    /// <summary>Resets the interval and observed work count.</summary>
    public void Reset()
    {
        Remaining = Interval;
        WorkCount = 0;
    }
}
