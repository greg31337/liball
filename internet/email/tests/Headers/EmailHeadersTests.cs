using System.Text;
using email;

namespace email.Tests;

public class EmailHeadersTests
{
    [Test]
    public void PreservesInsertionOrderDuplicatesCasingAndIdentity()
    {
        EmailHeader first = Header("Received", " from one");
        EmailHeader subject = Header("sUbJeCt", " hello");
        EmailHeader second = Header("RECEIVED", " from two");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { first, subject, second });

        Assert.That(headers.GetCount(), Is.EqualTo(3));
        Assert.That(headers.GetAt(0), Is.SameAs(first));
        Assert.That(headers.GetAt(1), Is.SameAs(subject));
        Assert.That(headers.GetAt(2), Is.SameAs(second));
        Assert.That(headers.GetAt(0).GetName(), Is.EqualTo("Received"));
        Assert.That(headers.GetAt(2).GetName(), Is.EqualTo("RECEIVED"));
    }

    [Test]
    public void NamedReadsAreCaseInsensitiveAndNeverCollapseDuplicates()
    {
        EmailHeader first = Header("Received", " one");
        EmailHeader middle = Header("Subject", " text");
        EmailHeader second = Header("received", " two");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { first, middle, second });

        Assert.That(headers.Contains("RECEIVED"), Is.True);
        Assert.That(headers.Contains("missing"), Is.False);
        Assert.That(headers.HasDuplicate("received"), Is.True);
        Assert.That(headers.HasDuplicate("subject"), Is.False);
        Assert.That(headers.GetCountNamed("rEcEiVeD"), Is.EqualTo(2));
        Assert.That(headers.IndexOfFirst("received"), Is.EqualTo(0));
        Assert.That(headers.IndexOfLast("RECEIVED"), Is.EqualTo(2));
        Assert.That(headers.GetFirst("received"), Is.SameAs(first));
        Assert.That(headers.GetLast("received"), Is.SameAs(second));
        Assert.That(headers.GetAllNamed("received"), Is.EqualTo(new[] { first, second }));
        Assert.That(headers.GetFirst("missing"), Is.Null);
        Assert.That(headers.GetAllNamed("missing"), Is.Empty);
    }

    [Test]
    public void AddAndInsertPreserveExistingHeadersAndExactTargetPosition()
    {
        EmailHeader first = Header("A", " one");
        EmailHeader second = Header("B", " two");
        EmailHeader inserted = Header("X-Test", " middle");
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();

        headers.Add(first);
        headers.Add(second);
        headers.Insert(1, inserted);

        Assert.That(headers.GetAll(), Is.EqualTo(new[] { first, inserted, second }));
        Assert.That(headers.ContainsHeader(inserted), Is.True);
        Assert.That(headers.IndexOf(inserted), Is.EqualTo(1));
        Assert.That(first.IsDirty(), Is.False);
        Assert.That(inserted.IsDirty(), Is.False);
    }

    [Test]
    public void ReplaceFirstChangesOnlyTheFirstCaseInsensitiveMatch()
    {
        EmailHeader first = Header("X-Test", " one");
        EmailHeader middle = Header("Subject", " text");
        EmailHeader second = Header("x-test", " two");
        EmailHeader replacement = Header("X-Replaced", " new");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { first, middle, second });

        bool replaced = headers.ReplaceFirst("X-TEST", replacement);
        bool missing = headers.ReplaceFirst("Missing", Header("Unused", " value"));

        Assert.That(replaced, Is.True);
        Assert.That(missing, Is.False);
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { replacement, middle, second }));
        Assert.That(headers.GetCountNamed("x-test"), Is.EqualTo(1));
        Assert.That(headers.GetCountNamed("x-replaced"), Is.EqualTo(1));
    }

    [Test]
    public void ReplaceAllInsertsOneReplacementAtFirstMatchAndRetainsOtherOrder()
    {
        EmailHeader prefix = Header("A", " prefix");
        EmailHeader first = Header("X-Test", " one");
        EmailHeader middle = Header("B", " middle");
        EmailHeader second = Header("x-test", " two");
        EmailHeader suffix = Header("C", " suffix");
        EmailHeader replacement = Header("X-Test", " replacement");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { prefix, first, middle, second, suffix });

        int removed = headers.ReplaceAll("X-TEST", replacement);

        Assert.That(removed, Is.EqualTo(2));
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { prefix, replacement, middle, suffix }));
        Assert.That(headers.GetCountNamed("x-test"), Is.EqualTo(1));
    }

    [Test]
    public void ReplaceAllSupportsMultipleOrZeroReplacementHeadersAtomically()
    {
        EmailHeader before = Header("A", " before");
        EmailHeader oldOne = Header("X-Test", " one");
        EmailHeader between = Header("B", " between");
        EmailHeader oldTwo = Header("X-Test", " two");
        EmailHeader replacementOne = Header("X-Test", " new one");
        EmailHeader replacementTwo = Header("X-Test", " new two");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { before, oldOne, between, oldTwo });

        int removed = headers.ReplaceAllWithHeaders("x-test", new List<EmailHeader> { replacementOne, replacementTwo });
        int removedAgain = headers.ReplaceAllWithHeaders("X-Test", new List<EmailHeader>());

        Assert.That(removed, Is.EqualTo(2));
        Assert.That(removedAgain, Is.EqualTo(2));
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { before, between }));
        Assert.That(headers.ReplaceAll("missing", replacementOne), Is.Zero);
    }

    [Test]
    public void RemoveOperationsReturnRemovedObjectsInOriginalOrder()
    {
        EmailHeader first = Header("Received", " one");
        EmailHeader subject = Header("Subject", " text");
        EmailHeader second = Header("received", " two");
        EmailHeader third = Header("RECEIVED", " three");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { first, subject, second, third });

        EmailHeader removedFirst = headers.RemoveFirst("rEcEiVeD")!;
        List<EmailHeader> removedRest = headers.RemoveAll("RECEIVED");

        Assert.That(removedFirst, Is.SameAs(first));
        Assert.That(removedRest, Is.EqualTo(new[] { second, third }));
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { subject }));
        Assert.That(headers.RemoveFirst("missing"), Is.Null);
        Assert.That(headers.RemoveAll("missing"), Is.Empty);
    }

    [Test]
    public void MoveMoveFirstSwapAndRemoveAtReorderByIdentity()
    {
        EmailHeader a = Header("A", " 1");
        EmailHeader b = Header("B", " 2");
        EmailHeader c = Header("C", " 3");
        EmailHeader d = Header("D", " 4");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { a, b, c, d });

        headers.Move(0, 3);
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { b, c, d, a }));
        Assert.That(headers.MoveFirst("D", 0), Is.True);
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { d, b, c, a }));
        headers.Swap(1, 3);
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { d, a, c, b }));
        EmailHeader removed = headers.RemoveAt(2);
        Assert.That(removed, Is.SameAs(c));
        Assert.That(headers.GetAll(), Is.EqualTo(new[] { d, a, b }));
        Assert.That(headers.MoveFirst("missing", 1), Is.False);
    }

    [Test]
    public void ClearRemovesEveryEntryWithoutMutatingHeaderObjects()
    {
        EmailHeader first = Header("A", " one");
        EmailHeader second = Header("B", " two");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { first, second });

        headers.Clear();

        Assert.That(headers.GetCount(), Is.Zero);
        Assert.That(first.GetName(), Is.EqualTo("A"));
        Assert.That(Ascii(first.GetRawValue()), Is.EqualTo(" one"));
        Assert.That(first.IsDirty(), Is.False);
        Assert.That(second.IsDirty(), Is.False);
    }

    [Test]
    public void MutationsRejectUnsafeNamesValuesAndNullWithoutPartialChanges()
    {
        EmailHeader existing = Header("Subject", " safe");
        EmailHeader valid = Header("X-Valid", " safe");
        EmailHeader unsafeName = Preserved(Bytes("Bad\r\nInjected"), Bytes(" value"));
        EmailHeader unsafeValue = Preserved(Bytes("X-Test"), Bytes(" safe\r\nInjected: yes"));
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { existing });

        Assert.Throws<ArgumentNullException>(() => headers.Add(null!));
        Assert.Throws<ArgumentException>(() => headers.Add(unsafeName));
        Assert.Throws<ArgumentException>(() => headers.Insert(0, unsafeValue));
        Assert.Throws<ArgumentException>(() => headers.ReplaceFirst("subject", unsafeValue));
        Assert.Throws<ArgumentException>(() => headers.ReplaceAllWithHeaders("subject", new List<EmailHeader> { valid, unsafeName }));

        Assert.That(headers.GetAll(), Is.EqualTo(new[] { existing }));
        Assert.That(existing.IsDirty(), Is.False);
    }

    [Test]
    public void PreservedConstructorCanRetainMalformedSourceHeadersForLaterRecovery()
    {
        EmailHeader unsafeName = Preserved(Bytes("Bad\0Name"), Bytes(" value"));
        EmailHeader unsafeValue = Preserved(Bytes("X-Test"), Bytes(" one\n two"));

        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { unsafeName, unsafeValue });

        Assert.That(headers.GetCount(), Is.EqualTo(2));
        Assert.That(headers.GetAt(0), Is.SameAs(unsafeName));
        Assert.That(headers.GetAt(1), Is.SameAs(unsafeValue));
        Assert.That(Ascii(headers.GetAt(0).GetOriginalNameBytes()), Is.EqualTo("Bad\0Name"));
        Assert.That(Ascii(headers.GetAt(1).GetOriginalRawValue()), Is.EqualTo(" one\n two"));
    }

    [Test]
    public void ReturnedListsAreDefensiveSnapshotsWhileHeadersRemainSharedObjects()
    {
        EmailHeader first = Header("A", " one");
        EmailHeader second = Header("B", " two");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { first, second });
        List<EmailHeader> all = headers.GetAll();
        List<EmailHeader> named = headers.GetAllNamed("a");
        all.Clear();
        named.Add(second);

        Assert.That(headers.GetCount(), Is.EqualTo(2));
        Assert.That(headers.GetAllNamed("a"), Is.EqualTo(new[] { first }));

        first.SetValue(" changed");
        Assert.That(Ascii(headers.GetAt(0).GetRawValue()), Is.EqualTo(" changed"));
    }

    [Test]
    public void LookupsTrackSafeHeaderRenamesWithoutAStaleNameIndex()
    {
        EmailHeader header = Header("X-Old", " value");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header });

        header.SetName("X-New");

        Assert.That(headers.Contains("x-old"), Is.False);
        Assert.That(headers.Contains("X-NEW"), Is.True);
        Assert.That(headers.GetFirst("x-new"), Is.SameAs(header));
    }

    [Test]
    public void NameMatchingUsesAsciiCaseFoldingOnly()
    {
        EmailHeader nonAscii = Preserved(Bytes("Ü"), Bytes(" value"));
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { nonAscii });

        Assert.That(headers.Contains("ü"), Is.False);
        Assert.That(headers.Contains("Ü"), Is.False);
    }

    [Test]
    public void CollectionMutationsDoNotInvalidateUnchangedHeaderCaches()
    {
        EmailHeader contentType = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-Type", Bytes(" text/plain; charset=utf-8"), EmailHeaderValueKind.MediaType);
        EmailHeader sibling = Header("Subject", " hello");
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { contentType, sibling });
        object parsed = contentType.GetStructuredValue()!;

        headers.Insert(0, Header("X-Trace", " one"));
        headers.MoveFirst("subject", 0);
        headers.RemoveFirst("x-trace");

        Assert.That(contentType.HasStructuredValueCache(), Is.True);
        Assert.That(contentType.GetStructuredValue(), Is.SameAs(parsed));
        Assert.That(contentType.IsDirty(), Is.False);
        Assert.That(sibling.IsDirty(), Is.False);
    }

    [Test]
    public void IndexAndArgumentValidationIsDeterministic()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { Header("A", " one") });

        Assert.Throws<ArgumentOutOfRangeException>(() => headers.GetAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => headers.GetAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => headers.Insert(2, Header("B", " two")));
        Assert.Throws<ArgumentOutOfRangeException>(() => headers.Move(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => headers.Swap(0, 1));
        Assert.Throws<ArgumentNullException>(() => headers.GetFirst(null!));
        Assert.Throws<ArgumentNullException>(() => headers.ContainsHeader((EmailHeader)null!));
        Assert.Throws<ArgumentNullException>(() => headers.IndexOf(null!));
        Assert.Throws<ArgumentNullException>(() => headers.ReplaceFirst("missing", null!));
        Assert.Throws<ArgumentNullException>(() => headers.ReplaceAllWithHeaders("a", (List<EmailHeader>)null!));
        Assert.That(headers.GetCount(), Is.EqualTo(1));
    }

    static EmailHeader Header(string name, string value)
    {
        return new EmailHeaderOperations().CreateWithNameAndRawValue(name, Bytes(value));
    }

    static EmailHeader Preserved(byte[] name, byte[] value)
    {
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssues(name, value, EmailLineEnding.CrLf, null!, new EmailSourcePosition(0, 1, 1), EmailHeaderValueKind.Unknown, EmailStructuredValueParseMode.Tolerant, new EmailIssueList());
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}
