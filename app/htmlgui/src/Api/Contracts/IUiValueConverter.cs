namespace htmlgui;

/// <summary>Converts portable binding values in both propagation directions.</summary>
public interface IUiValueConverter
{
    /// <summary>Converts one source value for a binding target.</summary>
    /// <param name="value">The copied source value.</param>
    /// <param name="context">The copied target-directed binding context.</param>
    /// <returns>The exceptionless conversion result.</returns>
    UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context);

    /// <summary>Converts one target value back to its state representation.</summary>
    /// <param name="value">The copied target value.</param>
    /// <param name="context">The copied source-directed binding context.</param>
    /// <returns>The exceptionless conversion result.</returns>
    UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context);
}
