namespace raw;

/// <summary>Reads and writes bounded, versioned JSON sidecars for generic raw descriptors.</summary>
internal static class RawSidecarCodec
{
    /// <summary>The sidecar schema version written and understood by this release.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Reads a sidecar from an exact UTF-8 JSON byte array.</summary>
    /// <param name="json">The complete sidecar bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed descriptor and preserved extension properties.</returns>
    /// <exception cref="RawFormatException">The sidecar schema or descriptor is invalid.</exception>
    /// <exception cref="RawLimitException">The sidecar exceeds a configured limit.</exception>
    /// <exception cref="RawUnsupportedException">The sidecar uses an unsupported schema version.</exception>
    public static RawSidecar Read(byte[] json, RawLimits limits)
    {
        if (json == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Sidecar JSON bytes are required.", -1);
        }
        return ReadMemory(json, limits, default(System.Threading.CancellationToken));
    }

    internal static RawSidecar ReadMemory(System.ReadOnlyMemory<byte> json, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        RequireSize(json.Length, limits);
        System.Text.Json.JsonDocumentOptions options = new System.Text.Json.JsonDocumentOptions();
        options.AllowTrailingCommas = false;
        options.CommentHandling = System.Text.Json.JsonCommentHandling.Disallow;
        options.MaxDepth = limits.MaxRecursionDepth;
        using (System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json, options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            RawSidecar sidecar = ReadRoot(document.RootElement, limits, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return sidecar;
        }
    }

    /// <summary>Reads a sidecar from the caller-owned stream without closing it.</summary>
    /// <param name="input">The readable sidecar stream.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed descriptor and preserved extension properties.</returns>
    /// <exception cref="RawFormatException">The stream or sidecar schema is invalid.</exception>
    /// <exception cref="RawLimitException">The sidecar exceeds a configured limit.</exception>
    /// <exception cref="RawUnsupportedException">The sidecar uses an unsupported schema version.</exception>
    public static RawSidecar Read(System.IO.Stream input, RawLimits limits)
    {
        ValidateLimits(limits);
        if (input == null || !input.CanRead)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A readable sidecar stream is required.", -1);
        }
        long maximum = MaximumSize(limits);
        int bufferLength = maximum < 8192 ? (int)maximum : 8192;
        byte[] buffer = new byte[bufferLength];
        using (System.IO.MemoryStream collected = new System.IO.MemoryStream(bufferLength))
        {
            while (collected.Length < maximum)
            {
                long remaining = maximum - collected.Length;
                int requested = remaining < buffer.Length ? (int)remaining : buffer.Length;
                int read = input.Read(buffer, 0, requested);
                if (read == 0)
                {
                    return Read(collected.ToArray(), limits);
                }
                collected.Write(buffer, 0, read);
            }
            int extra = input.Read(buffer, 0, 1);
            if (extra != 0)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The JSON sidecar exceeds the configured metadata or allocation limit.", -1);
            }
            return Read(collected.ToArray(), limits);
        }
    }

    internal static RawSidecar ReadStream(System.IO.Stream input, long offset, long length, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        RequireSize(length, limits);
        byte[] json = RawCodec.ReadBoundedStream(input, offset, length, limits, cancellationToken);
        return ReadMemory(json, limits, cancellationToken);
    }

    /// <summary>Writes a sidecar to a deterministic UTF-8 JSON byte array.</summary>
    /// <param name="sidecar">The sidecar document.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The complete sidecar bytes.</returns>
    /// <exception cref="RawWriteException">The sidecar version, descriptor, or extension set is invalid.</exception>
    /// <exception cref="RawLimitException">The encoded sidecar exceeds a configured limit.</exception>
    public static byte[] Write(RawSidecar sidecar, RawLimits limits)
    {
        return WriteBytes(sidecar, limits, default(System.Threading.CancellationToken));
    }

    internal static byte[] WriteBytes(RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateWrite(sidecar, limits, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            System.Text.Json.JsonWriterOptions options = new System.Text.Json.JsonWriterOptions();
            options.Indented = true;
            options.MaxDepth = limits.MaxRecursionDepth;
            using (System.Text.Json.Utf8JsonWriter writer = new System.Text.Json.Utf8JsonWriter(output, options))
            {
                WriteRoot(writer, sidecar, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                writer.Flush();
            }
            RequireSize(output.Length, limits);
            cancellationToken.ThrowIfCancellationRequested();
            return output.ToArray();
        }
    }

    /// <summary>Writes a sidecar to the caller-owned stream without closing it.</summary>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="sidecar">The sidecar document.</param>
    /// <param name="limits">The resource limits.</param>
    /// <exception cref="RawWriteException">The stream, sidecar version, descriptor, or extension set is invalid.</exception>
    /// <exception cref="RawLimitException">The encoded sidecar exceeds a configured limit.</exception>
    public static void Write(System.IO.Stream output, RawSidecar sidecar, RawLimits limits)
    {
        WriteStream(output, sidecar, limits, default(System.Threading.CancellationToken));
    }

    internal static void WriteStream(System.IO.Stream output, RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (output == null || !output.CanWrite)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A writable sidecar stream is required.", -1);
        }
        byte[] json = WriteBytes(sidecar, limits, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        output.Write(json, 0, json.Length);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal static RawSidecar ReadRoot(System.Text.Json.JsonElement root, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RequireKind(root, System.Text.Json.JsonValueKind.Object, "The sidecar root must be a JSON object.");
        int version = 0;
        RawDescriptor? descriptor = null;
        bool hasVersion = false;
        bool hasDescriptor = false;
        int propertyCount = 0;
        System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement> unknown = new System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>(System.StringComparer.Ordinal);
        foreach (System.Text.Json.JsonProperty property in root.EnumerateObject())
        {
            cancellationToken.ThrowIfCancellationRequested();
            propertyCount++;
            if (propertyCount > limits.MaxTagCount)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The JSON sidecar property count exceeds the configured tag-count limit.", -1);
            }
            if (!names.Add(property.Name))
            {
                throw Format("The JSON sidecar contains a duplicate top-level property.");
            }
            if (property.Name == "schemaVersion")
            {
                RequireKind(property.Value, System.Text.Json.JsonValueKind.Number, "The sidecar schemaVersion must be an integer.");
                version = property.Value.GetInt32();
                hasVersion = true;
            }
            else if (property.Name == "descriptor")
            {
                descriptor = ReadDescriptor(property.Value, limits, cancellationToken);
                hasDescriptor = true;
            }
            else
            {
                unknown.Add(property.Name, property.Value.Clone());
            }
        }
        if (!hasVersion || !hasDescriptor)
        {
            throw Format("The JSON sidecar requires schemaVersion and descriptor properties.");
        }
        if (version != CurrentSchemaVersion)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "The JSON sidecar schema version is not supported.", -1);
        }
        RawValidationReport report = (descriptor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Validate(limits);
        if (!report.IsValid())
        {
            throw Format("The JSON sidecar contains an invalid generic raw descriptor.");
        }
        return new RawSidecar(version, descriptor, unknown);
    }

    internal static RawDescriptor ReadDescriptor(System.Text.Json.JsonElement element, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RequireKind(element, System.Text.Json.JsonValueKind.Object, "The sidecar descriptor must be a JSON object.");
        RawDescriptor descriptor = new RawDescriptor();
        System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int seen = 0;
        foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!names.Add(property.Name))
            {
                throw Format("The JSON sidecar descriptor contains a duplicate property.");
            }
            seen++;
            if (property.Name == "width")
            {
                descriptor.Width = Integer(property.Value, "width");
            }
            else if (property.Name == "height")
            {
                descriptor.Height = Integer(property.Value, "height");
            }
            else if (property.Name == "bitDepth")
            {
                descriptor.BitDepth = Integer(property.Value, "bitDepth");
            }
            else if (property.Name == "storedWordBits")
            {
                descriptor.StoredWordBits = Integer(property.Value, "storedWordBits");
            }
            else if (property.Name == "sampleKind")
            {
                descriptor.SampleKind = SampleKind(Text(property.Value, "sampleKind"));
            }
            else if (property.Name == "packing")
            {
                descriptor.Packing = Packing(Text(property.Value, "packing"));
            }
            else if (property.Name == "byteOrder")
            {
                descriptor.ByteOrder = ByteOrder(Text(property.Value, "byteOrder"));
            }
            else if (property.Name == "bitOrder")
            {
                descriptor.BitOrder = BitOrder(Text(property.Value, "bitOrder"));
            }
            else if (property.Name == "layout")
            {
                descriptor.Layout = SampleLayout(Text(property.Value, "layout"));
            }
            else if (property.Name == "channelCount")
            {
                descriptor.ChannelCount = Integer(property.Value, "channelCount");
            }
            else if (property.Name == "channels")
            {
                descriptor.Channels = ReadChannels(property.Value, cancellationToken);
            }
            else if (property.Name == "photometric")
            {
                descriptor.Photometric = Photometric(Text(property.Value, "photometric"));
            }
            else if (property.Name == "cfa")
            {
                descriptor.Cfa = ReadCfa(property.Value, cancellationToken);
            }
            else if (property.Name == "orientation")
            {
                descriptor.Orientation = Orientation(Text(property.Value, "orientation"));
            }
            else if (property.Name == "rowStrideBytes")
            {
                descriptor.RowStrideBytes = LongInteger(property.Value, "rowStrideBytes");
            }
            else if (property.Name == "planeStrideBytes")
            {
                descriptor.PlaneStrideBytes = LongInteger(property.Value, "planeStrideBytes");
            }
            else if (property.Name == "dataOffset")
            {
                descriptor.DataOffset = LongInteger(property.Value, "dataOffset");
            }
            else if (property.Name == "dataLength")
            {
                descriptor.DataLength = LongInteger(property.Value, "dataLength");
            }
            else if (property.Name == "allowTrailingBytes")
            {
                descriptor.AllowTrailingBytes = Boolean(property.Value, "allowTrailingBytes");
            }
            else
            {
                throw Format("The JSON sidecar descriptor contains an unknown property.");
            }
        }
        if (seen != 19)
        {
            throw Format("The JSON sidecar descriptor is missing one or more required properties.");
        }
        return descriptor;
    }

    internal static RawChannelDescription[] ReadChannels(System.Text.Json.JsonElement element, System.Threading.CancellationToken cancellationToken)
    {
        RequireKind(element, System.Text.Json.JsonValueKind.Array, "The descriptor channels must be an array.");
        int count = element.GetArrayLength();
        if (count < 1 || count > 64)
        {
            throw Format("The descriptor channels array must contain one through sixty-four entries.");
        }
        RawChannelDescription[] channels = new RawChannelDescription[count];
        int index = 0;
        foreach (System.Text.Json.JsonElement item in element.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequireKind(item, System.Text.Json.JsonValueKind.Object, "Each descriptor channel must be an object.");
            string? name = null;
            RawChannelKind kind = RawChannelKind.Unknown;
            int seen = 0;
            System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach (System.Text.Json.JsonProperty property in item.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!names.Add(property.Name))
                {
                    throw Format("A descriptor channel contains a duplicate property.");
                }
                seen++;
                if (property.Name == "name")
                {
                    name = Text(property.Value, "channel name");
                }
                else if (property.Name == "kind")
                {
                    kind = ChannelKind(Text(property.Value, "channel kind"));
                }
                else
                {
                    throw Format("A descriptor channel contains an unknown property.");
                }
            }
            if (seen != 2 || name == null)
            {
                throw Format("Each descriptor channel requires name and kind properties.");
            }
            channels[index] = new RawChannelDescription(name, kind);
            index++;
        }
        return channels;
    }

    internal static RawCfaPattern? ReadCfa(System.Text.Json.JsonElement element, System.Threading.CancellationToken cancellationToken)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Null)
        {
            return null;
        }
        RequireKind(element, System.Text.Json.JsonValueKind.Object, "The descriptor cfa must be an object or null.");
        RawCfaLayout layout = RawCfaLayout.None;
        int repeatWidth = 0;
        int repeatHeight = 0;
        RawCfaColor[]? colors = null;
        int seen = 0;
        System.Collections.Generic.HashSet<string> names = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        foreach (System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!names.Add(property.Name))
            {
                throw Format("The descriptor cfa contains a duplicate property.");
            }
            seen++;
            if (property.Name == "layout")
            {
                layout = CfaLayout(Text(property.Value, "cfa layout"));
            }
            else if (property.Name == "repeatWidth")
            {
                repeatWidth = Integer(property.Value, "cfa repeatWidth");
            }
            else if (property.Name == "repeatHeight")
            {
                repeatHeight = Integer(property.Value, "cfa repeatHeight");
            }
            else if (property.Name == "colors")
            {
                colors = ReadColors(property.Value, cancellationToken);
            }
            else
            {
                throw Format("The descriptor cfa contains an unknown property.");
            }
        }
        if (seen != 4 || colors == null)
        {
            throw Format("The descriptor cfa requires layout, repeatWidth, repeatHeight, and colors properties.");
        }
        return new RawCfaPattern(layout, repeatWidth, repeatHeight, colors);
    }

    internal static RawCfaColor[] ReadColors(System.Text.Json.JsonElement element, System.Threading.CancellationToken cancellationToken)
    {
        RequireKind(element, System.Text.Json.JsonValueKind.Array, "The cfa colors must be an array.");
        int count = element.GetArrayLength();
        if (count < 1 || count > 4096)
        {
            throw Format("The cfa colors array must contain one through four thousand ninety-six entries.");
        }
        RawCfaColor[] colors = new RawCfaColor[count];
        int index = 0;
        foreach (System.Text.Json.JsonElement item in element.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            colors[index] = CfaColor(Text(item, "cfa color"));
            index++;
        }
        return colors;
    }

    internal static void WriteRoot(System.Text.Json.Utf8JsonWriter writer, RawSidecar sidecar, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", sidecar.SchemaVersion);
        writer.WritePropertyName("descriptor");
        WriteDescriptor(writer, sidecar.Descriptor, cancellationToken);
        string[] names = new string[sidecar.UnknownProperties.Count];
        sidecar.UnknownProperties.Keys.CopyTo(names, 0);
        System.Array.Sort(names, System.StringComparer.Ordinal);
        int index = 0;
        while (index < names.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = names[index];
            writer.WritePropertyName(name);
            sidecar.UnknownProperties[name].WriteTo(writer);
            index++;
        }
        writer.WriteEndObject();
    }

    internal static void WriteDescriptor(System.Text.Json.Utf8JsonWriter writer, RawDescriptor descriptor, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        writer.WriteStartObject();
        writer.WriteNumber("width", descriptor.Width);
        writer.WriteNumber("height", descriptor.Height);
        writer.WriteNumber("bitDepth", descriptor.BitDepth);
        writer.WriteNumber("storedWordBits", descriptor.StoredWordBits);
        writer.WriteString("sampleKind", descriptor.SampleKind.ToString());
        writer.WriteString("packing", descriptor.Packing.ToString());
        writer.WriteString("byteOrder", descriptor.ByteOrder.ToString());
        writer.WriteString("bitOrder", descriptor.BitOrder.ToString());
        writer.WriteString("layout", descriptor.Layout.ToString());
        writer.WriteNumber("channelCount", descriptor.ChannelCount);
        writer.WriteStartArray("channels");
        int channelIndex = 0;
        while (channelIndex < descriptor.Channels.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RawChannelDescription channel = descriptor.Channels[channelIndex];
            writer.WriteStartObject();
            writer.WriteString("name", channel.Name);
            writer.WriteString("kind", channel.Kind.ToString());
            writer.WriteEndObject();
            channelIndex++;
        }
        writer.WriteEndArray();
        writer.WriteString("photometric", descriptor.Photometric.ToString());
        if (descriptor.Cfa == null)
        {
            writer.WriteNull("cfa");
        }
        else
        {
            writer.WriteStartObject("cfa");
            writer.WriteString("layout", descriptor.Cfa.Layout.ToString());
            writer.WriteNumber("repeatWidth", descriptor.Cfa.RepeatWidth);
            writer.WriteNumber("repeatHeight", descriptor.Cfa.RepeatHeight);
            writer.WriteStartArray("colors");
            int colorIndex = 0;
            while (colorIndex < descriptor.Cfa.Colors.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.WriteStringValue(descriptor.Cfa.Colors[colorIndex].ToString());
                colorIndex++;
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteString("orientation", descriptor.Orientation.ToString());
        writer.WriteNumber("rowStrideBytes", descriptor.RowStrideBytes);
        writer.WriteNumber("planeStrideBytes", descriptor.PlaneStrideBytes);
        writer.WriteNumber("dataOffset", descriptor.DataOffset);
        writer.WriteNumber("dataLength", descriptor.DataLength);
        writer.WriteBoolean("allowTrailingBytes", descriptor.AllowTrailingBytes);
        writer.WriteEndObject();
    }

    internal static void ValidateWrite(RawSidecar sidecar, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateLimits(limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (sidecar == null || sidecar.Descriptor == null || sidecar.UnknownProperties == null)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A complete sidecar document is required.", -1);
        }
        if (sidecar.SchemaVersion != CurrentSchemaVersion)
        {
            throw new RawWriteException(RawDiagnosticCode.UnsupportedFormat, "Only the current JSON sidecar schema version can be written.", -1);
        }
        if (!sidecar.Descriptor.Validate(limits).IsValid())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidDescriptor, "The JSON sidecar descriptor is invalid.", -1);
        }
        if (limits.MaxTagCount < 2 || sidecar.UnknownProperties.Count > limits.MaxTagCount - 2)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The JSON sidecar property count exceeds the configured tag-count limit.", -1);
        }
        foreach (System.Collections.Generic.KeyValuePair<string, System.Text.Json.JsonElement> property in sidecar.UnknownProperties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (property.Key == null || property.Key.Length == 0 || property.Key == "schemaVersion" || property.Key == "descriptor" || property.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Unknown sidecar properties require unique non-reserved names and defined JSON values.", -1);
            }
            if (JsonDepth(property.Value, 2, cancellationToken) > limits.MaxRecursionDepth)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "An unknown JSON sidecar property exceeds the configured recursion-depth limit.", -1);
            }
        }
    }

    internal static int JsonDepth(System.Text.Json.JsonElement value, int depth, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int maximum = depth;
        if (value.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (System.Text.Json.JsonElement item in value.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                int itemDepth = JsonDepth(item, depth + 1, cancellationToken);
                if (itemDepth > maximum)
                {
                    maximum = itemDepth;
                }
            }
        }
        else if (value.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (System.Text.Json.JsonProperty property in value.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                int propertyDepth = JsonDepth(property.Value, depth + 1, cancellationToken);
                if (propertyDepth > maximum)
                {
                    maximum = propertyDepth;
                }
            }
        }
        return maximum;
    }

    internal static void ValidateLimits(RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit(limits.MaxFileSizeBytes, "The configured file-size limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxAllocationBytes, "The configured allocation limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxRecursionDepth, "The configured recursion-depth limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
    }

    internal static long MaximumSize(RawLimits limits)
    {
        long maximum = limits.MaxFileSizeBytes < limits.MaxMetadataBytes ? limits.MaxFileSizeBytes : limits.MaxMetadataBytes;
        if (limits.MaxAllocationBytes < maximum)
        {
            maximum = limits.MaxAllocationBytes;
        }
        return maximum < int.MaxValue ? maximum : int.MaxValue;
    }

    internal static void RequireSize(long size, RawLimits limits)
    {
        if (size > MaximumSize(limits))
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The JSON sidecar exceeds the configured metadata or allocation limit.", -1);
        }
    }

    internal static int Integer(System.Text.Json.JsonElement value, string name)
    {
        RequireKind(value, System.Text.Json.JsonValueKind.Number, "The descriptor integer property has the wrong JSON type.");
        return value.GetInt32();
    }

    internal static long LongInteger(System.Text.Json.JsonElement value, string name)
    {
        RequireKind(value, System.Text.Json.JsonValueKind.Number, "The descriptor long integer property has the wrong JSON type.");
        return value.GetInt64();
    }

    internal static bool Boolean(System.Text.Json.JsonElement value, string name)
    {
        if (value.ValueKind != System.Text.Json.JsonValueKind.True && value.ValueKind != System.Text.Json.JsonValueKind.False)
        {
            throw Format("The descriptor Boolean property has the wrong JSON type.");
        }
        return value.GetBoolean();
    }

    internal static string Text(System.Text.Json.JsonElement value, string name)
    {
        RequireKind(value, System.Text.Json.JsonValueKind.String, "The descriptor text property has the wrong JSON type.");
        string? result = value.GetString();
        if (result == null)
        {
            throw Format("The descriptor text property cannot be null.");
        }
        return result;
    }

    internal static void RequireKind(System.Text.Json.JsonElement value, System.Text.Json.JsonValueKind expected, string message)
    {
        if (value.ValueKind != expected)
        {
            throw Format(message);
        }
    }

    internal static RawSampleKind SampleKind(string value)
    {
        if (value == "UnsignedInteger") return RawSampleKind.UnsignedInteger;
        if (value == "SignedInteger") return RawSampleKind.SignedInteger;
        if (value == "Half") return RawSampleKind.Half;
        if (value == "Single") return RawSampleKind.Single;
        throw Format("The descriptor sampleKind value is unknown.");
    }

    internal static RawPacking Packing(string value)
    {
        if (value == "ByteAligned") return RawPacking.ByteAligned;
        if (value == "PackedBits") return RawPacking.PackedBits;
        if (value == "Raw10") return RawPacking.Raw10;
        if (value == "Raw12") return RawPacking.Raw12;
        if (value == "Raw14") return RawPacking.Raw14;
        throw Format("The descriptor packing value is unknown.");
    }

    internal static RawByteOrder ByteOrder(string value)
    {
        if (value == "LittleEndian") return RawByteOrder.LittleEndian;
        if (value == "BigEndian") return RawByteOrder.BigEndian;
        throw Format("The descriptor byteOrder value is unknown.");
    }

    internal static RawBitOrder BitOrder(string value)
    {
        if (value == "MostSignificantBitFirst") return RawBitOrder.MostSignificantBitFirst;
        if (value == "LeastSignificantBitFirst") return RawBitOrder.LeastSignificantBitFirst;
        throw Format("The descriptor bitOrder value is unknown.");
    }

    internal static RawSampleLayout SampleLayout(string value)
    {
        if (value == "Interleaved") return RawSampleLayout.Interleaved;
        if (value == "Planar") return RawSampleLayout.Planar;
        throw Format("The descriptor layout value is unknown.");
    }

    internal static RawChannelKind ChannelKind(string value)
    {
        if (value == "Unknown") return RawChannelKind.Unknown;
        if (value == "Gray") return RawChannelKind.Gray;
        if (value == "Red") return RawChannelKind.Red;
        if (value == "Green") return RawChannelKind.Green;
        if (value == "Blue") return RawChannelKind.Blue;
        if (value == "Alpha") return RawChannelKind.Alpha;
        if (value == "Cyan") return RawChannelKind.Cyan;
        if (value == "Magenta") return RawChannelKind.Magenta;
        if (value == "Yellow") return RawChannelKind.Yellow;
        if (value == "White") return RawChannelKind.White;
        if (value == "Custom") return RawChannelKind.Custom;
        throw Format("A descriptor channel kind is unknown.");
    }

    internal static RawPhotometricInterpretation Photometric(string value)
    {
        if (value == "Unknown") return RawPhotometricInterpretation.Unknown;
        if (value == "WhiteIsZero") return RawPhotometricInterpretation.WhiteIsZero;
        if (value == "BlackIsZero") return RawPhotometricInterpretation.BlackIsZero;
        if (value == "Rgb") return RawPhotometricInterpretation.Rgb;
        if (value == "Palette") return RawPhotometricInterpretation.Palette;
        if (value == "YCbCr") return RawPhotometricInterpretation.YCbCr;
        if (value == "ColorFilterArray") return RawPhotometricInterpretation.ColorFilterArray;
        if (value == "LinearRaw") return RawPhotometricInterpretation.LinearRaw;
        throw Format("The descriptor photometric value is unknown.");
    }

    internal static RawCfaLayout CfaLayout(string value)
    {
        if (value == "None") return RawCfaLayout.None;
        if (value == "Rggb") return RawCfaLayout.Rggb;
        if (value == "Bggr") return RawCfaLayout.Bggr;
        if (value == "Grbg") return RawCfaLayout.Grbg;
        if (value == "Gbrg") return RawCfaLayout.Gbrg;
        if (value == "Arbitrary") return RawCfaLayout.Arbitrary;
        throw Format("The descriptor cfa layout value is unknown.");
    }

    internal static RawCfaColor CfaColor(string value)
    {
        if (value == "Unknown") return RawCfaColor.Unknown;
        if (value == "Red") return RawCfaColor.Red;
        if (value == "Green") return RawCfaColor.Green;
        if (value == "Blue") return RawCfaColor.Blue;
        if (value == "Green2") return RawCfaColor.Green2;
        if (value == "Cyan") return RawCfaColor.Cyan;
        if (value == "Magenta") return RawCfaColor.Magenta;
        if (value == "Yellow") return RawCfaColor.Yellow;
        if (value == "White") return RawCfaColor.White;
        throw Format("A descriptor cfa color value is unknown.");
    }

    internal static RawOrientation Orientation(string value)
    {
        if (value == "Unknown") return RawOrientation.Unknown;
        if (value == "Normal") return RawOrientation.Normal;
        if (value == "MirrorHorizontal") return RawOrientation.MirrorHorizontal;
        if (value == "Rotate180") return RawOrientation.Rotate180;
        if (value == "MirrorVertical") return RawOrientation.MirrorVertical;
        if (value == "Transpose") return RawOrientation.Transpose;
        if (value == "Rotate90Clockwise") return RawOrientation.Rotate90Clockwise;
        if (value == "Transverse") return RawOrientation.Transverse;
        if (value == "Rotate270Clockwise") return RawOrientation.Rotate270Clockwise;
        throw Format("The descriptor orientation value is unknown.");
    }

    internal static RawFormatException Format(string message)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidDescriptor, message, -1);
    }
}
