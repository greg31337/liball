namespace raw;

/// <summary>Stores an exact TIFF unsigned or signed rational numerator and denominator.</summary>
/// <remarks>A zero denominator is retained so malformed metadata can be inspected and preserved. Call <see cref="Validate"/> before use, or rely on conversion and numeric equality methods to report it.</remarks>
public class RawRational
{
    /// <summary>The exact numerator represented as a signed 64-bit field.</summary>
    public long Numerator;
    /// <summary>The exact denominator represented as a signed 64-bit field.</summary>
    public long Denominator;
    /// <summary>Whether the stored TIFF rational uses signed 32-bit components.</summary>
    public bool Signed;

    /// <summary>Creates an exact unsigned TIFF rational.</summary>
    /// <param name="numerator">The unsigned 32-bit numerator.</param>
    /// <param name="denominator">The unsigned 32-bit denominator, including zero for malformed input.</param>
    public RawRational(uint numerator, uint denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
        Signed = false;
    }

    /// <summary>Creates an exact signed TIFF rational.</summary>
    /// <param name="numerator">The signed 32-bit numerator.</param>
    /// <param name="denominator">The signed 32-bit denominator, including zero for malformed input.</param>
    /// <returns>The exact signed rational.</returns>
    internal static RawRational CreateSigned(int numerator, int denominator)
    {
        RawRational value = new RawRational(0U, 1U);
        value.Numerator = numerator;
        value.Denominator = denominator;
        value.Signed = true;
        return value;
    }

    /// <summary>Validates the denominator and the component ranges implied by <see cref="Signed"/>.</summary>
    /// <returns>A report that retains separate machine-readable denominator and component-range findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Denominator == 0)
        {
            messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidRationalDenominator, RawSeverity.Error, RawFormat.Tiff, -1, "Rational.Denominator", "A rational denominator cannot be zero.", false));
        }

        if (Signed)
        {
            if (Numerator < int.MinValue || Numerator > int.MaxValue)
            {
                messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, RawFormat.Tiff, -1, "Rational.Numerator", "A signed rational numerator must fit in 32 bits.", false));
            }
            if (Denominator < int.MinValue || Denominator > int.MaxValue)
            {
                messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, RawFormat.Tiff, -1, "Rational.Denominator", "A signed rational denominator must fit in 32 bits.", false));
            }
        }
        else
        {
            if (Numerator < 0 || Numerator > uint.MaxValue)
            {
                messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, RawFormat.Tiff, -1, "Rational.Numerator", "An unsigned rational numerator must fit in 32 bits.", false));
            }
            if (Denominator < 0 || Denominator > uint.MaxValue)
            {
                messages.Add(new RawValidationMessage(RawValidationCategory.Metadata, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, RawFormat.Tiff, -1, "Rational.Denominator", "An unsigned rational denominator must fit in 32 bits.", false));
            }
        }

        return new RawValidationReport(true, messages.ToArray());
    }

    /// <summary>Converts the exact rational to the nearest double-precision value.</summary>
    /// <returns>The numerator divided by the denominator.</returns>
    /// <exception cref="RawFormatException">The denominator is zero or a component no longer fits its declared TIFF representation.</exception>
    internal double ToDouble()
    {
        RequireValid(this);
        return (double)Numerator / (double)Denominator;
    }

    /// <summary>Converts the exact rational to the nearest single-precision value.</summary>
    /// <returns>The numerator divided by the denominator rounded to single precision.</returns>
    /// <exception cref="RawFormatException">The denominator is zero or a component no longer fits its declared TIFF representation.</exception>
    internal float ToSingle()
    {
        return (float)ToDouble();
    }

    /// <summary>Tests whether another rational has the same signedness and exact stored components.</summary>
    /// <param name="other">The rational to compare, or <c>null</c>.</param>
    /// <returns><c>true</c> when signedness, numerator, and denominator are identical; otherwise <c>false</c>.</returns>
    internal bool EqualsExact(RawRational? other)
    {
        if (other == null)
        {
            return false;
        }
        return Signed == other.Signed && Numerator == other.Numerator && Denominator == other.Denominator;
    }

    /// <summary>Tests exact numeric equality after validating both denominators and component ranges.</summary>
    /// <param name="other">The rational to compare, or <c>null</c>.</param>
    /// <returns><c>true</c> when both rationals represent the same numeric value; otherwise <c>false</c>.</returns>
    /// <exception cref="RawFormatException">Either denominator is zero or a component no longer fits its declared TIFF representation.</exception>
    internal bool EqualsValue(RawRational? other)
    {
        if (other == null)
        {
            return false;
        }
        RequireValid(this);
        RequireValid(other);
        decimal left = (decimal)Numerator * (decimal)other.Denominator;
        decimal right = (decimal)other.Numerator * (decimal)Denominator;
        return left == right;
    }

    internal static void RequireValid(RawRational value)
    {
        if (value.Denominator == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidRationalDenominator, "A rational denominator cannot be zero.", -1);
        }

        if (value.Signed)
        {
            if (value.Numerator < int.MinValue || value.Numerator > int.MaxValue || value.Denominator < int.MinValue || value.Denominator > int.MaxValue)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A signed rational component does not fit in 32 bits.", -1);
            }
        }
        else
        {
            if (value.Numerator < 0 || value.Numerator > uint.MaxValue || value.Denominator < 0 || value.Denominator > uint.MaxValue)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "An unsigned rational component does not fit in 32 bits.", -1);
            }
        }
    }
}
