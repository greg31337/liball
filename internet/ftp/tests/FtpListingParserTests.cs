namespace ftp.Tests;

public class FtpListingParserTests
{
    [Test]
    public void Machine_listing_parses_standard_and_unknown_facts_and_copies_raw_lines()
    {
        string[] lines =
        {
            "Type=file;Size=42;Modify=20240229123456;X.Owner=Alice; report.txt",
            "Type=dir;Unique=folder-1; .hidden",
        };
        IFtpListingParser parser = new FtpListingParser();

        FtpListing listing = parser.ParseMachineListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.MachineReadable, Is.True);
            Assert.That(listing.FormatName, Is.EqualTo("MLSx"));
            Assert.That(listing.RawLines, Is.EqualTo(lines));
            Assert.That(listing.RawLines, Is.Not.SameAs(lines));
            Assert.That(listing.Entries, Has.Length.EqualTo(2));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("report.txt"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(42));
            Assert.That(listing.Entries[0].Facts, Has.Length.EqualTo(4));
            Assert.That(listing.Entries[0].Facts[3].Name, Is.EqualTo("x.owner"));
            Assert.That(listing.Entries[0].Facts[3].Standard, Is.False);
            Assert.That(listing.Entries[0].Facts[3].RawText,
                Is.EqualTo("X.Owner=Alice"));
            Assert.That(listing.Entries[0].RawLine, Is.EqualTo(lines[0]));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[1].Hidden, Is.True);
        });
    }

    [Test]
    public void Human_listing_detects_format_and_retains_unknown_rows_as_raw_entries()
    {
        string[] lines =
        {
            "-rw-r--r-- 1 alice staff 12 Feb 29 2024 report final.txt",
            "provider-specific diagnostic row",
        };
        FtpListingParser parser = new FtpListingParser();

        FtpListing listing = parser.ParseHumanListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.FormatName, Is.EqualTo("Unix"));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("report final.txt"));
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("alice"));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(12));
            Assert.That(listing.Entries[1].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(listing.Entries[1].Name,
                Is.EqualTo("provider-specific diagnostic row"));
            Assert.That(listing.Entries[1].RawLine, Is.EqualTo(lines[1]));
            Assert.That(listing.RawLines, Is.EqualTo(lines));
        });
    }

    [Test]
    public void Name_listing_preserves_names_without_inferring_types_or_full_paths()
    {
        string[] lines = { ".hidden", "folder name", "../literal" };
        FtpListingParser parser = new FtpListingParser();

        FtpListing listing = parser.ParseNameListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.FormatName, Is.EqualTo("Names"));
            Assert.That(listing.Entries.Select(entry => entry.Name),
                Is.EqualTo(lines));
            Assert.That(listing.Entries, Has.All.Matches<FtpEntry>(entry =>
                entry.Type == FtpEntryType.Unknown));
            Assert.That(listing.Entries, Has.All.Matches<FtpEntry>(entry =>
                entry.FullPath == null));
            Assert.That(listing.Entries[0].Hidden, Is.True);
            Assert.That(listing.Entries[2].RawLine, Is.EqualTo("../literal"));
        });
    }

    [Test]
    public void Every_listing_form_accepts_an_empty_bounded_listing()
    {
        FtpListingParser parser = new FtpListingParser();

        FtpListing machine = parser.ParseMachineListing(new string[0]);
        FtpListing human = parser.ParseHumanListing(new string[0]);
        FtpListing names = parser.ParseNameListing(new string[0]);

        Assert.Multiple(() =>
        {
            Assert.That(machine.Complete, Is.True);
            Assert.That(machine.Entries, Is.Empty);
            Assert.That(machine.RawLines, Is.Empty);
            Assert.That(machine.FormatName, Is.EqualTo("MLSx"));
            Assert.That(human.Complete, Is.True);
            Assert.That(human.FormatName, Is.EqualTo("Raw"));
            Assert.That(names.Complete, Is.True);
            Assert.That(names.FormatName, Is.EqualTo("Names"));
        });
    }

    [Test]
    public void Public_parser_rejects_invalid_or_overlong_lines_consistently()
    {
        FtpListingParser parser = new FtpListingParser();
        string overlong = new string('x', FtpDefaults.MaximumControlLineBytes + 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                parser.ParseMachineListing(null!));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseHumanListing(new[] { "embedded\nline" }));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseNameListing(new[] { overlong }));
        });
    }
}
