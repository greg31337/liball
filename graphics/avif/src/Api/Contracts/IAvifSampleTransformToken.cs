namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifSampleTransformToken"/>.</summary>
public interface IAvifSampleTransformToken
{
    /// <summary>Returns the encoded eight-bit token value.</summary>
    /// <returns>The value from zero through 255.</returns>
    int GetEncodedValue();

    /// <summary>Reports whether this token pushes a literal constant.</summary>
    /// <returns><c>true</c> for token value zero.</returns>
    bool IsConstant();

    /// <summary>Reports whether this token pushes a sample from an input image.</summary>
    /// <returns><c>true</c> for token values from one through 32.</returns>
    bool IsInputSample();

    /// <summary>Reports whether this token applies a unary or binary operator.</summary>
    /// <returns><c>true</c> for a recognized operator token.</returns>
    bool IsOperator();

    /// <summary>Returns the signed literal value.</summary>
    /// <returns>The constant, or zero for another token kind.</returns>
    long GetConstant();

    /// <summary>Returns the zero-based input image index.</summary>
    /// <returns>The input index, or minus one for another token kind.</returns>
    int GetInputIndex();

    /// <summary>Returns the recognized operator.</summary>
    /// <returns>The operator, or the default enum value for an operand token.</returns>
    AvifSampleTransformOperator GetOperator();
}
