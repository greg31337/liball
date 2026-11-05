using System.Text;

namespace odf.Tests;

public class PresentationEditorTests
{
    [Test]
    public void EditsSlidesNotesAndPresentationLayouts()
    {
        OdfDocument document = Read(PresentationXml("Source"));
        IOdfPresentationEditor editor = new OdfPresentationEditor();
        OdfPresentationPlaceholder placeholder =
            new OdfPresentationPlaceholder
            {
                ObjectName = "title",
                X = "1cm",
                Y = "2cm",
                Width = "10cm",
                Height = "3cm"
            };
        OdfStyleResource layout = editor.CreatePresentationLayout(
            document,
            "Title Layout",
            [placeholder]);
        OdfSlide slide = editor.AssignSlideLayout(
            document,
            document.Slides[0],
            layout);
        OdfPresentationNotes notes = editor.CreateSlideNotes(
            document,
            slide);
        notes = editor.AssignNotesLayout(document, notes, layout);
        notes.PageNumber = "1";
        editor.ApplyPresentationChanges(document);
        OdfSlide inserted = editor.InsertSlide(document, 1, "Inserted");
        inserted = editor.MoveSlide(document, inserted, 0);
        OdfSlide duplicate = editor.DuplicateSlide(document, inserted);
        editor.DeleteSlide(document, duplicate);
        OdfSlide appended = editor.CreateSlide(document, "Appended");
        editor.DeleteSlide(document, appended);
        OdfSlide copied = editor.CopySlide(
            document,
            document.Slides[1],
            document.Slides.Length);
        string copiedName = copied.Name;
        editor.DeleteSlide(document, copied);

        OdfDocument reread = ReadBytes(Write(document));
        OdfStyleResource changedLayout = editor.GetPresentationLayouts(
            reread).Single(item => item.Name == "Title Layout");
        OdfSlide changedSlide = reread.Slides.Single(item =>
            item.Name == "Source One");

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetSlides(reread).Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "Inserted",
                    "Source One",
                    "Source Two"
                }));
            Assert.That(copiedName, Is.EqualTo("Source One Copy 2"));
            Assert.That(changedLayout.PresentationPlaceholders,
                Has.Length.EqualTo(1));
            Assert.That(changedLayout.PresentationPlaceholders[0].ObjectName,
                Is.EqualTo("title"));
            Assert.That(changedLayout.PresentationPlaceholders[0].X,
                Is.EqualTo("1cm"));
            Assert.That(changedSlide.PresentationPageLayout,
                Is.SameAs(changedLayout));
            Assert.That(changedSlide.Notes, Is.Not.Null);
            Assert.That(changedSlide.Notes?.PresentationPageLayout,
                Is.SameAs(changedLayout));
            Assert.That(changedSlide.Notes?.PageNumber, Is.EqualTo("1"));
        });

        editor.RemoveSlideNotes(reread, changedSlide);
        changedLayout = editor.GetPresentationLayouts(reread)
            .Single(item => item.Name == "Title Layout");
        editor.RemovePresentationLayout(reread, changedLayout);
        OdfDocument removed = ReadBytes(Write(reread));
        Assert.Multiple(() =>
        {
            Assert.That(removed.Slides.Single(item =>
                item.Name == "Source One").Notes, Is.Null);
            Assert.That(editor.GetPresentationLayouts(removed).Any(item =>
                item.Name == "Title Layout"), Is.False);
            Assert.That(removed.Slides.Single(item =>
                item.Name == "Source One")
                .PresentationPageLayoutName, Is.Empty);
        });
    }

    [Test]
    public void EditsShowsSettingsAndIndependentTransitions()
    {
        OdfDocument document = Read(PresentationXml("Deck"));
        IOdfPresentationEditor editor = new OdfPresentationEditor();
        OdfPresentationShow show = editor.CreatePresentationShow(
            document,
            "Executive",
            [document.Slides[1], document.Slides[0]]);
        show = editor.SetPresentationShowSlides(
            document,
            show,
            [document.Slides[0], document.Slides[1]]);
        OdfPresentationSettings settings = new OdfPresentationSettings();
        settings.StartSlide = document.Slides[1];
        settings.SelectedShow = show;
        settings.FullScreen = true;
        settings.AnimationsEnabled = true;
        settings.TransitionOnClickEnabled = false;
        editor.SetPresentationSettings(document, settings);
        OdfSlideTransition transition = new OdfSlideTransition();
        transition.Kind = OdfTransitionKind.Fade;
        transition.Duration = "PT3S";
        transition.Speed = "fast";
        transition.SoundHref = "Sounds/cue.wav";
        transition.SoundType = "simple";
        transition.SoundPlayFull = true;
        OdfSlideTransition assigned = editor.SetSlideTransition(
            document,
            document.Slides[0],
            transition);

        OdfDocument reread = ReadBytes(Write(document));
        OdfSlide first = reread.Slides[0];
        OdfSlide second = reread.Slides[1];

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetPresentationShows(reread).Single().Name,
                Is.EqualTo("Executive"));
            Assert.That(reread.PresentationShows[0].Slides,
                Is.EqualTo(new[] { first, second }));
            Assert.That(reread.PresentationSettings.StartSlide,
                Is.SameAs(second));
            Assert.That(reread.PresentationSettings.SelectedShow,
                Is.SameAs(reread.PresentationShows[0]));
            Assert.That(reread.PresentationSettings.FullScreen, Is.True);
            Assert.That(reread.PresentationSettings.AnimationsEnabled,
                Is.True);
            Assert.That(first.Transition?.Kind,
                Is.EqualTo(OdfTransitionKind.Fade));
            Assert.That(first.Transition?.Duration, Is.EqualTo("PT3S"));
            Assert.That(first.Transition?.SoundHref,
                Is.EqualTo("Sounds/cue.wav"));
            Assert.That(first.Transition?.SoundPlayFull, Is.True);
            Assert.That(second.Transition, Is.Null);
            Assert.That(assigned.Style?.Name, Is.Not.EqualTo(second.StyleName));
        });

        editor.RemoveSlideTransition(reread, first);
        editor.RemovePresentationShow(
            reread,
            reread.PresentationShows.Single());
        OdfDocument removed = ReadBytes(Write(reread));
        Assert.Multiple(() =>
        {
            Assert.That(removed.Slides[0].Transition, Is.Null);
            Assert.That(removed.PresentationShows, Is.Empty);
            Assert.That(removed.PresentationSettings.SelectedShow, Is.Null);
        });
    }

    [Test]
    public void CreatesEditsAndRemovesAnimationTrees()
    {
        OdfDocument document = Read(PresentationXml("Animated"));
        IOdfPresentationEditor editor = new OdfPresentationEditor();
        OdfAnimation sequence = editor.CreateAnimation(
            document,
            document.Slides[0],
            null,
            OdfAnimationKind.Sequence);
        OdfAnimation parallel = editor.CreateAnimation(
            document,
            document.Slides[0],
            sequence,
            OdfAnimationKind.Parallel);
        OdfAnimation animate = editor.CreateAnimation(
            document,
            document.Slides[0],
            parallel,
            OdfAnimationKind.Animate);
        animate.XmlId = "animation-one";
        animate.TargetElement = "shape-one";
        animate.AttributeName = "svg:x";
        animate.Begin = "1.5s";
        animate.Duration = "2s";
        animate.Values = "0;5;10";
        animate.Fill = "freeze";
        editor.ApplyPresentationChanges(document);

        OdfAnimation changed = editor.GetAnimations(document).Single(item =>
            item.Kind == OdfAnimationKind.Animate);
        Assert.Multiple(() =>
        {
            Assert.That(document.Slides[0].Animations,
                Has.Length.EqualTo(1));
            Assert.That(changed.Parent?.Kind,
                Is.EqualTo(OdfAnimationKind.Parallel));
            Assert.That(changed.TargetObject?.Name,
                Is.EqualTo("Shape One"));
            Assert.That(changed.IsTargetValid, Is.True);
            Assert.That(changed.BeginTimes.Single().OffsetSeconds,
                Is.EqualTo(1.5));
            Assert.That(changed.DurationSeconds, Is.EqualTo(2));
            Assert.That(changed.EffectiveFill, Is.EqualTo("freeze"));
        });

        OdfDocument reread = ReadBytes(Write(document));
        OdfAnimation roundTrip = reread.Animations.Single(item =>
            item.Kind == OdfAnimationKind.Animate);
        Assert.That(roundTrip.Values, Is.EqualTo("0;5;10"));

        OdfAnimation subtree = reread.Animations.Single(item =>
            item.Kind == OdfAnimationKind.Parallel);
        editor.RemoveAnimation(reread, subtree);
        Assert.That(editor.GetAnimations(reread).Select(item => item.Kind),
            Is.EqualTo(new[] { OdfAnimationKind.Sequence }));
    }

    [Test]
    public void ImportsAndMergesSlidesThroughPublicEditor()
    {
        OdfDocument source = Read(PresentationXml("Source"));
        OdfDocument target = Read(PresentationXml("Target"));
        IOdfPresentationEditor editor = new OdfPresentationEditor();

        OdfSlide imported = editor.ImportSlide(
            source,
            target,
            source.Slides[0],
            1);
        OdfSlide[] merged = editor.MergePresentation(
            source,
            target,
            target.Slides.Length);

        Assert.Multiple(() =>
        {
            Assert.That(imported.Name, Is.EqualTo("Source One"));
            Assert.That(merged, Has.Length.EqualTo(2));
            Assert.That(target.Slides, Has.Length.EqualTo(5));
            Assert.That(target.Slides.Select(item => item.Name), Is.Unique);
        });
    }

    [Test]
    public void ImportsSlideWithMasterLayoutStyleAndNotesDependencies()
    {
        OdfDocument source = Read(DependencyPresentationXml(true));
        OdfDocument target = Read(DependencyPresentationXml(false));
        IOdfPresentationEditor editor = new OdfPresentationEditor();

        OdfSlide imported = editor.ImportSlide(
            source,
            target,
            source.Slides.Single(),
            1);
        OdfDrawingObject shape = imported.DrawingObjects.Single(item =>
            item.Name == "Source Shape");

        Assert.Multiple(() =>
        {
            Assert.That(imported.Name, Is.EqualTo("First Copy 2"));
            Assert.That(imported.MasterPageName,
                Is.EqualTo("Master Copy 2"));
            Assert.That(imported.MasterPage?.PageLayoutName,
                Is.EqualTo("PageLayout Copy 2"));
            Assert.That(imported.PresentationPageLayoutName,
                Is.EqualTo("Layout Copy 2"));
            Assert.That(imported.Header?.Text, Is.EqualTo("Source Header"));
            Assert.That(imported.Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Source notes"));
            Assert.That(shape.StyleName, Is.EqualTo("ShapeStyle Copy 2"));
            Assert.That(target.Styles.Single(item =>
                item.Name == shape.StyleName).ParentStyle?.Name,
                Is.EqualTo("Parent Copy 2"));
        });
    }

    [Test]
    public void ReadsMasterHandoutEventTransitionAndAnimationContract()
    {
        OdfDocument document = Read(PresentationContractXml());
        IOdfPresentationEditor editor = new OdfPresentationEditor();

        OdfSlide[] slides = editor.GetSlides(document);
        OdfStyleResource[] layouts =
            editor.GetPresentationLayouts(document);
        OdfPresentationShow[] shows =
            editor.GetPresentationShows(document);
        OdfAnimation[] animations = editor.GetAnimations(document);
        OdfSlide first = slides[0];
        OdfDrawingEvent drawingEvent = first.DrawingObjects.Single(item =>
            item.Name == "Action").Events.Single();
        OdfAnimation iterate = animations.Single(item =>
            item.Kind == OdfAnimationKind.Iterate);

        Assert.Multiple(() =>
        {
            Assert.That(slides, Is.Not.SameAs(document.Slides));
            Assert.That(layouts, Is.Not.SameAs(document.StyleResources));
            Assert.That(shows, Is.Not.SameAs(document.PresentationShows));
            Assert.That(animations, Is.Not.SameAs(document.Animations));
            Assert.That(first.MasterPage?.Name, Is.EqualTo("Master"));
            Assert.That(first.PresentationPageLayout?.Name,
                Is.EqualTo("Layout"));
            Assert.That(first.Notes?.PresentationPageLayout?.Name,
                Is.EqualTo("Layout"));
            Assert.That(first.Notes?.TextBlocks.Single().Text,
                Is.EqualTo("Speaker notes"));
            Assert.That(document.PresentationHandout
                ?.PresentationPageLayout?.Name, Is.EqualTo("Layout"));
            Assert.That(document.PresentationHandout?.Header?.Text,
                Is.EqualTo("Contract Header"));
            Assert.That(shows.Single().Slides,
                Is.EqualTo(new[] { slides[1], slides[0] }));
            Assert.That(first.Transition?.Kind,
                Is.EqualTo(OdfTransitionKind.BarWipe));
            Assert.That(first.Transition?.Duration, Is.EqualTo("PT2S"));
            Assert.That(drawingEvent.Kind,
                Is.EqualTo(OdfDrawingEventKind.Presentation));
            Assert.That(drawingEvent.Action, Is.EqualTo("show"));
            Assert.That(drawingEvent.Href, Is.EqualTo("#Second"));
            Assert.That(
                animations.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfAnimationKind.Sequence,
                    OdfAnimationKind.Parallel,
                    OdfAnimationKind.Iterate,
                    OdfAnimationKind.Animate,
                    OdfAnimationKind.Set,
                    OdfAnimationKind.AnimateColor,
                    OdfAnimationKind.AnimateMotion,
                    OdfAnimationKind.AnimateTransform,
                    OdfAnimationKind.TransitionFilter,
                    OdfAnimationKind.Audio,
                    OdfAnimationKind.Command
                }));
            Assert.That(iterate.TargetObject?.Name, Is.EqualTo("Action"));
            Assert.That(iterate.BeginTimes.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    OdfAnimationTimingKind.Syncbase,
                    OdfAnimationTimingKind.Event
                }));
            Assert.That(iterate.BeginTimes[0].ReferencedAnimation,
                Is.SameAs(animations[0]));
            Assert.That(iterate.BeginTimes[1].ReferencedDrawingObject,
                Is.SameAs(iterate.TargetObject));
            Assert.That(iterate.DurationSeconds, Is.EqualTo(2));
        });

        document.MasterPages.Single().FooterName = "Changed Footer";
        document.PresentationHandout!.DateTimeName = "Changed Date";
        first.Notes!.PageNumber = "7";
        drawingEvent.Action = "next-page";
        iterate.Duration = "3s";
        editor.ApplyPresentationChanges(document);
        OdfDocument reread = ReadBytes(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(reread.MasterPages.Single().FooterName,
                Is.EqualTo("Changed Footer"));
            Assert.That(reread.PresentationHandout?.DateTimeName,
                Is.EqualTo("Changed Date"));
            Assert.That(reread.Slides[0].Notes?.PageNumber, Is.EqualTo("7"));
            Assert.That(reread.Slides[0].DrawingObjects.Single(item =>
                item.Name == "Action").Events.Single().Action,
                Is.EqualTo("next-page"));
            Assert.That(reread.Animations.Single(item =>
                item.Kind == OdfAnimationKind.Iterate).DurationSeconds,
                Is.EqualTo(3));
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
            "editor.fodp",
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

    static string PresentationXml(string prefix)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " office:version='1.3' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:body><office:presentation>"
            + "<draw:page draw:name='" + prefix
            + " One' draw:id='page-one'>"
            + "<draw:rect draw:name='Shape One' draw:id='shape-one'/>"
            + "</draw:page>"
            + "<draw:page draw:name='" + prefix
            + " Two' draw:id='page-two'>"
            + "<draw:ellipse draw:name='Shape Two' draw:id='shape-two'/>"
            + "</draw:page>"
            + "</office:presentation></office:body>"
            + "</office:document>";
    }

    static string DependencyPresentationXml(bool source)
    {
        string marker = source ? "source" : "target";
        string shapeName = source ? "Source Shape" : "Target Shape";
        string notes = source ? "Source notes" : "Target notes";
        string header = source ? "Source Header" : "Target Header";
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:styles>"
            + "<style:style style:name='Parent' style:family='graphic'/>"
            + "<style:style style:name='ShapeStyle' style:family='graphic' "
            + "style:parent-style-name='Parent'/>"
            + "<style:style style:name='PageStyle' "
            + "style:family='drawing-page'/>"
            + "<style:presentation-page-layout style:name='Layout'>"
            + "<presentation:placeholder presentation:object='title'/>"
            + "</style:presentation-page-layout>"
            + "</office:styles>"
            + "<office:automatic-styles>"
            + "<style:page-layout style:name='PageLayout'>"
            + "<style:page-layout-properties fo:page-width='28cm'/>"
            + "</style:page-layout></office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='Master' "
            + "style:page-layout-name='PageLayout' "
            + "draw:style-name='PageStyle' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:rect draw:name='Master " + marker + "' "
            + "draw:style-name='ShapeStyle'/></style:master-page>"
            + "</office:master-styles>"
            + "<office:body><office:presentation>"
            + "<presentation:header-decl presentation:name='Header'>"
            + header + "</presentation:header-decl>"
            + "<draw:page draw:name='First' draw:id='page-" + marker + "' "
            + "draw:master-page-name='Master' draw:style-name='PageStyle' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:rect draw:name='" + shapeName + "' draw:id='shape-"
            + marker + "' draw:style-name='ShapeStyle'/>"
            + "<presentation:notes><draw:frame draw:name='Notes'>"
            + "<draw:text-box><text:p>" + notes + "</text:p>"
            + "</draw:text-box></draw:frame></presentation:notes>"
            + "</draw:page></office:presentation></office:body>"
            + "</office:document>";
    }

    static string PresentationContractXml()
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document" + Namespaces()
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:script='" + OdfNamespaces.Script + "'"
            + " office:version='1.3' office:mimetype='"
            + OdfMediaTypes.Presentation + "'>"
            + "<office:styles>"
            + "<style:presentation-page-layout style:name='Layout'>"
            + "<presentation:placeholder presentation:object='title'/>"
            + "</style:presentation-page-layout></office:styles>"
            + "<office:automatic-styles>"
            + "<style:style style:name='SlideStyle' "
            + "style:family='drawing-page'>"
            + "<style:drawing-page-properties presentation:duration='PT2S' "
            + "smil:type='barWipe' smil:subtype='leftToRight'/>"
            + "</style:style></office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='Master' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:frame draw:name='Master Title' "
            + "presentation:class='title'/></style:master-page>"
            + "<presentation:handout-master "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<presentation:placeholder presentation:object='handout'/>"
            + "</presentation:handout-master></office:master-styles>"
            + "<office:body><office:presentation>"
            + "<presentation:header-decl presentation:name='Header'>"
            + "Contract Header</presentation:header-decl>"
            + "<draw:page draw:name='First' draw:style-name='SlideStyle' "
            + "draw:master-page-name='Master' "
            + "presentation:presentation-page-layout-name='Layout' "
            + "presentation:use-header-name='Header'>"
            + "<draw:rect draw:name='Action' draw:id='shape'>"
            + "<office:event-listeners><presentation:event-listener "
            + "script:event-name='dom:click' presentation:action='show' "
            + "xlink:href='#Second'/></office:event-listeners>"
            + "</draw:rect>"
            + "<presentation:notes "
            + "presentation:presentation-page-layout-name='Layout'>"
            + "<draw:page-thumbnail draw:page-number='1'/>"
            + "<draw:frame draw:name='Notes'><draw:text-box>"
            + "<text:p>Speaker notes</text:p></draw:text-box></draw:frame>"
            + "</presentation:notes>"
            + "<anim:seq xml:id='master'><anim:par>"
            + "<anim:iterate xml:id='iterate' smil:targetElement='shape' "
            + "smil:begin='master.end+1.5s;shape.click-250ms' "
            + "smil:dur='2s'>"
            + "<anim:animate smil:attributeName='svg:x' smil:values='0;1'/>"
            + "<anim:set smil:attributeName='presentation:visibility' "
            + "smil:to='visible'/></anim:iterate>"
            + "<anim:animateColor anim:color-interpolation='rgb'/>"
            + "<anim:animateMotion svg:path='M 0 0 L 1 1'/>"
            + "<anim:animateTransform svg:type='rotate'/>"
            + "<anim:transitionFilter smil:type='fade'/>"
            + "<anim:audio xlink:href='Sounds/cue.wav'/>"
            + "<anim:command anim:command='play' smil:targetElement='shape'/>"
            + "</anim:par></anim:seq>"
            + "</draw:page><draw:page draw:name='Second'/>"
            + "<presentation:settings presentation:show='Custom'>"
            + "<presentation:show presentation:name='Custom' "
            + "presentation:pages='Second, First'/>"
            + "</presentation:settings>"
            + "</office:presentation></office:body></office:document>";
    }

    static string Namespaces()
    {
        return " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:anim='" + OdfNamespaces.Animation + "'"
            + " xmlns:smil='" + OdfNamespaces.Smil + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'";
    }
}
