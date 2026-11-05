namespace ttf;

/// <summary>Defines read-only JSTF inspection.</summary>
public interface IFontJustification
{
    /// <summary>Gets the major version.</summary><returns>The major version.</returns>
    int GetMajorVersion();
    /// <summary>Gets the minor version.</summary><returns>The minor version.</returns>
    int GetMinorVersion();
    /// <summary>Gets the script count.</summary><returns>The script count.</returns>
    int GetScriptCount();
    /// <summary>Gets a script.</summary><param name="index">The script index.</param><returns>The script.</returns>
    FontJustificationScript GetScript(int index);
    /// <summary>Finds a script.</summary><param name="scriptTag">The script tag.</param><returns>The script, or null.</returns>
    FontJustificationScript? FindScript(int scriptTag);
}
