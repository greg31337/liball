using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class PrimitiveTransformSettingsReader
{
    internal PrimitiveTransformSettings ReadPrimitiveSettings(GltfTransformStep step)
    {
        if (step.Settings == null)
        {
            throw new ArgumentException("Primitive transform settings must not be null.", "step");
        }

        PrimitiveTransformSettings result = new PrimitiveTransformSettings();
        if (!step.Settings.ContainsKey("maxVertices"))
        {
            return result;
        }

        JsonNode? value = step.Settings["maxVertices"];
        if (value == null || value.GetValueKind() != JsonValueKind.Number)
        {
            throw new ArgumentException(
                "Primitive transform setting 'maxVertices' must be an integer.",
                "step");
        }

        double numeric = double.Parse(
            value.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(numeric)
            || numeric < 3.0
            || numeric > int.MaxValue
            || numeric != Math.Floor(numeric))
        {
            throw new ArgumentException(
                "Primitive transform setting 'maxVertices' must be an Int32 value of at least three.",
                "step");
        }

        result.MaxVertices = (int)numeric;
        return result;
    }
}
