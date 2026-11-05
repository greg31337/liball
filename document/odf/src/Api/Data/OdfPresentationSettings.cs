namespace odf;

/// <summary>Describes presentation playback and show settings.</summary>
public class OdfPresentationSettings
{
    /// <summary>The named starting slide.</summary>
    public string StartPage = "";
    /// <summary>The selected custom show name.</summary>
    public string ShowName = "";
    /// <summary>The preserved full-screen token.</summary>
    public string FullScreenValue = "";
    /// <summary>Whether full-screen playback is requested.</summary>
    public bool FullScreen;
    /// <summary>Whether the full-screen token is absent or valid.</summary>
    public bool IsFullScreenValid;
    /// <summary>The preserved endless-playback token.</summary>
    public string EndlessValue = "";
    /// <summary>Whether endless playback is requested.</summary>
    public bool Endless;
    /// <summary>Whether the endless token is absent or valid.</summary>
    public bool IsEndlessValid;
    /// <summary>The pause duration between endless-show repetitions.</summary>
    public string Pause = "";
    /// <summary>Whether the pause duration is absent or valid.</summary>
    public bool IsPauseValid;
    /// <summary>The preserved show-logo token.</summary>
    public string ShowLogoValue = "";
    /// <summary>Whether the presentation logo is shown during a pause.</summary>
    public bool ShowLogo;
    /// <summary>Whether the show-logo token is absent or valid.</summary>
    public bool IsShowLogoValid;
    /// <summary>The preserved force-manual token.</summary>
    public string ForceManualValue = "";
    /// <summary>Whether automatic transitions are disabled during playback.</summary>
    public bool ForceManual;
    /// <summary>Whether the force-manual token is absent or valid.</summary>
    public bool IsForceManualValid;
    /// <summary>The preserved mouse-visible token.</summary>
    public string MouseVisibleValue = "";
    /// <summary>Whether the pointer is visible during playback.</summary>
    public bool MouseVisible;
    /// <summary>Whether the mouse-visible token is absent or valid.</summary>
    public bool IsMouseVisibleValid;
    /// <summary>The preserved mouse-as-pen token.</summary>
    public string MouseAsPenValue = "";
    /// <summary>Whether the pointer acts as a drawing pen.</summary>
    public bool MouseAsPen;
    /// <summary>Whether the mouse-as-pen token is absent or valid.</summary>
    public bool IsMouseAsPenValid;
    /// <summary>The preserved start-with-navigator token.</summary>
    public string StartWithNavigatorValue = "";
    /// <summary>Whether playback starts with the navigator.</summary>
    public bool StartWithNavigator;
    /// <summary>Whether the navigator token is absent or valid.</summary>
    public bool IsStartWithNavigatorValid;
    /// <summary>The preserved animations token.</summary>
    public string AnimationsValue = "";
    /// <summary>Whether animations are enabled.</summary>
    public bool AnimationsEnabled;
    /// <summary>Whether the animations token is absent or valid.</summary>
    public bool IsAnimationsValid;
    /// <summary>The preserved transition-on-click token.</summary>
    public string TransitionOnClickValue = "";
    /// <summary>Whether clicking advances the presentation.</summary>
    public bool TransitionOnClickEnabled;
    /// <summary>Whether the transition-on-click token is absent or valid.</summary>
    public bool IsTransitionOnClickValid;
    /// <summary>The preserved stay-on-top token.</summary>
    public string StayOnTopValue = "";
    /// <summary>Whether the presentation remains above other windows.</summary>
    public bool StayOnTop;
    /// <summary>Whether the stay-on-top token is absent or valid.</summary>
    public bool IsStayOnTopValid;
    /// <summary>The preserved end-slide token.</summary>
    public string ShowEndSlideValue = "";
    /// <summary>Whether the end-of-presentation slide is shown.</summary>
    public bool ShowEndSlide;
    /// <summary>Whether the end-slide token is absent or valid.</summary>
    public bool IsShowEndSlideValid;
    /// <summary>The resolved starting slide.</summary>
    public OdfSlide? StartSlide;
    /// <summary>The resolved selected custom show.</summary>
    public OdfPresentationShow? SelectedShow;
    /// <summary>The underlying settings element.</summary>
    public OdfNode? Node;
    internal string OriginalStartPage = "";
    internal string OriginalShowName = "";
    internal string OriginalFullScreenValue = "";
    internal string OriginalEndlessValue = "";
    internal string OriginalPause = "";
    internal string OriginalShowLogoValue = "";
    internal string OriginalForceManualValue = "";
    internal string OriginalMouseVisibleValue = "";
    internal string OriginalMouseAsPenValue = "";
    internal string OriginalStartWithNavigatorValue = "";
    internal string OriginalAnimationsValue = "";
    internal string OriginalTransitionOnClickValue = "";
    internal string OriginalStayOnTopValue = "";
    internal string OriginalShowEndSlideValue = "";
    internal OdfSlide? OriginalStartSlide;
    internal OdfPresentationShow? OriginalSelectedShow;
    internal OdfNode[] OriginalShowNodes = [];
}
