using System;
using System.Collections.Generic;

namespace gltf;

internal class DocumentAnimationCleaner
{
    internal AnimationCleanupSettingsReader Settings =
        new AnimationCleanupSettingsReader();
    internal AnimationLayoutAnalyzer Analyzer = new AnimationLayoutAnalyzer();
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal DocumentCloner Cloner = new DocumentCloner();
    internal DocumentCompactor Mapping = new DocumentCompactor();
    internal DocumentVertexTransformer Storage = new DocumentVertexTransformer();

    internal TransformStepExecution RemoveRedundantKeys(
        GltfDocument document,
        GltfTransformStep step)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        AnimationCleanupSettings settings =
            Settings.ReadAnimationCleanupSettings(step);
        bool changed = false;
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            AnimationLayoutAnalysis analysis = Analyzer.AnalyzeAnimation(
                document,
                animationIndex);
            if (analysis.IsValid
                && IsAnimationCompatible(document.Animations[animationIndex])
                && CleanAnimation(
                    document,
                    document.Animations[animationIndex],
                    analysis,
                    settings,
                    execution.Result))
            {
                changed = true;
            }

            animationIndex++;
        }

        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
    }

    internal bool IsAnimationCompatible(GltfAnimation animation)
    {
        if (animation.PropertyJson.Extensions.Count > 0
            || animation.PropertyJson.TypedExtensions.Count > 0)
        {
            return false;
        }

        int channelIndex = 0;
        while (channelIndex < animation.Channels.Count)
        {
            GltfAnimationChannel channel = animation.Channels[channelIndex];
            if (channel.PropertyJson.Extensions.Count > 0
                || channel.PropertyJson.TypedExtensions.Count > 0
                || !HasOnlyAnimationPointerExtension(channel.Target.PropertyJson))
            {
                return false;
            }

            channelIndex++;
        }

        return true;
    }

    internal bool HasOnlyAnimationPointerExtension(GltfPropertyJson property)
    {
        foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> extension
            in property.Extensions)
        {
            if (extension.Key != GltfExtensionNames.KhrAnimationPointer)
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, GltfTypedExtensionData> extension
            in property.TypedExtensions)
        {
            if (extension.Key != GltfExtensionNames.KhrAnimationPointer)
            {
                return false;
            }
        }

        return true;
    }

    internal bool CleanAnimation(
        GltfDocument document,
        GltfAnimation animation,
        AnimationLayoutAnalysis analysis,
        AnimationCleanupSettings settings,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int samplerIndex = 0;
        while (samplerIndex < animation.Samplers.Count
            && samplerIndex < analysis.Samplers.Count)
        {
            GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
            AnimationSamplerLayout samplerLayout = analysis.Samplers[samplerIndex];
            AnimationChannelLayout? channel = FindCompatibleChannel(
                analysis,
                samplerIndex);
            if (channel != null
                && samplerLayout.IsValid
                && samplerLayout.InputReady
                && samplerLayout.OutputReady
                && samplerLayout.KeyframeCount > 2
                && CanRewriteSampler(document, sampler))
            {
                List<int> kept = SelectKeptKeys(
                    samplerLayout,
                    channel,
                    settings);
                if (kept.Count < samplerLayout.KeyframeCount
                    && RewriteSampler(document, sampler, samplerLayout, kept))
                {
                    result.ChangedObjects.Add(
                        $"animations/{analysis.Animation}/samplers/{samplerIndex}");
                    changed = true;
                }
            }

            samplerIndex++;
        }

        return changed;
    }

    internal AnimationChannelLayout? FindCompatibleChannel(
        AnimationLayoutAnalysis analysis,
        int samplerIndex)
    {
        AnimationChannelLayout? selected = null;
        int channelIndex = 0;
        while (channelIndex < analysis.Channels.Count)
        {
            AnimationChannelLayout channel = analysis.Channels[channelIndex];
            if (channel.Sampler == samplerIndex)
            {
                if (!channel.IsValid
                    || !channel.ShapeKnown
                    || channel.ValueComponentCount < 1
                    || channel.ValueOffsets.Count
                        != analysis.Samplers[samplerIndex].KeyframeCount)
                {
                    return null;
                }

                if (selected == null)
                {
                    selected = channel;
                }
                else if (selected.ValueComponentCount != channel.ValueComponentCount
                    || selected.CubicSpline != channel.CubicSpline
                    || IsQuaternionChannel(selected) != IsQuaternionChannel(channel))
                {
                    return null;
                }
            }

            channelIndex++;
        }

        return selected;
    }

    internal bool IsQuaternionChannel(AnimationChannelLayout channel)
    {
        return channel.Path == GltfAnimationPath.Rotation || channel.Quaternion;
    }

    internal bool CanRewriteSampler(
        GltfDocument document,
        GltfAnimationSampler sampler)
    {
        if (sampler.PropertyJson.Extensions.Count > 0
            || sampler.PropertyJson.TypedExtensions.Count > 0
            || sampler.Input < 0
            || sampler.Input >= document.Accessors.Count
            || sampler.Output < 0
            || sampler.Output >= document.Accessors.Count)
        {
            return false;
        }

        GltfAccessor input = document.Accessors[sampler.Input];
        GltfAccessor output = document.Accessors[sampler.Output];
        return output.ComponentType != GltfComponentType.UnsignedInt
            && Storage.IsAccessorStorageCompatible(document, input)
            && Storage.IsAccessorStorageCompatible(document, output);
    }

    internal List<int> SelectKeptKeys(
        AnimationSamplerLayout sampler,
        AnimationChannelLayout channel,
        AnimationCleanupSettings settings)
    {
        List<int> result = new List<int>();
        result.Add(0);
        if (sampler.Interpolation == GltfAnimationInterpolation.Step)
        {
            int previous = 0;
            int keyIndex = 1;
            while (keyIndex + 1 < sampler.KeyframeCount)
            {
                if (!KeyValuesMatch(
                    sampler,
                    channel,
                    previous,
                    keyIndex,
                    settings.ValueTolerance))
                {
                    result.Add(keyIndex);
                    previous = keyIndex;
                }

                keyIndex++;
            }

            result.Add(sampler.KeyframeCount - 1);
            return result;
        }

        int anchor = 0;
        int last = sampler.KeyframeCount - 1;
        while (anchor < last)
        {
            int candidate = last;
            while (candidate > anchor + 1
                && !SegmentMatches(
                    sampler,
                    channel,
                    anchor,
                    candidate,
                    settings))
            {
                candidate--;
            }

            result.Add(candidate);
            anchor = candidate;
        }

        return result;
    }

    internal bool KeyValuesMatch(
        AnimationSamplerLayout sampler,
        AnimationChannelLayout channel,
        int firstKey,
        int secondKey,
        double tolerance)
    {
        int firstOffset = channel.ValueOffsets[firstKey];
        int secondOffset = channel.ValueOffsets[secondKey];
        return ComponentsMatch(
            sampler.OutputValues,
            firstOffset,
            sampler.OutputValues,
            secondOffset,
            channel.ValueComponentCount,
            tolerance);
    }

    internal bool SegmentMatches(
        AnimationSamplerLayout sampler,
        AnimationChannelLayout channel,
        int firstKey,
        int finalKey,
        AnimationCleanupSettings settings)
    {
        int keyIndex = firstKey + 1;
        while (keyIndex < finalKey)
        {
            bool matches;
            if (sampler.Interpolation == GltfAnimationInterpolation.CubicSpline)
            {
                matches = CubicKeyMatches(
                    sampler,
                    channel,
                    firstKey,
                    finalKey,
                    keyIndex,
                    settings);
            }
            else if (IsQuaternionChannel(channel))
            {
                matches = QuaternionKeyMatches(
                    sampler,
                    channel,
                    firstKey,
                    finalKey,
                    keyIndex,
                    settings.ValueTolerance);
            }
            else
            {
                matches = LinearKeyMatches(
                    sampler,
                    channel,
                    firstKey,
                    finalKey,
                    keyIndex,
                    settings.ValueTolerance);
            }

            if (!matches)
            {
                return false;
            }

            keyIndex++;
        }

        return true;
    }

    internal bool LinearKeyMatches(
        AnimationSamplerLayout sampler,
        AnimationChannelLayout channel,
        int firstKey,
        int finalKey,
        int keyIndex,
        double tolerance)
    {
        double amount = CalculateAmount(sampler, firstKey, finalKey, keyIndex);
        int firstOffset = channel.ValueOffsets[firstKey];
        int finalOffset = channel.ValueOffsets[finalKey];
        int keyOffset = channel.ValueOffsets[keyIndex];
        int componentIndex = 0;
        while (componentIndex < channel.ValueComponentCount)
        {
            double expected = sampler.OutputValues[firstOffset + componentIndex]
                + (sampler.OutputValues[finalOffset + componentIndex]
                    - sampler.OutputValues[firstOffset + componentIndex])
                    * amount;
            if (Math.Abs(expected
                    - sampler.OutputValues[keyOffset + componentIndex]) > tolerance)
            {
                return false;
            }

            componentIndex++;
        }

        return true;
    }

    internal bool QuaternionKeyMatches(
        AnimationSamplerLayout sampler,
        AnimationChannelLayout channel,
        int firstKey,
        int finalKey,
        int keyIndex,
        double tolerance)
    {
        double amount = CalculateAmount(sampler, firstKey, finalKey, keyIndex);
        double[] expected = InterpolateQuaternion(
            sampler.OutputValues,
            channel.ValueOffsets[firstKey],
            channel.ValueOffsets[finalKey],
            amount);
        int keyOffset = channel.ValueOffsets[keyIndex];
        double directDifference = 0.0;
        double oppositeDifference = 0.0;
        int componentIndex = 0;
        while (componentIndex < 4)
        {
            double direct = Math.Abs(expected[componentIndex]
                - sampler.OutputValues[keyOffset + componentIndex]);
            double opposite = Math.Abs(expected[componentIndex]
                + sampler.OutputValues[keyOffset + componentIndex]);
            if (direct > directDifference)
            {
                directDifference = direct;
            }

            if (opposite > oppositeDifference)
            {
                oppositeDifference = opposite;
            }

            componentIndex++;
        }

        return directDifference <= tolerance || oppositeDifference <= tolerance;
    }

    internal double[] InterpolateQuaternion(
        double[] values,
        int firstOffset,
        int finalOffset,
        double amount)
    {
        double dot = 0.0;
        int componentIndex = 0;
        while (componentIndex < 4)
        {
            dot += values[firstOffset + componentIndex]
                * values[finalOffset + componentIndex];
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

        double firstScale = 1.0 - amount;
        double finalScale = amount;
        if (dot < 0.9995)
        {
            double angle = Math.Acos(dot);
            double denominator = Math.Sin(angle);
            firstScale = Math.Sin((1.0 - amount) * angle) / denominator;
            finalScale = Math.Sin(amount * angle) / denominator;
        }

        double[] result = new double[4];
        componentIndex = 0;
        while (componentIndex < 4)
        {
            result[componentIndex] = firstScale
                    * values[firstOffset + componentIndex]
                + finalScale * finalSign
                    * values[finalOffset + componentIndex];
            componentIndex++;
        }

        NormalizeQuaternionResult(result);

        return result;
    }

    internal void NormalizeQuaternionResult(double[] values)
    {
        double squaredLength = values[0] * values[0]
            + values[1] * values[1]
            + values[2] * values[2]
            + values[3] * values[3];
        if (!double.IsFinite(squaredLength) || squaredLength == 0.0)
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

    internal bool CubicKeyMatches(
        AnimationSamplerLayout sampler,
        AnimationChannelLayout channel,
        int firstKey,
        int finalKey,
        int keyIndex,
        AnimationCleanupSettings settings)
    {
        double duration = sampler.KeyTimes[finalKey]
            - sampler.KeyTimes[firstKey];
        double amount = CalculateAmount(sampler, firstKey, finalKey, keyIndex);
        double amountSquared = amount * amount;
        double amountCubed = amountSquared * amount;
        double firstValueScale = 2.0 * amountCubed
            - 3.0 * amountSquared
            + 1.0;
        double firstTangentScale = duration
            * (amountCubed - 2.0 * amountSquared + amount);
        double finalValueScale = -2.0 * amountCubed + 3.0 * amountSquared;
        double finalTangentScale = duration
            * (amountCubed - amountSquared);
        double firstValueDerivative = (6.0 * amountSquared - 6.0 * amount)
            / duration;
        double firstTangentDerivative = 3.0 * amountSquared
            - 4.0 * amount
            + 1.0;
        double finalValueDerivative = (-6.0 * amountSquared + 6.0 * amount)
            / duration;
        double finalTangentDerivative = 3.0 * amountSquared - 2.0 * amount;
        int firstValueOffset = channel.ValueOffsets[firstKey];
        int firstTangentOffset = channel.OutTangentOffsets[firstKey];
        int finalValueOffset = channel.ValueOffsets[finalKey];
        int finalTangentOffset = channel.InTangentOffsets[finalKey];
        int keyValueOffset = channel.ValueOffsets[keyIndex];
        int keyInOffset = channel.InTangentOffsets[keyIndex];
        int keyOutOffset = channel.OutTangentOffsets[keyIndex];
        int componentIndex = 0;
        while (componentIndex < channel.ValueComponentCount)
        {
            double firstValue = sampler.OutputValues[
                firstValueOffset + componentIndex];
            double finalValue = sampler.OutputValues[
                finalValueOffset + componentIndex];
            double firstTangent = sampler.OutputValues[
                firstTangentOffset + componentIndex];
            double finalTangent = sampler.OutputValues[
                finalTangentOffset + componentIndex];
            double value = firstValueScale * firstValue
                + firstTangentScale * firstTangent
                + finalValueScale * finalValue
                + finalTangentScale * finalTangent;
            double derivative = firstValueDerivative * firstValue
                + firstTangentDerivative * firstTangent
                + finalValueDerivative * finalValue
                + finalTangentDerivative * finalTangent;
            if (Math.Abs(value
                    - sampler.OutputValues[keyValueOffset + componentIndex])
                    > settings.ValueTolerance
                || Math.Abs(derivative
                    - sampler.OutputValues[keyInOffset + componentIndex])
                    > settings.TangentTolerance
                || Math.Abs(derivative
                    - sampler.OutputValues[keyOutOffset + componentIndex])
                    > settings.TangentTolerance)
            {
                return false;
            }

            componentIndex++;
        }

        return true;
    }

    internal double CalculateAmount(
        AnimationSamplerLayout sampler,
        int firstKey,
        int finalKey,
        int keyIndex)
    {
        return (sampler.KeyTimes[keyIndex] - sampler.KeyTimes[firstKey])
            / (sampler.KeyTimes[finalKey] - sampler.KeyTimes[firstKey]);
    }

    internal bool ComponentsMatch(
        double[] first,
        int firstOffset,
        double[] second,
        int secondOffset,
        int count,
        double tolerance)
    {
        int componentIndex = 0;
        while (componentIndex < count)
        {
            if (Math.Abs(first[firstOffset + componentIndex]
                    - second[secondOffset + componentIndex]) > tolerance)
            {
                return false;
            }

            componentIndex++;
        }

        return true;
    }

    internal bool RewriteSampler(
        GltfDocument document,
        GltfAnimationSampler sampler,
        AnimationSamplerLayout layout,
        List<int> kept)
    {
        GltfAccessor input = document.Accessors[sampler.Input];
        GltfAccessor output = document.Accessors[sampler.Output];
        byte[] inputBytes = ReadRawAccessor(document, sampler.Input);
        byte[] outputBytes = ReadRawAccessor(document, sampler.Output);
        if (inputBytes.Length == 0
            || outputBytes.Length == 0
            || inputBytes.Length % input.Count != 0
            || outputBytes.Length % output.Count != 0)
        {
            return false;
        }

        int interpolationFactor = layout.Interpolation
            == GltfAnimationInterpolation.CubicSpline ? 3 : 1;
        int outputDivisor = layout.KeyframeCount * interpolationFactor;
        if (outputDivisor <= 0 || output.Count % outputDivisor != 0)
        {
            return false;
        }

        int inputElementLength = inputBytes.Length / input.Count;
        int outputElementLength = outputBytes.Length / output.Count;
        int outputElementsPerValue = output.Count / outputDivisor;
        byte[] selectedInput = SelectRawKeyGroups(
            inputBytes,
            inputElementLength,
            1,
            kept);
        byte[] selectedOutput = SelectRawKeyGroups(
            outputBytes,
            outputElementLength,
            outputElementsPerValue * interpolationFactor,
            kept);
        int createdInput = CreateRawAccessor(document, input, selectedInput);
        int createdOutput = CreateRawAccessor(document, output, selectedOutput);
        sampler.Input = createdInput;
        sampler.Output = createdOutput;
        return true;
    }

    internal byte[] ReadRawAccessor(GltfDocument document, int accessorIndex)
    {
        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData data = Accessors.ReadRawData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            return [];
        }

        return data.RawBytes;
    }

    internal byte[] SelectRawKeyGroups(
        byte[] source,
        int elementLength,
        int elementsPerKey,
        List<int> kept)
    {
        int groupLength = elementLength * elementsPerKey;
        byte[] result = new byte[groupLength * kept.Count];
        int keyIndex = 0;
        while (keyIndex < kept.Count)
        {
            int sourceOffset = kept[keyIndex] * groupLength;
            int destinationOffset = keyIndex * groupLength;
            int byteIndex = 0;
            while (byteIndex < groupLength)
            {
                result[destinationOffset + byteIndex] =
                    source[sourceOffset + byteIndex];
                byteIndex++;
            }

            keyIndex++;
        }

        return result;
    }

    internal int CreateRawAccessor(
        GltfDocument document,
        GltfAccessor source,
        byte[] rawBytes)
    {
        AccessorEncodingResult encoding = Encoder.EncodeRawAccessor(
            rawBytes,
            source.ComponentType,
            source.Type,
            source.Normalized,
            false);
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        GltfAccessor target = document.Accessors[created.Accessor];
        target.Name = source.Name;
        Cloner.Clones.Clear();
        Cloner.CloneJson = true;
        Cloner.CloneResourceBytes = true;
        target.PropertyJson = (GltfPropertyJson)(Cloner.CloneValue(source.PropertyJson)
            ?? new GltfPropertyJson());
        return created.Accessor;
    }
}
