using System;

namespace fake;
/// <summary>
/// Provides primitive value generation operations.
/// </summary>
public class PrimitiveProvider : IPrimitiveProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal PrimitiveProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("primitive");
    }

    /// <summary>
    /// Returns one decimal digit as a value from zero through nine.
    /// </summary>
    /// <returns>A uniformly generated decimal digit.</returns>
    public int Digit()
    {
        return NumericStringGenerator.Digit(RandomValue, 10);
    }

    /// <summary>
    /// Returns one digit value for a radix from 2 through 36.
    /// </summary>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <returns>A uniformly generated value from zero through one less than the radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>radix</c> is outside 2 through 36.</exception>
    public int DigitFromRadix(int radix)
    {
        return NumericStringGenerator.Digit(RandomValue, radix);
    }

    /// <summary>
    /// Returns a fixed-length string of independently generated decimal digits.
    /// </summary>
    /// <remarks>Leading zeroes are retained.</remarks>
    /// <param name="count">The number of digits to generate.</param>
    /// <returns>A decimal digit string of exactly the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Digits(int count)
    {
        return NumericStringGenerator.Digits(RandomValue, count, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a destination span with independently generated decimal digits.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteDigits(Span<char> destination)
    {
        return NumericStringGenerator.WriteDigits(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns a decimal representation of a value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A decimal numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval is empty or the possible output exceeds the configured limit.</exception>
    public string NumberString(long minimum, long maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(10, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns a decimal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <remarks>Padding counts magnitude digits and is placed after any sign.</remarks>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A decimal numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(10, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns a decimal representation of an unsigned value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned decimal numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval is empty or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromMinimumAndMaximum(ulong minimum, ulong maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(10, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an unsigned decimal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <remarks>Padding counts digits and is placed after an optional plus sign.</remarks>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned decimal numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(10, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns a representation in a radix from 2 through 36 using lowercase letter digits.
    /// </summary>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A numeric string in the requested radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromRadixAndMinimumAndMaximum(int radix, long minimum, long maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(radix, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns a representation in a radix from 2 through 36 with explicit bound, sign, and padding choices.
    /// </summary>
    /// <remarks>Letter digits are lowercase. Padding counts magnitude digits and is placed after any sign.</remarks>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A numeric string in the requested radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumericStringGenerator.Generate(RandomValue, radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an unsigned representation in a radix from 2 through 36 using lowercase letter digits.
    /// </summary>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned numeric string in the requested radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromRadixAndMinimumAndMaximumUsingInt32AndUInt64AndUInt64(int radix, ulong minimum, ulong maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(radix, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an unsigned representation in a radix from 2 through 36 with explicit bound, sign, and padding choices.
    /// </summary>
    /// <remarks>Letter digits are lowercase. Padding counts digits and is placed after an optional plus sign.</remarks>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned numeric string in the requested radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public string NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumericStringGenerator.Generate(RandomValue, radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns a binary representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A binary numeric string.</returns>
    public string Binary(long minimum, long maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(2, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns a binary representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A binary numeric string.</returns>
    public string BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(2, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns an unsigned binary representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned binary numeric string.</returns>
    public string BinaryFromMinimumAndMaximum(ulong minimum, ulong maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(2, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an unsigned binary representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned binary numeric string.</returns>
    public string BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(2, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns an octal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An octal numeric string.</returns>
    public string Octal(long minimum, long maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(8, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an octal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>An octal numeric string.</returns>
    public string OctalFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(8, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns an unsigned octal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned octal numeric string.</returns>
    public string OctalFromMinimumAndMaximum(ulong minimum, ulong maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(8, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an unsigned octal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned octal numeric string.</returns>
    public string OctalFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(8, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns a lowercase hexadecimal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A hexadecimal numeric string.</returns>
    public string Hex(long minimum, long maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(16, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns a lowercase hexadecimal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A hexadecimal numeric string.</returns>
    public string HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(16, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns an unsigned lowercase hexadecimal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned hexadecimal numeric string.</returns>
    public string HexFromMinimumAndMaximum(ulong minimum, ulong maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(16, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an unsigned lowercase hexadecimal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned hexadecimal numeric string.</returns>
    public string HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(16, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns a lowercase base-36 representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A base-36 numeric string.</returns>
    public string Base36(long minimum, long maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(36, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns a lowercase base-36 representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A base-36 numeric string.</returns>
    public string Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(36, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Returns an unsigned lowercase base-36 representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned base-36 numeric string.</returns>
    public string Base36FromMinimumAndMaximum(ulong minimum, ulong maximum)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(36, minimum, maximum, false, false, 0);
    }

    /// <summary>
    /// Returns an unsigned lowercase base-36 representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned base-36 numeric string.</returns>
    public string Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(36, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated decimal representation to a destination span.
    /// </summary>
    /// <remarks>The destination must accommodate the longest output permitted by the bounds and formatting.</remarks>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public int WriteNumberString(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination, 10, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated unsigned decimal representation to a destination span.
    /// </summary>
    /// <remarks>The destination must accommodate the longest output permitted by the bounds and formatting.</remarks>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public int WriteNumberStringFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(destination, 10, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated representation in a radix from 2 through 36 to a destination span.
    /// </summary>
    /// <remarks>Letter digits are lowercase. The destination must accommodate the longest output permitted by the bounds and formatting.</remarks>
    /// <param name="destination">The destination span.</param>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public int WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumericStringGenerator.Write(RandomValue, destination, radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Writes a generated unsigned representation in a radix from 2 through 36 to a destination span.
    /// </summary>
    /// <remarks>Letter digits are lowercase. The destination must accommodate the longest output permitted by the bounds and formatting.</remarks>
    /// <param name="destination">The destination span.</param>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    public int WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(Span<char> destination, int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return NumericStringGenerator.Write(RandomValue, destination, radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Writes a generated binary representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteBinary(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination, 2, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated unsigned binary representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteBinaryFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(destination, 2, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated octal representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteOctal(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination, 8, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated unsigned octal representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteOctalFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(destination, 8, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated lowercase hexadecimal representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteHex(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination, 16, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated unsigned lowercase hexadecimal representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteHexFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(destination, 16, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated lowercase base-36 representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteBase36(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(destination, 36, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Writes a generated unsigned lowercase base-36 representation to a destination span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>The number of characters written.</returns>
    public int WriteBase36FromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        return WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(destination, 36, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
    }

    /// <summary>
    /// Generates bytes and returns their canonical RFC 4648 Base16 encoding.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks.</remarks>
    /// <param name="byteCount">The number of random bytes to encode.</param>
    /// <returns>A Base16 string containing twice the requested byte count in characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>byteCount</c> is negative or the encoded output exceeds the configured limit.</exception>
    public string Base16(int byteCount)
    {
        BaseEncoding.Base16Length(byteCount, Context.CapturedOptions.MaximumOutputSize, "byteCount");
        byte[] source = new byte[byteCount];
        RandomValue.FillEntropy(new Span<byte>(source));
        return BaseEncoding.Base16(new ReadOnlySpan<byte>(source), Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns the canonical RFC 4648 Base16 encoding of a byte array.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The Base16 encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public string Base16FromSource(byte[] source)
    {
        Guard.NotNull(source, "source");
        return Base16FromSourceUsingReadOnlySpanOfByte(new ReadOnlySpan<byte>(source));
    }

    /// <summary>
    /// Returns the canonical RFC 4648 Base16 encoding of a read-only byte span.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The Base16 encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public string Base16FromSourceUsingReadOnlySpanOfByte(ReadOnlySpan<byte> source)
    {
        return BaseEncoding.Base16(source, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Writes the canonical RFC 4648 Base16 encoding of bytes to a destination span.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks. Characters after the encoded output are unchanged.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public int WriteBase16(ReadOnlySpan<byte> source, Span<char> destination)
    {
        return BaseEncoding.WriteBase16(source, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates bytes and returns their canonical RFC 4648 Base32 encoding.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="byteCount">The number of random bytes to encode.</param>
    /// <returns>A padded Base32 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>byteCount</c> is negative or the encoded output exceeds the configured limit.</exception>
    public string Base32(int byteCount)
    {
        BaseEncoding.Base32Length(byteCount, Context.CapturedOptions.MaximumOutputSize, "byteCount");
        byte[] source = new byte[byteCount];
        RandomValue.FillEntropy(new Span<byte>(source));
        return BaseEncoding.Base32(new ReadOnlySpan<byte>(source), Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns the canonical RFC 4648 Base32 encoding of a byte array.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base32 encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public string Base32FromSource(byte[] source)
    {
        Guard.NotNull(source, "source");
        return Base32FromSourceUsingReadOnlySpanOfByte(new ReadOnlySpan<byte>(source));
    }

    /// <summary>
    /// Returns the canonical RFC 4648 Base32 encoding of a read-only byte span.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base32 encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public string Base32FromSourceUsingReadOnlySpanOfByte(ReadOnlySpan<byte> source)
    {
        return BaseEncoding.Base32(source, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Writes the canonical RFC 4648 Base32 encoding of bytes to a destination span.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks. Characters after the encoded output are unchanged.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public int WriteBase32(ReadOnlySpan<byte> source, Span<char> destination)
    {
        return BaseEncoding.WriteBase32(source, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates bytes and returns their canonical RFC 4648 Base64 encoding.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="byteCount">The number of random bytes to encode.</param>
    /// <returns>A padded Base64 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>byteCount</c> is negative or the encoded output exceeds the configured limit.</exception>
    public string Base64(int byteCount)
    {
        BaseEncoding.Base64Length(byteCount, Context.CapturedOptions.MaximumOutputSize, "byteCount");
        byte[] source = new byte[byteCount];
        RandomValue.FillEntropy(new Span<byte>(source));
        return BaseEncoding.Base64(new ReadOnlySpan<byte>(source), Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns the canonical RFC 4648 Base64 encoding of a byte array.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base64 encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public string Base64FromSource(byte[] source)
    {
        Guard.NotNull(source, "source");
        return Base64FromSourceUsingReadOnlySpanOfByte(new ReadOnlySpan<byte>(source));
    }

    /// <summary>
    /// Returns the canonical RFC 4648 Base64 encoding of a read-only byte span.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base64 encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public string Base64FromSourceUsingReadOnlySpanOfByte(ReadOnlySpan<byte> source)
    {
        return BaseEncoding.Base64(source, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Writes the canonical RFC 4648 Base64 encoding of bytes to a destination span.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks. Characters after the encoded output are unchanged.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    public int WriteBase64(ReadOnlySpan<byte> source, Span<char> destination)
    {
        return BaseEncoding.WriteBase64(source, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length Base58 display string using the Bitcoin alphabet.
    /// </summary>
    /// <remarks>This is display generation, not a claim of compatibility with every Base58 binary encoding convention.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A Base58 display string of exactly the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Base58(int count)
    {
        return BaseEncoding.GenerateBase58(RandomValue, count, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a destination span with Base58 display characters from the Bitcoin alphabet.
    /// </summary>
    /// <remarks>This is display generation, not a claim of compatibility with every Base58 binary encoding convention.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteBase58(Span<char> destination)
    {
        return BaseEncoding.WriteBase58(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length string of mixed-case ASCII letters.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A string drawn uniformly from <c>A-Z</c> and <c>a-z</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Alpha(int count)
    {
        return CharacterStringGenerator.Generate(RandomValue, CharacterStringGenerator.AlphaAlphabet, count, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Returns an exact-length string of single-case ASCII letters.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <param name="uppercase">Whether to use <c>A-Z</c>; false uses <c>a-z</c>.</param>
    /// <returns>A uniformly generated ASCII letter string in the selected case.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string AlphaFromCountAndUppercase(int count, bool uppercase)
    {
        string alphabet = uppercase ? CharacterStringGenerator.UppercaseAlphabet : CharacterStringGenerator.LowercaseAlphabet;
        return CharacterStringGenerator.Generate(RandomValue, alphabet, count, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Fills a span with mixed-case ASCII letters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteAlpha(Span<char> destination)
    {
        return CharacterStringGenerator.Write(RandomValue, destination, CharacterStringGenerator.AlphaAlphabet, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Fills a span with single-case ASCII letters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <param name="uppercase">Whether to use <c>A-Z</c>; false uses <c>a-z</c>.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteAlphaFromDestinationAndUppercase(Span<char> destination, bool uppercase)
    {
        string alphabet = uppercase ? CharacterStringGenerator.UppercaseAlphabet : CharacterStringGenerator.LowercaseAlphabet;
        return CharacterStringGenerator.Write(RandomValue, destination, alphabet, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Returns an exact-length string of ASCII digits and mixed-case letters.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A string drawn uniformly from <c>0-9</c>, <c>A-Z</c>, and <c>a-z</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string AlphaNumeric(int count)
    {
        return CharacterStringGenerator.Generate(RandomValue, CharacterStringGenerator.AlphaNumericAlphabet, count, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Fills a span with ASCII digits and mixed-case letters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteAlphaNumeric(Span<char> destination)
    {
        return CharacterStringGenerator.Write(RandomValue, destination, CharacterStringGenerator.AlphaNumericAlphabet, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Returns an exact-length string of printable ASCII punctuation symbols.
    /// </summary>
    /// <remarks>Letters, digits, space, and control characters are excluded.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A uniformly generated ASCII punctuation string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Symbol(int count)
    {
        return CharacterStringGenerator.Generate(RandomValue, CharacterStringGenerator.SymbolAlphabet, count, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Fills a span with printable ASCII punctuation symbols.
    /// </summary>
    /// <remarks>Letters, digits, space, and control characters are excluded.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteSymbol(Span<char> destination)
    {
        return CharacterStringGenerator.Write(RandomValue, destination, CharacterStringGenerator.SymbolAlphabet, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Returns an exact-length string of ASCII whitespace characters.
    /// </summary>
    /// <remarks>The alphabet contains horizontal tab, line feed, vertical tab, form feed, carriage return, and space.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A uniformly generated ASCII whitespace string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Whitespace(int count)
    {
        return CharacterStringGenerator.Generate(RandomValue, CharacterStringGenerator.WhitespaceAlphabet, count, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Fills a span with ASCII whitespace characters.
    /// </summary>
    /// <remarks>The alphabet contains horizontal tab, line feed, vertical tab, form feed, carriage return, and space.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WriteWhitespace(Span<char> destination)
    {
        return CharacterStringGenerator.Write(RandomValue, destination, CharacterStringGenerator.WhitespaceAlphabet, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Returns an exact-length string of printable ASCII characters.
    /// </summary>
    /// <remarks>The alphabet is U+0020 space through U+007E tilde.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A uniformly generated printable ASCII string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Printable(int count)
    {
        return CharacterStringGenerator.Generate(RandomValue, CharacterStringGenerator.PrintableAlphabet, count, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Fills a span with printable ASCII characters.
    /// </summary>
    /// <remarks>The alphabet is U+0020 space through U+007E tilde.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    public int WritePrintable(Span<char> destination)
    {
        return CharacterStringGenerator.Write(RandomValue, destination, CharacterStringGenerator.PrintableAlphabet, true, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, false);
    }

    /// <summary>
    /// Returns an exact-length string sampled uniformly from a validated alphabet with replacement.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <param name="alphabet">The nonempty alphabet of distinct, non-surrogate UTF-16 characters.</param>
    /// <returns>A string containing characters from the supplied alphabet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>alphabet</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>alphabet</c> is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an input exceeds a configured limit or <c>count</c> is negative.</exception>
    public string FromCharacters(int count, string alphabet)
    {
        return FromCharactersFromCountAndAlphabetAndWithReplacement(count, alphabet, true);
    }

    /// <summary>
    /// Returns an exact-length string sampled uniformly from a validated alphabet.
    /// </summary>
    /// <remarks>Without replacement, every output character is distinct and every ordered sample has equal probability.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <param name="alphabet">The nonempty alphabet of distinct, non-surrogate UTF-16 characters.</param>
    /// <param name="withReplacement">Whether an alphabet character may be selected more than once.</param>
    /// <returns>A string containing characters from the supplied alphabet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>alphabet</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>alphabet</c> is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an input exceeds a configured limit, <c>count</c> is negative, or the alphabet is insufficient without replacement.</exception>
    public string FromCharactersFromCountAndAlphabetAndWithReplacement(int count, string alphabet, bool withReplacement)
    {
        return CharacterStringGenerator.Generate(RandomValue, alphabet, count, withReplacement, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, true);
    }

    /// <summary>
    /// Fills a span by sampling uniformly from a validated alphabet with replacement.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <param name="alphabet">The nonempty alphabet of distinct, non-surrogate UTF-16 characters.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>alphabet</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>alphabet</c> is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an input exceeds a configured limit.</exception>
    public int WriteFromCharacters(Span<char> destination, string alphabet)
    {
        return WriteFromCharactersFromDestinationAndAlphabetAndWithReplacement(destination, alphabet, true);
    }

    /// <summary>
    /// Fills a span by sampling uniformly from a validated alphabet.
    /// </summary>
    /// <remarks>Without replacement, every output character is distinct and every ordered sample has equal probability.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <param name="alphabet">The nonempty alphabet of distinct, non-surrogate UTF-16 characters.</param>
    /// <param name="withReplacement">Whether an alphabet character may be selected more than once.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>alphabet</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>alphabet</c> is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an input exceeds a configured limit or the alphabet is insufficient without replacement.</exception>
    public int WriteFromCharactersFromDestinationAndAlphabetAndWithReplacement(Span<char> destination, string alphabet, bool withReplacement)
    {
        return CharacterStringGenerator.Write(RandomValue, destination, alphabet, withReplacement, Context.CapturedOptions.MaximumOutputSize, Context.CapturedOptions.MaximumCount, true);
    }

    /// <summary>
    /// Returns an exact number of random bytes from the configured random mode.
    /// </summary>
    /// <param name="count">The number of bytes to generate.</param>
    /// <returns>A new array containing the generated bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public byte[] Bytes(int count)
    {
        return SecretGenerator.Bytes(RandomValue, count, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a byte span from the configured random mode.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public int WriteBytes(Span<byte> destination)
    {
        return SecretGenerator.WriteBytes(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length lowercase hexadecimal string.
    /// </summary>
    /// <param name="count">The number of hexadecimal characters to generate.</param>
    /// <returns>A uniformly generated lowercase hexadecimal string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string HexFromCount(int count)
    {
        return SecretGenerator.Hex(RandomValue, count, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a span with lowercase hexadecimal characters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public int WriteHexFromDestination(Span<char> destination)
    {
        return SecretGenerator.WriteHex(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns a lowercase hexadecimal value with a requested display length.
    /// </summary>
    /// <remarks>The result is hash-shaped random data, not a digest of input.</remarks>
    /// <param name="count">The number of hexadecimal characters to generate.</param>
    /// <returns>A uniformly generated hash-shaped string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Hash(int count)
    {
        return SecretGenerator.Hex(RandomValue, count, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a span with lowercase hexadecimal hash-shaped random data.
    /// </summary>
    /// <remarks>The result is not a digest of input.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public int WriteHash(Span<char> destination)
    {
        return SecretGenerator.WriteHex(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length opaque token using the unpadded Base64 URL alphabet.
    /// </summary>
    /// <remarks>Use cryptographic random mode when the token will protect a real secret or capability.</remarks>
    /// <param name="count">The number of token characters to generate.</param>
    /// <returns>A uniformly generated opaque token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string Token(int count)
    {
        return SecretGenerator.Token(RandomValue, count, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a span with opaque token characters from the unpadded Base64 URL alphabet.
    /// </summary>
    /// <remarks>Use cryptographic random mode when the token will protect a real secret or capability.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public int WriteToken(Span<char> destination)
    {
        return SecretGenerator.WriteToken(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length decimal PIN string, retaining leading zeroes.
    /// </summary>
    /// <remarks>Use cryptographic random mode for a PIN used outside deterministic fixture data.</remarks>
    /// <param name="digits">The number of decimal digits to generate.</param>
    /// <returns>A uniformly generated PIN string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>digits</c> is negative or exceeds the configured output limit.</exception>
    public string Pin(int digits)
    {
        return SecretGenerator.Pin(RandomValue, digits, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a span with decimal PIN digits, retaining leading zeroes.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public int WritePin(Span<char> destination)
    {
        return SecretGenerator.WritePin(RandomValue, destination, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns four groups of four unambiguous uppercase recovery-code characters.
    /// </summary>
    /// <returns>A recovery code whose groups are separated by hyphens.</returns>
    public string RecoveryCode()
    {
        return RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(4, 4, '-');
    }

    /// <summary>
    /// Returns an explicitly grouped recovery code using unambiguous uppercase characters.
    /// </summary>
    /// <param name="groupCount">The number of groups.</param>
    /// <param name="groupLength">The number of characters per group.</param>
    /// <param name="separator">The printable ASCII punctuation separator.</param>
    /// <returns>The grouped recovery code.</returns>
    /// <exception cref="ArgumentException">Thrown when the separator is unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a count is invalid or the output exceeds a configured limit.</exception>
    public string RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(int groupCount, int groupLength, char separator)
    {
        return SecretGenerator.RecoveryCode(RandomValue, groupCount, groupLength, separator, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills an exactly sized span with an explicitly grouped recovery code.
    /// </summary>
    /// <param name="destination">The span whose length must equal the requested recovery-code shape.</param>
    /// <param name="groupCount">The number of groups.</param>
    /// <param name="groupLength">The number of characters per group.</param>
    /// <param name="separator">The printable ASCII punctuation separator.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentException">Thrown when the separator is unsupported or the destination length is not exact.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a count is invalid or the output exceeds a configured limit.</exception>
    public int WriteRecoveryCode(Span<char> destination, int groupCount, int groupLength, char separator)
    {
        return SecretGenerator.WriteRecoveryCode(RandomValue, destination, groupCount, groupLength, separator, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length password containing lowercase, uppercase, digit, and symbol characters.
    /// </summary>
    /// <remarks>At least one character from every enabled class is guaranteed. Use cryptographic random mode for a password used outside deterministic fixture data.</remarks>
    /// <param name="length">The password length, which must be at least four.</param>
    /// <returns>The generated password.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length is too short or exceeds the configured output limit.</exception>
    public string Password(int length)
    {
        return PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(length, true, true, true, true);
    }

    /// <summary>
    /// Returns an exact-length password with explicit required character classes.
    /// </summary>
    /// <param name="length">The password length.</param>
    /// <param name="lowercase">Whether lowercase ASCII letters are enabled and required.</param>
    /// <param name="uppercase">Whether uppercase ASCII letters are enabled and required.</param>
    /// <param name="digits">Whether decimal digits are enabled and required.</param>
    /// <param name="symbols">Whether printable ASCII password symbols are enabled and required.</param>
    /// <returns>The generated password.</returns>
    /// <exception cref="ArgumentException">Thrown when no character class is enabled.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length cannot include all enabled classes or exceeds the configured output limit.</exception>
    public string PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(int length, bool lowercase, bool uppercase, bool digits, bool symbols)
    {
        return SecretGenerator.Password(RandomValue, length, lowercase, uppercase, digits, symbols, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills a span with a password requiring every standard character class.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination is too short or exceeds the configured output limit.</exception>
    public int WritePassword(Span<char> destination)
    {
        return WritePasswordFromDestinationAndLowercaseAndUppercaseAndDigitsAndSymbols(destination, true, true, true, true);
    }

    /// <summary>
    /// Fills a span with a password using explicit required character classes.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <param name="lowercase">Whether lowercase ASCII letters are enabled and required.</param>
    /// <param name="uppercase">Whether uppercase ASCII letters are enabled and required.</param>
    /// <param name="digits">Whether decimal digits are enabled and required.</param>
    /// <param name="symbols">Whether printable ASCII password symbols are enabled and required.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentException">Thrown when no character class is enabled.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination cannot include all enabled classes or exceeds the configured output limit.</exception>
    public int WritePasswordFromDestinationAndLowercaseAndUppercaseAndDigitsAndSymbols(Span<char> destination, bool lowercase, bool uppercase, bool digits, bool symbols)
    {
        return SecretGenerator.WritePassword(RandomValue, destination, lowercase, uppercase, digits, symbols, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns a passphrase containing an exact number of authored five-letter words separated by hyphens.
    /// </summary>
    /// <remarks>Words are independently selected with replacement. Use cryptographic random mode for a passphrase used outside deterministic fixture data.</remarks>
    /// <param name="wordCount">The number of words to generate.</param>
    /// <returns>The generated passphrase.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the word count or output exceeds a configured limit.</exception>
    public string Passphrase(int wordCount)
    {
        return PassphraseFromWordCountAndSeparator(wordCount, "-");
    }

    /// <summary>
    /// Returns a passphrase containing an exact number of authored five-letter words.
    /// </summary>
    /// <param name="wordCount">The number of words to generate.</param>
    /// <param name="separator">The well-formed UTF-16 separator placed between words.</param>
    /// <returns>The generated passphrase.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>separator</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>separator</c> contains unpaired surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the word count or output exceeds a configured limit.</exception>
    public string PassphraseFromWordCountAndSeparator(int wordCount, string separator)
    {
        return SecretGenerator.Passphrase(RandomValue, wordCount, separator, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Fills an exactly sized span with a passphrase of authored five-letter words.
    /// </summary>
    /// <param name="destination">The span whose length must equal the requested passphrase shape.</param>
    /// <param name="wordCount">The number of words to generate.</param>
    /// <param name="separator">The well-formed UTF-16 separator placed between words.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>separator</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the separator is malformed or the destination length is not exact.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the word count or output exceeds a configured limit.</exception>
    public int WritePassphrase(Span<char> destination, int wordCount, string separator)
    {
        return SecretGenerator.WritePassphrase(RandomValue, destination, wordCount, separator, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns a uniformly generated Unicode scalar value from the full scalar range.
    /// </summary>
    /// <remarks>Unicode scalar values exclude the UTF-16 surrogate range. Unassigned code points and noncharacters remain valid scalar values and may be returned.</remarks>
    /// <returns>An integer from U+0000 through U+10FFFF excluding surrogates.</returns>
    public int UnicodeScalar()
    {
        return UnicodeStringGenerator.Scalar(RandomValue);
    }

    /// <summary>
    /// Returns a uniformly generated Unicode scalar between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The exclusive maximum Unicode scalar.</param>
    /// <returns>A scalar within the requested range, with surrogates skipped uniformly.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is not a scalar or the interval is empty.</exception>
    public int UnicodeScalarFromMinimumScalarAndMaximumScalar(int minimumScalar, int maximumScalar)
    {
        return UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(minimumScalar, maximumScalar, false);
    }

    /// <summary>
    /// Returns a uniformly generated Unicode scalar with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The maximum Unicode scalar.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A scalar within the requested range, with surrogates skipped uniformly.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is not a scalar or the interval is empty.</exception>
    public int UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(int minimumScalar, int maximumScalar, bool maximumInclusive)
    {
        return UnicodeStringGenerator.Scalar(RandomValue, minimumScalar, maximumScalar, maximumInclusive);
    }

    /// <summary>
    /// Returns a string containing an exact number of uniformly generated Unicode scalar values.
    /// </summary>
    /// <remarks>Scalar count differs from UTF-16 length because supplementary scalars are encoded as surrogate pairs. Unassigned code points and noncharacters may be returned.</remarks>
    /// <param name="count">The number of Unicode scalar values to generate.</param>
    /// <returns>A well-formed UTF-16 string containing exactly the requested scalar count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or the possible UTF-16 output exceeds the configured limit.</exception>
    public string Unicode(int count)
    {
        return UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(count, 0, 0x10FFFF, true);
    }

    /// <summary>
    /// Returns a string of Unicode scalars between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="count">The number of Unicode scalar values to generate.</param>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The exclusive maximum Unicode scalar.</param>
    /// <returns>A well-formed UTF-16 string containing exactly the requested scalar count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible UTF-16 output exceeds the configured limit.</exception>
    public string UnicodeFromCountAndMinimumScalarAndMaximumScalar(int count, int minimumScalar, int maximumScalar)
    {
        return UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(count, minimumScalar, maximumScalar, false);
    }

    /// <summary>
    /// Returns a string of Unicode scalars with an explicit maximum-bound choice.
    /// </summary>
    /// <remarks>Surrogate code points are skipped uniformly when a valid range crosses the surrogate gap.</remarks>
    /// <param name="count">The number of Unicode scalar values to generate.</param>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The maximum Unicode scalar.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A well-formed UTF-16 string containing exactly the requested scalar count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible UTF-16 output exceeds the configured limit.</exception>
    public string UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(int count, int minimumScalar, int maximumScalar, bool maximumInclusive)
    {
        return UnicodeStringGenerator.Generate(RandomValue, count, minimumScalar, maximumScalar, maximumInclusive, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length string from a supported registered Unicode script repertoire.
    /// </summary>
    /// <remarks>The count is both the scalar count and UTF-16 length because current script repertoires contain BMP letters.</remarks>
    /// <param name="count">The number of script characters to generate.</param>
    /// <param name="script">The registered four-letter IANA script subtag.</param>
    /// <returns>A string containing characters from the requested script repertoire.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>script</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>script</c> is empty, unregistered, or unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string UnicodeFromCountAndScript(int count, string script)
    {
        return UnicodeStringGenerator.GenerateScript(RandomValue, count, script, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Returns an exact-length string from the configured locale's resolved script repertoire.
    /// </summary>
    /// <remarks>An explicit locale script wins; otherwise the IANA suppress-script is used. Chinese regions use Hans except TW, HK, and MO, which use Hant.</remarks>
    /// <param name="count">The number of script characters to generate.</param>
    /// <returns>A string containing characters from the configured locale script repertoire.</returns>
    /// <exception cref="ArgumentException">Thrown when the locale does not identify a supported default script.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string LocaleScript(int count)
    {
        return LocaleScriptFromCountAndLocale(count, Context.CapturedOptions.Locale);
    }

    /// <summary>
    /// Returns an exact-length string from an explicit locale's resolved script repertoire.
    /// </summary>
    /// <remarks>An explicit locale script wins; otherwise the IANA suppress-script is used. Chinese regions use Hans except TW, HK, and MO, which use Hant.</remarks>
    /// <param name="count">The number of script characters to generate.</param>
    /// <param name="locale">The BCP 47 locale whose script should be resolved.</param>
    /// <returns>A string containing characters from the resolved locale script repertoire.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>locale</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>locale</c> is malformed or does not identify a supported default script.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public string LocaleScriptFromCountAndLocale(int count, string locale)
    {
        return UnicodeStringGenerator.GenerateLocaleScript(RandomValue, count, locale, Context.CapturedOptions.MaximumOutputSize);
    }
}
