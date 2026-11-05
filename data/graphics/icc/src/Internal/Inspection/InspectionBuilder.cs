namespace icc;

internal static class InspectionBuilder
{
    internal static IccProfileSummary Build(IccProfile profile, IccInspectionOptions options)
    {
        ProfileDescriptionCore.ValidateProfile(profile);
        ValidateOptions(options);
        IccProfileSummary summary = new IccProfileSummary();
        summary.Headline = ProfileDescriptionCore.SummarizeProfile(profile);
        summary.Version = DescriptionFormatter.DescribeProfileVersion(profile.Header.Version);
        summary.ProfileClass = DescriptionFormatter.DescribeProfileClassKind(profile.Header.ProfileClass);
        summary.DataColorSpace = DescriptionFormatter.DescribeColorSpaceWithChannels(
            profile.Header.DataColorSpaceSignature, profile.Header.DataColorSpace);
        summary.ProfileConnectionSpace = DescriptionFormatter.DescribePcsKind(
            profile.Header.ProfileConnectionSpace);
        summary.TagCount = profile.Tags.Length;
        summary.Options = CopyOptions(options);
        summary.Validation = new ValidationReportCore().Build(profile, new IccValidationPolicy());
        summary.Root = BuildProfileNode(profile, summary.Validation, options, 0);
        return summary;
    }

    internal static IccInspectionNode BuildProfileNode(IccProfile profile,
        IccValidationReport validation, IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("Profile",
            $"{profile.Tags.Length} tags", "Profile", profile.Location);
        ApplyRawBytes(node, profile.OriginalBytes, options);
        System.Collections.Generic.List<IccInspectionNode> children =
            new System.Collections.Generic.List<IccInspectionNode>();
        int expected = options.Detail == IccInspectionDetail.Summary ? 1 : 5;
        int limit = GetChildLimit(node, expected, depth, options);
        if (limit > 0)
        {
            children.Add(BuildHeaderNode(profile.Header, options, depth + 1));
        }
        if (limit > 1)
        {
            children.Add(BuildTagTableNode(profile.TagTableEntries, options, depth + 1));
        }
        if (limit > 2)
        {
            children.Add(BuildTagsNode(profile.Tags, options, depth + 1));
        }
        if (limit > 3)
        {
            children.Add(BuildIssuesNode(validation.Issues, options, depth + 1));
        }
        if (limit > 4)
        {
            children.Add(BuildTransformPrecedenceNode(profile, options, depth + 1));
        }
        node.Children = children.ToArray();
        return node;
    }

    internal static IccInspectionNode BuildHeaderNode(IccProfileHeader header,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("Header",
            DescriptionFormatter.DescribeProfileVersion(header.Version), "Header",
            CreateLocation(0, header.RawBytes == null ? 0 : header.RawBytes.Length));
        byte[] rawBytes = header.RawBytes == null ? new byte[0] : header.RawBytes;
        ApplyRawBytes(node, rawBytes, options);
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        values.Add(CreateLeaf("DeclaredSize", FormatInteger(header.DeclaredSize), "Header.DeclaredSize"));
        values.Add(CreateLeaf("Version", DescriptionFormatter.DescribeProfileVersion(header.Version), "Header.Version"));
        values.Add(CreateLeaf("ProfileClass", DescriptionFormatter.DescribeProfileClassKind(header.ProfileClass)
            + "; " + FormatSignature(header.ProfileClassSignature), "Header.ProfileClass"));
        values.Add(CreateLeaf("DataColorSpace", DescriptionFormatter.DescribeColorSpaceWithChannels(
            header.DataColorSpaceSignature, header.DataColorSpace), "Header.DataColorSpace"));
        values.Add(CreateLeaf("ProfileConnectionSpace", DescriptionFormatter.DescribePcsKind(
            header.ProfileConnectionSpace) + "; " + FormatSignature(header.ProfileConnectionSpaceSignature),
            "Header.ProfileConnectionSpace"));
        values.Add(CreateLeaf("CreationDateTime", FormatDateTime(header.CreationDateTime),
            "Header.CreationDateTime"));
        values.Add(CreateLeaf("ProfileFileSignature", FormatSignature(header.ProfileFileSignature),
            "Header.ProfileFileSignature"));
        values.Add(CreateLeaf("RenderingIntent", DescriptionFormatter.DescribeRenderingIntentKind(
            header.RenderingIntentKind), "Header.RenderingIntent"));
        values.Add(CreateLeaf("ProfileFlags", FormatUnsigned(header.ProfileFlags), "Header.ProfileFlags"));
        values.Add(CreateLeaf("DeviceAttributes", FormatUnsignedLong(header.DeviceAttributes),
            "Header.DeviceAttributes"));
        if (options.Detail == IccInspectionDetail.Detailed || options.Detail == IccInspectionDetail.Raw)
        {
            values.Add(CreateLeaf("PreferredCmm", FormatSignature(header.PreferredCmmSignature),
                "Header.PreferredCmmSignature"));
            values.Add(CreateLeaf("PrimaryPlatform", FormatSignature(header.PrimaryPlatformSignature),
                "Header.PrimaryPlatformSignature"));
            values.Add(CreateLeaf("DeviceManufacturer", FormatSignature(header.DeviceManufacturerSignature),
                "Header.DeviceManufacturerSignature"));
            values.Add(CreateLeaf("DeviceModel", FormatSignature(header.DeviceModelSignature),
                "Header.DeviceModelSignature"));
            values.Add(CreateLeaf("ProfileCreator", FormatSignature(header.ProfileCreatorSignature),
                "Header.ProfileCreatorSignature"));
            values.Add(CreateLeaf("PcsIlluminant", $"{FormatInteger(header.PcsIlluminantX)}, {FormatInteger(header.PcsIlluminantY)}, {FormatInteger(header.PcsIlluminantZ)}",
                "Header.PcsIlluminant"));
            values.Add(CreateLeaf("SpectralPcs", FormatSignature(header.SpectralPcsSignature),
                "Header.SpectralPcsSignature"));
            values.Add(CreateLeaf("MaterialConnectionSpace", FormatSignature(
                header.MaterialConnectionSpaceSignature), "Header.MaterialConnectionSpaceSignature"));
            values.Add(CreateLeaf("ProfileSubclass", FormatSignature(header.ProfileSubclassSignature),
                "Header.ProfileSubclassSignature"));
        }
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static IccInspectionNode BuildTagTableNode(IccTagTableEntry[] entries,
        IccInspectionOptions options, int depth)
    {
        IccTagTableEntry[] source = entries == null ? new IccTagTableEntry[0] : entries;
        int count = source.Length;
        IccInspectionNode node = CreateNode("TagTable", $"{count} entries",
            "TagTableEntries", CreateLocation(128, 4 + (count * 12)));
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            IccTagTableEntry entry = source[index];
            string value = entry == null ? "null entry" : $"{FormatSignature(entry.Signature)}; offset={FormatLong(entry.Offset)}; size={FormatInteger(entry.Size)}; shared={FormatInteger(entry.SharedDataId)}";
            IccByteLocation location = entry == null ? new IccByteLocation()
                : CreateLocation(entry.Offset, entry.Size);
            IccInspectionNode child = CreateNode($"Entry[{index}]", value,
                $"TagTableEntries[{index}]", location);
            if (entry != null && entry.SharedDataId >= 0)
            {
                child.Children = new IccInspectionNode[]
                {
                    CreateLeaf("SharedPayload", $"identity {FormatInteger(entry.SharedDataId)}",
                        $"TagTableEntries[{index}].SharedDataId")
                };
            }
            children[index] = child;
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildTagsNode(IccTag[] tags,
        IccInspectionOptions options, int depth)
    {
        IccTag[] source = tags == null ? new IccTag[0] : tags;
        int count = source.Length;
        IccInspectionNode node = CreateNode("Tags", $"{count} decoded tags", "Tags",
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = BuildTagNode(source[index], index, options, depth + 1);
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildTagNode(IccTag tag, int index,
        IccInspectionOptions options, int depth)
    {
        if (tag == null)
        {
            return CreateLeaf($"Tag[{index}]", "null tag", $"Tags[{index}]");
        }
        string path = $"Tags[{index}]";
        IccTagData? data = tag.Data;
        IccTagTableEntry? entry = tag.TableEntry;
        IccInspectionNode node = CreateNode($"Tag[{index}]",
            $"{SignatureDatabase.DescribeTag(tag.Signature)}; {(data == null ? "data unavailable" : data.Kind.ToString())}", path,
            entry == null ? new IccByteLocation()
                : CreateLocation(entry.Offset, entry.Size));
        ApplyRawBytes(node, data == null || data.RawData == null
            ? new byte[0] : data.RawData.Bytes, options);
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        values.Add(CreateLeaf("Signature", FormatSignature(tag.Signature), path + ".Signature"));
        values.Add(CreateLeaf("Type", data != null && data.RawData != null
            && data.RawData.HasTypeSignature
            ? SignatureDatabase.DescribeType(data.RawData.TypeSignature) : "unavailable",
            path + ".Data.RawData.TypeSignature"));
        values.Add(CreateLeaf("SharedPayload", entry == null ? "unavailable"
            : entry.SharedDataId < 0 ? "not shared"
                : $"identity {FormatInteger(entry.SharedDataId)}",
            path + ".TableEntry.SharedDataId"));
        values.Add(BuildTagDataNode(data, path + ".Data", options, depth + 1));
        values.Add(CreateLeaf("DecodeResult", tag.DecodeResult == null ? "unavailable"
            : $"{tag.DecodeResult.Status}: {tag.DecodeResult.Message}",
            path + ".DecodeResult"));
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static IccInspectionNode BuildTagDataNode(IccTagData? data, string path,
        IccInspectionOptions options, int depth)
    {
        if (data == null)
        {
            return CreateLeaf("Data", "null", path);
        }
        IccInspectionNode node = CreateNode("Data", data.Kind.ToString(), path,
            new IccByteLocation());
        ApplyRawBytes(node, data.RawData == null ? new byte[0] : data.RawData.Bytes, options);
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        if (data.Curve != null)
        {
            values.Add(BuildCurveNode(data.Curve, "Curve", path + ".Curve", options, depth + 1));
        }
        if (data.Lut != null)
        {
            values.Add(BuildLutNode(data.Lut, path + ".Lut", options, depth + 1));
        }
        if (data.MultiStageLut != null)
        {
            values.Add(BuildMultiStageLutNode(data.MultiStageLut,
                path + ".MultiStageLut", options, depth + 1));
        }
        if (data.MultiProcessElements != null)
        {
            values.Add(BuildMpeNode(data.MultiProcessElements,
                path + ".MultiProcessElements", options, depth + 1));
        }
        if (data.TagArray != null)
        {
            values.Add(BuildTagArrayNode(data.TagArray, path + ".TagArray", options, depth + 1));
        }
        if (data.TagStructure != null)
        {
            values.Add(BuildTagStructureNode(data.TagStructure,
                path + ".TagStructure", options, depth + 1));
        }
        AddPrimitiveDataNodes(values, data, path, options, depth + 1);
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static void AddPrimitiveDataNodes(System.Collections.Generic.List<IccInspectionNode> values,
        IccTagData data, string path, IccInspectionOptions options, int depth)
    {
        if (data.IntegerValues != null && data.IntegerValues.Length > 0)
        {
            values.Add(BuildLongValuesNode("IntegerValues", data.IntegerValues,
                path + ".IntegerValues", options, depth));
        }
        if (data.FixedPointValues != null && data.FixedPointValues.Length > 0)
        {
            values.Add(BuildDoubleValuesNode("FixedPointValues", data.FixedPointValues,
                path + ".FixedPointValues", options, depth));
        }
        if (data.FloatingPointValues != null && data.FloatingPointValues.Length > 0)
        {
            values.Add(BuildDoubleValuesNode("FloatingPointValues", data.FloatingPointValues,
                path + ".FloatingPointValues", options, depth));
        }
        if (data.XyzValues != null && data.XyzValues.Length > 0)
        {
            values.Add(BuildXyzValuesNode(data.XyzValues, path + ".XyzValues", options, depth));
        }
        if (data.Text != null)
        {
            values.Add(CreateLeaf("Text", ProfileDescriptionCore.SelectTextValue(data.Text),
                path + ".Text"));
        }
        if (data.LocalizedStrings != null && data.LocalizedStrings.Length > 0)
        {
            values.Add(CreateLeaf("LocalizedText", DescriptionFormatter.DescribeLocalizedRecords(
                data.LocalizedStrings, options.MaximumChildren), path + ".LocalizedStrings"));
        }
        if (data.BinaryData != null && data.BinaryData.Length > 0)
        {
            IccInspectionNode binary = CreateLeaf("BinaryData", $"{data.BinaryData.Length} bytes",
                path + ".BinaryData");
            ApplyRawBytes(binary, data.BinaryData, options);
            values.Add(binary);
        }
        if (data.Signature != 0)
        {
            values.Add(CreateLeaf("Signature", FormatSignature(data.Signature), path + ".Signature"));
        }
    }

    internal static IccInspectionNode BuildCurveNode(IccCurve curve, string name, string path,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode(name, curve.Kind.ToString(), path,
            new IccByteLocation());
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        values.Add(CreateLeaf("Gamma", FormatDouble(curve.Gamma), path + ".Gamma"));
        values.Add(CreateLeaf("Domain", $"{FormatDouble(curve.InputMinimum)}..{FormatDouble(curve.InputMaximum)}",
            path + ".Domain"));
        if (curve.Samples != null && curve.Samples.Length > 0)
        {
            values.Add(BuildDoubleValuesNode("Samples", curve.Samples, path + ".Samples",
                options, depth + 1));
        }
        if (curve.Parametric != null)
        {
            IccInspectionNode parameters = BuildDoubleValuesNode("Parameters",
                curve.Parametric.Parameters, path + ".Parametric.Parameters", options, depth + 1);
            parameters.Value = $"function {FormatInteger(curve.Parametric.FunctionType)}; {parameters.Value}";
            values.Add(parameters);
        }
        if (curve.Segments != null && curve.Segments.Length > 0)
        {
            values.Add(BuildCurveSegmentsNode(curve.Segments, path + ".Segments",
                options, depth + 1));
        }
        if (curve.Analysis != null)
        {
            values.Add(CreateLeaf("Analysis",
                $"monotonic={curve.Analysis.IsMonotonic}; continuous={curve.Analysis.IsContinuous}; invertible={curve.Analysis.IsInvertible}; roundTrip={FormatDouble(curve.Analysis.MaximumRoundTripError)}",
                path + ".Analysis"));
        }
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static IccInspectionNode BuildCurveSegmentsNode(IccCurveSegment[] segments,
        string path, IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("Segments", $"{segments.Length} segments", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, segments.Length, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            IccCurveSegment segment = segments[index];
            children[index] = CreateLeaf($"Segment[{index}]", segment == null ? "null"
                : $"{segment.Kind}; breakpoint={FormatDouble(segment.BreakPoint)}; formula={FormatInteger(segment.FormulaType)}; parameters={segment.Parameters.Length}; samples={segment.Samples.Length}",
                $"{path}[{index}]");
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildLutNode(IccLut lut, string path,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("Lut",
            $"{lut.InputChannels} to {lut.OutputChannels} channels; {lut.PcsEncoding}", path,
            new IccByteLocation());
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        values.Add(BuildMatrixNode(lut.Matrix, path + ".Matrix", options, depth + 1));
        values.Add(BuildTableSetNode("InputTables", lut.InputTables,
            path + ".InputTables", options, depth + 1));
        values.Add(BuildClutNode(lut.Clut, path + ".Clut", options, depth + 1));
        values.Add(BuildTableSetNode("OutputTables", lut.OutputTables,
            path + ".OutputTables", options, depth + 1));
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static IccInspectionNode BuildMultiStageLutNode(IccMultiStageLut lut, string path,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("MultiStageLut",
            $"{lut.InputChannels} to {lut.OutputChannels} channels; {lut.PcsEncoding}", path,
            new IccByteLocation());
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        values.Add(BuildCurveSetNode("ACurves", lut.ACurves, path + ".ACurves", options, depth + 1));
        values.Add(BuildCurveSetNode("BCurves", lut.BCurves, path + ".BCurves", options, depth + 1));
        values.Add(BuildCurveSetNode("MCurves", lut.MCurves, path + ".MCurves", options, depth + 1));
        if (lut.Matrix != null)
        {
            values.Add(BuildMatrixNode(lut.Matrix, path + ".Matrix", options, depth + 1));
        }
        if (lut.Clut != null)
        {
            values.Add(BuildClutNode(lut.Clut, path + ".Clut", options, depth + 1));
        }
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static IccInspectionNode BuildMpeNode(IccMultiProcessElements pipeline, string path,
        IccInspectionOptions options, int depth)
    {
        IccProcessingElement[] source = pipeline.Elements == null
            ? new IccProcessingElement[0] : pipeline.Elements;
        int count = source.Length;
        IccInspectionNode node = CreateNode("MultiProcessElements",
            $"{pipeline.InputChannels} to {pipeline.OutputChannels} channels; {count} elements; executable={pipeline.IsExecutable}",
            path, new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = BuildMpeElementNode(source[index], index,
                path + $".Elements[{index}]", options, depth + 1);
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildMpeElementNode(IccProcessingElement element, int index,
        string path, IccInspectionOptions options, int depth)
    {
        if (element == null)
        {
            return CreateLeaf($"Element[{index}]", "null", path);
        }
        IccInspectionNode node = CreateNode($"Element[{index}]",
            $"{element.Kind}; {FormatSignature(element.Signature)}; {element.InputChannels} to {element.OutputChannels} channels",
            path, new IccByteLocation());
        ApplyRawBytes(node, element.RawData.Bytes, options);
        System.Collections.Generic.List<IccInspectionNode> values =
            new System.Collections.Generic.List<IccInspectionNode>();
        if (element.Curves != null && element.Curves.Length > 0)
        {
            values.Add(BuildCurveSetNode("Curves", element.Curves, path + ".Curves",
                options, depth + 1));
        }
        if (element.Matrix != null)
        {
            values.Add(BuildMatrixNode(element.Matrix, path + ".Matrix", options, depth + 1));
        }
        if (element.Clut != null)
        {
            values.Add(BuildClutNode(element.Clut, path + ".Clut", options, depth + 1));
        }
        if (element.ToneMap != null)
        {
            values.Add(BuildCurveNode(element.ToneMap.LuminanceCurve, "LuminanceCurve",
                path + ".ToneMap.LuminanceCurve", options, depth + 1));
        }
        AssignLimitedChildren(node, values, depth, options);
        return node;
    }

    internal static IccInspectionNode BuildTagArrayNode(IccTagArray array, string path,
        IccInspectionOptions options, int depth)
    {
        IccTagData[] source = array.Values == null ? new IccTagData[0] : array.Values;
        int count = source.Length;
        IccInspectionNode node = CreateNode("TagArray",
            $"{FormatSignature(array.ArraySignature)}; {count} values", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = BuildTagDataNode(source[index], $"{path}.Values[{index}]",
                options, depth + 1);
            children[index].Name = $"Value[{index}]";
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildTagStructureNode(IccTagStructure structure, string path,
        IccInspectionOptions options, int depth)
    {
        IccStructureMember[] source = structure.Members == null
            ? new IccStructureMember[0] : structure.Members;
        int count = source.Length;
        IccInspectionNode node = CreateNode("TagStructure",
            $"{FormatSignature(structure.StructureSignature)}; {count} members", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            IccStructureMember member = source[index];
            string memberPath = $"{path}.Members[{index}]";
            if (member == null)
            {
                children[index] = CreateLeaf($"Member[{index}]", "null", memberPath);
            }
            else
            {
                children[index] = BuildTagDataNode(member.Data, memberPath + ".Data",
                    options, depth + 1);
                children[index].Name = $"Member[{index}]";
                children[index].Value = $"{FormatSignature(member.Signature)}; {children[index].Value}";
            }
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildMatrixNode(IccMatrix matrix, string path,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("Matrix", matrix == null ? "null"
            : $"{matrix.Rows}x{matrix.Columns}", path, new IccByteLocation());
        if (matrix != null)
        {
            IccInspectionNode values = BuildDoubleValuesNode("Values", matrix.Values,
                path + ".Values", options, depth + 1);
            AssignLimitedChildren(node, new System.Collections.Generic.List<IccInspectionNode>
            {
                values
            }, depth, options);
        }
        return node;
    }

    internal static IccInspectionNode BuildClutNode(IccClut clut, string path,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("Clut", clut == null ? "null"
            : $"{clut.InputChannels} to {clut.OutputChannels} channels; {clut.Interpolation}",
            path, new IccByteLocation());
        if (clut != null)
        {
            System.Collections.Generic.List<IccInspectionNode> values =
                new System.Collections.Generic.List<IccInspectionNode>();
            values.Add(BuildIntValuesNode("GridPoints", clut.GridPoints,
                path + ".GridPoints", options, depth + 1));
            values.Add(BuildDoubleValuesNode("Values", clut.Values,
                path + ".Values", options, depth + 1));
            AssignLimitedChildren(node, values, depth, options);
        }
        return node;
    }

    internal static IccInspectionNode BuildCurveSetNode(string name, IccCurve[] curves, string path,
        IccInspectionOptions options, int depth)
    {
        IccCurve[] source = curves == null ? new IccCurve[0] : curves;
        int count = source.Length;
        IccInspectionNode node = CreateNode(name, $"{count} curves", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = BuildCurveNode(source[index], $"Curve[{index}]",
                $"{path}[{index}]", options, depth + 1);
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildTableSetNode(string name, double[][] tables, string path,
        IccInspectionOptions options, int depth)
    {
        double[][] source = tables == null ? new double[0][] : tables;
        int count = source.Length;
        IccInspectionNode node = CreateNode(name, $"{count} tables", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = BuildDoubleValuesNode($"Table[{index}]", source[index],
                $"{path}[{index}]", options, depth + 1);
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildDoubleValuesNode(string name, double[] values,
        string path, IccInspectionOptions options, int depth)
    {
        double[] source = values == null ? new double[0] : values;
        int count = source.Length;
        IccInspectionNode node = CreateNode(name, $"{count} values", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = CreateLeaf($"[{index}]", FormatDouble(source[index]),
                $"{path}[{index}]");
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildIntValuesNode(string name, int[] values,
        string path, IccInspectionOptions options, int depth)
    {
        int[] source = values == null ? new int[0] : values;
        int count = source.Length;
        IccInspectionNode node = CreateNode(name, $"{count} values", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = CreateLeaf($"[{index}]", FormatInteger(source[index]),
                $"{path}[{index}]");
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildLongValuesNode(string name, long[] values,
        string path, IccInspectionOptions options, int depth)
    {
        long[] source = values == null ? new long[0] : values;
        int count = source.Length;
        IccInspectionNode node = CreateNode(name, $"{count} values", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            children[index] = CreateLeaf($"[{index}]", FormatLong(source[index]),
                $"{path}[{index}]");
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildXyzValuesNode(IccXyzNumber[] values, string path,
        IccInspectionOptions options, int depth)
    {
        IccXyzNumber[] source = values == null ? new IccXyzNumber[0] : values;
        int count = source.Length;
        IccInspectionNode node = CreateNode("XyzValues", $"{count} values", path,
            new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            IccXyzNumber value = source[index];
            children[index] = CreateLeaf($"[{index}]", value == null ? "null"
                : $"X={FormatDouble(value.X)}, Y={FormatDouble(value.Y)}, Z={FormatDouble(value.Z)}",
                $"{path}[{index}]");
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildIssuesNode(IccValidationIssue[] issues,
        IccInspectionOptions options, int depth)
    {
        IccValidationIssue[] source = issues == null ? new IccValidationIssue[0] : issues;
        int count = source.Length;
        IccInspectionNode node = CreateNode("ValidationIssues", $"{count} issues",
            "Validation.Issues", new IccByteLocation());
        int limit = GetChildLimit(node, count, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            IccValidationIssue issue = source[index];
            children[index] = CreateNode($"Issue[{index}]", issue == null ? "null"
                : $"{issue.Severity} {issue.RuleId}: {issue.Message}", issue == null
                    ? $"Validation.Issues[{index}]" : issue.Path, issue == null
                    ? new IccByteLocation() : issue.Location);
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static IccInspectionNode BuildTransformPrecedenceNode(IccProfile profile,
        IccInspectionOptions options, int depth)
    {
        IccInspectionNode node = CreateNode("TransformPrecedence",
            "standard tag selection with fallback", "Transforms", new IccByteLocation());
        IccTransformDirection[] directions = new IccTransformDirection[]
        {
            IccTransformDirection.DeviceToPcs, IccTransformDirection.PcsToDevice
        };
        IccRenderingIntent[] intents = new IccRenderingIntent[]
        {
            IccRenderingIntent.Perceptual,
            IccRenderingIntent.MediaRelativeColorimetric,
            IccRenderingIntent.Saturation,
            IccRenderingIntent.IccAbsoluteColorimetric
        };
        int total = directions.Length * intents.Length;
        int limit = GetChildLimit(node, total, depth, options);
        IccInspectionNode[] children = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            IccTransformDirection direction = directions[index / intents.Length];
            IccRenderingIntent intent = intents[index % intents.Length];
            IntentSelection selection = ProfileDescriptionCore.SelectTransform(profile,
                direction, intent);
            string selected = selection.UsesMatrixTrc ? "matrix/TRC"
                : selection.TagSignature == 0 ? "none" : FormatSignature(selection.TagSignature);
            children[index] = CreateLeaf($"{direction}.{intent}",
                $"success={selection.Success}; selected={selected}; intent={selection.SelectedIntent}; fallback={selection.UsedFallback}; {selection.Message}",
                $"Transforms.{direction}.{intent}");
            index = index + 1;
        }
        node.Children = children;
        return node;
    }

    internal static void AssignLimitedChildren(IccInspectionNode node,
        System.Collections.Generic.List<IccInspectionNode> children, int depth,
        IccInspectionOptions options)
    {
        int limit = GetChildLimit(node, children.Count, depth, options);
        IccInspectionNode[] values = new IccInspectionNode[limit];
        int index = 0;
        while (index < limit)
        {
            values[index] = children[index];
            index = index + 1;
        }
        node.Children = values;
    }

    internal static int GetChildLimit(IccInspectionNode node, int count, int depth,
        IccInspectionOptions options)
    {
        if (count <= 0)
        {
            return 0;
        }
        if (depth >= options.MaximumDepth || options.MaximumChildren == 0)
        {
            node.IsTruncated = true;
            return 0;
        }
        if (count > options.MaximumChildren)
        {
            node.IsTruncated = true;
            return options.MaximumChildren;
        }
        return count;
    }

    internal static void ApplyRawBytes(IccInspectionNode node, byte[] bytes,
        IccInspectionOptions options)
    {
        if ((!options.IncludeRawBytes && options.Detail != IccInspectionDetail.Raw)
            || bytes == null || bytes.Length == 0)
        {
            return;
        }
        int length = bytes.Length < options.MaximumRawBytes
            ? bytes.Length : options.MaximumRawBytes;
        node.RawBytes = new byte[length];
        System.Array.Copy(bytes, node.RawBytes, length);
        if (length < bytes.Length)
        {
            node.IsTruncated = true;
        }
    }

    internal static IccInspectionNode CreateNode(string name, string value, string path,
        IccByteLocation location)
    {
        IccInspectionNode node = new IccInspectionNode();
        node.Name = name;
        node.Value = value == null ? string.Empty : value;
        node.Path = path;
        node.Location = CopyLocation(location);
        return node;
    }

    internal static IccInspectionNode CreateLeaf(string name, string value, string path)
    {
        return CreateNode(name, value, path, new IccByteLocation());
    }

    internal static IccByteLocation CreateLocation(long offset, long length)
    {
        IccByteLocation location = new IccByteLocation();
        location.Offset = offset;
        location.Length = length;
        return location;
    }

    internal static IccByteLocation CopyLocation(IccByteLocation source)
    {
        IccByteLocation location = new IccByteLocation();
        if (source != null)
        {
            location.Offset = source.Offset;
            location.Length = source.Length;
        }
        return location;
    }

    internal static IccInspectionOptions CopyOptions(IccInspectionOptions source)
    {
        IccInspectionOptions copy = new IccInspectionOptions();
        copy.Detail = source.Detail;
        copy.MaximumDepth = source.MaximumDepth;
        copy.MaximumChildren = source.MaximumChildren;
        copy.MaximumRawBytes = source.MaximumRawBytes;
        copy.IncludeRawBytes = source.IncludeRawBytes;
        return copy;
    }

    internal static void ValidateOptions(IccInspectionOptions options)
    {
        if (options == null)
        {
            throw new IccProfileValidationException("Inspection options cannot be null.");
        }
        ProfileDescriptionCore.ValidateInspectionArguments(options.Detail,
            options.MaximumChildren);
        if (options.MaximumDepth < 0 || options.MaximumRawBytes < 0)
        {
            throw new IccProfileValidationException(
                "Inspection depth and raw-byte limits cannot be negative.");
        }
    }

    internal static string FormatSignature(uint value)
    {
        return SignatureDatabase.FormatDescription("FourCC", value);
    }

    internal static string FormatDouble(double value)
    {
        return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatInteger(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatLong(long value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatUnsigned(uint value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatUnsignedLong(ulong value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string FormatDateTime(IccDateTime value)
    {
        if (value == null)
        {
            return "null";
        }
        return $"{value.Year:D4}-{value.Month:D2}-{value.Day:D2}T{value.Hour:D2}:{value.Minute:D2}:{value.Second:D2}";
    }
}
