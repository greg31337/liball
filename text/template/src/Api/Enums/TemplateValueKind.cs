using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Defines the supported value kind values used by the template API.
/// </summary>
public enum TemplateValueKind
{
    /// <summary>
    /// Represents a missing template value.
    /// </summary>
    Missing = 0,
    /// <summary>
    /// Represents a null template value.
    /// </summary>
    Null = 1,
    /// <summary>
    /// Represents a boolean template value.
    /// </summary>
    Boolean = 2,
    /// <summary>
    /// Represents a number template value.
    /// </summary>
    Number = 3,
    /// <summary>
    /// Represents a string template value.
    /// </summary>
    String = 4,
    /// <summary>
    /// Represents a safe string template value.
    /// </summary>
    SafeString = 5,
    /// <summary>
    /// Represents a list template value.
    /// </summary>
    List = 6,
    /// <summary>
    /// Represents a map template value.
    /// </summary>
    Map = 7,
    /// <summary>
    /// Represents an object template value.
    /// </summary>
    Object = 8,
    /// <summary>
    /// Represents a function template value.
    /// </summary>
    Function = 9,
    /// <summary>
    /// Represents a fragment template value.
    /// </summary>
    Fragment = 10
}

