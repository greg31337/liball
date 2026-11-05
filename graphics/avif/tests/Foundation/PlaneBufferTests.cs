namespace avif.Tests;

public class PlaneBufferTests
{
    [Test]
    public void PlaneUsesCheckedAlignedStrideAndClearedPadding()
    {
        PlaneBuffer plane = new PlaneBuffer(3, 2, 2, 8, 16, true);
        new byte[] { 1, 2, 3, 4, 5, 6 }.AsSpan().CopyTo(plane.GetRowSpan(0));
        new byte[] { 7, 8, 9, 10, 11, 12 }.AsSpan().CopyTo(plane.GetRowSpan(1));

        Assert.Multiple(() =>
        {
            Assert.That(plane.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(plane.Stride, Is.EqualTo(8));
            Assert.That((plane.Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Length, Is.EqualTo(16));
            Assert.That(plane.GetRowSpan(0).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(plane.GetRowSpan(1).ToArray(), Is.EqualTo(new byte[] { 7, 8, 9, 10, 11, 12 }));
            Assert.That(plane.Buffer.Data.AsSpan(6, 2).ToArray(), Is.EqualTo(new byte[] { 0, 0 }));
            Assert.That(plane.Buffer.Data.AsSpan(14, 2).ToArray(), Is.EqualTo(new byte[] { 0, 0 }));
        });

        plane.Close();
    }

    [Test]
    public void PlaneViewSharesVisiblePixelsWithoutOwningStorage()
    {
        PlaneBuffer owner = new PlaneBuffer(4, 2, 1, 4, 8, true);
        new byte[] { 1, 2, 3, 4 }.AsSpan().CopyTo(owner.GetRowSpan(0));
        new byte[] { 5, 6, 7, 8 }.AsSpan().CopyTo(owner.GetRowSpan(1));
        PlaneBuffer view = owner.CreateView(1, 0, 2, 2);

        view.GetRowSpan(1)[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(view.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(view.OwnsBuffer, Is.False);
            Assert.That(view.Stride, Is.EqualTo(owner.Stride));
            Assert.That(view.GetRowSpan(0).ToArray(), Is.EqualTo(new byte[] { 2, 3 }));
            Assert.That(owner.GetRowSpan(1).ToArray(), Is.EqualTo(new byte[] { 5, 9, 7, 8 }));
        });

        view.Close();
        Assert.That(owner.GetRowSpan(0).Length, Is.EqualTo(4));
        owner.Close();
    }

    [Test]
    public void PlaneCopyIsIndependentAndCopiesOnlyVisibleRows()
    {
        PlaneBuffer owner = new PlaneBuffer(3, 2, 1, 4, 8, true);
        new byte[] { 1, 2, 3 }.AsSpan().CopyTo(owner.GetRowSpan(0));
        new byte[] { 4, 5, 6 }.AsSpan().CopyTo(owner.GetRowSpan(1));
        (owner.Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Data[3] = 99;
        owner.Buffer.Data[7] = 88;

        PlaneBuffer copy = owner.CreateCopy();
        copy.GetRowSpan(0)[0] = 7;

        Assert.Multiple(() =>
        {
            Assert.That(copy.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(copy.GetRowSpan(0).ToArray(), Is.EqualTo(new byte[] { 7, 2, 3 }));
            Assert.That(copy.GetRowSpan(1).ToArray(), Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(owner.GetRowSpan(0).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That((copy.Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Data[3], Is.Zero);
            Assert.That(copy.Buffer.Data[7], Is.Zero);
        });

        owner.Close();
        Assert.That(copy.GetRowSpan(1).ToArray(), Is.EqualTo(new byte[] { 4, 5, 6 }));
        copy.Close();
    }

    [Test]
    public void CopyBetweenOverlappingViewsUsesOriginalSourceRows()
    {
        PlaneBuffer owner = new PlaneBuffer(2, 3, 1, 1, 6, true);
        new byte[] { 1, 2 }.AsSpan().CopyTo(owner.GetRowSpan(0));
        new byte[] { 3, 4 }.AsSpan().CopyTo(owner.GetRowSpan(1));
        new byte[] { 5, 6 }.AsSpan().CopyTo(owner.GetRowSpan(2));
        PlaneBuffer source = owner.CreateView(0, 0, 2, 2);
        PlaneBuffer destination = owner.CreateView(0, 1, 2, 2);

        AvifResultCode result = destination.CopyFrom(source);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(owner.GetRowSpan(0).ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(owner.GetRowSpan(1).ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(owner.GetRowSpan(2).ToArray(), Is.EqualTo(new byte[] { 3, 4 }));
        });

        source.Close();
        destination.Close();
        owner.Close();
    }

    [Test]
    public void ClosingPlaneOwnerInvalidatesViewsAndCleanupIsIdempotent()
    {
        PlaneBuffer owner = new PlaneBuffer(2, 2, 1, 2, 4, true);
        PlaneBuffer view = owner.CreateView(0, 0, 1, 2);

        AvifResultCode firstClose = owner.Close();
        AvifResultCode secondClose = owner.Close();

        Assert.Multiple(() =>
        {
            Assert.That(firstClose, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondClose, Is.EqualTo(AvifResultCode.Success));
            Assert.That(owner.Buffer, Is.Null);
            Assert.That(view.GetStateCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(view.GetRowSpan(0).Length, Is.Zero);
        });

        view.Close();
    }

    [Test]
    public void PlaneRejectsInvalidGeometryBoundsAndAllocationLimits()
    {
        PlaneBuffer invalid = new PlaneBuffer(0, 2, 1, 1, 8, true);
        PlaneBuffer limited = new PlaneBuffer(3, 2, 2, 8, 15, true);
        PlaneBuffer valid = new PlaneBuffer(4, 3, 1, 4, 12, true);
        PlaneBuffer outside = valid.CreateView(3, 0, 2, 1);
        PlaneBuffer empty = valid.CreateView(0, 0, 0, 1);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(outside.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(empty.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalid.Buffer, Is.Null);
            Assert.That(limited.Buffer, Is.Null);
        });

        valid.Close();
    }
}
