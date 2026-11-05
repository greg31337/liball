using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a calendar.
/// </summary>
public class EmailCalendar : IEmailCalendarContract
{
    List<EmailCalendarProperty> properties;
    List<EmailCalendarComponent> components;
    /// <summary>
    /// Creates a new calendar.
    /// </summary>
    /// <param name = "properties">The properties used by the operation.</param>
    /// <param name = "components">The components used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendar(List<EmailCalendarProperty> properties, List<EmailCalendarComponent> components)
    {
        if (properties == null)
            throw new ArgumentNullException("properties");
        if (components == null)
            throw new ArgumentNullException("components");
        this.properties = EmailCalendarSyntax.CopyProperties(properties, "properties");
        this.components = EmailCalendarSyntax.CopyComponents(components, "components");
    }

    /// <summary>
    /// Gets the property count.
    /// </summary>
    /// <returns>The current property count.</returns>
    public int GetPropertyCount()
    {
        return properties.Count;
    }

    /// <summary>
    /// Gets the property at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested property.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCalendarProperty GetPropertyAt(int index)
    {
        if (index < 0 || index >= properties.Count)
            throw new ArgumentOutOfRangeException("index");
        return properties[index];
    }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current properties.</returns>
    public List<EmailCalendarProperty> GetProperties()
    {
        return new List<EmailCalendarProperty>(properties);
    }

    /// <summary>
    /// Gets the properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "propertyName">The property name used by the operation.</param>
    /// <returns>The current properties.</returns>
    public List<EmailCalendarProperty> GetPropertiesNamed(string propertyName)
    {
        return EmailCalendarSyntax.FindProperties(properties, propertyName);
    }

    /// <summary>
    /// Gets the first property.
    /// </summary>
    /// <param name = "propertyName">The property name used by the operation.</param>
    /// <returns>The current first property.</returns>
    public EmailCalendarProperty? GetFirstProperty(string propertyName)
    {
        List<EmailCalendarProperty> matches = GetPropertiesNamed(propertyName);
        return matches.Count == 0 ? null : matches[0];
    }

    /// <summary>
    /// Determines whether the method is present or available.
    /// </summary>
    /// <returns><c>true</c> when the method is available; otherwise, <c>false</c>.</returns>
    public bool HasMethod()
    {
        return GetMethodName() != null;
    }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    /// <returns>The current method name.</returns>
    public string? GetMethodName()
    {
        return EmailCalendarValueParser.GetToken(GetFirstProperty("METHOD"));
    }

    /// <summary>
    /// Gets the method.
    /// </summary>
    /// <returns>The current method.</returns>
    public EmailCalendarMethod GetMethod()
    {
        return EmailCalendarValueParser.ParseMethod(GetMethodName());
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public string? GetVersion()
    {
        return EmailCalendarValueParser.GetText(GetFirstProperty("VERSION"));
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    /// <returns>The current product identifier.</returns>
    public string? GetProductIdentifier()
    {
        return EmailCalendarValueParser.GetText(GetFirstProperty("PRODID"));
    }

    /// <summary>
    /// Gets the component count.
    /// </summary>
    /// <returns>The current component count.</returns>
    public int GetComponentCount()
    {
        return components.Count;
    }

    /// <summary>
    /// Gets the component at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested component.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCalendarComponent GetComponentAt(int index)
    {
        if (index < 0 || index >= components.Count)
            throw new ArgumentOutOfRangeException("index");
        return components[index];
    }

    /// <summary>
    /// Gets the components.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current components.</returns>
    public List<EmailCalendarComponent> GetComponents()
    {
        return new List<EmailCalendarComponent>(components);
    }

    /// <summary>
    /// Gets the components.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "componentName">The component name used by the operation.</param>
    /// <returns>The current components.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public List<EmailCalendarComponent> GetComponentsNamed(string componentName)
    {
        if (componentName == null)
            throw new ArgumentNullException("componentName");
        List<EmailCalendarComponent> result = new List<EmailCalendarComponent>();
        int index = 0;
        while (index < components.Count)
        {
            if (string.Equals(components[index].GetName(), componentName, StringComparison.OrdinalIgnoreCase))
                result.Add(components[index]);
            index++;
        }

        return result;
    }

    /// <summary>
    /// Gets the events.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current events.</returns>
    public List<EmailCalendarComponent> GetEvents()
    {
        return GetComponentsNamed("VEVENT");
    }

    /// <summary>
    /// Gets the time zones.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current time zones.</returns>
    public List<EmailCalendarComponent> GetTimeZones()
    {
        return GetComponentsNamed("VTIMEZONE");
    }

    /// <summary>
    /// Gets the extension properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension properties.</returns>
    public List<EmailCalendarProperty> GetExtensionProperties()
    {
        List<EmailCalendarProperty> result = new List<EmailCalendarProperty>();
        int index = 0;
        while (index < properties.Count)
        {
            string? name = properties[index].GetName();
            if (!string.Equals(name, "VERSION", StringComparison.OrdinalIgnoreCase) && !string.Equals(name, "PRODID", StringComparison.OrdinalIgnoreCase) && !string.Equals(name, "CALSCALE", StringComparison.OrdinalIgnoreCase) && !string.Equals(name, "METHOD", StringComparison.OrdinalIgnoreCase))
                result.Add(properties[index]);
            index++;
        }

        return result;
    }
}
