using icc;

namespace icc.Tests;

public class ContractUsageTests
{
    [Test]
    public void ProfileIoContractsSupportByteMemoryBufferAndStreamWorkflows()
    {
        IIccProfileReader reader = new ContractProfileReader();
        IIccProfileWriter writer = new ContractProfileWriter();
        byte[] encodedProfile = { 10, 20, 30, 40 };
        IccReadOptions readOptions = new IccReadOptions();
        IccWriteOptions writeOptions = new IccWriteOptions();

        IccProfile bytesProfile = reader.ReadProfileBytes(encodedProfile, readOptions);
        IccProfile memoryProfile = reader.ReadProfileMemory(encodedProfile.AsMemory(), readOptions);
        using MemoryStream sourceStream = new MemoryStream(encodedProfile);
        IccProfile streamProfile = reader.ReadProfileStream(sourceStream, readOptions);

        bytesProfile.OriginalBytes = encodedProfile;
        byte[] destination = new byte[encodedProfile.Length];
        IccOperationResult bufferResult = writer.TryWriteProfileBuffer(bytesProfile, destination, writeOptions);
        using MemoryStream destinationStream = new MemoryStream();
        writer.WriteProfileStream(bytesProfile, destinationStream, writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(bytesProfile.Header.DeclaredSize, Is.EqualTo(4));
            Assert.That(memoryProfile.Header.DeclaredSize, Is.EqualTo(4));
            Assert.That(streamProfile.Header.DeclaredSize, Is.EqualTo(4));
            Assert.That(reader.ProbeProfileBytes(encodedProfile, readOptions).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(writer.MeasureProfileBytes(bytesProfile, writeOptions), Is.EqualTo(4));
            Assert.That(writer.WriteProfileBytes(bytesProfile, writeOptions), Is.EqualTo(encodedProfile));
            Assert.That(bufferResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(destination, Is.EqualTo(encodedProfile));
            Assert.That(destinationStream.ToArray(), Is.EqualTo(encodedProfile));
        });
    }

    [Test]
    public void TransformFactoryContractProducesAnExecutableTransformContract()
    {
        IIccColorTransform executable = new ContractColorTransform();
        IIccTransformFactory factory = new ContractTransformFactory(executable);
        IccProfile sourceProfile = new IccProfile();
        IccProfile destinationProfile = new IccProfile();

        IIccColorTransform transform = factory.CreateProfileTransform(
            sourceProfile,
            destinationProfile,
            IccRenderingIntent.Perceptual,
            IccIntentFallback.AllowAny,
            IccInterpolationKind.Linear);
        float[] transformed = transform.TransformSingleColor(new float[] { 0.25f, 0.75f });
        float[] destination = new float[4];
        transform.TransformInterleavedColors(
            new float[] { 0.1f, 0.2f, 0.3f, 0.4f },
            2,
            destination,
            2,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(transform.GetSourceChannelCount(), Is.EqualTo(2));
            Assert.That(transform.GetDestinationChannelCount(), Is.EqualTo(2));
            Assert.That(transformed, Is.EqualTo(new float[] { 0.75f, 0.25f }));
            Assert.That(destination, Is.EqualTo(new float[] { 0.2f, 0.1f, 0.4f, 0.3f }));
            Assert.That(transform.ProbeTransformColor(new float[] { 0.5f, 0.5f }).Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(factory.ProbeProfileTransform(sourceProfile, destinationProfile, IccRenderingIntent.Perceptual).Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    private class ContractProfileReader : IIccProfileReader
    {
        public IccProfile ReadProfileBytes(ReadOnlySpan<byte> profileBytes, IccReadOptions options)
        {
            return ProfileWithSize(profileBytes.Length);
        }

        public IccProfile ReadProfileMemory(ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
        {
            return ProfileWithSize(profileMemory.Length);
        }

        public IccProfile ReadProfileStream(Stream profileStream, IccReadOptions options)
        {
            return ProfileWithSize((int)(profileStream.Length - profileStream.Position));
        }

        public IccOperationResult ProbeProfileBytes(ReadOnlySpan<byte> profileBytes, IccReadOptions options)
        {
            return SuccessfulResult();
        }

        public IccOperationResult ProbeProfileMemory(ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
        {
            return SuccessfulResult();
        }

        public IccOperationResult ProbeProfileStream(Stream profileStream, IccReadOptions options)
        {
            return SuccessfulResult();
        }

        private static IccProfile ProfileWithSize(int size)
        {
            IccProfile profile = new IccProfile();
            profile.Header.DeclaredSize = size;
            return profile;
        }
    }

    private class ContractProfileWriter : IIccProfileWriter
    {
        public int MeasureProfileBytes(IccProfile profile, IccWriteOptions options)
        {
            return profile.OriginalBytes.Length;
        }

        public byte[] WriteProfileBytes(IccProfile profile, IccWriteOptions options)
        {
            return profile.OriginalBytes.ToArray();
        }

        public IccOperationResult TryWriteProfileBuffer(IccProfile profile, Span<byte> destination, IccWriteOptions options)
        {
            if (destination.Length < profile.OriginalBytes.Length)
            {
                IccOperationResult failure = new IccOperationResult();
                failure.Status = IccOperationStatus.ResourceLimitExceeded;
                return failure;
            }

            profile.OriginalBytes.CopyTo(destination);
            return SuccessfulResult();
        }

        public void WriteProfileStream(IccProfile profile, Stream destinationStream, IccWriteOptions options)
        {
            destinationStream.Write(profile.OriginalBytes);
        }
    }

    private class ContractTransformFactory : IIccTransformFactory
    {
        private readonly IIccColorTransform transform;

        public ContractTransformFactory(IIccColorTransform transform)
        {
            this.transform = transform;
        }

        public IIccColorTransform CreateProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent, IccIntentFallback fallback, IccInterpolationKind interpolation)
        {
            return transform;
        }

        public IIccColorTransform CreateConfiguredProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccTransformOptions options)
        {
            return transform;
        }

        public IIccColorTransform CreateProofingTransform(IccProfile sourceProfile, IccProfile proofProfile, IccProfile destinationProfile, IccRenderingIntent renderingIntent, IccRenderingIntent proofingIntent)
        {
            return transform;
        }

        public IIccColorTransform CreateConfiguredProofingTransform(IccProfile sourceProfile, IccProfile proofProfile, IccProfile destinationProfile, IccProofingOptions options)
        {
            return transform;
        }

        public IIccColorTransform CreateProfileSequenceTransform(IccProfile[] profiles, IccRenderingIntent intent, IccIntentFallback fallback)
        {
            return transform;
        }

        public IIccColorTransform CreateConfiguredProfileSequenceTransform(IccProfile[] profiles, IccTransformOptions options)
        {
            return transform;
        }

        public IIccColorTransform CreateDeviceLinkTransform(IccProfile deviceLinkProfile, IccTransformOptions options)
        {
            return transform;
        }

        public IccOperationResult ProbeProfileTransform(IccProfile sourceProfile, IccProfile destinationProfile, IccRenderingIntent intent)
        {
            return SuccessfulResult();
        }
    }

    private class ContractColorTransform : IIccColorTransform
    {
        public int GetSourceChannelCount()
        {
            return 2;
        }

        public int GetDestinationChannelCount()
        {
            return 2;
        }

        public float[] TransformSingleColor(ReadOnlySpan<float> sourceColor)
        {
            return new float[] { sourceColor[1], sourceColor[0] };
        }

        public void TransformInterleavedColors(ReadOnlySpan<float> sourceColors, int sourceStride, Span<float> destinationColors, int destinationStride, int colorCount)
        {
            int index = 0;
            while (index < colorCount)
            {
                destinationColors[index * destinationStride] = sourceColors[(index * sourceStride) + 1];
                destinationColors[(index * destinationStride) + 1] = sourceColors[index * sourceStride];
                index = index + 1;
            }
        }

        public IccOperationResult ProbeTransformColor(ReadOnlySpan<float> sourceColor)
        {
            return SuccessfulResult();
        }
    }

    private static IccOperationResult SuccessfulResult()
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = IccOperationStatus.Success;
        return result;
    }
}
