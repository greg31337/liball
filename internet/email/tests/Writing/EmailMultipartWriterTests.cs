using System.Text;
using email;

namespace email.Tests;

public class EmailMultipartWriterTests
{
    [Test]
    public void CanonicalMultipartWritesGeneratedDelimitersAndChildren()
    {
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateTextWithValue("first"),
            new EmailEntityFactory().CreateTextWithValue("second")
        });
        EmailWriteOptions options = DeterministicOptions("ordinary multipart");
        string boundary = new EmailBoundaryGenerator().CreateCandidate(options, 0);

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(multipart, options);
        string output = Text(result.GetBytes());
        EmailEntityParseResult reparsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(result.GetBytes(), new EmailParseOptionsOperations().CreatePreserving());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.Contain("boundary=\"" + boundary + "\""));
        Assert.That(output, Does.Contain("Content-Transfer-Encoding: 7bit\r\n"));
        Assert.That(output, Does.Contain("\r\n\r\n--" + boundary + "\r\n"));
        Assert.That(output, Does.Contain("\r\n--" + boundary + "\r\n"));
        Assert.That(output, Does.EndWith("\r\n--" + boundary + "--\r\n"));
        Assert.That(reparsed.IsComplete(), Is.True);
        Assert.That(reparsed.GetEntity()!.GetChildCount(), Is.EqualTo(2));
        Assert.That(ContentText(reparsed.GetEntity()!.GetChildAt(0)), Is.EqualTo("first"));
        Assert.That(ContentText(reparsed.GetEntity()!.GetChildAt(1)), Is.EqualTo("second"));
    }

    [Test]
    public void PreambleAndEpilogueArePlacedOutsideGeneratedDelimitersAndScanned()
    {
        EmailWriteOptions options = DeterministicOptions("preamble collision");
        string rejectedBoundary = new EmailBoundaryGenerator().CreateCandidate(options, 0);
        string selectedBoundary = new EmailBoundaryGenerator().CreateCandidate(options, 1);
        string source = "Content-Type: multipart/mixed; boundary=old\r\n\r\nintro\r\n--" + rejectedBoundary + "\r\nstill preamble\r\n--old\r\n\r\nbody\r\n--old--\r\nepilogue";
        MimeEntity multipart = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving()).GetEntity()!;

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(multipart!, options);
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Boundary(new EmailParser().ParseEntityWithInputUsingValueArray(result.GetBytes()).GetEntity()!), Is.EqualTo(selectedBoundary));
        Assert.That(output, Does.Contain("\r\n\r\nintro\r\n--" + rejectedBoundary + "\r\nstill preamble\r\n--" + selectedBoundary + "\r\n"));
        Assert.That(output, Does.EndWith("\r\n--" + selectedBoundary + "--\r\nepilogue\r\n"));
    }

    [Test]
    public void EmptyMultipartWritesOnlyAClosingDelimiter()
    {
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtype("mixed");
        EmailWriteOptions options = DeterministicOptions("empty multipart");
        string boundary = new EmailBoundaryGenerator().CreateCandidate(options, 0);

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(multipart, options);
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.EndWith("\r\n\r\n--" + boundary + "--\r\n"));
        Assert.That(output, Does.Not.Contain("\r\n--" + boundary + "\r\n"));
        Assert.That(Occurrences(output, "--" + boundary), Is.EqualTo(1));
    }

    [Test]
    public void NestedMultipartUsesDifferentCollisionFreeBoundaries()
    {
        MimeEntity inner = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateTextWithValue("inside")
        });
        MimeEntity outer = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>() { inner });
        EmailWriteOptions options = DeterministicOptions("nested multipart");

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(outer, options);
        EmailEntityParseResult reparsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(result.GetBytes(), new EmailParseOptionsOperations().CreatePreserving());
        MimeEntity reparsedOuter = reparsed.GetEntity()!;
        MimeEntity reparsedInner = reparsedOuter!.GetChildAt(0);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(reparsed.IsComplete(), Is.True);
        Assert.That(Boundary(reparsedInner), Is.EqualTo(new EmailBoundaryGenerator().CreateCandidate(options, 0)));
        Assert.That(Boundary(reparsedOuter), Is.EqualTo(new EmailBoundaryGenerator().CreateCandidate(options, 1)));
        Assert.That(Boundary(reparsedOuter), Is.Not.EqualTo(Boundary(reparsedInner)));
    }

    [Test]
    public void SignedFirstPartRetainsRawHeaderFoldingAndBodyBytes()
    {
        const string signedPart = "x-CuStOm: alpha\r\n\tbeta\r\nContent-Type: text/plain\r\nContent-Transfer-Encoding: 7bit\r\n\r\nsigned body";
        const string sourcePrefix = "Content-Type: multipart/signed; boundary=old; protocol=application/pkcs7-signature\r\n\r\n--old\r\n";
        const string sourceSuffix = "\r\n--old\r\nContent-Type: application/pkcs7-signature\r\nContent-Transfer-Encoding: base64\r\n\r\nc2ln\r\n--old--\r\n";
        MimeEntity signed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(sourcePrefix + signedPart + sourceSuffix), new EmailParseOptionsOperations().CreatePreserving()).GetEntity()!;
        EmailWriteOptions options = DeterministicOptions("signed multipart");
        string boundary = new EmailBoundaryGenerator().CreateCandidate(options, 0);

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(signed!, options);
        string output = Text(result.GetBytes());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(output, Does.Contain("--" + boundary + "\r\n" + signedPart + "\r\n--" + boundary + "\r\n"));
        Assert.That(output, Does.Not.Contain("X-Custom: alpha beta"));
        Assert.That(new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(result.GetBytes(), new EmailParseOptionsOperations().CreatePreserving()).IsComplete(), Is.True);
    }

    [Test]
    public void MultipartWritingEnforcesDepthAndPartLimitsWithoutPartialOutput()
    {
        MimeEntity nested = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity>()
            {
                new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity>() { new EmailEntityFactory().CreateTextWithValue("deep") })
            })
        });
        EmailWriteOptions depthOptions = DeterministicOptions("depth limit");
        depthOptions.SetMaximumNestingDepth(1);
        EmailWriteOptions partOptions = DeterministicOptions("part limit");
        partOptions.SetMaximumPartCount(1);

        EmailWriteResult depth = new EmailWriter().WriteEntityWithEntityAndOptions(nested, depthOptions);
        EmailWriteResult parts = new EmailWriter().WriteEntityWithEntityAndOptions(new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>() { new EmailEntityFactory().CreateTextWithValue("part") }), partOptions);

        AssertRejected(depth);
        AssertRejected(parts);
    }

    static void AssertRejected(EmailWriteResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.WritePolicyViolation), Is.True);
    }

    static EmailWriteOptions DeterministicOptions(string seed)
    {
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Bytes(seed));
        return options;
    }

    static string Boundary(MimeEntity entity)
    {
        return Text(entity.GetMediaType()!.GetParameters().GetFirst("boundary")!.GetValueBytes());
    }

    static string ContentText(MimeEntity entity)
    {
        return Text(entity.GetRawContent()!.Materialize(1000));
    }

    static int Occurrences(string value, string pattern)
    {
        int count = 0;
        int index = 0;
        while (index >= 0)
        {
            index = value.IndexOf(pattern, index, StringComparison.Ordinal);
            if (index >= 0)
            {
                count++;
                index += pattern.Length;
            }
        }

        return count;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
