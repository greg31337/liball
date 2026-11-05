using System.Collections.Generic;

namespace jpeg;

internal class JpegFrameHeaderParser
{
    internal JpegFrameHeader Parse(int markerValue, byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        if (!IsStartOfFrameMarker(markerValue))
        {
            throw JpegInvalidDataException.Create("Marker is not a JPEG-1 SOF marker.");
        }

        if (payload.Length < 6)
        {
            throw JpegInvalidDataException.Create("SOF payload is too short.");
        }

        int componentCount = payload[5];
        if (componentCount <= 0)
        {
            throw JpegInvalidDataException.Create("SOF component count must be greater than zero.");
        }

        int expectedLength = 6 + componentCount * 3;
        if (payload.Length != expectedLength)
        {
            throw JpegInvalidDataException.Create("SOF payload length does not match component count.");
        }

        JpegFrameHeader frame = new JpegFrameHeader();
        frame.Marker = (JpegMarker)markerValue;
        frame.Value = markerValue;
        frame.Process = (JpegProcess)markerValue;
        frame.Progressive = IsProgressiveProcess(markerValue);
        frame.ArithmeticCoding = IsArithmeticProcess(markerValue);
        frame.Precision = payload[0];
        frame.Height = JpegByteReader.ReadUInt16(payload, 1);
        frame.Width = JpegByteReader.ReadUInt16(payload, 3);
        frame.ComponentCount = componentCount;

        ValidateFrameDimensions(frame);
        ParseComponents(frame, payload);
        return frame;
    }

    internal void ValidateFrameDimensions(JpegFrameHeader frame)
    {
        if (frame.Precision <= 0 || frame.Precision > 16)
        {
            throw JpegInvalidDataException.Create("SOF sample precision must be between 1 and 16 bits.");
        }

        if (frame.Width <= 0)
        {
            throw JpegInvalidDataException.Create("SOF image width must be greater than zero.");
        }
    }

    internal void ParseComponents(JpegFrameHeader frame, byte[] payload)
    {
        int offset = 6;
        int index = 0;
        while (index < frame.ComponentCount)
        {
            JpegFrameComponent component = new JpegFrameComponent();
            component.Identifier = payload[offset];
            int sampling = payload[offset + 1];
            component.HorizontalSamplingFactor = sampling >> 4;
            component.VerticalSamplingFactor = sampling & 15;
            component.QuantizationTableIdentifier = payload[offset + 2];
            ValidateComponent(frame, component);
            frame.Components.Add(component);
            offset = offset + 3;
            index = index + 1;
        }
    }

    internal void ValidateComponent(JpegFrameHeader frame, JpegFrameComponent component)
    {
        if (ComponentIdentifierExists(frame, component.Identifier))
        {
            throw JpegInvalidDataException.Create("SOF component identifiers must be unique.");
        }

        if (component.HorizontalSamplingFactor < 1 || component.HorizontalSamplingFactor > 4)
        {
            throw JpegInvalidDataException.Create("SOF horizontal sampling factor must be between 1 and 4.");
        }

        if (component.VerticalSamplingFactor < 1 || component.VerticalSamplingFactor > 4)
        {
            throw JpegInvalidDataException.Create("SOF vertical sampling factor must be between 1 and 4.");
        }

        if (component.QuantizationTableIdentifier < 0 || component.QuantizationTableIdentifier > 3)
        {
            throw JpegInvalidDataException.Create("SOF quantization table identifier must be between 0 and 3.");
        }
    }

    internal bool ComponentIdentifierExists(JpegFrameHeader frame, int identifier)
    {
        foreach (JpegFrameComponent component in frame.Components)
        {
            if (component.Identifier == identifier)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsStartOfFrameMarker(int markerValue)
    {
        if (markerValue >= JpegConstants.MarkerStartOfFrame0 && markerValue <= JpegConstants.MarkerStartOfFrame3)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame5 && markerValue <= JpegConstants.MarkerStartOfFrame7)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame9 && markerValue <= JpegConstants.MarkerStartOfFrame11)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame13 && markerValue <= JpegConstants.MarkerStartOfFrame15)
        {
            return true;
        }

        if (markerValue == JpegConstants.MarkerJpegLs)
        {
            return true;
        }

        return false;
    }

    internal bool IsProgressiveProcess(int markerValue)
    {
        return markerValue == JpegConstants.MarkerStartOfFrame2
            || markerValue == JpegConstants.MarkerStartOfFrame6
            || markerValue == JpegConstants.MarkerStartOfFrame10
            || markerValue == JpegConstants.MarkerStartOfFrame14;
    }

    internal bool IsArithmeticProcess(int markerValue)
    {
        return markerValue == JpegConstants.MarkerStartOfFrame9
            || markerValue == JpegConstants.MarkerStartOfFrame10
            || markerValue == JpegConstants.MarkerStartOfFrame11
            || markerValue == JpegConstants.MarkerStartOfFrame13
            || markerValue == JpegConstants.MarkerStartOfFrame14
            || markerValue == JpegConstants.MarkerStartOfFrame15;
    }
}

internal class JpegFrameHeaderWriter
{
    internal byte[] WritePayload(JpegFrameHeader frame)
    {
        ValidateFrame(frame);
        int componentCount = GetComponentCount(frame);
        JpegByteWriter writer = JpegByteWriter.WithCapacity(6 + componentCount * 3);
        writer.WriteByte(frame.Precision);
        writer.WriteUInt16(frame.Height);
        writer.WriteUInt16(frame.Width);
        writer.WriteByte(componentCount);

        int index = 0;
        while (index < componentCount)
        {
            JpegFrameComponent component = frame.Components[index];
            writer.WriteByte(component.Identifier);
            writer.WriteByte((component.HorizontalSamplingFactor << 4) + component.VerticalSamplingFactor);
            writer.WriteByte(component.QuantizationTableIdentifier);
            index = index + 1;
        }

        return writer.ToArray();
    }

    internal void WriteMarkerSegment(JpegMarkerSegmentWriter segmentWriter, JpegFrameHeader frame)
    {
        if (segmentWriter == null)
        {
            throw new System.ArgumentNullException("segmentWriter");
        }

        int markerValue = GetMarkerValue(frame);
        byte[] payload = WritePayload(frame);
        segmentWriter.WriteMarkerSegmentValue(markerValue, payload);
    }

    internal void ValidateFrame(JpegFrameHeader frame)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        int markerValue = GetMarkerValue(frame);
        JpegFrameHeaderParser parser = new JpegFrameHeaderParser();
        if (!parser.IsStartOfFrameMarker(markerValue))
        {
            throw JpegWriteValidationException.Create("Marker is not a JPEG-1 SOF marker.");
        }

        if (frame.Value != 0 && frame.Marker != JpegMarker.Unknown && frame.Value != (int)frame.Marker)
        {
            throw JpegWriteValidationException.Create("SOF marker value and marker enum must match.");
        }

        if (frame.Process != JpegProcess.Unknown && (int)frame.Process != markerValue)
        {
            throw JpegWriteValidationException.Create("SOF process must match the marker being written.");
        }

        if (frame.Precision <= 0 || frame.Precision > 16)
        {
            throw JpegWriteValidationException.Create("SOF sample precision must be between 1 and 16 bits.");
        }

        if (frame.Height < 0 || frame.Height > 65535)
        {
            throw JpegWriteValidationException.Create("SOF image height must fit in two bytes.");
        }

        if (frame.Width <= 0 || frame.Width > 65535)
        {
            throw JpegWriteValidationException.Create("SOF image width must be between 1 and 65535.");
        }

        ValidateComponents(frame);
    }

    internal void ValidateComponents(JpegFrameHeader frame)
    {
        if (frame.Components == null)
        {
            throw JpegWriteValidationException.Create("SOF component list cannot be null.");
        }

        int componentCount = GetComponentCount(frame);
        if (componentCount <= 0 || componentCount > 255)
        {
            throw JpegWriteValidationException.Create("SOF component count must be between 1 and 255.");
        }

        if (frame.ComponentCount != 0 && frame.ComponentCount != componentCount)
        {
            throw JpegWriteValidationException.Create("SOF component count must match the component list.");
        }

        int index = 0;
        while (index < componentCount)
        {
            JpegFrameComponent component = frame.Components[index];
            ValidateComponent(frame, component, index);
            index = index + 1;
        }
    }

    internal void ValidateComponent(JpegFrameHeader frame, JpegFrameComponent component, int componentIndex)
    {
        if (component == null)
        {
            throw JpegWriteValidationException.Create("SOF component cannot be null.");
        }

        if (component.Identifier < 0 || component.Identifier > 255)
        {
            throw JpegWriteValidationException.Create("SOF component identifier must fit in one byte.");
        }

        if (component.HorizontalSamplingFactor < 1 || component.HorizontalSamplingFactor > 4)
        {
            throw JpegWriteValidationException.Create("SOF horizontal sampling factor must be between 1 and 4.");
        }

        if (component.VerticalSamplingFactor < 1 || component.VerticalSamplingFactor > 4)
        {
            throw JpegWriteValidationException.Create("SOF vertical sampling factor must be between 1 and 4.");
        }

        if (component.QuantizationTableIdentifier < 0 || component.QuantizationTableIdentifier > 3)
        {
            throw JpegWriteValidationException.Create("SOF quantization table identifier must be between 0 and 3.");
        }

        int index = 0;
        while (index < componentIndex)
        {
            if (frame.Components[index].Identifier == component.Identifier)
            {
                throw JpegWriteValidationException.Create("SOF component identifiers must be unique.");
            }

            index = index + 1;
        }
    }

    internal int GetMarkerValue(JpegFrameHeader frame)
    {
        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        if (frame.Value != 0)
        {
            return frame.Value;
        }

        if (frame.Marker != JpegMarker.Unknown)
        {
            return (int)frame.Marker;
        }

        if (frame.Process != JpegProcess.Unknown)
        {
            return (int)frame.Process;
        }

        throw JpegWriteValidationException.Create("SOF marker or process must be specified.");
    }

    internal int GetComponentCount(JpegFrameHeader frame)
    {
        if (frame.Components == null)
        {
            return 0;
        }

        return frame.Components.Count;
    }
}
