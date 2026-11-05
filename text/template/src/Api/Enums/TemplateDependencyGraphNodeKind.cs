using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Defines the supported dependency graph node kind values used by the template API.
/// </summary>
public enum TemplateDependencyGraphNodeKind
{
    /// <summary>
    /// Identifies the root form.
    /// </summary>
    Root = 0,
    /// <summary>
    /// Identifies the template form.
    /// </summary>
    Template = 1,
    /// <summary>
    /// Identifies the partial form.
    /// </summary>
    Partial = 2
}

