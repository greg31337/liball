namespace jpeg;

internal class JpegScanHeaderParser
{
    internal JpegScanHeader Parse(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (payload.Length < 4)
        {
            throw JpegInvalidDataException.Create("SOS payload is too short.");
        }

        int componentCount = payload[0];
        if (componentCount <= 0 || componentCount > 4)
        {
            throw JpegInvalidDataException.Create("SOS component count must be between 1 and 4.");
        }

        int expectedLength = 1 + componentCount * 2 + 3;
        if (payload.Length != expectedLength)
        {
            throw JpegInvalidDataException.Create("SOS payload length does not match component count.");
        }

        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = componentCount;
        ParseComponents(scan, payload);

        int spectralOffset = 1 + componentCount * 2;
        scan.SpectralStart = payload[spectralOffset];
        scan.SpectralEnd = payload[spectralOffset + 1];
        int successive = payload[spectralOffset + 2];
        scan.SuccessiveHigh = successive >> 4;
        scan.SuccessiveLow = successive & 15;
        ValidateSpectralRange(scan);
        return scan;
    }

    internal void ParseComponents(JpegScanHeader scan, byte[] payload)
    {
        int offset = 1;
        int index = 0;
        while (index < scan.ComponentCount)
        {
            JpegScanComponent component = new JpegScanComponent();
            component.Selector = payload[offset];
            int tableSelectors = payload[offset + 1];
            component.DcEntropyTableIdentifier = tableSelectors >> 4;
            component.AcEntropyTableIdentifier = tableSelectors & 15;
            ValidateComponent(scan, component);
            scan.Components.Add(component);
            offset = offset + 2;
            index = index + 1;
        }
    }

    internal void ValidateComponent(JpegScanHeader scan, JpegScanComponent component)
    {
        if (ComponentSelectorExists(scan, component.Selector))
        {
            throw JpegInvalidDataException.Create("SOS component selectors must be unique.");
        }

        if (component.DcEntropyTableIdentifier < 0 || component.DcEntropyTableIdentifier > 3)
        {
            throw JpegInvalidDataException.Create("SOS DC entropy table identifier must be between 0 and 3.");
        }

        if (component.AcEntropyTableIdentifier < 0 || component.AcEntropyTableIdentifier > 3)
        {
            throw JpegInvalidDataException.Create("SOS AC entropy table identifier must be between 0 and 3.");
        }
    }

    internal bool ComponentSelectorExists(JpegScanHeader scan, int selector)
    {
        foreach (JpegScanComponent component in scan.Components)
        {
            if (component.Selector == selector)
            {
                return true;
            }
        }

        return false;
    }

    internal void ValidateSpectralRange(JpegScanHeader scan)
    {
        if (scan.SpectralStart < 0 || scan.SpectralStart > 63)
        {
            throw JpegInvalidDataException.Create("SOS spectral start must be between 0 and 63.");
        }

        if (scan.SpectralEnd < 0 || scan.SpectralEnd > 63)
        {
            throw JpegInvalidDataException.Create("SOS spectral end must be between 0 and 63.");
        }

        if (scan.SpectralStart > scan.SpectralEnd && !IsLosslessPredictorRange(scan))
        {
            throw JpegInvalidDataException.Create("SOS spectral start cannot exceed spectral end.");
        }
    }

    internal bool IsLosslessPredictorRange(JpegScanHeader scan)
    {
        return scan.SpectralStart >= 1 && scan.SpectralStart <= 7 && scan.SpectralEnd == 0;
    }
}

internal class JpegScanHeaderWriter
{
    internal byte[] WritePayload(JpegScanHeader scan)
    {
        ValidateScan(scan);
        int componentCount = GetComponentCount(scan);
        JpegByteWriter writer = JpegByteWriter.WithCapacity(1 + componentCount * 2 + 3);
        writer.WriteByte(componentCount);

        int index = 0;
        while (index < componentCount)
        {
            JpegScanComponent component = scan.Components[index];
            writer.WriteByte(component.Selector);
            writer.WriteByte((component.DcEntropyTableIdentifier << 4) + component.AcEntropyTableIdentifier);
            index = index + 1;
        }

        writer.WriteByte(scan.SpectralStart);
        writer.WriteByte(scan.SpectralEnd);
        writer.WriteByte((scan.SuccessiveHigh << 4) + scan.SuccessiveLow);
        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegScanHeader scan)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        byte[] payload = WritePayload(scan);
        segmentWriter.WriteMarkerSegment(JpegMarker.StartOfScan, payload);
    }

    internal void ValidateScan(JpegScanHeader scan)
    {
        if (scan == null)
        {
            throw new System.ArgumentNullException("scan");
        }

        ValidateComponents(scan);
        ValidateSpectralRange(scan);

        if (scan.SuccessiveHigh < 0 || scan.SuccessiveHigh > 15)
        {
            throw JpegWriteValidationException.Create("SOS successive high approximation must be between 0 and 15.");
        }

        if (scan.SuccessiveLow < 0 || scan.SuccessiveLow > 15)
        {
            throw JpegWriteValidationException.Create("SOS successive low approximation must be between 0 and 15.");
        }
    }

    internal void ValidateComponents(JpegScanHeader scan)
    {
        if (scan.Components == null)
        {
            throw JpegWriteValidationException.Create("SOS component list cannot be null.");
        }

        int componentCount = GetComponentCount(scan);
        if (componentCount <= 0 || componentCount > 4)
        {
            throw JpegWriteValidationException.Create("SOS component count must be between 1 and 4.");
        }

        if (scan.ComponentCount != 0 && scan.ComponentCount != componentCount)
        {
            throw JpegWriteValidationException.Create("SOS component count must match the component list.");
        }

        int index = 0;
        while (index < componentCount)
        {
            JpegScanComponent component = scan.Components[index];
            ValidateComponent(scan, component, index);
            index = index + 1;
        }
    }

    internal void ValidateComponent(JpegScanHeader scan, JpegScanComponent component, int componentIndex)
    {
        if (component == null)
        {
            throw JpegWriteValidationException.Create("SOS component cannot be null.");
        }

        if (component.Selector < 0 || component.Selector > 255)
        {
            throw JpegWriteValidationException.Create("SOS component selector must fit in one byte.");
        }

        if (component.DcEntropyTableIdentifier < 0 || component.DcEntropyTableIdentifier > 3)
        {
            throw JpegWriteValidationException.Create("SOS DC entropy table identifier must be between 0 and 3.");
        }

        if (component.AcEntropyTableIdentifier < 0 || component.AcEntropyTableIdentifier > 3)
        {
            throw JpegWriteValidationException.Create("SOS AC entropy table identifier must be between 0 and 3.");
        }

        int index = 0;
        while (index < componentIndex)
        {
            if (scan.Components[index].Selector == component.Selector)
            {
                throw JpegWriteValidationException.Create("SOS component selectors must be unique.");
            }

            index = index + 1;
        }
    }

    internal void ValidateSpectralRange(JpegScanHeader scan)
    {
        if (scan.SpectralStart < 0 || scan.SpectralStart > 63)
        {
            throw JpegWriteValidationException.Create("SOS spectral start must be between 0 and 63.");
        }

        if (scan.SpectralEnd < 0 || scan.SpectralEnd > 63)
        {
            throw JpegWriteValidationException.Create("SOS spectral end must be between 0 and 63.");
        }

        if (scan.SpectralStart > scan.SpectralEnd && !IsLosslessPredictorRange(scan))
        {
            throw JpegWriteValidationException.Create("SOS spectral start cannot exceed spectral end.");
        }
    }

    internal bool IsLosslessPredictorRange(JpegScanHeader scan)
    {
        return scan.SpectralStart >= 1 && scan.SpectralStart <= 7 && scan.SpectralEnd == 0;
    }

    internal int GetComponentCount(JpegScanHeader scan)
    {
        if (scan.Components == null)
        {
            return 0;
        }

        return scan.Components.Count;
    }
}
