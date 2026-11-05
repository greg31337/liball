namespace avif.Tests;

public class AvifImageStorageTests
{
    [TestCase(AvifChromaFormat.Monochrome, 1, 0, 0)]
    [TestCase(AvifChromaFormat.Yuv420, 3, 3, 2)]
    [TestCase(AvifChromaFormat.Yuv422, 3, 3, 3)]
    [TestCase(AvifChromaFormat.Yuv444, 3, 5, 3)]
    public void ImagesAllocateEveryPlanarChromaLayout(
        AvifChromaFormat chromaFormat,
        int expectedPlaneCount,
        int expectedChromaWidth,
        int expectedChromaHeight)
    {
        int[] bitDepths = new int[] { 8, 10, 12, 16 };
        for (int index = 0; index < bitDepths.Length; index++)
        {
            AvifImage image = AvifImage.Create(
                5, 3, bitDepths[index], chromaFormat, false);
            AvifPlane? y = image.GetPlane(AvifPlaneKind.Y);
            AvifPlane? u = image.GetPlane(AvifPlaneKind.U);
            AvifPlane? v = image.GetPlane(AvifPlaneKind.V);

            Assert.Multiple(() =>
            {
                Assert.That(image.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(image.GetPlaneCount(), Is.EqualTo(expectedPlaneCount));
                Assert.That((y ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(5));
                Assert.That(y.GetHeight(), Is.EqualTo(3));
                Assert.That(y.GetBitDepth(), Is.EqualTo(bitDepths[index]));
                Assert.That(
                    y.GetRowByteCount(),
                    Is.EqualTo(5 * (bitDepths[index] > 8 ? 2 : 1)));
                Assert.That(u == null, Is.EqualTo(expectedPlaneCount == 1));
                Assert.That(v == null, Is.EqualTo(expectedPlaneCount == 1));
                if (u != null)
                {
                    Assert.That(u.GetWidth(), Is.EqualTo(expectedChromaWidth));
                    Assert.That(u.GetHeight(), Is.EqualTo(expectedChromaHeight));
                    Assert.That((v ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(expectedChromaWidth));
                    Assert.That(v.GetHeight(), Is.EqualTo(expectedChromaHeight));
                }
            });

            image.Close();
        }
    }

    [Test]
    public void ImageViewsShareAlignedRegionsAndCopiesBecomeIndependent()
    {
        AvifImage owner = AvifImage.Create(
            5,
            5,
            10,
            AvifChromaFormat.Yuv420,
            AvifRange.Limited,
            AvifChromaSamplePosition.Vertical,
            AvifAlphaMode.Premultiplied,
            8,
            new AvifSecurityLimits());
        (owner.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(2)[4] = 7;
        (owner.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(1)[2] = 9;

        AvifImage view = owner.CreateView(2, 2, 3, 3);
        AvifImage misaligned = owner.CreateView(1, 0, 2, 2);
        (view.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = 11;
        AvifImage copy = view.CreateCopy();
        (view.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0] = 13;

        Assert.Multiple(() =>
        {
            Assert.That(owner.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(owner.GetPlaneCount(), Is.EqualTo(4));
            Assert.That(owner.GetAlphaMode(), Is.EqualTo(AvifAlphaMode.Premultiplied));
            Assert.That(owner.GetRange(), Is.EqualTo(AvifRange.Limited));
            Assert.That(view.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(view.GetWidth(), Is.EqualTo(3));
            Assert.That(view.GetHeight(), Is.EqualTo(3));
            Assert.That((view.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(2));
            Assert.That((view.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(2));
            Assert.That((view.GetPlane(AvifPlaneKind.Alpha) ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(3));
            Assert.That((view.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).OwnsStorage(), Is.False);
            Assert.That((owner.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(2)[4], Is.EqualTo(13));
            Assert.That((view.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0], Is.EqualTo(9));
            Assert.That((copy.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0], Is.EqualTo(11));
            Assert.That((copy.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).OwnsStorage(), Is.True);
            Assert.That(misaligned.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        owner.Close();
        Assert.Multiple(() =>
        {
            Assert.That(view.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(copy.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((copy.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0)[0], Is.EqualTo(11));
        });

        view.Close();
        copy.Close();
        misaligned.Close();
    }

    [Test]
    public void ImageAllocationEnforcesCombinedPlaneAndDimensionLimits()
    {
        AvifSecurityLimits tooSmall = new AvifSecurityLimits();
        tooSmall.MaxDecodedBytes = 47;
        AvifSecurityLimits exact = new AvifSecurityLimits();
        exact.MaxDecodedBytes = 48;
        AvifSecurityLimits narrow = new AvifSecurityLimits();
        narrow.MaxWidth = 3;

        AvifImage limited = AvifImage.Create(
            4,
            4,
            8,
            AvifChromaFormat.Yuv444,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            tooSmall);
        AvifImage exactImage = AvifImage.Create(
            4,
            4,
            8,
            AvifChromaFormat.Yuv444,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            exact);
        AvifImage dimensionLimited = AvifImage.Create(
            4,
            2,
            8,
            AvifChromaFormat.Monochrome,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            narrow);
        AvifImage invalidDepth = AvifImage.Create(
            4, 4, 9, AvifChromaFormat.Yuv444, false);

        Assert.Multiple(() =>
        {
            Assert.That(limited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limited.GetPlaneCount(), Is.Zero);
            Assert.That(exactImage.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(exactImage.GetStorageByteCount(), Is.EqualTo(48));
            Assert.That(dimensionLimited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(invalidDepth.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        limited.Close();
        exactImage.Close();
        dimensionLimited.Close();
        invalidDepth.Close();
    }

    [Test]
    public void PublicPlaneSupportsAlignedViewsCopiesAndLifecycle()
    {
        AvifPlane owner = AvifPlane.Create(
            3, 2, 10, AvifPlaneKind.Y, 8, 16);
        new byte[] { 1, 2, 3, 4, 5, 6 }.AsSpan().CopyTo(
            owner.GetRowSpan(0));
        AvifPlane view = owner.CreateView(1, 0, 2, 2);
        AvifPlane copy = view.CreateCopy();
        view.GetRowSpan(0)[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(owner.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(owner.GetStride(), Is.EqualTo(8));
            Assert.That(owner.OwnsStorage(), Is.True);
            Assert.That(view.OwnsStorage(), Is.False);
            Assert.That(owner.GetRowSpan(0)[2], Is.EqualTo(9));
            Assert.That(copy.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
        });

        owner.Close();
        Assert.Multiple(() =>
        {
            Assert.That(view.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(copy.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        view.Close();
        copy.Close();
    }
}
