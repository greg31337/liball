namespace gif.Tests;

public class GifInterfaceTests
{
    [Test]
    public void DecoderInterface_NullOptionsUseDefaults()
    {
        IGifDecoder decoder = new GifCodec();

        GifDocument document = decoder.DecodeBytes(GifFixtures.MinimalGif89a(), null);

        Assert.That(document.Frames.Length, Is.EqualTo(1));
    }

    [Test]
    public void GifCodec_ImplementsDecoderAndEncoderRoles()
    {
        GifCodec codec = new GifCodec();
        IGifDecoder decoder = codec;
        IGifEncoder encoder = codec;
        byte[] source = GifFixtures.MinimalGif87a();

        GifInfo info = decoder.IdentifyBytes(source, new GifDecodeOptions());
        GifDocument document = decoder.DecodeBytes(source, new GifDecodeOptions());
        byte[] encoded = encoder.Encode(document, new GifEncodeOptions());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Width, Is.EqualTo(1));
        Assert.That(document.Frames, Has.Length.EqualTo(1));
        Assert.That(encoded, Is.EqualTo(source));
    }

    [Test]
    public void BlockReader_AcceptsInjectedReaderAndReportsItsFailureOffset()
    {
        FailingByteReader byteReader = new FailingByteReader(GifFixtures.MinimalGif87a(), GifConstants.HeaderSize);
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.ReadFromReader(byteReader, new GifDecodeOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.UnexpectedEndOfStream));
        Assert.That(result.BytesRead, Is.EqualTo(GifConstants.HeaderSize));
    }

    [Test]
    public void HeaderWriter_AcceptsInjectedWriterForDeterministicFailureTesting()
    {
        FailingByteWriter byteWriter = new FailingByteWriter(3);
        GifHeaderWriter writer = new GifHeaderWriter();

        GifHeaderWriteResult result = writer.Write(byteWriter, GifVersion.Gif89a);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.DestinationWriteFailed));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(3));
    }

    [Test]
    public void Quantizer_ResolvesEverySupportedModeThroughStrategyInterface()
    {
        GifQuantizer quantizer = new GifQuantizer();

        Assert.That(quantizer.CreateStrategy(GifQuantizerMode.Exact), Is.InstanceOf<IGifQuantizationStrategy>());
        Assert.That(quantizer.CreateStrategy(GifQuantizerMode.MedianCut), Is.InstanceOf<IGifQuantizationStrategy>());
        Assert.That(quantizer.CreateStrategy(GifQuantizerMode.Octree), Is.InstanceOf<IGifQuantizationStrategy>());
        Assert.That(quantizer.CreateStrategy(GifQuantizerMode.Grayscale), Is.InstanceOf<IGifQuantizationStrategy>());
        Assert.That(quantizer.CreateStrategy(GifQuantizerMode.WebSafe), Is.InstanceOf<IGifQuantizationStrategy>());
        Assert.That(quantizer.CreateStrategy((GifQuantizerMode)999), Is.Null);
    }

    class FailingByteReader : IGifByteReader
    {
        byte[] Data;
        int FailAtOffset;
        int Offset;

        public FailingByteReader(byte[] data, int failAtOffset)
        {
            Data = data;
            FailAtOffset = failAtOffset;
            Offset = 0;
        }

        public int ReadByte()
        {
            if (Offset >= FailAtOffset || Offset >= Data.Length)
            {
                return -1;
            }

            int value = Data[Offset];
            Offset = Offset + 1;
            return value;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (count > FailAtOffset - Offset || count > Data.Length - Offset)
            {
                return -1;
            }

            int index = 0;
            while (index < count)
            {
                buffer[offset + index] = Data[Offset + index];
                index = index + 1;
            }

            Offset = Offset + count;
            return count;
        }

        public bool Advance(int count)
        {
            if (count > FailAtOffset - Offset || count > Data.Length - Offset)
            {
                return false;
            }

            Offset = Offset + count;
            return true;
        }

        public int RemainingCount()
        {
            return Data.Length - Offset;
        }

        public long GetByteOffset()
        {
            return Offset;
        }
    }

    class FailingByteWriter : IGifByteWriter
    {
        int FailAtOffset;
        int Offset;

        public FailingByteWriter(int failAtOffset)
        {
            FailAtOffset = failAtOffset;
            Offset = 0;
        }

        public bool WriteByte(int value)
        {
            if (Offset >= FailAtOffset)
            {
                return false;
            }

            Offset = Offset + 1;
            return true;
        }

        public bool Write(byte[] data)
        {
            return WriteRange(data, 0, data.Length);
        }

        public bool WriteRange(byte[] data, int offset, int count)
        {
            int writable = FailAtOffset - Offset;
            if (count > writable)
            {
                Offset = Offset + writable;
                return false;
            }

            Offset = Offset + count;
            return true;
        }

        public bool WriteUInt16LittleEndian(int value)
        {
            return Write(new byte[2]);
        }

        public bool Flush()
        {
            return true;
        }

        public long GetByteOffset()
        {
            return Offset;
        }
    }
}
