namespace dns.Tests.Internal.Names;

public class DnsNameOperationsTests
{
    [Test]
    public void EqualityFoldsOnlyAsciiLettersAndRequiresMatchingTerminalState()
    {
        DnsName mixedCase = Parse("WwW.Example.");
        DnsName lowerCase = Parse("www.eXAMPLE.");
        DnsName relative = Parse("www.example");
        DnsName binaryUpper = CreateName(true, [0x41, 0x80]);
        DnsName binaryLower = CreateName(true, [0x61, 0x80]);
        DnsName binaryDifferent = CreateName(true, [0x61, 0x81]);

        DnsNameOperationResult caseResult = DnsNameOperations.NamesEqual(
            mixedCase,
            lowerCase);
        DnsNameOperationResult terminalResult = DnsNameOperations.NamesEqual(
            lowerCase,
            relative);
        DnsNameOperationResult binaryCaseResult = DnsNameOperations.NamesEqual(
            binaryUpper,
            binaryLower);
        DnsNameOperationResult binaryDifferenceResult = DnsNameOperations.NamesEqual(
            binaryUpper,
            binaryDifferent);

        Assert.Multiple(() =>
        {
            Assert.That(caseResult.Success, Is.True);
            Assert.That(caseResult.BooleanValue, Is.True);
            Assert.That(terminalResult.BooleanValue, Is.False);
            Assert.That(binaryCaseResult.BooleanValue, Is.True);
            Assert.That(binaryDifferenceResult.BooleanValue, Is.False);
        });
    }

    [Test]
    public void CanonicalComparisonStartsAtTheRightmostLabel()
    {
        DnsNameOperationResult caseEquivalent = DnsNameOperations.CompareNames(
            Parse("WwW.Example."),
            Parse("www.eXAMPLE."));
        DnsNameOperationResult rightmostFirst = DnsNameOperations.CompareNames(
            Parse("z.a."),
            Parse("a.b."));
        DnsNameOperationResult ancestorFirst = DnsNameOperations.CompareNames(
            Parse("example."),
            Parse("a.example."));
        DnsNameOperationResult shorterLabelFirst = DnsNameOperations.CompareNames(
            Parse("a.example."),
            Parse("aa.example."));
        DnsNameOperationResult relativeFirst = DnsNameOperations.CompareNames(
            Parse("example"),
            Parse("example."));

        Assert.Multiple(() =>
        {
            Assert.That(caseEquivalent.Comparison, Is.Zero);
            Assert.That(caseEquivalent.BooleanValue, Is.True);
            Assert.That(rightmostFirst.Comparison, Is.LessThan(0));
            Assert.That(ancestorFirst.Comparison, Is.LessThan(0));
            Assert.That(shorterLabelFirst.Comparison, Is.LessThan(0));
            Assert.That(relativeFirst.Comparison, Is.LessThan(0));
        });
    }

    [Test]
    public void SuffixAndAncestorOperationsRespectRootCaseAndTerminalState()
    {
        DnsName name = Parse("www.Example.");
        DnsName suffix = Parse("eXAMPLE.");
        DnsName root = Parse(".");
        DnsName relativeSuffix = Parse("example");

        Assert.Multiple(() =>
        {
            Assert.That(DnsNameOperations.HasNameSuffix(name, suffix).BooleanValue,
                Is.True);
            Assert.That(DnsNameOperations.HasNameSuffix(name, name).BooleanValue,
                Is.True);
            Assert.That(DnsNameOperations.HasNameSuffix(name, root).BooleanValue,
                Is.True);
            Assert.That(DnsNameOperations.HasNameSuffix(name, relativeSuffix).BooleanValue,
                Is.False);
            Assert.That(DnsNameOperations.IsAncestorName(suffix, name).BooleanValue,
                Is.True);
            Assert.That(DnsNameOperations.IsAncestorName(root, name).BooleanValue,
                Is.True);
            Assert.That(DnsNameOperations.IsAncestorName(name, name).BooleanValue,
                Is.False);
            Assert.That(DnsNameOperations.IsAncestorName(relativeSuffix, name).BooleanValue,
                Is.False);
        });
    }

    [Test]
    public void LabelCountUsesValidatedNameData()
    {
        DnsNameOperationResult populated = DnsNameOperations.CountNameLabels(
            Parse("www.example."));
        DnsNameOperationResult root = DnsNameOperations.CountNameLabels(Parse("."));

        Assert.Multiple(() =>
        {
            Assert.That(populated.Success, Is.True);
            Assert.That(populated.LabelCount, Is.EqualTo(2));
            Assert.That(root.Success, Is.True);
            Assert.That(root.LabelCount, Is.Zero);
        });
    }

    [Test]
    public void ParentPreservesCaseAndReturnsAnIndependentName()
    {
        DnsName source = Parse("www.Example.");
        DnsNameOperationResult result = DnsNameOperations.GetParentName(source);
        source.Labels[1][0] = 0x78;
        DnsNameOperationResult oneLabel = DnsNameOperations.GetParentName(
            Parse("example."));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Format(result.Name!), Is.EqualTo("Example."));
            Assert.That(result.Name!.Labels[0], Is.Not.SameAs(source.Labels[1]));
            Assert.That(oneLabel.Success, Is.True);
            Assert.That(Format(oneLabel.Name!), Is.EqualTo("."));
        });
    }

    [Test]
    public void ZeroLabelNamesHaveNoParent()
    {
        DnsNameOperationResult root = DnsNameOperations.GetParentName(Parse("."));
        DnsNameOperationResult emptyRelative = DnsNameOperations.GetParentName(
            Parse(string.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(root.Success, Is.False);
            Assert.That(root.Name, Is.Null);
            Assert.That(root.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(emptyRelative.Success, Is.False);
            Assert.That(emptyRelative.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
        });
    }

    [Test]
    public void ConcatenationCombinesRelativePrefixWithEitherSuffixKind()
    {
        DnsName prefix = Parse("WwW");
        DnsNameOperationResult absolute = DnsNameOperations.ConcatenateNames(
            prefix,
            Parse("Example."));
        DnsNameOperationResult relative = DnsNameOperations.ConcatenateNames(
            prefix,
            Parse("Example"));
        prefix.Labels[0][0] = 0x78;

        Assert.Multiple(() =>
        {
            Assert.That(absolute.Success, Is.True);
            Assert.That(Format(absolute.Name!), Is.EqualTo("WwW.Example."));
            Assert.That(absolute.Name!.Labels[0], Is.Not.SameAs(prefix.Labels[0]));
            Assert.That(relative.Success, Is.True);
            Assert.That(Format(relative.Name!), Is.EqualTo("WwW.Example"));
        });
    }

    [Test]
    public void ConcatenationRejectsAbsolutePrefixesAndExpandedLengthOverflow()
    {
        DnsNameOperationResult absolutePrefix = DnsNameOperations.ConcatenateNames(
            Parse("www."),
            Parse("example."));
        DnsName largePrefix = RawDnsName.CreateRelativeName(new List<byte[]>
        {
            new byte[63],
            new byte[63]
        }).Name!;
        DnsName largeSuffix = RawDnsName.CreateAbsoluteName(new List<byte[]>
        {
            new byte[63],
            new byte[62]
        }).Name!;
        DnsNameOperationResult overflow = DnsNameOperations.ConcatenateNames(
            largePrefix,
            largeSuffix);

        Assert.Multiple(() =>
        {
            Assert.That(absolutePrefix.Success, Is.False);
            Assert.That(absolutePrefix.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(absolutePrefix.Diagnostics[0].Field, Is.EqualTo("prefix"));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Name, Is.Null);
            Assert.That(overflow.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.NameTooLong));
        });
    }

    [Test]
    public void RelativizationRemovesACaseInsensitiveOriginAndPreservesLeadingCase()
    {
        DnsNameOperationResult result = DnsNameOperations.RelativizeName(
            Parse("WwW.Api.Example."),
            Parse("eXAMPLE."));
        DnsNameOperationResult equal = DnsNameOperations.RelativizeName(
            Parse("Example."),
            Parse("example."));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.IsAbsolute, Is.False);
            Assert.That(Format(result.Name), Is.EqualTo("WwW.Api"));
            Assert.That(equal.Success, Is.True);
            Assert.That(equal.Name!.IsAbsolute, Is.False);
            Assert.That(equal.Name.Labels, Is.Empty);
            Assert.That(Format(equal.Name), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void RelativizationRejectsUnrelatedOrDifferentlyTerminatedOrigins()
    {
        DnsNameOperationResult unrelated = DnsNameOperations.RelativizeName(
            Parse("www.example."),
            Parse("other."));
        DnsNameOperationResult terminalMismatch = DnsNameOperations.RelativizeName(
            Parse("www.example."),
            Parse("example"));
        DnsNameOperationResult relativeName = DnsNameOperations.RelativizeName(
            Parse("www.example"),
            Parse("example"));

        Assert.Multiple(() =>
        {
            Assert.That(unrelated.Success, Is.False);
            Assert.That(unrelated.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(unrelated.Diagnostics[0].Field, Is.EqualTo("origin"));
            Assert.That(terminalMismatch.Success, Is.False);
            Assert.That(terminalMismatch.Diagnostics[0].Field, Is.EqualTo("origin"));
            Assert.That(relativeName.Success, Is.False);
            Assert.That(relativeName.Diagnostics[0].Field, Is.EqualTo("name"));
        });
    }

    [Test]
    public void CommonAncestorUsesRightmostLabelsAndPreservesFirstNameCase()
    {
        DnsNameOperationResult shared = DnsNameOperations.FindCommonAncestor(
            Parse("a.WwW.Example."),
            Parse("b.www.eXAMPLE."));
        DnsNameOperationResult absoluteDisjoint =
            DnsNameOperations.FindCommonAncestor(Parse("a."), Parse("b."));
        DnsNameOperationResult relativeDisjoint =
            DnsNameOperations.FindCommonAncestor(Parse("a"), Parse("b"));

        Assert.Multiple(() =>
        {
            Assert.That(shared.Success, Is.True);
            Assert.That(Format(shared.Name!), Is.EqualTo("WwW.Example."));
            Assert.That(absoluteDisjoint.Success, Is.True);
            Assert.That(Format(absoluteDisjoint.Name!), Is.EqualTo("."));
            Assert.That(relativeDisjoint.Success, Is.True);
            Assert.That(Format(relativeDisjoint.Name!), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void CommonAncestorRequiresMatchingTerminalState()
    {
        DnsNameOperationResult result = DnsNameOperations.FindCommonAncestor(
            Parse("www.example."),
            Parse("www.example"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
        });
    }

    [Test]
    public void InvalidMutableNamesProduceDiagnosticsInsteadOfRuntimeFailures()
    {
        DnsName invalid = new DnsName();
        invalid.Labels.Add(new byte[64]);

        DnsNameOperationResult equality = DnsNameOperations.NamesEqual(
            invalid,
            Parse("example."));
        DnsNameOperationResult parent = DnsNameOperations.GetParentName(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(equality.Success, Is.False);
            Assert.That(equality.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(parent.Success, Is.False);
            Assert.That(parent.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        });
    }

    private static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    private static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }

    private static DnsName CreateName(bool isAbsolute, byte[] label)
    {
        DnsName name = new DnsName();
        name.IsAbsolute = isAbsolute;
        name.Labels.Add(label);
        return name;
    }
}
