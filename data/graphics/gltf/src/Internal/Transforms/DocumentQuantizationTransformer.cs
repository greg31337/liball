using System;
using System.Collections.Generic;

namespace gltf;

internal class QuantizationEncodingChoice
{
    internal bool Available;
    internal GltfComponentType ComponentType = GltfComponentType.Float;
    internal bool Normalized;
    internal double Error;
}

internal class DocumentQuantizationTransformer
{
    internal QuantizationTransformSettingsReader Settings =
        new QuantizationTransformSettingsReader();
    internal AttributeSemanticParser Semantics = new AttributeSemanticParser();
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal AccessorNormalization Normalization = new AccessorNormalization();
    internal AccessorLayout Layout = new AccessorLayout();
    internal MeshQuantization Quantization = new MeshQuantization();
    internal DocumentCloner Cloner = new DocumentCloner();
    internal DocumentCompactor Mapping = new DocumentCompactor();
    internal DocumentVertexTransformer Vertices = new DocumentVertexTransformer();
    internal BufferViewTargetCleaner Targets = new BufferViewTargetCleaner();
    internal List<AccessorTransformCacheEntry> Cache =
        new List<AccessorTransformCacheEntry>();
    internal List<int> SourceBufferViews = new List<int>();

    internal TransformStepExecution TransformQuantization(
        GltfDocument document,
        GltfTransformStep step,
        bool quantize)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        QuantizationTransformSettings settings =
            Settings.ReadQuantizationSettings(step);
        Cache.Clear();
        SourceBufferViews.Clear();
        bool changed = TransformMeshes(
            document,
            quantize,
            settings,
            execution.Result);
        bool usedBefore = document.ExtensionsUsed.Contains(
            GltfExtensionNames.KhrMeshQuantization);
        bool requiredBefore = document.ExtensionsRequired.Contains(
            GltfExtensionNames.KhrMeshQuantization);
        Quantization.UpdateDeclarations(document);
        bool usedAfter = document.ExtensionsUsed.Contains(
            GltfExtensionNames.KhrMeshQuantization);
        bool requiredAfter = document.ExtensionsRequired.Contains(
            GltfExtensionNames.KhrMeshQuantization);
        if (usedBefore != usedAfter)
        {
            execution.Result.ChangedObjects.Add("extensionsUsed");
            changed = true;
        }

        if (requiredBefore != requiredAfter)
        {
            execution.Result.ChangedObjects.Add("extensionsRequired");
            changed = true;
        }

        if (changed)
        {
            Targets.ClearUnusedTargets(
                document,
                execution.Result,
                SourceBufferViews);
            ClearUnusedSourceStrides(
                document,
                execution.Result,
                SourceBufferViews);
            if (document.Container != null)
            {
                document.Container = null;
                execution.Result.ChangedObjects.Add("container");
            }
        }

        return execution;
    }

    internal void ClearUnusedSourceStrides(
        GltfDocument document,
        GltfTransformStepResult result,
        List<int> candidates)
    {
        bool[] vertexViews = new bool[document.BufferViews.Count];
        bool[] indexViews = new bool[document.BufferViews.Count];
        Targets.CollectPrimitiveViews(document, vertexViews, indexViews);
        Targets.CollectInstanceViews(document, vertexViews);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            int viewIndex = candidates[candidateIndex];
            GltfBufferView view = document.BufferViews[viewIndex];
            if (view.HasByteStride && !vertexViews[viewIndex])
            {
                RepackAccessorsFromView(document, viewIndex, result);
                view.HasByteStride = false;
                view.ByteStride = 0;
                string identifier = $"bufferViews/{viewIndex}";
                if (!result.ChangedObjects.Contains(identifier))
                {
                    result.ChangedObjects.Add(identifier);
                }
            }

            candidateIndex++;
        }
    }

    internal void RepackAccessorsFromView(
        GltfDocument document,
        int viewIndex,
        GltfTransformStepResult result)
    {
        List<int> accessorIndices = new List<int>();
        List<byte[]> accessorBytes = new List<byte[]>();
        int accessorIndex = 0;
        int originalCount = document.Accessors.Count;
        while (accessorIndex < originalCount)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor.HasBufferView && accessor.BufferView == viewIndex)
            {
                GltfAccessorReadRequest request = new GltfAccessorReadRequest();
                request.Document = document;
                request.Accessor = accessorIndex;
                GltfAccessorData data = Accessors.ReadRawData(request);
                if (data.Status == GltfOperationStatus.Success
                    && data.RawBytes.Length > 0)
                {
                    accessorIndices.Add(accessorIndex);
                    accessorBytes.Add(data.RawBytes);
                }
            }

            accessorIndex++;
        }

        int selectedIndex = 0;
        while (selectedIndex < accessorIndices.Count)
        {
            accessorIndex = accessorIndices[selectedIndex];
            GltfAccessor accessor = document.Accessors[accessorIndex];
            AccessorEncodingResult encoding = Encoder.EncodeRawAccessor(
                accessorBytes[selectedIndex],
                accessor.ComponentType,
                accessor.Type,
                accessor.Normalized,
                false);
            AccessorCreationResult created = Encoder.CreateAccessorStorage(
                document,
                encoding,
                false);
            accessor.HasBufferView = true;
            accessor.BufferView = created.BufferView;
            accessor.HasByteOffset = false;
            accessor.ByteOffset = 0;
            string identifier = $"accessors/{accessorIndex}";
            if (!result.ChangedObjects.Contains(identifier))
            {
                result.ChangedObjects.Add(identifier);
            }

            selectedIndex++;
        }
    }

    internal bool TransformMeshes(
        GltfDocument document,
        bool quantize,
        QuantizationTransformSettings settings,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                bool primitiveChanged = false;
                if (Vertices.IsCompatiblePrimitive(primitive))
                {
                    primitiveChanged = TransformAttributeCollection(
                        document,
                        primitive.Attributes,
                        false,
                        quantize,
                        settings);
                    int targetIndex = 0;
                    while (targetIndex < primitive.Targets.Count)
                    {
                        primitiveChanged = TransformAttributeCollection(
                                document,
                                primitive.Targets[targetIndex].Attributes,
                                true,
                                quantize,
                                settings)
                            || primitiveChanged;
                        targetIndex++;
                    }
                }

                if (primitiveChanged)
                {
                    result.ChangedObjects.Add(
                        $"meshes/{meshIndex}/primitives/{primitiveIndex}");
                    changed = true;
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        return changed;
    }

    internal bool TransformAttributeCollection(
        GltfDocument document,
        List<GltfAttribute> attributes,
        bool morphTarget,
        bool quantize,
        QuantizationTransformSettings settings)
    {
        bool changed = false;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            GltfAttribute attribute = attributes[attributeIndex];
            AttributeSemantic semantic = Semantics.ParseAttributeSemantic(
                attribute.Semantic);
            if (semantic.Valid && IsTransformableSemantic(semantic.Kind, morphTarget))
            {
                int transformed = TransformAttributeAccessor(
                    document,
                    attribute.Accessor,
                    semantic.Kind,
                    morphTarget,
                    quantize,
                    settings);
                if (transformed != attribute.Accessor)
                {
                    attribute.Accessor = transformed;
                    changed = true;
                }
            }

            attributeIndex++;
        }

        return changed;
    }

    internal bool IsTransformableSemantic(
        AttributeSemanticKind kind,
        bool morphTarget)
    {
        if (morphTarget)
        {
            return kind == AttributeSemanticKind.Position
                || kind == AttributeSemanticKind.Normal
                || kind == AttributeSemanticKind.Tangent
                || kind == AttributeSemanticKind.TextureCoordinate;
        }

        return kind == AttributeSemanticKind.Position
            || kind == AttributeSemanticKind.Normal
            || kind == AttributeSemanticKind.Tangent
            || kind == AttributeSemanticKind.TextureCoordinate
            || kind == AttributeSemanticKind.Color
            || kind == AttributeSemanticKind.Weights;
    }

    internal int TransformAttributeAccessor(
        GltfDocument document,
        int accessorIndex,
        AttributeSemanticKind kind,
        bool morphTarget,
        bool quantize,
        QuantizationTransformSettings settings)
    {
        int cacheKind = (int)kind + (morphTarget ? 100 : 0) + (quantize ? 1000 : 0);
        int cached = FindCachedAccessor(accessorIndex, cacheKind);
        if (cached != GltfConstants.AbsentIndex)
        {
            return cached;
        }

        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return accessorIndex;
        }

        GltfAccessor source = document.Accessors[accessorIndex];
        if (!Vertices.IsAccessorStorageCompatible(document, source)
            || quantize && source.ComponentType != GltfComponentType.Float
            || !quantize && source.ComponentType == GltfComponentType.Float)
        {
            return accessorIndex;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        request.ApplyNormalization = true;
        GltfAccessorData decoded = Accessors.ReadFloatingData(request);
        if (decoded.Status != GltfOperationStatus.Success
            || decoded.FloatingValues.Length == 0
            || !ValuesFinite(decoded.FloatingValues))
        {
            return accessorIndex;
        }

        int created;
        if (quantize)
        {
            QuantizationEncodingChoice choice = SelectEncoding(
                decoded.FloatingValues,
                kind,
                morphTarget,
                settings.Tolerance);
            if (!choice.Available)
            {
                return accessorIndex;
            }

            created = WriteQuantizedAccessor(
                document,
                source,
                decoded.FloatingValues,
                choice);
        }
        else
        {
            created = WriteFloatingAccessor(
                document,
                source,
                decoded.FloatingValues);
        }

        RecordSourceBufferView(document, source);
        AddCachedAccessor(accessorIndex, cacheKind, created);
        return created;
    }

    internal bool ValuesFinite(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!double.IsFinite(values[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal QuantizationEncodingChoice SelectEncoding(
        double[] values,
        AttributeSemanticKind kind,
        bool morphTarget,
        double tolerance)
    {
        QuantizationEncodingChoice result = new QuantizationEncodingChoice();
        if (kind == AttributeSemanticKind.Normal
            || kind == AttributeSemanticKind.Tangent)
        {
            TrySignedNormalized(values, tolerance, result);
            return result;
        }

        if (kind == AttributeSemanticKind.Weights)
        {
            TryWeightNormalized(values, tolerance, result);
            return result;
        }

        if (kind == AttributeSemanticKind.Color)
        {
            TryUnsignedNormalized(values, tolerance, result);
            return result;
        }

        if (kind == AttributeSemanticKind.TextureCoordinate && !morphTarget)
        {
            TryUnsignedNormalized(values, tolerance, result);
            if (!result.Available)
            {
                TrySignedNormalized(values, tolerance, result);
            }
        }
        else if (kind == AttributeSemanticKind.Position && !morphTarget)
        {
            TryUnsignedNormalized(values, tolerance, result);
            if (!result.Available)
            {
                TrySignedNormalized(values, tolerance, result);
            }
        }
        else if (kind == AttributeSemanticKind.Position)
        {
            TrySignedNormalized(values, tolerance, result);
        }

        if (!result.Available)
        {
            TryIntegralEncoding(values, tolerance, morphTarget, result);
        }

        return result;
    }

    internal void TrySignedNormalized(
        double[] values,
        double tolerance,
        QuantizationEncodingChoice result)
    {
        if (!ValuesWithin(values, -1.0, 1.0))
        {
            return;
        }

        double byteError = MeasureSignedNormalizedError(
            values,
            GltfComponentType.SignedByte);
        if (byteError <= tolerance)
        {
            SetChoice(
                result,
                GltfComponentType.SignedByte,
                true,
                byteError);
            return;
        }

        double shortError = MeasureSignedNormalizedError(
            values,
            GltfComponentType.SignedShort);
        if (shortError <= tolerance)
        {
            SetChoice(
                result,
                GltfComponentType.SignedShort,
                true,
                shortError);
        }
    }

    internal void TryWeightNormalized(
        double[] values,
        double tolerance,
        QuantizationEncodingChoice result)
    {
        if (!ValuesWithin(values, 0.0, 1.0))
        {
            return;
        }

        double byteError = MeasureUnsignedNormalizedError(
            values,
            GltfComponentType.UnsignedByte);
        if (byteError <= tolerance
            && MeasureWeightSumError(
                values,
                GltfComponentType.UnsignedByte) <= 0.00005)
        {
            SetChoice(
                result,
                GltfComponentType.UnsignedByte,
                true,
                byteError);
            return;
        }

        double shortError = MeasureUnsignedNormalizedError(
            values,
            GltfComponentType.UnsignedShort);
        if (shortError <= tolerance
            && MeasureWeightSumError(
                values,
                GltfComponentType.UnsignedShort) <= 0.00005)
        {
            SetChoice(
                result,
                GltfComponentType.UnsignedShort,
                true,
                shortError);
        }
    }

    internal double MeasureWeightSumError(
        double[] values,
        GltfComponentType componentType)
    {
        double result = 0.0;
        int offset = 0;
        while (offset + 3 < values.Length)
        {
            double sourceSum = 0.0;
            double decodedSum = 0.0;
            int componentIndex = 0;
            while (componentIndex < 4)
            {
                double value = values[offset + componentIndex];
                uint encoded = Normalization.EncodeUnsignedValue(
                    value,
                    componentType);
                sourceSum += value;
                decodedSum += Normalization.DecodeUnsignedValue(
                    encoded,
                    componentType);
                componentIndex++;
            }

            double error = Math.Abs(decodedSum - sourceSum);
            if (error > result)
            {
                result = error;
            }

            offset += 4;
        }

        return result;
    }

    internal void TryUnsignedNormalized(
        double[] values,
        double tolerance,
        QuantizationEncodingChoice result)
    {
        if (!ValuesWithin(values, 0.0, 1.0))
        {
            return;
        }

        double byteError = MeasureUnsignedNormalizedError(
            values,
            GltfComponentType.UnsignedByte);
        if (byteError <= tolerance)
        {
            SetChoice(
                result,
                GltfComponentType.UnsignedByte,
                true,
                byteError);
            return;
        }

        double shortError = MeasureUnsignedNormalizedError(
            values,
            GltfComponentType.UnsignedShort);
        if (shortError <= tolerance)
        {
            SetChoice(
                result,
                GltfComponentType.UnsignedShort,
                true,
                shortError);
        }
    }

    internal void TryIntegralEncoding(
        double[] values,
        double tolerance,
        bool requireSigned,
        QuantizationEncodingChoice result)
    {
        bool nonNegative = ValuesWithin(values, 0.0, 65535.0);
        if (!requireSigned && nonNegative)
        {
            double byteError = MeasureIntegralError(values, 0.0, 255.0);
            if (byteError <= tolerance)
            {
                SetChoice(
                    result,
                    GltfComponentType.UnsignedByte,
                    false,
                    byteError);
                return;
            }

            double shortError = MeasureIntegralError(values, 0.0, 65535.0);
            if (shortError <= tolerance)
            {
                SetChoice(
                    result,
                    GltfComponentType.UnsignedShort,
                    false,
                    shortError);
                return;
            }
        }

        double signedByteError = MeasureIntegralError(values, -128.0, 127.0);
        if (signedByteError <= tolerance)
        {
            SetChoice(
                result,
                GltfComponentType.SignedByte,
                false,
                signedByteError);
            return;
        }

        double signedShortError = MeasureIntegralError(values, -32768.0, 32767.0);
        if (signedShortError <= tolerance)
        {
            SetChoice(
                result,
                GltfComponentType.SignedShort,
                false,
                signedShortError);
        }
    }

    internal bool ValuesWithin(double[] values, double minimum, double maximum)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < minimum || values[index] > maximum)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal double MeasureSignedNormalizedError(
        double[] values,
        GltfComponentType componentType)
    {
        double result = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            int encoded = Normalization.EncodeSignedValue(
                values[index],
                componentType);
            double decoded = Normalization.DecodeSignedValue(encoded, componentType);
            double error = Math.Abs(decoded - values[index]);
            if (error > result)
            {
                result = error;
            }

            index++;
        }

        return result;
    }

    internal double MeasureUnsignedNormalizedError(
        double[] values,
        GltfComponentType componentType)
    {
        double result = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            uint encoded = Normalization.EncodeUnsignedValue(
                values[index],
                componentType);
            double decoded = Normalization.DecodeUnsignedValue(encoded, componentType);
            double error = Math.Abs(decoded - values[index]);
            if (error > result)
            {
                result = error;
            }

            index++;
        }

        return result;
    }

    internal double MeasureIntegralError(
        double[] values,
        double minimum,
        double maximum)
    {
        if (!ValuesWithin(values, minimum, maximum))
        {
            return double.PositiveInfinity;
        }

        double result = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            double rounded = Math.Round(
                values[index],
                MidpointRounding.AwayFromZero);
            double error = Math.Abs(rounded - values[index]);
            if (error > result)
            {
                result = error;
            }

            index++;
        }

        return result;
    }

    internal void SetChoice(
        QuantizationEncodingChoice result,
        GltfComponentType componentType,
        bool normalized,
        double error)
    {
        result.Available = true;
        result.ComponentType = componentType;
        result.Normalized = normalized;
        result.Error = error;
    }

    internal int WriteQuantizedAccessor(
        GltfDocument document,
        GltfAccessor source,
        double[] values,
        QuantizationEncodingChoice choice)
    {
        AccessorEncodingResult encoding;
        if (choice.Normalized)
        {
            encoding = Encoder.EncodeFloatingAccessor(
                values,
                choice.ComponentType,
                source.Type,
                true);
        }
        else if (choice.ComponentType == GltfComponentType.SignedByte
            || choice.ComponentType == GltfComponentType.SignedShort)
        {
            encoding = Encoder.EncodeSignedAccessor(
                RoundSignedValues(values),
                choice.ComponentType,
                source.Type,
                false);
        }
        else
        {
            encoding = Encoder.EncodeUnsignedAccessor(
                RoundUnsignedValues(values),
                choice.ComponentType,
                source.Type,
                false);
        }

        return CreateVertexAccessor(document, source, encoding);
    }

    internal int[] RoundSignedValues(double[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = (int)Math.Round(
                values[index],
                MidpointRounding.AwayFromZero);
            index++;
        }

        return result;
    }

    internal uint[] RoundUnsignedValues(double[] values)
    {
        uint[] result = new uint[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = (uint)Math.Round(
                values[index],
                MidpointRounding.AwayFromZero);
            index++;
        }

        return result;
    }

    internal int WriteFloatingAccessor(
        GltfDocument document,
        GltfAccessor source,
        double[] values)
    {
        AccessorEncodingResult encoding = Encoder.EncodeFloatingAccessor(
            values,
            GltfComponentType.Float,
            source.Type,
            false);
        return CreateVertexAccessor(document, source, encoding);
    }

    internal int CreateVertexAccessor(
        GltfDocument document,
        GltfAccessor source,
        AccessorEncodingResult encoding)
    {
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        AlignVertexStorage(document, created, encoding);
        GltfBufferView view = document.BufferViews[created.BufferView];
        view.HasTarget = true;
        view.Target = GltfBufferViewTarget.ArrayBuffer;
        GltfAccessor target = document.Accessors[created.Accessor];
        target.Name = source.Name;
        Cloner.Clones.Clear();
        Cloner.CloneJson = true;
        Cloner.CloneResourceBytes = true;
        target.PropertyJson = (GltfPropertyJson)(Cloner.CloneValue(source.PropertyJson)
            ?? new GltfPropertyJson());
        return created.Accessor;
    }

    internal void AlignVertexStorage(
        GltfDocument document,
        AccessorCreationResult created,
        AccessorEncodingResult encoding)
    {
        AccessorElementLayout element = Layout.CalculateElementLayout(
            encoding.ComponentType,
            encoding.Type);
        int stride = FourByteAlignment.Align(element.ElementByteLength);
        if (encoding.Count <= 1 || stride == element.ElementByteLength)
        {
            return;
        }

        byte[] padded = new byte[stride * encoding.Count];
        int elementIndex = 0;
        while (elementIndex < encoding.Count)
        {
            int sourceOffset = elementIndex * element.ElementByteLength;
            int destinationOffset = elementIndex * stride;
            int byteIndex = 0;
            while (byteIndex < element.ElementByteLength)
            {
                padded[destinationOffset + byteIndex] =
                    encoding.Bytes[sourceOffset + byteIndex];
                byteIndex++;
            }

            elementIndex++;
        }

        GltfBuffer buffer = document.Buffers[created.Buffer];
        buffer.ByteLength = padded.Length;
        buffer.ResolvedBytes = padded;
        GltfBufferView view = document.BufferViews[created.BufferView];
        view.ByteLength = padded.Length;
        view.HasByteStride = true;
        view.ByteStride = stride;
    }

    internal void RecordSourceBufferView(GltfDocument document, GltfAccessor source)
    {
        if (source.HasBufferView
            && source.BufferView >= 0
            && source.BufferView < document.BufferViews.Count
            && !SourceBufferViews.Contains(source.BufferView))
        {
            SourceBufferViews.Add(source.BufferView);
        }
    }

    internal int FindCachedAccessor(int source, int kind)
    {
        int index = 0;
        while (index < Cache.Count)
        {
            AccessorTransformCacheEntry entry = Cache[index];
            if (entry.Source == source && entry.Kind == kind)
            {
                return entry.Result;
            }

            index++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal void AddCachedAccessor(int source, int kind, int result)
    {
        AccessorTransformCacheEntry entry = new AccessorTransformCacheEntry();
        entry.Source = source;
        entry.Kind = kind;
        entry.Result = result;
        Cache.Add(entry);
    }
}
