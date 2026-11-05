using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace template;

/// <summary>
/// Defines the supported member naming policy values used by the template API.
/// </summary>
public enum TemplateMemberNamingPolicy
{
    /// <summary>
    /// Matches members using exact naming.
    /// </summary>
    Exact = 0,
    /// <summary>
    /// Matches members using camel case naming.
    /// </summary>
    CamelCase = 1,
    /// <summary>
    /// Matches members using snake case naming.
    /// </summary>
    SnakeCase = 2,
    /// <summary>
    /// Matches members using kebab case naming.
    /// </summary>
    KebabCase = 3
}

