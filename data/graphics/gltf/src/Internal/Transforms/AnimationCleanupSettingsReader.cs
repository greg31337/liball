using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class AnimationCleanupSettings
{
    internal double ValueTolerance;
    internal double TangentTolerance;
}

internal class AnimationCleanupSettingsReader
{
    internal AnimationCleanupSettings ReadAnimationCleanupSettings(
        GltfTransformStep step)
    {
        if (step.Settings == null)
        {
            throw new ArgumentException(
                "Animation cleanup settings must not be null.",
                "step");
        }

        AnimationCleanupSettings result = new AnimationCleanupSettings();
        if (step.Settings.ContainsKey("tolerance"))
        {
            double tolerance = ReadNonNegativeTolerance(
                step.Settings["tolerance"],
                "tolerance");
            result.ValueTolerance = tolerance;
            result.TangentTolerance = tolerance;
        }

        if (step.Settings.ContainsKey("valueTolerance"))
        {
            result.ValueTolerance = ReadNonNegativeTolerance(
                step.Settings["valueTolerance"],
                "valueTolerance");
        }

        if (step.Settings.ContainsKey("tangentTolerance"))
        {
            result.TangentTolerance = ReadNonNegativeTolerance(
                step.Settings["tangentTolerance"],
                "tangentTolerance");
        }

        return result;
    }

    internal double ReadNonNegativeTolerance(JsonNode? value, string name)
    {
        if (value == null || value.GetValueKind() != JsonValueKind.Number)
        {
            throw new ArgumentException(
                $"Animation cleanup setting '{name}' must be numeric.",
                "step");
        }

        double result = double.Parse(
            value.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(result) || result < 0.0)
        {
            throw new ArgumentException(
                $"Animation cleanup setting '{name}' must be finite and non-negative.",
                "step");
        }

        return result;
    }
}
