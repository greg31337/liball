using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Supplies evaluated arguments, both block branches, and the active render context to a registered block helper.
/// </summary>
/// <remarks>
/// Returning a safe string from a helper suppresses automatic escaping for that result.
/// </remarks>
public class TemplateBlockHelperContext
{
    /// <summary>
    /// Stores the ordered arguments.
    /// </summary>
    public List<TemplateValue> Arguments;
    /// <summary>
    /// Stores the ordered body.
    /// </summary>
    public List<TemplateSyntaxNode> Body;
    /// <summary>
    /// Stores the ordered else body.
    /// </summary>
    public List<TemplateSyntaxNode> ElseBody;
    /// <summary>
    /// Specifies the render context for this block helper context.
    /// </summary>
    public TemplateRenderContext? RenderContext;

    /// <summary>
    /// Initializes a new template block helper context and validates any supplied values.
    /// </summary>
    /// <param name="arguments">The ordered arguments supplied by the template.</param>
    /// <param name="body">The nodes in the main block branch.</param>
    /// <param name="elseBody">The nodes in the alternate block branch.</param>
    /// <param name="renderContext">The active state for the current render.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateBlockHelperContext(List<TemplateValue> arguments, List<TemplateSyntaxNode> body, List<TemplateSyntaxNode> elseBody, TemplateRenderContext? renderContext)
    {
        if (arguments == null)
        {
            throw new ArgumentNullException("arguments");
        }

        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        if (elseBody == null)
        {
            throw new ArgumentNullException("elseBody");
        }

        Arguments = arguments;
        Body = body;
        ElseBody = elseBody;
        RenderContext = renderContext;
    }
}

