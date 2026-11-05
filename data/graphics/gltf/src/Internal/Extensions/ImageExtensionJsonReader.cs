using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class ImageExtensionJsonReader
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal bool HandlesImageExtension(string name)
    {
        return name == GltfExtensionNames.KhrTextureBasisu
            || name == GltfExtensionNames.ExtTextureWebp;
    }

    internal GltfExtensionResult ReadImageExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        if (context.Json == null || context.Json.GetValueKind() != JsonValueKind.Object)
        {
            AddIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The texture image extension payload must be a JSON object.",
                Fallback.CreateExtensionPointer(context));
            return result;
        }

        JsonObject source = context.Json.AsObject();
        bool hasSource = source.ContainsKey("source");
        int image = GltfConstants.AbsentIndex;
        if (!hasSource)
        {
            AddIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The required source member is missing.",
                Fallback.CreateExtensionPointer(context) + "/source");
        }
        else
        {
            image = ReadImageIndex(
                source["source"],
                result,
                Fallback.CreateExtensionPointer(context) + "/source");
        }

        GltfPropertyJson propertyJson = PropertyJson.ReadPropertyData(source, ["source"]);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (context.Name == GltfExtensionNames.KhrTextureBasisu)
        {
            GltfTextureBasisuExtension value = new GltfTextureBasisuExtension();
            value.HasSource = hasSource;
            value.Source = image;
            value.PropertyJson = propertyJson;
            typed.TextureBasisu = value;
        }
        else
        {
            GltfTextureWebpExtension value = new GltfTextureWebpExtension();
            value.HasSource = hasSource;
            value.Source = image;
            value.PropertyJson = propertyJson;
            typed.TextureWebp = value;
        }

        result.TypedData = typed;
        AddImageReference(context, result, image);
        return result;
    }

    internal int ReadImageIndex(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            AddIssue(result, GltfIssueCodes.TypeMismatch, "Expected an image index.", pointer);
            return GltfConstants.AbsentIndex;
        }

        double value = double.Parse(
            node.ToJsonString(),
            System.Globalization.CultureInfo.InvariantCulture);
        if (!double.IsFinite(value)
            || value != System.Math.Truncate(value)
            || value < 0.0
            || value > int.MaxValue)
        {
            AddIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Expected a non-negative 32-bit image index.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        return (int)value;
    }

    internal void AddImageReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int image)
    {
        if (image < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "textures";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/" + context.Name + "/source";
        reference.TargetCollection = "images";
        reference.TargetIndex = image;
        result.References.Add(reference);
    }

    internal void AddIssue(
        GltfExtensionResult result,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.Status = GltfOperationStatus.Invalid;
    }
}
