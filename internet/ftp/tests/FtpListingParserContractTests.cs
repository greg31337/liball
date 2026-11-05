namespace ftp.Tests;

public class FtpListingParserContractTests
{
    [Test]
    public void ParseMachineListing_decodes_every_standard_and_qualified_fact()
    {
        IFtpListingParser parser = new FtpListingParser();
        string line =
            "TyPe=FiLe;SiZe=00042;MoDiFy=20240229123456.125;" +
            "CrEaTe=20240102030405;UnIqUe=Aa-9;PeRm=RwAcDeFlMp;" +
            "LaNg=fr-CA;MeDiA-TyPe=application/octet-stream;" +
            "ChArSeT=UTF-8;UNIX.mode=0755;X.Owner=Alice; résumé 資料😀.txt";

        FtpListing listing = parser.ParseMachineListing(new[] { line });
        FtpEntry entry = listing.Entries[0];

        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo("MLSx"));
            Assert.That(listing.MachineReadable, Is.True);
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.RawLines, Is.EqualTo(new[] { line }));
            Assert.That(entry.Name, Is.EqualTo("résumé 資料😀.txt"));
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(entry.HasSize, Is.True);
            Assert.That(entry.Size, Is.EqualTo(42));
            Assert.That(entry.Modified.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc)
                    .AddTicks(1250000)));
            Assert.That(entry.Modified.FractionalSeconds, Is.EqualTo("125"));
            Assert.That(entry.Created.ValueUtc, Is.EqualTo(
                new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)));
            Assert.That(entry.Unique, Is.EqualTo("Aa-9"));
            Assert.That(entry.Permissions, Is.EqualTo(new[]
            {
                FtpPermission.Read,
                FtpPermission.Write,
                FtpPermission.Append,
                FtpPermission.CreateFile,
                FtpPermission.DeleteFile,
                FtpPermission.EnterDirectory,
                FtpPermission.Rename,
                FtpPermission.List,
                FtpPermission.CreateDirectory,
                FtpPermission.DeleteDirectory
            }));
            Assert.That(entry.Language, Is.EqualTo("fr-CA"));
            Assert.That(entry.MediaType, Is.EqualTo("application/octet-stream"));
            Assert.That(entry.Charset, Is.EqualTo("UTF-8"));
            Assert.That(entry.Facts, Has.Length.EqualTo(11));
            Assert.That(entry.Facts.Take(9),
                Has.All.Matches<FtpFact>(fact => fact.Standard));
            Assert.That(entry.Facts.Skip(9),
                Has.All.Matches<FtpFact>(fact => !fact.Standard));
            Assert.That(entry.Facts[9].Name, Is.EqualTo("unix.mode"));
            Assert.That(entry.Facts[10].Name, Is.EqualTo("x.owner"));
            Assert.That(entry.RawLine, Is.EqualTo(line));
        });
    }

    [Test]
    public void ParseMachineListing_maps_all_entry_types_and_pseudo_directories()
    {
        IFtpListingParser parser = new FtpListingParser();

        FtpListing listing = parser.ParseMachineListing(new[]
        {
            "type=file; file.txt",
            "type=dir; folder",
            "type=cdir; .",
            "type=pdir; ..",
            "type=OS.UNIX=block; device"
        });

        Assert.That(listing.Entries.Select(entry => entry.Type), Is.EqualTo(new[]
        {
            FtpEntryType.File,
            FtpEntryType.Directory,
            FtpEntryType.CurrentDirectory,
            FtpEntryType.ParentDirectory,
            FtpEntryType.Unknown
        }));
    }

    [Test]
    public void ParseHumanListing_decodes_Unix_rows_through_the_contract()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] lines =
        {
            "-rw-r--r-- 1 álïçè 開発 42 Feb 29 2024 résumé 資料😀.txt",
            "lrwxrwxrwx 1 root wheel 10 Jan 02 2024 latest -> résumé 資料😀.txt"
        };

        FtpListing listing = parser.ParseHumanListing(lines);

        Assert.Multiple(() =>
        {
            AssertListingHeader(listing, "Unix", lines);
            Assert.That(listing.Entries[0].Name,
                Is.EqualTo("résumé 資料😀.txt"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("álïçè"));
            Assert.That(listing.Entries[0].Group, Is.EqualTo("開発"));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(42));
            Assert.That(listing.Entries[0].UnixMode, Is.EqualTo(420));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.SymbolicLink));
            Assert.That(listing.Entries[1].LinkTarget,
                Is.EqualTo("résumé 資料😀.txt"));
        });
    }

    [Test]
    public void ParseHumanListing_decodes_DOS_rows_through_the_contract()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] lines =
        {
            "02-29-24  03:04PM       <DIR>          Dossier 資料",
            "02-29-2024  12:05AM               123 résumé 😀.txt"
        };

        FtpListing listing = parser.ParseHumanListing(lines);

        Assert.Multiple(() =>
        {
            AssertListingHeader(listing, "DOS/Windows", lines);
            Assert.That(listing.Entries[0].Name, Is.EqualTo("Dossier 資料"));
            Assert.That(listing.Entries[0].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[0].HasSize, Is.False);
            Assert.That(listing.Entries[0].Modified.ValueUtc.Hour, Is.EqualTo(15));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("résumé 😀.txt"));
            Assert.That(listing.Entries[1].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[1].Size, Is.EqualTo(123));
            Assert.That(listing.Entries[1].Modified.ValueUtc.Hour, Is.EqualTo(0));
        });
    }

    [Test]
    public void ParseHumanListing_decodes_EPLF_rows_through_the_contract()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] lines =
        {
            "+i8388621.29609,m824255902,r,s10376,\tREADME 資料.txt",
            "+i-folder,m1709251200,/,\t.archive"
        };

        FtpListing listing = parser.ParseHumanListing(lines);

        Assert.Multiple(() =>
        {
            AssertListingHeader(listing, "EPLF", lines);
            Assert.That(listing.Entries[0].Name, Is.EqualTo("README 資料.txt"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(10376));
            Assert.That(listing.Entries[0].Unique, Is.EqualTo("8388621.29609"));
            Assert.That(listing.Entries[0].Modified.ValueUtc,
                Is.EqualTo(DateTime.UnixEpoch.AddSeconds(824255902)));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[1].Hidden, Is.True);
        });
    }

    [Test]
    public void ParseHumanListing_decodes_VMS_rows_through_the_contract()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] lines =
        {
            "RÉSUMÉ.TXT;3 2/3 4-NOV-2023 12:34:56 [ALICE,STAFF] (RWED,RWED,RE,)",
            "ARCHIVE.DIR;1 1/2 5-NOV-2023 01:02:03 [ALICE] (RWE,RWE,RE,E)"
        };

        FtpListing listing = parser.ParseHumanListing(lines);

        Assert.Multiple(() =>
        {
            AssertListingHeader(listing, "VMS", lines);
            Assert.That(listing.Entries[0].Name, Is.EqualTo("RÉSUMÉ.TXT"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].HasSize, Is.False);
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("ALICE"));
            Assert.That(listing.Entries[0].Group, Is.EqualTo("STAFF"));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("ARCHIVE"));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
        });
    }

    [Test]
    public void ParseHumanListing_decodes_OS400_rows_through_the_contract()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] lines =
        {
            "QPGMR 8192 04/09/23 12:21:20 *STMF RÉSUMÉ 資料.TXT",
            "QPGMR 0 04/10/2023 01:02:03 *DIR Dossier 😀"
        };

        FtpListing listing = parser.ParseHumanListing(lines);

        Assert.Multiple(() =>
        {
            AssertListingHeader(listing, "OS/400", lines);
            Assert.That(listing.Entries[0].Name,
                Is.EqualTo("RÉSUMÉ 資料.TXT"));
            Assert.That(listing.Entries[0].Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(listing.Entries[0].Size, Is.EqualTo(8192));
            Assert.That(listing.Entries[0].Owner, Is.EqualTo("QPGMR"));
            Assert.That(listing.Entries[1].Name, Is.EqualTo("Dossier 😀"));
            Assert.That(listing.Entries[1].Type,
                Is.EqualTo(FtpEntryType.Directory));
            Assert.That(listing.Entries[1].HasSize, Is.False);
        });
    }

    [Test]
    public void Unknown_and_name_only_rows_remain_literal_and_uninterpreted()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] unknownLines =
        {
            "provider-specific diagnostic 資料",
            "<script>DELE victim</script>"
        };
        string[] names =
        {
            "résumé 資料😀.txt",
            "../outside",
            "C:\\Windows\\system32",
            "<script>alert(1)</script>"
        };

        FtpListing unknown = parser.ParseHumanListing(unknownLines);
        FtpListing nameOnly = parser.ParseNameListing(names);

        Assert.Multiple(() =>
        {
            AssertListingHeader(unknown, "Raw", unknownLines);
            Assert.That(unknown.Entries.Select(entry => entry.Name),
                Is.EqualTo(unknownLines));
            Assert.That(unknown.Entries,
                Has.All.Matches<FtpEntry>(entry =>
                    entry.Type == FtpEntryType.Unknown));
            AssertListingHeader(nameOnly, "Names", names);
            Assert.That(nameOnly.Entries.Select(entry => entry.Name),
                Is.EqualTo(names));
            Assert.That(nameOnly.Entries.Select(entry => entry.RawLine),
                Is.EqualTo(names));
            Assert.That(nameOnly.Entries,
                Has.All.Matches<FtpEntry>(entry =>
                    entry.Type == FtpEntryType.Unknown && entry.FullPath == null));
        });
    }

    [Test]
    public void Every_parse_method_rejects_null_injected_and_oversized_input()
    {
        IFtpListingParser parser = new FtpListingParser();
        string[] nullLine = new string[] { null! };
        string[] injected = new[] { "safe\r\nDELE victim" };
        string[] nul = new[] { "safe\0victim" };
        string[] oversized = new[]
        {
            new string('é', (FtpDefaults.MaximumControlLineBytes / 2) + 1)
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                parser.ParseMachineListing(null!));
            Assert.Throws<ArgumentNullException>(() =>
                parser.ParseHumanListing(null!));
            Assert.Throws<ArgumentNullException>(() =>
                parser.ParseNameListing(null!));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseMachineListing(nullLine));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseHumanListing(nullLine));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseNameListing(nullLine));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseMachineListing(injected));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseHumanListing(injected));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseNameListing(injected));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseMachineListing(nul));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseHumanListing(nul));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseNameListing(nul));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseMachineListing(oversized));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseHumanListing(oversized));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseNameListing(oversized));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseMachineListing(new[] { "type=file item" }));
            Assert.Throws<FtpProtocolException>(() =>
                parser.ParseMachineListing(new[] { "x.name=a=b; victim" }));
        });
    }

    private static void AssertListingHeader(
        FtpListing listing,
        string formatName,
        string[] rawLines)
    {
        Assert.Multiple(() =>
        {
            Assert.That(listing.FormatName, Is.EqualTo(formatName));
            Assert.That(listing.MachineReadable, Is.False);
            Assert.That(listing.Complete, Is.True);
            Assert.That(listing.RawLines, Is.EqualTo(rawLines));
            Assert.That(listing.RawLines, Is.Not.SameAs(rawLines));
        });
    }
}
