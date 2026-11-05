using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborOptionsAndResultsTests
{
    [Test]
    public void DecodeOptionsHaveBoundedStandardsCompatibleDefaults()
    {
        CborDecodeOptions options = new CborDecodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.AllowTrailingData, Is.False);
            Assert.That(options.DuplicateKeyPolicy, Is.EqualTo(CborDuplicateKeyPolicy.Reject));
            Assert.That(options.TagPolicy, Is.EqualTo(CborTagPolicy.ValidateKnown));
            Assert.That(options.SharedValuePolicy, Is.EqualTo(CborSharedValuePolicy.Disabled));
            Assert.That(options.ValidationLevel, Is.EqualTo(CborValidationLevel.Basic));
            Assert.That(
                options.DeterministicEncodingProfile,
                Is.EqualTo(CborEncodingProfile.CoreDeterministic)
            );
            Assert.That(options.AllowIndefiniteLength, Is.True);
            Assert.That(options.AllowUnknownSimpleValues, Is.True);
            Assert.That(options.RejectInvalidUtf8, Is.True);
            Assert.That(options.RetainRepresentation, Is.False);
            Assert.That(options.RetainSourceRanges, Is.False);
            Assert.That(options.MaxNestingDepth, Is.EqualTo(64));
            Assert.That(options.MaxTotalItems, Is.EqualTo(1000000));
            Assert.That(options.MaxArrayItems, Is.EqualTo(100000));
            Assert.That(options.MaxMapPairs, Is.EqualTo(100000));
            Assert.That(options.MaxStringBytes, Is.EqualTo(16777216));
            Assert.That(options.MaxTotalBytes, Is.EqualTo(67108864));
            Assert.That(options.MaxRetainedSourceBytes, Is.EqualTo(16777216));
            Assert.That(options.MaxSequenceItems, Is.EqualTo(1000000));
            Assert.That(options.MaxSequenceBytes, Is.EqualTo(67108864));
            Assert.That(options.MaxEmbeddedDepth, Is.EqualTo(8));
            Assert.That(options.MaxValidationIssues, Is.EqualTo(100));
            Assert.That(options.CollectValidationIssues, Is.True);
            Assert.That(options.TagRegistrations, Is.Empty);
        });
    }

    [Test]
    public void EncodeAndTextOptionsHaveExplicitLimits()
    {
        CborEncodeOptions encode = new CborEncodeOptions();
        CborDiagnosticOptions diagnostic = new CborDiagnosticOptions();
        CborJsonOptions json = new CborJsonOptions();

        Assert.Multiple(() =>
        {
            Assert.That(encode.EncodingProfile, Is.EqualTo(CborEncodingProfile.Basic));
            Assert.That(encode.MapKeyOrder, Is.EqualTo(CborMapKeyOrder.Preserve));
            Assert.That(encode.PreserveRepresentation, Is.False);
            Assert.That(encode.ReduceFloatWidth, Is.True);
            Assert.That(encode.AllowIndefiniteLength, Is.True);
            Assert.That(encode.LeaveOpen, Is.False);
            Assert.That(encode.MaxNestingDepth, Is.EqualTo(64));
            Assert.That(encode.MaxTotalItems, Is.EqualTo(1000000));
            Assert.That(encode.MaxOutputBytes, Is.EqualTo(67108864));
            Assert.That(encode.MaxSequenceItems, Is.EqualTo(1000000));
            Assert.That(encode.MaxSequenceBytes, Is.EqualTo(67108864));
            Assert.That(encode.MaxSharedValues, Is.EqualTo(100000));
            Assert.That(diagnostic.Style, Is.EqualTo(CborDiagnosticStyle.Compact));
            Assert.That(diagnostic.IndentSize, Is.EqualTo(2));
            Assert.That(diagnostic.MapKeyOrder, Is.EqualTo(CborMapKeyOrder.Preserve));
            Assert.That(diagnostic.MaxNestingDepth, Is.EqualTo(64));
            Assert.That(diagnostic.MaxCharacters, Is.EqualTo(16777216));
            Assert.That(diagnostic.MaxEmbeddedDepth, Is.EqualTo(8));
            Assert.That(diagnostic.MaxEmbeddedItems, Is.EqualTo(1000000));
            Assert.That(diagnostic.MaxEmbeddedBytes, Is.EqualTo(67108864));
            Assert.That(json.Policy, Is.EqualTo(CborJsonPolicy.Strict));
            Assert.That(
                json.DuplicatePropertyPolicy,
                Is.EqualTo(CborDuplicateKeyPolicy.Preserve));
            Assert.That(
                json.ByteStringConvention,
                Is.EqualTo(CborJsonByteStringConvention.TextString));
            Assert.That(
                json.TagConvention,
                Is.EqualTo(CborJsonTagConvention.Map));
            Assert.That(json.Indented, Is.False);
            Assert.That(json.MaxNestingDepth, Is.EqualTo(64));
            Assert.That(json.MaxInputCharacters, Is.EqualTo(67108864));
            Assert.That(json.MaxOutputCharacters, Is.EqualTo(67108864));
        });
    }

    [Test]
    public void MutableOptionCollectionsAreIndependent()
    {
        CborDecodeOptions firstDecode = new CborDecodeOptions();
        CborDecodeOptions secondDecode = new CborDecodeOptions();
        CborEncodeOptions firstEncode = new CborEncodeOptions();
        CborEncodeOptions secondEncode = new CborEncodeOptions();
        CborTagRegistration registration = new CborTagRegistration
        {
            TagNumber = CborTagNumbers.Uri,
            Name = "URI"
        };

        firstDecode.TagRegistrations.Add(registration);
        firstEncode.TagRegistrations.Add(registration);

        Assert.Multiple(() =>
        {
            Assert.That(firstDecode.TagRegistrations, Has.Count.EqualTo(1));
            Assert.That(secondDecode.TagRegistrations, Is.Empty);
            Assert.That(firstEncode.TagRegistrations, Has.Count.EqualTo(1));
            Assert.That(secondEncode.TagRegistrations, Is.Empty);
        });
    }

    [Test]
    public void MappingOptionsAndContextsHaveIndependentBoundedState()
    {
        CborMappingOptions options = new CborMappingOptions();
        CborMappingContext first = new CborMappingContext();
        CborMappingContext second = new CborMappingContext();
        object native = new object();
        CborValue value = new CborValue();

        first.NativeReferences.Add(native);
        first.ValueReferences.Add(value);

        Assert.Multiple(() =>
        {
            Assert.That(options.RequireAllMembers, Is.True);
            Assert.That(options.MapObjectsAsArrays, Is.False);
            Assert.That(options.IgnoreUnknownMembers, Is.False);
            Assert.That(options.AllowIntegerNarrowing, Is.False);
            Assert.That(options.PreserveObjectReferences, Is.False);
            Assert.That(options.MemberNamingPolicy,
                Is.EqualTo(CborMappingNamingPolicy.Exact));
            Assert.That(options.DictionaryKeyPolicy,
                Is.EqualTo(CborMappingKeyPolicy.Any));
            Assert.That(options.MaxNestingDepth, Is.EqualTo(64));
            Assert.That(options.MaxCollectionItems, Is.EqualTo(100000));
            Assert.That(options.MaxTotalItems, Is.EqualTo(1000000));
            Assert.That(options.TypeRegistrations, Is.Empty);
            Assert.That(first.NativeReferences, Has.Count.EqualTo(1));
            Assert.That(first.ValueReferences, Has.Count.EqualTo(1));
            Assert.That(second.NativeReferences, Is.Empty);
            Assert.That(second.ValueReferences, Is.Empty);
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
        });
    }

    [Test]
    public void ResultsStartInSafeIncompleteStates()
    {
        CborReadResult read = new CborReadResult();
        CborTokenReadResult tokenRead = new CborTokenReadResult();
        CborWriteResult write = new CborWriteResult();
        CborValidationIssue issue = new CborValidationIssue();
        CborValidationResult validation = new CborValidationResult();
        CborTagResult tag = new CborTagResult();

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(CborReadStatus.None));
            Assert.That(read.ErrorOffset, Is.EqualTo(-1));
            Assert.That(read.ItemIndex, Is.EqualTo(-1));
            Assert.That(read.SourceOffset, Is.EqualTo(-1));
            Assert.That(read.Value, Is.Null);
            Assert.That(tokenRead.Status, Is.EqualTo(CborReadStatus.None));
            Assert.That(tokenRead.ErrorOffset, Is.EqualTo(-1));
            Assert.That(tokenRead.Token, Is.Null);
            Assert.That(write.Status, Is.EqualTo(CborWriteStatus.None));
            Assert.That(write.Written, Is.Zero);
            Assert.That(write.Required, Is.Zero);
            Assert.That(issue.ByteOffset, Is.EqualTo(-1));
            Assert.That(issue.Severity, Is.EqualTo(CborIssueSeverity.Error));
            Assert.That(validation.IsValid, Is.False);
            Assert.That(validation.Issues, Is.Empty);
            Assert.That(tag.Handled, Is.False);
            Assert.That(tag.IsValid, Is.False);
        });
    }

    [Test]
    public void RegistrationsAndMappingContextRetainTheirCollaborators()
    {
        StubTagHandler tagHandler = new StubTagHandler();
        StubTypeAdapter typeAdapter = new StubTypeAdapter();
        CborTagRegistration tagRegistration = new CborTagRegistration
        {
            TagNumber = CborTagNumbers.SelfDescribedCbor,
            Name = "self-described CBOR",
            Handler = tagHandler
        };
        CborTypeRegistration typeRegistration = new CborTypeRegistration
        {
            RuntimeType = typeof(string),
            Adapter = typeAdapter
        };
        CborMappingContext context = new CborMappingContext();
        context.Options.TypeRegistrations.Add(typeRegistration);

        Assert.Multiple(() =>
        {
            Assert.That(tagRegistration.TagNumber, Is.EqualTo(CborTagNumbers.SelfDescribedCbor));
            Assert.That(tagRegistration.Handler, Is.SameAs(tagHandler));
            Assert.That(tagRegistration.ValidateContent, Is.True);
            Assert.That(typeRegistration.RuntimeType, Is.EqualTo(typeof(string)));
            Assert.That(typeRegistration.Adapter, Is.SameAs(typeAdapter));
            Assert.That(context.Options.TypeRegistrations[0], Is.SameAs(typeRegistration));
        });
    }

    private sealed class StubTagHandler : ICborTagHandler
    {
        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult { Handled = true, IsValid = true, Value = content };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult { Handled = true, IsValid = true, Value = content };
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult { Handled = true, IsValid = true, NativeValue = nativeValue };
        }
    }

    private sealed class StubTypeAdapter : ICborTypeAdapter
    {
        public bool SupportsType(Type runtimeType)
        {
            return runtimeType == typeof(string);
        }

        public CborValue EncodeObject(object value, CborMappingContext context)
        {
            return new CborValue { Kind = CborValueKind.TextString, TextStringValue = value.ToString() };
        }

        public object? DecodeObject(CborValue value, Type targetType, CborMappingContext context)
        {
            return value.TextStringValue;
        }
    }
}
