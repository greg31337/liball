namespace svg.Tests;

public class SvgAnimationTargetTests
{
    [Test]
    public void AutoResolutionSelectsCssPropertiesAndParentTargets()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='fill' to='red'/></rect></svg>");
        SvgNode rectangle = document.DocumentElement.Children[0];

        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(animation.Success, Is.True, Message(animation));
            Assert.That(animation.Kind, Is.EqualTo(SvgAnimationKind.Animate));
            Assert.That(animation.Target, Is.SameAs(rectangle));
            Assert.That(animation.TargetKind, Is.EqualTo(SvgAnimationTargetKind.CssProperty));
            Assert.That(animation.AttributeType, Is.EqualTo(SvgAnimationAttributeType.Auto));
            Assert.That(animation.Property.Name, Is.EqualTo("fill"));
            Assert.That(animation.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Paint));
            Assert.That(animation.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void ExplicitXmlSelectsPresentationAndOrdinaryAttributes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='box'><animate attributeName='fill' attributeType='XML'/></rect><animate href='#box' attributeName='pathLength' attributeType='XML'/></svg>";
        SvgDocument document = Load(source);
        SvgNode rectangle = document.DocumentElement.Children[0];

        SvgAnimation presentation = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[0]);
        SvgAnimation ordinary = new SvgAnimationTargetResolver().ResolveWithSource(document.DocumentElement.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(presentation.Success, Is.True, Message(presentation));
            Assert.That(presentation.TargetKind, Is.EqualTo(SvgAnimationTargetKind.PresentationProperty));
            Assert.That(presentation.Property.Name, Is.EqualTo("fill"));
            Assert.That(ordinary.Success, Is.True, Message(ordinary));
            Assert.That(ordinary.Target, Is.SameAs(rectangle));
            Assert.That(ordinary.TargetReference.Status, Is.EqualTo(SvgReferenceStatus.Resolved));
            Assert.That(ordinary.TargetKind, Is.EqualTo(SvgAnimationTargetKind.XmlAttribute));
            Assert.That(ordinary.LocalName, Is.EqualTo("pathLength"));
            Assert.That(ordinary.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Number));
        });
    }

    [Test]
    public void QualifiedXmlAttributeNamesUseInScopeNamespaces()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><image><animate attributeName='xlink:href' attributeType='XML'/></image></svg>";
        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSource(Load(source).DocumentElement.Children[0].Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(animation.Success, Is.True, Message(animation));
            Assert.That(animation.TargetKind, Is.EqualTo(SvgAnimationTargetKind.XmlAttribute));
            Assert.That(animation.AttributePrefix, Is.EqualTo("xlink"));
            Assert.That(animation.LocalName, Is.EqualTo("href"));
            Assert.That(animation.NamespaceUri, Is.EqualTo(SvgNamespaces.XLink));
            Assert.That(animation.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Discrete));
        });
    }

    [Test]
    public void TransformAndMotionAnimationsSelectDistinctTransformSlots()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g><animateTransform attributeName='transform' type='rotate'/><animateMotion path='M0 0L1 1'/></g></svg>";
        SvgNode group = Load(source).DocumentElement.Children[0];

        SvgAnimation transform = new SvgAnimationTargetResolver().ResolveWithSource(group.Children[0]);
        SvgAnimation motion = new SvgAnimationTargetResolver().ResolveWithSource(group.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(transform.Success, Is.True, Message(transform));
            Assert.That(transform.Kind, Is.EqualTo(SvgAnimationKind.AnimateTransform));
            Assert.That(transform.TargetKind, Is.EqualTo(SvgAnimationTargetKind.Transform));
            Assert.That(transform.AttributeType, Is.EqualTo(SvgAnimationAttributeType.Xml));
            Assert.That(transform.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Transform));
            Assert.That(motion.Success, Is.True, Message(motion));
            Assert.That(motion.Kind, Is.EqualTo(SvgAnimationKind.AnimateMotion));
            Assert.That(motion.TargetKind, Is.EqualTo(SvgAnimationTargetKind.Motion));
            Assert.That(motion.AttributeName, Is.Empty);
            Assert.That(motion.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Transform));
        });
    }

    [Test]
    public void CssResolutionRejectsUnknownAndNonAnimatableProperties()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='not-a-property' attributeType='CSS'/><animate attributeName='font' attributeType='CSS'/></rect></svg>";
        SvgNode rectangle = Load(source).DocumentElement.Children[0];

        SvgAnimation unknown = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[0]);
        SvgAnimation shorthand = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Success, Is.False);
            Assert.That(unknown.Error, Is.EqualTo(SvgAnimationTargetError.UnknownProperty));
            Assert.That(unknown.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidCss));
            Assert.That(shorthand.Success, Is.False);
            Assert.That(shorthand.Error, Is.EqualTo(SvgAnimationTargetError.NotAnimatable));
            Assert.That(shorthand.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedFeature));
        });
    }

    [Test]
    public void XmlResolutionRejectsNonAnimatableAndInapplicableAttributes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect><animate attributeName='id' attributeType='XML'/><animate attributeName='viewBox' attributeType='XML'/></rect></svg>";
        SvgNode rectangle = Load(source).DocumentElement.Children[0];

        SvgAnimation id = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[0]);
        SvgAnimation viewBox = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(id.Error, Is.EqualTo(SvgAnimationTargetError.NotAnimatable));
            Assert.That(viewBox.Error, Is.EqualTo(SvgAnimationTargetError.InapplicableAttribute));
            Assert.That(id.Success, Is.False);
            Assert.That(viewBox.Success, Is.False);
        });
    }

    [Test]
    public void InvalidNamesTypesTargetsAndReferencesProduceStableFailures()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><animate attributeName='fill'/><rect><animate attributeName='bad:name:again'/><animate attributeName='fill' attributeType='css'/><animate href='#missing' attributeName='fill'/></rect></svg>";
        SvgDocument document = Load(source);
        SvgNode rootAnimation = document.DocumentElement.Children[0];
        SvgNode rectangle = document.DocumentElement.Children[1];

        SvgAnimation rootTarget = new SvgAnimationTargetResolver().ResolveWithSource(rootAnimation);
        SvgAnimation badName = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[0]);
        SvgAnimation badType = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[1]);
        SvgAnimation missingReference = new SvgAnimationTargetResolver().ResolveWithSource(rectangle.Children[2]);

        Assert.Multiple(() =>
        {
            Assert.That(rootTarget.Success, Is.True, Message(rootTarget));
            Assert.That(rootTarget.Target, Is.SameAs(document.DocumentElement));
            Assert.That(badName.Error, Is.EqualTo(SvgAnimationTargetError.InvalidAttributeName));
            Assert.That(badType.Error, Is.EqualTo(SvgAnimationTargetError.InvalidAttributeType));
            Assert.That(missingReference.Error, Is.EqualTo(SvgAnimationTargetError.MissingTarget));
            Assert.That(missingReference.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidReference), Is.True);
        });
    }

    [Test]
    public void ExplicitHrefWinsOverTheParentAndAttributeNamesAreBounded()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><circle id='target'/><g><animate href='#target' attributeName='r'/></g></svg>";
        SvgDocument document = Load(source);
        SvgNode animationNode = document.DocumentElement.Children[1].Children[0];
        SvgAnimationTargetOptions options = new SvgAnimationTargetOptions();
        options.MaxAttributeNameCharacters = 3;

        SvgAnimation resolved = new SvgAnimationTargetResolver().ResolveWithSourceAndOptions(animationNode, options);
        options.MaxAttributeNameCharacters = 0;
        SvgAnimation invalidOptions = new SvgAnimationTargetResolver().ResolveWithSourceAndOptions(animationNode, options);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.True, Message(resolved));
            Assert.That(resolved.Target, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(resolved.Property.Name, Is.EqualTo("r"));
            Assert.That(resolved.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Length));
            Assert.That(invalidOptions.Success, Is.False);
            Assert.That(invalidOptions.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
        });
    }

    [Test]
    public void NoNamespaceSvgTreesRemainValidAnimationTargets()
    {
        SvgDocument document = Load("<svg><rect><set attributeName='opacity' to='0'/></rect></svg>");

        SvgAnimation animation = new SvgAnimationTargetResolver().ResolveWithSource(document.DocumentElement.Children[0].Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(animation.Success, Is.True, Message(animation));
            Assert.That(animation.Kind, Is.EqualTo(SvgAnimationKind.Set));
            Assert.That(animation.TargetKind, Is.EqualTo(SvgAnimationTargetKind.CssProperty));
            Assert.That(animation.AnimationType, Is.EqualTo(SvgPropertyAnimationType.Number));
            Assert.That(animation.TargetVersion, Is.EqualTo(animation.Target.Version));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Message(SvgAnimation animation)
    {
        return string.Join(" | ", animation.Diagnostics.Select(item => item.Code + ": " + item.Message));
    }
}
