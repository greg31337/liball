using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class SceneTransformSettingsReader
{
    internal SceneTransformSettings ReadSceneTransformSettings(
        GltfTransformStep step,
        int nodeCount,
        int sceneCount)
    {
        if (step.Settings == null)
        {
            throw new ArgumentException("Scene transform settings must not be null.", "step");
        }

        SceneTransformSettings result = new SceneTransformSettings();
        ReadNodeSelection(step.Settings, nodeCount, result);
        ReadSceneSelection(step.Settings, sceneCount, result);
        if (step.Settings.ContainsKey("matrix"))
        {
            double[] values = ReadNumericArray(step.Settings["matrix"], 16, "matrix");
            result.Matrix = new TransformMath().ReadColumnMajorMatrix(values);
            result.HasMatrix = true;
        }

        if (step.Settings.ContainsKey("scale"))
        {
            result.UnitScale = ReadFiniteNumber(step.Settings["scale"], "scale");
            if (result.UnitScale <= 0.0)
            {
                throw new ArgumentException(
                    "Scene transform setting 'scale' must be positive.",
                    "step");
            }
        }

        if (step.Settings.ContainsKey("axisBasis"))
        {
            result.AxisBasis = ReadAxisBasis(step.Settings["axisBasis"]);
            result.HasAxisBasis = true;
        }

        return result;
    }

    internal void ReadNodeSelection(
        JsonObject settings,
        int nodeCount,
        SceneTransformSettings result)
    {
        if (!settings.ContainsKey("nodes"))
        {
            return;
        }

        result.HasNodes = true;
        JsonArray? values = settings["nodes"] as JsonArray;
        if (values == null)
        {
            throw new ArgumentException(
                "Scene transform setting 'nodes' must be an array.",
                "settings");
        }

        int index = 0;
        while (index < values.Count)
        {
            int node = ReadNonNegativeInteger(values[index], "nodes");
            if (node >= nodeCount)
            {
                throw new ArgumentException(
                    $"Scene transform node index {node} does not resolve.",
                    "settings");
            }

            if (!result.Nodes.Contains(node))
            {
                result.Nodes.Add(node);
            }

            index++;
        }
    }

    internal void ReadSceneSelection(
        JsonObject settings,
        int sceneCount,
        SceneTransformSettings result)
    {
        if (!settings.ContainsKey("scene"))
        {
            return;
        }

        int scene = ReadNonNegativeInteger(settings["scene"], "scene");
        if (scene >= sceneCount)
        {
            throw new ArgumentException(
                $"Scene transform scene index {scene} does not resolve.",
                "settings");
        }

        result.HasScene = true;
        result.Scene = scene;
    }

    internal int ReadNonNegativeInteger(JsonNode? value, string name)
    {
        double numeric = ReadFiniteNumber(value, name);
        if (numeric < 0.0
            || numeric > int.MaxValue
            || numeric != Math.Floor(numeric))
        {
            throw new ArgumentException(
                $"Scene transform setting '{name}' must contain non-negative Int32 values.",
                "settings");
        }

        return (int)numeric;
    }

    internal double ReadFiniteNumber(JsonNode? value, string name)
    {
        if (value == null || value.GetValueKind() != JsonValueKind.Number)
        {
            throw new ArgumentException(
                $"Scene transform setting '{name}' must be numeric.",
                "settings");
        }

        double result = double.Parse(
            value.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(result))
        {
            throw new ArgumentException(
                $"Scene transform setting '{name}' must be finite.",
                "settings");
        }

        return result;
    }

    internal double[] ReadNumericArray(
        JsonNode? value,
        int expectedCount,
        string name)
    {
        JsonArray? array = value as JsonArray;
        if (array == null || array.Count != expectedCount)
        {
            throw new ArgumentException(
                $"Scene transform setting '{name}' must contain {expectedCount} numbers.",
                "settings");
        }

        double[] result = new double[expectedCount];
        int index = 0;
        while (index < array.Count)
        {
            result[index] = ReadFiniteNumber(array[index], name);
            if (Math.Abs(result[index]) > float.MaxValue)
            {
                throw new ArgumentException(
                    $"Scene transform setting '{name}' exceeds the matrix value range.",
                    "settings");
            }

            index++;
        }

        return result;
    }

    internal Matrix4x4 ReadAxisBasis(JsonNode? value)
    {
        JsonArray? array = value as JsonArray;
        if (array == null || (array.Count != 9 && array.Count != 16))
        {
            throw new ArgumentException(
                "Scene transform setting 'axisBasis' must contain 9 or 16 numbers.",
                "settings");
        }

        List<double> values = new List<double>();
        int index = 0;
        while (index < array.Count)
        {
            values.Add(ReadFiniteNumber(array[index], "axisBasis"));
            index++;
        }

        if (values.Count == 9)
        {
            return new Matrix4x4(
                (float)values[0], (float)values[1], (float)values[2], 0.0f,
                (float)values[3], (float)values[4], (float)values[5], 0.0f,
                (float)values[6], (float)values[7], (float)values[8], 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        return new Matrix4x4(
            (float)values[0], (float)values[1], (float)values[2], (float)values[3],
            (float)values[4], (float)values[5], (float)values[6], (float)values[7],
            (float)values[8], (float)values[9], (float)values[10], (float)values[11],
            (float)values[12], (float)values[13], (float)values[14], (float)values[15]);
    }
}
