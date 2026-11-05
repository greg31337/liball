namespace pdf;

/// <summary>
/// Defines a reusable ordered group whose items start together when the complete group fits in one flow area.
/// </summary>
public interface IPdfKeepWithNextGroup
{
    /// <summary>
    /// Adds an item to the end of this keep-with-next group.
    /// </summary>
    /// <param name="item">The layout element to add.</param>
    void AddItem(IPdfLayoutElement item);

    /// <summary>
    /// Returns the number of items in this keep-with-next group.
    /// </summary>
    /// <returns>The number of grouped layout elements.</returns>
    int GetItemCount();
}
