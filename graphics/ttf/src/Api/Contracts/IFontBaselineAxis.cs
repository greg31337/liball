namespace ttf;

/// <summary>Defines baseline tags and scripts for one BASE axis.</summary>
public interface IFontBaselineAxis
{
    /// <summary>Reports whether this is the horizontal writing axis.</summary>
    /// <returns>True for the horizontal axis.</returns>
    bool IsHorizontal();

    /// <summary>Reports whether this is the vertical writing axis.</summary>
    /// <returns>True for the vertical axis.</returns>
    bool IsVertical();

    /// <summary>Gets the number of baseline tags.</summary>
    /// <returns>The tag count.</returns>
    int GetBaselineTagCount();

    /// <summary>Gets one baseline tag.</summary>
    /// <param name="index">The zero-based tag index.</param>
    /// <returns>The big-endian tag.</returns>
    int GetBaselineTag(int index);

    /// <summary>Gets the number of script records.</summary>
    /// <returns>The script count.</returns>
    int GetScriptCount();

    /// <summary>Gets one script record.</summary>
    /// <param name="index">The zero-based script index.</param>
    /// <returns>The script record.</returns>
    FontBaselineScript GetScript(int index);

    /// <summary>Finds a script record by exact tag.</summary>
    /// <param name="scriptTag">The big-endian script tag.</param>
    /// <returns>The script, or null when absent.</returns>
    FontBaselineScript? FindScript(int scriptTag);
}
