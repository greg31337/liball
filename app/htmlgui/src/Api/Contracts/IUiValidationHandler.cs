using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Receives copied custom-validation callbacks without public delegates.</summary>
public interface IUiValidationHandler
{
    /// <summary>Validates one field, cross-field, or form rule.</summary>
    /// <param name="context">The copied callback context and submitted values.</param>
    /// <returns>A task containing the exceptionless validation outcome.</returns>
    Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context);
}
