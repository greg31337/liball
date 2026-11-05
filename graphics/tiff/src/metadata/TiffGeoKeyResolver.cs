namespace tiff;

internal static class TiffGeoKeyResolver
{
    internal const int UserDefinedCode = 32767;

    internal static TiffGeoKeyDirectory Resolve(TiffGeoKeyDirectory directory, TiffGeoParameterMetadata parameters, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffGeoKeyDirectory resolved = directory == null ? new TiffGeoKeyDirectory() : directory.Copy();
        TiffGeoParameterMetadata actualParameters = parameters == null ? new TiffGeoParameterMetadata() : parameters;
        ResetSemantics(resolved);
        int index = 0;
        while (index < resolved.Keys.Length)
        {
            if (HasEarlierKey(resolved.Keys, index, resolved.Keys[index].KeyId))
            {
                diagnostics.AddErrorForTag(TiffErrorCode.DuplicateTag, "GeoKeyDirectoryTag contains a duplicate key identifier.", imageIndex, TiffTagId.GeoKeyDirectory);
            }

            ResolveEntry(resolved.Keys[index], actualParameters, imageIndex, diagnostics);
            index = index + 1;
        }

        ApplyModelType(resolved, imageIndex, diagnostics);
        ApplyRasterType(resolved, imageIndex, diagnostics);
        ApplyEpsgCodes(resolved, imageIndex, diagnostics);
        resolved.UserDefinedCrsName = FindUserDefinedCitation(resolved);
        resolved.PrivateKeys = CollectPrivateKeys(resolved.Keys);
        ValidateKeyModelConsistency(resolved, imageIndex, diagnostics);
        return resolved;
    }

    internal static void ResetSemantics(TiffGeoKeyDirectory directory)
    {
        directory.ModelType = TiffGeoModelType.Unknown;
        directory.RasterType = TiffGeoRasterType.Unknown;
        directory.HasGeographicCrsEpsgCode = false;
        directory.GeographicCrsEpsgCode = 0;
        directory.HasProjectedCrsEpsgCode = false;
        directory.ProjectedCrsEpsgCode = 0;
        directory.HasVerticalCrsEpsgCode = false;
        directory.VerticalCrsEpsgCode = 0;
        directory.HasUserDefinedCrs = false;
        directory.UserDefinedCrsName = "";
        directory.PrivateKeys = new TiffGeoKeyEntry[0];
    }

    internal static bool HasEarlierKey(TiffGeoKeyEntry[] keys, int endIndex, int keyId)
    {
        int index = 0;
        while (index < endIndex)
        {
            if (keys[index] != null && keys[index].KeyId == keyId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ResolveEntry(TiffGeoKeyEntry entry, TiffGeoParameterMetadata parameters, int imageIndex, TiffDiagnostics diagnostics)
    {
        entry.HasResolvedValue = false;
        entry.ValueKind = TiffGeoKeyValueKind.None;
        entry.IntegerValues = new int[0];
        entry.DoubleValues = new double[0];
        entry.AsciiValue = "";
        entry.IsPrivate = entry.KeyId >= 32768 && entry.KeyId <= 65535;
        if (entry.TiffTagLocation == 0)
        {
            if (entry.Count != 1)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Inline GeoKey values must contain exactly one SHORT value.", imageIndex, TiffTagId.GeoKeyDirectory);
                return;
            }

            entry.IntegerValues = new int[] { entry.ValueOffset };
            entry.ValueKind = TiffGeoKeyValueKind.Short;
            entry.HasResolvedValue = true;
            return;
        }

        if (entry.TiffTagLocation == TiffTagId.GeoDoubleParams)
        {
            ResolveDoubleEntry(entry, parameters, imageIndex, diagnostics);
            return;
        }

        if (entry.TiffTagLocation == TiffTagId.GeoAsciiParams)
        {
            ResolveAsciiEntry(entry, parameters, imageIndex, diagnostics);
            return;
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GeoKey value location must be inline, GeoDoubleParamsTag, or GeoAsciiParamsTag.", imageIndex, TiffTagId.GeoKeyDirectory);
    }

    internal static void ResolveDoubleEntry(TiffGeoKeyEntry entry, TiffGeoParameterMetadata parameters, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!parameters.HasDoubleParams)
        {
            diagnostics.AddWarningForTag(TiffErrorCode.MissingRequiredTag, "GeoKey references a missing GeoDoubleParamsTag; the raw key entry was preserved.", imageIndex, TiffTagId.GeoDoubleParams);
            return;
        }

        if (entry.Count <= 0 || entry.ValueOffset < 0 || entry.ValueOffset > parameters.DoubleParams.Length - entry.Count)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GeoKey DOUBLE parameter range is outside GeoDoubleParamsTag.", imageIndex, TiffTagId.GeoKeyDirectory);
            return;
        }

        entry.DoubleValues = new double[entry.Count];
        int index = 0;
        while (index < entry.Count)
        {
            entry.DoubleValues[index] = parameters.DoubleParams[entry.ValueOffset + index];
            index = index + 1;
        }

        entry.ValueKind = TiffGeoKeyValueKind.Double;
        entry.HasResolvedValue = true;
    }

    internal static void ResolveAsciiEntry(TiffGeoKeyEntry entry, TiffGeoParameterMetadata parameters, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!parameters.HasAsciiParams)
        {
            diagnostics.AddWarningForTag(TiffErrorCode.MissingRequiredTag, "GeoKey references a missing GeoAsciiParamsTag; the raw key entry was preserved.", imageIndex, TiffTagId.GeoAsciiParams);
            return;
        }

        if (entry.Count <= 0 || entry.ValueOffset < 0 || entry.ValueOffset > parameters.AsciiText.Length - entry.Count)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GeoKey ASCII parameter range is outside GeoAsciiParamsTag.", imageIndex, TiffTagId.GeoKeyDirectory);
            return;
        }

        string value = parameters.AsciiText.Substring(entry.ValueOffset, entry.Count);
        if (value.Length > 0 && value[value.Length - 1] == '|')
        {
            value = value.Substring(0, value.Length - 1);
        }

        entry.AsciiValue = value;
        entry.ValueKind = TiffGeoKeyValueKind.Ascii;
        entry.HasResolvedValue = true;
    }

    internal static void ApplyModelType(TiffGeoKeyDirectory directory, int imageIndex, TiffDiagnostics diagnostics)
    {
        int value = FindSemanticIntegerValue(directory.Keys, TiffGeoKeyId.ModelType, imageIndex, diagnostics);
        if (value == 0)
        {
            return;
        }

        if (value == 1)
        {
            directory.ModelType = TiffGeoModelType.Projected;
        }
        else if (value == 2)
        {
            directory.ModelType = TiffGeoModelType.Geographic;
        }
        else if (value == 3)
        {
            directory.ModelType = TiffGeoModelType.Geocentric;
        }
        else if (value == UserDefinedCode)
        {
            directory.ModelType = TiffGeoModelType.UserDefined;
            directory.HasUserDefinedCrs = true;
        }
        else
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GTModelTypeGeoKey contains an unrecognized model type.", imageIndex, TiffTagId.GeoKeyDirectory);
        }
    }

    internal static void ApplyRasterType(TiffGeoKeyDirectory directory, int imageIndex, TiffDiagnostics diagnostics)
    {
        int value = FindSemanticIntegerValue(directory.Keys, TiffGeoKeyId.RasterType, imageIndex, diagnostics);
        if (value == 0)
        {
            return;
        }

        if (value == 1)
        {
            directory.RasterType = TiffGeoRasterType.PixelIsArea;
        }
        else if (value == 2)
        {
            directory.RasterType = TiffGeoRasterType.PixelIsPoint;
        }
        else
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GTRasterTypeGeoKey must declare RasterPixelIsArea or RasterPixelIsPoint.", imageIndex, TiffTagId.GeoKeyDirectory);
        }
    }

    internal static void ApplyEpsgCodes(TiffGeoKeyDirectory directory, int imageIndex, TiffDiagnostics diagnostics)
    {
        int geographic = FindSemanticIntegerValue(directory.Keys, TiffGeoKeyId.GeographicCrs, imageIndex, diagnostics);
        int projected = FindSemanticIntegerValue(directory.Keys, TiffGeoKeyId.ProjectedCrs, imageIndex, diagnostics);
        int vertical = FindSemanticIntegerValue(directory.Keys, TiffGeoKeyId.VerticalCrs, imageIndex, diagnostics);
        ApplyGeographicCode(directory, geographic, imageIndex, diagnostics);
        ApplyProjectedCode(directory, projected, imageIndex, diagnostics);
        ApplyVerticalCode(directory, vertical, imageIndex, diagnostics);
    }

    internal static void ApplyGeographicCode(TiffGeoKeyDirectory directory, int value, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (value == UserDefinedCode)
        {
            directory.HasUserDefinedCrs = true;
        }
        else if (value > 0 && value < UserDefinedCode)
        {
            directory.HasGeographicCrsEpsgCode = true;
            directory.GeographicCrsEpsgCode = value;
        }
        else if (value != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GeographicTypeGeoKey contains an invalid EPSG or user-defined identifier.", imageIndex, TiffTagId.GeoKeyDirectory);
        }
    }

    internal static void ApplyProjectedCode(TiffGeoKeyDirectory directory, int value, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (value == UserDefinedCode)
        {
            directory.HasUserDefinedCrs = true;
        }
        else if (value > 0 && value < UserDefinedCode)
        {
            directory.HasProjectedCrsEpsgCode = true;
            directory.ProjectedCrsEpsgCode = value;
        }
        else if (value != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ProjectedCRSGeoKey contains an invalid EPSG or user-defined identifier.", imageIndex, TiffTagId.GeoKeyDirectory);
        }
    }

    internal static void ApplyVerticalCode(TiffGeoKeyDirectory directory, int value, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (value == UserDefinedCode)
        {
            directory.HasUserDefinedCrs = true;
        }
        else if (value > 0 && value < UserDefinedCode)
        {
            directory.HasVerticalCrsEpsgCode = true;
            directory.VerticalCrsEpsgCode = value;
        }
        else if (value != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "VerticalGeoKey contains an invalid EPSG or user-defined identifier.", imageIndex, TiffTagId.GeoKeyDirectory);
        }
    }

    internal static int FindSemanticIntegerValue(TiffGeoKeyEntry[] keys, int keyId, int imageIndex, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < keys.Length)
        {
            TiffGeoKeyEntry key = keys[index];
            if (key != null && key.KeyId == keyId)
            {
                if (key.HasResolvedValue && key.ValueKind == TiffGeoKeyValueKind.Short && key.IntegerValues.Length == 1)
                {
                    return key.IntegerValues[0];
                }

                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GeoTIFF model, raster, and CRS identifier keys must contain one inline SHORT value.", imageIndex, TiffTagId.GeoKeyDirectory);
                return 0;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static string FindUserDefinedCitation(TiffGeoKeyDirectory directory)
    {
        if (!directory.HasUserDefinedCrs)
        {
            return "";
        }

        string value = FindAsciiValue(directory.Keys, TiffGeoKeyId.ProjectedCitation);
        if (value.Length == 0)
        {
            value = FindAsciiValue(directory.Keys, TiffGeoKeyId.GeographicCitation);
        }

        if (value.Length == 0)
        {
            value = FindAsciiValue(directory.Keys, TiffGeoKeyId.VerticalCitation);
        }

        if (value.Length == 0)
        {
            value = FindAsciiValue(directory.Keys, TiffGeoKeyId.Citation);
        }

        return value;
    }

    internal static string FindAsciiValue(TiffGeoKeyEntry[] keys, int keyId)
    {
        int index = 0;
        while (index < keys.Length)
        {
            TiffGeoKeyEntry key = keys[index];
            if (key != null && key.KeyId == keyId && key.HasResolvedValue && key.ValueKind == TiffGeoKeyValueKind.Ascii)
            {
                return key.AsciiValue;
            }

            index = index + 1;
        }

        return "";
    }

    internal static TiffGeoKeyEntry[] CollectPrivateKeys(TiffGeoKeyEntry[] keys)
    {
        int count = 0;
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] != null && keys[index].IsPrivate)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        TiffGeoKeyEntry[] privateKeys = new TiffGeoKeyEntry[count];
        index = 0;
        int destinationIndex = 0;
        while (index < keys.Length)
        {
            if (keys[index] != null && keys[index].IsPrivate)
            {
                privateKeys[destinationIndex] = keys[index].Copy();
                destinationIndex = destinationIndex + 1;
            }

            index = index + 1;
        }

        return privateKeys;
    }

    internal static void ValidateModelTags(TiffGeoModelMetadata model, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (model.HasTransformation && (model.HasPixelScale || model.HasTiepoints))
        {
            diagnostics.AddWarningForTag(TiffErrorCode.InvalidTagValue, "ModelTransformationTag conflicts with scale-and-tiepoint model tags; the transformation matrix takes precedence.", imageIndex, TiffTagId.ModelTransformation);
        }

        if (model.HasPixelScale != model.HasTiepoints)
        {
            diagnostics.AddWarningForTag(TiffErrorCode.MissingRequiredTag, "ModelPixelScaleTag and ModelTiepointTag normally appear together; the incomplete model cannot provide a scale-and-tiepoint transform.", imageIndex, model.HasPixelScale ? TiffTagId.ModelTiepoint : TiffTagId.ModelPixelScale);
        }
    }

    internal static void ValidateKeyModelConsistency(TiffGeoKeyDirectory directory, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (directory.ModelType == TiffGeoModelType.Geographic && directory.HasProjectedCrsEpsgCode)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "A geographic GTModelTypeGeoKey conflicts with ProjectedCRSGeoKey.", imageIndex, TiffTagId.GeoKeyDirectory);
        }
    }
}
