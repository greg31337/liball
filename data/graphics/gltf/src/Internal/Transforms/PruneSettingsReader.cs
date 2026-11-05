using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class PruneSettings
{
    internal List<int> Scenes = new List<int>();
    internal bool HasScenes;
    internal bool KeepAnimations = true;
    internal bool KeepSkins = true;
    internal bool KeepExtensions = true;
    internal bool KeepExtras = true;
}

internal class PruneSettingsReader
{
    internal PruneSettings ReadSettings(GltfTransformStep step, int sceneCount)
    {
        if (step.Settings == null)
        {
            throw new ArgumentException("Prune settings must not be null.", "step");
        }

        PruneSettings result = new PruneSettings();
        result.KeepAnimations = ReadBooleanSetting(
            step.Settings,
            "keepAnimations",
            true);
        result.KeepSkins = ReadBooleanSetting(step.Settings, "keepSkins", true);
        result.KeepExtensions = ReadBooleanSetting(
            step.Settings,
            "keepExtensions",
            true);
        result.KeepExtras = ReadBooleanSetting(step.Settings, "keepExtras", true);
        if (step.Settings.ContainsKey("scenes"))
        {
            result.HasScenes = true;
            JsonArray? scenes = step.Settings["scenes"] as JsonArray;
            if (scenes == null)
            {
                throw new ArgumentException(
                    "Prune setting 'scenes' must be an array of scene indices.",
                    "step");
            }

            int index = 0;
            while (index < scenes.Count)
            {
                JsonNode? item = scenes[index];
                if (item == null || item.GetValueKind() != JsonValueKind.Number)
                {
                    throw new ArgumentException(
                        $"Prune scene selection {index} must be an integer.",
                        "step");
                }

                double numeric = double.Parse(
                    item.ToJsonString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
                if (!double.IsFinite(numeric)
                    || numeric < 0.0
                    || numeric > int.MaxValue
                    || numeric != Math.Floor(numeric))
                {
                    throw new ArgumentException(
                        $"Prune scene selection {index} must be a non-negative Int32 index.",
                        "step");
                }

                int scene = (int)numeric;
                if (scene >= sceneCount)
                {
                    throw new ArgumentException(
                        $"Prune scene index {scene} does not resolve to a scene.",
                        "step");
                }

                if (!result.Scenes.Contains(scene))
                {
                    result.Scenes.Add(scene);
                }

                index++;
            }
        }

        return result;
    }

    internal bool ReadBooleanSetting(
        JsonObject settings,
        string name,
        bool defaultValue)
    {
        if (!settings.ContainsKey(name))
        {
            return defaultValue;
        }

        JsonNode? value = settings[name];
        if (value == null
            || (value.GetValueKind() != JsonValueKind.True
                && value.GetValueKind() != JsonValueKind.False))
        {
            throw new ArgumentException(
                $"Prune setting '{name}' must be a boolean.",
                "settings");
        }

        return value.GetValue<bool>();
    }
}
