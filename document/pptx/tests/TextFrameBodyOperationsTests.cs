using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TextFrameBodyOperationsTests
{
    [Test]
    public void BodyOperations_RoundTripMarginsAnchoringAutofitWrappingOverflowAndOrientation()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 400L, 200L));
        shape.SetText("Typed body settings");
        IPptxTextFrameOperations operations = new PptxTextFrameOperations();
        PptxTextFrame frame = shape.GetTextFrame();

        operations.SetMargins(frame, new PptxMargins(100L, 200L, 300L, 400L));
        operations.SetAnchoring(frame, "center", true);
        operations.SetAutofit(frame, "normal", 80000, 12000);
        operations.SetWrapping(frame, "none");
        operations.SetOverflow(frame, "clip", "ellipsis");
        operations.SetVerticalText(frame, "vertical270");
        operations.SetOrientation(frame, 5400000, true);

        PptxPresentation reopened = Reopen(presentation);
        PptxTextFrameBody body = operations.GetBody(reopened.GetSlide(0).GetShapeTree().Get(0).GetTextFrame());

        Assert.That(body.Margins.Left, Is.EqualTo(100L));
        Assert.That(body.Margins.Top, Is.EqualTo(200L));
        Assert.That(body.Margins.Right, Is.EqualTo(300L));
        Assert.That(body.Margins.Bottom, Is.EqualTo(400L));
        Assert.That(body.VerticalAlignment, Is.EqualTo("ctr"));
        Assert.That(body.CenteredAnchor, Is.True);
        Assert.That(body.Autofit, Is.EqualTo(PptxTextFrameBody.AutofitNormal));
        Assert.That(body.AutofitFontScale, Is.EqualTo(80000));
        Assert.That(body.AutofitLineSpacingReduction, Is.EqualTo(12000));
        Assert.That(body.Wrapping, Is.EqualTo("none"));
        Assert.That(body.HorizontalOverflow, Is.EqualTo("clip"));
        Assert.That(body.VerticalOverflow, Is.EqualTo("ellipsis"));
        Assert.That(body.VerticalText, Is.EqualTo("vert270"));
        Assert.That(body.Rotation, Is.EqualTo(5400000));
        Assert.That(body.Upright, Is.True);
    }

    [Test]
    public void AutofitOperations_ReplaceOnlyAutofitAndPreserveFollowingBodyContent()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 400L, 200L));
        shape.SetText("Preserve extensions");
        PptxTextFrame frame = shape.GetTextFrame();
        XmlElement properties = frame.EnsureBodyProperties();
        XmlDocument document = PptxTextFrame.GetOwnerDocument(properties);
        XmlElement extensions = document.CreateElement("a", "extLst", PptxNamespaces.Drawing);
        XmlElement extension = document.CreateElement("a", "ext", PptxNamespaces.Drawing);
        extension.SetAttribute("uri", "future-body-setting");
        extensions.AppendChild(extension);
        properties.AppendChild(extensions);
        PptxTextFrameOperations operations = new PptxTextFrameOperations();

        operations.SetAutofit(frame, "normal", 75000, 5000);
        operations.SetAutofit(frame, "grow-shape", 100000, 0);

        PptxTextFrameBody shapeFit = operations.GetBody(frame);
        Assert.That(shapeFit.Autofit, Is.EqualTo(PptxTextFrameBody.AutofitShape));
        Assert.That(frame.BodyPropertiesXml, Does.Contain("future-body-setting"));
        Assert.That(frame.BodyPropertiesXml, Does.Contain("spAutoFit"));
        Assert.That(frame.BodyPropertiesXml, Does.Not.Contain("normAutofit"));
        Assert.That(frame.BodyPropertiesXml.IndexOf("spAutoFit"), Is.LessThan(frame.BodyPropertiesXml.IndexOf("extLst")));

        operations.SetAutofit(frame, "inherited", 100000, 0);
        Assert.That(operations.GetBody(frame).Autofit, Is.Empty);
        Assert.That(frame.BodyPropertiesXml, Does.Contain("future-body-setting"));
        Assert.That(frame.BodyPropertiesXml, Does.Not.Contain("spAutoFit"));
    }

    [Test]
    public void GetBody_ReturnsDrawingDefaultsWithoutCreatingMissingTextBody()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddConnector(PptxConnector.Straight, new PptxPoint(0L, 0L), new PptxPoint(400L, 200L));
        PptxTextFrame frame = shape.GetTextFrame();
        PptxTextFrameBody body = new PptxTextFrameOperations().GetBody(frame);

        Assert.That(frame.Present, Is.False);
        Assert.That(body.Margins.Left, Is.EqualTo(91440L));
        Assert.That(body.Margins.Top, Is.EqualTo(45720L));
        Assert.That(body.VerticalAlignment, Is.EqualTo("t"));
        Assert.That(body.Wrapping, Is.EqualTo("square"));
        Assert.That(body.HorizontalOverflow, Is.EqualTo("overflow"));
        Assert.That(body.VerticalOverflow, Is.EqualTo("overflow"));
        Assert.That(body.VerticalText, Is.EqualTo("horz"));
        Assert.That(body.Autofit, Is.Empty);
    }

    [Test]
    public void BodyOperations_RejectInvalidTypedValuesBeforeChangingTheFrame()
    {
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddConnector(PptxConnector.Straight, new PptxPoint(0L, 0L), new PptxPoint(400L, 200L));
        PptxTextFrame frame = shape.GetTextFrame();
        PptxTextFrameOperations operations = new PptxTextFrameOperations();

        Assert.Throws<System.ArgumentNullException>(() => operations.SetMargins(frame, null!));
        Assert.Throws<System.ArgumentException>(() => operations.SetAnchoring(frame, "sideways", false));
        Assert.Throws<System.ArgumentException>(() => operations.SetAutofit(frame, "elastic", 100000, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetAutofit(frame, "normal", 100001, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => operations.SetAutofit(frame, "normal", 100000, -1));
        Assert.Throws<System.ArgumentException>(() => operations.SetWrapping(frame, "tight"));
        Assert.Throws<System.ArgumentException>(() => operations.SetOverflow(frame, "ellipsis", "overflow"));
        Assert.Throws<System.ArgumentException>(() => operations.SetOverflow(frame, "overflow", "scroll"));
        Assert.Throws<System.ArgumentException>(() => operations.SetVerticalText(frame, "diagonal"));
        Assert.That(frame.Present, Is.False);
    }

    static PptxPresentation Reopen(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
