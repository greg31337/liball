using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class QuantizationTransformSettings
{
    internal double Tolerance = 0.0001;
}

internal class QuantizationTransformSettingsReader
{
    internal QuantizationTransformSettings ReadQuantizationSettings(
        GltfTransformStep step)
    {
        if (step.Settings == null)
        {
            throw new ArgumentException(
                "Quantization settings must not be null.",
                "step");
        }

        QuantizationTransformSettings result = new QuantizationTransformSettings();
        if (!step.Settings.ContainsKey("tolerance"))
        {
            return result;
        }

        JsonNode? value = step.Settings["tolerance"];
        if (value == null || value.GetValueKind() != JsonValueKind.Number)
        {
            throw new ArgumentException(
                "Quantization setting 'tolerance' must be numeric.",
                "step");
        }

        double tolerance = double.Parse(
            value.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new ArgumentException(
                "Quantization setting 'tolerance' must be finite and non-negative.",
                "step");
        }

        result.Tolerance = tolerance;
        return result;
    }
}
