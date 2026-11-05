namespace gltf;

/// <summary>Identifies the topology used to interpret a glTF mesh primitive.</summary>
public enum GltfPrimitiveMode
{
    /// <summary>Individual points, corresponding to glTF constant <c>0</c>.</summary>
    Points = 0,

    /// <summary>Independent line segments, corresponding to glTF constant <c>1</c>.</summary>
    Lines = 1,

    /// <summary>A closed line loop, corresponding to glTF constant <c>2</c>.</summary>
    LineLoop = 2,

    /// <summary>A connected line strip, corresponding to glTF constant <c>3</c>.</summary>
    LineStrip = 3,

    /// <summary>Independent triangles, corresponding to glTF constant <c>4</c>.</summary>
    Triangles = 4,

    /// <summary>A connected triangle strip, corresponding to glTF constant <c>5</c>.</summary>
    TriangleStrip = 5,

    /// <summary>A connected triangle fan, corresponding to glTF constant <c>6</c>.</summary>
    TriangleFan = 6
}
