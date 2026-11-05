namespace dns.Tests.Api;

public class DnsZoneDirectiveTests
{
    [Test]
    public void OriginAndTimeToLiveDirectivesApplyToFollowingRecords()
    {
        DnsZoneLoadOptions options = CreateOptions();
        string source = "$ORIGIN example.\n" +
            "$TTL 1h30m\n" +
            "@ IN SOA ns hostmaster 1 1h 10m 1d 5m\n" +
            "$ORIGIN sub\n" +
            "www A 192.0.2.1\n" +
            "mail 120 CH TXT \"value\"\n" +
            "next TXT \"inherited\"\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                FailureReason(result));
            Assert.That(result.RecordsLoaded, Is.EqualTo(4));
            Assert.That(Format(result.Zone!.Apex), Is.EqualTo("example."));
            Assert.That(result.Zone.HasDefaultTimeToLive, Is.True);
            Assert.That(result.Zone.DefaultTimeToLive, Is.EqualTo(5400));
            Assert.That(Format(result.Zone.Nodes[1].Name),
                Is.EqualTo("www.sub.example."));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].TimeToLive,
                Is.EqualTo(5400));
            Assert.That(result.Zone.Nodes[2].RecordSets[0].TimeToLive,
                Is.EqualTo(120));
            Assert.That(result.Zone.Nodes[3].RecordSets[0].TimeToLive,
                Is.EqualTo(5400));
            Assert.That(result.Zone.Nodes[3].RecordSets[0].ClassValue,
                Is.EqualTo(3));
        });
    }

    [Test]
    public void GenerateSupportsStepsOffsetsWidthsBasesAndLiteralDollars()
    {
        DnsZoneLoadOptions options = CreateOptions();
        string source = "$ORIGIN example.\n" +
            "$TTL 60\n" +
            "$GENERATE 1-5/2 host${-1,2,d} A 192.0.2.$\n" +
            "$GENERATE 15-16 hex${0,2,X} CNAME value-$$-$\n" +
            "$GENERATE 16-16 ${0,2,n} PTR target$\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True,
                FailureReason(result));
            Assert.That(result.RecordsLoaded, Is.EqualTo(6));
            Assert.That(Format(result.Zone!.Nodes[0].Name),
                Is.EqualTo("host00.example."));
            Assert.That(Format(result.Zone.Nodes[1].Name),
                Is.EqualTo("host02.example."));
            Assert.That(Format(result.Zone.Nodes[2].Name),
                Is.EqualTo("host04.example."));
            Assert.That(Format(result.Zone.Nodes[3].Name),
                Is.EqualTo("hex0F.example."));
            Assert.That(Format(result.Zone.Nodes[4].Name),
                Is.EqualTo("hex10.example."));
            Assert.That(Format(result.Zone.Nodes[5].Name),
                Is.EqualTo("0.1.example."));
            Assert.That(Format(result.Zone.Nodes[3].RecordSets[0].Records[0]
                    .Data!.Name!.Name),
                Is.EqualTo("value-\\$-15.example."));
            Assert.That(Format(result.Zone.Nodes[5].RecordSets[0].Records[0]
                    .Data!.Name!.Name),
                Is.EqualTo("target16.example."));
        });
    }

    [Test]
    public void GenerateAcceptsInclusiveIntegerBoundaryValues()
    {
        DnsZoneLoadOptions options = CreateOptions();
        string source = "$ORIGIN example.\n" +
            "$TTL 60\n" +
            "$GENERATE 0-2147483647/2147483647 host$ TXT \"value-$\"\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, FailureReason(result));
            Assert.That(result.RecordsLoaded, Is.EqualTo(2));
            Assert.That(Format(result.Zone!.Nodes[0].Name),
                Is.EqualTo("host0.example."));
            Assert.That(Format(result.Zone.Nodes[1].Name),
                Is.EqualTo("host2147483647.example."));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].Records[0]
                    .Data!.CharacterStrings!.Strings[0],
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("value-0")));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].Records[0]
                    .Data!.CharacterStrings!.Strings[0],
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "value-2147483647")));
        });
    }

    [TestCase("3-1", "ordered nonnegative bounds")]
    [TestCase("0-1/0", "step must be positive")]
    [TestCase("2147483648-2147483648", "ordered nonnegative bounds")]
    [TestCase("0-1/2147483648", "step must be positive")]
    public void GenerateRejectsRangesOutsideSupportedBounds(
        string range,
        string reason)
    {
        DnsZoneLoadOptions options = CreateOptions();

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            "$GENERATE " + range + " host$ TXT \"value\"", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.RecordsLoaded, Is.EqualTo(0));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidZone));
            Assert.That(result.Diagnostics[0].Reason, Does.Contain(reason));
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Column, Is.EqualTo(1));
        });
    }

    [TestCase("$GENERATE 0-0 host${-1} TXT value")]
    [TestCase("$GENERATE 2147483647-2147483647 host${1} TXT value")]
    public void GenerateRejectsSubstitutionsOutsideSupportedBounds(
        string source)
    {
        DnsZoneLoadOptions options = CreateOptions();

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.RecordsLoaded, Is.EqualTo(0));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain("outside its range"));
        });
    }

    [Test]
    public void IncludeResolvesPathsAndRestoresParentOriginAndOwner()
    {
        string directory = CreateTemporaryDirectory();
        string childPath = Path.Combine(directory, "child zone.db");
        string nestedPath = Path.Combine(directory, "nested.zone");
        try
        {
            File.WriteAllText(childPath,
                "$ORIGIN child\n" +
                "@ A 192.0.2.2\n" +
                "$INCLUDE nested.zone nested\n");
            File.WriteAllText(nestedPath, "@ TXT \"nested\"\n");
            DnsZoneLoadOptions options = CreateOptions();
            options.BaseDirectory = directory;
            string source = "$ORIGIN example.\n" +
                "$TTL 60\n" +
                "before A 192.0.2.1\n" +
                "$INCLUDE \"child zone.db\" included\n" +
                "    AAAA 2001:db8::1\n" +
                "after A 192.0.2.3\n";

            DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
                source, options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True,
                    FailureReason(result));
                Assert.That(result.RecordsLoaded, Is.EqualTo(5));
                Assert.That(result.IncludedFiles,
                    Is.EqualTo(new[]
                    {
                        Path.GetFullPath(childPath),
                        Path.GetFullPath(nestedPath)
                    }));
                Assert.That(Format(result.Zone!.Nodes[0].Name),
                    Is.EqualTo("before.example."));
                Assert.That(result.Zone.Nodes[0].RecordSets,
                    Has.Count.EqualTo(2));
                Assert.That(Format(result.Zone.Nodes[1].Name),
                    Is.EqualTo("child.included.example."));
                Assert.That(Format(result.Zone.Nodes[2].Name),
                    Is.EqualTo("nested.child.included.example."));
                Assert.That(Format(result.Zone.Nodes[3].Name),
                    Is.EqualTo("after.example."));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncludeCycleReturnsLocatedDiagnostic()
    {
        string directory = CreateTemporaryDirectory();
        string childPath = Path.Combine(directory, "child.zone");
        try
        {
            File.WriteAllText(childPath, "$INCLUDE child.zone\n");
            DnsZoneLoadOptions options = CreateOptions();
            options.BaseDirectory = directory;

            DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
                "$INCLUDE child.zone", options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostics[0].Reason,
                    Does.Contain("cycle"));
                Assert.That(result.Diagnostics[0].SourcePath,
                    Is.EqualTo(Path.GetFullPath(childPath)));
                Assert.That(result.IncludedFiles,
                    Is.EqualTo(new[] { Path.GetFullPath(childPath) }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncludeDepthLimitStopsNestedFiles()
    {
        string directory = CreateTemporaryDirectory();
        string firstPath = Path.Combine(directory, "first.zone");
        string secondPath = Path.Combine(directory, "second.zone");
        try
        {
            File.WriteAllText(firstPath, "$INCLUDE second.zone\n");
            File.WriteAllText(secondPath, "$TTL 60\n");
            DnsZoneLoadOptions options = CreateOptions();
            options.BaseDirectory = directory;
            options.MaximumIncludeDepth = 1;

            DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
                "$INCLUDE first.zone", options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
                Assert.That(result.Diagnostics[0].Reason,
                    Does.Contain("depth"));
                Assert.That(result.Diagnostics[0].SourcePath,
                    Is.EqualTo(Path.GetFullPath(firstPath)));
                Assert.That(result.IncludedFiles,
                    Is.EqualTo(new[] { Path.GetFullPath(firstPath) }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void IncludedFileDiagnosticsRetainSourcePathLineAndColumn()
    {
        string directory = CreateTemporaryDirectory();
        string childPath = Path.Combine(directory, "child.zone");
        try
        {
            File.WriteAllText(childPath,
                "$TTL 60\n" +
                "$UNKNOWN value\n");
            DnsZoneLoadOptions options = CreateOptions();
            options.BaseDirectory = directory;

            DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
                "$INCLUDE child.zone", options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Reason,
                    Does.Contain("not recognized"));
                Assert.That(result.Diagnostics[0].SourcePath,
                    Is.EqualTo(Path.GetFullPath(childPath)));
                Assert.That(result.Diagnostics[0].HasLine, Is.True);
                Assert.That(result.Diagnostics[0].Line, Is.EqualTo(2));
                Assert.That(result.Diagnostics[0].HasColumn, Is.True);
                Assert.That(result.Diagnostics[0].Column, Is.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GenerateHonorsPerDirectiveExpansionLimit()
    {
        DnsZoneLoadOptions options = CreateOptions();
        options.MaximumGeneratedRecords = 2;

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            "$ORIGIN example.\n$TTL 60\n" +
            "$GENERATE 1-3 host$ A 192.0.2.$", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.RecordsLoaded, Is.EqualTo(0));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain("generated record count"));
        });
    }

    [Test]
    public void GenerateHonorsTotalRecordLimitBeforeExpansion()
    {
        DnsZoneLoadOptions options = CreateOptions();
        options.MaximumRecords = 2;

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            "$ORIGIN example.\n$TTL 60\n" +
            "existing A 192.0.2.1\n" +
            "$GENERATE 1-2 host$ A 192.0.2.$", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.RecordsLoaded, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain("zone record limit"));
        });
    }

    [TestCase("$GENERATE 1-1 host$ A 192.0.2.$", true, false,
        "$GENERATE processing is disabled.")]
    [TestCase("$INCLUDE absent.zone", false, true,
        "$INCLUDE processing is disabled.")]
    public void DirectiveFeatureFlagsAreEnforced(
        string source,
        bool allowInclude,
        bool allowGenerate,
        string reason)
    {
        DnsZoneLoadOptions options = CreateOptions();
        options.AllowInclude = allowInclude;
        options.AllowGenerate = allowGenerate;

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Reason, Is.EqualTo(reason));
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Column, Is.EqualTo(1));
        });
    }

    static DnsZoneLoadOptions CreateOptions()
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.ValidateZone = false;
        return options;
    }

    static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(), "dns-zone-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    static string Format(DnsName name)
    {
        return new DnsNameService().FormatName(name, new DnsTextOptions());
    }

    static string FailureReason(DnsZoneLoadResult result)
    {
        if (result.Diagnostics.Count == 0) return string.Empty;
        return result.Diagnostics[0].Reason;
    }
}
