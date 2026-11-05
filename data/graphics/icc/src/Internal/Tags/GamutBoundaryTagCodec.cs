namespace icc;

internal static class GamutBoundaryTagCodec
{
    internal const int HeaderSize = 20;

    internal static void DecodeGamutBoundary(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < HeaderSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A gamutBoundaryDescriptionType payload requires at least {HeaderSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The gamutBoundaryDescriptionType reserved bytes shall be zero.");
            return;
        }

        int pcsChannels = ReadUInt16At(payload, 8);
        int deviceChannels = ReadUInt16At(payload, 10);
        uint encodedVertexCount = ReadUInt32At(payload, 12);
        uint encodedTriangleCount = ReadUInt32At(payload, 16);
        if (pcsChannels < 3)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "A gamutBoundaryDescriptionType shall declare at least three PCS channels.");
            return;
        }

        if (encodedVertexCount < 4 || encodedTriangleCount < 4)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "A gamutBoundaryDescriptionType shall contain at least four vertices and four faces.");
            return;
        }

        if (encodedVertexCount > System.Array.MaxLength || encodedTriangleCount > System.Array.MaxLength)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The gamut-boundary vertex or face count exceeds the supported managed array size.");
            return;
        }

        long expectedSize = HeaderSize + ((long)encodedTriangleCount * 12L)
            + ((long)encodedVertexCount * (pcsChannels + deviceChannels) * 4L);
        if (expectedSize != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The gamut-boundary counts require {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        string contextError = ValidateTagContext(state, pcsChannels, deviceChannels);
        if (contextError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, contextError);
            return;
        }

        int vertexCount = (int)encodedVertexCount;
        int triangleCount = (int)encodedTriangleCount;
        IccGamutTriangle[] triangles = new IccGamutTriangle[triangleCount];
        BigEndianReader triangleReader = new BigEndianReader(payload.Slice(HeaderSize, triangleCount * 12), state.AbsolutePayloadOffset + HeaderSize);
        int index = 0;
        while (index < triangleCount)
        {
            uint first = triangleReader.ReadUInt32();
            uint second = triangleReader.ReadUInt32();
            uint third = triangleReader.ReadUInt32();
            if (first >= encodedVertexCount || second >= encodedVertexCount || third >= encodedVertexCount)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Gamut-boundary face {index} contains a vertex ID outside the range 0 through {vertexCount - 1}.");
                return;
            }

            IccGamutTriangle triangle = new IccGamutTriangle();
            triangle.First = (int)first;
            triangle.Second = (int)second;
            triangle.Third = (int)third;
            triangles[index] = triangle;
            index = index + 1;
        }

        string topologyError = ValidateMeshTopology(triangles, vertexCount);
        if (topologyError.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, topologyError);
            return;
        }

        int coordinateOffset = HeaderSize + (triangleCount * 12);
        if (!Icc2ArrayTagCodec.ContainsOnlyFiniteFloatingPointValues(payload.Slice(coordinateOffset), IccTypeSignatures.Float32Array))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "Gamut-boundary PCS and device coordinates shall contain only finite float32 values.");
            return;
        }

        BigEndianReader coordinateReader = new BigEndianReader(payload.Slice(coordinateOffset), state.AbsolutePayloadOffset + coordinateOffset);
        IccGamutVertex[] vertices = new IccGamutVertex[vertexCount];
        index = 0;
        while (index < vertexCount)
        {
            IccGamutVertex vertex = new IccGamutVertex();
            vertex.PcsColor.Components = ReadCoordinateVector(coordinateReader, pcsChannels);
            SetPcsMetadata(vertex.PcsColor, state, pcsChannels);
            vertices[index] = vertex;
            index = index + 1;
        }

        index = 0;
        while (index < vertexCount)
        {
            vertices[index].DeviceColor.Components = ReadCoordinateVector(coordinateReader, deviceChannels);
            if (deviceChannels != 0)
            {
                vertices[index].DeviceColor.ColorSpace = state.DataColorSpace;
                vertices[index].DeviceColor.ColorSpaceSignature = state.DataColorSpaceSignature;
            }

            index = index + 1;
        }

        IccGamutBoundary boundary = new IccGamutBoundary();
        boundary.PcsChannels = pcsChannels;
        boundary.DeviceChannels = deviceChannels;
        boundary.Vertices = vertices;
        boundary.Triangles = triangles;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.GamutBoundary);
        data.GamutBoundary = boundary;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded a gamut boundary with {vertexCount} vertices and {triangleCount} faces.");
    }

    internal static byte[] SerializeGamutBoundary(IccGamutBoundary boundary)
    {
        string validationError = ValidateBoundaryForWrite(boundary);
        if (validationError.Length != 0)
        {
            throw new IccProfileWriteException(validationError);
        }

        long payloadSize = HeaderSize + ((long)boundary.Triangles.Length * 12L)
            + ((long)boundary.Vertices.Length * (boundary.PcsChannels + boundary.DeviceChannels) * 4L);
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The gamutBoundaryDescriptionType payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.GamutBoundaryDescription);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)boundary.PcsChannels);
        writer.WriteUInt16((ushort)boundary.DeviceChannels);
        writer.WriteUInt32((uint)boundary.Vertices.Length);
        writer.WriteUInt32((uint)boundary.Triangles.Length);
        int index = 0;
        while (index < boundary.Triangles.Length)
        {
            IccGamutTriangle triangle = boundary.Triangles[index];
            writer.WriteUInt32((uint)triangle.First);
            writer.WriteUInt32((uint)triangle.Second);
            writer.WriteUInt32((uint)triangle.Third);
            index = index + 1;
        }

        index = 0;
        while (index < boundary.Vertices.Length)
        {
            WriteCoordinateVector(writer, boundary.Vertices[index].PcsColor.Components);
            index = index + 1;
        }

        index = 0;
        while (index < boundary.Vertices.Length)
        {
            WriteCoordinateVector(writer, boundary.Vertices[index].DeviceColor.Components);
            index = index + 1;
        }

        return payload;
    }

    internal static string ValidateBoundaryForWrite(IccGamutBoundary boundary)
    {
        if (boundary == null || boundary.Vertices == null || boundary.Triangles == null)
        {
            return "A gamut boundary and its vertex and face arrays cannot be null.";
        }

        if (boundary.PcsChannels < 3 || boundary.PcsChannels > ushort.MaxValue
            || boundary.DeviceChannels < 0 || boundary.DeviceChannels > ushort.MaxValue)
        {
            return "A gamut boundary shall declare at least three PCS channels and valid unsigned 16-bit channel counts.";
        }

        if (boundary.Vertices.Length < 4 || boundary.Triangles.Length < 4)
        {
            return "A gamut boundary shall contain at least four vertices and four faces.";
        }

        string topologyError = ValidateMeshTopology(boundary.Triangles, boundary.Vertices.Length);
        if (topologyError.Length != 0)
        {
            return topologyError;
        }

        int index = 0;
        while (index < boundary.Vertices.Length)
        {
            IccGamutVertex vertex = boundary.Vertices[index];
            if (vertex == null)
            {
                return $"Gamut-boundary vertex {index} cannot be null.";
            }

            string pcsError = ValidateCoordinateVector(vertex.PcsColor, boundary.PcsChannels, $"vertex {index} PCS");
            if (pcsError.Length != 0)
            {
                return pcsError;
            }

            string deviceError = ValidateCoordinateVector(vertex.DeviceColor, boundary.DeviceChannels, $"vertex {index} device");
            if (deviceError.Length != 0)
            {
                return deviceError;
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static string ValidateMeshTopology(IccGamutTriangle[] triangles, int vertexCount)
    {
        if (triangles == null)
        {
            return "Gamut-boundary faces cannot be null.";
        }

        bool[] referenced = new bool[vertexCount];
        System.Collections.Generic.Dictionary<ulong, int> edgeCounts = new System.Collections.Generic.Dictionary<ulong, int>();
        System.Collections.Generic.Dictionary<ulong, int> edgeDirections = new System.Collections.Generic.Dictionary<ulong, int>();
        System.Collections.Generic.Dictionary<string, bool> faces = new System.Collections.Generic.Dictionary<string, bool>();
        int index = 0;
        while (index < triangles.Length)
        {
            IccGamutTriangle triangle = triangles[index];
            if (triangle == null)
            {
                return $"Gamut-boundary face {index} cannot be null.";
            }

            if (triangle.First < 0 || triangle.First >= vertexCount || triangle.Second < 0 || triangle.Second >= vertexCount
                || triangle.Third < 0 || triangle.Third >= vertexCount)
            {
                return $"Gamut-boundary face {index} contains an out-of-range vertex ID.";
            }

            if (triangle.First == triangle.Second || triangle.Second == triangle.Third || triangle.Third == triangle.First)
            {
                return $"Gamut-boundary face {index} shall reference three distinct vertices.";
            }

            string faceKey = CreateFaceKey(triangle);
            if (faces.ContainsKey(faceKey))
            {
                return $"Gamut-boundary face {index} duplicates another face.";
            }

            faces.Add(faceKey, true);
            referenced[triangle.First] = true;
            referenced[triangle.Second] = true;
            referenced[triangle.Third] = true;
            RecordTriangleEdge(edgeCounts, edgeDirections, triangle.First, triangle.Second);
            RecordTriangleEdge(edgeCounts, edgeDirections, triangle.Second, triangle.Third);
            RecordTriangleEdge(edgeCounts, edgeDirections, triangle.Third, triangle.First);
            index = index + 1;
        }

        index = 0;
        while (index < referenced.Length)
        {
            if (!referenced[index])
            {
                return $"Gamut-boundary vertex {index} is not referenced by any face.";
            }

            index = index + 1;
        }

        foreach (System.Collections.Generic.KeyValuePair<ulong, int> edge in edgeCounts)
        {
            if (edge.Value != 2 || edgeDirections[edge.Key] != 0)
            {
                return "Gamut-boundary faces shall form a closed, consistently oriented mesh.";
            }
        }

        return string.Empty;
    }

    internal static string ValidateTagContext(TagDecodeState state, int pcsChannels, int deviceChannels)
    {
        if (!state.HasProfileContext)
        {
            return string.Empty;
        }

        if (!IsGamutBoundaryTag(state.TagSignature))
        {
            return "A gamutBoundaryDescriptionType is permitted only for the gbd0, gbd1, gbd2, and gbd3 tags.";
        }

        if (state.ProfileClass != IccProfileClass.Input && state.ProfileClass != IccProfileClass.Display
            && state.ProfileClass != IccProfileClass.Output && state.ProfileClass != IccProfileClass.ColorSpace)
        {
            return "Gamut-boundary description tags are permitted only in Input, Display, Output, or ColorSpace profiles.";
        }

        int colorimetricChannels = ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
        int spectralChannels = (int)(state.SpectralPcsSignature & 0x0000ffff);
        if ((colorimetricChannels != 0 || spectralChannels != 0)
            && pcsChannels != colorimetricChannels && pcsChannels != spectralChannels)
        {
            return $"The gamut boundary declares {pcsChannels} PCS channels, which does not match a PCS in the profile header.";
        }

        int expectedDeviceChannels = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        if (deviceChannels != 0 && deviceChannels != expectedDeviceChannels)
        {
            return $"The gamut boundary declares {deviceChannels} device channels, but the profile data colour space has {expectedDeviceChannels}.";
        }

        return string.Empty;
    }

    internal static bool IsGamutBoundaryTag(uint tagSignature)
    {
        return tagSignature == IccTagSignatures.GamutBoundary0 || tagSignature == IccTagSignatures.GamutBoundary1
            || tagSignature == IccTagSignatures.GamutBoundary2 || tagSignature == IccTagSignatures.GamutBoundary3;
    }

    internal static void SetPcsMetadata(IccColorVector color, TagDecodeState state, int pcsChannels)
    {
        int colorimetricChannels = ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
        if (colorimetricChannels == pcsChannels)
        {
            color.ColorSpaceSignature = state.ProfileConnectionSpaceSignature;
            if (state.ProfileConnectionSpace == IccPcsKind.Xyz)
            {
                color.ColorSpace = IccColorSpaceKind.Xyz;
            }
            else if (state.ProfileConnectionSpace == IccPcsKind.Lab)
            {
                color.ColorSpace = IccColorSpaceKind.Lab;
            }

            return;
        }

        int spectralChannels = (int)(state.SpectralPcsSignature & 0x0000ffff);
        if (spectralChannels == pcsChannels)
        {
            color.ColorSpaceSignature = state.SpectralPcsSignature;
            color.ColorSpace = HeaderCodec.DecodeSpectralPcs(state.SpectralPcsSignature);
        }
    }

    internal static string ValidateCoordinateVector(IccColorVector color, int expectedCount, string name)
    {
        if (color == null || color.Components == null || color.Components.Length != expectedCount)
        {
            return $"The gamut-boundary {name} coordinate count shall be {expectedCount}.";
        }

        int index = 0;
        while (index < color.Components.Length)
        {
            double value = color.Components[index];
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return $"The gamut-boundary {name} coordinate {index} shall be finite.";
            }

            IccNumberCodec.ConvertDoubleToFloat32(value);
            index = index + 1;
        }

        return string.Empty;
    }

    internal static double[] ReadCoordinateVector(BigEndianReader reader, int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = IccNumberCodec.ReadFloat32(reader);
            index = index + 1;
        }

        return values;
    }

    internal static void WriteCoordinateVector(BigEndianWriter writer, double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            IccNumberCodec.WriteFloat32(writer, IccNumberCodec.ConvertDoubleToFloat32(values[index]));
            index = index + 1;
        }
    }

    internal static string CreateFaceKey(IccGamutTriangle triangle)
    {
        int first = triangle.First;
        int second = triangle.Second;
        int third = triangle.Third;
        if (first > second)
        {
            int swap = first;
            first = second;
            second = swap;
        }

        if (second > third)
        {
            int swap = second;
            second = third;
            third = swap;
        }

        if (first > second)
        {
            int swap = first;
            first = second;
            second = swap;
        }

        return $"{first}:{second}:{third}";
    }

    internal static void RecordTriangleEdge(System.Collections.Generic.Dictionary<ulong, int> counts,
        System.Collections.Generic.Dictionary<ulong, int> directions, int first, int second)
    {
        int lower = first < second ? first : second;
        int upper = first < second ? second : first;
        ulong key = ((ulong)(uint)lower << 32) | (uint)upper;
        int direction = first < second ? 1 : -1;
        if (counts.ContainsKey(key))
        {
            counts[key] = counts[key] + 1;
            directions[key] = directions[key] + direction;
        }
        else
        {
            counts.Add(key, 1);
            directions.Add(key, direction);
        }
    }

    internal static int ReadUInt16At(System.ReadOnlyMemory<byte> payload, int offset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 2), offset);
        return reader.ReadUInt16();
    }

    internal static uint ReadUInt32At(System.ReadOnlyMemory<byte> payload, int offset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 4), offset);
        return reader.ReadUInt32();
    }
}
