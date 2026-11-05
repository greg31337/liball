using System.Text.Json;

namespace jmap.Tests;

public class EmailQuerySorterTests
{
    [Test]
    public void DefaultOrderingUsesNewestReceivedDateAndDeterministicIdTies()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email later = Message("e3", "t3", "2026-09-11T12:00:00.1Z");
        Email tieZ = Message("z", "tz", "2026-09-11T12:00:00Z");
        Email tieA = Message("a", "ta", "2026-09-11T12:00:00Z");
        Email earlier = Message("e0", "t0", "2026-09-10T12:00:00Z");

        List<Email> ordered = sorter.OrderEmails(
            new List<Email> { tieZ, earlier, later, tieA }, null, null, false);
        List<Email> explicitlyUnsorted = sorter.OrderEmails(
            new List<Email> { tieZ, earlier, later, tieA }, null, new List<EmailComparator>(), false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ordered.Select(email => email.Id), Is.EqualTo(new[] { "e3", "a", "z", "e0" }));
            Assert.That(explicitlyUnsorted.Select(email => email.Id),
                Is.EqualTo(new[] { "a", "e0", "e3", "z" }));
        }
    }

    [Test]
    public void OrdersBySizeAddressBaseSubjectAndSentDateComparators()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email alpha = Message("alpha", "t1", "2026-09-11T00:00:00Z");
        alpha.Size = 20;
        alpha.From = Address("zoe@example.test");
        alpha.To = Address("amy@example.test");
        alpha.Subject = "[list] Re: Project Update (fwd)";
        alpha.SentAt = "2026-09-11T10:00:00Z";
        Email beta = Message("beta", "t2", "2026-09-11T00:00:00Z");
        beta.Size = 10;
        beta.From = Address("amy@example.test");
        beta.To = Address("zoe@example.test");
        beta.Subject = "Beta";
        beta.SentAt = "2026-09-11T11:00:00Z";
        List<Email> values = new List<Email> { alpha, beta };

        List<Email> bySize = sorter.OrderEmails(values, null,
            new List<EmailComparator> { Comparator(JmapProperties.Size, true) }, false);
        List<Email> byFrom = sorter.OrderEmails(values, null,
            new List<EmailComparator> { Comparator(JmapProperties.From, true) }, false);
        List<Email> byTo = sorter.OrderEmails(values, null,
            new List<EmailComparator> { Comparator(JmapProperties.To, true) }, false);
        List<Email> bySubject = sorter.OrderEmails(values, null,
            new List<EmailComparator> { Comparator(JmapProperties.Subject, true) }, false);
        List<Email> bySent = sorter.OrderEmails(values, null,
            new List<EmailComparator> { Comparator(JmapProperties.SentAt, false) }, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bySize.Select(email => email.Id), Is.EqualTo(new[] { "beta", "alpha" }));
            Assert.That(byFrom.Select(email => email.Id), Is.EqualTo(new[] { "beta", "alpha" }));
            Assert.That(byTo.Select(email => email.Id), Is.EqualTo(new[] { "alpha", "beta" }));
            Assert.That(bySubject.Select(email => email.Id), Is.EqualTo(new[] { "beta", "alpha" }));
            Assert.That(bySent.Select(email => email.Id), Is.EqualTo(new[] { "beta", "alpha" }));
            Assert.That(sorter.BaseSubject(alpha.Subject), Is.EqualTo("Project Update"));
            Assert.That(sorter.BaseSubject("[Fwd: Re: Project Update]"), Is.EqualTo("Project Update"));
            Assert.That(sorter.BaseSubject("Re: [list] Project Update"), Is.EqualTo("Project Update"));
            Assert.That(sorter.BaseSubject("[list] Re: Project Update"), Is.EqualTo("Project Update"));
        }
    }

    [Test]
    public void AddressComparatorsFallBackToDecodedHeaderAddresses()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email zoe = Message("zoe", "t1", "2026-09-11T00:00:00Z");
        zoe.Headers = new List<EmailHeader>
        {
            new EmailHeader { Name = "From", Value = "Zoe <zoe@example.test>" }
        };
        Email alice = Message("alice", "t2", "2026-09-11T00:00:00Z");
        alice.Headers = new List<EmailHeader>
        {
            new EmailHeader { Name = "From", Value = "Alice <alice@example.test>" }
        };

        List<Email> ordered = sorter.OrderEmails(new List<Email> { zoe, alice }, null,
            new List<EmailComparator> { Comparator(JmapProperties.From, true) }, false);

        Assert.That(ordered.Select(email => email.Id), Is.EqualTo(new[] { "alice", "zoe" }));
    }

    [Test]
    public void AdvertisedComparatorSetIsCompleteAndCollationsRemainDistinct()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Assert.That(sorter.ComparatorProperties(), Is.EquivalentTo(new[]
        {
            JmapProperties.ReceivedAt,
            JmapProperties.Size,
            JmapProperties.From,
            JmapProperties.To,
            JmapProperties.Subject,
            JmapProperties.SentAt,
            JmapProperties.HasKeyword,
            JmapProperties.SomeInThreadHaveKeyword,
            JmapProperties.AllInThreadHaveKeyword
        }));

        Email lower = Message("a", "lower-thread", "2026-09-11T00:00:00Z");
        lower.From = Address("é@example.test");
        Email upper = Message("z", "upper-thread", "2026-09-11T00:00:00Z");
        upper.From = Address("É@example.test");
        EmailComparator ascii = Comparator(JmapProperties.From, true);
        ascii.Collation = "i;ascii-casemap";
        EmailComparator unicode = Comparator(JmapProperties.From, true);
        unicode.Collation = "i;unicode-casemap";

        List<Email> asciiOrdered = sorter.OrderEmails(
            new List<Email> { lower, upper }, null, new List<EmailComparator> { ascii }, false);
        List<Email> unicodeOrdered = sorter.OrderEmails(
            new List<Email> { upper, lower }, null,
            new List<EmailComparator> { unicode }, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(asciiOrdered.Select(email => email.Id), Is.EqualTo(new[] { "z", "a" }));
            Assert.That(unicodeOrdered.Select(email => email.Id), Is.EqualTo(new[] { "a", "z" }));
            Assert.That(sorter.ValidateComparators(
                new List<EmailComparator> { ascii, unicode },
                sorter.ComparatorProperties().ToList(),
                new List<string> { "i;ascii-casemap", "i;unicode-casemap" }), Is.Null);
        }
    }

    [Test]
    public void ChainedComparatorsAndIdFallbackProduceStableOrdering()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email beta = Message("c", "t3", "2026-09-11T00:00:00Z");
        beta.Size = 10;
        beta.Subject = "Beta";
        Email alphaLaterId = Message("b", "t2", "2026-09-11T00:00:00Z");
        alphaLaterId.Size = 10;
        alphaLaterId.Subject = "Alpha";
        Email alphaEarlierId = Message("a", "t1", "2026-09-11T00:00:00Z");
        alphaEarlierId.Size = 10;
        alphaEarlierId.Subject = "Alpha";
        Email large = Message("d", "t4", "2026-09-11T00:00:00Z");
        large.Size = 20;
        large.Subject = "Aardvark";
        List<EmailComparator> comparators = new List<EmailComparator>
        {
            Comparator(JmapProperties.Size, true),
            Comparator(JmapProperties.Subject, true)
        };

        List<Email> first = sorter.OrderEmails(
            new List<Email> { large, alphaLaterId, beta, alphaEarlierId },
            null, comparators, false);
        List<Email> second = sorter.OrderEmails(
            new List<Email> { beta, alphaEarlierId, large, alphaLaterId },
            null, comparators, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Select(email => email.Id),
                Is.EqualTo(new[] { "a", "b", "c", "d" }));
            Assert.That(second.Select(email => email.Id),
                Is.EqualTo(new[] { "a", "b", "c", "d" }));
        }
    }

    [Test]
    public void OrdersByIndividualAndWholeThreadKeywordBooleans()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email first = Message("first", "t1", "2026-09-11T00:00:00Z");
        first.Keywords = Keywords("$flagged", "$seen");
        Email firstSibling = Message("first-sibling", "t1", "2026-09-10T00:00:00Z");
        firstSibling.Keywords = Keywords("$answered");
        Email second = Message("second", "t2", "2026-09-11T00:00:00Z");
        second.Keywords = Keywords("$seen");
        Email secondSibling = Message("second-sibling", "t2", "2026-09-10T00:00:00Z");
        secondSibling.Keywords = Keywords("$seen");
        List<Email> matching = new List<Email> { second, first };
        List<Email> complete = new List<Email> { first, firstSibling, second, secondSibling };

        List<Email> individual = sorter.OrderEmails(matching, complete,
            new List<EmailComparator> { KeywordComparator(JmapProperties.HasKeyword, "$flagged", false) },
            false);
        List<Email> some = sorter.OrderEmails(matching, complete,
            new List<EmailComparator>
            {
                KeywordComparator(JmapProperties.SomeInThreadHaveKeyword, "$answered", false)
            }, false);
        List<Email> all = sorter.OrderEmails(matching, complete,
            new List<EmailComparator>
            {
                KeywordComparator(JmapProperties.AllInThreadHaveKeyword, "$seen", false)
            }, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(individual.Select(email => email.Id), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(some.Select(email => email.Id), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(all.Select(email => email.Id), Is.EqualTo(new[] { "second", "first" }));
        }
    }

    [Test]
    public void CollapseThreadsKeepsTheFirstSortedMatchFromEachThread()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email oldFirst = Message("old-first", "t1", "2026-09-10T00:00:00Z");
        Email other = Message("other", "t2", "2026-09-11T11:00:00Z");
        Email newFirst = Message("new-first", "t1", "2026-09-11T12:00:00Z");

        List<Email> collapsed = sorter.OrderEmails(
            new List<Email> { oldFirst, other, newFirst }, null, null, true);

        Assert.That(collapsed.Select(email => email.Id), Is.EqualTo(new[] { "new-first", "other" }));
    }

    [Test]
    public void ValidatesAdvertisedPropertiesKeywordsDirectionsCollationsAndExtensions()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        List<string> advertised = sorter.ComparatorProperties().ToList();
        List<string> collations = new List<string> { "i;ascii-casemap" };
        EmailComparator valid = Comparator(JmapProperties.Subject, true);
        valid.Collation = "i;ascii-casemap";
        EmailComparator notAdvertised = Comparator(JmapProperties.Subject, true);
        EmailComparator missingKeyword = Comparator(JmapProperties.HasKeyword, true);
        EmailComparator extraKeyword = Comparator(JmapProperties.Size, true);
        extraKeyword.Keyword = "$seen";
        EmailComparator badKeyword = KeywordComparator(JmapProperties.HasKeyword, "$Seen", true);
        EmailComparator badDirection = new EmailComparator
        {
            Property = JmapProperties.Size,
            IsAscending = true
        };
        EmailComparator badCollation = Comparator(JmapProperties.Subject, true);
        badCollation.Collation = "i;unicode-casemap";
        EmailComparator extension = Comparator(JmapProperties.Size, true);
        extension.UnknownProperties.Add("vendor", Json("true"));
        EmailComparator ignoredNumericCollation = Comparator(JmapProperties.Size, true);
        ignoredNumericCollation.Collation = "vendor-collation";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { valid }, advertised, collations),
                Is.Null);
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { notAdvertised },
                new List<string> { JmapProperties.Size }, collations)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { missingKeyword },
                advertised, collations)!.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { extraKeyword },
                advertised, collations)!.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { badKeyword },
                advertised, collations)!.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { badDirection },
                advertised, collations)!.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { badCollation },
                advertised, collations)!.Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { extension },
                advertised, collations)!.Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(sorter.ValidateComparators(new List<EmailComparator> { ignoredNumericCollation },
                advertised, collations), Is.Null);
        }
    }

    [Test]
    public void RejectsInvalidSortingDataAndMissingThreadIdsForCollapse()
    {
        EmailQuerySorter sorter = new EmailQuerySorter();
        Email duplicate = Message("same", "t1", "2026-09-11T00:00:00Z");
        Email duplicateAgain = Message("same", "t2", "2026-09-10T00:00:00Z");
        Email missingThread = Message("missing-thread", "t3", "2026-09-11T00:00:00Z");
        missingThread.ThreadId = null;

        JmapProtocolException duplicateError = Assert.Throws<JmapProtocolException>(() => sorter.OrderEmails(
            new List<Email> { duplicate, duplicateAgain }, null, null, false))!;
        JmapProtocolException threadError = Assert.Throws<JmapProtocolException>(() => sorter.OrderEmails(
            new List<Email> { missingThread }, null, null, true))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(duplicateError.Path, Is.EqualTo("matchingEmails/1/id"));
            Assert.That(threadError.Path, Is.EqualTo("emails/0/threadId"));
        }
    }

    private static Email Message(string id, string threadId, string receivedAt)
    {
        return new Email
        {
            Id = id,
            ThreadId = threadId,
            ReceivedAt = receivedAt,
            Keywords = new Dictionary<string, bool>()
        };
    }

    private static EmailComparator Comparator(string property, bool ascending)
    {
        return new EmailComparator
        {
            Property = property,
            IsAscending = ascending,
            HasIsAscending = true
        };
    }

    private static EmailComparator KeywordComparator(string property, string keyword, bool ascending)
    {
        EmailComparator comparator = Comparator(property, ascending);
        comparator.Keyword = keyword;
        return comparator;
    }

    private static List<EmailAddress> Address(string value)
    {
        return new List<EmailAddress> { new EmailAddress { Email = value } };
    }

    private static Dictionary<string, bool> Keywords(params string[] values)
    {
        Dictionary<string, bool> result = new Dictionary<string, bool>();
        foreach (string value in values)
        {
            result.Add(value, true);
        }

        return result;
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }
}
