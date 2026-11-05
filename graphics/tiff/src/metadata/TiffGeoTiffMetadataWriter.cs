using System;

namespace tiff;

internal class TiffGeoTiffMetadataWriter
{
    internal static TiffGeoTiffWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, modelMetadata, keyDirectory, parameters, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffGeoTiffWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, modelMetadata, keyDirectory, parameters, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffGeoTiffWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffTagValue[] actualValues = BuildValues(values, modelMetadata, keyDirectory, parameters, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGeoTiffWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGeoTiffWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        TiffDiagnostics writeDiagnostics = TiffDirectoryWriter.WriteClassicWithValueBytes(destination.Slice((int)directoryOffset, byteCount), entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, actualOptions);
        diagnostics.Merge(writeDiagnostics);
        return TiffGeoTiffWriteResult.Create(directoryOffset, (ulong)byteCount, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTagValue[] actualValues = BuildValues(values, modelMetadata, keyDirectory, parameters, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(actualValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(actualValues, TiffByteOrder.LittleEndian, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        return TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] actualValues = values;
        if (values == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "GeoTIFF writer requires directory tag values.");
            actualValues = new TiffTagValue[0];
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "GeoTIFF writer requires a known byte order.");
        }

        if (directoryOffset == 0 || directoryOffset > int.MaxValue || directoryOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "GeoTIFF directory offset is outside the destination buffer.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic GeoTIFF IFD next offset must fit in 32 bits.");
        }

        if (diagnostics.HasErrors())
        {
            return false;
        }

        TiffTagValue[] encodedValues = BuildValues(actualValues, NormalizeModel(modelMetadata), NormalizeKeyDirectory(keyDirectory), NormalizeParameters(parameters), diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        TiffIfdEntry[] entries = TiffExifMetadataWriter.CreateEntries(encodedValues);
        byte[][] valueBytes = TiffExifMetadataWriter.EncodeValues(encodedValues, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        int byteCount = TiffDirectoryWriter.CalculateClassicDirectoryLayoutByteCount(entries, valueBytes);
        if (directoryOffset > ulong.MaxValue - (ulong)byteCount || directoryOffset + (ulong)byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "GeoTIFF destination buffer is too short for the directory.");
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffTagValue[] BuildValues(TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffDiagnostics diagnostics)
    {
        TiffGeoModelMetadata actualModel = NormalizeModel(modelMetadata);
        TiffGeoKeyDirectory actualKeyDirectory = NormalizeKeyDirectory(keyDirectory);
        TiffGeoParameterMetadata actualParameters = NormalizeParameters(parameters);
        TiffTagValue[] geoValues = CreateGeoValues(actualModel, actualKeyDirectory, actualParameters, diagnostics);
        TiffGeoKeyResolver.Resolve(actualKeyDirectory, actualParameters, TiffDiagnostic.NoImageIndex, diagnostics);
        TiffGeoKeyResolver.ValidateModelTags(actualModel, TiffDiagnostic.NoImageIndex, diagnostics);
        int nonGeoCount = CountNonGeoTiffValues(values);
        TiffTagValue[] result = new TiffTagValue[nonGeoCount + geoValues.Length];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (values != null && sourceIndex < values.Length)
        {
            if (values[sourceIndex] != null && !IsGeoTiffTag(values[sourceIndex].TagId))
            {
                result[destinationIndex] = values[sourceIndex].Copy();
                destinationIndex = destinationIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        int geoIndex = 0;
        while (geoIndex < geoValues.Length)
        {
            result[destinationIndex] = geoValues[geoIndex].Copy();
            destinationIndex = destinationIndex + 1;
            geoIndex = geoIndex + 1;
        }

        return result;
    }

    internal static TiffTagValue[] CreateGeoValues(TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] values = new TiffTagValue[0];
        if (modelMetadata.HasPixelScale)
        {
            values = AppendValue(values, CreatePixelScaleValue(modelMetadata.PixelScale, diagnostics));
        }

        if (modelMetadata.HasTiepoints)
        {
            values = AppendValue(values, CreateTiepointValue(modelMetadata.Tiepoints, diagnostics));
        }

        if (modelMetadata.HasTransformation)
        {
            values = AppendValue(values, CreateTransformationValue(modelMetadata.Transformation, diagnostics));
        }

        if (keyDirectory.HasDirectory)
        {
            values = AppendValue(values, CreateGeoKeyDirectoryValue(keyDirectory, diagnostics));
        }

        if (parameters.HasDoubleParams)
        {
            values = AppendValue(values, CreateGeoDoubleParamsValue(parameters.DoubleParams, diagnostics));
        }

        if (parameters.HasAsciiParams)
        {
            values = AppendValue(values, CreateGeoAsciiParamsValue(parameters, diagnostics));
        }

        return values;
    }

    internal static TiffTagValue CreatePixelScaleValue(double[] pixelScale, TiffDiagnostics diagnostics)
    {
        if (pixelScale == null || pixelScale.Length != 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ModelPixelScaleTag requires exactly three DOUBLE values.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelPixelScale);
            return new TiffTagValue();
        }

        return TiffTagValue.CreateFloatingArray(TiffTagId.ModelPixelScale, TiffDataType.Double, pixelScale);
    }

    internal static TiffTagValue CreateTiepointValue(TiffGeoTiepoint[] tiepoints, TiffDiagnostics diagnostics)
    {
        if (tiepoints == null || tiepoints.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ModelTiepointTag requires at least one six-DOUBLE tiepoint.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelTiepoint);
            return new TiffTagValue();
        }

        double[] values = new double[tiepoints.Length * 6];
        int index = 0;
        while (index < tiepoints.Length)
        {
            TiffGeoTiepoint tiepoint = NormalizeTiepoint(tiepoints[index]);
            int offset = index * 6;
            values[offset] = tiepoint.RasterI;
            values[offset + 1] = tiepoint.RasterJ;
            values[offset + 2] = tiepoint.RasterK;
            values[offset + 3] = tiepoint.ModelX;
            values[offset + 4] = tiepoint.ModelY;
            values[offset + 5] = tiepoint.ModelZ;
            index = index + 1;
        }

        return TiffTagValue.CreateFloatingArray(TiffTagId.ModelTiepoint, TiffDataType.Double, values);
    }

    internal static TiffTagValue CreateTransformationValue(double[] transformation, TiffDiagnostics diagnostics)
    {
        if (transformation == null || transformation.Length != 16)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ModelTransformationTag requires exactly sixteen DOUBLE values.", TiffDiagnostic.NoImageIndex, TiffTagId.ModelTransformation);
            return new TiffTagValue();
        }

        return TiffTagValue.CreateFloatingArray(TiffTagId.ModelTransformation, TiffDataType.Double, transformation);
    }

    internal static TiffTagValue CreateGeoKeyDirectoryValue(TiffGeoKeyDirectory keyDirectory, TiffDiagnostics diagnostics)
    {
        if (keyDirectory == null || keyDirectory.Keys == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoKeyDirectoryTag requires a key array.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoKeyDirectory);
            return new TiffTagValue();
        }

        if (keyDirectory.NumberOfKeys != keyDirectory.Keys.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoKeyDirectoryTag header key count must match the key array length.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoKeyDirectory);
            return new TiffTagValue();
        }

        ulong[] values = new ulong[4 + (keyDirectory.Keys.Length * 4)];
        if (!WriteUShortValue(values, 0, keyDirectory.KeyDirectoryVersion, diagnostics, TiffTagId.GeoKeyDirectory))
        {
            return new TiffTagValue();
        }

        if (!WriteUShortValue(values, 1, keyDirectory.KeyRevision, diagnostics, TiffTagId.GeoKeyDirectory))
        {
            return new TiffTagValue();
        }

        if (!WriteUShortValue(values, 2, keyDirectory.MinorRevision, diagnostics, TiffTagId.GeoKeyDirectory))
        {
            return new TiffTagValue();
        }

        if (!WriteUShortValue(values, 3, keyDirectory.NumberOfKeys, diagnostics, TiffTagId.GeoKeyDirectory))
        {
            return new TiffTagValue();
        }

        int index = 0;
        while (index < keyDirectory.Keys.Length)
        {
            TiffGeoKeyEntry key = NormalizeKey(keyDirectory.Keys[index]);
            int offset = 4 + (index * 4);
            if (!WriteUShortValue(values, offset, key.KeyId, diagnostics, TiffTagId.GeoKeyDirectory))
            {
                return new TiffTagValue();
            }

            if (!WriteUShortValue(values, offset + 1, key.TiffTagLocation, diagnostics, TiffTagId.GeoKeyDirectory))
            {
                return new TiffTagValue();
            }

            if (!WriteUShortValue(values, offset + 2, key.Count, diagnostics, TiffTagId.GeoKeyDirectory))
            {
                return new TiffTagValue();
            }

            if (!WriteUShortValue(values, offset + 3, key.ValueOffset, diagnostics, TiffTagId.GeoKeyDirectory))
            {
                return new TiffTagValue();
            }

            index = index + 1;
        }

        return TiffTagValue.CreateUnsignedArray(TiffTagId.GeoKeyDirectory, TiffDataType.Short, values);
    }

    internal static TiffTagValue CreateGeoDoubleParamsValue(double[] doubleParams, TiffDiagnostics diagnostics)
    {
        if (doubleParams == null || doubleParams.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoDoubleParamsTag requires at least one DOUBLE value.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoDoubleParams);
            return new TiffTagValue();
        }

        return TiffTagValue.CreateFloatingArray(TiffTagId.GeoDoubleParams, TiffDataType.Double, doubleParams);
    }

    internal static TiffTagValue CreateGeoAsciiParamsValue(TiffGeoParameterMetadata parameters, TiffDiagnostics diagnostics)
    {
        byte[] bytes = ResolveAsciiBytes(parameters, diagnostics);
        if (bytes.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoAsciiParamsTag requires at least one ASCII byte.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoAsciiParams);
            return new TiffTagValue();
        }

        return TiffTagValue.CreateRaw(TiffTagId.GeoAsciiParams, TiffDataType.Ascii, bytes.Length, bytes);
    }

    internal static byte[] ResolveAsciiBytes(TiffGeoParameterMetadata parameters, TiffDiagnostics diagnostics)
    {
        if (parameters != null && parameters.AsciiBytes != null && parameters.AsciiBytes.Length > 0)
        {
            if (!ValidateAsciiBytes(parameters.AsciiBytes, diagnostics))
            {
                return new byte[0];
            }

            return TiffTagValue.CopyByteArray(parameters.AsciiBytes);
        }

        string text = "";
        if (parameters != null)
        {
            text = TiffGeoParameterMetadata.NormalizeString(parameters.AsciiText);
        }

        return EncodeAsciiText(text, diagnostics);
    }

    internal static byte[] EncodeAsciiText(string text, TiffDiagnostics diagnostics)
    {
        string normalized = TiffGeoParameterMetadata.NormalizeString(text);
        byte[] bytes = new byte[normalized.Length + 1];
        int index = 0;
        while (index < normalized.Length)
        {
            char value = normalized[index];
            if (value > 127)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidAsciiValue, "GeoAsciiParamsTag contains a character outside the 7-bit ASCII range.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoAsciiParams);
                return new byte[0];
            }

            bytes[index] = (byte)value;
            index = index + 1;
        }

        bytes[bytes.Length - 1] = 0;

        return bytes;
    }

    internal static bool ValidateAsciiBytes(byte[] bytes, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (bytes != null && index < bytes.Length)
        {
            if (bytes[index] > 127)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidAsciiValue, "GeoAsciiParamsTag contains a byte outside the 7-bit ASCII range.", TiffDiagnostic.NoImageIndex, TiffTagId.GeoAsciiParams);
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool WriteUShortValue(ulong[] values, int index, int value, TiffDiagnostics diagnostics, int tagId)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "GeoKeyDirectoryTag values must fit in an unsigned SHORT.", TiffDiagnostic.NoImageIndex, tagId);
            return false;
        }

        values[index] = (ulong)value;
        return true;
    }

    internal static TiffTagValue[] AppendValue(TiffTagValue[] values, TiffTagValue value)
    {
        TiffTagValue[] result = new TiffTagValue[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static int CountNonGeoTiffValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return 0;
        }

        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != null && !IsGeoTiffTag(values[index].TagId))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static bool IsGeoTiffTag(int tagId)
    {
        return tagId == TiffTagId.ModelPixelScale || tagId == TiffTagId.ModelTiepoint || tagId == TiffTagId.ModelTransformation || tagId == TiffTagId.GeoKeyDirectory || tagId == TiffTagId.GeoDoubleParams || tagId == TiffTagId.GeoAsciiParams;
    }

    internal static bool HasAnyGeoTiffMetadata(TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters)
    {
        TiffGeoModelMetadata actualModel = NormalizeModel(modelMetadata);
        TiffGeoKeyDirectory actualKeyDirectory = NormalizeKeyDirectory(keyDirectory);
        TiffGeoParameterMetadata actualParameters = NormalizeParameters(parameters);
        return actualModel.HasPixelScale || actualModel.HasTiepoints || actualModel.HasTransformation || actualKeyDirectory.HasDirectory || actualParameters.HasDoubleParams || actualParameters.HasAsciiParams;
    }

    internal static TiffGeoModelMetadata NormalizeModel(TiffGeoModelMetadata modelMetadata)
    {
        if (modelMetadata == null)
        {
            return new TiffGeoModelMetadata();
        }

        return modelMetadata.Copy();
    }

    internal static TiffGeoKeyDirectory NormalizeKeyDirectory(TiffGeoKeyDirectory keyDirectory)
    {
        if (keyDirectory == null)
        {
            return new TiffGeoKeyDirectory();
        }

        return keyDirectory.Copy();
    }

    internal static TiffGeoParameterMetadata NormalizeParameters(TiffGeoParameterMetadata parameters)
    {
        if (parameters == null)
        {
            return new TiffGeoParameterMetadata();
        }

        return parameters.Copy();
    }

    internal static TiffGeoTiepoint NormalizeTiepoint(TiffGeoTiepoint tiepoint)
    {
        if (tiepoint == null)
        {
            return new TiffGeoTiepoint();
        }

        return tiepoint.Copy();
    }

    internal static TiffGeoKeyEntry NormalizeKey(TiffGeoKeyEntry key)
    {
        if (key == null)
        {
            return new TiffGeoKeyEntry();
        }

        return key.Copy();
    }
}
