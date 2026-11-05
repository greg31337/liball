using System;
using System.Collections.Generic;

namespace gltf;

internal class AnimationSampler
{
    internal AnimationLayoutAnalyzer LayoutAnalyzer = new AnimationLayoutAnalyzer();
    internal SceneEvaluator SceneEvaluator = new SceneEvaluator();

    internal GltfAnimationPose SampleDocumentAnimation(
        GltfDocument document,
        GltfAnimationEvaluationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        GltfAnimationPose result = new GltfAnimationPose();
        result.Animation = options.Animation;
        if (!double.IsFinite(options.Time))
        {
            AddPoseIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Animation sample time must be finite.",
                "/time");
            return result;
        }

        if (options.WrapMode != GltfAnimationWrapMode.Clamp
            && options.WrapMode != GltfAnimationWrapMode.Loop)
        {
            AddPoseIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                $"Animation wrap mode value {(int)options.WrapMode} is not supported.",
                "/wrapMode");
            return result;
        }

        AnimationLayoutAnalysis analysis = LayoutAnalyzer.AnalyzeAnimation(
            document,
            options.Animation);
        CopyAnalysisDiagnostics(result, analysis);
        if (!analysis.IsValid || analysis.Status != GltfOperationStatus.Success)
        {
            result.Status = analysis.Status;
            return result;
        }

        result.Time = MapAnimationTime(options.Time, analysis.Duration, options.WrapMode);
        int channelIndex = 0;
        while (channelIndex < analysis.Channels.Count)
        {
            AnimationChannelLayout channel = analysis.Channels[channelIndex];
            if (channel.ShapeKnown
                && channel.IsValid
                && channel.Sampler >= 0
                && channel.Sampler < analysis.Samplers.Count)
            {
                AnimationSamplerLayout sampler = analysis.Samplers[channel.Sampler];
                double[] values = SampleChannelValues(
                    channel,
                    sampler,
                    result.Time,
                    options.NormalizeRotations);
                StoreChannelValues(result, channel, values);
            }

            channelIndex++;
        }

        return result;
    }

    internal double CalculateDocumentAnimationDuration(
        GltfDocument document,
        int animationIndex)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        AnimationLayoutAnalysis analysis = LayoutAnalyzer.AnalyzeAnimation(document, animationIndex);
        if (!analysis.IsValid || analysis.Status != GltfOperationStatus.Success)
        {
            string message = "The animation layout is invalid.";
            if (analysis.Diagnostics.Count > 0)
            {
                message = analysis.Diagnostics[0].Message;
            }

            throw new GltfValidationException(message);
        }

        if (!analysis.HasDuration)
        {
            return 0.0;
        }

        return analysis.Duration;
    }

    internal double MapAnimationTime(
        double time,
        double duration,
        GltfAnimationWrapMode wrapMode)
    {
        if (duration <= 0.0)
        {
            return 0.0;
        }

        if (wrapMode == GltfAnimationWrapMode.Loop)
        {
            double mapped = time % duration;
            if (mapped < 0.0)
            {
                mapped += duration;
            }

            return mapped;
        }

        if (time < 0.0)
        {
            return 0.0;
        }

        if (time > duration)
        {
            return duration;
        }

        return time;
    }

    internal double[] SampleChannelValues(
        AnimationChannelLayout channel,
        AnimationSamplerLayout sampler,
        double time,
        bool normalizeRotation)
    {
        int componentCount = channel.ValueComponentCount;
        double[] result = new double[componentCount];
        if (componentCount < 1
            || sampler.KeyTimes.Length < 1
            || channel.ValueOffsets.Count < 1)
        {
            return result;
        }

        int firstKey = FindKeyframeIndex(sampler.KeyTimes, time);
        int finalKey = firstKey;
        if (firstKey + 1 < sampler.KeyTimes.Length
            && time > sampler.KeyTimes[firstKey])
        {
            finalKey = firstKey + 1;
        }

        if (finalKey == firstKey
            || sampler.Interpolation == GltfAnimationInterpolation.Step)
        {
            CopyKeyframeValue(channel, sampler, firstKey, result);
        }
        else if (sampler.Interpolation == GltfAnimationInterpolation.CubicSpline)
        {
            SampleCubicValues(channel, sampler, firstKey, finalKey, time, result);
        }
        else if (channel.Path == GltfAnimationPath.Rotation || channel.Quaternion)
        {
            SampleQuaternionValues(channel, sampler, firstKey, finalKey, time, result);
        }
        else
        {
            SampleLinearValues(channel, sampler, firstKey, finalKey, time, result);
        }

        if ((channel.Path == GltfAnimationPath.Rotation || channel.Quaternion)
            && normalizeRotation)
        {
            NormalizeQuaternion(result);
        }

        return result;
    }

    internal int FindKeyframeIndex(double[] keyTimes, double time)
    {
        if (time <= keyTimes[0])
        {
            return 0;
        }

        int finalIndex = keyTimes.Length - 1;
        if (time >= keyTimes[finalIndex])
        {
            return finalIndex;
        }

        int keyIndex = 0;
        while (keyIndex + 1 < keyTimes.Length
            && time >= keyTimes[keyIndex + 1])
        {
            keyIndex++;
        }

        return keyIndex;
    }

    internal void CopyKeyframeValue(
        AnimationChannelLayout channel,
        AnimationSamplerLayout sampler,
        int keyframeIndex,
        double[] result)
    {
        int offset = channel.ValueOffsets[keyframeIndex];
        int componentIndex = 0;
        while (componentIndex < result.Length)
        {
            result[componentIndex] = sampler.OutputValues[offset + componentIndex];
            componentIndex++;
        }
    }

    internal void SampleLinearValues(
        AnimationChannelLayout channel,
        AnimationSamplerLayout sampler,
        int firstKey,
        int finalKey,
        double time,
        double[] result)
    {
        double amount = CalculateInterpolationAmount(sampler, firstKey, finalKey, time);
        int firstOffset = channel.ValueOffsets[firstKey];
        int finalOffset = channel.ValueOffsets[finalKey];
        int componentIndex = 0;
        while (componentIndex < result.Length)
        {
            double first = sampler.OutputValues[firstOffset + componentIndex];
            double final = sampler.OutputValues[finalOffset + componentIndex];
            result[componentIndex] = first + (final - first) * amount;
            componentIndex++;
        }
    }

    internal void SampleQuaternionValues(
        AnimationChannelLayout channel,
        AnimationSamplerLayout sampler,
        int firstKey,
        int finalKey,
        double time,
        double[] result)
    {
        int firstOffset = channel.ValueOffsets[firstKey];
        int finalOffset = channel.ValueOffsets[finalKey];
        double dot = 0.0;
        int componentIndex = 0;
        while (componentIndex < 4)
        {
            dot += sampler.OutputValues[firstOffset + componentIndex]
                * sampler.OutputValues[finalOffset + componentIndex];
            componentIndex++;
        }

        double finalSign = 1.0;
        if (dot < 0.0)
        {
            dot = -dot;
            finalSign = -1.0;
        }

        if (dot > 1.0)
        {
            dot = 1.0;
        }

        double amount = CalculateInterpolationAmount(sampler, firstKey, finalKey, time);
        double firstScale = 1.0 - amount;
        double finalScale = amount;
        if (dot < 0.9995)
        {
            double angle = Math.Acos(dot);
            double denominator = Math.Sin(angle);
            firstScale = Math.Sin((1.0 - amount) * angle) / denominator;
            finalScale = Math.Sin(amount * angle) / denominator;
        }

        componentIndex = 0;
        while (componentIndex < 4)
        {
            result[componentIndex] = firstScale
                    * sampler.OutputValues[firstOffset + componentIndex]
                + finalScale * finalSign
                    * sampler.OutputValues[finalOffset + componentIndex];
            componentIndex++;
        }
    }

    internal void SampleCubicValues(
        AnimationChannelLayout channel,
        AnimationSamplerLayout sampler,
        int firstKey,
        int finalKey,
        double time,
        double[] result)
    {
        double amount = CalculateInterpolationAmount(sampler, firstKey, finalKey, time);
        double amountSquared = amount * amount;
        double amountCubed = amountSquared * amount;
        double duration = sampler.KeyTimes[finalKey] - sampler.KeyTimes[firstKey];
        double firstValueScale = 2.0 * amountCubed - 3.0 * amountSquared + 1.0;
        double firstTangentScale = duration
            * (amountCubed - 2.0 * amountSquared + amount);
        double finalValueScale = -2.0 * amountCubed + 3.0 * amountSquared;
        double finalTangentScale = duration * (amountCubed - amountSquared);
        int firstValueOffset = channel.ValueOffsets[firstKey];
        int firstTangentOffset = channel.OutTangentOffsets[firstKey];
        int finalValueOffset = channel.ValueOffsets[finalKey];
        int finalTangentOffset = channel.InTangentOffsets[finalKey];
        int componentIndex = 0;
        while (componentIndex < result.Length)
        {
            result[componentIndex] = firstValueScale
                    * sampler.OutputValues[firstValueOffset + componentIndex]
                + firstTangentScale
                    * sampler.OutputValues[firstTangentOffset + componentIndex]
                + finalValueScale
                    * sampler.OutputValues[finalValueOffset + componentIndex]
                + finalTangentScale
                    * sampler.OutputValues[finalTangentOffset + componentIndex];
            componentIndex++;
        }
    }

    internal double CalculateInterpolationAmount(
        AnimationSamplerLayout sampler,
        int firstKey,
        int finalKey,
        double time)
    {
        double firstTime = sampler.KeyTimes[firstKey];
        double duration = sampler.KeyTimes[finalKey] - firstTime;
        return (time - firstTime) / duration;
    }

    internal void NormalizeQuaternion(double[] values)
    {
        double squaredLength = values[0] * values[0]
            + values[1] * values[1]
            + values[2] * values[2]
            + values[3] * values[3];
        if (squaredLength == 0.0 || !double.IsFinite(squaredLength))
        {
            return;
        }

        double divisor = Math.Sqrt(squaredLength);
        int componentIndex = 0;
        while (componentIndex < 4)
        {
            values[componentIndex] /= divisor;
            componentIndex++;
        }
    }

    internal void StoreChannelValues(
        GltfAnimationPose result,
        AnimationChannelLayout channel,
        double[] values)
    {
        switch (channel.Path)
        {
            case GltfAnimationPath.Translation:
                result.Translations[channel.Node] = values;
                break;
            case GltfAnimationPath.Rotation:
                result.Rotations[channel.Node] = values;
                break;
            case GltfAnimationPath.Scale:
                result.Scales[channel.Node] = values;
                break;
            case GltfAnimationPath.Weights:
                result.Weights[channel.Node] = values;
                break;
            case GltfAnimationPath.Pointer:
                result.PointerValues[channel.Pointer] = values;
                break;
            default:
                break;
        }
    }

    internal GltfScenePose ApplyDocumentAnimationPose(
        GltfDocument document,
        GltfAnimationPose pose,
        GltfSceneEvaluationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pose == null)
        {
            throw new ArgumentNullException("pose");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        GltfScenePose result = new GltfScenePose();
        result.Status = pose.Status;
        CopyPoseDiagnostics(result, pose);
        if (pose.Status != GltfOperationStatus.Success)
        {
            return result;
        }

        if (!ValidateApplicationNodes(document, result))
        {
            return result;
        }

        GltfDocument clone = CloneDocumentNodes(document);
        ApplyPointerValues(clone, pose, result);
        ApplyTranslationValues(clone, pose, result);
        ApplyRotationValues(clone, pose, result);
        ApplyScaleValues(clone, pose, result);
        ApplyWeightValues(clone, pose, result);
        if (result.Status != GltfOperationStatus.Success)
        {
            return result;
        }

        GltfScenePose evaluated = SceneEvaluator.EvaluateDocumentScene(clone, options);
        CopyPoseDiagnostics(evaluated, pose);
        return evaluated;
    }

    internal void ApplyPointerValues(
        GltfDocument document,
        GltfAnimationPose pose,
        GltfScenePose result)
    {
        foreach (KeyValuePair<string, double[]> entry in pose.PointerValues)
        {
            AnimationPointerTarget target = new AnimationPointerResolver().ResolvePointer(
                document,
                entry.Key);
            if (!target.IsValid || target.TargetCollection != "nodes"
                || target.Segments.Count < 3)
            {
                continue;
            }

            GltfNode node = document.Nodes[target.TargetIndex];
            string member = target.Segments[2];
            if (member == "translation")
            {
                ApplyPointerVector(node, entry.Value, 3, 0, result, entry.Key);
            }
            else if (member == "rotation")
            {
                ApplyPointerVector(node, entry.Value, 4, 1, result, entry.Key);
            }
            else if (member == "scale")
            {
                ApplyPointerVector(node, entry.Value, 3, 2, result, entry.Key);
            }
            else if (member == "weights")
            {
                ApplyPointerWeights(document, node, target, entry.Value, result, entry.Key);
            }
        }
    }

    internal void ApplyPointerVector(
        GltfNode node,
        double[] values,
        int expectedLength,
        int targetKind,
        GltfScenePose result,
        string pointer)
    {
        if (values == null || values.Length != expectedLength)
        {
            AddSceneIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                $"Pointer value must contain {expectedLength} components.",
                pointer);
            return;
        }

        double[] copy = new double[values.Length];
        Array.Copy(values, copy, values.Length);
        if (targetKind == 0)
        {
            node.HasTranslation = true;
            node.Translation = copy;
        }
        else if (targetKind == 1)
        {
            node.HasMatrix = false;
            node.HasRotation = true;
            node.Rotation = copy;
        }
        else
        {
            node.HasMatrix = false;
            node.HasScale = true;
            node.Scale = copy;
        }
    }

    internal void ApplyPointerWeights(
        GltfDocument document,
        GltfNode node,
        AnimationPointerTarget target,
        double[] values,
        GltfScenePose result,
        string pointer)
    {
        int count = new AnimationPointerResolver().FindMorphTargetCount(document, node);
        if (target.Segments.Count == 3)
        {
            if (values == null || values.Length != count)
            {
                AddSceneIssue(
                    result,
                    GltfIssueCodes.ArrayLengthNotInList,
                    $"Pointer weight value must contain {count} components.",
                    pointer);
                return;
            }

            node.Weights = new double[count];
            Array.Copy(values, node.Weights, count);
            return;
        }

        if (values == null || values.Length != 1)
        {
            AddSceneIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "Pointer weight component must contain one value.",
                pointer);
            return;
        }

        int weightIndex = new AnimationPointerResolver().ReadCollectionIndex(target, 3);
        if (node.Weights == null || node.Weights.Length != count)
        {
            node.Weights = CreateDefaultWeights(document, node, count);
        }

        node.Weights[weightIndex] = values[0];
    }

    internal double[] CreateDefaultWeights(GltfDocument document, GltfNode node, int count)
    {
        double[] result = new double[count];
        if (!node.HasMesh || node.Mesh < 0 || node.Mesh >= document.Meshes.Count)
        {
            return result;
        }

        GltfMesh mesh = document.Meshes[node.Mesh];
        if (mesh.Weights != null && mesh.Weights.Length == count)
        {
            Array.Copy(mesh.Weights, result, count);
        }

        return result;
    }

    internal GltfDocument CloneDocumentNodes(GltfDocument document)
    {
        GltfDocument result = new GltfDocument();
        result.Asset = document.Asset;
        result.Accessors = document.Accessors;
        result.Animations = document.Animations;
        result.Buffers = document.Buffers;
        result.BufferViews = document.BufferViews;
        result.Cameras = document.Cameras;
        result.Images = document.Images;
        result.Materials = document.Materials;
        result.Meshes = document.Meshes;
        result.Samplers = document.Samplers;
        result.Scenes = document.Scenes;
        result.Skins = document.Skins;
        result.Textures = document.Textures;
        result.HasDefaultScene = document.HasDefaultScene;
        result.DefaultScene = document.DefaultScene;
        result.ExtensionsUsed = document.ExtensionsUsed;
        result.ExtensionsRequired = document.ExtensionsRequired;
        result.PropertyJson = document.PropertyJson;
        result.Resources = document.Resources;
        result.Container = document.Container;
        result.Nodes.Clear();
        if (document.Nodes == null)
        {
            return result;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode source = document.Nodes[nodeIndex];
            if (source == null)
            {
                result.Nodes.Add(new GltfNode());
            }
            else
            {
                result.Nodes.Add(CloneNode(source));
            }

            nodeIndex++;
        }

        return result;
    }

    internal bool ValidateApplicationNodes(GltfDocument document, GltfScenePose result)
    {
        if (document.Nodes == null)
        {
            AddSceneIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                "The node collection is missing.",
                "/nodes");
            return false;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            if (document.Nodes[nodeIndex] == null)
            {
                AddSceneIssue(
                    result,
                    GltfIssueCodes.ArrayTypeMismatch,
                    "The node is null.",
                    $"/nodes/{nodeIndex}");
                return false;
            }

            nodeIndex++;
        }

        return true;
    }

    internal GltfNode CloneNode(GltfNode source)
    {
        GltfNode result = new GltfNode();
        result.Children = new List<int>(source.Children);
        result.HasCamera = source.HasCamera;
        result.Camera = source.Camera;
        result.HasSkin = source.HasSkin;
        result.Skin = source.Skin;
        result.HasMesh = source.HasMesh;
        result.Mesh = source.Mesh;
        result.HasMatrix = source.HasMatrix;
        result.Matrix = CloneNumericArray(source.Matrix);
        result.HasRotation = source.HasRotation;
        result.Rotation = CloneNumericArray(source.Rotation);
        result.HasScale = source.HasScale;
        result.Scale = CloneNumericArray(source.Scale);
        result.HasTranslation = source.HasTranslation;
        result.Translation = CloneNumericArray(source.Translation);
        if (source.Weights != null)
        {
            result.Weights = CloneNumericArray(source.Weights);
        }

        result.Name = source.Name;
        result.PropertyJson = source.PropertyJson;
        return result;
    }

    internal double[] CloneNumericArray(double[] source)
    {
        double[] result = new double[source.Length];
        source.CopyTo(result, 0);
        return result;
    }

    internal void ApplyTranslationValues(
        GltfDocument document,
        GltfAnimationPose pose,
        GltfScenePose result)
    {
        foreach (KeyValuePair<int, double[]> entry in pose.Translations)
        {
            if (ValidatePoseTarget(document, entry.Key, entry.Value, 3, result, "translation"))
            {
                GltfNode node = document.Nodes[entry.Key];
                node.HasTranslation = true;
                node.Translation = CloneNumericArray(entry.Value);
            }
        }
    }

    internal void ApplyRotationValues(
        GltfDocument document,
        GltfAnimationPose pose,
        GltfScenePose result)
    {
        foreach (KeyValuePair<int, double[]> entry in pose.Rotations)
        {
            if (ValidatePoseTarget(document, entry.Key, entry.Value, 4, result, "rotation"))
            {
                GltfNode node = document.Nodes[entry.Key];
                node.HasRotation = true;
                node.Rotation = CloneNumericArray(entry.Value);
            }
        }
    }

    internal void ApplyScaleValues(
        GltfDocument document,
        GltfAnimationPose pose,
        GltfScenePose result)
    {
        foreach (KeyValuePair<int, double[]> entry in pose.Scales)
        {
            if (ValidatePoseTarget(document, entry.Key, entry.Value, 3, result, "scale"))
            {
                GltfNode node = document.Nodes[entry.Key];
                node.HasScale = true;
                node.Scale = CloneNumericArray(entry.Value);
            }
        }
    }

    internal void ApplyWeightValues(
        GltfDocument document,
        GltfAnimationPose pose,
        GltfScenePose result)
    {
        foreach (KeyValuePair<int, double[]> entry in pose.Weights)
        {
            int expectedLength = FindPoseMorphTargetCount(document, entry.Key);
            if (expectedLength < 1)
            {
                AddSceneIssue(
                    result,
                    GltfIssueCodes.AnimationChannelTargetNodeWeightsNoMorphs,
                    "An animation weights pose requires a node mesh with morph targets.",
                    $"/nodes/{entry.Key}/weights");
            }
            else if (ValidatePoseTarget(
                document,
                entry.Key,
                entry.Value,
                expectedLength,
                result,
                "weights"))
            {
                document.Nodes[entry.Key].Weights = CloneNumericArray(entry.Value);
            }
        }
    }

    internal int FindPoseMorphTargetCount(GltfDocument document, int nodeIndex)
    {
        if (document.Nodes == null
            || nodeIndex < 0
            || nodeIndex >= document.Nodes.Count
            || document.Nodes[nodeIndex] == null)
        {
            return 0;
        }

        return LayoutAnalyzer.FindMorphTargetCount(document, document.Nodes[nodeIndex]);
    }

    internal bool ValidatePoseTarget(
        GltfDocument document,
        int nodeIndex,
        double[] values,
        int expectedLength,
        GltfScenePose result,
        string member)
    {
        if (document.Nodes == null
            || nodeIndex < 0
            || nodeIndex >= document.Nodes.Count
            || document.Nodes[nodeIndex] == null)
        {
            AddSceneIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                $"Animation pose node index {nodeIndex} does not resolve to a node.",
                $"/nodes/{nodeIndex}");
            return false;
        }

        if (values == null || values.Length != expectedLength || expectedLength < 1)
        {
            AddSceneIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                $"Animation pose {member} values have an invalid length.",
                $"/nodes/{nodeIndex}/{member}");
            return false;
        }

        if (member != "weights" && document.Nodes[nodeIndex].HasMatrix)
        {
            AddSceneIssue(
                result,
                GltfIssueCodes.AnimationChannelTargetNodeMatrix,
                "A TRS animation pose cannot be applied to a node with a matrix transform.",
                $"/nodes/{nodeIndex}");
            return false;
        }

        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            if (!double.IsFinite(values[valueIndex]))
            {
                AddSceneIssue(
                    result,
                    GltfIssueCodes.AccessorInvalidFloat,
                    $"Animation pose {member} values must be finite.",
                    $"/nodes/{nodeIndex}/{member}/{valueIndex}");
                return false;
            }

            valueIndex++;
        }

        return true;
    }

    internal void CopyAnalysisDiagnostics(
        GltfAnimationPose result,
        AnimationLayoutAnalysis analysis)
    {
        int issueIndex = 0;
        while (issueIndex < analysis.Diagnostics.Count)
        {
            result.Diagnostics.Add(analysis.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void CopyPoseDiagnostics(GltfScenePose result, GltfAnimationPose pose)
    {
        int issueIndex = 0;
        while (issueIndex < pose.Diagnostics.Count)
        {
            result.Diagnostics.Add(pose.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void AddPoseIssue(
        GltfAnimationPose result,
        string code,
        string message,
        string pointer)
    {
        result.Status = GltfOperationStatus.Invalid;
        result.Diagnostics.Add(CreateAnimationIssue(code, message, pointer));
    }

    internal void AddSceneIssue(
        GltfScenePose result,
        string code,
        string message,
        string pointer)
    {
        result.Status = GltfOperationStatus.Invalid;
        result.Diagnostics.Add(CreateAnimationIssue(code, message, pointer));
    }

    internal GltfValidationIssue CreateAnimationIssue(
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
}
