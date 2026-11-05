namespace ttf;

/// <summary>Contains preserved TrueType hinting programs and control-value data.</summary>
/// <remarks>
/// This data is retained for inspection and downstream preservation only. The
/// library does not execute TrueType instructions, grid-fit outlines, or
/// rasterize glyphs.
/// </remarks>
public class TrueTypeHintingData : ITrueTypeHintingData
{
    byte[] fontProgram;
    byte[] controlValueProgram;
    byte[] controlValueData;

    internal TrueTypeHintingData(
        FontFace face,
        double[]? normalizedCoordinates)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }

        fontProgram = CopyTable(face, FontTags.Fpgm);
        controlValueProgram = CopyTable(face, FontTags.Prep);
        controlValueData = CopyTable(face, FontTags.Cvt);
        SfntCvarTable? cvar = normalizedCoordinates == null
            ? null
            : face.GetCvarTable();

        if (normalizedCoordinates != null && cvar != null)
        {
            controlValueData = cvar.Apply(
                controlValueData,
                normalizedCoordinates);
        }
        else if (normalizedCoordinates != null && face.HasTable(FontTags.Cvar))
        {
            throw FontFormatException.Create(
                "Varied control values require a supported cvar table.");
        }
    }

    internal static TrueTypeHintingData Create(FontFace face)
    {
        return new TrueTypeHintingData(face, null);
    }

    internal static TrueTypeHintingData Create(
        FontFace face,
        double[] normalizedCoordinates)
    {
        if (normalizedCoordinates == null)
        {
            throw new System.ArgumentNullException("normalizedCoordinates");
        }

        return new TrueTypeHintingData(face, normalizedCoordinates);
    }

    /// <summary>Reports whether an <c>fpgm</c> font program is present.</summary>
    /// <returns>True when the preserved font program is nonempty.</returns>
    public bool HasFontProgram() { return fontProgram.Length != 0; }

    /// <summary>Gets the preserved <c>fpgm</c> byte count.</summary>
    /// <returns>The font-program byte count.</returns>
    public int GetFontProgramLength() { return fontProgram.Length; }

    /// <summary>Copies the unexecuted <c>fpgm</c> font program.</summary>
    /// <returns>A new array containing the program bytes.</returns>
    public byte[] CopyFontProgram() { return fontProgram.AsSpan().ToArray(); }

    /// <summary>Reports whether a <c>prep</c> control-value program is present.</summary>
    /// <returns>True when the preserved control-value program is nonempty.</returns>
    public bool HasControlValueProgram()
    {
        return controlValueProgram.Length != 0;
    }

    /// <summary>Gets the preserved <c>prep</c> byte count.</summary>
    /// <returns>The control-value-program byte count.</returns>
    public int GetControlValueProgramLength()
    {
        return controlValueProgram.Length;
    }

    /// <summary>Copies the unexecuted <c>prep</c> control-value program.</summary>
    /// <returns>A new array containing the program bytes.</returns>
    public byte[] CopyControlValueProgram()
    {
        return controlValueProgram.AsSpan().ToArray();
    }

    /// <summary>Reports whether preserved <c>cvt </c> data is present.</summary>
    /// <returns>True when the control-value data is nonempty.</returns>
    public bool HasControlValueData() { return controlValueData.Length != 0; }

    /// <summary>Gets the preserved <c>cvt </c> byte count.</summary>
    /// <returns>The raw control-value-data byte count.</returns>
    public int GetControlValueDataLength() { return controlValueData.Length; }

    /// <summary>Copies the instance-adjusted <c>cvt </c> table data.</summary>
    /// <returns>A new array containing the control values as FWORD bytes.</returns>
    public byte[] CopyControlValueData()
    {
        return controlValueData.AsSpan().ToArray();
    }

    static byte[] CopyTable(FontFace face, int tag)
    {
        SfntTableData? table = face.GetTableStore().FindTable(tag);
        return table == null ? new byte[0] : table.GetDataSlice().CopyBytes();
    }
}
