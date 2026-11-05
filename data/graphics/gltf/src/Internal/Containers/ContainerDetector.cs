using System;

namespace gltf;

internal class ContainerDetector
{
    internal GltfContainerFormat Detect(byte[] prefix, GltfContainerFormat requestedFormat, string? sourceName)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (requestedFormat == GltfContainerFormat.Json)
        {
            return GltfContainerFormat.Json;
        }

        if (requestedFormat == GltfContainerFormat.Glb)
        {
            return GltfContainerFormat.Glb;
        }

        if (requestedFormat != GltfContainerFormat.Auto)
        {
            throw new ArgumentOutOfRangeException("requestedFormat");
        }

        string inputDescription = "The input";
        if (sourceName != null && sourceName.Length > 0)
        {
            inputDescription = $"The input '{sourceName}'";
        }

        if (prefix.Length >= GltfConstants.AlignmentByteLength)
        {
            uint magic = (uint)prefix[0]
                | ((uint)prefix[1] << 8)
                | ((uint)prefix[2] << 16)
                | ((uint)prefix[3] << 24);
            if (magic == GltfConstants.GlbMagic)
            {
                return GltfContainerFormat.Glb;
            }
        }

        int offset = 0;
        if (prefix.Length >= 3
            && prefix[0] == 0xEF
            && prefix[1] == 0xBB
            && prefix[2] == 0xBF)
        {
            offset = 3;
        }

        while (offset < prefix.Length
            && (prefix[offset] == 0x20
                || prefix[offset] == 0x09
                || prefix[offset] == 0x0A
                || prefix[offset] == 0x0D))
        {
            offset++;
        }

        if (offset >= prefix.Length)
        {
            throw new GltfParseException($"{inputDescription} does not contain a glTF document.");
        }

        if (prefix[offset] == 0x7B)
        {
            return GltfContainerFormat.Json;
        }

        if (prefix[0] == 0x67)
        {
            throw new GltfParseException($"{inputDescription} begins with malformed GLB magic.");
        }

        throw new GltfParseException($"{inputDescription} is neither JSON glTF nor binary GLB.");
    }
}
