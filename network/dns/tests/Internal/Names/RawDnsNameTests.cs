namespace dns.Tests.Internal.Names;

public class RawDnsNameTests
{
    [Test]
    public void RootNameIsAbsoluteEmptyAndOneWireOctet()
    {
        RawDnsNameResult first = RawDnsName.CreateRootName();
        RawDnsNameResult second = RawDnsName.CreateRootName();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Name, Is.Not.Null);
            Assert.That(first.Name!.IsAbsolute, Is.True);
            Assert.That(first.Name.Labels, Is.Empty);
            Assert.That(first.LabelCount, Is.Zero);
            Assert.That(first.ExpandedWireLength, Is.EqualTo(1));
            Assert.That(first.Diagnostics, Is.Empty);
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.Name!.Labels, Is.Not.SameAs(first.Name.Labels));
        });
    }

    [Test]
    public void EmptyRelativeNameRemainsDistinctFromRoot()
    {
        RawDnsNameResult result = RawDnsName.CreateRelativeName(new List<byte[]>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.Not.Null);
            Assert.That(result.Name!.IsAbsolute, Is.False);
            Assert.That(result.Name.Labels, Is.Empty);
            Assert.That(result.ExpandedWireLength, Is.EqualTo(1));
        });
    }

    [Test]
    public void AbsoluteConstructionPreservesCaseOctetsAndOwnsLabels()
    {
        byte[] firstLabel = [0x57, 0x77, 0x57];
        byte[] secondLabel = [0x45, 0x78, 0x41, 0x6d, 0x50, 0x6c, 0x45];
        byte[] binaryLabel = [0, 0x80, 0xff];
        List<byte[]> labels = new List<byte[]> { firstLabel, secondLabel, binaryLabel };

        RawDnsNameResult result = RawDnsName.CreateAbsoluteName(labels);
        firstLabel[0] = 0;
        labels.Add([1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(result.Name.Labels, Has.Count.EqualTo(3));
            Assert.That(result.Name.Labels[0],
                Is.EqualTo(new byte[] { 0x57, 0x77, 0x57 }));
            Assert.That(result.Name.Labels[1],
                Is.EqualTo(new byte[] { 0x45, 0x78, 0x41, 0x6d, 0x50, 0x6c, 0x45 }));
            Assert.That(result.Name.Labels[2],
                Is.EqualTo(new byte[] { 0, 0x80, 0xff }));
            Assert.That(result.Name.Labels[0], Is.Not.SameAs(firstLabel));
            Assert.That(result.LabelCount, Is.EqualTo(3));
            Assert.That(result.ExpandedWireLength, Is.EqualTo(17));
        });
    }

    [Test]
    public void RelativeConstructionPreservesLabelsWithoutMakingThemAbsolute()
    {
        RawDnsNameResult result = RawDnsName.CreateRelativeName(
            new List<byte[]> { new byte[] { 0x41 }, new byte[] { 0x62 } });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.IsAbsolute, Is.False);
            Assert.That(result.Name.Labels[0], Is.EqualTo(new byte[] { 0x41 }));
            Assert.That(result.Name.Labels[1], Is.EqualTo(new byte[] { 0x62 }));
            Assert.That(result.LabelCount, Is.EqualTo(2));
            Assert.That(result.ExpandedWireLength, Is.EqualTo(5));
        });
    }

    [Test]
    public void ValidationReturnsAnIndependentCasePreservingName()
    {
        DnsName source = new DnsName();
        source.IsAbsolute = true;
        source.Labels.Add([0x4d, 0x69, 0x58, 0x65, 0x44]);

        RawDnsNameResult result = RawDnsName.ValidateName(source);
        source.Labels[0][0] = 0x6d;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.Not.SameAs(source));
            Assert.That(result.Name!.Labels, Is.Not.SameAs(source.Labels));
            Assert.That(result.Name.Labels[0], Is.Not.SameAs(source.Labels[0]));
            Assert.That(result.Name.Labels[0],
                Is.EqualTo(new byte[] { 0x4d, 0x69, 0x58, 0x65, 0x44 }));
            Assert.That(result.Name.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void LabelAtMaximumLengthIsAccepted()
    {
        byte[] label = new byte[DnsProtocolLimits.MaximumLabelLength];
        int index = 0;
        while (index < label.Length)
        {
            label[index] = (byte)index;
            index = index + 1;
        }

        RawDnsNameResult result = RawDnsName.CreateAbsoluteName(
            new List<byte[]> { label });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.Labels[0], Has.Length.EqualTo(63));
            Assert.That(result.ExpandedWireLength, Is.EqualTo(65));
        });
    }

    [Test]
    public void EmptyAndOverlongLabelsAreRejected()
    {
        RawDnsNameResult empty = RawDnsName.CreateAbsoluteName(
            new List<byte[]> { Array.Empty<byte>() });
        RawDnsNameResult overlong = RawDnsName.CreateAbsoluteName(
            new List<byte[]> { new byte[64] });

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Name, Is.Null);
            Assert.That(empty.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(empty.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(empty.Diagnostics[0].Field, Is.EqualTo("labels[0]"));
            Assert.That(overlong.Success, Is.False);
            Assert.That(overlong.Name, Is.Null);
            Assert.That(overlong.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        });
    }

    [Test]
    public void ExactExpandedNameLimitIsAcceptedForAbsoluteAndRelativeNames()
    {
        List<byte[]> labels = new List<byte[]>
        {
            new byte[63],
            new byte[63],
            new byte[63],
            new byte[61]
        };

        RawDnsNameResult absolute = RawDnsName.CreateAbsoluteName(labels);
        RawDnsNameResult relative = RawDnsName.CreateRelativeName(labels);

        Assert.Multiple(() =>
        {
            Assert.That(absolute.Success, Is.True);
            Assert.That(absolute.ExpandedWireLength,
                Is.EqualTo(DnsProtocolLimits.MaximumNameLength));
            Assert.That(absolute.Name!.IsAbsolute, Is.True);
            Assert.That(relative.Success, Is.True);
            Assert.That(relative.ExpandedWireLength,
                Is.EqualTo(DnsProtocolLimits.MaximumNameLength));
            Assert.That(relative.Name!.IsAbsolute, Is.False);
        });
    }

    [Test]
    public void ExpandedNameBeyondLimitIsRejectedAtTheOverflowingLabel()
    {
        List<byte[]> labels = new List<byte[]>
        {
            new byte[63],
            new byte[63],
            new byte[63],
            new byte[62]
        };

        RawDnsNameResult result = RawDnsName.CreateAbsoluteName(labels);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.NameTooLong));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("labels[3]"));
        });
    }

    [Test]
    public void MaximumOneOctetLabelCountIsBoundedByExpandedLength()
    {
        List<byte[]> acceptedLabels = new List<byte[]>();
        List<byte[]> rejectedLabels = new List<byte[]>();
        int index = 0;
        while (index < 128)
        {
            byte[] label = [(byte)index];
            rejectedLabels.Add(label);
            if (index < 127)
            {
                acceptedLabels.Add(label);
            }

            index = index + 1;
        }

        RawDnsNameResult accepted = RawDnsName.CreateAbsoluteName(acceptedLabels);
        RawDnsNameResult rejected = RawDnsName.CreateAbsoluteName(rejectedLabels);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.LabelCount, Is.EqualTo(127));
            Assert.That(accepted.ExpandedWireLength, Is.EqualTo(255));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.NameTooLong));
            Assert.That(rejected.Diagnostics[0].Field, Is.EqualTo("labels[127]"));
        });
    }

    [Test]
    public void NullMutableInputsProduceDiagnosticsInsteadOfRuntimeFailures()
    {
        DnsName nameWithNullCollection = new DnsName();
        nameWithNullCollection.Labels = null!;
        List<byte[]> labelsWithNull = new List<byte[]> { null! };

        RawDnsNameResult nullName = RawDnsName.ValidateName(null!);
        RawDnsNameResult nullCollection = RawDnsName.ValidateName(nameWithNullCollection);
        RawDnsNameResult nullLabel = RawDnsName.CreateRelativeName(labelsWithNull);

        Assert.Multiple(() =>
        {
            Assert.That(nullName.Success, Is.False);
            Assert.That(nullName.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(nullName.Diagnostics[0].Field, Is.EqualTo("name"));
            Assert.That(nullCollection.Success, Is.False);
            Assert.That(nullCollection.Diagnostics[0].Field, Is.EqualTo("labels"));
            Assert.That(nullLabel.Success, Is.False);
            Assert.That(nullLabel.Diagnostics[0].Field, Is.EqualTo("labels[0]"));
        });
    }
}
