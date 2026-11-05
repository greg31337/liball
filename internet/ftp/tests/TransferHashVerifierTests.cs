using System.Text;

namespace ftp.Tests;

public class TransferHashVerifierTests
{
    [TestCase("MD5", "25F9E794323B453885F5181F1B624D0B")]
    [TestCase("SHA1", "F7C3BC1D808E04732ADF679965CCC34CA7AE3441")]
    [TestCase("SHA-256", "15E2B0D3C33891EBB0F1EF609EC419420C20E320CE94C65FBC8C3312448EB225")]
    [TestCase("SHA512", "D9E6762DD1C8EAF6D61B3C6192FC408D4D6D5F1176D0C29169BC24E71C3F274AD27FCD5811B313D681F7E55EC02D73D499C95455B6B5BB503ACF574FBA8FFE85")]
    [TestCase("CRC-32", "CBF43926")]
    public void Local_hashes_match_standard_vectors(string algorithm, string expected)
    {
        byte[] content = Encoding.ASCII.GetBytes("123456789");
        FtpTransferHashRequest request = CreateHashRequest(
            new MemoryStream(content), algorithm, 2, true);

        FtpTransferHashResult result = TransferHash.Calculate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Checksum, Is.EqualTo(expected));
            Assert.That(result.BytesRead, Is.EqualTo(content.Length));
            Assert.That(result.Algorithm,
                Is.EqualTo(TransferHash.NormalizeAlgorithm(algorithm)));
        });
    }

    [Test]
    public void Hashing_uses_bounded_reads_from_the_current_position_and_honors_ownership()
    {
        TrackingHashStream source = new TrackingHashStream(
            Encoding.ASCII.GetBytes("x123456789"), 3, null, null);
        source.Position = 1;
        FtpTransferHashRequest request = CreateHashRequest(source, "SHA-256", 4, true);

        FtpTransferHashResult result = TransferHash.Calculate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Checksum,
                Is.EqualTo("15E2B0D3C33891EBB0F1EF609EC419420C20E320CE94C65FBC8C3312448EB225"));
            Assert.That(result.BytesRead, Is.EqualTo(9));
            Assert.That(source.LargestRequestedRead, Is.LessThanOrEqualTo(4));
            Assert.That(source.Disposed, Is.False);
            Assert.That(source.Position, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void Cancellation_and_deadline_close_owned_sources()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        TrackingHashStream canceledSource = new TrackingHashStream(
            new byte[] { 1, 2, 3, 4 }, 2, null, cancellation);
        FtpTransferHashRequest canceled = CreateHashRequest(
            canceledSource, "MD5", 2, false);
        canceled.CancellationToken = cancellation.Token;

        TrackingHashStream timedOutSource = new TrackingHashStream(
            new byte[] { 1 }, 1, null, null);
        FtpTransferHashRequest timedOut = CreateHashRequest(
            timedOutSource, "CRC32", 2, false);
        timedOut.DeadlineMilliseconds = 0;

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => TransferHash.Calculate(canceled));
            Assert.That(canceledSource.Disposed, Is.True);
            Assert.That(canceledSource.ReadCalls, Is.EqualTo(1));
            Assert.Throws<TimeoutException>(() => TransferHash.Calculate(timedOut));
            Assert.That(timedOutSource.Disposed, Is.True);
            Assert.That(timedOutSource.ReadCalls, Is.Zero);
        });
    }

    [Test]
    public void Read_failure_is_wrapped_after_owned_source_cleanup()
    {
        IOException failure = new IOException("read failed");
        TrackingHashStream source = new TrackingHashStream(
            new byte[] { 1 }, 1, failure, null);
        FtpTransferHashRequest request = CreateHashRequest(source, "SHA-1", 8, false);

        FtpTransferException? error = Assert.Throws<FtpTransferException>(() =>
            TransferHash.Calculate(request));

        Assert.Multiple(() =>
        {
            Assert.That(error!.InnerException, Is.SameAs(failure));
            Assert.That(source.Disposed, Is.True);
        });
    }

    [Test]
    public void Invalid_hash_configuration_is_rejected_before_reading()
    {
        TrackingHashStream source = new TrackingHashStream(
            new byte[] { 1 }, 1, null, null);
        FtpTransferHashRequest unsupported = CreateHashRequest(source, "SHA-224", 8, true);
        FtpTransferHashRequest unbounded = CreateHashRequest(source, "MD5", 0, true);
        FtpTransferHashRequest invalidDeadline = CreateHashRequest(source, "CRC32", 8, true);
        invalidDeadline.DeadlineMilliseconds = -2;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => TransferHash.Calculate(null));
            Assert.Throws<FtpConfigurationException>(() => TransferHash.Calculate(unsupported));
            Assert.Throws<FtpConfigurationException>(() => TransferHash.Calculate(unbounded));
            Assert.Throws<FtpConfigurationException>(() => TransferHash.Calculate(invalidDeadline));
            Assert.That(source.ReadCalls, Is.Zero);
        });
    }

    [Test]
    public void Size_verification_requires_an_exact_available_size()
    {
        FtpTransferVerificationRequest match = CreateVerification(FtpVerificationMode.Size);
        match.HasExpectedSize = true;
        match.ExpectedSize = 123;
        match.HasActualSize = true;
        match.ActualSize = 123;
        FtpTransferVerificationRequest mismatch = CreateVerification(FtpVerificationMode.Size);
        mismatch.HasExpectedSize = true;
        mismatch.ExpectedSize = 123;
        mismatch.HasActualSize = true;
        mismatch.ActualSize = 124;
        FtpTransferVerificationRequest unavailable = CreateVerification(FtpVerificationMode.Size);
        unavailable.HasExpectedSize = true;
        unavailable.ExpectedSize = 123;

        FtpTransferResult matchingResult = TransferVerifier.Verify(match);
        FtpTransferResult mismatchingResult = TransferVerifier.Verify(mismatch);
        FtpTransferResult unavailableResult = TransferVerifier.Verify(unavailable);

        Assert.Multiple(() =>
        {
            Assert.That(matchingResult.VerificationAttempted, Is.True);
            Assert.That(matchingResult.Verified, Is.True);
            Assert.That(matchingResult.HasActualSize, Is.True);
            Assert.That(matchingResult.ActualSize, Is.EqualTo(123));
            Assert.That(mismatchingResult.Verified, Is.False);
            Assert.That(unavailableResult.Verified, Is.False);
        });
    }

    [Test]
    public void Modification_time_verification_preserves_sub_tick_and_leap_second_semantics()
    {
        FtpTransferVerificationRequest trailingZeroMatch = CreateVerification(
            FtpVerificationMode.ModifiedTime);
        trailingZeroMatch.ExpectedModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20240506070809.123456780");
        trailingZeroMatch.ActualModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20240506070809.12345678");
        FtpTransferVerificationRequest subTickMismatch = CreateVerification(
            FtpVerificationMode.ModifiedTime);
        subTickMismatch.ExpectedModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20240506070809.12345678");
        subTickMismatch.ActualModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20240506070809.12345679");
        FtpTransferVerificationRequest leapMismatch = CreateVerification(
            FtpVerificationMode.ModifiedTime);
        leapMismatch.ExpectedModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20161231235960");
        leapMismatch.ActualModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20161231235959");

        Assert.Multiple(() =>
        {
            Assert.That(TransferVerifier.Verify(trailingZeroMatch).Verified, Is.True);
            Assert.That(TransferVerifier.Verify(subTickMismatch).Verified, Is.False);
            Assert.That(TransferVerifier.Verify(leapMismatch).Verified, Is.False);
        });
    }

    [Test]
    public void Combined_verification_requires_both_size_and_modified_time()
    {
        FtpTransferVerificationRequest request = CreateVerification(
            FtpVerificationMode.SizeAndModifiedTime);
        request.HasExpectedSize = true;
        request.ExpectedSize = 10;
        request.HasActualSize = true;
        request.ActualSize = 10;
        request.ExpectedModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20240102030405");
        request.ActualModifiedTime = FtpTimestampCodec.ParseTimestampValue(
            "20240102030405");

        FtpTransferResult match = TransferVerifier.Verify(request);
        request.ActualSize = 9;
        FtpTransferResult mismatch = TransferVerifier.Verify(request);

        Assert.Multiple(() =>
        {
            Assert.That(match.Verified, Is.True);
            Assert.That(mismatch.Verified, Is.False);
        });
    }

    [Test]
    public void Checksum_verification_canonicalizes_algorithm_aliases_and_hex_case()
    {
        string checksum =
            "15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225";
        FtpTransferVerificationRequest match = CreateVerification(FtpVerificationMode.Checksum);
        match.ExpectedChecksumAlgorithm = "sha256";
        match.ExpectedChecksum = checksum;
        match.ActualChecksumAlgorithm = "SHA-256";
        match.ActualChecksum = checksum.ToUpperInvariant();
        FtpTransferVerificationRequest wrongAlgorithm = CreateVerification(
            FtpVerificationMode.Checksum);
        wrongAlgorithm.ExpectedChecksumAlgorithm = "SHA-256";
        wrongAlgorithm.ExpectedChecksum = checksum;
        wrongAlgorithm.ActualChecksumAlgorithm = "SHA-1";
        wrongAlgorithm.ActualChecksum = new string('0', 40);
        FtpTransferVerificationRequest malformedActual = CreateVerification(
            FtpVerificationMode.Checksum);
        malformedActual.ExpectedChecksumAlgorithm = "SHA-256";
        malformedActual.ExpectedChecksum = checksum;
        malformedActual.ActualChecksumAlgorithm = "SHA-256";
        malformedActual.ActualChecksum = new string('Z', 64);

        FtpTransferResult matchingResult = TransferVerifier.Verify(match);

        Assert.Multiple(() =>
        {
            Assert.That(matchingResult.Verified, Is.True);
            Assert.That(matchingResult.ChecksumAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(matchingResult.ExpectedChecksum, Is.EqualTo(checksum.ToUpperInvariant()));
            Assert.That(matchingResult.ActualChecksum, Is.EqualTo(checksum.ToUpperInvariant()));
            Assert.That(TransferVerifier.Verify(wrongAlgorithm).Verified, Is.False);
            Assert.That(TransferVerifier.Verify(malformedActual).Verified, Is.False);
        });
    }

    [Test]
    public void Verification_rejects_missing_or_inconsistent_expected_metadata()
    {
        FtpTransferVerificationRequest size = CreateVerification(FtpVerificationMode.Size);
        FtpTransferVerificationRequest modified = CreateVerification(
            FtpVerificationMode.ModifiedTime);
        FtpTransferVerificationRequest checksum = CreateVerification(
            FtpVerificationMode.Checksum);
        checksum.ExpectedChecksumAlgorithm = "MD5";
        checksum.ExpectedChecksum = "not-a-checksum";
        FtpTransferVerificationRequest hiddenActual = CreateVerification(
            FtpVerificationMode.None);
        hiddenActual.ActualSize = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => TransferVerifier.Verify(null));
            Assert.Throws<FtpConfigurationException>(() => TransferVerifier.Verify(size));
            Assert.Throws<FtpConfigurationException>(() => TransferVerifier.Verify(modified));
            Assert.Throws<FtpConfigurationException>(() => TransferVerifier.Verify(checksum));
            Assert.Throws<FtpTransferException>(() => TransferVerifier.Verify(hiddenActual));
        });
    }

    [Test]
    public void No_verification_reports_that_no_check_was_attempted()
    {
        FtpTransferResult result = TransferVerifier.Verify(
            CreateVerification(FtpVerificationMode.None));

        Assert.Multiple(() =>
        {
            Assert.That(result.VerificationAttempted, Is.False);
            Assert.That(result.Verified, Is.False);
            Assert.That(result.VerificationMode, Is.EqualTo(FtpVerificationMode.None));
        });
    }

    private static FtpTransferHashRequest CreateHashRequest(
        Stream source,
        string algorithm,
        int bufferBytes,
        bool leaveOpen)
    {
        FtpTransferHashRequest request = new FtpTransferHashRequest();
        request.Source = source;
        request.Algorithm = algorithm;
        request.BufferBytes = bufferBytes;
        request.LeaveOpen = leaveOpen;
        return request;
    }

    private static FtpTransferVerificationRequest CreateVerification(FtpVerificationMode mode)
    {
        FtpTransferVerificationRequest request = new FtpTransferVerificationRequest();
        request.Mode = mode;
        return request;
    }

    private class TrackingHashStream : MemoryStream
    {
        private readonly int maximumChunk;
        private readonly Exception? failure;
        private readonly CancellationTokenSource? cancellation;

        public bool Disposed;
        public int LargestRequestedRead;
        public int ReadCalls;

        public TrackingHashStream(
            byte[] content,
            int maximumChunk,
            Exception? failure,
            CancellationTokenSource? cancellation)
            : base(content)
        {
            this.maximumChunk = maximumChunk;
            this.failure = failure;
            this.cancellation = cancellation;
            Disposed = false;
            LargestRequestedRead = 0;
            ReadCalls = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            LargestRequestedRead = Math.Max(LargestRequestedRead, count);
            if (failure != null)
            {
                throw failure;
            }

            int boundedCount = Math.Min(count, maximumChunk);
            int read = base.Read(buffer, offset, boundedCount);
            if (read != 0 && cancellation != null)
            {
                cancellation.Cancel();
            }

            return read;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
