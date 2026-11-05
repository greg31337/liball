using System.Text;

namespace odf.Tests;

public class PresentationDocumentModelTests
{
    [Test]
    public void MaterializesSlidesAssignmentsNotesHandoutAndDeclarations()
    {
        OdfDocument document = Read(PresentationXml());
        OdfSlide first = document.Slides[0];
        OdfSlide second = document.Slides[1];
        OdfPresentationNotes notes = first.Notes
            ?? new OdfPresentationNotes();
        OdfPresentationHandout handout = document.PresentationHandout
            ?? new OdfPresentationHandout();
        OdfMasterPage master = document.MasterPages.Single();
        OdfStyleResource layout = document.StyleResources.Single(item =>
            item.Kind == OdfStyleResourceKind.PresentationLayout);
        OdfPresentationDeclaration header =
            document.PresentationDeclarations.Single(item =>
                item.Kind == OdfPresentationDeclarationKind.Header);

        Assert.Multiple(() =>
        {
            Assert.That(document.Slides, Has.Length.EqualTo(2));
            Assert.That(first.Index, Is.Zero);
            Assert.That(first.Name, Is.EqualTo("Intro"));
            Assert.That(first.Id, Is.EqualTo("slide-id"));
            Assert.That(first.XmlId, Is.EqualTo("slide-xml"));
            Assert.That(first.MasterPage, Is.SameAs(master));
            Assert.That(first.PresentationPageLayout, Is.SameAs(layout));
            Assert.That(first.Header, Is.SameAs(header));
            Assert.That(first.HeaderName, Is.EqualTo("Header1"));
            Assert.That(first.FooterName, Is.EqualTo("Footer1"));
            Assert.That(first.DateTimeName, Is.EqualTo("Date1"));
            Assert.That(first.IsVisible, Is.False);
            Assert.That(first.IsVisibilityValid, Is.True);
            Assert.That(first.IsNavigationOrderValid, Is.True);
            Assert.That(
                first.NavigationObjects.Select(item => item.Id),
                Is.EqualTo(new[] { "shape2", "shape1" }));
            Assert.That(first.DrawingObjects, Has.Length.EqualTo(2));
            Assert.That(first.Placeholders.Select(item => item.ObjectName),
                Does.Contain("title"));
            Assert.That(first.Placeholders.Single(item =>
                item.ObjectName == "title").IsPlaceholder, Is.True);
            Assert.That(first.Placeholders.Single(item =>
                item.ObjectName == "title").LayoutPlaceholder,
                Is.SameAs(layout.PresentationPlaceholders.Single()));
            Assert.That(notes.StyleName, Is.EqualTo("NotesStyle"));
            Assert.That(notes.PageNumber, Is.EqualTo("1"));
            Assert.That(notes.PresentationPageLayout, Is.SameAs(layout));
            Assert.That(notes.TextBlocks.Single().Text,
                Is.EqualTo("Speaker notes"));
            Assert.That(notes.Placeholders.Single().ObjectName,
                Is.EqualTo("notes"));
            Assert.That(notes.DrawingObjects, Has.Length.EqualTo(2));
            Assert.That(second.IsVisible, Is.True);
            Assert.That(second.IsNavigationOrderValid, Is.False);
            Assert.That(layout.PresentationPlaceholders.Single().ObjectName,
                Is.EqualTo("title"));
            Assert.That(master.PresentationPageLayout, Is.SameAs(layout));
            Assert.That(master.HeaderName, Is.EqualTo("Header1"));
            Assert.That(master.PresentationHeader, Is.SameAs(header));
            Assert.That(master.PresentationPlaceholders.Single().ObjectName,
                Is.EqualTo("title"));
            Assert.That(handout.PresentationPageLayout, Is.SameAs(layout));
            Assert.That(handout.HeaderName, Is.EqualTo("Header1"));
            Assert.That(handout.Header, Is.SameAs(header));
            Assert.That(handout.Placeholders.Single().ObjectName,
                Is.EqualTo("handout"));
            Assert.That(document.PresentationDeclarations, Has.Length.EqualTo(3));
            Assert.That(document.PresentationDeclarations.Single(item =>
                item.Kind == OdfPresentationDeclarationKind.Header).Text,
                Is.EqualTo("Quarterly Review"));
            Assert.That(document.PresentationDeclarations.Single(item =>
                item.Kind == OdfPresentationDeclarationKind.DateTime)
                .DataStyleName, Is.EqualTo("DateStyle"));
        });
    }

    [Test]
    public void WritesSlidePlaceholderNotesMasterHandoutAndDeclarationChanges()
    {
        OdfDocument document = Read(PresentationXml());
        OdfSlide first = document.Slides[0];
        first.Name = "Changed Intro";
        first.Visibility = "visible";
        first.HeaderName = "HeaderChanged";
        first.NavigationOrder = "shape1 shape2";
        OdfPresentationPlaceholder title = first.Placeholders.Single(item =>
            item.ObjectName == "title");
        title.UserTransformedValue = "true";
        title.X = "2cm";
        OdfPresentationNotes notes = first.Notes
            ?? new OdfPresentationNotes();
        notes.PageNumber = "7";
        notes.StyleName = "ChangedNotes";
        OdfStyleResource layout = document.StyleResources.Single(item =>
            item.Kind == OdfStyleResourceKind.PresentationLayout);
        layout.PresentationPlaceholders[0].Width = "20cm";
        OdfMasterPage master = document.MasterPages.Single();
        master.FooterName = "FooterChanged";
        OdfPresentationHandout handout = document.PresentationHandout
            ?? new OdfPresentationHandout();
        handout.DateTimeName = "DateChanged";
        OdfPresentationDeclaration header =
            document.PresentationDeclarations.Single(item =>
                item.Kind == OdfPresentationDeclarationKind.Header);
        header.Name = "HeaderChanged";
        header.Text = "Changed Header";

        OdfDocument reread = ReadBytes(Write(document));
        OdfSlide changed = reread.Slides[0];
        OdfPresentationPlaceholder changedTitle =
            changed.Placeholders.Single(item => item.ObjectName == "title");

        Assert.Multiple(() =>
        {
            Assert.That(changed.Name, Is.EqualTo("Changed Intro"));
            Assert.That(changed.IsVisible, Is.True);
            Assert.That(changed.HeaderName, Is.EqualTo("HeaderChanged"));
            Assert.That(changed.NavigationObjects.Select(item => item.Id),
                Is.EqualTo(new[] { "shape1", "shape2" }));
            Assert.That(changedTitle.IsUserTransformed, Is.True);
            Assert.That(changedTitle.X, Is.EqualTo("2cm"));
            Assert.That(changed.Notes?.PageNumber, Is.EqualTo("7"));
            Assert.That(changed.Notes?.StyleName, Is.EqualTo("ChangedNotes"));
            Assert.That(reread.StyleResources.Single(item =>
                item.Kind == OdfStyleResourceKind.PresentationLayout)
                .PresentationPlaceholders[0].Width, Is.EqualTo("20cm"));
            Assert.That(reread.MasterPages.Single().FooterName,
                Is.EqualTo("FooterChanged"));
            Assert.That(reread.PresentationHandout?.DateTimeName,
                Is.EqualTo("DateChanged"));
            Assert.That(reread.PresentationDeclarations.Single(item =>
                item.Kind == OdfPresentationDeclarationKind.Header).Name,
                Is.EqualTo("HeaderChanged"));
            Assert.That(reread.PresentationDeclarations.Single(item =>
                item.Kind == OdfPresentationDeclarationKind.Header).Text,
                Is.EqualTo("Changed Header"));
        });
    }

    static OdfDocument Read(string xml)
    {
        return ReadBytes(Encoding.UTF8.GetBytes(xml));
    }

    static OdfDocument ReadBytes(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "slides.fodp",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string PresentationXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + Namespaces()
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:styles>"
            + "<style:presentation-page-layout style:name='TitleLayout'>"
            + "<presentation:placeholder presentation:object='title' "
            + "svg:x='1cm' svg:y='1cm' svg:width='18cm' svg:height='3cm'/>"
            + "</style:presentation-page-layout>"
            + "</office:styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='MainMaster' "
            + "presentation:presentation-page-layout-name='TitleLayout' "
            + "presentation:use-header-name='Header1' "
            + "presentation:use-footer-name='Footer1' "
            + "presentation:use-date-time-name='Date1'>"
            + "<draw:frame draw:name='Master Title' presentation:class='title' "
            + "presentation:placeholder='true'/>"
            + "</style:master-page>"
            + "<presentation:handout-master draw:style-name='HandoutStyle' "
            + "presentation:presentation-page-layout-name='TitleLayout' "
            + "presentation:use-header-name='Header1' "
            + "presentation:use-footer-name='Footer1' "
            + "presentation:use-date-time-name='Date1'>"
            + "<presentation:placeholder presentation:object='handout' "
            + "svg:x='1cm' svg:y='2cm' svg:width='20cm' svg:height='15cm'/>"
            + "</presentation:handout-master>"
            + "</office:master-styles>"
            + "<office:body><office:presentation>"
            + "<presentation:header-decl presentation:name='Header1'>"
            + "Quarterly Review</presentation:header-decl>"
            + "<presentation:footer-decl presentation:name='Footer1'>"
            + "Confidential</presentation:footer-decl>"
            + "<presentation:date-time-decl presentation:name='Date1' "
            + "presentation:source='current-date' style:data-style-name='DateStyle'/>"
            + "<draw:page draw:name='Intro' draw:id='slide-id' xml:id='slide-xml' "
            + "draw:master-page-name='MainMaster' "
            + "presentation:presentation-page-layout-name='TitleLayout' "
            + "presentation:use-header-name='Header1' "
            + "presentation:use-footer-name='Footer1' "
            + "presentation:use-date-time-name='Date1' "
            + "presentation:visibility='hidden' "
            + "draw:nav-order='shape2 shape1'>"
            + "<draw:rect draw:name='Title' draw:id='shape1' "
            + "presentation:class='title' presentation:placeholder='true' "
            + "presentation:user-transformed='false' svg:x='1cm'/>"
            + "<draw:frame draw:name='Outline' draw:id='shape2' "
            + "presentation:class='outline'/>"
            + "<presentation:notes draw:style-name='NotesStyle' "
            + "presentation:presentation-page-layout-name='TitleLayout'>"
            + "<draw:page-thumbnail draw:page-number='1'/>"
            + "<draw:frame draw:name='Notes Text' presentation:class='notes' "
            + "presentation:placeholder='true'><draw:text-box>"
            + "<text:p>Speaker notes</text:p></draw:text-box></draw:frame>"
            + "</presentation:notes>"
            + "</draw:page>"
            + "<draw:page draw:name='Second' draw:nav-order='missing'>"
            + "<draw:rect draw:name='Only Shape' draw:id='only'/></draw:page>"
            + "</office:presentation></office:body>"
            + "</office:document>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:xml='" + OdfNamespaces.Xml + "'";
    }
}
