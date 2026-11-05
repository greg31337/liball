namespace gltf;

/// <summary>Represents a glTF perspective or orthographic camera.</summary>
public class GltfCamera
{
    /// <summary>The required camera projection type.</summary>
    public GltfCameraType Type = GltfCameraType.Perspective;

    /// <summary>The perspective parameters when <see cref="Type"/> is perspective.</summary>
    public GltfPerspectiveCamera? Perspective;

    /// <summary>The orthographic parameters when <see cref="Type"/> is orthographic.</summary>
    public GltfOrthographicCamera? Orthographic;

    /// <summary>The optional camera name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents a glTF perspective projection.</summary>
public class GltfPerspectiveCamera
{
    /// <summary>Whether the aspect ratio member was present.</summary>
    public bool HasAspectRatio;

    /// <summary>The optional viewport aspect ratio.</summary>
    public double AspectRatio;

    /// <summary>The required vertical field of view in radians.</summary>
    public double VerticalFieldOfView;

    /// <summary>Whether the finite far plane member was present.</summary>
    public bool HasFarPlane;

    /// <summary>The finite far plane distance, ignored when its presence field is false.</summary>
    public double FarPlane;

    /// <summary>The required near plane distance.</summary>
    public double NearPlane;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents a glTF orthographic projection.</summary>
public class GltfOrthographicCamera
{
    /// <summary>The required horizontal magnification.</summary>
    public double HorizontalMagnification;

    /// <summary>The required vertical magnification.</summary>
    public double VerticalMagnification;

    /// <summary>The required far plane distance.</summary>
    public double FarPlane;

    /// <summary>The required near plane distance.</summary>
    public double NearPlane;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
