namespace avif;

/// <summary>Defines access to an authored camera intrinsic matrix.</summary>
public interface IAvifCameraIntrinsicMatrix
{
    /// <summary>Returns the construction result.</summary>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode GetCode();

    /// <summary>Reports whether separate Y focal length and skew fields are present.</summary>
    /// <returns><c>true</c> for the full matrix form.</returns>
    bool HasFullIntrinsics();

    /// <summary>Returns the power-of-two denominator shift for the primary fields.</summary>
    /// <returns>A value from zero through thirty-one.</returns>
    int GetDenominatorShift();

    /// <summary>Returns the power-of-two denominator shift for skew.</summary>
    /// <returns>A value from zero through thirty-one.</returns>
    int GetSkewDenominatorShift();

    /// <summary>Returns the encoded X focal length numerator.</summary>
    /// <returns>The signed numerator.</returns>
    int GetFocalLengthX();

    /// <summary>Returns the encoded X principal-point numerator.</summary>
    /// <returns>The signed numerator.</returns>
    int GetPrincipalPointX();

    /// <summary>Returns the encoded Y principal-point numerator.</summary>
    /// <returns>The signed numerator.</returns>
    int GetPrincipalPointY();

    /// <summary>Returns the encoded Y focal length numerator.</summary>
    /// <returns>The explicit value, or the X focal length for the simplified form.</returns>
    int GetFocalLengthY();

    /// <summary>Returns the encoded skew numerator.</summary>
    /// <returns>The skew numerator, or zero for the simplified form.</returns>
    int GetSkewFactor();
}
