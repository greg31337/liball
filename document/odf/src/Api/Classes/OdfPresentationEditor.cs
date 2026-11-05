namespace odf;

/// <summary>Provides typed presentation editing operations.</summary>
public class OdfPresentationEditor : IOdfPresentationEditor
{
    /// <summary>Initializes a presentation editor.</summary>
    public OdfPresentationEditor()
    {
    }

    /// <summary>Gets presentation slides in document order.</summary>
    public OdfSlide[] GetSlides(OdfDocument document)
    {
        return PresentationEditingModel.GetSlides(document);
    }

    /// <summary>Appends a slide.</summary>
    public OdfSlide CreateSlide(OdfDocument document, string name)
    {
        return PresentationSlideEditingModel.CreateSlide(document, name);
    }

    /// <summary>Inserts a slide at an index.</summary>
    public OdfSlide InsertSlide(
        OdfDocument document,
        int index,
        string name)
    {
        return PresentationSlideEditingModel.InsertSlide(
            document,
            index,
            name);
    }

    /// <summary>Deletes a slide and clears references to its identifiers.</summary>
    public void DeleteSlide(OdfDocument document, OdfSlide slide)
    {
        PresentationSlideEditingModel.DeleteSlide(document, slide);
    }

    /// <summary>Moves a slide to another index.</summary>
    public OdfSlide MoveSlide(
        OdfDocument document,
        OdfSlide slide,
        int destinationIndex)
    {
        return PresentationSlideEditingModel.ReorderSlide(
            document,
            slide,
            destinationIndex);
    }

    /// <summary>Duplicates a slide immediately after itself.</summary>
    public OdfSlide DuplicateSlide(OdfDocument document, OdfSlide slide)
    {
        return PresentationSlideEditingModel.DuplicateSlide(document, slide);
    }

    /// <summary>Copies a slide to a selected index.</summary>
    public OdfSlide CopySlide(
        OdfDocument document,
        OdfSlide slide,
        int insertionIndex)
    {
        return PresentationSlideEditingModel.CopySlide(
            document,
            slide,
            insertionIndex);
    }

    /// <summary>Imports one slide and its dependencies.</summary>
    public OdfSlide ImportSlide(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfSlide slide,
        int insertionIndex)
    {
        return PresentationImportModel.ImportSlide(
            sourceDocument,
            targetDocument,
            slide,
            insertionIndex);
    }

    /// <summary>Merges all source slides and dependencies.</summary>
    public OdfSlide[] MergePresentation(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        int insertionIndex)
    {
        return PresentationImportModel.MergePresentation(
            sourceDocument,
            targetDocument,
            insertionIndex);
    }

    /// <summary>Creates an empty notes page for a slide.</summary>
    public OdfPresentationNotes CreateSlideNotes(
        OdfDocument document,
        OdfSlide slide)
    {
        return PresentationEditingModel.CreateSlideNotes(document, slide);
    }

    /// <summary>Removes a slide notes page.</summary>
    public void RemoveSlideNotes(
        OdfDocument document,
        OdfSlide slide)
    {
        PresentationEditingModel.RemoveSlideNotes(document, slide);
    }

    /// <summary>Gets presentation page-layout resources.</summary>
    public OdfStyleResource[] GetPresentationLayouts(OdfDocument document)
    {
        return PresentationEditingModel.GetPresentationLayouts(document);
    }

    /// <summary>Creates a presentation layout.</summary>
    public OdfStyleResource CreatePresentationLayout(
        OdfDocument document,
        string name,
        OdfPresentationPlaceholder[] placeholders)
    {
        return PresentationEditingModel.CreatePresentationLayout(
            document,
            name,
            placeholders);
    }

    /// <summary>Removes a presentation layout.</summary>
    public void RemovePresentationLayout(
        OdfDocument document,
        OdfStyleResource layout)
    {
        PresentationEditingModel.RemovePresentationLayout(
            document,
            layout);
    }

    /// <summary>Assigns a presentation layout to a slide.</summary>
    public OdfSlide AssignSlideLayout(
        OdfDocument document,
        OdfSlide slide,
        OdfStyleResource layout)
    {
        return PresentationEditingModel.AssignSlideLayout(
            document,
            slide,
            layout);
    }

    /// <summary>Assigns a presentation layout to notes.</summary>
    public OdfPresentationNotes AssignNotesLayout(
        OdfDocument document,
        OdfPresentationNotes notes,
        OdfStyleResource layout)
    {
        return PresentationEditingModel.AssignNotesLayout(
            document,
            notes,
            layout);
    }

    /// <summary>Gets named custom shows.</summary>
    public OdfPresentationShow[] GetPresentationShows(OdfDocument document)
    {
        return PresentationEditingModel.GetPresentationShows(document);
    }

    /// <summary>Creates a named custom show.</summary>
    public OdfPresentationShow CreatePresentationShow(
        OdfDocument document,
        string name,
        OdfSlide[] slides)
    {
        return PresentationEditingModel.CreatePresentationShow(
            document,
            name,
            slides);
    }

    /// <summary>Changes the slide sequence of a custom show.</summary>
    public OdfPresentationShow SetPresentationShowSlides(
        OdfDocument document,
        OdfPresentationShow show,
        OdfSlide[] slides)
    {
        return PresentationEditingModel.SetPresentationShowSlides(
            document,
            show,
            slides);
    }

    /// <summary>Removes a custom show.</summary>
    public void RemovePresentationShow(
        OdfDocument document,
        OdfPresentationShow show)
    {
        PresentationEditingModel.RemovePresentationShow(document, show);
    }

    /// <summary>Replaces presentation playback settings.</summary>
    public OdfPresentationSettings SetPresentationSettings(
        OdfDocument document,
        OdfPresentationSettings settings)
    {
        return PresentationEditingModel.SetPresentationSettings(
            document,
            settings);
    }

    /// <summary>Assigns an independent transition style to a slide.</summary>
    public OdfSlideTransition SetSlideTransition(
        OdfDocument document,
        OdfSlide slide,
        OdfSlideTransition transition)
    {
        return PresentationEditingModel.SetSlideTransition(
            document,
            slide,
            transition);
    }

    /// <summary>Removes the transition from one slide.</summary>
    public void RemoveSlideTransition(
        OdfDocument document,
        OdfSlide slide)
    {
        PresentationEditingModel.RemoveSlideTransition(document, slide);
    }

    /// <summary>Gets all animations in preorder.</summary>
    public OdfAnimation[] GetAnimations(OdfDocument document)
    {
        return PresentationEditingModel.GetAnimations(document);
    }

    /// <summary>Creates a root or nested animation construct.</summary>
    public OdfAnimation CreateAnimation(
        OdfDocument document,
        OdfSlide slide,
        OdfAnimation? parent,
        OdfAnimationKind kind)
    {
        return PresentationEditingModel.CreateAnimation(
            document,
            slide,
            parent,
            kind);
    }

    /// <summary>Removes an animation subtree.</summary>
    public void RemoveAnimation(
        OdfDocument document,
        OdfAnimation animation)
    {
        PresentationEditingModel.RemoveAnimation(document, animation);
    }

    /// <summary>Applies direct changes to presentation data fields.</summary>
    public void ApplyPresentationChanges(OdfDocument document)
    {
        PresentationEditingModel.ApplyPresentationChanges(document);
    }
}
