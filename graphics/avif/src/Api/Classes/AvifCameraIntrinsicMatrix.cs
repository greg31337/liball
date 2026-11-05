namespace avif;

/// <summary>Describes the encoded intrinsic matrix of a camera.</summary>
public class AvifCameraIntrinsicMatrix : IAvifCameraIntrinsicMatrix
{
    AvifResultCode code;
    bool fullIntrinsics;
    int denominatorShift;
    int skewDenominatorShift;
    int focalLengthX;
    int principalPointX;
    int principalPointY;
    int focalLengthY;
    int skewFactor;

    /// <summary>Creates a simplified or full intrinsic matrix.</summary>
    /// <param name="fullIntrinsics">Whether Y focal length and skew are explicitly stored.</param>
    /// <param name="denominatorShift">Power-of-two denominator shift from zero through thirty-one.</param>
    /// <param name="skewDenominatorShift">Skew denominator shift from zero through thirty-one.</param>
    /// <param name="focalLengthX">X focal length numerator.</param>
    /// <param name="principalPointX">X principal-point numerator.</param>
    /// <param name="principalPointY">Y principal-point numerator.</param>
    /// <param name="focalLengthY">Y focal length numerator for the full form.</param>
    /// <param name="skewFactor">Skew numerator for the full form.</param>
    public AvifCameraIntrinsicMatrix(
        bool fullIntrinsics,
        int denominatorShift,
        int skewDenominatorShift,
        int focalLengthX,
        int principalPointX,
        int principalPointY,
        int focalLengthY,
        int skewFactor)
    {
        this.fullIntrinsics = fullIntrinsics;
        this.denominatorShift = denominatorShift;
        this.skewDenominatorShift = skewDenominatorShift;
        this.focalLengthX = focalLengthX;
        this.principalPointX = principalPointX;
        this.principalPointY = principalPointY;
        this.focalLengthY = fullIntrinsics ? focalLengthY : focalLengthX;
        this.skewFactor = fullIntrinsics ? skewFactor : 0;
        code = denominatorShift < 0 || denominatorShift > 31
            || skewDenominatorShift < 0 || skewDenominatorShift > 31
            ? AvifResultCode.InvalidArgument : AvifResultCode.Success;
    }

    internal AvifCameraIntrinsicMatrix CreateCopy()
    {
        return new AvifCameraIntrinsicMatrix(
            fullIntrinsics,
            denominatorShift,
            skewDenominatorShift,
            focalLengthX,
            principalPointX,
            principalPointY,
            focalLengthY,
            skewFactor);
    }

    internal bool ValuesEqual(AvifCameraIntrinsicMatrix other)
    {
        return other != null
            && fullIntrinsics == other.fullIntrinsics
            && denominatorShift == other.denominatorShift
            && skewDenominatorShift == other.skewDenominatorShift
            && focalLengthX == other.focalLengthX
            && principalPointX == other.principalPointX
            && principalPointY == other.principalPointY
            && focalLengthY == other.focalLengthY
            && skewFactor == other.skewFactor;
    }

    /// <summary>Returns the construction result.</summary>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Reports whether separate Y focal length and skew fields are present.</summary>
    /// <returns><c>true</c> for the full matrix form.</returns>
    public bool HasFullIntrinsics()
    {
        return fullIntrinsics;
    }

    /// <summary>Returns the power-of-two denominator shift for the primary fields.</summary>
    /// <returns>A value from zero through thirty-one.</returns>
    public int GetDenominatorShift()
    {
        return denominatorShift;
    }

    /// <summary>Returns the power-of-two denominator shift for skew.</summary>
    /// <returns>A value from zero through thirty-one.</returns>
    public int GetSkewDenominatorShift()
    {
        return skewDenominatorShift;
    }

    /// <summary>Returns the encoded X focal length numerator.</summary>
    /// <returns>The signed numerator.</returns>
    public int GetFocalLengthX()
    {
        return focalLengthX;
    }

    /// <summary>Returns the encoded X principal-point numerator.</summary>
    /// <returns>The signed numerator.</returns>
    public int GetPrincipalPointX()
    {
        return principalPointX;
    }

    /// <summary>Returns the encoded Y principal-point numerator.</summary>
    /// <returns>The signed numerator.</returns>
    public int GetPrincipalPointY()
    {
        return principalPointY;
    }

    /// <summary>Returns the encoded Y focal length numerator.</summary>
    /// <returns>The explicit value, or the X focal length for the simplified form.</returns>
    public int GetFocalLengthY()
    {
        return focalLengthY;
    }

    /// <summary>Returns the encoded skew numerator.</summary>
    /// <returns>The skew numerator, or zero for the simplified form.</returns>
    public int GetSkewFactor()
    {
        return skewFactor;
    }
}
