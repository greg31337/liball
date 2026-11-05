using System.Text;

namespace dns.Tests.Internal.Resolver;

public class ResolverSearchOperationsTests
{
    [Test]
    public void AbsoluteNameBypassesTheSearchList()
    {
        DnsResolveRequest request = Request(Name(true, "WWW", "Example"), 1);
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "corp", "example"));

        DnsCandidateSequence result =
            ResolverSearchOperations.CreateCandidateSequence(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.AbsoluteFirst, Is.True);
            Assert.That(result.Names, Has.Count.EqualTo(1));
            Assert.That(Text(result.Names[0]), Is.EqualTo("WWW.Example."));
        });
    }

    [Test]
    public void RelativeNameBelowNdotsUsesSearchOrderThenAbsoluteName()
    {
        DnsResolveRequest request = Request(Name(false, "printer"), 1);
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "corp", "example"));
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "lab", "example"));

        DnsCandidateSequence result =
            ResolverSearchOperations.CreateCandidateSequence(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.AbsoluteFirst, Is.False);
            Assert.That(result.Names.Select(Text), Is.EqualTo(new[]
            {
                "printer.corp.example.",
                "printer.lab.example.",
                "printer."
            }));
        });
    }

    [Test]
    public void RelativeNameMeetingNdotsUsesAbsoluteNameFirst()
    {
        DnsResolveRequest request = Request(Name(false, "www", "dev"), 1);
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "corp", "example"));

        DnsCandidateSequence result =
            ResolverSearchOperations.CreateCandidateSequence(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.AbsoluteFirst, Is.True);
            Assert.That(result.Names.Select(Text), Is.EqualTo(new[]
            {
                "www.dev.",
                "www.dev.corp.example."
            }));
        });
    }

    [Test]
    public void ZeroNdotsTriesAbsoluteFirstWhileEmptySearchAddsNoDuplicate()
    {
        DnsResolveRequest withSearch = Request(Name(false, "printer"), 0);
        withSearch.Options.Configuration.SearchDomains.Add(
            Name(true, "corp", "example"));
        DnsResolveRequest withoutSearch = Request(Name(false, "printer"), 5);

        DnsCandidateSequence absoluteFirst =
            ResolverSearchOperations.CreateCandidateSequence(withSearch);
        DnsCandidateSequence single =
            ResolverSearchOperations.CreateCandidateSequence(withoutSearch);

        Assert.Multiple(() =>
        {
            Assert.That(absoluteFirst.AbsoluteFirst, Is.True);
            Assert.That(absoluteFirst.Names.Select(Text), Is.EqualTo(new[]
            {
                "printer.",
                "printer.corp.example."
            }));
            Assert.That(single.AbsoluteFirst, Is.False);
            Assert.That(single.Names.Select(Text),
                Is.EqualTo(new[] { "printer." }));
            Assert.That(ResolverSearchOperations.SelectTerminalStatus(
                single,
                new List<DnsResolveStatus>
                {
                    DnsResolveStatus.NameError
                }), Is.EqualTo(DnsResolveStatus.NameError));
        });
    }

    [Test]
    public void CanonicalDuplicateCandidatesAreSuppressedInPlace()
    {
        DnsResolveRequest request = Request(Name(false, "host"), 2);
        request.Options.Configuration.SearchDomains.Add(Name(true));
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "EXAMPLE"));
        request.Options.Configuration.SearchDomains.Add(
            Name(true, "example"));

        DnsCandidateSequence result =
            ResolverSearchOperations.CreateCandidateSequence(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Names, Has.Count.EqualTo(2));
            Assert.That(result.Names.Select(Text), Is.EqualTo(new[]
            {
                "host.",
                "host.example."
            }));
        });
    }

    [Test]
    public void OverlongSearchExpansionIsSkippedWithoutLosingAbsoluteCandidate()
    {
        string longLabel = new string('a', 63);
        DnsResolveRequest request = Request(
            Name(false, longLabel, longLabel, longLabel),
            10);
        request.Options.Configuration.SearchDomains.Add(
            Name(true, longLabel));

        DnsCandidateSequence result =
            ResolverSearchOperations.CreateCandidateSequence(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Names, Has.Count.EqualTo(1));
            Assert.That(result.Names[0].Labels, Has.Count.EqualTo(3));
            Assert.That(result.Names[0].IsAbsolute, Is.True);
        });
    }

    [Test]
    public void SearchContinuationDistinguishesRetryableAndTerminalStatuses()
    {
        DnsResolveStatus[] continuing =
        [
            DnsResolveStatus.NoData,
            DnsResolveStatus.NameError,
            DnsResolveStatus.ServerFailure
        ];
        DnsResolveStatus[] terminal =
        [
            DnsResolveStatus.Success,
            DnsResolveStatus.TimedOut,
            DnsResolveStatus.Cancelled,
            DnsResolveStatus.Refused,
            DnsResolveStatus.ProtocolError,
            DnsResolveStatus.Loop,
            DnsResolveStatus.DepthLimitExceeded,
            DnsResolveStatus.Unsupported
        ];

        Assert.Multiple(() =>
        {
            Assert.That(continuing.All(
                ResolverSearchOperations.ShouldContinueCandidateSearch),
                Is.True);
            Assert.That(terminal.Any(
                ResolverSearchOperations.ShouldContinueCandidateSearch),
                Is.False);
        });
    }

    [Test]
    public void ExhaustedSearchSelectsPortableTerminalStatusPrecedence()
    {
        DnsCandidateSequence searchFirst = Sequence(false, 3);
        DnsCandidateSequence absoluteFirst = Sequence(true, 3);

        DnsResolveStatus noData = ResolverSearchOperations.SelectTerminalStatus(
            searchFirst,
            new List<DnsResolveStatus>
            {
                DnsResolveStatus.NameError,
                DnsResolveStatus.ServerFailure,
                DnsResolveStatus.NoData
            });
        DnsResolveStatus serverFailure =
            ResolverSearchOperations.SelectTerminalStatus(
                Sequence(false, 2),
                new List<DnsResolveStatus>
                {
                    DnsResolveStatus.NameError,
                    DnsResolveStatus.ServerFailure
                });
        DnsResolveStatus initialAbsolute =
            ResolverSearchOperations.SelectTerminalStatus(
                absoluteFirst,
                new List<DnsResolveStatus>
                {
                    DnsResolveStatus.ServerFailure,
                    DnsResolveStatus.NameError,
                    DnsResolveStatus.NoData
                });
        DnsResolveStatus terminal = ResolverSearchOperations.SelectTerminalStatus(
            searchFirst,
            new List<DnsResolveStatus> { DnsResolveStatus.TimedOut });

        Assert.Multiple(() =>
        {
            Assert.That(noData, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(serverFailure,
                Is.EqualTo(DnsResolveStatus.ServerFailure));
            Assert.That(initialAbsolute,
                Is.EqualTo(DnsResolveStatus.ServerFailure));
            Assert.That(terminal, Is.EqualTo(DnsResolveStatus.TimedOut));
            Assert.That(() => ResolverSearchOperations.SelectTerminalStatus(
                searchFirst,
                new List<DnsResolveStatus> { DnsResolveStatus.NameError }),
                Throws.TypeOf<DnsResolutionException>());
        });
    }

    static DnsCandidateSequence Sequence(bool absoluteFirst, int count)
    {
        DnsCandidateSequence result = new DnsCandidateSequence();
        result.AbsoluteFirst = absoluteFirst;
        int index = 0;
        while (index < count)
        {
            result.Names.Add(Name(true, "candidate" + index));
            index = index + 1;
        }

        return result;
    }

    static DnsResolveRequest Request(DnsName name, int ndots)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = name;
        result.Options.Configuration.NameDots = ndots;
        return result;
    }

    static DnsName Name(bool absolute, params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = absolute;
        int index = 0;
        while (index < labels.Length)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return result;
    }

    static string Text(DnsName name)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < name.Labels.Count)
        {
            if (index > 0)
            {
                result.Append('.');
            }

            result.Append(Encoding.ASCII.GetString(name.Labels[index]));
            index = index + 1;
        }

        if (name.IsAbsolute)
        {
            result.Append('.');
        }

        return result.ToString();
    }
}
