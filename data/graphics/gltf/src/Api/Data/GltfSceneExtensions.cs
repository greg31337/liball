using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

/// <summary>Represents the <c>KHR_texture_transform</c> texture-info payload.</summary>
public class GltfTextureTransformExtension
{
    /// <summary>Whether the texture-coordinate offset was explicitly provided.</summary>
    public bool HasOffset;

    /// <summary>The two-dimensional texture-coordinate offset.</summary>
    public double[] Offset = [0.0, 0.0];

    /// <summary>Whether texture-coordinate rotation was explicitly provided.</summary>
    public bool HasRotation;

    /// <summary>The counter-clockwise texture-coordinate rotation in radians.</summary>
    public double Rotation;

    /// <summary>Whether texture-coordinate scale was explicitly provided.</summary>
    public bool HasScale;

    /// <summary>The two-dimensional texture-coordinate scale.</summary>
    public double[] Scale = [1.0, 1.0];

    /// <summary>Whether a texture-coordinate set override was provided.</summary>
    public bool HasTextureCoordinate;

    /// <summary>The texture-coordinate set override.</summary>
    public int TextureCoordinate;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the root <c>KHR_lights_punctual</c> payload.</summary>
public class GltfLightsPunctualExtension
{
    /// <summary>The punctual light definitions.</summary>
    public List<GltfPunctualLight> Lights = new List<GltfPunctualLight>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one punctual light definition.</summary>
public class GltfPunctualLight
{
    /// <summary>Whether the light color was explicitly provided.</summary>
    public bool HasColor;

    /// <summary>The linear RGB light color.</summary>
    public double[] Color = [1.0, 1.0, 1.0];

    /// <summary>Whether light intensity was explicitly provided.</summary>
    public bool HasIntensity;

    /// <summary>The light intensity in the units defined by its type.</summary>
    public double Intensity = 1.0;

    /// <summary>The required light type string.</summary>
    public string Type = "";

    /// <summary>Whether a finite light range was explicitly provided.</summary>
    public bool HasRange;

    /// <summary>The distance cutoff of the light.</summary>
    public double Range;

    /// <summary>The optional spot-light cone definition.</summary>
    public GltfPunctualLightSpot? Spot;

    /// <summary>The optional user-defined light name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the cone definition of a punctual spot light.</summary>
public class GltfPunctualLightSpot
{
    /// <summary>Whether the inner cone angle was explicitly provided.</summary>
    public bool HasInnerConeAngle;

    /// <summary>The inner cone angle in radians.</summary>
    public double InnerConeAngle;

    /// <summary>Whether the outer cone angle was explicitly provided.</summary>
    public bool HasOuterConeAngle;

    /// <summary>The outer cone angle in radians.</summary>
    public double OuterConeAngle = 0.7853981633974483;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents a node reference into root <c>KHR_lights_punctual</c> data.</summary>
public class GltfPunctualLightNodeExtension
{
    /// <summary>The referenced punctual light index.</summary>
    public int Light = GltfConstants.AbsentIndex;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_node_visibility</c> node payload.</summary>
public class GltfNodeVisibilityExtension
{
    /// <summary>Whether visibility was explicitly provided.</summary>
    public bool HasVisible;

    /// <summary>Whether the node and its descendants are visible.</summary>
    public bool Visible = true;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the snapshot <c>KHR_node_hoverability</c> node payload.</summary>
public class GltfNodeHoverabilityExtension
{
    /// <summary>Whether hoverability was explicitly provided.</summary>
    public bool HasHoverable;

    /// <summary>Whether the node and its descendants respond to hover actions.</summary>
    public bool Hoverable = true;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the snapshot <c>KHR_node_selectability</c> node payload.</summary>
public class GltfNodeSelectabilityExtension
{
    /// <summary>Whether selectability was explicitly provided.</summary>
    public bool HasSelectable;

    /// <summary>Whether the node and its descendants respond to select actions.</summary>
    public bool Selectable = true;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>EXT_mesh_gpu_instancing</c> node payload.</summary>
public class GltfMeshGpuInstancingExtension
{
    /// <summary>Instance attribute accessor indices keyed by semantic.</summary>
    public Dictionary<string, int> Attributes = new Dictionary<string, int>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the <c>KHR_animation_pointer</c> channel-target payload.</summary>
public class GltfAnimationPointerExtension
{
    /// <summary>The required RFC 6901 pointer to the animated property.</summary>
    public string Pointer = "";

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents root <c>KHR_materials_variants</c> definitions.</summary>
public class GltfMaterialsVariantsExtension
{
    /// <summary>The material variant definitions.</summary>
    public List<GltfMaterialVariant> Variants = new List<GltfMaterialVariant>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one named material variant.</summary>
public class GltfMaterialVariant
{
    /// <summary>The required variant name.</summary>
    public string Name = "";

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents primitive <c>KHR_materials_variants</c> mappings.</summary>
public class GltfMaterialsVariantsPrimitiveExtension
{
    /// <summary>The primitive material-to-variant mappings.</summary>
    public List<GltfMaterialVariantMapping> Mappings = new List<GltfMaterialVariantMapping>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Associates a material with one or more material variants.</summary>
public class GltfMaterialVariantMapping
{
    /// <summary>The referenced variant indices.</summary>
    public List<int> Variants = new List<int>();

    /// <summary>The referenced material index.</summary>
    public int Material = GltfConstants.AbsentIndex;

    /// <summary>The optional user-defined mapping name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents snapshot metadata for <c>KHR_gaussian_splatting</c>.</summary>
public class GltfGaussianSplattingExtension
{
    /// <summary>Whether the required kernel member was explicitly provided.</summary>
    public bool HasKernel;

    /// <summary>The required Gaussian reconstruction kernel.</summary>
    public string Kernel = "";

    /// <summary>Whether the required color-space member was explicitly provided.</summary>
    public bool HasColorSpace;

    /// <summary>The required reconstructed color space.</summary>
    public string ColorSpace = "";

    /// <summary>Whether a projection method was explicitly provided.</summary>
    public bool HasProjection;

    /// <summary>The Gaussian projection method.</summary>
    public string Projection = "perspective";

    /// <summary>Whether a sorting method was explicitly provided.</summary>
    public bool HasSortingMethod;

    /// <summary>The Gaussian sorting method.</summary>
    public string SortingMethod = "cameraDistance";

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents the root entry point of snapshot <c>KHR_interactivity</c> data.</summary>
public class GltfInteractivityExtension
{
    /// <summary>Whether the required graphs member was explicitly provided.</summary>
    public bool HasGraphs;

    /// <summary>The behavior graph definitions.</summary>
    public List<GltfInteractivityGraph> Graphs = new List<GltfInteractivityGraph>();

    /// <summary>Whether the default behavior graph was explicitly provided.</summary>
    public bool HasDefaultGraph;

    /// <summary>The default behavior graph index.</summary>
    public int DefaultGraph;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one <c>KHR_interactivity</c> behavior graph.</summary>
public class GltfInteractivityGraph
{
    /// <summary>Whether the types collection was explicitly provided.</summary>
    public bool HasTypes;

    /// <summary>The graph value type definitions.</summary>
    public List<GltfInteractivityType> Types = new List<GltfInteractivityType>();

    /// <summary>Whether the variables collection was explicitly provided.</summary>
    public bool HasVariables;

    /// <summary>The graph variables.</summary>
    public List<GltfInteractivityVariable> Variables = new List<GltfInteractivityVariable>();

    /// <summary>Whether the events collection was explicitly provided.</summary>
    public bool HasEvents;

    /// <summary>The graph events.</summary>
    public List<GltfInteractivityEvent> Events = new List<GltfInteractivityEvent>();

    /// <summary>Whether the declarations collection was explicitly provided.</summary>
    public bool HasDeclarations;

    /// <summary>The graph operation declarations.</summary>
    public List<GltfInteractivityDeclaration> Declarations = new List<GltfInteractivityDeclaration>();

    /// <summary>Whether the nodes collection was explicitly provided.</summary>
    public bool HasNodes;

    /// <summary>The graph operation nodes.</summary>
    public List<GltfInteractivityNode> Nodes = new List<GltfInteractivityNode>();

    /// <summary>The optional user-defined graph name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one <c>KHR_interactivity</c> value type.</summary>
public class GltfInteractivityType
{
    /// <summary>Whether the required signature member was explicitly provided.</summary>
    public bool HasSignature;

    /// <summary>The required value type signature.</summary>
    public string Signature = "";

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one <c>KHR_interactivity</c> graph variable.</summary>
public class GltfInteractivityVariable
{
    /// <summary>Whether the required type member was explicitly provided.</summary>
    public bool HasType;

    /// <summary>The referenced graph type index.</summary>
    public int Type = GltfConstants.AbsentIndex;

    /// <summary>Whether an initial value was explicitly provided.</summary>
    public bool HasValue;

    /// <summary>The optional initial value components.</summary>
    public JsonArray? Value;

    /// <summary>The optional user-defined variable name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one <c>KHR_interactivity</c> graph event.</summary>
public class GltfInteractivityEvent
{
    /// <summary>Whether an event identifier was explicitly provided.</summary>
    public bool HasId;

    /// <summary>The optional event identifier.</summary>
    public string? Id;

    /// <summary>Whether event value sockets were explicitly provided.</summary>
    public bool HasValues;

    /// <summary>Event value sockets keyed by socket identifier.</summary>
    public Dictionary<string, GltfInteractivityEventValue> Values = new Dictionary<string, GltfInteractivityEventValue>();

    /// <summary>The optional user-defined event name.</summary>
    public string? Name;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one typed value on a <c>KHR_interactivity</c> event.</summary>
public class GltfInteractivityEventValue
{
    /// <summary>Whether the required type member was explicitly provided.</summary>
    public bool HasType;

    /// <summary>The referenced graph type index.</summary>
    public int Type = GltfConstants.AbsentIndex;

    /// <summary>Whether an initial value was explicitly provided.</summary>
    public bool HasValue;

    /// <summary>The optional initial value components.</summary>
    public JsonArray? Value;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one <c>KHR_interactivity</c> operation declaration.</summary>
public class GltfInteractivityDeclaration
{
    /// <summary>Whether the required operation member was explicitly provided.</summary>
    public bool HasOperation;

    /// <summary>The required operation identifier.</summary>
    public string Operation = "";

    /// <summary>Whether input value socket declarations were explicitly provided.</summary>
    public bool HasInputValueSockets;

    /// <summary>Input value socket declarations keyed by socket identifier.</summary>
    public Dictionary<string, GltfInteractivityDeclarationValueSocket> InputValueSockets = new Dictionary<string, GltfInteractivityDeclarationValueSocket>();

    /// <summary>Whether output value socket declarations were explicitly provided.</summary>
    public bool HasOutputValueSockets;

    /// <summary>Output value socket declarations keyed by socket identifier.</summary>
    public Dictionary<string, GltfInteractivityDeclarationValueSocket> OutputValueSockets = new Dictionary<string, GltfInteractivityDeclarationValueSocket>();

    /// <summary>Whether an extension owner was explicitly provided.</summary>
    public bool HasExtension;

    /// <summary>The optional extension defining the operation.</summary>
    public string? Extension;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents a typed value socket on an interactivity declaration.</summary>
public class GltfInteractivityDeclarationValueSocket
{
    /// <summary>Whether the required type member was explicitly provided.</summary>
    public bool HasType;

    /// <summary>The referenced graph type index.</summary>
    public int Type = GltfConstants.AbsentIndex;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one operation node in a <c>KHR_interactivity</c> graph.</summary>
public class GltfInteractivityNode
{
    /// <summary>Whether the required declaration member was explicitly provided.</summary>
    public bool HasDeclaration;

    /// <summary>The referenced operation declaration index.</summary>
    public int Declaration = GltfConstants.AbsentIndex;

    /// <summary>Whether configuration values were explicitly provided.</summary>
    public bool HasConfiguration;

    /// <summary>Configuration values keyed by configuration identifier.</summary>
    public Dictionary<string, GltfInteractivityConfiguration> Configuration = new Dictionary<string, GltfInteractivityConfiguration>();

    /// <summary>Whether input value sockets were explicitly provided.</summary>
    public bool HasValues;

    /// <summary>Input value sockets keyed by socket identifier.</summary>
    public Dictionary<string, GltfInteractivityValueSocket> Values = new Dictionary<string, GltfInteractivityValueSocket>();

    /// <summary>Whether output flow sockets were explicitly provided.</summary>
    public bool HasFlows;

    /// <summary>Output flow sockets keyed by socket identifier.</summary>
    public Dictionary<string, GltfInteractivityFlowSocket> Flows = new Dictionary<string, GltfInteractivityFlowSocket>();

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one interactivity operation configuration value.</summary>
public class GltfInteractivityConfiguration
{
    /// <summary>Whether a configuration value was explicitly provided.</summary>
    public bool HasValue;

    /// <summary>The optional configuration value components.</summary>
    public JsonArray? Value;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one interactivity input value socket.</summary>
public class GltfInteractivityValueSocket
{
    /// <summary>Whether a source node index was provided.</summary>
    public bool HasNode;

    /// <summary>The source interactivity node index.</summary>
    public int Node = GltfConstants.AbsentIndex;

    /// <summary>Whether an output socket identifier was explicitly provided.</summary>
    public bool HasSocket;

    /// <summary>The output socket identifier.</summary>
    public string Socket = "value";

    /// <summary>Whether an inline value type index was provided.</summary>
    public bool HasType;

    /// <summary>The inline value type index.</summary>
    public int Type = GltfConstants.AbsentIndex;

    /// <summary>Whether an inline value was explicitly provided.</summary>
    public bool HasValue;

    /// <summary>The optional inline value components.</summary>
    public JsonArray? Value;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}

/// <summary>Represents one interactivity output flow socket.</summary>
public class GltfInteractivityFlowSocket
{
    /// <summary>Whether the required destination node was explicitly provided.</summary>
    public bool HasNode;

    /// <summary>The destination interactivity node index.</summary>
    public int Node = GltfConstants.AbsentIndex;

    /// <summary>Whether an input socket identifier was explicitly provided.</summary>
    public bool HasSocket;

    /// <summary>The destination input socket identifier.</summary>
    public string Socket = "in";

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
