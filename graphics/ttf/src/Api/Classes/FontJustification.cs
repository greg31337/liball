namespace ttf;

/// <summary>Provides read-only JSTF inspection.</summary>
public class FontJustification : IFontJustification
{
    SfntJstfTable table;
    internal FontJustification(SfntJstfTable table) { this.table = table; }
    /// <summary>Gets the major version.</summary><returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }
    /// <summary>Gets the minor version.</summary><returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }
    /// <summary>Gets the script count.</summary><returns>The script count.</returns>
    public int GetScriptCount() { return table.GetScriptCount(); }
    /// <summary>Gets a script.</summary><param name="index">The script index.</param><returns>The script.</returns>
    public FontJustificationScript GetScript(int index)
    {
        return new FontJustificationScript(table.GetScript(index));
    }
    /// <summary>Finds a script.</summary><param name="scriptTag">The script tag.</param><returns>The script, or null.</returns>
    public FontJustificationScript? FindScript(int scriptTag)
    {
        SfntJstfScript? script = table.FindScript(scriptTag);
        return script == null ? null : new FontJustificationScript(script);
    }
}
