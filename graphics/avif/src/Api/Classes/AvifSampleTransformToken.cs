namespace avif;

/// <summary>Describes one operand or operator in a sample-transform postfix expression.</summary>
public class AvifSampleTransformToken : IAvifSampleTransformToken
{
    int encodedValue;
    int kind;
    long constant;
    int inputIndex;
    AvifSampleTransformOperator operation;

    internal AvifSampleTransformToken(
        int encodedValue,
        int kind,
        long constant,
        int inputIndex,
        AvifSampleTransformOperator operation)
    {
        this.encodedValue = encodedValue;
        this.kind = kind;
        this.constant = constant;
        this.inputIndex = inputIndex;
        this.operation = operation;
    }

    /// <summary>Returns the encoded eight-bit token value.</summary>
    /// <returns>The value from zero through 255.</returns>
    public int GetEncodedValue()
    {
        return encodedValue;
    }

    /// <summary>Reports whether this token pushes a literal constant.</summary>
    /// <returns><c>true</c> for token value zero.</returns>
    public bool IsConstant()
    {
        return kind == 0;
    }

    /// <summary>Reports whether this token pushes a sample from an input image.</summary>
    /// <returns><c>true</c> for token values from one through 32.</returns>
    public bool IsInputSample()
    {
        return kind == 1;
    }

    /// <summary>Reports whether this token applies a unary or binary operator.</summary>
    /// <returns><c>true</c> for a recognized operator token.</returns>
    public bool IsOperator()
    {
        return kind == 2;
    }

    /// <summary>Returns the signed literal value.</summary>
    /// <returns>The constant, or zero for another token kind.</returns>
    public long GetConstant()
    {
        return IsConstant() ? constant : 0;
    }

    /// <summary>Returns the zero-based input image index.</summary>
    /// <returns>The input index, or minus one for another token kind.</returns>
    public int GetInputIndex()
    {
        return IsInputSample() ? inputIndex : -1;
    }

    /// <summary>Returns the recognized operator.</summary>
    /// <returns>The operator, or the default enum value for an operand token.</returns>
    public AvifSampleTransformOperator GetOperator()
    {
        return operation;
    }

    internal bool IsUnaryOperator()
    {
        return encodedValue >= 64 && encodedValue <= 67;
    }
}
