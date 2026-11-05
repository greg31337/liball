namespace htmlgui;

/// <summary>Creates and registers keyboard maps and processes normalized composite-widget input.</summary>
public interface IUiKeyboardController
{
    /// <summary>Creates an APG-compatible keyboard map without registering it.</summary>
    /// <param name="id">The stable map identifier.</param>
    /// <param name="widget">The supported widget token.</param>
    /// <param name="orientation">The widget orientation.</param>
    /// <param name="isRightToLeft">Whether logical horizontal directions reverse.</param>
    /// <returns>The keyboard map.</returns>
    /// <exception cref="System.ArgumentException">Thrown when an identifier or widget token is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the orientation is invalid.</exception>
    UiKeyboardMap CreateKeyboardMap(string id, string widget, UiOrientation orientation, bool isRightToLeft);

    /// <summary>Registers or replaces a keyboard map after exceptionless validation.</summary>
    /// <param name="map">The optional keyboard map.</param>
    /// <returns>The registration result.</returns>
    UiResult RegisterKeyboardMap(UiKeyboardMap? map);

    /// <summary>Processes a mapped shortcut, composite-widget key, or type-ahead character.</summary>
    /// <param name="mapId">The optional identifier of a registered map.</param>
    /// <param name="keyboard">The optional normalized keyboard data.</param>
    /// <param name="typeAhead">The optional current type-ahead state.</param>
    /// <param name="items">The optional keyed type-ahead labels.</param>
    /// <param name="generation">The nonnegative type-ahead generation.</param>
    /// <param name="allowRepeat">Whether repeated input may dispatch an action.</param>
    /// <param name="resetTypeAhead">Whether to reset the current buffer before processing.</param>
    /// <returns>The deterministic keyboard-processing result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when keyboard, type-ahead, or item data is null for a registered map.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the generation is negative.</exception>
    UiKeyboardResult ProcessKeyboardInput(string? mapId, UiKeyboardData? keyboard, UiTypeAheadState? typeAhead, UiValueEntry[]? items, int generation, bool allowRepeat, bool resetTypeAhead);

    /// <summary>Returns a copy of type-ahead state with its buffer and match reset.</summary>
    /// <param name="typeAhead">The current type-ahead state.</param>
    /// <param name="generation">The nonnegative reset generation.</param>
    /// <returns>The reset state.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the state is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the generation is negative.</exception>
    UiTypeAheadState ResetKeyboardState(UiTypeAheadState typeAhead, int generation);
}
