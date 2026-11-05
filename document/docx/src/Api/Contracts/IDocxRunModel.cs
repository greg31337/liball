namespace docx;

/// <summary>
/// Provides ordered navigation and editing for direct run children.
/// </summary>
public interface IDocxRunModel
{
    /// <summary>
    /// Returns whether direct run formatting is present.
    /// </summary>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    bool HasFormat();

    /// <summary>
    /// Returns the direct run format model.
    /// </summary>
    /// <returns>The run format model.</returns>
    DocxRunFormat GetFormat();

    /// <summary>
    /// Replaces the direct run format model.
    /// </summary>
    /// <param name="format">The replacement run format.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>format</c> is null.</exception>
    void SetFormat(DocxRunFormat format);

    /// <summary>
    /// Returns all direct run children in document order.
    /// </summary>
    /// <returns>A new list containing the run children.</returns>
    List<DocxRunChild> GetChildren();

    /// <summary>
    /// Returns the number of direct run children.
    /// </summary>
    /// <returns>The child count.</returns>
    int ChildCount();

    /// <summary>
    /// Returns a direct run child by position.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The requested child.</returns>
    DocxRunChild GetChild(int index);

    /// <summary>
    /// Returns all direct text nodes in document order.
    /// </summary>
    /// <returns>A new list containing the text nodes.</returns>
    List<DocxText> GetTextNodes();

    /// <summary>
    /// Returns the number of direct text nodes.
    /// </summary>
    /// <returns>The text-node count.</returns>
    int TextCount();

    /// <summary>
    /// Returns all parsed run controls in document order.
    /// </summary>
    /// <returns>A new list containing tabs, carriage returns, breaks, hyphens, and symbols.</returns>
    List<DocxBreak> GetBreaks();

    /// <summary>
    /// Returns the number of parsed run controls.
    /// </summary>
    /// <returns>The run-control count.</returns>
    int BreakCount();

    /// <summary>
    /// Returns a parsed run control by position.
    /// </summary>
    /// <param name="index">The zero-based run-control position.</param>
    /// <returns>The requested run control.</returns>
    DocxBreak GetBreak(int index);

    /// <summary>
    /// Returns direct tab children in document order.
    /// </summary>
    /// <returns>A new list containing the tabs.</returns>
    List<DocxRunChild> GetTabs();

    /// <summary>
    /// Returns the number of direct tab children.
    /// </summary>
    /// <returns>The tab count.</returns>
    int TabCount();

    /// <summary>
    /// Returns a direct tab child by position.
    /// </summary>
    /// <param name="index">The zero-based tab position.</param>
    /// <returns>The requested tab.</returns>
    DocxRunChild GetTab(int index);

    /// <summary>
    /// Returns direct carriage-return children in document order.
    /// </summary>
    /// <returns>A new list containing the carriage returns.</returns>
    List<DocxRunChild> GetCarriageReturns();

    /// <summary>
    /// Returns the number of direct carriage-return children.
    /// </summary>
    /// <returns>The carriage-return count.</returns>
    int CarriageReturnCount();

    /// <summary>
    /// Returns a direct carriage-return child by position.
    /// </summary>
    /// <param name="index">The zero-based carriage-return position.</param>
    /// <returns>The requested carriage return.</returns>
    DocxRunChild GetCarriageReturn(int index);

    /// <summary>
    /// Returns direct line, page, and column break children in document order.
    /// </summary>
    /// <returns>A new list containing the break elements.</returns>
    List<DocxRunChild> GetBreakElements();

    /// <summary>
    /// Returns the number of direct line, page, and column break children.
    /// </summary>
    /// <returns>The break-element count.</returns>
    int BreakElementCount();

    /// <summary>
    /// Returns a direct line, page, or column break child by position.
    /// </summary>
    /// <param name="index">The zero-based break-element position.</param>
    /// <returns>The requested break element.</returns>
    DocxRunChild GetBreakElement(int index);

    /// <summary>
    /// Returns direct soft and non-breaking hyphen children in document order.
    /// </summary>
    /// <returns>A new list containing the hyphen controls.</returns>
    List<DocxRunChild> GetHyphens();

    /// <summary>
    /// Returns the number of direct hyphen controls.
    /// </summary>
    /// <returns>The hyphen-control count.</returns>
    int HyphenCount();

    /// <summary>
    /// Returns a direct hyphen control by position.
    /// </summary>
    /// <param name="index">The zero-based hyphen-control position.</param>
    /// <returns>The requested hyphen control.</returns>
    DocxRunChild GetHyphen(int index);

    /// <summary>
    /// Returns direct symbol children in document order.
    /// </summary>
    /// <returns>A new list containing the symbols.</returns>
    List<DocxRunChild> GetSymbols();

    /// <summary>
    /// Returns the number of direct symbol children.
    /// </summary>
    /// <returns>The symbol count.</returns>
    int SymbolCount();

    /// <summary>
    /// Returns a direct symbol child by position.
    /// </summary>
    /// <param name="index">The zero-based symbol position.</param>
    /// <returns>The requested symbol.</returns>
    DocxRunChild GetSymbol(int index);

    /// <summary>
    /// Returns direct complex-field character children in document order.
    /// </summary>
    /// <returns>A new list containing the field characters.</returns>
    List<DocxRunChild> GetFieldCharacters();

    /// <summary>
    /// Returns the number of direct complex-field character children.
    /// </summary>
    /// <returns>The field-character count.</returns>
    int FieldCharacterCount();

    /// <summary>
    /// Returns a direct complex-field character by position.
    /// </summary>
    /// <param name="index">The zero-based field-character position.</param>
    /// <returns>The requested field character.</returns>
    DocxRunChild GetFieldCharacter(int index);

    /// <summary>
    /// Returns direct complex-field instruction children in document order.
    /// </summary>
    /// <returns>A new list containing the field instructions.</returns>
    List<DocxRunChild> GetFieldInstructions();

    /// <summary>
    /// Returns the number of direct complex-field instruction children.
    /// </summary>
    /// <returns>The field-instruction count.</returns>
    int FieldInstructionCount();

    /// <summary>
    /// Returns a direct complex-field instruction by position.
    /// </summary>
    /// <param name="index">The zero-based field-instruction position.</param>
    /// <returns>The requested field instruction.</returns>
    DocxRunChild GetFieldInstruction(int index);

    /// <summary>
    /// Returns all direct drawings in document order.
    /// </summary>
    /// <returns>A new list containing the drawings.</returns>
    List<DocxDrawing> GetDrawings();

    /// <summary>
    /// Returns the number of direct drawings.
    /// </summary>
    /// <returns>The drawing count.</returns>
    int DrawingCount();

    /// <summary>
    /// Returns a drawing by position.
    /// </summary>
    /// <param name="index">The zero-based drawing position.</param>
    /// <returns>The requested drawing.</returns>
    DocxDrawing GetDrawing(int index);

    /// <summary>
    /// Returns preserved direct children not represented by another child kind.
    /// </summary>
    /// <returns>A new list containing the preserved unknown children.</returns>
    List<DocxRunChild> GetPreservedUnknownChildren();

    /// <summary>
    /// Returns the number of preserved direct children not represented by another kind.
    /// </summary>
    /// <returns>The preserved unknown child count.</returns>
    int PreservedUnknownChildCount();

    /// <summary>
    /// Returns a preserved unknown child by position.
    /// </summary>
    /// <param name="index">The zero-based preserved unknown child position.</param>
    /// <returns>The requested preserved unknown child.</returns>
    DocxRunChild GetPreservedUnknownChild(int index);

    /// <summary>
    /// Returns a text node by position.
    /// </summary>
    /// <param name="index">The zero-based text-node position.</param>
    /// <returns>The requested text node.</returns>
    DocxText GetTextNode(int index);

    /// <summary>
    /// Appends a direct run child.
    /// </summary>
    /// <param name="child">The child to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    void AddRunChild(DocxRunChild child);

    /// <summary>
    /// Inserts a direct run child.
    /// </summary>
    /// <param name="index">The zero-based insertion position.</param>
    /// <param name="child">The child to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    void InsertRunChild(int index, DocxRunChild child);

    /// <summary>
    /// Inserts a text node at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="text">The text node to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    void InsertTextNode(int index, DocxText text);

    /// <summary>
    /// Inserts a run control at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="value">The tab, carriage return, break, hyphen, or symbol to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    void InsertBreak(int index, DocxBreak value);

    /// <summary>
    /// Inserts a drawing at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="drawing">The drawing to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>drawing</c> is null.</exception>
    void InsertDrawing(int index, DocxDrawing drawing);

    /// <summary>
    /// Replaces a direct run child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="child">The replacement child.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    DocxRunChild ReplaceRunChild(int index, DocxRunChild child);

    /// <summary>
    /// Replaces a direct run child with a text node.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="text">The replacement text node.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    DocxRunChild ReplaceChildWithText(int index, DocxText text);

    /// <summary>
    /// Replaces a direct run child with a run control.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="value">The replacement tab, carriage return, break, hyphen, or symbol.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    DocxRunChild ReplaceChildWithBreak(int index, DocxBreak value);

    /// <summary>
    /// Replaces a direct run child with a drawing.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="drawing">The replacement drawing.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>drawing</c> is null.</exception>
    DocxRunChild ReplaceChildWithDrawing(int index, DocxDrawing drawing);

    /// <summary>
    /// Moves a direct run child to another position.
    /// </summary>
    /// <param name="sourceIndex">The current zero-based child position.</param>
    /// <param name="destinationIndex">The resulting zero-based child position.</param>
    void MoveRunChild(int sourceIndex, int destinationIndex);

    /// <summary>
    /// Removes a direct run child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The removed child.</returns>
    DocxRunChild RemoveRunChildAt(int index);
}
