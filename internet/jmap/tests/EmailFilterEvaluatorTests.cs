using System.Text.Json;

namespace jmap.Tests;

public class EmailFilterEvaluatorTests
{
    [Test]
    public void MatchesMailboxDateSizeAndAttachmentConditionsAtTheirSpecifiedBoundaries()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        Email email = Candidate();
        EmailFilter matching = new EmailFilter
        {
            InMailbox = "inbox",
            InMailboxOtherThan = new List<string> { "inbox", "trash" },
            After = "2026-09-11T12:00:00Z",
            Before = "2026-09-11T12:00:00.1Z",
            MinSize = 120,
            HasMinSize = true,
            MaxSize = 121,
            HasMaxSize = true,
            HasAttachment = true,
            HasHasAttachment = true
        };
        EmailFilter exclusiveBefore = new EmailFilter { Before = "2026-09-11T12:00:00Z" };
        EmailFilter inclusiveAfter = new EmailFilter { After = "2026-09-11T12:00:00.1Z" };
        EmailFilter exclusiveMaximum = new EmailFilter { MaxSize = 120, HasMaxSize = true };
        EmailFilter noOtherMailbox = new EmailFilter
        {
            InMailboxOtherThan = new List<string> { "inbox", "archive" }
        };
        EmailFilter wrongAttachment = new EmailFilter
        {
            HasAttachment = false,
            HasHasAttachment = true
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.Matches(email, null, matching), Is.True);
            Assert.That(evaluator.Matches(email, null, exclusiveBefore), Is.False);
            Assert.That(evaluator.Matches(email, null, inclusiveAfter), Is.False);
            Assert.That(evaluator.Matches(email, null, exclusiveMaximum), Is.False);
            Assert.That(evaluator.Matches(email, null, noOtherMailbox), Is.False);
            Assert.That(evaluator.Matches(email, null, wrongAttachment), Is.False);
        }
    }

    [Test]
    public void MatchesIndividualAndThreadKeywordPredicates()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        Email email = Candidate();
        List<Email> thread = ThreadEmails(email);
        EmailFilter matching = new EmailFilter
        {
            HasKeyword = "$flagged",
            NotKeyword = "$draft",
            AllInThreadHaveKeyword = "$seen",
            SomeInThreadHaveKeyword = "$answered",
            NoneInThreadHaveKeyword = "$junk"
        };
        EmailFilter notAll = new EmailFilter { AllInThreadHaveKeyword = "$flagged" };
        EmailFilter noneViolated = new EmailFilter { NoneInThreadHaveKeyword = "$answered" };
        EmailFilter missingIndividual = new EmailFilter { HasKeyword = "$draft" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.Matches(email, thread, matching), Is.True);
            Assert.That(evaluator.Matches(email, thread, notAll), Is.False);
            Assert.That(evaluator.Matches(email, thread, noneViolated), Is.False);
            Assert.That(evaluator.Matches(email, thread, missingIndividual), Is.False);
        }
    }

    [Test]
    public void MatchesSearchAddressSubjectBodyAndHeaderFields()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        Email email = Candidate();
        EmailFilter allTextFields = new EmailFilter
        {
            Text = "alice café visible",
            From = "ALICE example.test",
            To = "Bob",
            Cc = "carol@example.test",
            Bcc = "Dave",
            Subject = "project CAFÉ",
            Body = "visible diagram",
            Header = new List<string> { "x-trace", "release marker" }
        };
        EmailFilter headerExists = new EmailFilter { Header = new List<string> { "X-Trace" } };
        EmailFilter hiddenHtml = new EmailFilter { Body = "hidden-code" };
        EmailFilter unindexedHeader = new EmailFilter { Text = "release marker" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.Matches(email, null, allTextFields), Is.True);
            Assert.That(evaluator.Matches(email, null, headerExists), Is.True);
            Assert.That(evaluator.Matches(email, null, hiddenHtml), Is.False);
            Assert.That(evaluator.Matches(email, null, unindexedHeader), Is.False);
        }
    }

    [Test]
    public void EveryFilterConditionMatchesIndependentlyAndFalseMapValuesDoNotCount()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        Email email = Candidate();
        email.MailboxIds!["disabled"] = false;
        email.Keywords!["$muted"] = false;
        List<Email> thread = ThreadEmails(email);
        Dictionary<string, EmailFilter> matching = new Dictionary<string, EmailFilter>
        {
            [JmapProperties.InMailbox] = new EmailFilter { InMailbox = "inbox" },
            [JmapProperties.InMailboxOtherThan] = new EmailFilter
            {
                InMailboxOtherThan = new List<string> { "inbox" }
            },
            [JmapProperties.Before] = new EmailFilter
            {
                Before = "2026-09-11T12:00:00.1Z"
            },
            [JmapProperties.After] = new EmailFilter
            {
                After = "2026-09-11T12:00:00Z"
            },
            [JmapProperties.MinSize] = new EmailFilter { MinSize = 120, HasMinSize = true },
            [JmapProperties.MaxSize] = new EmailFilter { MaxSize = 121, HasMaxSize = true },
            [JmapProperties.AllInThreadHaveKeyword] = new EmailFilter
            {
                AllInThreadHaveKeyword = "$seen"
            },
            [JmapProperties.SomeInThreadHaveKeyword] = new EmailFilter
            {
                SomeInThreadHaveKeyword = "$answered"
            },
            [JmapProperties.NoneInThreadHaveKeyword] = new EmailFilter
            {
                NoneInThreadHaveKeyword = "$junk"
            },
            [JmapProperties.HasKeyword] = new EmailFilter { HasKeyword = "$flagged" },
            [JmapProperties.NotKeyword] = new EmailFilter { NotKeyword = "$draft" },
            [JmapProperties.HasAttachment] = new EmailFilter
            {
                HasAttachment = true,
                HasHasAttachment = true
            },
            [JmapProperties.Text] = new EmailFilter
            {
                Text = "\"project café\" visible"
            },
            [JmapProperties.From] = new EmailFilter { From = "Alice example.test" },
            [JmapProperties.To] = new EmailFilter { To = "Bob" },
            [JmapProperties.Cc] = new EmailFilter { Cc = "carol@example.test" },
            [JmapProperties.Bcc] = new EmailFilter { Bcc = "Dave" },
            [JmapProperties.Subject] = new EmailFilter { Subject = "\"project café\"" },
            [JmapProperties.Body] = new EmailFilter { Body = "visible diagram" },
            [JmapProperties.Header] = new EmailFilter
            {
                Header = new List<string> { "X-Trace", "release marker" }
            }
        };

        foreach (KeyValuePair<string, EmailFilter> condition in matching)
        {
            Assert.That(evaluator.Matches(email, thread, condition.Value),
                Is.True, condition.Key);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.Matches(email, thread,
                new EmailFilter { InMailbox = "disabled" }), Is.False);
            Assert.That(evaluator.Matches(email, thread,
                new EmailFilter { HasKeyword = "$muted" }), Is.False);
            Assert.That(evaluator.Matches(email, thread,
                new EmailFilter { NotKeyword = "$seen" }), Is.False);
            Assert.That(evaluator.Matches(email, thread, new EmailFilter
            {
                HasAttachment = false,
                HasHasAttachment = true
            }), Is.False);
            Assert.That(evaluator.Matches(email, thread,
                new EmailFilter { Header = new List<string> { "Missing" } }), Is.False);
        }
    }

    [Test]
    public void CombinesNestedAndOrAndNotFilters()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        Email email = Candidate();
        EmailFilter alternatives = Operator(
            JmapFilterOperator.Or,
            new EmailFilter { Subject = "missing" },
            new EmailFilter { HasKeyword = "$flagged" });
        EmailFilter notDraft = Operator(
            JmapFilterOperator.Not,
            new EmailFilter { HasKeyword = "$draft" });
        EmailFilter root = Operator(JmapFilterOperator.And, alternatives, notDraft);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.Matches(email, null, root), Is.True);
            Assert.That(evaluator.Matches(email, null, Operator(JmapFilterOperator.And)), Is.True);
            Assert.That(evaluator.Matches(email, null, Operator(JmapFilterOperator.Or)), Is.False);
        }
    }

    [Test]
    public void ValidatesConditionValuesOperatorShapeDepthAndExtensions()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        EmailFilter valid = new EmailFilter
        {
            InMailbox = "inbox",
            Before = "2026-09-12T00:00:00Z",
            HasKeyword = "$seen",
            Header = new List<string> { "List-Id", "example" }
        };
        EmailFilter badMailbox = new EmailFilter { InMailbox = "bad/id" };
        EmailFilter badDate = new EmailFilter { Before = "tomorrow" };
        EmailFilter badKeyword = new EmailFilter { HasKeyword = "$Seen" };
        EmailFilter badHeader = new EmailFilter { Header = new List<string> { "Bad Header" } };
        EmailFilter badSizeFlags = new EmailFilter { MinSize = 1 };
        EmailFilter mixedOperator = Operator(JmapFilterOperator.And, new EmailFilter());
        mixedOperator.Subject = "not allowed";
        EmailFilter badNot = Operator(JmapFilterOperator.Not, new EmailFilter(), new EmailFilter());
        EmailFilter extension = new EmailFilter();
        extension.UnknownProperties.Add("vendorCondition", Json("true"));
        EmailFilter operatorExtension = Operator(JmapFilterOperator.And, new EmailFilter());
        operatorExtension.UnknownProperties.Add("vendorOperator", Json("true"));
        EmailFilter nested = Operator(JmapFilterOperator.And, new EmailFilter());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluator.ValidateFilter(valid, 8), Is.Null);
            Assert.That(evaluator.ValidateFilter(badMailbox, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(badDate, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(badKeyword, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(badHeader, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(badSizeFlags, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(mixedOperator, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(badNot, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(evaluator.ValidateFilter(extension, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(evaluator.ValidateFilter(operatorExtension, 8)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(evaluator.ValidateFilter(nested, 1)!.Kind,
                Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
        }
    }

    [Test]
    public void ThreadKeywordFiltersRequireTheCompleteMatchingThread()
    {
        EmailFilterEvaluator evaluator = new EmailFilterEvaluator();
        Email email = Candidate();
        EmailFilter filter = new EmailFilter { SomeInThreadHaveKeyword = "$seen" };
        Email wrongThread = new Email
        {
            Id = "other",
            ThreadId = "different",
            Keywords = new Dictionary<string, bool> { ["$seen"] = true }
        };

        JmapProtocolException missing = Assert.Throws<JmapProtocolException>(
            () => evaluator.Matches(email, null, filter))!;
        JmapProtocolException wrong = Assert.Throws<JmapProtocolException>(
            () => evaluator.Matches(email, new List<Email> { email, wrongThread }, filter))!;
        JmapProtocolException omitted = Assert.Throws<JmapProtocolException>(
            () => evaluator.Matches(email, new List<Email> { ThreadEmails(email)[1] }, filter))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Path, Is.EqualTo("threadEmails"));
            Assert.That(wrong.Path, Is.EqualTo("threadEmails/1"));
            Assert.That(omitted.Path, Is.EqualTo("threadEmails"));
        }
    }

    private static Email Candidate()
    {
        EmailBodyPart html = new EmailBodyPart { PartId = "html", Type = "text/html" };
        return new Email
        {
            Id = "e1",
            ThreadId = "t1",
            MailboxIds = new Dictionary<string, bool>
            {
                ["inbox"] = true,
                ["archive"] = true
            },
            Keywords = new Dictionary<string, bool>
            {
                ["$seen"] = true,
                ["$flagged"] = true
            },
            ReceivedAt = "2026-09-11T12:00:00Z",
            Size = 120,
            HasAttachment = true,
            From = Addresses("Alice", "alice@example.test"),
            To = Addresses("Bob", "bob@example.test"),
            Cc = Addresses("Carol", "carol@example.test"),
            Bcc = Addresses("Dave", "dave@example.test"),
            Subject = "Project Café",
            Headers = new List<EmailHeader>
            {
                Header("From", "Alice <alice@example.test>"),
                Header("To", "Bob <bob@example.test>"),
                Header("Cc", "Carol <carol@example.test>"),
                Header("Bcc", "Dave <dave@example.test>"),
                Header("Subject", "=?UTF-8?Q?Project_Caf=C3=A9?="),
                Header("X-Trace", "release marker")
            },
            BodyStructure = new EmailBodyPart
            {
                Type = "multipart/alternative",
                SubParts = new List<EmailBodyPart> { html }
            },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["html"] = new EmailBodyValue
                {
                    Value = "<p>Visible body <img alt='diagram'></p><script>hidden-code</script>"
                }
            }
        };
    }

    private static List<Email> ThreadEmails(Email candidate)
    {
        Email second = new Email
        {
            Id = "e2",
            ThreadId = "t1",
            Keywords = new Dictionary<string, bool>
            {
                ["$seen"] = true,
                ["$answered"] = true
            }
        };
        return new List<Email> { candidate, second };
    }

    private static List<EmailAddress> Addresses(string name, string address)
    {
        return new List<EmailAddress> { new EmailAddress { Name = name, Email = address } };
    }

    private static EmailHeader Header(string name, string value)
    {
        return new EmailHeader { Name = name, Value = value };
    }

    private static EmailFilter Operator(JmapFilterOperator value, params EmailFilter[] conditions)
    {
        return new EmailFilter
        {
            Operator = value,
            HasOperator = true,
            Conditions = new List<EmailFilter>(conditions)
        };
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }
}
