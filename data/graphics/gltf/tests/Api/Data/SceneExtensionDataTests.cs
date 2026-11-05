using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Data;

[TestFixture]
public class SceneExtensionDataTests
{
    [Test]
    public void TextureTransformAndNodeCapabilities_MatchSchemaDefaults()
    {
        GltfTextureTransformExtension firstTransform = new GltfTextureTransformExtension();
        GltfTextureTransformExtension secondTransform = new GltfTextureTransformExtension();
        GltfNodeVisibilityExtension visibility = new GltfNodeVisibilityExtension();
        GltfNodeHoverabilityExtension hoverability = new GltfNodeHoverabilityExtension();
        GltfNodeSelectabilityExtension selectability = new GltfNodeSelectabilityExtension();

        firstTransform.Offset[0] = 1.0;
        firstTransform.Scale[0] = 2.0;

        Assert.Multiple(() =>
        {
            Assert.That(firstTransform.HasOffset, Is.False);
            Assert.That(secondTransform.Offset, Is.EqualTo(new[] { 0.0, 0.0 }));
            Assert.That(firstTransform.Offset, Is.Not.SameAs(secondTransform.Offset));
            Assert.That(firstTransform.HasRotation, Is.False);
            Assert.That(firstTransform.Rotation, Is.Zero);
            Assert.That(firstTransform.HasScale, Is.False);
            Assert.That(secondTransform.Scale, Is.EqualTo(new[] { 1.0, 1.0 }));
            Assert.That(firstTransform.Scale, Is.Not.SameAs(secondTransform.Scale));
            Assert.That(firstTransform.HasTextureCoordinate, Is.False);
            Assert.That(firstTransform.TextureCoordinate, Is.Zero);
            Assert.That(visibility.HasVisible, Is.False);
            Assert.That(visibility.Visible, Is.True);
            Assert.That(hoverability.HasHoverable, Is.False);
            Assert.That(hoverability.Hoverable, Is.True);
            Assert.That(selectability.HasSelectable, Is.False);
            Assert.That(selectability.Selectable, Is.True);
        });
    }

    [Test]
    public void PunctualLightData_PreservesRequiredReferencesAndOptionalDefaults()
    {
        GltfLightsPunctualExtension firstRoot = new GltfLightsPunctualExtension();
        GltfLightsPunctualExtension secondRoot = new GltfLightsPunctualExtension();
        GltfPunctualLight firstLight = new GltfPunctualLight();
        GltfPunctualLight secondLight = new GltfPunctualLight();
        GltfPunctualLightSpot spot = new GltfPunctualLightSpot();
        GltfPunctualLightNodeExtension node = new GltfPunctualLightNodeExtension();

        firstRoot.Lights.Add(firstLight);
        firstLight.Color[0] = 0.5;

        Assert.Multiple(() =>
        {
            Assert.That(secondRoot.Lights, Is.Empty);
            Assert.That(firstRoot.Lights, Is.Not.SameAs(secondRoot.Lights));
            Assert.That(firstLight.Type, Is.Empty);
            Assert.That(firstLight.HasColor, Is.False);
            Assert.That(secondLight.Color, Is.EqualTo(new[] { 1.0, 1.0, 1.0 }));
            Assert.That(firstLight.Color, Is.Not.SameAs(secondLight.Color));
            Assert.That(firstLight.HasIntensity, Is.False);
            Assert.That(firstLight.Intensity, Is.EqualTo(1.0));
            Assert.That(firstLight.HasRange, Is.False);
            Assert.That(firstLight.Range, Is.Zero);
            Assert.That(firstLight.Spot, Is.Null);
            Assert.That(firstLight.Name, Is.Null);
            Assert.That(spot.HasInnerConeAngle, Is.False);
            Assert.That(spot.InnerConeAngle, Is.Zero);
            Assert.That(spot.HasOuterConeAngle, Is.False);
            Assert.That(spot.OuterConeAngle, Is.EqualTo(0.7853981633974483));
            Assert.That(node.Light, Is.EqualTo(GltfConstants.AbsentIndex));
        });
    }

    [Test]
    public void ReferenceAndPrimitiveExtensions_OwnMappingsAndSnapshotDefaults()
    {
        GltfMeshGpuInstancingExtension firstInstancing = new GltfMeshGpuInstancingExtension();
        GltfMeshGpuInstancingExtension secondInstancing = new GltfMeshGpuInstancingExtension();
        GltfAnimationPointerExtension pointer = new GltfAnimationPointerExtension();
        GltfMaterialsVariantsExtension variants = new GltfMaterialsVariantsExtension();
        GltfMaterialsVariantsPrimitiveExtension primitiveVariants = new GltfMaterialsVariantsPrimitiveExtension();
        GltfMaterialVariant variant = new GltfMaterialVariant();
        GltfMaterialVariantMapping mapping = new GltfMaterialVariantMapping();
        GltfGaussianSplattingExtension gaussian = new GltfGaussianSplattingExtension();

        firstInstancing.Attributes["TRANSLATION"] = 0;
        mapping.Variants.Add(0);
        variants.Variants.Add(variant);
        primitiveVariants.Mappings.Add(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(secondInstancing.Attributes, Is.Empty);
            Assert.That(firstInstancing.Attributes, Is.Not.SameAs(secondInstancing.Attributes));
            Assert.That(pointer.Pointer, Is.Empty);
            Assert.That(variant.Name, Is.Empty);
            Assert.That(variants.Variants[0], Is.SameAs(variant));
            Assert.That(mapping.Material, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(mapping.Name, Is.Null);
            Assert.That(mapping.Variants, Is.EqualTo(new[] { 0 }));
            Assert.That(primitiveVariants.Mappings[0], Is.SameAs(mapping));
            Assert.That(gaussian.Kernel, Is.Empty);
            Assert.That(gaussian.HasKernel, Is.False);
            Assert.That(gaussian.ColorSpace, Is.Empty);
            Assert.That(gaussian.HasColorSpace, Is.False);
            Assert.That(gaussian.HasProjection, Is.False);
            Assert.That(gaussian.Projection, Is.EqualTo("perspective"));
            Assert.That(gaussian.HasSortingMethod, Is.False);
            Assert.That(gaussian.SortingMethod, Is.EqualTo("cameraDistance"));
        });
    }

    [Test]
    public void InteractivityData_ModelsTypedGraphsAndSocketDefaults()
    {
        GltfInteractivityExtension extension = new GltfInteractivityExtension();
        GltfInteractivityGraph graph = new GltfInteractivityGraph();
        GltfInteractivityType type = new GltfInteractivityType();
        GltfInteractivityVariable variable = new GltfInteractivityVariable();
        GltfInteractivityEvent eventData = new GltfInteractivityEvent();
        GltfInteractivityEventValue eventValue = new GltfInteractivityEventValue();
        GltfInteractivityDeclaration declaration = new GltfInteractivityDeclaration();
        GltfInteractivityDeclarationValueSocket declarationSocket = new GltfInteractivityDeclarationValueSocket();
        GltfInteractivityNode node = new GltfInteractivityNode();
        GltfInteractivityConfiguration configuration = new GltfInteractivityConfiguration();
        GltfInteractivityValueSocket valueSocket = new GltfInteractivityValueSocket();
        GltfInteractivityFlowSocket flowSocket = new GltfInteractivityFlowSocket();

        variable.Value = new JsonArray(1.0);
        graph.Types.Add(type);
        graph.Variables.Add(variable);
        graph.Events.Add(eventData);
        graph.Declarations.Add(declaration);
        graph.Nodes.Add(node);
        extension.Graphs.Add(graph);

        Assert.Multiple(() =>
        {
            Assert.That(extension.HasDefaultGraph, Is.False);
            Assert.That(extension.HasGraphs, Is.False);
            Assert.That(extension.DefaultGraph, Is.Zero);
            Assert.That(extension.Graphs[0], Is.SameAs(graph));
            Assert.That(graph.Name, Is.Null);
            Assert.That(graph.HasTypes, Is.False);
            Assert.That(graph.HasVariables, Is.False);
            Assert.That(graph.HasEvents, Is.False);
            Assert.That(graph.HasDeclarations, Is.False);
            Assert.That(graph.HasNodes, Is.False);
            Assert.That(type.HasSignature, Is.False);
            Assert.That(type.Signature, Is.Empty);
            Assert.That(variable.Type, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(variable.HasType, Is.False);
            Assert.That(variable.HasValue, Is.False);
            Assert.That(variable.Value, Has.Count.EqualTo(1));
            Assert.That(variable.Name, Is.Null);
            Assert.That(eventData.Id, Is.Null);
            Assert.That(eventData.HasId, Is.False);
            Assert.That(eventData.HasValues, Is.False);
            Assert.That(eventData.Values, Is.Empty);
            Assert.That(eventData.Name, Is.Null);
            Assert.That(eventValue.Type, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(eventValue.HasType, Is.False);
            Assert.That(eventValue.HasValue, Is.False);
            Assert.That(eventValue.Value, Is.Null);
            Assert.That(declaration.Operation, Is.Empty);
            Assert.That(declaration.HasOperation, Is.False);
            Assert.That(declaration.HasInputValueSockets, Is.False);
            Assert.That(declaration.HasOutputValueSockets, Is.False);
            Assert.That(declaration.HasExtension, Is.False);
            Assert.That(declaration.InputValueSockets, Is.Empty);
            Assert.That(declaration.OutputValueSockets, Is.Empty);
            Assert.That(declaration.Extension, Is.Null);
            Assert.That(declarationSocket.Type, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(declarationSocket.HasType, Is.False);
            Assert.That(node.Declaration, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(node.HasDeclaration, Is.False);
            Assert.That(node.HasConfiguration, Is.False);
            Assert.That(node.HasValues, Is.False);
            Assert.That(node.HasFlows, Is.False);
            Assert.That(node.Configuration, Is.Empty);
            Assert.That(node.Values, Is.Empty);
            Assert.That(node.Flows, Is.Empty);
            Assert.That(configuration.Value, Is.Null);
            Assert.That(configuration.HasValue, Is.False);
            Assert.That(valueSocket.HasNode, Is.False);
            Assert.That(valueSocket.Node, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(valueSocket.HasSocket, Is.False);
            Assert.That(valueSocket.Socket, Is.EqualTo("value"));
            Assert.That(valueSocket.HasType, Is.False);
            Assert.That(valueSocket.Type, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(valueSocket.HasValue, Is.False);
            Assert.That(valueSocket.Value, Is.Null);
            Assert.That(flowSocket.Node, Is.EqualTo(GltfConstants.AbsentIndex));
            Assert.That(flowSocket.HasNode, Is.False);
            Assert.That(flowSocket.HasSocket, Is.False);
            Assert.That(flowSocket.Socket, Is.EqualTo("in"));
        });
    }
}
