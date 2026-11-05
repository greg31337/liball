using System.Collections.Generic;

namespace gltf;

internal class CompressionExtensionValidator
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal CompressionExtensionJsonReader Reader = new CompressionExtensionJsonReader();

    internal GltfExtensionResult ValidateCompressionExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result;
        GltfTypedExtensionData? typed;
        if (context.TypedData == null)
        {
            result = Reader.ReadCompressionExtension(context, registry);
            typed = result.TypedData;
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, registry);
            typed = context.TypedData;
            result.TypedData = typed;
        }

        if (typed == null)
        {
            return result;
        }

        ValidateLocation(context, result);
        if (context.Name == GltfExtensionNames.KhrDracoMeshCompression
            && typed.DracoMeshCompression != null)
        {
            ValidateDraco(context, result, typed.DracoMeshCompression);
            if (context.TypedData != null)
            {
                Reader.AddBufferViewReference(
                    context,
                    result,
                    typed.DracoMeshCompression.BufferView,
                    "bufferView");
            }
        }
        else if (context.ObjectLocation == "buffer")
        {
            GltfMeshoptCompressionBufferExtension? buffer = GetMeshoptBuffer(
                context.Name,
                typed);
            if (buffer != null)
            {
                ValidateFallbackBuffer(context, result, buffer);
            }
        }
        else
        {
            GltfMeshoptCompressionBufferViewExtension? view = GetMeshoptBufferView(
                context.Name,
                typed);
            if (view != null)
            {
                ValidateMeshoptView(context, result, view);
                if (context.TypedData != null)
                {
                    Reader.AddBufferReference(context, result, view.Buffer);
                }
            }
        }

        return result;
    }

    internal void ValidateLocation(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        bool valid = context.Name == GltfExtensionNames.KhrDracoMeshCompression
            && context.ObjectLocation == "mesh.primitive";
        if (context.Name == GltfExtensionNames.KhrMeshoptCompression
            || context.Name == GltfExtensionNames.ExtMeshoptCompression)
        {
            valid = context.ObjectLocation == "buffer"
                || context.ObjectLocation == "bufferView";
        }

        if (!valid)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                $"Extension '{context.Name}' is not valid on {context.ObjectLocation}.",
                Fallback.CreateExtensionPointer(context));
        }
    }

    internal void ValidateDraco(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfDracoMeshCompressionExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (context.Document == null
            || context.Document.BufferViews == null
            || value.BufferView < 0
            || value.BufferView >= context.Document.BufferViews.Count
            || context.Document.BufferViews[value.BufferView] == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The Draco bufferView reference does not resolve.",
                pointer + "/bufferView");
        }

        if (value.Attributes == null || value.Attributes.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "Draco attributes must not be empty.",
                pointer + "/attributes");
            return;
        }

        List<int> identifiers = new List<int>();
        foreach (KeyValuePair<string, int> attribute in value.Attributes)
        {
            string attributeName = attribute.Key ?? "";
            string attributePointer = pointer + "/attributes/"
                + Fallback.EscapePointerSegment(attributeName);
            if (attributeName.Length == 0 || attribute.Value < 0)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    "Draco attribute names must be non-empty and identifiers must be non-negative.",
                    attributePointer);
            }
            else if (identifiers.Contains(attribute.Value))
            {
                AddIssue(
                    result,
                    GltfIssueCodes.DuplicateElements,
                    "Draco attribute identifiers must be unique.",
                    attributePointer);
            }
            else
            {
                identifiers.Add(attribute.Value);
            }
        }

        GltfMeshPrimitive? primitive = ResolveDracoPrimitive(context);
        if (primitive != null)
        {
            ValidateDracoPrimitive(context, result, value, primitive);
        }
    }

    internal void ValidateDracoPrimitive(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfDracoMeshCompressionExtension value,
        GltfMeshPrimitive primitive)
    {
        string pointer = CreateDracoPointer(context);
        if (primitive.Mode != GltfPrimitiveMode.Triangles
            && primitive.Mode != GltfPrimitiveMode.TriangleStrip)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "Draco compression supports only TRIANGLES and TRIANGLE_STRIP primitives.",
                pointer + "/mode");
        }

        bool required = context.Document != null
            && context.Document.ExtensionsRequired != null
            && context.Document.ExtensionsRequired.Contains(
                GltfExtensionNames.KhrDracoMeshCompression);
        int attributeIndex = 0;
        while (primitive.Attributes != null
            && attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute != null && attribute.Semantic != null)
            {
                bool compressed = value.Attributes.ContainsKey(attribute.Semantic);
                if (required && !compressed)
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.UnsatisfiedDependency,
                        "A required Draco primitive must include every primitive attribute in its compressed mapping.",
                        pointer + "/attributes/" + Fallback.EscapePointerSegment(attribute.Semantic));
                }

                if (compressed)
                {
                    ValidateDracoAccessorFallback(
                        context,
                        result,
                        attribute.Accessor,
                        required,
                        pointer + "/attributes/" + Fallback.EscapePointerSegment(attribute.Semantic));
                }
            }

            attributeIndex++;
        }

        foreach (KeyValuePair<string, int> compressedAttribute in value.Attributes)
        {
            if (!PrimitiveHasAttribute(primitive, compressedAttribute.Key))
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    "A Draco attribute mapping must reference an attribute on the parent primitive.",
                    pointer + "/extensions/KHR_draco_mesh_compression/attributes/"
                        + Fallback.EscapePointerSegment(compressedAttribute.Key));
            }
        }

        if (primitive.HasIndices)
        {
            ValidateDracoAccessorFallback(
                context,
                result,
                primitive.Indices,
                required,
                pointer + "/indices");
        }
    }

    internal void ValidateDracoAccessorFallback(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int accessorIndex,
        bool required,
        string pointer)
    {
        if (context.Document == null
            || context.Document.Accessors == null
            || accessorIndex < 0
            || accessorIndex >= context.Document.Accessors.Count
            || context.Document.Accessors[accessorIndex] == null)
        {
            return;
        }

        GltfAccessor accessor = context.Document.Accessors[accessorIndex];
        bool hasStorage = accessor.HasBufferView || accessor.Sparse != null;
        if (required && hasStorage)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "Accessors compressed by a required Draco extension must not contain fallback storage.",
                pointer);
        }
        else if (!required && !hasStorage)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "An optional Draco extension requires uncompressed fallback accessor storage.",
                pointer);
        }
    }

    internal GltfMeshPrimitive? ResolveDracoPrimitive(GltfExtensionContext context)
    {
        if (context.Document == null
            || context.Document.Meshes == null
            || !context.HasObjectIndex
            || !context.HasSubObjectIndex
            || context.ObjectIndex < 0
            || context.ObjectIndex >= context.Document.Meshes.Count)
        {
            return null;
        }

        GltfMesh mesh = context.Document.Meshes[context.ObjectIndex];
        if (mesh == null
            || mesh.Primitives == null
            || context.SubObjectIndex < 0
            || context.SubObjectIndex >= mesh.Primitives.Count)
        {
            return null;
        }

        return mesh.Primitives[context.SubObjectIndex];
    }

    internal bool PrimitiveHasAttribute(
        GltfMeshPrimitive primitive,
        string semantic)
    {
        int index = 0;
        while (primitive.Attributes != null && index < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[index];
            if (attribute != null && attribute.Semantic == semantic)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal string CreateDracoPointer(GltfExtensionContext context)
    {
        if (context.HasObjectIndex && context.HasSubObjectIndex)
        {
            return $"/meshes/{context.ObjectIndex}/primitives/{context.SubObjectIndex}";
        }

        return "/mesh.primitive";
    }

    internal void ValidateMeshoptView(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshoptCompressionBufferViewExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        GltfBufferView? parent = ResolveParentBufferView(context);
        if (value.Buffer < 0
            || context.Document == null
            || context.Document.Buffers == null
            || value.Buffer >= context.Document.Buffers.Count
            || context.Document.Buffers[value.Buffer] == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The Meshopt compressed buffer reference does not resolve.",
                pointer + "/buffer");
        }
        else
        {
            GltfBuffer compressed = context.Document.Buffers[value.Buffer];
            long end = (long)value.ByteOffset + value.ByteLength;
            if (value.ByteOffset < 0
                || value.ByteLength < 1
                || end < value.ByteOffset
                || end > compressed.ByteLength)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.BufferViewTooLong,
                    "The Meshopt compressed byte range exceeds its buffer.",
                    pointer + "/byteLength");
            }
        }

        if (parent == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The parent Meshopt bufferView does not resolve.",
                pointer);
        }
        else
        {
            long decodedLength = (long)value.ByteStride * value.Count;
            if (value.ByteStride < 1
                || value.Count < 1
                || decodedLength != parent.ByteLength)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    "Meshopt byteStride multiplied by count must equal the parent bufferView byteLength.",
                    pointer + "/count");
            }

            if (parent.HasByteStride && parent.ByteStride != value.ByteStride)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.BufferViewInvalidByteStride,
                    "The parent and Meshopt byteStride values must match.",
                    pointer + "/byteStride");
            }

            ValidateViewConflict(context, result, parent);
            ValidateRequiredFallback(context, result, parent);
        }

        ValidateModeAndFilter(context, result, value);
    }

    internal void ValidateModeAndFilter(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshoptCompressionBufferViewExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        bool attributes = value.Mode == "ATTRIBUTES";
        bool triangles = value.Mode == "TRIANGLES";
        bool indices = value.Mode == "INDICES";
        if (!attributes && !triangles && !indices)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "Meshopt mode must be ATTRIBUTES, TRIANGLES, or INDICES.",
                pointer + "/mode");
        }

        bool filterValid = value.Filter == "NONE"
            || value.Filter == "OCTAHEDRAL"
            || value.Filter == "QUATERNION"
            || value.Filter == "EXPONENTIAL";
        if (context.Name == GltfExtensionNames.KhrMeshoptCompression
            && value.Filter == "COLOR")
        {
            filterValid = true;
        }

        if (!filterValid)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "The Meshopt filter is not supported by this extension version.",
                pointer + "/filter");
        }

        if (attributes
            && (value.ByteStride < 4
                || value.ByteStride > 256
                || value.ByteStride % 4 != 0))
        {
            AddIssue(
                result,
                GltfIssueCodes.BufferViewInvalidByteStride,
                "Meshopt ATTRIBUTES byteStride must be a multiple of four from 4 through 256.",
                pointer + "/byteStride");
        }

        if ((triangles || indices)
            && value.ByteStride != 2
            && value.ByteStride != 4)
        {
            AddIssue(
                result,
                GltfIssueCodes.BufferViewInvalidByteStride,
                "Meshopt TRIANGLES and INDICES byteStride must be 2 or 4.",
                pointer + "/byteStride");
        }

        if (triangles && value.Count % 3 != 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueMultipleOf,
                "Meshopt TRIANGLES count must be divisible by three.",
                pointer + "/count");
        }

        if ((triangles || indices) && value.Filter != "NONE")
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "Meshopt index modes do not allow a post-decode filter.",
                pointer + "/filter");
        }

        if (value.Filter == "OCTAHEDRAL"
            && value.ByteStride != 4
            && value.ByteStride != 8)
        {
            AddFilterStrideIssue(result, pointer, "OCTAHEDRAL", "4 or 8");
        }
        else if (value.Filter == "QUATERNION" && value.ByteStride != 8)
        {
            AddFilterStrideIssue(result, pointer, "QUATERNION", "8");
        }
        else if (value.Filter == "EXPONENTIAL" && value.ByteStride % 4 != 0)
        {
            AddFilterStrideIssue(result, pointer, "EXPONENTIAL", "a multiple of 4");
        }
        else if (value.Filter == "COLOR"
            && value.ByteStride != 4
            && value.ByteStride != 8)
        {
            AddFilterStrideIssue(result, pointer, "COLOR", "4 or 8");
        }
    }

    internal void ValidateFallbackBuffer(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshoptCompressionBufferExtension value)
    {
        if (!value.Fallback || context.Document == null)
        {
            return;
        }

        int bufferIndex = context.ObjectIndex;
        int viewIndex = 0;
        while (viewIndex < context.Document.BufferViews.Count)
        {
            GltfBufferView view = context.Document.BufferViews[viewIndex];
            if (view != null && view.Buffer == bufferIndex)
            {
                if (!HasExtension(view.PropertyJson, context.Name))
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.UnsatisfiedDependency,
                        "Every bufferView referencing a fallback buffer must use the same Meshopt extension.",
                        $"/bufferViews/{viewIndex}/buffer");
                }
            }

            GltfMeshoptCompressionBufferViewExtension? compressed = GetViewMetadata(
                view,
                context.Name);
            if (compressed != null && compressed.Buffer == bufferIndex)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnsatisfiedDependency,
                    "A fallback buffer cannot also contain Meshopt compressed source data.",
                    $"/bufferViews/{viewIndex}/extensions/{context.Name}/buffer");
            }

            viewIndex++;
        }

        GltfBuffer? buffer = ResolveParentBuffer(context);
        if (buffer != null)
        {
            ValidateBufferConflict(context, result, buffer);
        }
    }

    internal void ValidateViewConflict(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfBufferView view)
    {
        string other = GetOtherMeshoptName(context.Name);
        if (HasExtension(view.PropertyJson, other))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "KHR_meshopt_compression and EXT_meshopt_compression cannot be used on the same bufferView.",
                Fallback.CreateExtensionPointer(context));
        }
    }

    internal void ValidateBufferConflict(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfBuffer buffer)
    {
        string other = GetOtherMeshoptName(context.Name);
        if (HasExtension(buffer.PropertyJson, other))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "KHR_meshopt_compression and EXT_meshopt_compression cannot be used on the same buffer.",
                Fallback.CreateExtensionPointer(context));
        }
    }

    internal void ValidateRequiredFallback(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfBufferView parent)
    {
        if (context.Document == null
            || parent.Buffer < 0
            || parent.Buffer >= context.Document.Buffers.Count)
        {
            return;
        }

        GltfBuffer fallback = context.Document.Buffers[parent.Buffer];
        if (fallback == null)
        {
            return;
        }

        ValidateBufferConflict(context, result, fallback);
        bool isGlbBuffer = context.Document.Container != null
            && context.Document.Container.Format == GltfContainerFormat.Glb
            && parent.Buffer == 0;
        if (fallback.Uri == null
            && !isGlbBuffer
            && (context.Document.ExtensionsRequired == null
                || !context.Document.ExtensionsRequired.Contains(context.Name)))
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "Meshopt must be required when the uncompressed fallback buffer has no resource.",
                "/extensionsRequired");
        }
    }

    internal GltfBufferView? ResolveParentBufferView(GltfExtensionContext context)
    {
        if (context.Document == null
            || context.Document.BufferViews == null
            || context.ObjectIndex < 0
            || context.ObjectIndex >= context.Document.BufferViews.Count)
        {
            return null;
        }

        return context.Document.BufferViews[context.ObjectIndex];
    }

    internal GltfBuffer? ResolveParentBuffer(GltfExtensionContext context)
    {
        if (context.Document == null
            || context.Document.Buffers == null
            || context.ObjectIndex < 0
            || context.ObjectIndex >= context.Document.Buffers.Count)
        {
            return null;
        }

        return context.Document.Buffers[context.ObjectIndex];
    }

    internal GltfMeshoptCompressionBufferExtension? GetMeshoptBuffer(
        string name,
        GltfTypedExtensionData typed)
    {
        if (name == GltfExtensionNames.KhrMeshoptCompression)
        {
            return typed.KhrMeshoptCompressionBuffer;
        }

        return typed.ExtMeshoptCompressionBuffer;
    }

    internal GltfMeshoptCompressionBufferViewExtension? GetMeshoptBufferView(
        string name,
        GltfTypedExtensionData typed)
    {
        if (name == GltfExtensionNames.KhrMeshoptCompression)
        {
            return typed.KhrMeshoptCompressionBufferView;
        }

        return typed.ExtMeshoptCompressionBufferView;
    }

    internal GltfMeshoptCompressionBufferViewExtension? GetViewMetadata(
        GltfBufferView? view,
        string name)
    {
        if (view == null
            || view.PropertyJson == null
            || view.PropertyJson.TypedExtensions == null
            || !view.PropertyJson.TypedExtensions.ContainsKey(name))
        {
            return null;
        }

        return GetMeshoptBufferView(name, view.PropertyJson.TypedExtensions[name]);
    }

    internal bool HasExtension(GltfPropertyJson propertyJson, string name)
    {
        return propertyJson != null
            && ((propertyJson.Extensions != null
                    && propertyJson.Extensions.ContainsKey(name))
                || (propertyJson.TypedExtensions != null
                    && propertyJson.TypedExtensions.ContainsKey(name)));
    }

    internal string GetOtherMeshoptName(string name)
    {
        if (name == GltfExtensionNames.KhrMeshoptCompression)
        {
            return GltfExtensionNames.ExtMeshoptCompression;
        }

        return GltfExtensionNames.KhrMeshoptCompression;
    }

    internal void AddFilterStrideIssue(
        GltfExtensionResult result,
        string pointer,
        string filter,
        string expected)
    {
        AddIssue(
            result,
            GltfIssueCodes.BufferViewInvalidByteStride,
            $"Meshopt {filter} filter byteStride must be {expected}.",
            pointer + "/byteStride");
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
