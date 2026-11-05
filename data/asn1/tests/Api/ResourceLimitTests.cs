using asn1;

namespace asn1.Tests;

public class ResourceLimitTests
{
    [Test]
    public void DefaultsBoundEveryResourceDimension()
    {
        AsnResourceLimits limits = new AsnResourceLimits();

        Assert.That(limits.Unlimited, Is.False);
        Assert.That(limits.MaximumNestingDepth, Is.EqualTo(128));
        Assert.That(limits.MaximumCollectionElements, Is.EqualTo(1000000));
        Assert.That(limits.MaximumTagOctets, Is.EqualTo(16));
        Assert.That(limits.MaximumLengthOctets, Is.EqualTo(8));
        Assert.That(limits.MaximumIntegerBits, Is.EqualTo(1048576));
        Assert.That(limits.MaximumTextCharacters, Is.EqualTo(16777216));
        Assert.That(limits.MaximumOpenTypeBytes, Is.EqualTo(16777216));
        Assert.That(limits.MaximumTotalAllocationBytes, Is.EqualTo(268435456));
        Assert.That(limits.MaximumDiagnostics, Is.EqualTo(10000));
    }

    [Test]
    public void CallersCanSelectSmallerOperationSpecificBounds()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumNestingDepth = 8,
            MaximumCollectionElements = 20,
            MaximumTagOctets = 5,
            MaximumLengthOctets = 4,
            MaximumIntegerBits = 256,
            MaximumTextCharacters = 1024,
            MaximumOpenTypeBytes = 2048,
            MaximumTotalAllocationBytes = 4096,
            MaximumDiagnostics = 12
        };

        Assert.That(limits.Unlimited, Is.False);
        Assert.That(limits.MaximumNestingDepth, Is.EqualTo(8));
        Assert.That(limits.MaximumCollectionElements, Is.EqualTo(20));
        Assert.That(limits.MaximumTagOctets, Is.EqualTo(5));
        Assert.That(limits.MaximumLengthOctets, Is.EqualTo(4));
        Assert.That(limits.MaximumIntegerBits, Is.EqualTo(256));
        Assert.That(limits.MaximumTextCharacters, Is.EqualTo(1024));
        Assert.That(limits.MaximumOpenTypeBytes, Is.EqualTo(2048));
        Assert.That(limits.MaximumTotalAllocationBytes, Is.EqualTo(4096));
        Assert.That(limits.MaximumDiagnostics, Is.EqualTo(12));
    }

    [Test]
    public void UnlimitedBehaviorRequiresAnExplicitRequest()
    {
        AsnResourceLimits bounded = new AsnResourceLimits();
        AsnResourceLimits unlimited = new AsnResourceLimits { Unlimited = true };

        Assert.That(bounded.Unlimited, Is.False);
        Assert.That(unlimited.Unlimited, Is.True);
        Assert.That(unlimited.MaximumNestingDepth, Is.EqualTo(bounded.MaximumNestingDepth));
        Assert.That(unlimited.MaximumCollectionElements, Is.EqualTo(bounded.MaximumCollectionElements));
        Assert.That(unlimited.MaximumTagOctets, Is.EqualTo(bounded.MaximumTagOctets));
        Assert.That(unlimited.MaximumLengthOctets, Is.EqualTo(bounded.MaximumLengthOctets));
        Assert.That(unlimited.MaximumIntegerBits, Is.EqualTo(bounded.MaximumIntegerBits));
        Assert.That(unlimited.MaximumTextCharacters, Is.EqualTo(bounded.MaximumTextCharacters));
        Assert.That(unlimited.MaximumOpenTypeBytes, Is.EqualTo(bounded.MaximumOpenTypeBytes));
        Assert.That(unlimited.MaximumTotalAllocationBytes, Is.EqualTo(bounded.MaximumTotalAllocationBytes));
        Assert.That(unlimited.MaximumDiagnostics, Is.EqualTo(bounded.MaximumDiagnostics));
    }
}
