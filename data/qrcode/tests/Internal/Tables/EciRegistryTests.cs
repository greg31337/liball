using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EciRegistryTests
{
    private static readonly int[] ExpectedRegisteredAssignments = new int[]
    {
        0, 1, 2, 3,
        4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
        15, 16, 17, 18,
        20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35,
        170, 899, 1800, 799996, 799999, 800000, 810899
    };

    [Test]
    public void RegisteredAssignmentsAreCompleteOrderedAndNamed()
    {
        Assert.That(EciRegistry.GetRegisteredAssignmentCount(), Is.EqualTo(ExpectedRegisteredAssignments.Length));
        for (int index = 0; index < ExpectedRegisteredAssignments.Length; index++)
        {
            int assignment = ExpectedRegisteredAssignments[index];
            Assert.That(EciRegistry.GetRegisteredAssignment(index), Is.EqualTo(assignment));
            Assert.That(EciRegistry.IsRegistered(assignment), Is.True);
            Assert.That(EciRegistry.GetCanonicalName(assignment), Is.Not.Empty);
        }

        Assert.That(EciRegistry.IsRegistered(14), Is.False);
        Assert.That(EciRegistry.IsRegistered(19), Is.False);
        Assert.That(EciRegistry.IsRegistered(36), Is.False);
        Assert.That(EciRegistry.IsRegistered(1799), Is.False);
    }

    [TestCase("CP437", 2)]
    [TestCase("ibm-437", 2)]
    [TestCase("latin1", 3)]
    [TestCase("ISO_8859-7", 9)]
    [TestCase("shift-jis", 20)]
    [TestCase("cp1252", 23)]
    [TestCase("unicodebigunmarked", 25)]
    [TestCase("utf8", 26)]
    [TestCase("ascii", 27)]
    [TestCase("big-5", 28)]
    [TestCase("euc-cn", 29)]
    [TestCase("euc-kr", 30)]
    [TestCase("windows-936", 31)]
    [TestCase("gb-18030", 32)]
    [TestCase("utf32le", 35)]
    [TestCase("iso-ir-170", 170)]
    [TestCase("binary", 899)]
    [TestCase("gzip", 1800)]
    [TestCase("null-transformation", 799999)]
    [TestCase("application-launcher", 800000)]
    [TestCase("interpretive-prefix", 810899)]
    public void CanonicalAliasesResolveCaseInsensitively(string alias, int expectedAssignment)
    {
        Assert.That(EciRegistry.FindAssignment(alias), Is.EqualTo(expectedAssignment));
    }

    [Test]
    public void EntryKindsSeparateCharacterSetsFromProtocolEntries()
    {
        for (int assignment = 0; assignment <= 35; assignment++)
        {
            if (EciRegistry.IsRegistered(assignment))
            {
                Assert.That(EciRegistry.GetEntryKind(assignment), Is.EqualTo(EciRegistry.CharacterSetKind));
                Assert.That(EciRegistry.IsProtocolOnly(assignment), Is.False);
            }
        }

        Assert.That(EciRegistry.GetEntryKind(170), Is.EqualTo(EciRegistry.CharacterSetKind));
        Assert.That(EciRegistry.GetEntryKind(899), Is.EqualTo(EciRegistry.BinaryKind));
        Assert.That(EciRegistry.GetEntryKind(1800), Is.EqualTo(EciRegistry.TransformationKind));
        Assert.That(EciRegistry.GetEntryKind(800000), Is.EqualTo(EciRegistry.ApplicationKind));
        Assert.That(EciRegistry.GetEntryKind(799996), Is.EqualTo(EciRegistry.ProtocolControlKind));
        Assert.That(EciRegistry.GetEntryKind(799999), Is.EqualTo(EciRegistry.ProtocolControlKind));
        Assert.That(EciRegistry.GetEntryKind(810899), Is.EqualTo(EciRegistry.ProtocolControlKind));
        Assert.That(EciRegistry.IsProtocolOnly(899), Is.True);
        Assert.That(EciRegistry.IsProtocolOnly(1800), Is.True);
        Assert.That(EciRegistry.IsProtocolOnly(800000), Is.True);
    }

    [Test]
    public void BuiltInDotNetMappingsEncodeWithStrictExpectedByteOrder()
    {
        int[] supported = new int[] { 1, 3, 25, 26, 27, 33, 34, 35 };
        for (int index = 0; index < ExpectedRegisteredAssignments.Length; index++)
        {
            int assignment = ExpectedRegisteredAssignments[index];
            Assert.That(EciRegistry.HasDotNetEncoding(assignment), Is.EqualTo(Array.IndexOf(supported, assignment) >= 0));
        }

        Assert.That(EciRegistry.GetDotNetEncoding(1).GetBytes("é"), Is.EqualTo(new byte[] { 0xE9 }));
        Assert.That(EciRegistry.GetDotNetEncoding(3).GetBytes("é"), Is.EqualTo(new byte[] { 0xE9 }));
        Assert.That(EciRegistry.GetDotNetEncoding(25).GetBytes("A"), Is.EqualTo(new byte[] { 0x00, 0x41 }));
        Assert.That(EciRegistry.GetDotNetEncoding(26).GetBytes("€"), Is.EqualTo(new byte[] { 0xE2, 0x82, 0xAC }));
        Assert.That(EciRegistry.GetDotNetEncoding(27).GetBytes("A"), Is.EqualTo(new byte[] { 0x41 }));
        Assert.That(EciRegistry.GetDotNetEncoding(33).GetBytes("A"), Is.EqualTo(new byte[] { 0x41, 0x00 }));
        Assert.That(EciRegistry.GetDotNetEncoding(34).GetBytes("A"), Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x41 }));
        Assert.That(EciRegistry.GetDotNetEncoding(35).GetBytes("A"), Is.EqualTo(new byte[] { 0x41, 0x00, 0x00, 0x00 }));

        Encoding utf8 = EciRegistry.GetDotNetEncoding(26);
        Assert.That(() => utf8.GetString(new byte[] { 0xC0, 0xAF }), Throws.TypeOf<DecoderFallbackException>());
        Assert.That(() => EciRegistry.GetDotNetEncoding(20), Throws.TypeOf<NotSupportedException>());
        Assert.That(() => EciRegistry.GetDotNetEncoding(899), Throws.TypeOf<NotSupportedException>());
        Assert.That(EciRegistry.HasDotNetEncoding(999), Is.False);
    }

    [TestCase(0, 1)]
    [TestCase(127, 1)]
    [TestCase(128, 2)]
    [TestCase(16383, 2)]
    [TestCase(16384, 3)]
    [TestCase(811799, 3)]
    [TestCase(999999, 3)]
    public void DesignatorWidthsUseExactQrBoundaries(int assignment, int expectedBytes)
    {
        Assert.That(EciRegistry.GetDesignatorByteCount(assignment), Is.EqualTo(expectedBytes));
    }

    [Test]
    public void ProtocolRangesDistinguishPrivateAndInvalidAssignments()
    {
        Assert.That(EciRegistry.IsPrivateUseAssignment(399999), Is.False);
        Assert.That(EciRegistry.IsPrivateUseAssignment(400000), Is.True);
        Assert.That(EciRegistry.IsPrivateUseAssignment(799995), Is.True);
        Assert.That(EciRegistry.IsPrivateUseAssignment(799996), Is.False);
        Assert.That(EciRegistry.IsPrivateUseAssignment(810899), Is.False);
        Assert.That(EciRegistry.IsPrivateUseAssignment(810900), Is.True);
        Assert.That(EciRegistry.IsPrivateUseAssignment(811799), Is.True);
        Assert.That(EciRegistry.IsPrivateUseAssignment(811800), Is.False);

        Assert.That(EciRegistry.IsInvalidProtocolAssignment(799996), Is.False);
        Assert.That(EciRegistry.IsInvalidProtocolAssignment(799997), Is.True);
        Assert.That(EciRegistry.IsInvalidProtocolAssignment(799998), Is.True);
        Assert.That(EciRegistry.IsInvalidProtocolAssignment(799999), Is.False);
        Assert.That(EciRegistry.IsInvalidProtocolAssignment(811799), Is.False);
        Assert.That(EciRegistry.IsInvalidProtocolAssignment(811800), Is.True);
        Assert.That(EciRegistry.IsInvalidProtocolAssignment(999999), Is.True);
    }

    [Test]
    public void InvalidLookupsAndOutOfRangeAssignmentsAreRejected()
    {
        Assert.That(() => EciRegistry.GetRegisteredAssignment(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EciRegistry.GetRegisteredAssignment(ExpectedRegisteredAssignments.Length), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EciRegistry.GetCanonicalName(14), Throws.TypeOf<ArgumentException>());
        Assert.That(() => EciRegistry.GetEntryKind(19), Throws.TypeOf<ArgumentException>());
        Assert.That(() => EciRegistry.FindAssignment("not-an-eci"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => EciRegistry.FindAssignment(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => EciRegistry.GetDesignatorByteCount(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EciRegistry.GetDesignatorByteCount(1000000), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EciRegistry.IsPrivateUseAssignment(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EciRegistry.IsInvalidProtocolAssignment(1000000), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
