namespace gltf;

/// <summary>Provides standard glTF attribute semantics and animation target path strings.</summary>
public static class GltfSemantics
{
    /// <summary>The vertex position attribute semantic.</summary>
    public const string Position = "POSITION";

    /// <summary>The vertex normal attribute semantic.</summary>
    public const string Normal = "NORMAL";

    /// <summary>The vertex tangent attribute semantic.</summary>
    public const string Tangent = "TANGENT";

    /// <summary>The prefix of a numbered texture coordinate attribute semantic.</summary>
    public const string TextureCoordinatePrefix = "TEXCOORD_";

    /// <summary>The prefix of a numbered vertex color attribute semantic.</summary>
    public const string ColorPrefix = "COLOR_";

    /// <summary>The prefix of a numbered joint index attribute semantic.</summary>
    public const string JointsPrefix = "JOINTS_";

    /// <summary>The prefix of a numbered joint weight attribute semantic.</summary>
    public const string WeightsPrefix = "WEIGHTS_";

    /// <summary>The required prefix of an application-defined attribute semantic.</summary>
    public const string CustomAttributePrefix = "_";

    /// <summary>The translation animation target path.</summary>
    public const string TranslationPath = "translation";

    /// <summary>The rotation animation target path.</summary>
    public const string RotationPath = "rotation";

    /// <summary>The scale animation target path.</summary>
    public const string ScalePath = "scale";

    /// <summary>The morph weights animation target path.</summary>
    public const string WeightsPath = "weights";

    /// <summary>The <c>KHR_animation_pointer</c> animation target path.</summary>
    public const string PointerPath = "pointer";
}
