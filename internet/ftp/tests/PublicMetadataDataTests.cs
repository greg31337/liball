namespace ftp.Tests;

public class PublicMetadataDataTests
{
    [Test]
    public void Timestamp_is_unavailable_but_uses_a_utc_placeholder()
    {
        FtpTimestamp timestamp = new FtpTimestamp();

        Assert.Multiple(() =>
        {
            Assert.That(timestamp.HasValue, Is.False);
            Assert.That(timestamp.ValueUtc, Is.EqualTo(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)));
            Assert.That(timestamp.ValueUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(timestamp.FractionalSeconds, Is.Empty);
            Assert.That(timestamp.RawValue, Is.Empty);
            Assert.That(timestamp.LeapSecond, Is.False);
        });
    }

    [Test]
    public void Fact_starts_empty_and_preserves_unknown_status()
    {
        FtpFact fact = new FtpFact();

        Assert.Multiple(() =>
        {
            Assert.That(fact.Name, Is.Empty);
            Assert.That(fact.Value, Is.Empty);
            Assert.That(fact.RawText, Is.Empty);
            Assert.That(fact.Standard, Is.False);
        });
    }

    [Test]
    public void Entry_owns_metadata_collections_and_marks_value_metadata_unavailable()
    {
        FtpEntry first = new FtpEntry();
        FtpEntry second = new FtpEntry();

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.Empty);
            Assert.That(first.FullPath, Is.Null);
            Assert.That(first.Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(first.Facts, Is.Empty);
            Assert.That(first.Facts, Is.Not.SameAs(second.Facts));
            Assert.That(first.HasSize, Is.False);
            Assert.That(first.Size, Is.Zero);
            Assert.That(first.Modified, Is.Not.SameAs(second.Modified));
            Assert.That(first.Modified.HasValue, Is.False);
            Assert.That(first.Created, Is.Not.SameAs(second.Created));
            Assert.That(first.Created.HasValue, Is.False);
            Assert.That(first.Permissions, Is.Empty);
            Assert.That(first.Permissions, Is.Not.SameAs(second.Permissions));
            Assert.That(first.HasUnixMode, Is.False);
            Assert.That(first.UnixMode, Is.Zero);
            Assert.That(first.Owner, Is.Null);
            Assert.That(first.Group, Is.Null);
            Assert.That(first.Language, Is.Null);
            Assert.That(first.MediaType, Is.Null);
            Assert.That(first.Unique, Is.Null);
            Assert.That(first.Charset, Is.Null);
            Assert.That(first.LinkTarget, Is.Null);
            Assert.That(first.Hidden, Is.False);
            Assert.That(first.RawLine, Is.Empty);
        });
    }

    [Test]
    public void Listing_owns_empty_ordered_collections_and_is_incomplete()
    {
        FtpListing first = new FtpListing();
        FtpListing second = new FtpListing();

        Assert.Multiple(() =>
        {
            Assert.That(first.Entries, Is.Empty);
            Assert.That(first.Entries, Is.Not.SameAs(second.Entries));
            Assert.That(first.RawLines, Is.Empty);
            Assert.That(first.RawLines, Is.Not.SameAs(second.RawLines));
            Assert.That(first.MachineReadable, Is.False);
            Assert.That(first.Complete, Is.False);
            Assert.That(first.FormatName, Is.Null);
            Assert.That(first.CommandResult, Is.Null);
            Assert.That(first.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Path_probe_separates_probe_failure_from_path_absence()
    {
        FtpPathProbeResult first = new FtpPathProbeResult();
        FtpPathProbeResult second = new FtpPathProbeResult();

        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.False);
            Assert.That(first.Exists, Is.False);
            Assert.That(first.Entry, Is.Null);
            Assert.That(first.Commands, Is.Empty);
            Assert.That(first.Commands, Is.Not.SameAs(second.Commands));
            Assert.That(first.FailureMessage, Is.Null);
        });

        first.Succeeded = true;
        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Exists, Is.False);
        });
    }
}
