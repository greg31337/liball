namespace gltf;

/// <summary>Identifies a glTF accessor component representation.</summary>
public enum GltfComponentType
{
    /// <summary>A signed 8-bit integer component, corresponding to glTF constant <c>5120</c>.</summary>
    SignedByte = 5120,

    /// <summary>An unsigned 8-bit integer component, corresponding to glTF constant <c>5121</c>.</summary>
    UnsignedByte = 5121,

    /// <summary>A signed 16-bit integer component, corresponding to glTF constant <c>5122</c>.</summary>
    SignedShort = 5122,

    /// <summary>An unsigned 16-bit integer component, corresponding to glTF constant <c>5123</c>.</summary>
    UnsignedShort = 5123,

    /// <summary>An unsigned 32-bit integer component, corresponding to glTF constant <c>5125</c>.</summary>
    UnsignedInt = 5125,

    /// <summary>An IEEE 754 single-precision component, corresponding to glTF constant <c>5126</c>.</summary>
    Float = 5126
}
