using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendar"/>.
/// </summary>
public interface IEmailCalendarContract
{
    /// <summary>
    /// Gets the property count.
    /// </summary>
    /// <returns>The current property count.</returns>
    int GetPropertyCount();
    /// <summary>
    /// Gets the property at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested property.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailCalendarProperty GetPropertyAt(int index);
    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    List<EmailCalendarProperty> GetProperties();
    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "propertyName">The property name used by the operation.</param>
    /// <returns>The current properties.</returns>
    List<EmailCalendarProperty> GetPropertiesNamed(string propertyName);
    /// <summary>
    /// Gets the first property.
    /// </summary>
    /// <param name = "propertyName">The property name used by the operation.</param>
    /// <returns>The current first property.</returns>
    EmailCalendarProperty? GetFirstProperty(string propertyName);
    /// <summary>
    /// Determines whether the method is present or available.
    /// </summary>
    /// <returns><c>true</c> when the method is available; otherwise, <c>false</c>.</returns>
    bool HasMethod();
    /// <summary>
    /// Gets the method name.
    /// </summary>
    /// <returns>The current method name.</returns>
    string? GetMethodName();
    /// <summary>
    /// Gets the method.
    /// </summary>
    /// <returns>The current method.</returns>
    EmailCalendarMethod GetMethod();
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    string? GetVersion();
    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    /// <returns>The current product identifier.</returns>
    string? GetProductIdentifier();
    /// <summary>
    /// Gets the component count.
    /// </summary>
    /// <returns>The current component count.</returns>
    int GetComponentCount();
    /// <summary>
    /// Gets the component at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested component.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailCalendarComponent GetComponentAt(int index);
    /// <summary>
    /// Gets the components.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current components.</returns>
    List<EmailCalendarComponent> GetComponents();
    /// <summary>
    /// Gets the components.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "componentName">The component name used by the operation.</param>
    /// <returns>The current components.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    List<EmailCalendarComponent> GetComponentsNamed(string componentName);
    /// <summary>
    /// Gets the events.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current events.</returns>
    List<EmailCalendarComponent> GetEvents();
    /// <summary>
    /// Gets the time zones.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current time zones.</returns>
    List<EmailCalendarComponent> GetTimeZones();
    /// <summary>
    /// Gets the extension properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension properties.</returns>
    List<EmailCalendarProperty> GetExtensionProperties();
}
