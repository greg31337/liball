namespace icc;

internal static class MeasurementTagCodec
{
    internal static void DecodeMeasurement(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != 36)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A measurementType payload requires exactly 36 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The measurementType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader reader = new BigEndianReader(payload.Slice(8), state.AbsolutePayloadOffset + 8);
        uint observer = reader.ReadUInt32();
        IccXyzNumber backing = IccNumberCodec.ReadXyzNumber(reader);
        uint geometry = reader.ReadUInt32();
        double flare = IccNumberCodec.ReadU16Fixed16(reader);
        uint illuminant = reader.ReadUInt32();
        if (observer > 2 || geometry > 2 || illuminant > 8 || flare > 1.0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The measurementType contains an undefined observer, geometry, illuminant, or a flare outside zero through one.");
            return;
        }

        IccMeasurement measurement = new IccMeasurement();
        measurement.Observer = (IccMeasurementObserver)observer;
        measurement.Backing = ToXyzColor(backing);
        measurement.Geometry = (IccMeasurementGeometry)geometry;
        measurement.Flare = flare;
        measurement.Illuminant = (IccStandardIlluminant)illuminant;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Measurement);
        data.Measurement = measurement;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, "Decoded measurementType conditions.");
    }

    internal static byte[] SerializeMeasurement(IccMeasurement measurement)
    {
        if (measurement == null || measurement.Backing == null)
        {
            throw new IccProfileWriteException("Measurement data and its backing colour cannot be null.");
        }

        int observer = (int)measurement.Observer;
        int geometry = (int)measurement.Geometry;
        int illuminant = (int)measurement.Illuminant;
        if (observer < 0 || observer > 2 || geometry < 0 || geometry > 2 || illuminant < 0 || illuminant > 8)
        {
            throw new IccProfileWriteException("Measurement observer, geometry, and illuminant values shall use defined ICC encodings.");
        }

        if (System.Double.IsNaN(measurement.Flare) || System.Double.IsInfinity(measurement.Flare) || measurement.Flare < 0.0 || measurement.Flare > 1.0)
        {
            throw new IccProfileWriteException("Measurement flare shall be finite and between zero and one.");
        }

        ValidateXyzColor(measurement.Backing, "measurement backing");

        byte[] payload = new byte[36];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Measurement);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)observer);
        IccNumberCodec.WriteXyzNumber(writer, ToXyzNumber(measurement.Backing));
        writer.WriteUInt32((uint)geometry);
        IccNumberCodec.WriteU16Fixed16(writer, measurement.Flare);
        writer.WriteUInt32((uint)illuminant);
        return payload;
    }

    internal static IccXyzColor ToXyzColor(IccXyzNumber value)
    {
        IccXyzColor color = new IccXyzColor();
        color.X = value.X;
        color.Y = value.Y;
        color.Z = value.Z;
        return color;
    }

    internal static IccXyzNumber ToXyzNumber(IccXyzColor value)
    {
        IccXyzNumber number = new IccXyzNumber();
        number.X = value.X;
        number.Y = value.Y;
        number.Z = value.Z;
        return number;
    }

    internal static void ValidateXyzColor(IccXyzColor value, string description)
    {
        if (System.Double.IsNaN(value.X) || System.Double.IsInfinity(value.X) || value.X < IccNumberCodec.S15Fixed16Minimum || value.X > IccNumberCodec.S15Fixed16Maximum
            || System.Double.IsNaN(value.Y) || System.Double.IsInfinity(value.Y) || value.Y < IccNumberCodec.S15Fixed16Minimum || value.Y > IccNumberCodec.S15Fixed16Maximum
            || System.Double.IsNaN(value.Z) || System.Double.IsInfinity(value.Z) || value.Z < IccNumberCodec.S15Fixed16Minimum || value.Z > IccNumberCodec.S15Fixed16Maximum)
        {
            throw new IccProfileWriteException($"The {description} XYZ values shall fit the finite signed 15.16 fixed-point range.");
        }
    }
}
