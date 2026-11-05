using System;

namespace email;
/// <summary>
/// Provides operations for working with safe arithmetic.
/// </summary>
public class EmailSafeArithmetic : IEmailSafeArithmeticContract
{
    internal static bool CanAddNonNegativeImplementation(long left, long right)
    {
        if (left < 0 || right < 0)
        {
            return false;
        }

        return right <= long.MaxValue - left;
    }

    /// <summary>
    /// Determines whether the instance can add non negative.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns><c>true</c> when the requested operation is supported; otherwise, <c>false</c>.</returns>
    public bool CanAddNonNegative(long left, long right)
    {
        return EmailSafeArithmetic.CanAddNonNegativeImplementation(left, right);
    }

    internal static long AddNonNegativeImplementation(long left, long right)
    {
        ValidateNonNegative(left, "left");
        ValidateNonNegative(right, "right");
        if (right > long.MaxValue - left)
        {
            throw new OverflowException("The sum exceeds Int64.MaxValue.");
        }

        return left + right;
    }

    /// <summary>
    /// Adds a non negative to the current collection or model.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public long AddNonNegative(long left, long right)
    {
        return EmailSafeArithmetic.AddNonNegativeImplementation(left, right);
    }

    internal static bool CanMultiplyNonNegativeImplementation(long left, long right)
    {
        if (left < 0 || right < 0)
        {
            return false;
        }

        if (left == 0 || right == 0)
        {
            return true;
        }

        return right <= long.MaxValue / left;
    }

    /// <summary>
    /// Determines whether the instance can multiply non negative.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns><c>true</c> when the requested operation is supported; otherwise, <c>false</c>.</returns>
    public bool CanMultiplyNonNegative(long left, long right)
    {
        return EmailSafeArithmetic.CanMultiplyNonNegativeImplementation(left, right);
    }

    internal static long MultiplyNonNegativeImplementation(long left, long right)
    {
        ValidateNonNegative(left, "left");
        ValidateNonNegative(right, "right");
        if (left != 0 && right > long.MaxValue / left)
        {
            throw new OverflowException("The product exceeds Int64.MaxValue.");
        }

        return left * right;
    }

    /// <summary>
    /// Multiplies the non negative.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public long MultiplyNonNegative(long left, long right)
    {
        return EmailSafeArithmetic.MultiplyNonNegativeImplementation(left, right);
    }

    internal static long CeilingDivideNonNegativeImplementation(long value, long divisor)
    {
        ValidateNonNegative(value, "value");
        if (divisor < 1)
        {
            throw new ArgumentOutOfRangeException("divisor");
        }

        long quotient = value / divisor;
        if (value % divisor == 0)
        {
            return quotient;
        }

        return quotient + 1;
    }

    /// <summary>
    /// Divides and rounds up the non negative.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "divisor">The divisor used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long CeilingDivideNonNegative(long value, long divisor)
    {
        return EmailSafeArithmetic.CeilingDivideNonNegativeImplementation(value, divisor);
    }

    internal static int ToInt32NonNegativeImplementation(long value)
    {
        ValidateNonNegative(value, "value");
        if (value > int.MaxValue)
        {
            throw new OverflowException("The value exceeds Int32.MaxValue.");
        }

        return (int)value;
    }

    /// <summary>
    /// Converts to the int 32 non negative.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The computed count or size.</returns>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public int ToInt32NonNegative(long value)
    {
        return EmailSafeArithmetic.ToInt32NonNegativeImplementation(value);
    }

    static void ValidateNonNegative(long value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
