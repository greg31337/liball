using System.Collections.Generic;

namespace gltf;

internal class AdvancedExtensionValidator
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal AdvancedExtensionJsonReader Reader = new AdvancedExtensionJsonReader();
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal MeshPrimitiveValidator Meshes = new MeshPrimitiveValidator();
    internal AttributeSemanticParser Semantics = new AttributeSemanticParser();

    internal GltfExtensionResult ValidateAdvancedExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result;
        GltfTypedExtensionData? typed;
        if (context.TypedData == null)
        {
            result = Reader.ReadAdvancedExtension(context, registry);
            typed = result.TypedData;
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, registry);
            typed = context.TypedData;
            result.TypedData = typed;
        }

        if (context.Name == GltfExtensionNames.KhrGaussianSplatting)
        {
            ValidateGaussian(context, result, typed);
        }
        else
        {
            ValidateInteractivity(context, result, typed);
        }

        return result;
    }

    internal void ValidateGaussian(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTypedExtensionData? typed)
    {
        string pointer = Reader.CreatePointer(context);
        if (context.ObjectLocation != "mesh.primitive")
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                "KHR_gaussian_splatting is valid only on mesh primitives.",
                pointer);
        }

        if (typed == null || typed.GaussianSplatting == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The extension must contain Gaussian splatting metadata.",
                pointer);
            return;
        }

        GltfGaussianSplattingExtension value = typed.GaussianSplatting;
        if (!value.HasKernel)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The Gaussian kernel is required.",
                pointer + "/kernel");
        }

        if (!value.HasColorSpace)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The Gaussian color space is required.",
                pointer + "/colorSpace");
        }

        bool needsExtension = (value.HasKernel && value.Kernel != "ellipse")
            || (value.HasColorSpace
                && value.ColorSpace != "srgb_rec709_display"
                && value.ColorSpace != "lin_rec709_display")
            || (value.HasProjection && value.Projection != "perspective")
            || (value.HasSortingMethod
                && value.SortingMethod != "cameraDistance");
        if (needsExtension && value.PropertyJson.Extensions.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "Snapshot-external Gaussian values require a defining nested extension.",
                pointer);
        }

        if (value.Kernel != "ellipse")
        {
            return;
        }

        GltfMeshPrimitive? primitive = ResolvePrimitive(context);
        if (primitive == null)
        {
            if (context.Document != null)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    "The Gaussian splatting mesh primitive does not resolve.",
                    pointer);
            }

            return;
        }

        if (primitive.Mode != GltfPrimitiveMode.Points)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "Ellipse Gaussian splats must use POINTS primitive mode.",
                CreatePrimitivePointer(context) + "/mode");
        }

        ValidateRequiredGaussianAttribute(
            context,
            result,
            primitive,
            "POSITION",
            1);
        ValidateRequiredGaussianAttribute(
            context,
            result,
            primitive,
            "KHR_gaussian_splatting:ROTATION",
            2);
        ValidateRequiredGaussianAttribute(
            context,
            result,
            primitive,
            "KHR_gaussian_splatting:SCALE",
            3);
        ValidateRequiredGaussianAttribute(
            context,
            result,
            primitive,
            "KHR_gaussian_splatting:OPACITY",
            4);
        ValidateRequiredGaussianAttribute(
            context,
            result,
            primitive,
            "KHR_gaussian_splatting:SH_DEGREE_0_COEF_0",
            5);
        ValidateSphericalHarmonics(context, result, primitive);
    }

    internal void ValidateRequiredGaussianAttribute(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshPrimitive primitive,
        string semantic,
        int kind)
    {
        GltfAttribute? attribute = FindAttribute(primitive, semantic);
        string pointer = CreatePrimitivePointer(context) + "/attributes/"
            + Fallback.EscapePointerSegment(semantic);
        if (attribute == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                $"Ellipse Gaussian splats require the {semantic} attribute.",
                pointer);
            return;
        }

        if (context.Document == null
            || attribute.Accessor < 0
            || attribute.Accessor >= context.Document.Accessors.Count)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The Gaussian splatting attribute accessor does not resolve.",
                pointer);
            return;
        }

        GltfAccessor accessor = context.Document.Accessors[attribute.Accessor];
        bool validFormat = kind == 1
            ? Meshes.ValidateBaseAttributeFormat(
                context.Document,
                Semantics.ParseAttributeSemantic(semantic),
                accessor)
            : HasGaussianFormat(accessor, kind);
        if (!validFormat)
        {
            AddIssue(
                result,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat,
                $"The {semantic} accessor format is invalid for Gaussian splatting.",
                pointer);
            return;
        }

        ValidateGaussianValues(
            context.Document,
            attribute.Accessor,
            kind,
            result,
            pointer);
    }

    internal bool HasGaussianFormat(GltfAccessor accessor, int kind)
    {
        if (kind == 1)
        {
            return accessor.Type == GltfAccessorType.Vector3
                && accessor.ComponentType == GltfComponentType.Float
                && !accessor.Normalized;
        }

        if (kind == 2)
        {
            return accessor.Type == GltfAccessorType.Vector4
                && ((accessor.ComponentType == GltfComponentType.Float
                        && !accessor.Normalized)
                    || (accessor.Normalized
                        && (accessor.ComponentType == GltfComponentType.SignedByte
                            || accessor.ComponentType == GltfComponentType.SignedShort)));
        }

        if (kind == 3)
        {
            return accessor.Type == GltfAccessorType.Vector3
                && ((accessor.ComponentType == GltfComponentType.Float
                        && !accessor.Normalized)
                    || (accessor.ComponentType == GltfComponentType.UnsignedByte
                        || accessor.ComponentType == GltfComponentType.UnsignedShort));
        }

        if (kind == 4)
        {
            return accessor.Type == GltfAccessorType.Scalar
                && ((accessor.ComponentType == GltfComponentType.Float
                        && !accessor.Normalized)
                    || (accessor.Normalized
                        && (accessor.ComponentType == GltfComponentType.UnsignedByte
                            || accessor.ComponentType == GltfComponentType.UnsignedShort)));
        }

        return accessor.Type == GltfAccessorType.Vector3
            && accessor.ComponentType == GltfComponentType.Float
            && !accessor.Normalized;
    }

    internal void ValidateGaussianValues(
        GltfDocument document,
        int accessorIndex,
        int kind,
        GltfExtensionResult result,
        string pointer)
    {
        if (kind != 2 && kind != 3 && kind != 4)
        {
            return;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData data = Accessors.ReadFloatingData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            return;
        }

        if (kind == 2)
        {
            int index = 0;
            while (index + 3 < data.FloatingValues.Length)
            {
                double x = data.FloatingValues[index];
                double y = data.FloatingValues[index + 1];
                double z = data.FloatingValues[index + 2];
                double w = data.FloatingValues[index + 3];
                double lengthSquared = x * x + y * y + z * z + w * w;
                if (System.Math.Abs(lengthSquared - 1.0) > 0.0002)
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.ValueNotInRange,
                        "Gaussian rotations must be normalized quaternions.",
                        pointer);
                    return;
                }

                index += 4;
            }

            return;
        }

        int valueIndex = 0;
        while (valueIndex < data.FloatingValues.Length)
        {
            double value = data.FloatingValues[valueIndex];
            bool invalid = kind == 3 ? value < 0.0 : value < 0.0 || value > 1.0;
            if (invalid)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    kind == 3
                        ? "Gaussian scales must not be negative."
                        : "Gaussian opacities must be between zero and one.",
                    pointer);
                return;
            }

            valueIndex++;
        }
    }

    internal void ValidateSphericalHarmonics(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshPrimitive primitive)
    {
        int degree1 = CountAttributes(
            primitive,
            "KHR_gaussian_splatting:SH_DEGREE_1_COEF_");
        int degree2 = CountAttributes(
            primitive,
            "KHR_gaussian_splatting:SH_DEGREE_2_COEF_");
        int degree3 = CountAttributes(
            primitive,
            "KHR_gaussian_splatting:SH_DEGREE_3_COEF_");
        if ((degree1 != 0 && degree1 != 3)
            || (degree2 != 0 && degree2 != 5)
            || (degree3 != 0 && degree3 != 7)
            || (degree2 > 0 && degree1 != 3)
            || (degree3 > 0 && (degree1 != 3 || degree2 != 5)))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "Spherical harmonic degrees must be complete and contiguous.",
                CreatePrimitivePointer(context) + "/attributes");
        }

        if (degree1 > 0 || degree2 > 0 || degree3 > 0)
        {
            ValidateGaussianDegreeFormats(context, result, primitive, 1, 3);
        }

        if (degree2 > 0 || degree3 > 0)
        {
            ValidateGaussianDegreeFormats(context, result, primitive, 2, 5);
        }

        if (degree3 > 0)
        {
            ValidateGaussianDegreeFormats(context, result, primitive, 3, 7);
        }
    }

    internal void ValidateGaussianDegreeFormats(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshPrimitive primitive,
        int degree,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            string semantic = "KHR_gaussian_splatting:SH_DEGREE_"
                + degree + "_COEF_" + index;
            ValidateRequiredGaussianAttribute(
                context,
                result,
                primitive,
                semantic,
                5);

            index++;
        }
    }

    internal int CountAttributes(GltfMeshPrimitive primitive, string prefix)
    {
        int count = 0;
        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            if (primitive.Attributes[index].Semantic.StartsWith(
                prefix,
                System.StringComparison.Ordinal))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal GltfAttribute? FindAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[index];
            if (attribute.Semantic == semantic)
            {
                return attribute;
            }

            index++;
        }

        return null;
    }

    internal GltfMeshPrimitive? ResolvePrimitive(GltfExtensionContext context)
    {
        if (context.Document == null
            || !context.HasObjectIndex
            || !context.HasSubObjectIndex
            || context.ObjectIndex < 0
            || context.ObjectIndex >= context.Document.Meshes.Count)
        {
            return null;
        }

        GltfMesh mesh = context.Document.Meshes[context.ObjectIndex];
        if (context.SubObjectIndex < 0
            || context.SubObjectIndex >= mesh.Primitives.Count)
        {
            return null;
        }

        return mesh.Primitives[context.SubObjectIndex];
    }

    internal string CreatePrimitivePointer(GltfExtensionContext context)
    {
        if (context.HasObjectIndex && context.HasSubObjectIndex)
        {
            return "/meshes/" + context.ObjectIndex
                + "/primitives/" + context.SubObjectIndex;
        }

        return "/mesh.primitive";
    }

    internal void ValidateInteractivity(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTypedExtensionData? typed)
    {
        string pointer = Reader.CreatePointer(context);
        if (context.ObjectLocation != "glTF")
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                "KHR_interactivity is valid only on the glTF root.",
                pointer);
        }

        if (typed == null || typed.Interactivity == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The extension must contain an interactivity entry point.",
                pointer);
            return;
        }

        GltfInteractivityExtension value = typed.Interactivity;
        if (!value.HasGraphs)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The interactivity graphs collection is required.",
                pointer + "/graphs");
            return;
        }

        if (value.Graphs.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "The interactivity graphs collection must not be empty.",
                pointer + "/graphs");
        }

        if (value.HasDefaultGraph
            && (value.DefaultGraph < 0 || value.DefaultGraph >= value.Graphs.Count))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The default interactivity graph does not resolve.",
                pointer + "/graph");
        }

        int graphIndex = 0;
        while (graphIndex < value.Graphs.Count)
        {
            ValidateGraph(
                context.Document,
                value.Graphs[graphIndex],
                result,
                pointer + "/graphs/" + graphIndex);
            graphIndex++;
        }
    }

    internal void ValidateGraph(
        GltfDocument? document,
        GltfInteractivityGraph graph,
        GltfExtensionResult result,
        string pointer)
    {
        ValidateOptionalCollection(graph.HasTypes, graph.Types.Count, result, pointer + "/types");
        ValidateOptionalCollection(
            graph.HasVariables,
            graph.Variables.Count,
            result,
            pointer + "/variables");
        ValidateOptionalCollection(graph.HasEvents, graph.Events.Count, result, pointer + "/events");
        ValidateOptionalCollection(
            graph.HasDeclarations,
            graph.Declarations.Count,
            result,
            pointer + "/declarations");
        ValidateOptionalCollection(graph.HasNodes, graph.Nodes.Count, result, pointer + "/nodes");

        if (graph.HasVariables && !graph.HasTypes)
        {
            AddDependencyIssue(result, "Variables require graph types.", pointer + "/types");
        }

        if (graph.HasNodes && !graph.HasDeclarations)
        {
            AddDependencyIssue(
                result,
                "Interactivity nodes require declarations.",
                pointer + "/declarations");
        }

        int typeIndex = 0;
        while (typeIndex < graph.Types.Count)
        {
            ValidateType(graph.Types[typeIndex], result, pointer + "/types/" + typeIndex);
            typeIndex++;
        }

        int variableIndex = 0;
        while (variableIndex < graph.Variables.Count)
        {
            GltfInteractivityVariable variable = graph.Variables[variableIndex];
            string variablePointer = pointer + "/variables/" + variableIndex;
            ValidateTypeReference(
                variable.HasType,
                variable.Type,
                graph.Types.Count,
                result,
                variablePointer + "/type");
            ValidateValue(variable.HasValue, variable.Value, result, variablePointer + "/value");
            variableIndex++;
        }

        int eventIndex = 0;
        while (eventIndex < graph.Events.Count)
        {
            ValidateEvent(
                graph.Events[eventIndex],
                graph.Types.Count,
                result,
                pointer + "/events/" + eventIndex);
            eventIndex++;
        }

        int declarationIndex = 0;
        while (declarationIndex < graph.Declarations.Count)
        {
            ValidateDeclaration(
                document,
                graph.Declarations[declarationIndex],
                graph.Types.Count,
                result,
                pointer + "/declarations/" + declarationIndex);
            declarationIndex++;
        }

        int nodeIndex = 0;
        while (nodeIndex < graph.Nodes.Count)
        {
            ValidateNode(
                graph,
                graph.Nodes[nodeIndex],
                result,
                pointer + "/nodes/" + nodeIndex);
            nodeIndex++;
        }

        ValidateGraphCycles(graph, result, pointer + "/nodes");
    }

    internal void ValidateOptionalCollection(
        bool present,
        int count,
        GltfExtensionResult result,
        string pointer)
    {
        if (present && count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "An explicitly present interactivity collection must not be empty.",
                pointer);
        }
    }

    internal void ValidateType(
        GltfInteractivityType type,
        GltfExtensionResult result,
        string pointer)
    {
        if (!type.HasSignature)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The interactivity type signature is required.",
                pointer + "/signature");
        }
        else if (!IsTypeSignature(type.Signature))
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "The interactivity type signature is not recognized by this snapshot.",
                pointer + "/signature");
        }
    }

    internal bool IsTypeSignature(string value)
    {
        return value == "bool"
            || value == "float"
            || value == "float2"
            || value == "float3"
            || value == "float4"
            || value == "float2x2"
            || value == "float3x3"
            || value == "float4x4"
            || value == "int"
            || value == "ref"
            || value == "custom";
    }

    internal void ValidateEvent(
        GltfInteractivityEvent eventValue,
        int typeCount,
        GltfExtensionResult result,
        string pointer)
    {
        if (eventValue.HasValues && eventValue.Values.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedProperty,
                "An event values object must not be empty.",
                pointer + "/values");
        }

        if (eventValue.Values.ContainsKey("event"))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedProperty,
                "The event socket id is reserved and cannot be declared as an event value.",
                pointer + "/values/event");
        }

        foreach (KeyValuePair<string, GltfInteractivityEventValue> member in eventValue.Values)
        {
            string memberPointer = pointer + "/values/"
                + Fallback.EscapePointerSegment(member.Key);
            ValidateTypeReference(
                member.Value.HasType,
                member.Value.Type,
                typeCount,
                result,
                memberPointer + "/type");
            ValidateValue(
                member.Value.HasValue,
                member.Value.Value,
                result,
                memberPointer + "/value");
        }
    }

    internal void ValidateDeclaration(
        GltfDocument? document,
        GltfInteractivityDeclaration declaration,
        int typeCount,
        GltfExtensionResult result,
        string pointer)
    {
        if (!declaration.HasOperation)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The interactivity declaration operation is required.",
                pointer + "/op");
        }
        else
        {
            int separator = declaration.Operation.IndexOf('/');
            if (separator <= 0 || separator == declaration.Operation.Length - 1)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.PatternMismatch,
                    "Interactivity operations must use domain/operation names.",
                    pointer + "/op");
            }
        }

        if ((declaration.HasInputValueSockets || declaration.HasOutputValueSockets)
            && !declaration.HasExtension)
        {
            AddDependencyIssue(
                result,
                "Extension-defined declaration sockets require an extension owner.",
                pointer + "/extension");
        }

        if (declaration.HasInputValueSockets
            && declaration.InputValueSockets.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedProperty,
                "An input value socket declaration object must not be empty.",
                pointer + "/inputValueSockets");
        }

        if (declaration.HasOutputValueSockets
            && declaration.OutputValueSockets.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedProperty,
                "An output value socket declaration object must not be empty.",
                pointer + "/outputValueSockets");
        }

        if (declaration.HasExtension
            && document != null
            && declaration.Extension != null
            && !document.ExtensionsUsed.Contains(declaration.Extension))
        {
            AddDependencyIssue(
                result,
                "The declaration owner must be listed in extensionsUsed.",
                pointer + "/extension");
        }

        ValidateDeclarationSockets(
            declaration.InputValueSockets,
            typeCount,
            result,
            pointer + "/inputValueSockets");
        ValidateDeclarationSockets(
            declaration.OutputValueSockets,
            typeCount,
            result,
            pointer + "/outputValueSockets");
    }

    internal void ValidateDeclarationSockets(
        Dictionary<string, GltfInteractivityDeclarationValueSocket> sockets,
        int typeCount,
        GltfExtensionResult result,
        string pointer)
    {
        foreach (KeyValuePair<string, GltfInteractivityDeclarationValueSocket> member in sockets)
        {
            ValidateTypeReference(
                member.Value.HasType,
                member.Value.Type,
                typeCount,
                result,
                pointer + "/" + Fallback.EscapePointerSegment(member.Key) + "/type");
        }
    }

    internal void ValidateNode(
        GltfInteractivityGraph graph,
        GltfInteractivityNode node,
        GltfExtensionResult result,
        string pointer)
    {
        if (!node.HasDeclaration)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The interactivity node declaration is required.",
                pointer + "/declaration");
        }
        else if (node.Declaration < 0 || node.Declaration >= graph.Declarations.Count)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The interactivity node declaration does not resolve.",
                pointer + "/declaration");
        }

        ValidateSocketCollection(node.HasConfiguration, node.Configuration.Count, result, pointer + "/configuration");
        ValidateSocketCollection(node.HasValues, node.Values.Count, result, pointer + "/values");
        ValidateSocketCollection(node.HasFlows, node.Flows.Count, result, pointer + "/flows");

        foreach (KeyValuePair<string, GltfInteractivityConfiguration> member in node.Configuration)
        {
            ValidateValue(
                member.Value.HasValue,
                member.Value.Value,
                result,
                pointer + "/configuration/"
                    + Fallback.EscapePointerSegment(member.Key) + "/value");
        }

        foreach (KeyValuePair<string, GltfInteractivityValueSocket> member in node.Values)
        {
            ValidateValueSocket(
                member.Value,
                graph,
                result,
                pointer + "/values/" + Fallback.EscapePointerSegment(member.Key));
        }

        foreach (KeyValuePair<string, GltfInteractivityFlowSocket> member in node.Flows)
        {
            string flowPointer = pointer + "/flows/"
                + Fallback.EscapePointerSegment(member.Key);
            if (!member.Value.HasNode)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UndefinedProperty,
                    "A flow socket destination node is required.",
                    flowPointer + "/node");
            }
            else if (member.Value.Node < 0 || member.Value.Node >= graph.Nodes.Count)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    "The flow socket destination node does not resolve.",
                    flowPointer + "/node");
            }
        }
    }

    internal void ValidateSocketCollection(
        bool present,
        int count,
        GltfExtensionResult result,
        string pointer)
    {
        if (present && count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedProperty,
                "An explicitly present socket object must not be empty.",
                pointer);
        }
    }

    internal void ValidateValueSocket(
        GltfInteractivityValueSocket socket,
        GltfInteractivityGraph graph,
        GltfExtensionResult result,
        string pointer)
    {
        if (!socket.HasNode && !socket.HasType)
        {
            AddDependencyIssue(
                result,
                "An input value socket requires a node connection or inline type.",
                pointer);
        }

        if (socket.HasNode && socket.HasValue)
        {
            AddIssue(
                result,
                GltfIssueCodes.OneOfMismatch,
                "An input value socket cannot define both node and value.",
                pointer);
        }

        if (socket.HasSocket && !socket.HasNode)
        {
            AddDependencyIssue(
                result,
                "An output socket identifier requires a source node.",
                pointer + "/node");
        }

        if (socket.HasNode
            && (socket.Node < 0 || socket.Node >= graph.Nodes.Count))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The input value socket source node does not resolve.",
                pointer + "/node");
        }

        if (socket.HasType)
        {
            ValidateTypeReference(
                true,
                socket.Type,
                graph.Types.Count,
                result,
                pointer + "/type");
        }

        if (socket.HasValue && !socket.HasType)
        {
            AddDependencyIssue(
                result,
                "An inline value requires a type.",
                pointer + "/type");
        }

        ValidateValue(socket.HasValue, socket.Value, result, pointer + "/value");
    }

    internal void ValidateTypeReference(
        bool present,
        int type,
        int typeCount,
        GltfExtensionResult result,
        string pointer)
    {
        if (!present)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The interactivity type reference is required.",
                pointer);
        }
        else if (type < 0 || type >= typeCount)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The interactivity type reference does not resolve.",
                pointer);
        }
    }

    internal void ValidateValue(
        bool present,
        System.Text.Json.Nodes.JsonArray? value,
        GltfExtensionResult result,
        string pointer)
    {
        if (present && (value == null || value.Count == 0))
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "An explicitly present interactivity value must not be empty.",
                pointer);
        }
    }

    internal void ValidateGraphCycles(
        GltfInteractivityGraph graph,
        GltfExtensionResult result,
        string pointer)
    {
        int[] states = new int[graph.Nodes.Count];
        int index = 0;
        while (index < graph.Nodes.Count)
        {
            if (states[index] == 0
                && VisitGraphNode(graph, index, states, result, pointer))
            {
                return;
            }

            index++;
        }
    }

    internal bool VisitGraphNode(
        GltfInteractivityGraph graph,
        int nodeIndex,
        int[] states,
        GltfExtensionResult result,
        string pointer)
    {
        states[nodeIndex] = 1;
        GltfInteractivityNode node = graph.Nodes[nodeIndex];
        foreach (KeyValuePair<string, GltfInteractivityValueSocket> member in node.Values)
        {
            if (member.Value.HasNode
                && VisitGraphEdge(
                    graph,
                    member.Value.Node,
                    states,
                    result,
                    pointer,
                    pointer + "/" + nodeIndex + "/values/"
                        + Fallback.EscapePointerSegment(member.Key) + "/node"))
            {
                return true;
            }
        }

        foreach (KeyValuePair<string, GltfInteractivityFlowSocket> member in node.Flows)
        {
            if (member.Value.HasNode
                && VisitGraphEdge(
                    graph,
                    member.Value.Node,
                    states,
                    result,
                    pointer,
                    pointer + "/" + nodeIndex + "/flows/"
                        + Fallback.EscapePointerSegment(member.Key) + "/node"))
            {
                return true;
            }
        }

        states[nodeIndex] = 2;
        return false;
    }

    internal bool VisitGraphEdge(
        GltfInteractivityGraph graph,
        int target,
        int[] states,
        GltfExtensionResult result,
        string graphPointer,
        string edgePointer)
    {
        if (target < 0 || target >= graph.Nodes.Count)
        {
            return false;
        }

        if (states[target] == 1)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "The interactivity graph contains a directed cycle.",
                edgePointer);
            return true;
        }

        return states[target] == 0
            && VisitGraphNode(graph, target, states, result, graphPointer);
    }

    internal void AddDependencyIssue(
        GltfExtensionResult result,
        string message,
        string pointer)
    {
        AddIssue(result, GltfIssueCodes.UnsatisfiedDependency, message, pointer);
    }

    internal void AddIssue(
        GltfExtensionResult result,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.Status = GltfOperationStatus.Invalid;
    }
}
