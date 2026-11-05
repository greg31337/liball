using System;

namespace gltf;

internal class ExtensionFallback
{
    internal GltfExtensionResult CreatePreservedResult(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        if (context.Name == null || context.Name.Length == 0)
        {
            throw new ArgumentException("The extension context name must not be empty.", "context");
        }

        GltfExtensionResult result = new GltfExtensionResult();
        result.Descriptor = registry.FindDescriptor(context.Name);
        if (result.Descriptor == null)
        {
            result.Descriptor = CreateUnknownDescriptor(context.Name);
        }

        result.Json = JsonValueCloner.CloneNode(context.Json);
        result.TypedData = context.TypedData;
        return result;
    }

    internal GltfExtensionDescriptor CreateUnknownDescriptor(string name)
    {
        GltfExtensionDescriptor result = new GltfExtensionDescriptor();
        result.Name = name;
        result.AllowsFallback = true;
        return result;
    }

    internal string CreateExtensionPointer(GltfExtensionContext context)
    {
        if (context.ObjectJsonPointer.Length > 0)
        {
            return context.ObjectJsonPointer
                + "/extensions/"
                + EscapePointerSegment(context.Name);
        }

        if (context.ObjectLocation == null || context.ObjectLocation.Length == 0
            || context.ObjectLocation == "glTF")
        {
            return "/extensions/" + EscapePointerSegment(context.Name);
        }

        string result = "/" + context.ObjectLocation;
        if (context.HasObjectIndex)
        {
            result += "/" + context.ObjectIndex;
        }

        return result + "/extensions/" + EscapePointerSegment(context.Name);
    }

    internal string EscapePointerSegment(string value)
    {
        return value.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
    }
}
