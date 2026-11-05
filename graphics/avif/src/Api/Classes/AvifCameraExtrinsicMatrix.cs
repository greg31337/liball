namespace avif;

/// <summary>Describes optional camera position, orientation, and coordinate-system fields.</summary>
public class AvifCameraExtrinsicMatrix : IAvifCameraExtrinsicMatrix
{
    AvifResultCode code;
    bool positionXPresent;
    bool positionYPresent;
    bool positionZPresent;
    bool orientationPresent;
    bool largeOrientation;
    bool worldCoordinateSystemIdPresent;
    int positionX;
    int positionY;
    int positionZ;
    int quaternionX;
    int quaternionY;
    int quaternionZ;
    uint worldCoordinateSystemId;

    /// <summary>Creates camera extrinsic metadata with explicit presence flags.</summary>
    /// <param name="positionXPresent">Whether X position is stored.</param>
    /// <param name="positionYPresent">Whether Y position is stored.</param>
    /// <param name="positionZPresent">Whether Z position is stored.</param>
    /// <param name="orientationPresent">Whether quaternion X, Y, and Z are stored.</param>
    /// <param name="largeOrientation">Whether quaternion fields use 32 bits instead of 16.</param>
    /// <param name="worldCoordinateSystemIdPresent">Whether the coordinate-system identifier is stored.</param>
    /// <param name="positionX">Encoded X position.</param>
    /// <param name="positionY">Encoded Y position.</param>
    /// <param name="positionZ">Encoded Z position.</param>
    /// <param name="quaternionX">Encoded quaternion X numerator.</param>
    /// <param name="quaternionY">Encoded quaternion Y numerator.</param>
    /// <param name="quaternionZ">Encoded quaternion Z numerator.</param>
    /// <param name="worldCoordinateSystemId">Coordinate-system identifier.</param>
    public AvifCameraExtrinsicMatrix(
        bool positionXPresent,
        bool positionYPresent,
        bool positionZPresent,
        bool orientationPresent,
        bool largeOrientation,
        bool worldCoordinateSystemIdPresent,
        int positionX,
        int positionY,
        int positionZ,
        int quaternionX,
        int quaternionY,
        int quaternionZ,
        uint worldCoordinateSystemId)
    {
        this.positionXPresent = positionXPresent;
        this.positionYPresent = positionYPresent;
        this.positionZPresent = positionZPresent;
        this.orientationPresent = orientationPresent;
        this.largeOrientation = largeOrientation;
        this.worldCoordinateSystemIdPresent = worldCoordinateSystemIdPresent;
        this.positionX = positionXPresent ? positionX : 0;
        this.positionY = positionYPresent ? positionY : 0;
        this.positionZ = positionZPresent ? positionZ : 0;
        this.quaternionX = orientationPresent ? quaternionX : 0;
        this.quaternionY = orientationPresent ? quaternionY : 0;
        this.quaternionZ = orientationPresent ? quaternionZ : 0;
        this.worldCoordinateSystemId = worldCoordinateSystemIdPresent
            ? worldCoordinateSystemId : 0;
        code = Validate();
    }

    internal AvifCameraExtrinsicMatrix CreateCopy()
    {
        return new AvifCameraExtrinsicMatrix(
            positionXPresent,
            positionYPresent,
            positionZPresent,
            orientationPresent,
            largeOrientation,
            worldCoordinateSystemIdPresent,
            positionX,
            positionY,
            positionZ,
            quaternionX,
            quaternionY,
            quaternionZ,
            worldCoordinateSystemId);
    }

    internal bool ValuesEqual(AvifCameraExtrinsicMatrix other)
    {
        return other != null
            && positionXPresent == other.positionXPresent
            && positionYPresent == other.positionYPresent
            && positionZPresent == other.positionZPresent
            && orientationPresent == other.orientationPresent
            && largeOrientation == other.largeOrientation
            && worldCoordinateSystemIdPresent == other.worldCoordinateSystemIdPresent
            && positionX == other.positionX
            && positionY == other.positionY
            && positionZ == other.positionZ
            && quaternionX == other.quaternionX
            && quaternionY == other.quaternionY
            && quaternionZ == other.quaternionZ
            && worldCoordinateSystemId == other.worldCoordinateSystemId;
    }

    AvifResultCode Validate()
    {
        if (largeOrientation && !orientationPresent)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (!orientationPresent)
        {
            return AvifResultCode.Success;
        }
        if (!largeOrientation
            && (quaternionX < short.MinValue || quaternionX > short.MaxValue
                || quaternionY < short.MinValue || quaternionY > short.MaxValue
                || quaternionZ < short.MinValue || quaternionZ > short.MaxValue))
        {
            return AvifResultCode.InvalidArgument;
        }
        double divisor = largeOrientation ? 1073741824.0 : 16384.0;
        double x = quaternionX / divisor;
        double y = quaternionY / divisor;
        double z = quaternionZ / divisor;
        return x * x + y * y + z * z <= 1.0
            ? AvifResultCode.Success : AvifResultCode.InvalidArgument;
    }

    /// <summary>Returns the construction result.</summary>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Reports whether X position is present.</summary>
    /// <returns>The presence flag.</returns>
    public bool HasPositionX()
    {
        return positionXPresent;
    }

    /// <summary>Reports whether Y position is present.</summary>
    /// <returns>The presence flag.</returns>
    public bool HasPositionY()
    {
        return positionYPresent;
    }

    /// <summary>Reports whether Z position is present.</summary>
    /// <returns>The presence flag.</returns>
    public bool HasPositionZ()
    {
        return positionZPresent;
    }

    /// <summary>Reports whether quaternion orientation is present.</summary>
    /// <returns>The presence flag.</returns>
    public bool HasOrientation()
    {
        return orientationPresent;
    }

    /// <summary>Reports whether 32-bit quaternion fields are used.</summary>
    /// <returns>The field-size flag.</returns>
    public bool UsesLargeOrientationFields()
    {
        return largeOrientation;
    }

    /// <summary>Reports whether a world-coordinate-system identifier is present.</summary>
    /// <returns>The presence flag.</returns>
    public bool HasWorldCoordinateSystemId()
    {
        return worldCoordinateSystemIdPresent;
    }

    /// <summary>Returns the encoded X position.</summary>
    /// <returns>The position, or zero when absent.</returns>
    public int GetPositionX()
    {
        return positionX;
    }

    /// <summary>Returns the encoded Y position.</summary>
    /// <returns>The position, or zero when absent.</returns>
    public int GetPositionY()
    {
        return positionY;
    }

    /// <summary>Returns the encoded Z position.</summary>
    /// <returns>The position, or zero when absent.</returns>
    public int GetPositionZ()
    {
        return positionZ;
    }

    /// <summary>Returns the encoded quaternion X numerator.</summary>
    /// <returns>The numerator, or zero when absent.</returns>
    public int GetQuaternionXRaw()
    {
        return quaternionX;
    }

    /// <summary>Returns the encoded quaternion Y numerator.</summary>
    /// <returns>The numerator, or zero when absent.</returns>
    public int GetQuaternionYRaw()
    {
        return quaternionY;
    }

    /// <summary>Returns the encoded quaternion Z numerator.</summary>
    /// <returns>The numerator, or zero when absent.</returns>
    public int GetQuaternionZRaw()
    {
        return quaternionZ;
    }

    /// <summary>Returns the world-coordinate-system identifier.</summary>
    /// <returns>The identifier, or zero when absent.</returns>
    public uint GetWorldCoordinateSystemId()
    {
        return worldCoordinateSystemId;
    }
}
