namespace ftp.Tests;

public class FtpHumanListingParserTests
{
    [Test]
    public void Unix_parser_reads_modes_ownership_links_hidden_names_and_raw_fallbacks()
    {
        string[] lines =
        {
            " -rw-r--r-- 1 alice staff 42 Feb 29 2024 report final.txt",
            "lrwxrwxrwx 1 root wheel 10 Jan 02 2024 latest -> report.txt",
            "drwx------ 2 alice staff 4096 Mar 03 2024 .secret",
            "total 8",
        };

        FtpListing listing = FtpHumanListingParser.ParseListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo("Unix"));
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.RawLines, Is.EqualTo(lines));
            Assert.That(listing.RawLines, Is.Not.SameAs(lines));
            Assert.That(listing.Entries, Has.Length.EqualTo(4));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("report final.txt"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].HasSize, Is.True);
            Assert.That(listing.Entries[0].Size, Is.EqualTo(42));
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("alice"));
            Assert.That(listing.Entries[0].Group, Is.EqualTo("staff"));
            Assert.That(listing.Entries[0].HasUnixMode, Is.True);
            Assert.That(listing.Entries[0].UnixMode, Is.EqualTo(420));
            Assert.That(listing.Entries[0].Modified.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(listing.Entries[0].RawLine, Is.EqualTo(lines[0]));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("latest"));
            Assert.That(listing.Entries[1].LinkTarget, Is.EqualTo("report.txt"));
            Assert.That(listing.Entries[2].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[2].Hidden, Is.True);
            Assert.That(listing.Entries[3].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(listing.Entries[3].Name, Is.EqualTo("total 8"));
            Assert.That(listing.Entries[3].RawLine, Is.EqualTo("total 8"));
            Assert.That(FtpUnixListingParser.ParseLine(
                "-rwsr-Sr-t 1 root staff 1 Jan 01 2024 special").Entry!.UnixMode,
                Is.EqualTo(4069));
        });
    }

    [Test]
    public void Dos_parser_reads_directory_file_size_and_AM_PM_timestamps()
    {
        string[] lines =
        {
            "02-29-24  03:04PM       <DIR>          Folder Name",
            "02-29-2024  12:05AM               123 report.txt",
        };

        FtpListing listing = FtpHumanListingParser.ParseListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo("DOS/Windows"));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("Folder Name"));
            Assert.That(listing.Entries[0].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[0].HasSize, Is.False);
            Assert.That(listing.Entries[0].Modified.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 15, 4, 0, DateTimeKind.Utc)));
            Assert.That(listing.Entries[1].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[1].Size, Is.EqualTo(123));
            Assert.That(listing.Entries[1].Modified.ValueUtc.Hour, Is.EqualTo(0));
        });
    }

    [Test]
    public void Eplf_parser_reads_type_size_epoch_unique_id_and_names_with_spaces()
    {
        string[] lines =
        {
            "+i8388621.29609,m824255902,r,s10376,\tREADME final.txt",
            "+i-folder,m1709251200,/,\t.archive",
        };

        FtpListing listing = FtpHumanListingParser.ParseListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo("EPLF"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("README final.txt"));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(10376));
            Assert.That(listing.Entries[0].Unique, Is.EqualTo("8388621.29609"));
            Assert.That(listing.Entries[0].Modified.ValueUtc, Is.EqualTo(
                DateTime.UnixEpoch.AddSeconds(824255902)));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[1].Hidden, Is.True);
        });
    }

    [Test]
    public void Vms_parser_strips_versions_and_directory_suffix_without_inventing_byte_sizes()
    {
        string[] lines =
        {
            "README.TXT;3 2/3 4-NOV-2023 12:34:56 [ALICE,STAFF] (RWED,RWED,RE,)",
            "ARCHIVE.DIR;1 1/2 5-NOV-2023 01:02:03 [ALICE] (RWE,RWE,RE,E)",
        };

        FtpListing listing = FtpHumanListingParser.ParseListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo("VMS"));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("README.TXT"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].HasSize, Is.False);
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("ALICE"));
            Assert.That(listing.Entries[0].Group, Is.EqualTo("STAFF"));
            Assert.That(listing.Entries[0].Modified.ValueUtc, Is.EqualTo(
                new DateTime(2023, 11, 4, 12, 34, 56, DateTimeKind.Utc)));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("ARCHIVE"));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[1].Owner, Is.EqualTo("ALICE"));
            Assert.That(listing.Entries[1].Group, Is.Null);
        });
    }

    [Test]
    public void Os400_parser_reads_owner_object_type_size_and_timestamp()
    {
        string[] lines =
        {
            "QPGMR 8192 04/09/23 12:21:20 *STMF README.TXT",
            "QPGMR 0 04/10/2023 01:02:03 *DIR Folder Name",
        };

        FtpListing listing = FtpHumanListingParser.ParseListing(lines);

        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo("OS/400"));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("README.TXT"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].HasSize, Is.True);
            Assert.That(listing.Entries[0].Size, Is.EqualTo(8192));
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("QPGMR"));
            Assert.That(listing.Entries[0].Modified.ValueUtc, Is.EqualTo(
                new DateTime(2023, 4, 9, 12, 21, 20, DateTimeKind.Utc)));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("Folder Name"));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[1].HasSize, Is.False);
        });
    }

    [Test]
    public void Detection_uses_fixed_precedence_for_ties_and_raw_fallback_for_nonmatching_lines()
    {
        string unix = "-rw-r--r-- 1 alice staff 5 Jan 01 2024 unix.txt";
        string dos = "01-01-24  01:00PM  7 windows.txt";

        FtpListing tied = FtpHumanListingParser.ParseListing(new[] { unix, dos });
        FtpListing raw = FtpHumanListingParser.ParseListing(new[]
        {
            "unrecognized server-specific entry",
        });

        Assert.Multiple(() =>
        {
            Assert.That(tied.FormatName, Is.EqualTo("Unix"));
            Assert.That(tied.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(tied.Entries[1].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(tied.Entries[1].Name, Is.EqualTo(dos));
            Assert.That(tied.Entries[1].RawLine, Is.EqualTo(dos));
            Assert.That(raw.FormatName, Is.EqualTo("Raw"));
            Assert.That(raw.Complete, Is.True);
            Assert.That(raw.Entries[0].Type, Is.EqualTo(FtpEntryType.Unknown));
            Assert.That(raw.Entries[0].RawLine,
                Is.EqualTo("unrecognized server-specific entry"));
        });
    }

    [Test]
    public void Parser_rejects_embedded_line_endings_and_overlong_entries()
    {
        string overlong = new string('x', FtpDefaults.MaximumControlLineBytes + 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpHumanListingParser.ParseListing(new[] { "bad\nline" }));
            Assert.Throws<FtpProtocolException>(() =>
                FtpHumanListingParser.ParseListing(new[] { overlong }));
        });
    }
}
