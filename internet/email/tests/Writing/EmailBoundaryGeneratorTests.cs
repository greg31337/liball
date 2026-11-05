using System.Text;
using email;

namespace email.Tests;

public class EmailBoundaryGeneratorTests
{
    [Test]
    public void DeterministicCandidatesAreStableSeededAndMimeSafe()
    {
        EmailWriteOptions firstOptions = DeterministicOptions("first seed");
        EmailWriteOptions secondOptions = DeterministicOptions("second seed");

        string first = new EmailBoundaryGenerator().CreateCandidate(firstOptions, 0);
        string repeated = new EmailBoundaryGenerator().CreateCandidate(firstOptions, 0);
        string nextAttempt = new EmailBoundaryGenerator().CreateCandidate(firstOptions, 1);
        string secondSeed = new EmailBoundaryGenerator().CreateCandidate(secondOptions, 0);

        Assert.That(repeated, Is.EqualTo(first));
        Assert.That(nextAttempt, Is.Not.EqualTo(first));
        Assert.That(secondSeed, Is.Not.EqualTo(first));
        Assert.That(first, Does.StartWith(EmailBoundaryGeneratorConstants.BoundaryPrefix));
        Assert.That(first.Length, Is.EqualTo(EmailBoundaryGeneratorConstants.BoundaryPrefix.Length + EmailBoundaryGeneratorConstants.DefaultRandomByteCount * 2));
        Assert.That(first.Length, Is.LessThanOrEqualTo(70));
        Assert.DoesNotThrow(() => new EmailBoundaryCollisionScanner(first));
    }

    [Test]
    public void SecureGenerationReturnsDifferentMimeSafeCandidates()
    {
        EmailBoundaryGenerationResult first = new EmailBoundaryGenerator().GenerateWith();
        EmailBoundaryGenerationResult second = new EmailBoundaryGenerator().GenerateWith();

        Assert.That(first.IsDeterministic(), Is.False);
        Assert.That(first.GetAttemptCount(), Is.EqualTo(1));
        Assert.That(first.GetCollisionCount(), Is.Zero);
        Assert.That(first.GetScannedByteCount(), Is.Zero);
        Assert.That(first.GetBoundary(), Is.Not.EqualTo(second.GetBoundary()));
        Assert.DoesNotThrow(() => new EmailBoundaryCollisionScanner(first.GetBoundary()));
    }

    [Test]
    public void ScannerFindsOpeningAndClosingDelimitersAcrossEverySplit()
    {
        byte[] opening = Bytes("preamble\r\n--boundary \t\r\nepilogue");
        byte[] closing = Bytes("preamble\n--boundary--\t");

        AssertEverySplitCollides(opening, "boundary");
        AssertEverySplitCollides(closing, "boundary");
    }

    [TestCase("text--boundary\r\n")]
    [TestCase("--boundary-extra\r\n")]
    [TestCase("--boundary-\r\n")]
    [TestCase("--other\r\n")]
    public void ScannerIgnoresNonDelimiterOccurrences(string value)
    {
        EmailBoundaryCollisionScanner scanner = new EmailBoundaryCollisionScanner("boundary");

        scanner.Feed(Bytes(value));

        Assert.That(scanner.Complete(), Is.False);
        Assert.That(scanner.HasCollision(), Is.False);
        Assert.That(scanner.GetSourceByteCount(), Is.EqualTo(Bytes(value).Length));
    }

    [Test]
    public void GenerationRetriesCollisionSplitAcrossReadBuffer()
    {
        EmailWriteOptions options = DeterministicOptions("buffer split");
        string colliding = new EmailBoundaryGenerator().CreateCandidate(options, 0);
        string expected = new EmailBoundaryGenerator().CreateCandidate(options, 1);
        string prefix = new string('x', EmailBoundaryGeneratorConstants.DefaultBufferBytes - 5);
        byte[] source = Bytes(prefix + "\r\n--" + colliding + "\r\n");
        EmailContent content = new EmailContentOperations().CreateMemory(source, EmailContentOwnership.Borrowed, false);

        EmailBoundaryGenerationResult result = new EmailBoundaryGenerator().GenerateWithContentsAndOptions(new List<EmailContent>() { content }, options);

        Assert.That(result.GetBoundary(), Is.EqualTo(expected));
        Assert.That(result.GetAttemptCount(), Is.EqualTo(2));
        Assert.That(result.GetCollisionCount(), Is.EqualTo(1));
        Assert.That(result.GetScannedByteCount(), Is.EqualTo(source.LongLength * 2));
        Assert.That(result.IsDeterministic(), Is.True);
    }

    [Test]
    public void GenerationStopsAfterBoundedCollisionRetries()
    {
        EmailWriteOptions options = DeterministicOptions("exhaust retries");
        StringBuilder source = new StringBuilder();
        int attempt = 0;
        while (attempt < EmailBoundaryGeneratorConstants.MaximumAttempts)
        {
            source.Append("--");
            source.Append(new EmailBoundaryGenerator().CreateCandidate(options, attempt));
            source.Append("\r\n");
            attempt++;
        }

        EmailContent content = new EmailContentOperations().CreateMemory(Bytes(source.ToString()), EmailContentOwnership.Borrowed, false);

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailBoundaryGenerator().GenerateWithContentsAndOptions(new List<EmailContent>() { content }, options));

        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxBoundaryGenerationAttempts"));
        Assert.That(exception.GetLimit(), Is.EqualTo(EmailBoundaryGeneratorConstants.MaximumAttempts));
        Assert.That(exception.GetObserved(), Is.EqualTo(EmailBoundaryGeneratorConstants.MaximumAttempts + 1));
    }

    [Test]
    public void GenerationRejectsContentThatCannotBeRetried()
    {
        EmailContent content = new EmailContentOperations().CreateStream(new NonSeekableReadStream(Bytes("body")), EmailContentOwnership.Owned);

        Assert.Throws<ArgumentException>(() => new EmailBoundaryGenerator().GenerateWithContentsAndOptions(new List<EmailContent>() { content }, new EmailWriteOptions()));

        content.Close();
    }

    [Test]
    public void ScannerValidatesBoundaryAndLifecycle()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailBoundaryCollisionScanner(null!));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryCollisionScanner("bad@boundary"));
        Assert.Throws<ArgumentException>(() => new EmailBoundaryCollisionScanner(new string('a', 71)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBoundaryGenerator().CreateCandidate(new EmailWriteOptions(), -1));

        EmailBoundaryCollisionScanner scanner = new EmailBoundaryCollisionScanner("valid");
        Assert.That(scanner.Complete(), Is.False);
        Assert.That(scanner.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => scanner.Feed(new byte[] { 1 }));
        Assert.Throws<InvalidOperationException>(() => scanner.Complete());
    }

    static void AssertEverySplitCollides(byte[] source, string boundary)
    {
        int split = 0;
        while (split <= source.Length)
        {
            EmailBoundaryCollisionScanner scanner = new EmailBoundaryCollisionScanner(boundary);
            scanner.Feed(new ReadOnlySpan<byte>(source, 0, split));
            scanner.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split));

            Assert.That(scanner.Complete(), Is.True, "split " + split);
            Assert.That(scanner.HasCollision(), Is.True, "split " + split);
            Assert.That(scanner.GetSourceByteCount(), Is.EqualTo(source.LongLength), "split " + split);
            split++;
        }
    }

    static EmailWriteOptions DeterministicOptions(string seed)
    {
        EmailWriteOptions options = new EmailWriteOptions();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Bytes(seed));
        return options;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    class NonSeekableReadStream : Stream
    {
        MemoryStream source;

        public NonSeekableReadStream(byte[] value)
        {
            source = new MemoryStream(value, false);
        }

        public override bool CanRead
        {
            get { return source.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return source.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
