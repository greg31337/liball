namespace gltf;

/// <summary>Identifies the logical shape of a glTF accessor element.</summary>
public enum GltfAccessorType
{
    /// <summary>One scalar component.</summary>
    Scalar = 0,

    /// <summary>A two-component vector.</summary>
    Vector2 = 1,

    /// <summary>A three-component vector.</summary>
    Vector3 = 2,

    /// <summary>A four-component vector.</summary>
    Vector4 = 3,

    /// <summary>A two-column, two-row matrix.</summary>
    Matrix2 = 4,

    /// <summary>A three-column, three-row matrix.</summary>
    Matrix3 = 5,

    /// <summary>A four-column, four-row matrix.</summary>
    Matrix4 = 6
}
