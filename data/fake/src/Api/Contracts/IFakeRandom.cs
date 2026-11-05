using System;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="FakeRandom"/>.</summary>
public interface IFakeRandom
{
    /// <summary>
    /// Returns the next unsigned 64-bit value from the full output range.
    /// </summary>
    /// <returns>The next value from the configured random source.</returns>
    ulong ULong();
    /// <summary>
    /// Returns an unsigned 64-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    ulong ULongFromMaximumExclusive(ulong maximumExclusive);
    /// <summary>
    /// Returns an unsigned 64-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    ulong ULongFromMinimumAndMaximum(ulong minimum, ulong maximum);
    /// <summary>
    /// Returns an unsigned 64-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    ulong ULongFromMinimumAndMaximumAndMaximumInclusive(ulong minimum, ulong maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a signed 64-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="long.MinValue"/> through <see cref="long.MaxValue"/>.</returns>
    long Long();
    /// <summary>
    /// Returns a nonnegative signed 64-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    long LongFromMaximumExclusive(long maximumExclusive);
    /// <summary>
    /// Returns a signed 64-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    long LongFromMinimumAndMaximum(long minimum, long maximum);
    /// <summary>
    /// Returns a signed 64-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    long LongFromMinimumAndMaximumAndMaximumInclusive(long minimum, long maximum, bool maximumInclusive);
    /// <summary>
    /// Returns an unsigned 32-bit value from the full range.
    /// </summary>
    /// <returns>A value from zero through <see cref="uint.MaxValue"/>.</returns>
    uint UInt();
    /// <summary>
    /// Returns an unsigned 32-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    uint UIntFromMaximumExclusive(uint maximumExclusive);
    /// <summary>
    /// Returns an unsigned 32-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    uint UIntFromMinimumAndMaximum(uint minimum, uint maximum);
    /// <summary>
    /// Returns an unsigned 32-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    uint UIntFromMinimumAndMaximumAndMaximumInclusive(uint minimum, uint maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a signed 32-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="int.MinValue"/> through <see cref="int.MaxValue"/>.</returns>
    int Int();
    /// <summary>
    /// Returns a signed 32-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    int IntFromMinimumAndMaximum(int minimum, int maximum);
    /// <summary>
    /// Returns a signed 32-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    int IntFromMinimumAndMaximumAndMaximumInclusive(int minimum, int maximum, bool maximumInclusive);
    /// <summary>
    /// Returns an unsigned 16-bit value from the full range.
    /// </summary>
    /// <returns>A value from zero through <see cref="ushort.MaxValue"/>.</returns>
    ushort UShort();
    /// <summary>
    /// Returns an unsigned 16-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    ushort UShortFromMaximumExclusive(ushort maximumExclusive);
    /// <summary>
    /// Returns an unsigned 16-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    ushort UShortFromMinimumAndMaximum(ushort minimum, ushort maximum);
    /// <summary>
    /// Returns an unsigned 16-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    ushort UShortFromMinimumAndMaximumAndMaximumInclusive(ushort minimum, ushort maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a signed 16-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="short.MinValue"/> through <see cref="short.MaxValue"/>.</returns>
    short Short();
    /// <summary>
    /// Returns a nonnegative signed 16-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    short ShortFromMaximumExclusive(short maximumExclusive);
    /// <summary>
    /// Returns a signed 16-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    short ShortFromMinimumAndMaximum(short minimum, short maximum);
    /// <summary>
    /// Returns a signed 16-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    short ShortFromMinimumAndMaximumAndMaximumInclusive(short minimum, short maximum, bool maximumInclusive);
    /// <summary>
    /// Returns an unsigned 8-bit value from the full range.
    /// </summary>
    /// <returns>A value from zero through <see cref="byte.MaxValue"/>.</returns>
    byte Byte();
    /// <summary>
    /// Returns an unsigned 8-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    byte ByteFromMaximumExclusive(byte maximumExclusive);
    /// <summary>
    /// Returns an unsigned 8-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    byte ByteFromMinimumAndMaximum(byte minimum, byte maximum);
    /// <summary>
    /// Returns an unsigned 8-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    byte ByteFromMinimumAndMaximumAndMaximumInclusive(byte minimum, byte maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a signed 8-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="sbyte.MinValue"/> through <see cref="sbyte.MaxValue"/>.</returns>
    sbyte SByte();
    /// <summary>
    /// Returns a nonnegative signed 8-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    sbyte SByteFromMaximumExclusive(sbyte maximumExclusive);
    /// <summary>
    /// Returns a signed 8-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    sbyte SByteFromMinimumAndMaximum(sbyte minimum, sbyte maximum);
    /// <summary>
    /// Returns a signed 8-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    sbyte SByteFromMinimumAndMaximumAndMaximumInclusive(sbyte minimum, sbyte maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a nonnegative arbitrary-width integer below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive or requires more than the configured output limit.</exception>
    System.Numerics.BigInteger BigInteger(System.Numerics.BigInteger maximumExclusive);
    /// <summary>
    /// Returns an arbitrary-width integer between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or requires more than the configured output limit.</exception>
    System.Numerics.BigInteger BigIntegerFromMinimumAndMaximum(System.Numerics.BigInteger minimum, System.Numerics.BigInteger maximum);
    /// <summary>
    /// Returns an arbitrary-width integer with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty or requires more than the configured output limit.</exception>
    System.Numerics.BigInteger BigIntegerFromMinimumAndMaximumAndMaximumInclusive(System.Numerics.BigInteger minimum, System.Numerics.BigInteger maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a finite single-precision value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A value in the interval from zero inclusive to one exclusive.</returns>
    float Float();
    /// <summary>
    /// Returns a nonnegative finite single-precision value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The finite exclusive maximum.</param>
    /// <returns>A value from zero inclusive through the exclusive maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not finite and positive.</exception>
    float FloatFromMaximumExclusive(float maximumExclusive);
    /// <summary>
    /// Returns a finite single-precision value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    float FloatFromMinimumAndMaximum(float minimum, float maximum);
    /// <summary>
    /// Returns a finite single-precision value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the selected interval is empty.</exception>
    float FloatFromMinimumAndMaximumAndMaximumInclusive(float minimum, float maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a finite double-precision value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A value in the interval from zero inclusive to one exclusive.</returns>
    double Double();
    /// <summary>
    /// Returns a nonnegative finite double-precision value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The finite exclusive maximum.</param>
    /// <returns>A value from zero inclusive through the exclusive maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not finite and positive.</exception>
    double DoubleFromMaximumExclusive(double maximumExclusive);
    /// <summary>
    /// Returns a finite double-precision value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    double DoubleFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>
    /// Returns a finite double-precision value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the selected interval is empty.</exception>
    double DoubleFromMinimumAndMaximumAndMaximumInclusive(double minimum, double maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a decimal value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A value in the interval from zero inclusive to one exclusive.</returns>
    decimal Decimal();
    /// <summary>
    /// Returns a nonnegative decimal value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero inclusive through the exclusive maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    decimal DecimalFromMaximumExclusive(decimal maximumExclusive);
    /// <summary>
    /// Returns a decimal value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    decimal DecimalFromMinimumAndMaximum(decimal minimum, decimal maximum);
    /// <summary>
    /// Returns a decimal value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    decimal DecimalFromMinimumAndMaximumAndMaximumInclusive(decimal minimum, decimal maximum, bool maximumInclusive);
    /// <summary>
    /// Returns an unbiased boolean with equal false and true probabilities.
    /// </summary>
    /// <returns>A random boolean.</returns>
    bool Bool();
    /// <summary>
    /// Returns a boolean using a probability of true.
    /// </summary>
    /// <param name="probability">The finite probability from zero through one.</param>
    /// <returns>True with the requested probability; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    bool BoolFromProbability(double probability);
    /// <summary>
    /// Returns a boolean using an exact integer ratio of true outcomes.
    /// </summary>
    /// <param name="numerator">The number of true outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>True with probability <c>numerator / denominator</c>; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    bool BoolFromNumeratorAndDenominator(long numerator, long denominator);
    /// <summary>
    /// Returns a boolean using a probability of true.
    /// </summary>
    /// <remarks>Exact zero and one probabilities do not consume random state.</remarks>
    /// <param name="probability">The finite probability from zero through one.</param>
    /// <returns>True with the requested probability; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    bool Probability(double probability);
    /// <summary>
    /// Returns a boolean using an exact integer ratio of true outcomes.
    /// </summary>
    /// <remarks>Zero and full ratios do not consume random state.</remarks>
    /// <param name="numerator">The number of true outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>True with probability <c>numerator / denominator</c>; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    bool ProbabilityFromNumeratorAndDenominator(long numerator, long denominator);
    /// <summary>
    /// Returns a boolean using an exact integer ratio of true outcomes.
    /// </summary>
    /// <remarks>Zero and full ratios do not consume random state.</remarks>
    /// <param name="numerator">The number of true outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>True with probability <c>numerator / denominator</c>; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    bool Ratio(long numerator, long denominator);
    /// <summary>
    /// Optionally returns a value with equal missing and present probabilities.
    /// </summary>
    /// <param name="value">The value to return when selected.</param>
    /// <returns>The value when selected; otherwise <c>null</c>.</returns>
    object? Maybe(object? value);
    /// <summary>
    /// Optionally returns a value using a probability of presence.
    /// </summary>
    /// <param name="value">The value to return when selected.</param>
    /// <param name="probability">The finite probability of returning the value.</param>
    /// <returns>The value when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    object? MaybeFromValueAndProbability(object? value, double probability);
    /// <summary>
    /// Optionally returns a value using an exact ratio of present outcomes.
    /// </summary>
    /// <param name="value">The value to return when selected.</param>
    /// <param name="numerator">The number of present outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>The value when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    object? MaybeFromValueAndNumeratorAndDenominator(object? value, long numerator, long denominator);
    /// <summary>
    /// Optionally invokes a factory with equal missing and present probabilities.
    /// </summary>
    /// <param name="factory">The factory to invoke only when selected.</param>
    /// <returns>The factory result when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>factory</c> is null.</exception>
    object? MaybeFromFactory(System.Func<object?> factory);
    /// <summary>
    /// Optionally invokes a factory using a probability of presence.
    /// </summary>
    /// <param name="factory">The factory to invoke only when selected.</param>
    /// <param name="probability">The finite probability of invoking the factory.</param>
    /// <returns>The factory result when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>factory</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    object? MaybeFromFactoryAndProbability(System.Func<object?> factory, double probability);
    /// <summary>
    /// Optionally invokes a factory using an exact ratio of present outcomes.
    /// </summary>
    /// <param name="factory">The factory to invoke only when selected.</param>
    /// <param name="numerator">The number of present outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>The factory result when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>factory</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    object? MaybeFromFactoryAndNumeratorAndDenominator(System.Func<object?> factory, long numerator, long denominator);
    /// <summary>
    /// Returns a uniform value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A uniform value in the unit interval.</returns>
    double Uniform();
    /// <summary>
    /// Returns a uniform value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A uniform value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    double UniformFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>
    /// Returns a uniform value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A uniform value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the selected interval is empty.</exception>
    double UniformFromMinimumAndMaximumAndMaximumInclusive(double minimum, double maximum, bool maximumInclusive);
    /// <summary>
    /// Returns a standard normal value with mean zero and standard deviation one.
    /// </summary>
    /// <returns>A finite normally distributed value.</returns>
    double Normal();
    /// <summary>
    /// Returns a normally distributed value.
    /// </summary>
    /// <param name="mean">The finite distribution mean.</param>
    /// <param name="standardDeviation">The finite, nonnegative standard deviation.</param>
    /// <returns>A finite normally distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a parameter is non-finite or the standard deviation is negative.</exception>
    double NormalFromMeanAndStandardDeviation(double mean, double standardDeviation);
    /// <summary>
    /// Returns a standard log-normal value whose logarithm has mean zero and standard deviation one.
    /// </summary>
    /// <returns>A positive finite log-normally distributed value.</returns>
    double LogNormal();
    /// <summary>
    /// Returns a log-normally distributed value.
    /// </summary>
    /// <param name="mean">The finite mean of the underlying normal logarithm.</param>
    /// <param name="standardDeviation">The finite, nonnegative standard deviation of the logarithm.</param>
    /// <returns>A positive finite log-normally distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a parameter is non-finite or the standard deviation is negative.</exception>
    double LogNormalFromMeanAndStandardDeviation(double mean, double standardDeviation);
    /// <summary>
    /// Returns an exponentially distributed value with rate one.
    /// </summary>
    /// <returns>A nonnegative finite exponentially distributed value.</returns>
    double Exponential();
    /// <summary>
    /// Returns an exponentially distributed value with a specified rate.
    /// </summary>
    /// <param name="rate">The finite positive rate.</param>
    /// <returns>A nonnegative finite exponentially distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>rate</c> is non-finite or not positive.</exception>
    double ExponentialFromRate(double rate);
    /// <summary>
    /// Returns a triangular value from zero through one with mode one half.
    /// </summary>
    /// <returns>A finite triangularly distributed value.</returns>
    double Triangular();
    /// <summary>
    /// Returns a symmetric triangular value between two bounds.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A finite triangularly distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    double TriangularFromMinimumAndMaximum(double minimum, double maximum);
    /// <summary>
    /// Returns a triangular value between two bounds with a specified mode.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <param name="mode">The finite mode between the bounds, inclusive.</param>
    /// <returns>A finite triangularly distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a value is non-finite, the interval is empty, or the mode is outside it.</exception>
    double TriangularFromMinimumAndMaximumAndMode(double minimum, double maximum, double mode);
    /// <summary>
    /// Returns an index from a weighted discrete distribution.
    /// </summary>
    /// <remarks>
    /// Weights may be normalized or relative. Zero-weight indexes are never selected.
    /// Each positive-weight interval excludes its upper boundary, so an exact cumulative
    /// boundary selects the next positive-weight index.
    /// </remarks>
    /// <param name="weights">The finite, nonnegative weights containing at least one positive value.</param>
    /// <returns>The selected zero-based index.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty or contains no positive value.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the array exceeds the configured count limit or contains a negative or non-finite weight.</exception>
    int WeightedDiscrete(double[] weights);
    /// <summary>
    /// Selects one string according to corresponding weights.
    /// </summary>
    /// <remarks>
    /// Weights may be normalized or relative. Zero-weight values are never selected.
    /// Each positive-weight interval excludes its upper boundary, so an exact cumulative
    /// boundary selects the next positive-weight value.
    /// </remarks>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="weights">One finite, nonnegative weight per value, with at least one positive weight.</param>
    /// <returns>The selected string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> or <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the arrays are empty, their lengths differ, or no weight is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an array exceeds the configured count limit or a weight is negative or non-finite.</exception>
    string WeightedChoice(string[] values, double[] weights);
    /// <summary>
    /// Selects one object according to corresponding weights.
    /// </summary>
    /// <remarks>
    /// Weights may be normalized or relative. Zero-weight values are never selected.
    /// Each positive-weight interval excludes its upper boundary, so an exact cumulative
    /// boundary selects the next positive-weight value.
    /// </remarks>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="weights">One finite, nonnegative weight per value, with at least one positive weight.</param>
    /// <returns>The selected object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> or <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the arrays are empty, their lengths differ, or no weight is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an array exceeds the configured count limit or a weight is negative or non-finite.</exception>
    object WeightedChoiceFromValuesAndWeights(object[] values, double[] weights);
    /// <summary>
    /// Selects one string uniformly from an array.
    /// </summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    string Choice(string[] values);
    /// <summary>
    /// Selects one object uniformly from an array.
    /// </summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    object ChoiceFromValues(object[] values);
    /// <summary>
    /// Selects strings uniformly with replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the selected strings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit or <c>count</c> is negative.</exception>
    string[] Choices(string[] values, int count);
    /// <summary>
    /// Selects strings uniformly with an explicit replacement choice.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <param name="withReplacement">Whether a source position may be selected more than once.</param>
    /// <returns>A new array containing the selected strings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient values exist without replacement.</exception>
    string[] ChoicesFromValuesAndCountAndWithReplacement(string[] values, int count, bool withReplacement);
    /// <summary>
    /// Selects objects uniformly with replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the selected objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit or <c>count</c> is negative.</exception>
    object[] ChoicesFromValuesAndCount(object[] values, int count);
    /// <summary>
    /// Selects objects uniformly with an explicit replacement choice.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <param name="withReplacement">Whether a source position may be selected more than once.</param>
    /// <returns>A new array containing the selected objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient values exist without replacement.</exception>
    object[] ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(object[] values, int count, bool withReplacement);
    /// <summary>
    /// Samples string source positions uniformly without replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the sampled strings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient source positions exist.</exception>
    string[] Sample(string[] values, int count);
    /// <summary>
    /// Samples object source positions uniformly without replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the sampled objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient source positions exist.</exception>
    object[] SampleFromValuesAndCount(object[] values, int count);
    /// <summary>
    /// Returns a uniformly shuffled copy of a string array.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <returns>A new array containing every source position once.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    string[] Shuffle(string[] values);
    /// <summary>
    /// Returns a uniformly shuffled copy of an object array.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <returns>A new array containing every source position once.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    object[] ShuffleFromValues(object[] values);
    /// <summary>
    /// Invokes a callback a bounded number of times and returns the results in invocation order.
    /// </summary>
    /// <param name="count">The number of callback results to create.</param>
    /// <param name="generate">The callback invoked once for each result.</param>
    /// <returns>A new array containing the generated objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured count limit.</exception>
    object[] Repeat(int count, Func<object> generate);
    /// <summary>
    /// Fills an existing object array by invoking a callback once for each destination position.
    /// </summary>
    /// <remarks>If the callback throws, the completed destination prefix remains filled and the exception is propagated.</remarks>
    /// <param name="destination">The object array to fill in ascending index order.</param>
    /// <param name="generate">The callback invoked once for each destination position.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured count limit.</exception>
    void FillFromDestinationAndGenerate(object[] destination, Func<object> generate);
    /// <summary>
    /// Selects one declared value uniformly from a runtime enum type.
    /// </summary>
    /// <param name="enumType">The enum type whose declared values form the selection set.</param>
    /// <returns>The selected boxed enum value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>enumType</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>enumType</c> does not identify an enum.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the number of declared values exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when the enum declares no values.</exception>
    object EnumValue(Type enumType);
    /// <summary>
    /// Creates an unbounded sequence starting at zero and incrementing by one.
    /// </summary>
    /// <returns>A new independent sequence state.</returns>
    SequenceState Sequence();
    /// <summary>
    /// Creates an unbounded arithmetic sequence.
    /// </summary>
    /// <param name="start">The first value.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <returns>A new independent sequence state.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    SequenceState SequenceFromStartAndIncrement(long start, long increment);
    /// <summary>
    /// Creates a finite arithmetic sequence with an exclusive boundary.
    /// </summary>
    /// <param name="start">The first value.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <param name="boundaryExclusive">The exclusive boundary in the direction of the increment.</param>
    /// <returns>A new independent sequence state.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the increment is zero or the boundary is not beyond the start in its direction.</exception>
    SequenceState SequenceFromStartAndIncrementAndBoundaryExclusive(long start, long increment, long boundaryExclusive);
    /// <summary>
    /// Creates a cyclic arithmetic sequence with an exclusive boundary.
    /// </summary>
    /// <remarks>After the last value before the boundary, the next value is the configured start.</remarks>
    /// <param name="start">The first value.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <param name="boundaryExclusive">The exclusive boundary in the direction of the increment.</param>
    /// <returns>A new independent cyclic sequence state.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the increment is zero or the boundary is not beyond the start in its direction.</exception>
    SequenceState Cycle(long start, long increment, long boundaryExclusive);
    /// <summary>Returns a cached named dataset sequence starting at zero and incrementing by one.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <returns>The named sequence shared by providers in this generation context.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the name was previously used with different sequence configuration.</exception>
    SequenceState SequenceFromName(string name);
    /// <summary>Returns a cached named arithmetic dataset sequence.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="start">The first value returned after creation or reset.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <returns>The named sequence shared by providers in this generation context.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    /// <exception cref="FakeException">Thrown when the name was previously used with different sequence configuration.</exception>
    SequenceState SequenceFromNameAndStartAndIncrement(string name, long start, long increment);
    /// <summary>Resets a named dataset sequence if it has been created.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    void ResetSequence(string name);
    /// <summary>Resets every named dataset sequence in this generation context.</summary>
    void ResetSequenceFromNoArguments();
    /// <summary>
    /// Returns a cached named uniqueness scope shared by this generator's providers and random forks.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <returns>The named uniqueness scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    UniqueScope Unique(string name);
    /// <summary>
    /// Generates a string unique within a named scope.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="generate">The callback used to generate candidate strings.</param>
    /// <returns>The first candidate not previously accepted by the named scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the retry budget is exhausted.</exception>
    string UniqueFromNameAndGenerate(string name, Func<string> generate);
    /// <summary>
    /// Generates an object with a composite string key unique within a named scope.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="generate">The callback used to generate candidate objects.</param>
    /// <param name="compositeKey">The callback that selects the ordered string key parts.</param>
    /// <returns>The first candidate whose composite key was not previously accepted by the named scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when key selection fails or the retry budget is exhausted.</exception>
    object UniqueFromNameAndGenerateAndCompositeKey(string name, Func<object> generate, Func<object, string[]> compositeKey);
    /// <summary>
    /// Resets a named uniqueness scope, creating it if necessary.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    void ResetUnique(string name);
    /// <summary>
    /// Resets every uniqueness scope created by this generator's context.
    /// </summary>
    void ResetUniqueFromNoArguments();
    /// <summary>
    /// Creates an array and fills it from the configured random source.
    /// </summary>
    /// <param name="count">The number of bytes to create.</param>
    /// <returns>A newly allocated array containing random bytes.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    byte[] Bytes(int count);
    /// <summary>
    /// Fills an array from the configured random source.
    /// </summary>
    /// <param name="destination">The array to fill.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>destination</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    void Fill(byte[] destination);
    /// <summary>
    /// Restarts a deterministic stream from its current seed.
    /// Cryptographic mode continues from the platform random source.
    /// </summary>
    void Reset();
    /// <summary>
    /// Captures the exact current position and reset seed of a deterministic stream.
    /// </summary>
    /// <returns>An independent deterministic-state snapshot.</returns>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    FakeState Snapshot();
    /// <summary>
    /// Restores the exact position and reset seed recorded in a deterministic snapshot.
    /// </summary>
    /// <param name="state">The deterministic-state snapshot to restore.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>state</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the snapshot algorithm or state is invalid.</exception>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    void Restore(FakeState state);
    /// <summary>
    /// Creates an independent deterministic child stream with a stable name.
    /// </summary>
    /// <remarks>
    /// The child is derived from this stream's reset seed and does not consume
    /// this stream. In deterministic mode, repeating a name produces the same
    /// child sequence. Cryptographic child streams remain non-reproducible.
    /// </remarks>
    /// <param name="name">The nonempty child-stream name.</param>
    /// <returns>A new child stream at its initial position.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    FakeRandom Fork(string name);
    /// <summary>
    /// Replaces the current seed with an integer seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    void Reseed(int seed);
    /// <summary>
    /// Replaces the current seed with a long seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    void ReseedFromSeed(long seed);
    /// <summary>
    /// Replaces the current seed with a UTF-8 string seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>seed</c> is null.</exception>
    void ReseedFromSeedUsingString(string seed);
    /// <summary>
    /// Replaces the current seed with a byte-array seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed bytes.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>seed</c> is null.</exception>
    void ReseedFromSeedUsingByteArray(byte[] seed);
}
