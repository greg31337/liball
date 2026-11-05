using System;
using System.Collections.Generic;

namespace gltf;

internal class AnimationSamplerLayout
{
    internal int Sampler = GltfConstants.AbsentIndex;
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal bool IsValid = true;
    internal bool InputReady;
    internal bool OutputReady;
    internal GltfAnimationInterpolation Interpolation = GltfAnimationInterpolation.Linear;
    internal int InputAccessor = GltfConstants.AbsentIndex;
    internal int OutputAccessor = GltfConstants.AbsentIndex;
    internal int KeyframeCount;
    internal double FirstTime;
    internal double LastTime;
    internal double[] KeyTimes = [];
    internal double[] OutputValues = [];
    internal GltfAccessor? Output;
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class AnimationChannelLayout
{
    internal int Channel = GltfConstants.AbsentIndex;
    internal int Sampler = GltfConstants.AbsentIndex;
    internal int Node = GltfConstants.AbsentIndex;
    internal GltfAnimationPath Path = GltfAnimationPath.Translation;
    internal string Pointer = "";
    internal AnimationPointerValueType PointerValueType = AnimationPointerValueType.Float;
    internal bool Quaternion;
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal bool IsValid = true;
    internal bool ShapeKnown = true;
    internal bool CubicSpline;
    internal int MorphTargetCount;
    internal int ValueComponentCount;
    internal int ExpectedOutputElementCount;
    internal List<int> InTangentOffsets = new List<int>();
    internal List<int> ValueOffsets = new List<int>();
    internal List<int> OutTangentOffsets = new List<int>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class AnimationLayoutAnalysis
{
    internal int Animation = GltfConstants.AbsentIndex;
    internal GltfOperationStatus Status = GltfOperationStatus.Success;
    internal bool IsValid = true;
    internal bool HasDuration;
    internal double Duration;
    internal List<AnimationSamplerLayout> Samplers = new List<AnimationSamplerLayout>();
    internal List<AnimationChannelLayout> Channels = new List<AnimationChannelLayout>();
    internal List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}

internal class AnimationLayoutAnalyzer
{
    internal AccessorCodecEngine AccessorCodec = new AccessorCodecEngine();
    internal AccessorLayout AccessorLayout = new AccessorLayout();
    internal AnimationPointerResolver PointerResolver = new AnimationPointerResolver();

    internal AnimationLayoutAnalysis AnalyzeAnimation(GltfDocument document, int animationIndex)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        AnimationLayoutAnalysis result = new AnimationLayoutAnalysis();
        result.Animation = animationIndex;
        if (document.Animations == null
            || animationIndex < 0
            || animationIndex >= document.Animations.Count)
        {
            AddAnalysisIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Animation index {animationIndex} does not resolve to an animation.",
                $"/animations/{animationIndex}");
            return result;
        }

        GltfAnimation animation = document.Animations[animationIndex];
        if (animation == null)
        {
            AddAnalysisIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The selected animation is null.",
                $"/animations/{animationIndex}");
            return result;
        }

        AnalyzeSamplers(document, animation, result);
        AnalyzeChannels(document, animation, result);
        return result;
    }

    internal void AnalyzeSamplers(
        GltfDocument document,
        GltfAnimation animation,
        AnimationLayoutAnalysis result)
    {
        string pointer = $"/animations/{result.Animation}/samplers";
        if (animation.Samplers == null)
        {
            AddAnalysisIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The animation sampler collection is missing.",
                pointer);
            return;
        }

        int samplerIndex = 0;
        while (samplerIndex < animation.Samplers.Count)
        {
            AnimationSamplerLayout layout = new AnimationSamplerLayout();
            layout.Sampler = samplerIndex;
            result.Samplers.Add(layout);
            GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
            if (sampler == null)
            {
                AddSamplerIssue(
                    result,
                    layout,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The animation sampler is null.",
                    $"{pointer}/{samplerIndex}");
            }
            else
            {
                AnalyzeSampler(document, sampler, result, layout);
            }

            samplerIndex++;
        }
    }

    internal void AnalyzeSampler(
        GltfDocument document,
        GltfAnimationSampler sampler,
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout)
    {
        string pointer = $"/animations/{result.Animation}/samplers/{layout.Sampler}";
        layout.InputAccessor = sampler.Input;
        layout.OutputAccessor = sampler.Output;
        layout.Interpolation = GltfAnimationInterpolation.Linear;
        if (sampler.HasInterpolation)
        {
            layout.Interpolation = sampler.Interpolation;
        }

        if (layout.Interpolation != GltfAnimationInterpolation.Linear
            && layout.Interpolation != GltfAnimationInterpolation.Step
            && layout.Interpolation != GltfAnimationInterpolation.CubicSpline)
        {
            AddSamplerIssue(
                result,
                layout,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Error,
                $"Animation interpolation value {(int)layout.Interpolation} is not supported.",
                $"{pointer}/interpolation");
        }

        GltfAccessor? input = ResolveSamplerAccessor(
            document,
            sampler.Input,
            result,
            layout,
            $"{pointer}/input");
        GltfAccessor? output = ResolveSamplerAccessor(
            document,
            sampler.Output,
            result,
            layout,
            $"{pointer}/output");
        layout.Output = output;
        if (input != null)
        {
            AnalyzeInputAccessor(document, input, result, layout, pointer);
        }

        if (output != null)
        {
            AnalyzeOutputAccessor(document, output, result, layout, pointer);
        }
    }

    internal GltfAccessor? ResolveSamplerAccessor(
        GltfDocument document,
        int accessorIndex,
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        string pointer)
    {
        if (document.Accessors == null
            || accessorIndex < 0
            || accessorIndex >= document.Accessors.Count
            || document.Accessors[accessorIndex] == null)
        {
            AddSamplerIssue(
                result,
                layout,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Accessor index {accessorIndex} does not resolve to an accessor.",
                pointer);
            return null;
        }

        return document.Accessors[accessorIndex];
    }

    internal void AnalyzeInputAccessor(
        GltfDocument document,
        GltfAccessor accessor,
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        string samplerPointer)
    {
        string pointer = $"/accessors/{layout.InputAccessor}";
        layout.KeyframeCount = accessor.Count;
        if (accessor.ComponentType != GltfComponentType.Float
            || accessor.Type != GltfAccessorType.Scalar
            || accessor.Normalized)
        {
            AddSamplerIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerInputAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "Animation input accessors must contain non-normalized FLOAT SCALAR values.",
                pointer);
        }

        int minimumCount = 1;
        if (layout.Interpolation == GltfAnimationInterpolation.CubicSpline)
        {
            minimumCount = 2;
        }

        if (accessor.Count < minimumCount)
        {
            AddSamplerIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerInputAccessorTooFewElements,
                GltfValidationSeverity.Error,
                $"Animation interpolation requires at least {minimumCount} input keyframes.",
                $"{pointer}/count");
        }

        if (accessor.Minimum == null || accessor.Maximum == null)
        {
            AddSamplerIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerInputAccessorWithoutBounds,
                GltfValidationSeverity.Error,
                "Animation input accessors must define minimum and maximum bounds.",
                pointer);
        }

        ValidateAccessorStride(document, accessor, result, layout, pointer);
        if (accessor.ComponentType != GltfComponentType.Float
            || accessor.Type != GltfAccessorType.Scalar
            || accessor.Count < 1)
        {
            return;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = layout.InputAccessor;
        request.ApplyNormalization = false;
        GltfAccessorData data = AccessorCodec.ReadFloatingData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            CopySamplerReadFailure(result, layout, data, samplerPointer);
            return;
        }

        layout.InputReady = true;
        layout.KeyTimes = data.FloatingValues;
        ValidateKeyTimes(result, layout, pointer);
    }

    internal void AnalyzeOutputAccessor(
        GltfDocument document,
        GltfAccessor accessor,
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        string samplerPointer)
    {
        string pointer = $"/accessors/{layout.OutputAccessor}";
        ValidateAccessorStride(document, accessor, result, layout, pointer);
        if (!AccessorLayout.IsKnownComponentType(accessor.ComponentType)
            || !AccessorLayout.IsKnownAccessorType(accessor.Type)
            || accessor.Type == GltfAccessorType.Matrix2
            || accessor.Type == GltfAccessorType.Matrix3
            || accessor.Type == GltfAccessorType.Matrix4
            || accessor.Count < 1)
        {
            return;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = layout.OutputAccessor;
        request.ApplyNormalization = true;
        GltfAccessorData data = AccessorCodec.ReadFloatingData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            CopySamplerReadFailure(result, layout, data, samplerPointer);
            return;
        }

        layout.OutputReady = true;
        layout.OutputValues = data.FloatingValues;
        int valueIndex = 0;
        while (valueIndex < layout.OutputValues.Length)
        {
            if (!double.IsFinite(layout.OutputValues[valueIndex]))
            {
                AddSamplerIssue(
                    result,
                    layout,
                    GltfIssueCodes.AccessorInvalidFloat,
                    GltfValidationSeverity.Error,
                    $"Animation output component {valueIndex} is not finite.",
                    pointer);
                return;
            }

            valueIndex++;
        }
    }

    internal void ValidateAccessorStride(
        GltfDocument document,
        GltfAccessor accessor,
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        string pointer)
    {
        if (!accessor.HasBufferView
            || document.BufferViews == null
            || accessor.BufferView < 0
            || accessor.BufferView >= document.BufferViews.Count)
        {
            return;
        }

        GltfBufferView view = document.BufferViews[accessor.BufferView];
        if (view != null && view.HasByteStride)
        {
            AddSamplerIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerAccessorWithBytestride,
                GltfValidationSeverity.Error,
                "Animation sampler accessors must be tightly packed without byteStride.",
                pointer);
        }
    }

    internal void ValidateKeyTimes(
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        string pointer)
    {
        int timeIndex = 0;
        while (timeIndex < layout.KeyTimes.Length)
        {
            double time = layout.KeyTimes[timeIndex];
            if (!double.IsFinite(time))
            {
                AddSamplerIssue(
                    result,
                    layout,
                    GltfIssueCodes.AccessorInvalidFloat,
                    GltfValidationSeverity.Error,
                    $"Animation input time at index {timeIndex} is not finite.",
                    $"{pointer}/{timeIndex}");
            }
            else
            {
                if (time < 0.0)
                {
                    AddSamplerIssue(
                        result,
                        layout,
                        GltfIssueCodes.AccessorAnimationInputNegative,
                        GltfValidationSeverity.Error,
                        $"Animation input time {time} at index {timeIndex} is negative.",
                        $"{pointer}/{timeIndex}");
                }

                if (timeIndex > 0
                    && double.IsFinite(layout.KeyTimes[timeIndex - 1])
                    && time <= layout.KeyTimes[timeIndex - 1])
                {
                    AddSamplerIssue(
                        result,
                        layout,
                        GltfIssueCodes.AccessorAnimationInputNonIncreasing,
                        GltfValidationSeverity.Error,
                        "Animation input times must be strictly increasing.",
                        $"{pointer}/{timeIndex}");
                }
            }

            timeIndex++;
        }

        if (layout.KeyTimes.Length > 0)
        {
            layout.FirstTime = layout.KeyTimes[0];
            layout.LastTime = layout.KeyTimes[layout.KeyTimes.Length - 1];
            if (double.IsFinite(layout.LastTime)
                && (!result.HasDuration || layout.LastTime > result.Duration))
            {
                result.HasDuration = true;
                result.Duration = layout.LastTime;
            }
        }
    }

    internal void AnalyzeChannels(
        GltfDocument document,
        GltfAnimation animation,
        AnimationLayoutAnalysis result)
    {
        string pointer = $"/animations/{result.Animation}/channels";
        if (animation.Channels == null)
        {
            AddAnalysisIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                GltfValidationSeverity.Error,
                "The animation channel collection is missing.",
                pointer);
            return;
        }

        int channelIndex = 0;
        while (channelIndex < animation.Channels.Count)
        {
            AnimationChannelLayout layout = new AnimationChannelLayout();
            layout.Channel = channelIndex;
            result.Channels.Add(layout);
            GltfAnimationChannel channel = animation.Channels[channelIndex];
            if (channel == null)
            {
                AddChannelIssue(
                    result,
                    layout,
                    GltfIssueCodes.ArrayTypeMismatch,
                    GltfValidationSeverity.Error,
                    "The animation channel is null.",
                    $"{pointer}/{channelIndex}");
            }
            else
            {
                AnalyzeChannel(document, animation, channel, result, layout);
            }

            channelIndex++;
        }
    }

    internal void AnalyzeChannel(
        GltfDocument document,
        GltfAnimation animation,
        GltfAnimationChannel channel,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout)
    {
        string pointer = $"/animations/{result.Animation}/channels/{layout.Channel}";
        layout.Sampler = channel.Sampler;
        if (channel.Sampler < 0 || channel.Sampler >= result.Samplers.Count)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Animation sampler index {channel.Sampler} does not resolve to a sampler.",
                $"{pointer}/sampler");
            return;
        }

        AnimationSamplerLayout sampler = result.Samplers[channel.Sampler];
        layout.CubicSpline = sampler.Interpolation == GltfAnimationInterpolation.CubicSpline;
        if (!sampler.IsValid)
        {
            layout.IsValid = false;
            layout.Status = sampler.Status;
        }

        if (channel.Target == null)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "The animation channel target is missing.",
                $"{pointer}/target");
            return;
        }

        layout.Node = channel.Target.Node;
        layout.Path = channel.Target.Path;
        if (layout.Path == GltfAnimationPath.Pointer)
        {
            ConfigurePointerTarget(document, animation, channel.Target, result, layout, pointer);
            ValidateOutputLayout(result, layout, sampler);
            return;
        }

        if (layout.Path != GltfAnimationPath.Translation
            && layout.Path != GltfAnimationPath.Rotation
            && layout.Path != GltfAnimationPath.Scale
            && layout.Path != GltfAnimationPath.Weights)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Error,
                $"Animation path value {(int)layout.Path} is not supported.",
                $"{pointer}/target/path");
            return;
        }

        GltfNode? node = ResolveTargetNode(document, channel.Target, result, layout, pointer);
        if (node == null)
        {
            return;
        }

        ValidateDuplicateTarget(animation, channel.Target, result, layout, pointer);
        ValidateTargetNode(node, result, layout, pointer);
        ConfigureTargetShape(document, node, result, layout, pointer);
        ValidateOutputLayout(result, layout, sampler);
    }

    internal void ConfigurePointerTarget(
        GltfDocument document,
        GltfAnimation animation,
        GltfAnimationTarget target,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string pointer)
    {
        string? targetPointer = PointerResolver.GetTargetPointer(target);
        if (targetPointer == null)
        {
            layout.ShapeKnown = false;
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.UndefinedProperty,
                GltfValidationSeverity.Error,
                "A pointer animation target must define a string pointer.",
                pointer + "/target/extensions/KHR_animation_pointer/pointer");
            return;
        }

        AnimationPointerTarget resolved = PointerResolver.ResolvePointer(
            document,
            targetPointer);
        if (!resolved.IsValid)
        {
            layout.ShapeKnown = false;
            AddChannelIssue(
                result,
                layout,
                resolved.IssueCode,
                GltfValidationSeverity.Error,
                resolved.Message,
                pointer + "/target/extensions/KHR_animation_pointer/pointer");
            return;
        }

        layout.Pointer = targetPointer;
        layout.PointerValueType = resolved.ValueType;
        layout.ValueComponentCount = resolved.ComponentCount;
        layout.Quaternion = resolved.Quaternion;
        ValidateDuplicatePointerTarget(animation, result, layout, pointer);
    }

    internal void ValidateDuplicatePointerTarget(
        GltfAnimation animation,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string pointer)
    {
        int previousIndex = 0;
        while (previousIndex < layout.Channel)
        {
            GltfAnimationChannel previous = animation.Channels[previousIndex];
            if (previous != null && previous.Target != null)
            {
                string? previousPointer = PointerResolver.GetTargetPointer(previous.Target);
                if (previousPointer == null && previous.Target.HasNode)
                {
                    previousPointer = GetCoreNodePointer(previous.Target);
                }

                if (previousPointer != null
                    && PointersOverlap(layout.Pointer, previousPointer))
                {
                    AddChannelIssue(
                        result,
                        layout,
                        GltfIssueCodes.AnimationDuplicateTargets,
                        GltfValidationSeverity.Error,
                        "An animation may target each property only once.",
                        pointer + "/target");
                    return;
                }
            }

            previousIndex++;
        }
    }

    internal GltfNode? ResolveTargetNode(
        GltfDocument document,
        GltfAnimationTarget target,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string pointer)
    {
        if (!target.HasNode
            || document.Nodes == null
            || target.Node < 0
            || target.Node >= document.Nodes.Count
            || document.Nodes[target.Node] == null)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                "The animation channel target node does not resolve to a node.",
                $"{pointer}/target/node");
            return null;
        }

        return document.Nodes[target.Node];
    }

    internal void ValidateDuplicateTarget(
        GltfAnimation animation,
        GltfAnimationTarget target,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string pointer)
    {
        int previousIndex = 0;
        while (previousIndex < layout.Channel)
        {
            GltfAnimationChannel previous = animation.Channels[previousIndex];
            if (previous != null
                && previous.Target != null
                && (previous.Target.HasNode
                    && previous.Target.Node == target.Node
                    && previous.Target.Path == target.Path
                    || PointersOverlap(
                        GetCoreNodePointer(target),
                        PointerResolver.GetTargetPointer(previous.Target))))
            {
                AddChannelIssue(
                    result,
                    layout,
                    GltfIssueCodes.AnimationDuplicateTargets,
                    GltfValidationSeverity.Error,
                    "An animation may target each node and path combination only once.",
                    $"{pointer}/target");
                return;
            }

            previousIndex++;
        }
    }

    internal void ValidateTargetNode(
        GltfNode node,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string pointer)
    {
        if (layout.Path != GltfAnimationPath.Weights && node.HasMatrix)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.AnimationChannelTargetNodeMatrix,
                GltfValidationSeverity.Error,
                "A node with a matrix transform cannot be targeted by a TRS animation channel.",
                $"{pointer}/target/node");
        }

        if (layout.Path != GltfAnimationPath.Weights && node.HasSkin)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.AnimationChannelTargetNodeSkin,
                GltfValidationSeverity.Warning,
                "The animation channel targets a node that has a skin.",
                $"{pointer}/target/node");
        }
    }

    internal void ConfigureTargetShape(
        GltfDocument document,
        GltfNode node,
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string pointer)
    {
        switch (layout.Path)
        {
            case GltfAnimationPath.Translation:
            case GltfAnimationPath.Scale:
                layout.ValueComponentCount = 3;
                break;
            case GltfAnimationPath.Rotation:
                layout.ValueComponentCount = 4;
                break;
            case GltfAnimationPath.Weights:
                layout.MorphTargetCount = FindMorphTargetCount(document, node);
                layout.ValueComponentCount = layout.MorphTargetCount;
                if (layout.MorphTargetCount < 1)
                {
                    AddChannelIssue(
                        result,
                        layout,
                        GltfIssueCodes.AnimationChannelTargetNodeWeightsNoMorphs,
                        GltfValidationSeverity.Error,
                        "A weights animation target node must instantiate a mesh with morph targets.",
                        $"{pointer}/target/node");
                }

                break;
            default:
                layout.ShapeKnown = false;
                break;
        }
    }

    internal int FindMorphTargetCount(GltfDocument document, GltfNode node)
    {
        if (!node.HasMesh
            || document.Meshes == null
            || node.Mesh < 0
            || node.Mesh >= document.Meshes.Count
            || document.Meshes[node.Mesh] == null)
        {
            return 0;
        }

        GltfMesh mesh = document.Meshes[node.Mesh];
        if (mesh.Primitives == null)
        {
            return 0;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive != null && primitive.Targets != null)
            {
                return primitive.Targets.Count;
            }

            primitiveIndex++;
        }

        return 0;
    }

    internal void ValidateOutputLayout(
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        AnimationSamplerLayout sampler)
    {
        if (sampler.Output == null || !layout.ShapeKnown)
        {
            return;
        }

        GltfAccessor accessor = sampler.Output;
        GltfAccessorType expectedType = GltfAccessorType.Scalar;
        if (layout.Path == GltfAnimationPath.Pointer)
        {
            if (layout.PointerValueType == AnimationPointerValueType.Float2)
            {
                expectedType = GltfAccessorType.Vector2;
            }
            else if (layout.PointerValueType == AnimationPointerValueType.Float3)
            {
                expectedType = GltfAccessorType.Vector3;
            }
            else if (layout.PointerValueType == AnimationPointerValueType.Float4)
            {
                expectedType = GltfAccessorType.Vector4;
            }
        }
        else if (layout.Path == GltfAnimationPath.Translation
            || layout.Path == GltfAnimationPath.Scale)
        {
            expectedType = GltfAccessorType.Vector3;
        }
        else if (layout.Path == GltfAnimationPath.Rotation)
        {
            expectedType = GltfAccessorType.Vector4;
        }

        if (accessor.Type != expectedType || !IsAllowedOutputComponent(accessor, layout))
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "The animation output accessor format does not match the target path.",
                $"/accessors/{sampler.OutputAccessor}");
        }

        if (layout.Path == GltfAnimationPath.Pointer
            && (layout.PointerValueType == AnimationPointerValueType.Boolean
                || layout.PointerValueType == AnimationPointerValueType.Integer)
            && sampler.Interpolation != GltfAnimationInterpolation.Step)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerOutputAccessorInvalidFormat,
                GltfValidationSeverity.Error,
                "Boolean and integer animation pointers require STEP interpolation.",
                $"/animations/{result.Animation}/samplers/{layout.Sampler}/interpolation");
        }

        long factor = layout.CubicSpline ? 3L : 1L;
        long expectedCount = (long)sampler.KeyframeCount * factor;
        if (layout.Path == GltfAnimationPath.Weights
            || layout.Path == GltfAnimationPath.Pointer
                && layout.PointerValueType == AnimationPointerValueType.FloatArray)
        {
            expectedCount *= layout.ValueComponentCount;
        }

        if (expectedCount <= int.MaxValue)
        {
            layout.ExpectedOutputElementCount = (int)expectedCount;
        }

        if (expectedCount > int.MaxValue || accessor.Count != expectedCount)
        {
            AddChannelIssue(
                result,
                layout,
                GltfIssueCodes.AnimationSamplerOutputAccessorInvalidCount,
                GltfValidationSeverity.Error,
                $"Animation output accessor count {accessor.Count} does not match expected count {expectedCount}.",
                $"/accessors/{sampler.OutputAccessor}/count");
        }

        BuildValueOffsets(layout, sampler.KeyframeCount);
        if ((layout.Path == GltfAnimationPath.Rotation || layout.Quaternion)
            && sampler.OutputReady
            && accessor.Type == GltfAccessorType.Vector4)
        {
            ValidateQuaternionValues(result, layout, sampler);
        }
    }

    internal bool IsAllowedOutputComponent(
        GltfAccessor accessor,
        AnimationChannelLayout layout)
    {
        if (layout.Path == GltfAnimationPath.Pointer)
        {
            if (layout.PointerValueType == AnimationPointerValueType.Boolean)
            {
                return accessor.ComponentType == GltfComponentType.UnsignedByte;
            }

            if (layout.PointerValueType == AnimationPointerValueType.Integer)
            {
                return !accessor.Normalized
                    && accessor.ComponentType != GltfComponentType.Float;
            }

            return accessor.ComponentType == GltfComponentType.Float
                    && !accessor.Normalized
                || accessor.ComponentType != GltfComponentType.Float;
        }

        if (accessor.ComponentType == GltfComponentType.Float)
        {
            return !accessor.Normalized;
        }

        if (layout.Path != GltfAnimationPath.Rotation
            && layout.Path != GltfAnimationPath.Weights)
        {
            return false;
        }

        bool allowedInteger = accessor.ComponentType == GltfComponentType.SignedByte
            || accessor.ComponentType == GltfComponentType.UnsignedByte
            || accessor.ComponentType == GltfComponentType.SignedShort
            || accessor.ComponentType == GltfComponentType.UnsignedShort;
        return allowedInteger && accessor.Normalized;
    }

    internal string? GetCoreNodePointer(GltfAnimationTarget target)
    {
        if (!target.HasNode)
        {
            return null;
        }

        string member = "";
        if (target.Path == GltfAnimationPath.Translation)
        {
            member = "translation";
        }
        else if (target.Path == GltfAnimationPath.Rotation)
        {
            member = "rotation";
        }
        else if (target.Path == GltfAnimationPath.Scale)
        {
            member = "scale";
        }
        else if (target.Path == GltfAnimationPath.Weights)
        {
            member = "weights";
        }

        if (member.Length == 0)
        {
            return null;
        }

        return "/nodes/" + target.Node + "/" + member;
    }

    internal bool PointersOverlap(string? first, string? second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        return string.Equals(first, second, StringComparison.Ordinal)
            || first.StartsWith(second + "/", StringComparison.Ordinal)
            || second.StartsWith(first + "/", StringComparison.Ordinal);
    }

    internal void BuildValueOffsets(AnimationChannelLayout layout, int keyframeCount)
    {
        if (layout.ValueComponentCount < 1 || keyframeCount < 1)
        {
            return;
        }

        int keyframeIndex = 0;
        while (keyframeIndex < keyframeCount)
        {
            if (layout.CubicSpline)
            {
                int tripletOffset = keyframeIndex * layout.ValueComponentCount * 3;
                layout.InTangentOffsets.Add(tripletOffset);
                layout.ValueOffsets.Add(tripletOffset + layout.ValueComponentCount);
                layout.OutTangentOffsets.Add(tripletOffset + layout.ValueComponentCount * 2);
            }
            else
            {
                layout.ValueOffsets.Add(keyframeIndex * layout.ValueComponentCount);
            }

            keyframeIndex++;
        }
    }

    internal void ValidateQuaternionValues(
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        AnimationSamplerLayout sampler)
    {
        int keyframeIndex = 0;
        while (keyframeIndex < layout.ValueOffsets.Count)
        {
            int offset = layout.ValueOffsets[keyframeIndex];
            if (offset >= 0 && offset + 3 < sampler.OutputValues.Length)
            {
                double x = sampler.OutputValues[offset];
                double y = sampler.OutputValues[offset + 1];
                double z = sampler.OutputValues[offset + 2];
                double w = sampler.OutputValues[offset + 3];
                double squaredLength = x * x + y * y + z * z + w * w;
                if (!double.IsFinite(squaredLength)
                    || Math.Abs(squaredLength - 1.0) > 0.00001)
                {
                    AddChannelIssue(
                        result,
                        layout,
                        GltfIssueCodes.AccessorAnimationSamplerOutputNonNormalizedQuaternion,
                        GltfValidationSeverity.Error,
                        $"Animation rotation keyframe {keyframeIndex} is not a unit quaternion.",
                        $"/accessors/{sampler.OutputAccessor}/{offset}");
                }
            }

            keyframeIndex++;
        }
    }

    internal void CopySamplerReadFailure(
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        GltfAccessorData data,
        string pointer)
    {
        GltfValidationIssue issue = data.Diagnostic ?? new GltfValidationIssue();
        if (data.Diagnostic == null)
        {
            issue.Code = GltfIssueCodes.OperationUnavailable;
            issue.Severity = GltfValidationSeverity.Error;
            issue.Message = "Animation accessor data could not be decoded.";
            issue.JsonPointer = pointer;
        }

        layout.Diagnostics.Add(issue);
        result.Diagnostics.Add(issue);
        layout.IsValid = false;
        if (layout.Status == GltfOperationStatus.Success
            || data.Status == GltfOperationStatus.Invalid
            || data.Status == GltfOperationStatus.Failed)
        {
            layout.Status = data.Status;
        }

        result.IsValid = false;
        if (data.Status == GltfOperationStatus.Invalid
            || data.Status == GltfOperationStatus.Failed)
        {
            result.Status = data.Status;
        }
        else if (result.Status == GltfOperationStatus.Success)
        {
            result.Status = data.Status;
        }
    }

    internal void AddAnalysisIssue(
        AnimationLayoutAnalysis result,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = CreateIssue(code, severity, message, pointer);
        result.Diagnostics.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            result.IsValid = false;
            result.Status = GltfOperationStatus.Invalid;
        }
    }

    internal void AddSamplerIssue(
        AnimationLayoutAnalysis result,
        AnimationSamplerLayout layout,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = CreateIssue(code, severity, message, pointer);
        layout.Diagnostics.Add(issue);
        result.Diagnostics.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            layout.IsValid = false;
            layout.Status = GltfOperationStatus.Invalid;
            result.IsValid = false;
            result.Status = GltfOperationStatus.Invalid;
        }
    }

    internal void AddChannelIssue(
        AnimationLayoutAnalysis result,
        AnimationChannelLayout layout,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = CreateIssue(code, severity, message, pointer);
        layout.Diagnostics.Add(issue);
        result.Diagnostics.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            layout.IsValid = false;
            layout.Status = GltfOperationStatus.Invalid;
            result.IsValid = false;
            result.Status = GltfOperationStatus.Invalid;
        }
    }

    internal GltfValidationIssue CreateIssue(
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        return issue;
    }
}
