using System;
using System.Xml;

namespace tiff;

internal class TiffOmeCoordinateState
{
    internal int ChannelIndex;
    internal int ZIndex;
    internal int TimeIndex;

    internal TiffOmeCoordinateState(int channelIndex, int zIndex, int timeIndex)
    {
        ChannelIndex = channelIndex;
        ZIndex = zIndex;
        TimeIndex = timeIndex;
    }
}

internal class TiffOmeXmlParser
{
    internal static TiffOmeMetadata Parse(string xml, string imageDescriptionText, byte[] imageDescriptionRawBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        string actualXml = TiffOmeMetadata.NormalizeString(xml);
        TiffOmeMetadata metadata = TiffOmeMetadata.Create(true, actualXml, imageDescriptionText, imageDescriptionRawBytes, false, false, "", new TiffOmeImage[0], new TiffOmeFileReference[0], new TiffOmePyramidLevel[0]);
        if (!HasCompleteOmeRoot(actualXml))
        {
            metadata.IsPartialMetadata = true;
            diagnostics.AddWarningForTag(TiffErrorCode.InvalidTagValue, "OME XML has an incomplete root element; only the original XML was preserved.", TiffDiagnostic.NoImageIndex, TiffTagId.ImageDescription);
            return metadata;
        }

        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        document.LoadXml(actualXml);
        XmlElement? root = document.DocumentElement;
        if (root == null || root.LocalName != "OME")
        {
            metadata.IsPartialMetadata = true;
            return metadata;
        }

        metadata.Uuid = root.GetAttribute("UUID");
        XmlElement? binaryOnly = FindChildElement(root, "BinaryOnly");
        if (binaryOnly != null)
        {
            metadata.IsPartialMetadata = true;
            metadata.PartialMetadataFileName = binaryOnly.GetAttribute("MetadataFile");
            metadata.PartialMetadataUuid = binaryOnly.GetAttribute("UUID");
            if (metadata.PartialMetadataFileName.Length == 0 || metadata.PartialMetadataUuid.Length == 0)
            {
                diagnostics.AddWarningForTag(TiffErrorCode.InvalidTagValue, "OME BinaryOnly metadata is missing its companion file name or UUID.", TiffDiagnostic.NoImageIndex, TiffTagId.ImageDescription);
            }
            else
            {
                metadata.FileReferences = AppendFileReference(metadata.FileReferences, TiffOmeFileReference.Create(metadata.PartialMetadataUuid, metadata.PartialMetadataFileName, false));
            }
        }

        int imageIndex = 0;
        int childIndex = 0;
        while (childIndex < root.ChildNodes.Count)
        {
            XmlNode? node = root.ChildNodes[childIndex];
            if (node != null && node.NodeType == XmlNodeType.Element && node.LocalName == "Image")
            {
                TiffOmeImage image = ParseImage((XmlElement)node, imageIndex, metadata.Uuid, limits, metadata, diagnostics);
                metadata.Images = AppendImage(metadata.Images, image);
                imageIndex = imageIndex + 1;
            }

            childIndex = childIndex + 1;
        }

        if (metadata.Images.Length == 0 && binaryOnly == null)
        {
            metadata.IsPartialMetadata = true;
        }

        metadata.IsMultiFile = CalculateIsMultiFile(metadata.FileReferences);
        return metadata;
    }

    internal static TiffOmeImage ParseImage(XmlElement imageElement, int imageIndex, string documentUuid, TiffLimits limits, TiffOmeMetadata metadata, TiffDiagnostics diagnostics)
    {
        string id = imageElement.GetAttribute("ID");
        string name = imageElement.GetAttribute("Name");
        XmlElement? pixels = FindChildElement(imageElement, "Pixels");
        if (pixels == null)
        {
            metadata.IsPartialMetadata = true;
            return TiffOmeImage.Create(imageIndex, id, name, "", TiffOmeDimensionOrder.Unknown, "", 0, 0, 0, 0, 0, "", new TiffOmePlane[0]);
        }

        string pixelsId = pixels.GetAttribute("ID");
        string dimensionOrderText = pixels.GetAttribute("DimensionOrder");
        TiffOmeDimensionOrder dimensionOrder = ParseDimensionOrder(dimensionOrderText);
        ulong sizeX = ParseUnsignedLong(pixels.GetAttribute("SizeX"));
        ulong sizeY = ParseUnsignedLong(pixels.GetAttribute("SizeY"));
        int sizeC = ParseNonNegativeInt(pixels.GetAttribute("SizeC"));
        int sizeZ = ParseNonNegativeInt(pixels.GetAttribute("SizeZ"));
        int sizeT = ParseNonNegativeInt(pixels.GetAttribute("SizeT"));
        string pixelType = pixels.GetAttribute("Type");
        if (id.Length == 0 || pixelsId.Length == 0 || dimensionOrder == TiffOmeDimensionOrder.Unknown || sizeX == 0 || sizeY == 0 || sizeC <= 0 || sizeZ <= 0 || sizeT <= 0 || pixelType.Length == 0)
        {
            metadata.IsPartialMetadata = true;
        }

        TiffOmePlane[] planes = new TiffOmePlane[0];
        int childIndex = 0;
        while (childIndex < pixels.ChildNodes.Count)
        {
            XmlNode? node = pixels.ChildNodes[childIndex];
            if (node != null && node.NodeType == XmlNodeType.Element && node.LocalName == "TiffData")
            {
                planes = AppendTiffDataPlanes(planes, (XmlElement)node, imageIndex, dimensionOrder, sizeC, sizeZ, sizeT, documentUuid, limits, metadata, diagnostics);
            }

            childIndex = childIndex + 1;
        }

        if (planes.Length == 0)
        {
            metadata.IsPartialMetadata = true;
        }

        return TiffOmeImage.Create(imageIndex, id, name, pixelsId, dimensionOrder, dimensionOrderText, sizeX, sizeY, sizeC, sizeZ, sizeT, pixelType, planes);
    }

    internal static TiffOmePlane[] AppendTiffDataPlanes(TiffOmePlane[] planes, XmlElement tiffData, int imageIndex, TiffOmeDimensionOrder dimensionOrder, int sizeC, int sizeZ, int sizeT, string documentUuid, TiffLimits limits, TiffOmeMetadata metadata, TiffDiagnostics diagnostics)
    {
        int firstC = ParseOptionalNonNegativeInt(tiffData.GetAttribute("FirstC"), 0);
        int firstZ = ParseOptionalNonNegativeInt(tiffData.GetAttribute("FirstZ"), 0);
        int firstT = ParseOptionalNonNegativeInt(tiffData.GetAttribute("FirstT"), 0);
        int firstIfd = ParseOptionalNonNegativeInt(tiffData.GetAttribute("IFD"), 0);
        int defaultPlaneCount = 1;
        if (!tiffData.HasAttribute("IFD"))
        {
            defaultPlaneCount = CalculatePlaneCount(sizeC, sizeZ, sizeT);
        }

        int planeCount = ParseOptionalNonNegativeInt(tiffData.GetAttribute("PlaneCount"), defaultPlaneCount);
        if (planeCount <= 0)
        {
            metadata.IsPartialMetadata = true;
            return planes;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)planeCount * 96L))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "OME plane mappings exceed the configured allocation limit.");
            return planes;
        }

        XmlElement? uuidElement = FindChildElement(tiffData, "UUID");
        string uuid = "";
        string fileName = "";
        if (uuidElement != null)
        {
            uuid = TiffOmeMetadata.NormalizeString(uuidElement.InnerText).Trim();
            fileName = uuidElement.GetAttribute("FileName");
        }

        bool isCurrentFile = uuid.Length == 0 && fileName.Length == 0;
        if (uuid.Length > 0 && documentUuid.Length > 0 && uuid == documentUuid)
        {
            isCurrentFile = true;
        }

        bool isExternal = !isCurrentFile;
        if (uuid.Length > 0 || fileName.Length > 0)
        {
            metadata.FileReferences = AppendFileReference(metadata.FileReferences, TiffOmeFileReference.Create(uuid, fileName, isCurrentFile));
        }

        TiffOmeCoordinateState coordinates = new TiffOmeCoordinateState(firstC, firstZ, firstT);
        int offset = 0;
        while (offset < planeCount)
        {
            bool validCoordinates = coordinates.ChannelIndex >= 0 && coordinates.ChannelIndex < sizeC && coordinates.ZIndex >= 0 && coordinates.ZIndex < sizeZ && coordinates.TimeIndex >= 0 && coordinates.TimeIndex < sizeT;
            if (!validCoordinates || firstIfd > int.MaxValue - offset)
            {
                metadata.IsPartialMetadata = true;
            }
            else
            {
                TiffOmePlane plane = TiffOmePlane.Create(imageIndex, planes.Length, coordinates.ChannelIndex, coordinates.ZIndex, coordinates.TimeIndex, true, firstIfd + offset, uuid, fileName, isExternal);
                planes = AppendPlane(planes, plane);
            }

            AdvanceCoordinates(coordinates, dimensionOrder, sizeC, sizeZ, sizeT);
            offset = offset + 1;
        }

        return planes;
    }

    internal static void AdvanceCoordinates(TiffOmeCoordinateState coordinates, TiffOmeDimensionOrder dimensionOrder, int sizeC, int sizeZ, int sizeT)
    {
        string order = GetDimensionOrderText(dimensionOrder);
        int axisIndex = 2;
        while (axisIndex < order.Length)
        {
            char axis = order[axisIndex];
            int value = GetCoordinate(coordinates, axis) + 1;
            int size = GetCoordinateSize(axis, sizeC, sizeZ, sizeT);
            if (value < size)
            {
                SetCoordinate(coordinates, axis, value);
                return;
            }

            SetCoordinate(coordinates, axis, 0);
            axisIndex = axisIndex + 1;
        }
    }

    internal static int GetCoordinate(TiffOmeCoordinateState coordinates, char axis)
    {
        if (axis == 'C')
        {
            return coordinates.ChannelIndex;
        }

        if (axis == 'Z')
        {
            return coordinates.ZIndex;
        }

        return coordinates.TimeIndex;
    }

    internal static void SetCoordinate(TiffOmeCoordinateState coordinates, char axis, int value)
    {
        if (axis == 'C')
        {
            coordinates.ChannelIndex = value;
            return;
        }

        if (axis == 'Z')
        {
            coordinates.ZIndex = value;
            return;
        }

        coordinates.TimeIndex = value;
    }

    internal static int GetCoordinateSize(char axis, int sizeC, int sizeZ, int sizeT)
    {
        if (axis == 'C')
        {
            return sizeC;
        }

        if (axis == 'Z')
        {
            return sizeZ;
        }

        return sizeT;
    }

    internal static XmlElement? FindChildElement(XmlElement parent, string localName)
    {
        int index = 0;
        while (index < parent.ChildNodes.Count)
        {
            XmlNode? node = parent.ChildNodes[index];
            if (node != null && node.NodeType == XmlNodeType.Element && node.LocalName == localName)
            {
                return (XmlElement)node;
            }

            index = index + 1;
        }

        return null;
    }

    internal static TiffOmeDimensionOrder ParseDimensionOrder(string text)
    {
        switch (text)
        {
            case "XYZCT":
                return TiffOmeDimensionOrder.XYZCT;
            case "XYZTC":
                return TiffOmeDimensionOrder.XYZTC;
            case "XYCZT":
                return TiffOmeDimensionOrder.XYCZT;
            case "XYCTZ":
                return TiffOmeDimensionOrder.XYCTZ;
            case "XYTCZ":
                return TiffOmeDimensionOrder.XYTCZ;
            case "XYTZC":
                return TiffOmeDimensionOrder.XYTZC;
            default:
                return TiffOmeDimensionOrder.Unknown;
        }
    }

    internal static string GetDimensionOrderText(TiffOmeDimensionOrder order)
    {
        switch (order)
        {
            case TiffOmeDimensionOrder.XYZCT:
                return "XYZCT";
            case TiffOmeDimensionOrder.XYZTC:
                return "XYZTC";
            case TiffOmeDimensionOrder.XYCZT:
                return "XYCZT";
            case TiffOmeDimensionOrder.XYCTZ:
                return "XYCTZ";
            case TiffOmeDimensionOrder.XYTCZ:
                return "XYTCZ";
            case TiffOmeDimensionOrder.XYTZC:
                return "XYTZC";
            default:
                return "XYZCT";
        }
    }

    internal static int ParseOptionalNonNegativeInt(string text, int defaultValue)
    {
        if (text == null || text.Length == 0)
        {
            return defaultValue;
        }

        return ParseNonNegativeInt(text);
    }

    internal static int CalculatePlaneCount(int sizeC, int sizeZ, int sizeT)
    {
        if (sizeC <= 0 || sizeZ <= 0 || sizeT <= 0)
        {
            return 0;
        }

        if (sizeC > int.MaxValue / sizeZ)
        {
            return 0;
        }

        int count = sizeC * sizeZ;
        if (count > int.MaxValue / sizeT)
        {
            return 0;
        }

        return count * sizeT;
    }

    internal static int ParseNonNegativeInt(string text)
    {
        if (text == null || text.Length == 0)
        {
            return 0;
        }

        int value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char digit = text[index];
            if (digit < '0' || digit > '9' || value > (int.MaxValue - (digit - '0')) / 10)
            {
                return 0;
            }

            value = value * 10 + (digit - '0');
            index = index + 1;
        }

        return value;
    }

    internal static ulong ParseUnsignedLong(string text)
    {
        if (text == null || text.Length == 0)
        {
            return 0;
        }

        ulong value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char digit = text[index];
            if (digit < '0' || digit > '9')
            {
                return 0;
            }

            ulong number = (ulong)(digit - '0');
            if (value > (ulong.MaxValue - number) / 10UL)
            {
                return 0;
            }

            value = value * 10UL + number;
            index = index + 1;
        }

        return value;
    }

    internal static bool HasCompleteOmeRoot(string xml)
    {
        if (xml == null || xml.Length == 0)
        {
            return false;
        }

        return xml.IndexOf("</OME>", StringComparison.Ordinal) >= 0 || xml.IndexOf("<OME/>", StringComparison.Ordinal) >= 0 || xml.IndexOf("<OME />", StringComparison.Ordinal) >= 0;
    }

    internal static bool CalculateIsMultiFile(TiffOmeFileReference[] references)
    {
        int index = 0;
        while (index < references.Length)
        {
            if (!references[index].IsCurrentFile)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static TiffOmeImage[] AppendImage(TiffOmeImage[] images, TiffOmeImage image)
    {
        TiffOmeImage[] result = new TiffOmeImage[images.Length + 1];
        int index = 0;
        while (index < images.Length)
        {
            result[index] = images[index].Copy();
            index = index + 1;
        }

        result[index] = image.Copy();
        return result;
    }

    internal static TiffOmePlane[] AppendPlane(TiffOmePlane[] planes, TiffOmePlane plane)
    {
        TiffOmePlane[] result = new TiffOmePlane[planes.Length + 1];
        int index = 0;
        while (index < planes.Length)
        {
            result[index] = planes[index].Copy();
            index = index + 1;
        }

        result[index] = plane.Copy();
        return result;
    }

    internal static TiffOmeFileReference[] AppendFileReference(TiffOmeFileReference[] references, TiffOmeFileReference reference)
    {
        int index = 0;
        while (index < references.Length)
        {
            if (references[index].Uuid == reference.Uuid && references[index].FileName == reference.FileName)
            {
                return TiffOmeMetadata.CopyFileReferences(references);
            }

            index = index + 1;
        }

        TiffOmeFileReference[] result = new TiffOmeFileReference[references.Length + 1];
        index = 0;
        while (index < references.Length)
        {
            result[index] = references[index].Copy();
            index = index + 1;
        }

        result[index] = reference.Copy();
        return result;
    }
}
