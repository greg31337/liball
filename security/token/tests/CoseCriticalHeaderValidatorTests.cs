using token;

namespace token.Tests;

public class CoseCriticalHeaderValidatorTests
{
    [Test]
    public void HeaderWithoutCritIgnoresUnrecognizedNonCriticalExtensions()
    {
        CoseHeader header = Header(
            new CborMapEntry(Text("urn:example:ignored"), Boolean(true)));

        TokenResult result = CoseCriticalHeaderValidator.Validate(header);

        AssertSuccess(result);
    }

    [Test]
    public void UnderstoodIntegerTextAndLargeLabelsAreAccepted()
    {
        CborItem large = Unsigned((ulong)int.MaxValue + 1UL);
        CoseHeader header = Header(
            Entry(CoseHeaderLabels.Critical,
                Array(Int(-70000), Text("application-control"), large)),
            new CborMapEntry(Int(-70000), Boolean(true)),
            new CborMapEntry(Text("application-control"), Boolean(true)),
            new CborMapEntry(large, Boolean(true)));

        TokenResult result = CoseCriticalHeaderValidator.Validate(
            header,
            new CborItem[]
            {
                Text("application-control"),
                Unsigned((ulong)int.MaxValue + 1UL),
                Int(-70000)
            });

        AssertSuccess(result);
    }

    [Test]
    public void UnknownCriticalLabelsAreRejectedWithExactMatching()
    {
        CoseHeader textHeader = Header(
            Entry(CoseHeaderLabels.Critical, Array(Text("Custom"))),
            new CborMapEntry(Text("Custom"), Boolean(true)));
        CoseHeader signedHeader = Header(
            Entry(CoseHeaderLabels.Critical, Array(Int(-1))),
            new CborMapEntry(Int(-1), Boolean(true)));

        TokenResult textResult = CoseCriticalHeaderValidator.Validate(
            textHeader,
            new CborItem[] { Text("custom") });
        TokenResult signedResult = CoseCriticalHeaderValidator.Validate(
            signedHeader,
            new CborItem[] { Unsigned(0) });

        Assert.Multiple(() =>
        {
            AssertFailure(textResult, "unknown_cose_critical_header", 0);
            AssertFailure(signedResult, "unknown_cose_critical_header", 0);
        });
    }

    [Test]
    public void CommonParametersProcessedByTheHeaderModelAreUnderstood()
    {
        CborItem critical = Array(
            Int(CoseHeaderLabels.Algorithm),
            Int(CoseHeaderLabels.Critical),
            Int(CoseHeaderLabels.ContentType));
        CoseHeader header = Header(
            Entry(CoseHeaderLabels.Algorithm, Int(-7)),
            Entry(CoseHeaderLabels.Critical, critical),
            Entry(CoseHeaderLabels.ContentType, Text("application/cwt")));

        TokenResult result = CoseCriticalHeaderValidator.Validate(header);

        AssertSuccess(result);
    }

    [Test]
    public void CritMustBeProtected()
    {
        CoseHeader header = CoseHeader.Create(
            System.Array.Empty<CborMapEntry>(),
            new CborMapEntry[]
            {
                Entry(CoseHeaderLabels.Critical, Array(Text("custom"))),
                new CborMapEntry(Text("custom"), Boolean(true))
            });

        TokenResult result = CoseCriticalHeaderValidator.Validate(
            header,
            new CborItem[] { Text("custom") });

        AssertFailure(result, "cose_critical_header_not_protected", -1);
    }

    [Test]
    public void CriticalParameterMustItselfBeProtected()
    {
        CoseHeader header = CoseHeader.Create(
            new CborMapEntry[]
            {
                Entry(CoseHeaderLabels.Critical, Array(Text("custom")))
            },
            new CborMapEntry[]
            {
                new CborMapEntry(Text("custom"), Boolean(true))
            });

        TokenResult result = CoseCriticalHeaderValidator.Validate(
            header,
            new CborItem[] { Text("custom") });

        AssertFailure(result, "missing_cose_critical_header", 0);
    }

    [Test]
    public void EmptyInvalidAndDuplicateCriticalLabelsAreRejected()
    {
        CoseHeader empty = Header(
            Entry(CoseHeaderLabels.Critical, Array()));
        CoseHeader invalid = Header(
            Entry(CoseHeaderLabels.Critical, Array(Boolean(true))));
        CoseHeader duplicate = Header(
            Entry(CoseHeaderLabels.Critical, Array(Text("custom"), Text("custom"))),
            new CborMapEntry(Text("custom"), Boolean(true)));

        TokenResult emptyResult = CoseCriticalHeaderValidator.Validate(empty);
        TokenResult invalidResult = CoseCriticalHeaderValidator.Validate(invalid);
        TokenResult duplicateResult = CoseCriticalHeaderValidator.Validate(
            duplicate,
            new CborItem[] { Text("custom") });

        Assert.Multiple(() =>
        {
            AssertFailure(emptyResult, "invalid_cose_critical_header", -1);
            AssertFailure(invalidResult, "invalid_cose_critical_header", 0);
            AssertFailure(duplicateResult, "duplicate_cose_critical_header", 1);
        });
    }

    [Test]
    public void InvalidMutableHeaderStructuresReturnStableFailures()
    {
        CoseHeader missing = new CoseHeader();
        missing.ProtectedParameters = null;
        CoseHeader incomplete = new CoseHeader();
        incomplete.ProtectedParameters = new CborMapEntry[] { null };
        CoseHeader collision = CoseHeader.Create(
            new CborMapEntry[] { new CborMapEntry(Text("same"), Boolean(true)) },
            new CborMapEntry[] { new CborMapEntry(Text("same"), Boolean(false)) });

        TokenResult missingResult = CoseCriticalHeaderValidator.Validate(missing);
        TokenResult incompleteResult = CoseCriticalHeaderValidator.Validate(incomplete);
        TokenResult collisionResult = CoseCriticalHeaderValidator.Validate(collision);

        Assert.Multiple(() =>
        {
            AssertFailure(missingResult, "invalid_cose_header", -1);
            AssertFailure(incompleteResult, "invalid_cose_header", -1);
            AssertFailure(collisionResult, "duplicate_cose_header_label", 0);
        });
    }

    [Test]
    public void InvalidUnderstoodLabelsReturnAStableFailure()
    {
        CoseHeader header = new CoseHeader();

        TokenResult result = CoseCriticalHeaderValidator.Validate(
            header,
            new CborItem[] { Boolean(true) });

        AssertFailure(result, "invalid_cose_understood_header_label", 0);
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        CoseHeader header = new CoseHeader();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseCriticalHeaderValidator.Validate(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseCriticalHeaderValidator.Validate(header, null),
                Throws.ArgumentNullException);
        });
    }

    static CoseHeader Header(params CborMapEntry[] protectedParameters)
    {
        return CoseHeader.Create(
            protectedParameters,
            System.Array.Empty<CborMapEntry>());
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
    }

    static CborItem Int(int value)
    {
        if (value >= 0)
        {
            return Unsigned((ulong)value);
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.NegativeInteger;
        item.IntegerArgument = (ulong)(-(long)value - 1L);
        return item;
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static void AssertSuccess(TokenResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    static void AssertFailure(TokenResult result, string code, int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(position));
    }
}
