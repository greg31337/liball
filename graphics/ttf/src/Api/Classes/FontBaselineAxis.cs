namespace ttf;

/// <summary>Contains baseline tags and scripts for one BASE axis.</summary>
public class FontBaselineAxis : IFontBaselineAxis
{
    SfntBaseAxis axis;
    FontBaselineScript[] scripts;

    internal FontBaselineAxis(SfntBaseAxis axis)
    {
        this.axis = axis;
        scripts = new FontBaselineScript[axis.GetScriptCount()];
        int index = 0;

        while (index < scripts.Length)
        {
            scripts[index] = new FontBaselineScript(this, axis.GetScript(index));
            index += 1;
        }
    }

    /// <summary>Reports whether this is the horizontal writing axis.</summary>
    /// <returns>True for the horizontal axis.</returns>
    public bool IsHorizontal() { return axis.IsHorizontal(); }

    /// <summary>Reports whether this is the vertical writing axis.</summary>
    /// <returns>True for the vertical axis.</returns>
    public bool IsVertical() { return axis.IsVertical(); }

    /// <summary>Gets the number of baseline tags.</summary>
    /// <returns>The tag count.</returns>
    public int GetBaselineTagCount() { return axis.GetBaselineTagCount(); }

    /// <summary>Gets one baseline tag.</summary>
    /// <param name="index">The zero-based tag index.</param>
    /// <returns>The big-endian tag.</returns>
    public int GetBaselineTag(int index)
    {
        if (index < 0 || index >= axis.GetBaselineTagCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return axis.GetBaselineTag(index);
    }

    /// <summary>Gets the number of script records.</summary>
    /// <returns>The script count.</returns>
    public int GetScriptCount() { return scripts.Length; }

    /// <summary>Gets one script record.</summary>
    /// <param name="index">The zero-based script index.</param>
    /// <returns>The script record.</returns>
    public FontBaselineScript GetScript(int index)
    {
        if (index < 0 || index >= scripts.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return scripts[index];
    }

    /// <summary>Finds a script record by exact tag.</summary>
    /// <param name="scriptTag">The big-endian script tag.</param>
    /// <returns>The script, or null when absent.</returns>
    public FontBaselineScript? FindScript(int scriptTag)
    {
        int index = 0;

        while (index < scripts.Length)
        {
            if (scripts[index].GetScriptTag() == scriptTag)
            {
                return scripts[index];
            }

            index += 1;
        }

        return null;
    }

    internal int GetBaselineTagValue(int index)
    {
        return axis.GetBaselineTag(index);
    }
}
