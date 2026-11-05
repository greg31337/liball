using System.Collections.Generic;

namespace gltf;

internal class ImageBufferViewPackPlan
{
    internal byte[] BufferBytes = [];
    internal List<int> ResourceBufferViews = new List<int>();
    internal List<int> ByteOffsets = new List<int>();
    internal List<int> ByteLengths = new List<int>();
    internal List<string> MimeTypes = new List<string>();
    internal List<byte[]> Images = new List<byte[]>();
}

internal class ImageBufferViewPacker
{
    internal ImageBufferViewPackPlan PackImages(GltfResourceSet resources)
    {
        if (resources == null)
        {
            throw new System.ArgumentNullException("resources");
        }

        ValidateImageResources(resources);
        ImageBufferViewPackPlan result = new ImageBufferViewPackPlan();
        int totalByteLength = 0;
        int resourceIndex = 0;
        while (resourceIndex < resources.Resources.Count)
        {
            GltfResource resource = resources.Resources[resourceIndex];
            int bufferViewIndex = -1;
            if (resource.Kind == GltfResourceKind.Image)
            {
                string mimeType = GetImageMimeType(resource);
                bufferViewIndex = FindPackedImage(result, resource.Bytes, mimeType);
                if (bufferViewIndex < 0)
                {
                    int offset = FourByteAlignment.Align(totalByteLength);
                    long end = (long)offset + resource.Bytes.Length;
                    if (end > int.MaxValue)
                    {
                        throw new GltfWriteException("Packed image buffer exceeds the supported signed byte length.");
                    }

                    bufferViewIndex = result.ByteOffsets.Count;
                    result.ByteOffsets.Add(offset);
                    result.ByteLengths.Add(resource.Bytes.Length);
                    result.MimeTypes.Add(mimeType);
                    result.Images.Add(OwnedBytes.CopyArray(resource.Bytes));
                    totalByteLength = (int)end;
                }
            }

            result.ResourceBufferViews.Add(bufferViewIndex);
            resourceIndex++;
        }

        result.BufferBytes = new byte[totalByteLength];
        int imageIndex = 0;
        while (imageIndex < result.Images.Count)
        {
            byte[] image = result.Images[imageIndex];
            System.Array.Copy(
                image,
                0,
                result.BufferBytes,
                result.ByteOffsets[imageIndex],
                image.Length);
            imageIndex++;
        }

        return result;
    }

    internal void ValidateImageResources(GltfResourceSet resources)
    {
        int index = 0;
        while (index < resources.Resources.Count)
        {
            GltfResource resource = resources.Resources[index];
            if (resource == null)
            {
                throw new GltfWriteException($"Document resource {index} is null.");
            }

            if (resource.Bytes == null)
            {
                throw new GltfWriteException($"Document resource {index} bytes are null.");
            }

            if (resource.Kind == GltfResourceKind.Image)
            {
                GetImageMimeType(resource);
            }

            index++;
        }
    }

    internal int FindPackedImage(ImageBufferViewPackPlan plan, byte[] bytes, string mimeType)
    {
        int index = 0;
        while (index < plan.Images.Count)
        {
            if (string.Equals(
                    plan.MimeTypes[index],
                    mimeType,
                    System.StringComparison.OrdinalIgnoreCase)
                && HaveEqualImageBytes(plan.Images[index], bytes))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool HaveEqualImageBytes(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal string GetImageMimeType(GltfResource resource)
    {
        if (resource.MimeType != null && resource.MimeType.Length > 0)
        {
            return resource.MimeType.ToLowerInvariant();
        }

        switch (resource.ImageFormat)
        {
            case GltfImageFormat.Png:
                return GltfMimeTypes.Png;
            case GltfImageFormat.Jpeg:
                return GltfMimeTypes.Jpeg;
            case GltfImageFormat.Ktx2:
                return GltfMimeTypes.Ktx2;
            case GltfImageFormat.WebP:
                return GltfMimeTypes.WebP;
            case GltfImageFormat.Avif:
                return GltfMimeTypes.Avif;
            case GltfImageFormat.Dds:
                return GltfMimeTypes.Dds;
            default:
                throw new GltfWriteException("A packed image resource must have a known media type.");
        }
    }
}
