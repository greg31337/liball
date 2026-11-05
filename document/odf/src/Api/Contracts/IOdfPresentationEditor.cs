namespace odf;

/// <summary>Provides typed editing operations for presentations.</summary>
public interface IOdfPresentationEditor
{
    /// <summary>Gets presentation slides in document order.</summary>
    /// <param name="document">The presentation document.</param>
    /// <returns>An independent slide array.</returns>
    public OdfSlide[] GetSlides(OdfDocument document);
    /// <summary>Appends a slide.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="name">The requested slide name.</param>
    /// <returns>The created slide.</returns>
    public OdfSlide CreateSlide(OdfDocument document, string name);
    /// <summary>Inserts a slide at an index.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="name">The requested slide name.</param>
    /// <returns>The inserted slide.</returns>
    public OdfSlide InsertSlide(
        OdfDocument document,
        int index,
        string name);
    /// <summary>Deletes a slide and clears references to its identifiers.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to delete.</param>
    public void DeleteSlide(OdfDocument document, OdfSlide slide);
    /// <summary>Moves a slide to another index.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to move.</param>
    /// <param name="destinationIndex">The destination index.</param>
    /// <returns>The refreshed slide.</returns>
    public OdfSlide MoveSlide(
        OdfDocument document,
        OdfSlide slide,
        int destinationIndex);
    /// <summary>Duplicates a slide immediately after itself.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to duplicate.</param>
    /// <returns>The duplicate slide.</returns>
    public OdfSlide DuplicateSlide(OdfDocument document, OdfSlide slide);
    /// <summary>Copies a slide to a selected index in the same document.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to copy.</param>
    /// <param name="insertionIndex">The insertion index.</param>
    /// <returns>The copied slide.</returns>
    public OdfSlide CopySlide(
        OdfDocument document,
        OdfSlide slide,
        int insertionIndex);
    /// <summary>Imports one slide and its dependencies.</summary>
    /// <param name="sourceDocument">The source presentation.</param>
    /// <param name="targetDocument">The target presentation.</param>
    /// <param name="slide">The source slide.</param>
    /// <param name="insertionIndex">The target insertion index.</param>
    /// <returns>The imported slide.</returns>
    public OdfSlide ImportSlide(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        OdfSlide slide,
        int insertionIndex);
    /// <summary>Merges all source slides and dependencies.</summary>
    /// <param name="sourceDocument">The source presentation.</param>
    /// <param name="targetDocument">The target presentation.</param>
    /// <param name="insertionIndex">The first target insertion index.</param>
    /// <returns>The imported slides.</returns>
    public OdfSlide[] MergePresentation(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        int insertionIndex);
    /// <summary>Creates an empty notes page for a slide.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The owning slide.</param>
    /// <returns>The created notes page.</returns>
    public OdfPresentationNotes CreateSlideNotes(
        OdfDocument document,
        OdfSlide slide);
    /// <summary>Removes a slide notes page.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The owning slide.</param>
    public void RemoveSlideNotes(
        OdfDocument document,
        OdfSlide slide);
    /// <summary>Gets presentation page-layout resources.</summary>
    /// <param name="document">The presentation document.</param>
    /// <returns>An independent layout array.</returns>
    public OdfStyleResource[] GetPresentationLayouts(OdfDocument document);
    /// <summary>Creates a presentation layout from placeholder definitions.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="name">The unique layout name.</param>
    /// <param name="placeholders">The placeholder definitions.</param>
    /// <returns>The created layout.</returns>
    public OdfStyleResource CreatePresentationLayout(
        OdfDocument document,
        string name,
        OdfPresentationPlaceholder[] placeholders);
    /// <summary>Removes a layout and clears its typed assignments.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="layout">The layout to remove.</param>
    public void RemovePresentationLayout(
        OdfDocument document,
        OdfStyleResource layout);
    /// <summary>Assigns a presentation layout to a slide.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to update.</param>
    /// <param name="layout">The layout to assign.</param>
    /// <returns>The refreshed slide.</returns>
    public OdfSlide AssignSlideLayout(
        OdfDocument document,
        OdfSlide slide,
        OdfStyleResource layout);
    /// <summary>Assigns a presentation layout to a notes page.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="notes">The notes page to update.</param>
    /// <param name="layout">The layout to assign.</param>
    /// <returns>The refreshed notes page.</returns>
    public OdfPresentationNotes AssignNotesLayout(
        OdfDocument document,
        OdfPresentationNotes notes,
        OdfStyleResource layout);
    /// <summary>Gets named custom shows.</summary>
    /// <param name="document">The presentation document.</param>
    /// <returns>An independent show array.</returns>
    public OdfPresentationShow[] GetPresentationShows(OdfDocument document);
    /// <summary>Creates a named custom show.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="name">The unique show name.</param>
    /// <param name="slides">The slides in playback order.</param>
    /// <returns>The created show.</returns>
    public OdfPresentationShow CreatePresentationShow(
        OdfDocument document,
        string name,
        OdfSlide[] slides);
    /// <summary>Changes the slide sequence of a custom show.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="show">The show to update.</param>
    /// <param name="slides">The replacement slide sequence.</param>
    /// <returns>The refreshed show.</returns>
    public OdfPresentationShow SetPresentationShowSlides(
        OdfDocument document,
        OdfPresentationShow show,
        OdfSlide[] slides);
    /// <summary>Removes a custom show and clears its settings selection.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="show">The show to remove.</param>
    public void RemovePresentationShow(
        OdfDocument document,
        OdfPresentationShow show);
    /// <summary>Replaces presentation playback settings.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="settings">The requested settings.</param>
    /// <returns>The refreshed settings.</returns>
    public OdfPresentationSettings SetPresentationSettings(
        OdfDocument document,
        OdfPresentationSettings settings);
    /// <summary>Assigns an independent transition style to a slide.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to update.</param>
    /// <param name="transition">The transition definition.</param>
    /// <returns>The assigned transition.</returns>
    public OdfSlideTransition SetSlideTransition(
        OdfDocument document,
        OdfSlide slide,
        OdfSlideTransition transition);
    /// <summary>Removes the transition from one slide.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The slide to update.</param>
    public void RemoveSlideTransition(
        OdfDocument document,
        OdfSlide slide);
    /// <summary>Gets all animations in preorder.</summary>
    /// <param name="document">The presentation document.</param>
    /// <returns>An independent animation array.</returns>
    public OdfAnimation[] GetAnimations(OdfDocument document);
    /// <summary>Creates a root or nested animation construct.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="slide">The owning slide.</param>
    /// <param name="parent">The timing parent, or <c>null</c> for a root.</param>
    /// <param name="kind">The animation kind.</param>
    /// <returns>The created animation.</returns>
    public OdfAnimation CreateAnimation(
        OdfDocument document,
        OdfSlide slide,
        OdfAnimation? parent,
        OdfAnimationKind kind);
    /// <summary>Removes an animation subtree.</summary>
    /// <param name="document">The presentation document.</param>
    /// <param name="animation">The animation subtree root.</param>
    public void RemoveAnimation(
        OdfDocument document,
        OdfAnimation animation);
    /// <summary>Applies direct changes made to presentation data fields.</summary>
    /// <param name="document">The presentation document.</param>
    public void ApplyPresentationChanges(OdfDocument document);
}
