using System;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="PrimitiveProvider"/>.</summary>
public interface IPrimitiveProvider
{
    /// <summary>
    /// Returns one decimal digit as a value from zero through nine.
    /// </summary>
    /// <returns>A uniformly generated decimal digit.</returns>
    int Digit();
    /// <summary>
    /// Returns one digit value for a radix from 2 through 36.
    /// </summary>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <returns>A uniformly generated value from zero through one less than the radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>radix</c> is outside 2 through 36.</exception>
    int DigitFromRadix(int radix);
    /// <summary>
    /// Returns a fixed-length string of independently generated decimal digits.
    /// </summary>
    /// <remarks>Leading zeroes are retained.</remarks>
    /// <param name="count">The number of digits to generate.</param>
    /// <returns>A decimal digit string of exactly the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Digits(int count);
    /// <summary>
    /// Fills a destination span with independently generated decimal digits.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteDigits(Span<char> destination);
    /// <summary>
    /// Returns a decimal representation of a value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A decimal numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval is empty or the possible output exceeds the configured limit.</exception>
    string NumberString(long minimum, long maximum);
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
    string NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns a decimal representation of an unsigned value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned decimal numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the interval is empty or the possible output exceeds the configured limit.</exception>
    string NumberStringFromMinimumAndMaximum(ulong minimum, ulong maximum);
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
    string NumberStringFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns a representation in a radix from 2 through 36 using lowercase letter digits.
    /// </summary>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A numeric string in the requested radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    string NumberStringFromRadixAndMinimumAndMaximum(int radix, long minimum, long maximum);
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
    string NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns an unsigned representation in a radix from 2 through 36 using lowercase letter digits.
    /// </summary>
    /// <param name="radix">The number of digits in the numeral system.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned numeric string in the requested radix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible output exceeds the configured limit.</exception>
    string NumberStringFromRadixAndMinimumAndMaximumUsingInt32AndUInt64AndUInt64(int radix, ulong minimum, ulong maximum);
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
    string NumberStringFromRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns a binary representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A binary numeric string.</returns>
    string Binary(long minimum, long maximum);
    /// <summary>
    /// Returns a binary representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A binary numeric string.</returns>
    string BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns an unsigned binary representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned binary numeric string.</returns>
    string BinaryFromMinimumAndMaximum(ulong minimum, ulong maximum);
    /// <summary>
    /// Returns an unsigned binary representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned binary numeric string.</returns>
    string BinaryFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns an octal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An octal numeric string.</returns>
    string Octal(long minimum, long maximum);
    /// <summary>
    /// Returns an octal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>An octal numeric string.</returns>
    string OctalFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns an unsigned octal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned octal numeric string.</returns>
    string OctalFromMinimumAndMaximum(ulong minimum, ulong maximum);
    /// <summary>
    /// Returns an unsigned octal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned octal numeric string.</returns>
    string OctalFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns a lowercase hexadecimal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A hexadecimal numeric string.</returns>
    string Hex(long minimum, long maximum);
    /// <summary>
    /// Returns a lowercase hexadecimal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A hexadecimal numeric string.</returns>
    string HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns an unsigned lowercase hexadecimal representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned hexadecimal numeric string.</returns>
    string HexFromMinimumAndMaximum(ulong minimum, ulong maximum);
    /// <summary>
    /// Returns an unsigned lowercase hexadecimal representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned hexadecimal numeric string.</returns>
    string HexFromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns a lowercase base-36 representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A base-36 numeric string.</returns>
    string Base36(long minimum, long maximum);
    /// <summary>
    /// Returns a lowercase base-36 representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether nonnegative values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of magnitude digits.</param>
    /// <returns>A base-36 numeric string.</returns>
    string Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Returns an unsigned lowercase base-36 representation between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>An unsigned base-36 numeric string.</returns>
    string Base36FromMinimumAndMaximum(ulong minimum, ulong maximum);
    /// <summary>
    /// Returns an unsigned lowercase base-36 representation with explicit bound, sign, and padding choices.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <param name="alwaysSign">Whether values receive a leading plus sign.</param>
    /// <param name="minimumDigits">The minimum number of digits.</param>
    /// <returns>An unsigned base-36 numeric string.</returns>
    string Base36FromMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingUInt64AndUInt64AndBooleanAndBooleanAndInt32(ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteNumberString(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteNumberStringFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteNumberStringFromDestinationAndRadixAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigitsUsingSpanOfCharAndInt32AndUInt64AndUInt64AndBooleanAndBooleanAndInt32(Span<char> destination, int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteBinary(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteBinaryFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteOctal(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteOctalFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteHex(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteHexFromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteBase36(Span<char> destination, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
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
    int WriteBase36FromDestinationAndMinimumAndMaximumAndMaximumInclusiveAndAlwaysSignAndMinimumDigits(Span<char> destination, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits);
    /// <summary>
    /// Generates bytes and returns their canonical RFC 4648 Base16 encoding.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks.</remarks>
    /// <param name="byteCount">The number of random bytes to encode.</param>
    /// <returns>A Base16 string containing twice the requested byte count in characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>byteCount</c> is negative or the encoded output exceeds the configured limit.</exception>
    string Base16(int byteCount);
    /// <summary>
    /// Returns the canonical RFC 4648 Base16 encoding of a byte array.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The Base16 encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string Base16FromSource(byte[] source);
    /// <summary>
    /// Returns the canonical RFC 4648 Base16 encoding of a read-only byte span.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The Base16 encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string Base16FromSourceUsingReadOnlySpanOfByte(ReadOnlySpan<byte> source);
    /// <summary>
    /// Writes the canonical RFC 4648 Base16 encoding of bytes to a destination span.
    /// </summary>
    /// <remarks>The output uses uppercase letters and has no padding or line breaks. Characters after the encoded output are unchanged.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    int WriteBase16(ReadOnlySpan<byte> source, Span<char> destination);
    /// <summary>
    /// Generates bytes and returns their canonical RFC 4648 Base32 encoding.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="byteCount">The number of random bytes to encode.</param>
    /// <returns>A padded Base32 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>byteCount</c> is negative or the encoded output exceeds the configured limit.</exception>
    string Base32(int byteCount);
    /// <summary>
    /// Returns the canonical RFC 4648 Base32 encoding of a byte array.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base32 encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string Base32FromSource(byte[] source);
    /// <summary>
    /// Returns the canonical RFC 4648 Base32 encoding of a read-only byte span.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base32 encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string Base32FromSourceUsingReadOnlySpanOfByte(ReadOnlySpan<byte> source);
    /// <summary>
    /// Writes the canonical RFC 4648 Base32 encoding of bytes to a destination span.
    /// </summary>
    /// <remarks>The output uses the standard uppercase alphabet, required padding, zero pad bits, and no line breaks. Characters after the encoded output are unchanged.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    int WriteBase32(ReadOnlySpan<byte> source, Span<char> destination);
    /// <summary>
    /// Generates bytes and returns their canonical RFC 4648 Base64 encoding.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="byteCount">The number of random bytes to encode.</param>
    /// <returns>A padded Base64 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>byteCount</c> is negative or the encoded output exceeds the configured limit.</exception>
    string Base64(int byteCount);
    /// <summary>
    /// Returns the canonical RFC 4648 Base64 encoding of a byte array.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base64 encoding.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string Base64FromSource(byte[] source);
    /// <summary>
    /// Returns the canonical RFC 4648 Base64 encoding of a read-only byte span.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The padded Base64 encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    string Base64FromSourceUsingReadOnlySpanOfByte(ReadOnlySpan<byte> source);
    /// <summary>
    /// Writes the canonical RFC 4648 Base64 encoding of bytes to a destination span.
    /// </summary>
    /// <remarks>The output uses the standard alphabet, required padding, zero pad bits, and no line breaks. Characters after the encoded output are unchanged.</remarks>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when the destination is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the encoded output exceeds the configured limit.</exception>
    int WriteBase64(ReadOnlySpan<byte> source, Span<char> destination);
    /// <summary>
    /// Returns an exact-length Base58 display string using the Bitcoin alphabet.
    /// </summary>
    /// <remarks>This is display generation, not a claim of compatibility with every Base58 binary encoding convention.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A Base58 display string of exactly the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Base58(int count);
    /// <summary>
    /// Fills a destination span with Base58 display characters from the Bitcoin alphabet.
    /// </summary>
    /// <remarks>This is display generation, not a claim of compatibility with every Base58 binary encoding convention.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteBase58(Span<char> destination);
    /// <summary>
    /// Returns an exact-length string of mixed-case ASCII letters.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A string drawn uniformly from <c>A-Z</c> and <c>a-z</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Alpha(int count);
    /// <summary>
    /// Returns an exact-length string of single-case ASCII letters.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <param name="uppercase">Whether to use <c>A-Z</c>; false uses <c>a-z</c>.</param>
    /// <returns>A uniformly generated ASCII letter string in the selected case.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string AlphaFromCountAndUppercase(int count, bool uppercase);
    /// <summary>
    /// Fills a span with mixed-case ASCII letters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteAlpha(Span<char> destination);
    /// <summary>
    /// Fills a span with single-case ASCII letters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <param name="uppercase">Whether to use <c>A-Z</c>; false uses <c>a-z</c>.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteAlphaFromDestinationAndUppercase(Span<char> destination, bool uppercase);
    /// <summary>
    /// Returns an exact-length string of ASCII digits and mixed-case letters.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A string drawn uniformly from <c>0-9</c>, <c>A-Z</c>, and <c>a-z</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string AlphaNumeric(int count);
    /// <summary>
    /// Fills a span with ASCII digits and mixed-case letters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteAlphaNumeric(Span<char> destination);
    /// <summary>
    /// Returns an exact-length string of printable ASCII punctuation symbols.
    /// </summary>
    /// <remarks>Letters, digits, space, and control characters are excluded.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A uniformly generated ASCII punctuation string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Symbol(int count);
    /// <summary>
    /// Fills a span with printable ASCII punctuation symbols.
    /// </summary>
    /// <remarks>Letters, digits, space, and control characters are excluded.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteSymbol(Span<char> destination);
    /// <summary>
    /// Returns an exact-length string of ASCII whitespace characters.
    /// </summary>
    /// <remarks>The alphabet contains horizontal tab, line feed, vertical tab, form feed, carriage return, and space.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A uniformly generated ASCII whitespace string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Whitespace(int count);
    /// <summary>
    /// Fills a span with ASCII whitespace characters.
    /// </summary>
    /// <remarks>The alphabet contains horizontal tab, line feed, vertical tab, form feed, carriage return, and space.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WriteWhitespace(Span<char> destination);
    /// <summary>
    /// Returns an exact-length string of printable ASCII characters.
    /// </summary>
    /// <remarks>The alphabet is U+0020 space through U+007E tilde.</remarks>
    /// <param name="count">The number of characters to generate.</param>
    /// <returns>A uniformly generated printable ASCII string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Printable(int count);
    /// <summary>
    /// Fills a span with printable ASCII characters.
    /// </summary>
    /// <remarks>The alphabet is U+0020 space through U+007E tilde.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination length exceeds the configured output limit.</exception>
    int WritePrintable(Span<char> destination);
    /// <summary>
    /// Returns an exact-length string sampled uniformly from a validated alphabet with replacement.
    /// </summary>
    /// <param name="count">The number of characters to generate.</param>
    /// <param name="alphabet">The nonempty alphabet of distinct, non-surrogate UTF-16 characters.</param>
    /// <returns>A string containing characters from the supplied alphabet.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>alphabet</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>alphabet</c> is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an input exceeds a configured limit or <c>count</c> is negative.</exception>
    string FromCharacters(int count, string alphabet);
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
    string FromCharactersFromCountAndAlphabetAndWithReplacement(int count, string alphabet, bool withReplacement);
    /// <summary>
    /// Fills a span by sampling uniformly from a validated alphabet with replacement.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <param name="alphabet">The nonempty alphabet of distinct, non-surrogate UTF-16 characters.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>alphabet</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>alphabet</c> is empty or contains a duplicate or surrogate.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an input exceeds a configured limit.</exception>
    int WriteFromCharacters(Span<char> destination, string alphabet);
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
    int WriteFromCharactersFromDestinationAndAlphabetAndWithReplacement(Span<char> destination, string alphabet, bool withReplacement);
    /// <summary>
    /// Returns an exact number of random bytes from the configured random mode.
    /// </summary>
    /// <param name="count">The number of bytes to generate.</param>
    /// <returns>A new array containing the generated bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    byte[] Bytes(int count);
    /// <summary>
    /// Fills a byte span from the configured random mode.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    int WriteBytes(Span<byte> destination);
    /// <summary>
    /// Returns an exact-length lowercase hexadecimal string.
    /// </summary>
    /// <param name="count">The number of hexadecimal characters to generate.</param>
    /// <returns>A uniformly generated lowercase hexadecimal string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string HexFromCount(int count);
    /// <summary>
    /// Fills a span with lowercase hexadecimal characters.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    int WriteHexFromDestination(Span<char> destination);
    /// <summary>
    /// Returns a lowercase hexadecimal value with a requested display length.
    /// </summary>
    /// <remarks>The result is hash-shaped random data, not a digest of input.</remarks>
    /// <param name="count">The number of hexadecimal characters to generate.</param>
    /// <returns>A uniformly generated hash-shaped string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Hash(int count);
    /// <summary>
    /// Fills a span with lowercase hexadecimal hash-shaped random data.
    /// </summary>
    /// <remarks>The result is not a digest of input.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    int WriteHash(Span<char> destination);
    /// <summary>
    /// Returns an exact-length opaque token using the unpadded Base64 URL alphabet.
    /// </summary>
    /// <remarks>Use cryptographic random mode when the token will protect a real secret or capability.</remarks>
    /// <param name="count">The number of token characters to generate.</param>
    /// <returns>A uniformly generated opaque token.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string Token(int count);
    /// <summary>
    /// Fills a span with opaque token characters from the unpadded Base64 URL alphabet.
    /// </summary>
    /// <remarks>Use cryptographic random mode when the token will protect a real secret or capability.</remarks>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    int WriteToken(Span<char> destination);
    /// <summary>
    /// Returns an exact-length decimal PIN string, retaining leading zeroes.
    /// </summary>
    /// <remarks>Use cryptographic random mode for a PIN used outside deterministic fixture data.</remarks>
    /// <param name="digits">The number of decimal digits to generate.</param>
    /// <returns>A uniformly generated PIN string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>digits</c> is negative or exceeds the configured output limit.</exception>
    string Pin(int digits);
    /// <summary>
    /// Fills a span with decimal PIN digits, retaining leading zeroes.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    int WritePin(Span<char> destination);
    /// <summary>
    /// Returns four groups of four unambiguous uppercase recovery-code characters.
    /// </summary>
    /// <returns>A recovery code whose groups are separated by hyphens.</returns>
    string RecoveryCode();
    /// <summary>
    /// Returns an explicitly grouped recovery code using unambiguous uppercase characters.
    /// </summary>
    /// <param name="groupCount">The number of groups.</param>
    /// <param name="groupLength">The number of characters per group.</param>
    /// <param name="separator">The printable ASCII punctuation separator.</param>
    /// <returns>The grouped recovery code.</returns>
    /// <exception cref="ArgumentException">Thrown when the separator is unsupported.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a count is invalid or the output exceeds a configured limit.</exception>
    string RecoveryCodeFromGroupCountAndGroupLengthAndSeparator(int groupCount, int groupLength, char separator);
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
    int WriteRecoveryCode(Span<char> destination, int groupCount, int groupLength, char separator);
    /// <summary>
    /// Returns an exact-length password containing lowercase, uppercase, digit, and symbol characters.
    /// </summary>
    /// <remarks>At least one character from every enabled class is guaranteed. Use cryptographic random mode for a password used outside deterministic fixture data.</remarks>
    /// <param name="length">The password length, which must be at least four.</param>
    /// <returns>The generated password.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the length is too short or exceeds the configured output limit.</exception>
    string Password(int length);
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
    string PasswordFromLengthAndLowercaseAndUppercaseAndDigitsAndSymbols(int length, bool lowercase, bool uppercase, bool digits, bool symbols);
    /// <summary>
    /// Fills a span with a password requiring every standard character class.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <returns>The destination length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the destination is too short or exceeds the configured output limit.</exception>
    int WritePassword(Span<char> destination);
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
    int WritePasswordFromDestinationAndLowercaseAndUppercaseAndDigitsAndSymbols(Span<char> destination, bool lowercase, bool uppercase, bool digits, bool symbols);
    /// <summary>
    /// Returns a passphrase containing an exact number of authored five-letter words separated by hyphens.
    /// </summary>
    /// <remarks>Words are independently selected with replacement. Use cryptographic random mode for a passphrase used outside deterministic fixture data.</remarks>
    /// <param name="wordCount">The number of words to generate.</param>
    /// <returns>The generated passphrase.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the word count or output exceeds a configured limit.</exception>
    string Passphrase(int wordCount);
    /// <summary>
    /// Returns a passphrase containing an exact number of authored five-letter words.
    /// </summary>
    /// <param name="wordCount">The number of words to generate.</param>
    /// <param name="separator">The well-formed UTF-16 separator placed between words.</param>
    /// <returns>The generated passphrase.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>separator</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>separator</c> contains unpaired surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the word count or output exceeds a configured limit.</exception>
    string PassphraseFromWordCountAndSeparator(int wordCount, string separator);
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
    int WritePassphrase(Span<char> destination, int wordCount, string separator);
    /// <summary>
    /// Returns a uniformly generated Unicode scalar value from the full scalar range.
    /// </summary>
    /// <remarks>Unicode scalar values exclude the UTF-16 surrogate range. Unassigned code points and noncharacters remain valid scalar values and may be returned.</remarks>
    /// <returns>An integer from U+0000 through U+10FFFF excluding surrogates.</returns>
    int UnicodeScalar();
    /// <summary>
    /// Returns a uniformly generated Unicode scalar between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The exclusive maximum Unicode scalar.</param>
    /// <returns>A scalar within the requested range, with surrogates skipped uniformly.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is not a scalar or the interval is empty.</exception>
    int UnicodeScalarFromMinimumScalarAndMaximumScalar(int minimumScalar, int maximumScalar);
    /// <summary>
    /// Returns a uniformly generated Unicode scalar with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The maximum Unicode scalar.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A scalar within the requested range, with surrogates skipped uniformly.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound is not a scalar or the interval is empty.</exception>
    int UnicodeScalarFromMinimumScalarAndMaximumScalarAndMaximumInclusive(int minimumScalar, int maximumScalar, bool maximumInclusive);
    /// <summary>
    /// Returns a string containing an exact number of uniformly generated Unicode scalar values.
    /// </summary>
    /// <remarks>Scalar count differs from UTF-16 length because supplementary scalars are encoded as surrogate pairs. Unassigned code points and noncharacters may be returned.</remarks>
    /// <param name="count">The number of Unicode scalar values to generate.</param>
    /// <returns>A well-formed UTF-16 string containing exactly the requested scalar count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or the possible UTF-16 output exceeds the configured limit.</exception>
    string Unicode(int count);
    /// <summary>
    /// Returns a string of Unicode scalars between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="count">The number of Unicode scalar values to generate.</param>
    /// <param name="minimumScalar">The inclusive minimum Unicode scalar.</param>
    /// <param name="maximumScalar">The exclusive maximum Unicode scalar.</param>
    /// <returns>A well-formed UTF-16 string containing exactly the requested scalar count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is invalid or the possible UTF-16 output exceeds the configured limit.</exception>
    string UnicodeFromCountAndMinimumScalarAndMaximumScalar(int count, int minimumScalar, int maximumScalar);
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
    string UnicodeFromCountAndMinimumScalarAndMaximumScalarAndMaximumInclusive(int count, int minimumScalar, int maximumScalar, bool maximumInclusive);
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
    string UnicodeFromCountAndScript(int count, string script);
    /// <summary>
    /// Returns an exact-length string from the configured locale's resolved script repertoire.
    /// </summary>
    /// <remarks>An explicit locale script wins; otherwise the IANA suppress-script is used. Chinese regions use Hans except TW, HK, and MO, which use Hant.</remarks>
    /// <param name="count">The number of script characters to generate.</param>
    /// <returns>A string containing characters from the configured locale script repertoire.</returns>
    /// <exception cref="ArgumentException">Thrown when the locale does not identify a supported default script.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    string LocaleScript(int count);
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
    string LocaleScriptFromCountAndLocale(int count, string locale);
}
