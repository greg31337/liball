using System;
using System.Collections.Generic;

namespace html;
/// <summary>Identifies whether a depth-first walk is entering or leaving a node.</summary>
public enum HtmlTreeVisitPhase
{
    /// <summary>Invokes the depth-first visitor before visiting the node's children.</summary>
    Enter = 0,
    /// <summary>Invokes the depth-first visitor after visiting the node's children.</summary>
    Exit = 1
}
