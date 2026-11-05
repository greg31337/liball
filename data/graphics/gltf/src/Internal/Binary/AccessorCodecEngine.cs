using System;

namespace gltf;

internal class AccessorCodecReadContext
{
    internal bool Ready;
    internal GltfDocument Document = new GltfDocument();
    internal GltfAccessor Accessor = new GltfAccessor();
    internal GltfAccessorData Result = new GltfAccessorData();
    internal byte[] Source = [];
    internal AccessorByteRange Range = new AccessorByteRange();
}

internal class AccessorCodecViewSource
{
    internal bool Ready;
    internal byte[] Source = [];
    internal AccessorByteRange Range = new AccessorByteRange();
}

internal class AccessorCodecWriteContext
{
    internal bool Ready;
    internal GltfDocument Document = new GltfDocument();
    internal AccessorEncodingResult Encoding = new AccessorEncodingResult();
    internal GltfAccessorData Result = new GltfAccessorData();
    internal int InputKind;
}

internal class AccessorCodecEngine
{
    internal AccessorLayout Layout = new AccessorLayout();
    internal AccessorDecoder Decoder = new AccessorDecoder();
    internal AccessorNormalization Normalization = new AccessorNormalization();
    internal SparseAccessorDecoder SparseDecoder = new SparseAccessorDecoder();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal AccessorSparseConverter SparseConverter = new AccessorSparseConverter();

    internal GltfAccessorData ReadFloatingData(GltfAccessorReadRequest request)
    {
        AccessorCodecReadContext context = PrepareReadMetadata(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        if (IsMatrixType(context.Accessor.Type))
        {
            return FailReadForm(context, "Floating scalar/vector output is incompatible with a matrix accessor.");
        }

        PrepareReadSource(context, request);
        if (!context.Ready)
        {
            return context.Result;
        }

        double[] values = Decoder.DecodeFloatingValues(context.Source, context.Range);
        if (context.Accessor.Normalized && request.ApplyNormalization)
        {
            values = NormalizeDecodedValues(values, context.Accessor.ComponentType);
            context.Result.Normalized = true;
        }

        context.Result.FloatingValues = values;
        return context.Result;
    }

    internal GltfAccessorData ReadSignedData(GltfAccessorReadRequest request)
    {
        AccessorCodecReadContext context = PrepareReadMetadata(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        if ((context.Accessor.ComponentType != GltfComponentType.SignedByte
                && context.Accessor.ComponentType != GltfComponentType.SignedShort)
            || IsMatrixType(context.Accessor.Type))
        {
            return FailReadForm(context, "Signed values require signed byte or signed short components.");
        }

        PrepareReadSource(context, request);
        if (context.Ready)
        {
            context.Result.SignedValues = Decoder.DecodeSignedValues(
                context.Source,
                context.Range);
        }

        return context.Result;
    }

    internal GltfAccessorData ReadUnsignedData(GltfAccessorReadRequest request)
    {
        AccessorCodecReadContext context = PrepareReadMetadata(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        if (!IsUnsignedComponent(context.Accessor.ComponentType)
            || IsMatrixType(context.Accessor.Type))
        {
            return FailReadForm(context, "Unsigned values require an unsigned integer component type.");
        }

        PrepareReadSource(context, request);
        if (context.Ready)
        {
            context.Result.UnsignedValues = Decoder.DecodeUnsignedValues(
                context.Source,
                context.Range);
        }

        return context.Result;
    }

    internal GltfAccessorData ReadIndexData(GltfAccessorReadRequest request)
    {
        AccessorCodecReadContext context = PrepareReadMetadata(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        if (context.Accessor.Type != GltfAccessorType.Scalar
            || !IsUnsignedComponent(context.Accessor.ComponentType)
            || context.Accessor.Normalized)
        {
            return FailReadForm(context, "Indices require non-normalized scalar unsigned integer data.");
        }

        PrepareReadSource(context, request);
        if (context.Ready)
        {
            context.Result.Indices = Decoder.DecodeIndices(context.Source, context.Range);
        }

        return context.Result;
    }

    internal GltfAccessorData ReadMatrixData(GltfAccessorReadRequest request)
    {
        AccessorCodecReadContext context = PrepareReadMetadata(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        if (!IsMatrixType(context.Accessor.Type))
        {
            return FailReadForm(context, "Matrix values require a matrix accessor type.");
        }

        PrepareReadSource(context, request);
        if (!context.Ready)
        {
            return context.Result;
        }

        double[] values = Decoder.DecodeMatrixValues(context.Source, context.Range);
        if (context.Accessor.Normalized && request.ApplyNormalization)
        {
            values = NormalizeDecodedValues(values, context.Accessor.ComponentType);
            context.Result.Normalized = true;
        }

        context.Result.MatrixValues = values;
        return context.Result;
    }

    internal GltfAccessorData ReadRawData(GltfAccessorReadRequest request)
    {
        AccessorCodecReadContext context = PrepareReadMetadata(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        PrepareReadSource(context, request);
        if (context.Ready)
        {
            context.Result.RawBytes = Decoder.DecodeRawElements(context.Source, context.Range);
        }

        return context.Result;
    }

    internal GltfAccessorData CreateDenseData(GltfAccessorWriteRequest request)
    {
        AccessorCodecWriteContext context = PrepareWriteData(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        AccessorCreationResult creation = Encoder.CreateAccessorStorage(
            context.Document,
            context.Encoding,
            request.CalculateBounds);
        CompleteWriteResult(context, request, creation.Accessor);
        return context.Result;
    }

    internal GltfAccessorData CreateSparseData(GltfAccessorWriteRequest request)
    {
        AccessorCodecWriteContext context = PrepareWriteData(request);
        if (!context.Ready)
        {
            return context.Result;
        }

        if (!request.AllowSparse)
        {
            return FailWrite(
                context,
                GltfIssueCodes.ArrayTypeMismatch,
                "Sparse accessor creation requires AllowSparse.");
        }

        if (double.IsNaN(request.SparseZeroTolerance)
            || double.IsInfinity(request.SparseZeroTolerance)
            || request.SparseZeroTolerance < 0.0)
        {
            return FailWrite(
                context,
                GltfIssueCodes.AccessorInvalidFloat,
                "SparseZeroTolerance must be finite and non-negative.");
        }

        AccessorCreationResult creation = Encoder.CreateAccessorStorage(
            context.Document,
            context.Encoding,
            request.CalculateBounds);
        AccessorSparseConversionResult conversion = SparseConverter.GenerateSparseAccessor(
            context.Document,
            creation.Accessor,
            request.SparseZeroTolerance);
        RemoveDenseCreationStorage(context.Document, creation, conversion);
        CompleteWriteResult(context, request, creation.Accessor);
        return context.Result;
    }

    internal AccessorCodecReadContext PrepareReadMetadata(GltfAccessorReadRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        AccessorCodecReadContext context = new AccessorCodecReadContext();
        if (request.Document == null)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "The accessor read request does not contain a document.",
                "");
            return context;
        }

        context.Document = request.Document;
        if (request.Document.Accessors == null
            || request.Accessor < 0
            || request.Accessor >= request.Document.Accessors.Count
            || request.Document.Accessors[request.Accessor] == null)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "The accessor index does not resolve to an accessor.",
                $"/accessors/{request.Accessor}");
            return context;
        }

        context.Accessor = request.Document.Accessors[request.Accessor];
        context.Result.HasAccessor = true;
        context.Result.Accessor = request.Accessor;
        context.Result.ComponentType = context.Accessor.ComponentType;
        context.Result.Type = context.Accessor.Type;
        context.Result.Count = context.Accessor.Count;
        if (!Layout.IsKnownComponentType(context.Accessor.ComponentType)
            || !Layout.IsKnownAccessorType(context.Accessor.Type))
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ArrayTypeMismatch,
                "The accessor component type or shape is unknown.",
                $"/accessors/{request.Accessor}");
            return context;
        }

        if (context.Accessor.Count < 1)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorTooLong,
                "The accessor count must be positive.",
                $"/accessors/{request.Accessor}");
            return context;
        }

        context.Result.ComponentCount = Layout.GetComponentCount(context.Accessor.Type);
        long outputComponentCount = (long)context.Accessor.Count
            * context.Result.ComponentCount;
        if (request.MaximumElementCount < 0
            || context.Accessor.Count > request.MaximumElementCount
            || outputComponentCount > int.MaxValue)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorTooLong,
                "The accessor exceeds the requested output limit.",
                $"/accessors/{request.Accessor}/count");
            return context;
        }

        if (context.Accessor.Normalized
            && !IsNormalizableComponent(context.Accessor.ComponentType))
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorNormalizedInvalid,
                "Only byte and short integer components can be normalized.",
                $"/accessors/{request.Accessor}/normalized");
            return context;
        }

        context.Ready = true;
        return context;
    }

    internal void PrepareReadSource(
        AccessorCodecReadContext context,
        GltfAccessorReadRequest request)
    {
        GltfAccessor accessor = context.Accessor;
        if (PrepareCompressionSource(context))
        {
            return;
        }

        AccessorCodecViewSource? baseView = null;
        if (accessor.HasBufferView)
        {
            baseView = ResolveAccessorView(
                context,
                accessor,
                accessor.BufferView,
                false,
                $"/accessors/{context.Result.Accessor}/bufferView");
            if (!baseView.Ready)
            {
                return;
            }
        }

        if (!request.ApplySparseValues || accessor.Sparse == null)
        {
            if (baseView != null)
            {
                context.Source = baseView.Source;
                context.Range = baseView.Range;
                return;
            }

            if (accessor.Sparse != null && !request.ApplySparseValues)
            {
                CreateZeroAccessorSource(context);
                return;
            }

            if (NeedsDracoCodec(context.Document, context.Result.Accessor))
            {
                FailRead(
                    context,
                    GltfOperationStatus.Unavailable,
                    GltfIssueCodes.UnsupportedExtension,
                    "The accessor is available only through KHR_draco_mesh_compression, but no compression codec is configured.",
                    $"/accessors/{context.Result.Accessor}");
                return;
            }

            FailRead(
                context,
                GltfOperationStatus.Unavailable,
                GltfIssueCodes.UnresolvedReference,
                "The accessor does not have resolved base data.",
                $"/accessors/{context.Result.Accessor}/bufferView");
            return;
        }

        PrepareSparseSource(context, baseView);
    }

    internal bool PrepareCompressionSource(AccessorCodecReadContext context)
    {
        string key = "compression:accessor:" + context.Result.Accessor;
        if (context.Document.Resources == null
            || context.Document.Resources.Cache == null
            || !context.Document.Resources.Cache.ContainsKey(key))
        {
            return false;
        }

        GltfResource resource = context.Document.Resources.Cache[key];
        if (resource == null || resource.Bytes == null)
        {
            return false;
        }

        GltfAccessor payload = new GltfAccessor();
        payload.ComponentType = context.Accessor.ComponentType;
        payload.Count = context.Accessor.Count;
        payload.Type = context.Accessor.Type;
        AccessorByteRange range = Layout.CalculateAccessorByteRange(payload, null);
        if (range.BufferByteEnd > resource.Bytes.Length)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorTooLong,
                "The decoded compression stream is shorter than its accessor layout.",
                $"/accessors/{context.Result.Accessor}");
            return true;
        }

        context.Source = resource.Bytes;
        context.Range = range;
        return true;
    }

    internal AccessorCodecViewSource ResolveAccessorView(
        AccessorCodecReadContext context,
        GltfAccessor payload,
        int viewIndex,
        bool sparsePayload,
        string pointer)
    {
        AccessorCodecViewSource result = new AccessorCodecViewSource();
        GltfDocument document = context.Document;
        if (document.BufferViews == null
            || viewIndex < 0
            || viewIndex >= document.BufferViews.Count
            || document.BufferViews[viewIndex] == null)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "The buffer view reference is invalid.",
                pointer);
            return result;
        }

        GltfBufferView view = document.BufferViews[viewIndex];
        if ((view.HasByteOffset && view.ByteOffset < 0)
            || view.ByteLength < 0
            || (payload.HasByteOffset && payload.ByteOffset < 0)
            || (view.HasByteStride
                && (view.ByteStride < 4
                    || view.ByteStride > 252
                    || view.ByteStride % 4 != 0))
            || (view.HasTarget && !Layout.IsKnownBufferViewTarget(view.Target)))
        {
            string code = GltfIssueCodes.BufferViewTooLong;
            if (view.HasByteStride)
            {
                code = GltfIssueCodes.BufferViewInvalidByteStride;
            }

            FailRead(
                context,
                GltfOperationStatus.Invalid,
                code,
                "The accessor buffer view layout is invalid.",
                pointer);
            return result;
        }

        if (sparsePayload && (view.HasByteStride || view.HasTarget))
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.BufferViewInvalidByteStride,
                "Sparse buffer views cannot define byteStride or target.",
                pointer);
            return result;
        }

        BufferViewByteRange viewRange = Layout.CalculateBufferViewRange(view);
        AccessorByteRange range = Layout.CalculateAccessorByteRange(payload, viewRange);
        if (range.ElementByteStride < range.ElementLayout.ElementByteLength)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.BufferViewInvalidByteStride,
                "The buffer view stride is shorter than an accessor element.",
                pointer);
            return result;
        }

        if (range.AccessorByteEnd > view.ByteLength)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.BufferViewTooLong,
                "The accessor byte range exceeds its buffer view.",
                pointer);
            return result;
        }

        if (!Layout.IsComponentOffsetAligned(range.ElementLayout, range.AccessorByteOffset)
            || !Layout.IsElementOffsetAligned(
                range.ElementLayout,
                range.FirstElementBufferByteOffset))
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorOffsetAlignment,
                "The accessor byte range is misaligned or outside its buffer view.",
                pointer);
            return result;
        }

        if (document.Buffers == null
            || view.Buffer < 0
            || view.Buffer >= document.Buffers.Count
            || document.Buffers[view.Buffer] == null)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "The buffer view does not reference a valid buffer.",
                pointer);
            return result;
        }

        GltfBuffer buffer = document.Buffers[view.Buffer];
        if (!buffer.Resolved || buffer.ResolvedBytes == null)
        {
            if (HasMeshoptExtension(view) || HasMeshoptBufferExtension(buffer))
            {
                FailRead(
                    context,
                    GltfOperationStatus.Unavailable,
                    GltfIssueCodes.UnsupportedExtension,
                    "The buffer view requires a Meshopt compression codec.",
                    pointer);
            }
            else
            {
                FailRead(
                    context,
                    GltfOperationStatus.Unavailable,
                    GltfIssueCodes.UnresolvedReference,
                    "The accessor buffer has not been resolved.",
                    pointer);
            }

            return result;
        }

        if (buffer.ByteLength < 0
            || viewRange.ByteEnd > buffer.ByteLength
            || range.BufferByteEnd > buffer.ResolvedBytes.Length)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.BufferViewTooLong,
                "The accessor byte range exceeds the resolved buffer.",
                pointer);
            return result;
        }

        result.Source = buffer.ResolvedBytes;
        result.Range = range;
        result.Ready = true;
        return result;
    }

    internal void PrepareSparseSource(
        AccessorCodecReadContext context,
        AccessorCodecViewSource? baseView)
    {
        GltfAccessor accessor = context.Accessor;
        GltfAccessorSparse? sparse = accessor.Sparse;
        string pointer = $"/accessors/{context.Result.Accessor}/sparse";
        if (sparse == null || sparse.Indices == null || sparse.Values == null)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                "The sparse accessor payload is incomplete.",
                pointer);
            return;
        }

        if (sparse.Count < 1 || sparse.Count > accessor.Count)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorTooLong,
                "The sparse accessor count is outside the accessor count.",
                pointer);
            return;
        }

        if (!IsSparseIndexComponent(sparse.Indices.ComponentType))
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ArrayTypeMismatch,
                "Sparse indices require an unsigned integer component type.",
                $"{pointer}/indices/componentType");
            return;
        }

        GltfAccessor indexPayload = new GltfAccessor();
        indexPayload.HasByteOffset = sparse.Indices.HasByteOffset;
        indexPayload.ByteOffset = sparse.Indices.ByteOffset;
        indexPayload.ComponentType = sparse.Indices.ComponentType;
        indexPayload.Count = sparse.Count;
        indexPayload.Type = GltfAccessorType.Scalar;
        AccessorCodecViewSource indexView = ResolveAccessorView(
            context,
            indexPayload,
            sparse.Indices.BufferView,
            true,
            $"{pointer}/indices/bufferView");
        if (!indexView.Ready)
        {
            return;
        }

        GltfAccessor valuePayload = new GltfAccessor();
        valuePayload.HasByteOffset = sparse.Values.HasByteOffset;
        valuePayload.ByteOffset = sparse.Values.ByteOffset;
        valuePayload.ComponentType = accessor.ComponentType;
        valuePayload.Count = sparse.Count;
        valuePayload.Type = accessor.Type;
        AccessorCodecViewSource valueView = ResolveAccessorView(
            context,
            valuePayload,
            sparse.Values.BufferView,
            true,
            $"{pointer}/values/bufferView");
        if (!valueView.Ready)
        {
            return;
        }

        uint[] indices = Decoder.DecodeUnsignedValues(indexView.Source, indexView.Range);
        uint previous = 0U;
        int index = 0;
        while (index < indices.Length)
        {
            if ((long)indices[index] >= accessor.Count
                || (index > 0 && indices[index] <= previous))
            {
                FailRead(
                    context,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "Sparse indices must be strictly increasing and within the accessor count.",
                    $"{pointer}/indices");
                return;
            }

            previous = indices[index];
            index++;
        }

        byte[] baseSource = [];
        AccessorByteRange? baseRange = null;
        if (baseView != null)
        {
            baseSource = baseView.Source;
            baseRange = baseView.Range;
        }

        context.Source = SparseDecoder.OverlayRawElements(
            baseSource,
            baseRange,
            indexView.Source,
            indexView.Range,
            valueView.Source,
            valueView.Range,
            accessor.Count);
        GltfAccessor tightAccessor = new GltfAccessor();
        tightAccessor.ComponentType = accessor.ComponentType;
        tightAccessor.Type = accessor.Type;
        tightAccessor.Count = accessor.Count;
        context.Range = Layout.CalculateAccessorByteRange(tightAccessor, null);
    }

    internal void CreateZeroAccessorSource(AccessorCodecReadContext context)
    {
        GltfAccessor payload = new GltfAccessor();
        payload.ComponentType = context.Accessor.ComponentType;
        payload.Type = context.Accessor.Type;
        payload.Count = context.Accessor.Count;
        AccessorByteRange range = Layout.CalculateAccessorByteRange(payload, null);
        if (range.AccessorByteLength > int.MaxValue)
        {
            FailRead(
                context,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.AccessorTooLong,
                "The accessor byte length exceeds the supported output size.",
                $"/accessors/{context.Result.Accessor}");
            return;
        }

        context.Source = new byte[(int)range.AccessorByteLength];
        context.Range = range;
    }

    internal AccessorCodecWriteContext PrepareWriteData(GltfAccessorWriteRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        AccessorCodecWriteContext context = new AccessorCodecWriteContext();
        if (request.Document == null)
        {
            FailWrite(
                context,
                GltfIssueCodes.UnresolvedReference,
                "The accessor write request does not contain a document.");
            return context;
        }

        context.Document = request.Document;
        if (request.Document.Accessors == null
            || request.Document.BufferViews == null
            || request.Document.Buffers == null
            || request.FloatingValues == null
            || request.SignedValues == null
            || request.UnsignedValues == null
            || request.RawBytes == null
            || !Layout.IsKnownComponentType(request.ComponentType)
            || !Layout.IsKnownAccessorType(request.Type))
        {
            FailWrite(
                context,
                GltfIssueCodes.ArrayTypeMismatch,
                "The accessor write request is incomplete or has an unknown layout.");
            return context;
        }

        if (request.Normalized && !IsNormalizableComponent(request.ComponentType))
        {
            FailWrite(
                context,
                GltfIssueCodes.AccessorNormalizedInvalid,
                "Only byte and short integer components can be normalized.");
            return context;
        }

        int sourceCount = 0;
        if (request.FloatingValues.Length > 0)
        {
            sourceCount++;
            context.InputKind = 1;
        }

        if (request.SignedValues.Length > 0)
        {
            sourceCount++;
            context.InputKind = 2;
        }

        if (request.UnsignedValues.Length > 0)
        {
            sourceCount++;
            context.InputKind = 3;
        }

        if (request.RawBytes.Length > 0)
        {
            sourceCount++;
            context.InputKind = 4;
        }

        if (sourceCount != 1)
        {
            FailWrite(
                context,
                GltfIssueCodes.ArrayTypeMismatch,
                "Exactly one non-empty accessor input representation is required.");
            return context;
        }

        AccessorElementLayout element = Layout.CalculateElementLayout(
            request.ComponentType,
            request.Type);
        int inputLength = GetWriteInputLength(request, context.InputKind);
        int divisor = element.ComponentCount;
        if (context.InputKind == 4)
        {
            divisor = element.ElementByteLength;
        }

        if (inputLength == 0 || inputLength % divisor != 0)
        {
            FailWrite(
                context,
                GltfIssueCodes.ArrayTypeMismatch,
                "The input length does not contain complete accessor elements.");
            return context;
        }

        if (!ValidateWriteRepresentation(request, context, element))
        {
            return context;
        }

        if (!ValidateWriteValues(request, context))
        {
            return context;
        }

        context.Encoding = EncodeWriteInput(request, context.InputKind);
        if (!ValidateEncodedFloatValues(request, context))
        {
            return context;
        }

        context.Result.ComponentType = request.ComponentType;
        context.Result.Type = request.Type;
        context.Result.ComponentCount = element.ComponentCount;
        context.Result.Count = context.Encoding.Count;
        context.Result.Normalized = request.Normalized;
        context.Ready = true;
        return context;
    }

    internal bool ValidateWriteRepresentation(
        GltfAccessorWriteRequest request,
        AccessorCodecWriteContext context,
        AccessorElementLayout element)
    {
        if (context.InputKind == 1)
        {
            if (request.ComponentType != GltfComponentType.Float
                && (!request.Normalized || !IsNormalizableComponent(request.ComponentType)))
            {
                FailWrite(
                    context,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "Floating input requires float components or normalized byte/short components.");
                return false;
            }
        }
        else if (context.InputKind == 2)
        {
            if (IsMatrixType(request.Type)
                || (request.ComponentType != GltfComponentType.SignedByte
                    && request.ComponentType != GltfComponentType.SignedShort))
            {
                FailWrite(
                    context,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "Signed input requires a scalar/vector signed byte or signed short layout.");
                return false;
            }
        }
        else if (context.InputKind == 3)
        {
            if (IsMatrixType(request.Type)
                || !IsUnsignedComponent(request.ComponentType)
                || (request.ComponentType == GltfComponentType.UnsignedInt
                    && request.Type != GltfAccessorType.Scalar))
            {
                FailWrite(
                    context,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "Unsigned input requires an unsigned scalar/vector layout; unsigned int is limited to scalar indices.");
                return false;
            }
        }
        else if (context.InputKind == 4
            && request.ComponentType == GltfComponentType.UnsignedInt
            && request.Type != GltfAccessorType.Scalar)
        {
            FailWrite(
                context,
                GltfIssueCodes.ArrayTypeMismatch,
                "Unsigned int raw input is limited to scalar indices.");
            return false;
        }

        if (element.ComponentCount < 1)
        {
            FailWrite(
                context,
                GltfIssueCodes.ArrayTypeMismatch,
                "The accessor element layout is invalid.");
            return false;
        }

        return true;
    }

    internal bool ValidateWriteValues(
        GltfAccessorWriteRequest request,
        AccessorCodecWriteContext context)
    {
        if (context.InputKind == 1)
        {
            int index = 0;
            while (index < request.FloatingValues.Length)
            {
                double value = request.FloatingValues[index];
                bool invalid = double.IsNaN(value) || double.IsInfinity(value);
                if (request.ComponentType == GltfComponentType.Float
                    && float.IsInfinity((float)value))
                {
                    invalid = true;
                }

                if (request.ComponentType == GltfComponentType.UnsignedByte
                    || request.ComponentType == GltfComponentType.UnsignedShort)
                {
                    if (value < 0.0 || value > 1.0)
                    {
                        invalid = true;
                    }
                }
                else if (request.ComponentType != GltfComponentType.Float
                    && (value < -1.0 || value > 1.0))
                {
                    invalid = true;
                }

                if (invalid)
                {
                    FailWrite(
                        context,
                        GltfIssueCodes.AccessorInvalidFloat,
                        "Floating input values must be finite and within the encoded normalized range.");
                    return false;
                }

                index++;
            }
        }
        else if (context.InputKind == 2)
        {
            int minimum = -32768;
            int maximum = 32767;
            if (request.ComponentType == GltfComponentType.SignedByte)
            {
                minimum = -128;
                maximum = 127;
            }

            int index = 0;
            while (index < request.SignedValues.Length)
            {
                if (request.SignedValues[index] < minimum
                    || request.SignedValues[index] > maximum)
                {
                    FailWrite(
                        context,
                        GltfIssueCodes.ArrayTypeMismatch,
                        "A signed input value is outside the selected component range.");
                    return false;
                }

                index++;
            }
        }
        else if (context.InputKind == 3)
        {
            uint maximum = uint.MaxValue;
            if (request.ComponentType == GltfComponentType.UnsignedByte)
            {
                maximum = 255U;
            }
            else if (request.ComponentType == GltfComponentType.UnsignedShort)
            {
                maximum = 65535U;
            }

            int index = 0;
            while (index < request.UnsignedValues.Length)
            {
                if (request.UnsignedValues[index] > maximum)
                {
                    FailWrite(
                        context,
                        GltfIssueCodes.ArrayTypeMismatch,
                        "An unsigned input value is outside the selected component range.");
                    return false;
                }

                index++;
            }
        }

        return true;
    }

    internal bool ValidateEncodedFloatValues(
        GltfAccessorWriteRequest request,
        AccessorCodecWriteContext context)
    {
        if (request.ComponentType != GltfComponentType.Float)
        {
            return true;
        }

        GltfAccessor payload = new GltfAccessor();
        payload.ComponentType = context.Encoding.ComponentType;
        payload.Type = context.Encoding.Type;
        payload.Count = context.Encoding.Count;
        AccessorByteRange range = Layout.CalculateAccessorByteRange(payload, null);
        double[] values = Decoder.DecodeFloatingValues(context.Encoding.Bytes, range);
        int index = 0;
        while (index < values.Length)
        {
            if (double.IsNaN(values[index]) || double.IsInfinity(values[index]))
            {
                FailWrite(
                    context,
                    GltfIssueCodes.AccessorInvalidFloat,
                    "Encoded floating input contains a non-finite value.");
                return false;
            }

            index++;
        }

        return true;
    }

    internal AccessorEncodingResult EncodeWriteInput(
        GltfAccessorWriteRequest request,
        int inputKind)
    {
        if (inputKind == 4)
        {
            bool indexData = request.ComponentType == GltfComponentType.UnsignedInt;
            return Encoder.EncodeRawAccessor(
                request.RawBytes,
                request.ComponentType,
                request.Type,
                request.Normalized,
                indexData);
        }

        if (inputKind == 1)
        {
            if (IsMatrixType(request.Type))
            {
                return Encoder.EncodeMatrixAccessor(
                    request.FloatingValues,
                    request.ComponentType,
                    request.Type,
                    request.Normalized);
            }

            return Encoder.EncodeFloatingAccessor(
                request.FloatingValues,
                request.ComponentType,
                request.Type,
                request.Normalized);
        }

        if (inputKind == 2)
        {
            return Encoder.EncodeSignedAccessor(
                request.SignedValues,
                request.ComponentType,
                request.Type,
                request.Normalized);
        }

        if (request.ComponentType == GltfComponentType.UnsignedInt)
        {
            return Encoder.EncodeIndexAccessor(
                request.UnsignedValues,
                request.ComponentType);
        }

        return Encoder.EncodeUnsignedAccessor(
            request.UnsignedValues,
            request.ComponentType,
            request.Type,
            request.Normalized);
    }

    internal void CompleteWriteResult(
        AccessorCodecWriteContext context,
        GltfAccessorWriteRequest request,
        int accessorIndex)
    {
        context.Result.Status = GltfOperationStatus.Success;
        context.Result.HasAccessor = true;
        context.Result.Accessor = accessorIndex;
        if (context.InputKind == 1)
        {
            context.Result.FloatingValues = CopyDoubleArray(request.FloatingValues);
            if (IsMatrixType(request.Type))
            {
                context.Result.MatrixValues = CopyDoubleArray(request.FloatingValues);
            }
        }
        else if (context.InputKind == 2)
        {
            context.Result.SignedValues = CopyIntArray(request.SignedValues);
        }
        else if (context.InputKind == 3)
        {
            context.Result.UnsignedValues = CopyUIntArray(request.UnsignedValues);
            if (request.ComponentType == GltfComponentType.UnsignedInt)
            {
                context.Result.Indices = CopyUIntArray(request.UnsignedValues);
            }
        }
        else
        {
            context.Result.RawBytes = CopyByteArray(request.RawBytes);
        }
    }

    internal void RemoveDenseCreationStorage(
        GltfDocument document,
        AccessorCreationResult creation,
        AccessorSparseConversionResult conversion)
    {
        document.BufferViews.RemoveAt(creation.BufferView);
        document.Buffers.RemoveAt(creation.Buffer);
        if (conversion.SparseCount > 0)
        {
            GltfAccessor accessor = document.Accessors[creation.Accessor];
            GltfAccessorSparse? sparse = accessor.Sparse;
            if (sparse == null)
            {
                return;
            }

            sparse.Indices.BufferView--;
            sparse.Values.BufferView--;
            GltfBufferView indicesView = document.BufferViews[sparse.Indices.BufferView];
            GltfBufferView valuesView = document.BufferViews[sparse.Values.BufferView];
            indicesView.Buffer--;
            valuesView.Buffer--;
        }
    }

    internal double[] NormalizeDecodedValues(
        double[] values,
        GltfComponentType componentType)
    {
        if (componentType == GltfComponentType.SignedByte
            || componentType == GltfComponentType.SignedShort)
        {
            int[] signed = new int[values.Length];
            int index = 0;
            while (index < values.Length)
            {
                signed[index] = (int)values[index];
                index++;
            }

            return Normalization.DecodeSignedValues(signed, componentType);
        }

        uint[] unsigned = new uint[values.Length];
        int unsignedIndex = 0;
        while (unsignedIndex < values.Length)
        {
            unsigned[unsignedIndex] = (uint)values[unsignedIndex];
            unsignedIndex++;
        }

        return Normalization.DecodeUnsignedValues(unsigned, componentType);
    }

    internal GltfAccessorData FailReadForm(
        AccessorCodecReadContext context,
        string message)
    {
        FailRead(
            context,
            GltfOperationStatus.Invalid,
            GltfIssueCodes.ArrayTypeMismatch,
            message,
            $"/accessors/{context.Result.Accessor}");
        return context.Result;
    }

    internal void FailRead(
        AccessorCodecReadContext context,
        GltfOperationStatus status,
        string code,
        string message,
        string pointer)
    {
        context.Ready = false;
        context.Result.Status = status;
        context.Result.Diagnostic = CreateDiagnostic(code, message, pointer);
    }

    internal GltfAccessorData FailWrite(
        AccessorCodecWriteContext context,
        string code,
        string message)
    {
        context.Ready = false;
        context.Result.Status = GltfOperationStatus.Invalid;
        context.Result.Diagnostic = CreateDiagnostic(code, message, "");
        return context.Result;
    }

    internal GltfValidationIssue CreateDiagnostic(
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        return issue;
    }

    internal bool IsUnsignedComponent(GltfComponentType componentType)
    {
        return componentType == GltfComponentType.UnsignedByte
            || componentType == GltfComponentType.UnsignedShort
            || componentType == GltfComponentType.UnsignedInt;
    }

    internal bool IsSparseIndexComponent(GltfComponentType componentType)
    {
        return IsUnsignedComponent(componentType);
    }

    internal bool IsNormalizableComponent(GltfComponentType componentType)
    {
        return componentType == GltfComponentType.SignedByte
            || componentType == GltfComponentType.UnsignedByte
            || componentType == GltfComponentType.SignedShort
            || componentType == GltfComponentType.UnsignedShort;
    }

    internal bool IsMatrixType(GltfAccessorType type)
    {
        return type == GltfAccessorType.Matrix2
            || type == GltfAccessorType.Matrix3
            || type == GltfAccessorType.Matrix4;
    }

    internal bool HasMeshoptExtension(GltfBufferView view)
    {
        return view.PropertyJson != null
            && view.PropertyJson.Extensions != null
            && (view.PropertyJson.Extensions.ContainsKey(
                    GltfExtensionNames.KhrMeshoptCompression)
                || view.PropertyJson.Extensions.ContainsKey(
                    GltfExtensionNames.ExtMeshoptCompression));
    }

    internal bool HasMeshoptBufferExtension(GltfBuffer buffer)
    {
        return buffer.PropertyJson != null
            && buffer.PropertyJson.Extensions != null
            && (buffer.PropertyJson.Extensions.ContainsKey(
                    GltfExtensionNames.KhrMeshoptCompression)
                || buffer.PropertyJson.Extensions.ContainsKey(
                    GltfExtensionNames.ExtMeshoptCompression));
    }

    internal bool NeedsDracoCodec(GltfDocument document, int accessorIndex)
    {
        if (document.Meshes == null)
        {
            return false;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            if (mesh != null && mesh.Primitives != null)
            {
                int primitiveIndex = 0;
                while (primitiveIndex < mesh.Primitives.Count)
                {
                    GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                    if (primitive != null
                        && HasDracoExtension(primitive)
                        && PrimitiveReferencesAccessor(primitive, accessorIndex))
                    {
                        return true;
                    }

                    primitiveIndex++;
                }
            }

            meshIndex++;
        }

        return false;
    }

    internal bool HasDracoExtension(GltfMeshPrimitive primitive)
    {
        return primitive.PropertyJson != null
            && primitive.PropertyJson.Extensions != null
            && primitive.PropertyJson.Extensions.ContainsKey(
                GltfExtensionNames.KhrDracoMeshCompression);
    }

    internal bool PrimitiveReferencesAccessor(
        GltfMeshPrimitive primitive,
        int accessorIndex)
    {
        if (primitive.HasIndices && primitive.Indices == accessorIndex)
        {
            return true;
        }

        if (primitive.Attributes != null)
        {
            int index = 0;
            while (index < primitive.Attributes.Count)
            {
                GltfAttribute attribute = primitive.Attributes[index];
                if (attribute != null && attribute.Accessor == accessorIndex)
                {
                    return true;
                }

                index++;
            }
        }

        if (primitive.Targets == null)
        {
            return false;
        }

        int targetIndex = 0;
        while (targetIndex < primitive.Targets.Count)
        {
            GltfMorphTarget target = primitive.Targets[targetIndex];
            if (target != null && target.Attributes != null)
            {
                int targetAttributeIndex = 0;
                while (targetAttributeIndex < target.Attributes.Count)
                {
                    GltfAttribute attribute = target.Attributes[targetAttributeIndex];
                    if (attribute != null && attribute.Accessor == accessorIndex)
                    {
                        return true;
                    }

                    targetAttributeIndex++;
                }
            }

            targetIndex++;
        }

        return false;
    }

    internal int GetWriteInputLength(GltfAccessorWriteRequest request, int inputKind)
    {
        if (inputKind == 1)
        {
            return request.FloatingValues.Length;
        }

        if (inputKind == 2)
        {
            return request.SignedValues.Length;
        }

        if (inputKind == 3)
        {
            return request.UnsignedValues.Length;
        }

        return request.RawBytes.Length;
    }

    internal double[] CopyDoubleArray(double[] source)
    {
        double[] result = new double[source.Length];
        source.CopyTo(result, 0);
        return result;
    }

    internal int[] CopyIntArray(int[] source)
    {
        int[] result = new int[source.Length];
        source.CopyTo(result, 0);
        return result;
    }

    internal uint[] CopyUIntArray(uint[] source)
    {
        uint[] result = new uint[source.Length];
        source.CopyTo(result, 0);
        return result;
    }

    internal byte[] CopyByteArray(byte[] source)
    {
        byte[] result = new byte[source.Length];
        source.CopyTo(result, 0);
        return result;
    }
}
