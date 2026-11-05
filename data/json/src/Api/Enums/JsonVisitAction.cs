namespace json;

/// <summary>Directs depth-first traversal after visiting a JSON value.</summary>
public enum JsonVisitAction
{
    /// <summary>Continues normal depth-first traversal.</summary>
    Continue = 0,

    /// <summary>Continues traversal without visiting the current value's children.</summary>
    SkipChildren = 1,

    /// <summary>Stops traversal successfully.</summary>
    Stop = 2
}
