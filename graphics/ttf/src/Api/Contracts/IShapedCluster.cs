namespace ttf;

/// <summary>Defines one immutable logical source cluster.</summary>
public interface IShapedCluster
{
    /// <summary>Gets the source UTF-16 start.</summary><returns>The absolute offset.</returns>
    int GetUtf16Start();
    /// <summary>Gets the source UTF-16 length.</summary><returns>The code-unit length.</returns>
    int GetUtf16Length();
    /// <summary>Gets the exclusive source UTF-16 end.</summary><returns>The absolute end offset.</returns>
    int GetUtf16End();
    /// <summary>Gets the source Unicode scalar count.</summary><returns>The scalar count.</returns>
    int GetUnicodeScalarCount();
    /// <summary>Gets one source Unicode scalar.</summary><param name="index">The scalar index.</param><returns>The scalar value.</returns>
    int GetUnicodeScalar(int index);
    /// <summary>Copies the source Unicode scalars.</summary><returns>An independent array.</returns>
    int[] CopyUnicodeScalars();
    /// <summary>Gets the output glyph count.</summary><returns>The glyph count.</returns>
    int GetGlyphCount();
    /// <summary>Gets one output glyph visual index.</summary><param name="index">The member index.</param><returns>The visual glyph index.</returns>
    int GetGlyphVisualIndex(int index);
    /// <summary>Gets the logical cluster index.</summary><returns>The logical index.</returns>
    int GetLogicalIndex();
    /// <summary>Gets the visual cluster index.</summary><returns>The visual index.</returns>
    int GetVisualIndex();
    /// <summary>Reports whether breaking before this cluster may change shaping.</summary><returns>True when unsafe.</returns>
    bool IsUnsafeToBreak();
}
