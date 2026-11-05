namespace ftp.Tests;

public class FtpMlsxCodecTests
{
    [Test]
    public void Parser_decodes_every_standard_fact_and_normalizes_names()
    {
        const string line =
            "TyPe=FiLe;SiZe=00042;MoDiFy=20240229123456.125;" +
            "CrEaTe=20240102030405;UnIqUe=Aa-9;PeRm=RwAcDeFlMp;" +
            "LaNg=en-US;MeDiA-TyPe=application/vnd.example+json;" +
            "ChArSeT=UTF-8; report.json";

        FtpEntry entry = FtpMlsxCodec.ParseEntry(line);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Name, Is.EqualTo("report.json"));
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(entry.HasSize, Is.True);
            Assert.That(entry.Size, Is.EqualTo(42));
            Assert.That(entry.Modified.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc).AddTicks(1250000)));
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
            Assert.That(entry.Language, Is.EqualTo("en-US"));
            Assert.That(entry.MediaType, Is.EqualTo("application/vnd.example+json"));
            Assert.That(entry.Charset, Is.EqualTo("UTF-8"));
            Assert.That(entry.Facts, Has.Length.EqualTo(9));
            Assert.That(entry.Facts, Has.All.Matches<FtpFact>(fact => fact.Standard));
            Assert.That(entry.Facts[0].Name, Is.EqualTo("type"));
            Assert.That(entry.Facts[0].RawText, Is.EqualTo("TyPe=FiLe"));
            Assert.That(entry.RawLine, Is.EqualTo(line));
        });
    }

    [TestCase("file", FtpEntryType.File)]
    [TestCase("DIR", FtpEntryType.Directory)]
    [TestCase("cDiR", FtpEntryType.CurrentDirectory)]
    [TestCase("PDIR", FtpEntryType.ParentDirectory)]
    [TestCase("OS.UNIX=slink", FtpEntryType.SymbolicLink)]
    [TestCase("os.windows=REPARSE", FtpEntryType.SymbolicLink)]
    [TestCase("OS.a=b", FtpEntryType.Unknown)]
    [TestCase("OS.UNIX=block", FtpEntryType.Unknown)]
    public void Parser_maps_every_RFC_entry_type(string value, FtpEntryType expected)
    {
        FtpEntry entry = FtpMlsxCodec.ParseEntry("type=" + value + "; item");

        Assert.That(entry.Type, Is.EqualTo(expected));
    }

    [Test]
    public void Parser_preserves_OS_and_local_facts_without_interpreting_them()
    {
        FtpEntry entry = FtpMlsxCodec.ParseEntry(
            "UNIX.mode=0755;OS/2.ea=foo-bar;X.Vendor=Aa,1; item");

        Assert.Multiple(() =>
        {
            Assert.That(entry.Facts, Has.Length.EqualTo(3));
            Assert.That(entry.Facts[0].Name, Is.EqualTo("unix.mode"));
            Assert.That(entry.Facts[0].Value, Is.EqualTo("0755"));
            Assert.That(entry.Facts[0].RawText, Is.EqualTo("UNIX.mode=0755"));
            Assert.That(entry.Facts[0].Standard, Is.False);
            Assert.That(entry.Facts[1].Name, Is.EqualTo("os/2.ea"));
            Assert.That(entry.Facts[1].Value, Is.EqualTo("foo-bar"));
            Assert.That(entry.Facts[2].Name, Is.EqualTo("x.vendor"));
            Assert.That(entry.Facts[2].Value, Is.EqualTo("Aa,1"));
            Assert.That(entry.HasUnixMode, Is.False);
        });
    }

    [Test]
    public void Entry_separator_consumes_exactly_one_space_and_preserves_the_pathname()
    {
        FtpEntry leadingSpaces = FtpMlsxCodec.ParseEntry("type=file;   leading and trailing  ");
        FtpEntry noFacts = FtpMlsxCodec.ParseEntry("  no facts");
        FtpEntry emptyName = FtpMlsxCodec.ParseEntry("type=dir; ");

        Assert.Multiple(() =>
        {
            Assert.That(leadingSpaces.Name, Is.EqualTo("  leading and trailing  "));
            Assert.That(noFacts.Facts, Is.Empty);
            Assert.That(noFacts.Name, Is.EqualTo(" no facts"));
            Assert.That(emptyName.Name, Is.Empty);
            Assert.That(FtpMlsxCodec.FormatEntry(leadingSpaces),
                Is.EqualTo("type=file;   leading and trailing  "));
            Assert.That(FtpMlsxCodec.FormatEntry(noFacts), Is.EqualTo("  no facts"));
        });
    }

    [Test]
    public void Control_entry_codec_handles_the_distinct_MLST_indentation_space()
    {
        FtpEntry entry = FtpMlsxCodec.ParseControlEntry(" Type=file;Size=7; /tmp/a b");

        Assert.Multiple(() =>
        {
            Assert.That(entry.Name, Is.EqualTo("/tmp/a b"));
            Assert.That(entry.Type, Is.EqualTo(FtpEntryType.File));
            Assert.That(entry.Size, Is.EqualTo(7));
            Assert.That(entry.RawLine, Is.EqualTo(" Type=file;Size=7; /tmp/a b"));
            Assert.That(FtpMlsxCodec.FormatControlEntry(entry),
                Is.EqualTo(" type=file;size=7; /tmp/a b"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpMlsxCodec.ParseControlEntry("Type=file; item"));
        });
    }

    [Test]
    public void Fact_formatter_uses_normalized_names_and_preserves_case_sensitive_values()
    {
        FtpFact fact = FtpMlsxCodec.ParseFact("X.Vendor=Aa-9");

        Assert.Multiple(() =>
        {
            Assert.That(fact.Name, Is.EqualTo("x.vendor"));
            Assert.That(fact.Value, Is.EqualTo("Aa-9"));
            Assert.That(fact.RawText, Is.EqualTo("X.Vendor=Aa-9"));
            Assert.That(FtpMlsxCodec.FormatFact(fact), Is.EqualTo("x.vendor=Aa-9"));
        });
    }

    [Test]
    public void Permission_parser_accepts_empty_case_insensitive_and_duplicate_indicators()
    {
        FtpEntry empty = FtpMlsxCodec.ParseEntry("perm=; empty");
        FtpEntry duplicate = FtpMlsxCodec.ParseEntry("perm=rRww; duplicate");

        Assert.Multiple(() =>
        {
            Assert.That(empty.Permissions, Is.Empty);
            Assert.That(duplicate.Permissions,
                Is.EqualTo(new[] { FtpPermission.Read, FtpPermission.Write }));
        });
    }

    [Test]
    public void Lenient_directory_parser_ignores_nonstandard_permission_indicators()
    {
        const string listing =
            "type=file;size=7;perm=rwT; example.bin\r\n";

        FtpListing compatible =
            FtpMlsxCodec.ParseDirectoryText(listing, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpMlsxCodec.ParseDirectoryText(listing, true));
            Assert.That(compatible.Entries, Has.Length.EqualTo(1));
            Assert.That(compatible.Entries[0].Permissions,
                Is.EqualTo(new[]
                {
                    FtpPermission.Read,
                    FtpPermission.Write
                }));
            Assert.That(compatible.Entries[0].Facts.Single(
                fact => fact.Name == FtpFactNames.Perm).Value,
                Is.EqualTo("rwT"));
        });
    }

    [Test]
    public void Parser_rejects_malformed_fact_series_and_unqualified_unknown_names()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("type=file item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("type=file;; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("type=file;item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("unknown=value; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("x.=value; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("x.name=a*b; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("x.name=a=b; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("type=file; bad\rname"));
        });
    }

    [Test]
    public void Parser_validates_each_standard_fact_value()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("type=link; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("type=OS.=block; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("size=-1; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry(
                "size=9223372036854775808; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry(
                "modify=20230229000000; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("unique=; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("perm=z; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("lang=; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("media-type=text; item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry("charset=; item"));
        });
    }

    [Test]
    public void Codec_enforces_fact_count_and_encoded_line_bounds()
    {
        string facts = "";
        int index = 0;
        while (index < FtpDefaults.MaximumFactCount + 1)
        {
            facts = facts + "x.f" + index + "=v;";
            index++;
        }
        string longPath = new string('\u00e9', FtpDefaults.MaximumControlLineBytes / 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry(facts + " item"));
            Assert.Throws<FtpProtocolException>(() => FtpMlsxCodec.ParseEntry(" " + longPath));
        });
    }
}
