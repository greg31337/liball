using System.Text.Json.Nodes;

namespace gltf;

internal class ImageExtensionJsonWriter
{
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal JsonNode? WriteImageExtension(
        GltfExtensionContext context,
        GltfTypedExtensionData typed)
    {
        if (context.Name == GltfExtensionNames.KhrTextureBasisu
            && typed.TextureBasisu != null)
        {
            return WriteBasisu(typed.TextureBasisu);
        }

        if (context.Name == GltfExtensionNames.ExtTextureWebp
            && typed.TextureWebp != null)
        {
            return WriteWebp(typed.TextureWebp);
        }

        return null;
    }

    internal JsonObject WriteBasisu(GltfTextureBasisuExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSource)
        {
            result["source"] = JsonValue.Create(source.Source);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteWebp(GltfTextureWebpExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSource)
        {
            result["source"] = JsonValue.Create(source.Source);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }
}
