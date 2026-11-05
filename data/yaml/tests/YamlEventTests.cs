using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlEventTests
{
    [Test]
    public void EventFactories_CreateExpectedEventFields()
    {
        global::yaml.YamlSourceSpan span = global::yaml.YamlSourceSpan.FromBounds(1, 2, 3, 4, 2, 6);

        global::yaml.YamlEvent scalar = global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.SingleQuoted, span);
        global::yaml.YamlEvent alias = global::yaml.YamlEvent.Alias("anchor", span);
        global::yaml.YamlEvent sequence = global::yaml.YamlEvent.SequenceStart(global::yaml.YamlCollectionStyle.Flow, span);
        global::yaml.YamlEvent mapping = global::yaml.YamlEvent.MappingStart(global::yaml.YamlCollectionStyle.Block, span);

        Assert.That(scalar.Kind, Is.EqualTo(global::yaml.YamlEventKind.Scalar));
        Assert.That(scalar.Value, Is.EqualTo("value"));
        Assert.That(scalar.ScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.SingleQuoted));
        Assert.That(scalar.Span, Is.SameAs(span));
        Assert.That(alias.Kind, Is.EqualTo(global::yaml.YamlEventKind.Alias));
        Assert.That(alias.AliasName, Is.EqualTo("anchor"));
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(mapping.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
    }

    [Test]
    public void DiagnosticEvent_StoresDiagnosticAndSpan()
    {
        global::yaml.YamlDiagnostic diagnostic = global::yaml.YamlDiagnostic.Error("YAML999", "bad", global::yaml.YamlSourceSpan.FromBounds(0, 1, 1, 1, 1, 2));

        global::yaml.YamlEvent yamlEvent = global::yaml.YamlEvent.DiagnosticEvent(diagnostic);

        Assert.That(yamlEvent.Kind, Is.EqualTo(global::yaml.YamlEventKind.Diagnostic));
        Assert.That(yamlEvent.Diagnostic, Is.SameAs(diagnostic));
        Assert.That(yamlEvent.Span, Is.SameAs(diagnostic.Span));
    }

    [Test]
    public void Copy_CopiesMutableFieldsAndSpan()
    {
        global::yaml.YamlEvent original = global::yaml.YamlEvent.Scalar("value", global::yaml.YamlScalarStyle.Plain, global::yaml.YamlSourceSpan.FromBounds(1, 1, 2, 6, 1, 7));
        original.Tag = "tag:yaml.org,2002:str";
        original.Anchor = "a1";

        global::yaml.YamlEvent copy = original.Copy();

        Assert.That(copy, Is.Not.SameAs(original));
        Assert.That(copy.Kind, Is.EqualTo(original.Kind));
        Assert.That(copy.Value, Is.EqualTo(original.Value));
        Assert.That(copy.Tag, Is.EqualTo(original.Tag));
        Assert.That(copy.Anchor, Is.EqualTo(original.Anchor));
        Assert.That(copy.Span, Is.Not.SameAs(original.Span));
        Assert.That(copy.Span.StartIndex, Is.EqualTo(original.Span.StartIndex));
    }

    [Test]
    public void EventCategoryHelpers_ClassifyNodeCollectionAndBoundaryEvents()
    {
        Assert.That(global::yaml.YamlEventRules.IsNodeEvent(global::yaml.YamlEventKind.Scalar), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsNodeEvent(global::yaml.YamlEventKind.Alias), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsCollectionStart(global::yaml.YamlEventKind.SequenceStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsCollectionStart(global::yaml.YamlEventKind.MappingStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsCollectionEnd(global::yaml.YamlEventKind.SequenceEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsCollectionEnd(global::yaml.YamlEventKind.MappingEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsStreamBoundary(global::yaml.YamlEventKind.StreamStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.IsDocumentBoundary(global::yaml.YamlEventKind.DocumentEnd), Is.True);
    }

    [Test]
    public void OrderingRules_ConstrainStreamAndDocumentBoundaries()
    {
        Assert.That(global::yaml.YamlEventRules.CanStart(global::yaml.YamlEventKind.StreamStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanStart(global::yaml.YamlEventKind.DocumentStart), Is.False);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.Unknown, global::yaml.YamlEventKind.StreamStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.StreamStart, global::yaml.YamlEventKind.DocumentStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.StreamStart, global::yaml.YamlEventKind.StreamEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.StreamStart, global::yaml.YamlEventKind.Scalar), Is.False);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.DocumentEnd, global::yaml.YamlEventKind.DocumentStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.DocumentEnd, global::yaml.YamlEventKind.StreamEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.DocumentEnd, global::yaml.YamlEventKind.Scalar), Is.False);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.StreamEnd, global::yaml.YamlEventKind.DocumentStart), Is.False);
    }

    [Test]
    public void OrderingRules_AllowCoarseNodeAndCollectionSequences()
    {
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.DocumentStart, global::yaml.YamlEventKind.Scalar), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.DocumentStart, global::yaml.YamlEventKind.SequenceStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.SequenceStart, global::yaml.YamlEventKind.SequenceEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.SequenceStart, global::yaml.YamlEventKind.MappingStart), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.Scalar, global::yaml.YamlEventKind.Scalar), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.Scalar, global::yaml.YamlEventKind.SequenceEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.MappingEnd, global::yaml.YamlEventKind.DocumentEnd), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.Comment, global::yaml.YamlEventKind.Scalar), Is.True);
        Assert.That(global::yaml.YamlEventRules.CanFollow(global::yaml.YamlEventKind.Diagnostic, global::yaml.YamlEventKind.DocumentEnd), Is.True);
    }
}
