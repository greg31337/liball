namespace avif;

/// <summary>Defines access to authored camera position and orientation metadata.</summary>
public interface IAvifCameraExtrinsicMatrix
{
    /// <summary>Returns the construction result.</summary>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode GetCode();

    /// <summary>Reports whether X position is present.</summary>
    /// <returns>The presence flag.</returns>
    bool HasPositionX();

    /// <summary>Reports whether Y position is present.</summary>
    /// <returns>The presence flag.</returns>
    bool HasPositionY();

    /// <summary>Reports whether Z position is present.</summary>
    /// <returns>The presence flag.</returns>
    bool HasPositionZ();

    /// <summary>Reports whether quaternion orientation is present.</summary>
    /// <returns>The presence flag.</returns>
    bool HasOrientation();

    /// <summary>Reports whether 32-bit quaternion fields are used.</summary>
    /// <returns>The field-size flag.</returns>
    bool UsesLargeOrientationFields();

    /// <summary>Reports whether a world-coordinate-system identifier is present.</summary>
    /// <returns>The presence flag.</returns>
    bool HasWorldCoordinateSystemId();

    /// <summary>Returns the encoded X position.</summary>
    /// <returns>The position, or zero when absent.</returns>
    int GetPositionX();

    /// <summary>Returns the encoded Y position.</summary>
    /// <returns>The position, or zero when absent.</returns>
    int GetPositionY();

    /// <summary>Returns the encoded Z position.</summary>
    /// <returns>The position, or zero when absent.</returns>
    int GetPositionZ();

    /// <summary>Returns the encoded quaternion X numerator.</summary>
    /// <returns>The numerator, or zero when absent.</returns>
    int GetQuaternionXRaw();

    /// <summary>Returns the encoded quaternion Y numerator.</summary>
    /// <returns>The numerator, or zero when absent.</returns>
    int GetQuaternionYRaw();

    /// <summary>Returns the encoded quaternion Z numerator.</summary>
    /// <returns>The numerator, or zero when absent.</returns>
    int GetQuaternionZRaw();

    /// <summary>Returns the world-coordinate-system identifier.</summary>
    /// <returns>The identifier, or zero when absent.</returns>
    uint GetWorldCoordinateSystemId();
}
