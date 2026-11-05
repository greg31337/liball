using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSafeArithmetic"/>.
/// </summary>
public interface IEmailSafeArithmeticContract
{
    /// <summary>
    /// Determines whether the instance can add non negative.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns><c>true</c> when the requested operation is supported; otherwise, <c>false</c>.</returns>
    bool CanAddNonNegative(long left, long right);
    /// <summary>
    /// Adds a non negative to the current collection or model.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    long AddNonNegative(long left, long right);
    /// <summary>
    /// Determines whether the instance can multiply non negative.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns><c>true</c> when the requested operation is supported; otherwise, <c>false</c>.</returns>
    bool CanMultiplyNonNegative(long left, long right);
    /// <summary>
    /// Multiplies the non negative.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    long MultiplyNonNegative(long left, long right);
    /// <summary>
    /// Divides and rounds up the non negative.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "divisor">The divisor used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long CeilingDivideNonNegative(long value, long divisor);
    /// <summary>
    /// Converts to the int 32 non negative.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    int ToInt32NonNegative(long value);
}
