using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a calendar component.
/// </summary>
public class EmailCalendarComponent : IEmailCalendarComponentContract
{
    string name;
    List<EmailCalendarProperty> properties;
    List<EmailCalendarComponent> components;
    /// <summary>
    /// Creates a new calendar component.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "properties">The properties used by the operation.</param>
    /// <param name = "components">The components used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCalendarComponent(string name, List<EmailCalendarProperty> properties, List<EmailCalendarComponent> components)
    {
        if (!EmailCalendarSyntax.IsName(name))
            throw new ArgumentException("A calendar component name must be an iCalendar name token.", "name");
        if (properties == null)
            throw new ArgumentNullException("properties");
        if (components == null)
            throw new ArgumentNullException("components");
        this.name = name.ToUpperInvariant();
        this.properties = EmailCalendarSyntax.CopyProperties(properties, "properties");
        this.components = EmailCalendarSyntax.CopyComponents(components, "components");
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    public string GetName()
    {
        return name;
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
    /// Determines whether the uid is present or available.
    /// </summary>
    /// <returns><c>true</c> when the uid is available; otherwise, <c>false</c>.</returns>
    public bool HasUid()
    {
        return GetUid() != null;
    }

    /// <summary>
    /// Gets the uid.
    /// </summary>
    /// <returns>The current uid.</returns>
    public string? GetUid()
    {
        return EmailCalendarValueParser.GetText(GetFirstProperty("UID"));
    }

    /// <summary>
    /// Gets the organizer.
    /// </summary>
    /// <returns>The current organizer.</returns>
    public EmailCalendarParticipant? GetOrganizer()
    {
        return EmailCalendarValueParser.ParseParticipant(GetFirstProperty("ORGANIZER"));
    }

    /// <summary>
    /// Gets the attendees.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attendees.</returns>
    public List<EmailCalendarParticipant> GetAttendees()
    {
        List<EmailCalendarParticipant> result = new List<EmailCalendarParticipant>();
        List<EmailCalendarProperty> values = GetPropertiesNamed("ATTENDEE");
        int index = 0;
        while (index < values.Count)
        {
            EmailCalendarParticipant? participant = EmailCalendarValueParser.ParseParticipant(values[index]);
            if (participant != null)
                result.Add(participant);
            index++;
        }

        return result;
    }

    /// <summary>
    /// Gets the start.
    /// </summary>
    /// <returns>The current start.</returns>
    public EmailCalendarDateTime? GetStart()
    {
        return EmailCalendarValueParser.ParseDate(GetFirstProperty("DTSTART"));
    }

    /// <summary>
    /// Gets the end.
    /// </summary>
    /// <returns>The current end.</returns>
    public EmailCalendarDateTime? GetEnd()
    {
        return EmailCalendarValueParser.ParseDate(GetFirstProperty("DTEND"));
    }

    /// <summary>
    /// Gets the due.
    /// </summary>
    /// <returns>The current due.</returns>
    public EmailCalendarDateTime? GetDue()
    {
        return EmailCalendarValueParser.ParseDate(GetFirstProperty("DUE"));
    }

    /// <summary>
    /// Gets the stamp.
    /// </summary>
    /// <returns>The current stamp.</returns>
    public EmailCalendarDateTime? GetStamp()
    {
        return EmailCalendarValueParser.ParseDate(GetFirstProperty("DTSTAMP"));
    }

    /// <summary>
    /// Gets the created.
    /// </summary>
    /// <returns>The current created.</returns>
    public EmailCalendarDateTime? GetCreated()
    {
        return EmailCalendarValueParser.ParseDate(GetFirstProperty("CREATED"));
    }

    /// <summary>
    /// Gets the last modified.
    /// </summary>
    /// <returns>The current last modified.</returns>
    public EmailCalendarDateTime? GetLastModified()
    {
        return EmailCalendarValueParser.ParseDate(GetFirstProperty("LAST-MODIFIED"));
    }

    /// <summary>
    /// Determines whether the sequence is present or available.
    /// </summary>
    /// <returns><c>true</c> when the sequence is available; otherwise, <c>false</c>.</returns>
    public bool HasSequence()
    {
        return EmailCalendarValueParser.ParseNonnegativeInteger(GetFirstProperty("SEQUENCE")) >= 0;
    }

    /// <summary>
    /// Gets the sequence.
    /// </summary>
    /// <returns>The current sequence.</returns>
    public int GetSequence()
    {
        return HasSequence() ? EmailCalendarValueParser.ParseNonnegativeInteger(GetFirstProperty("SEQUENCE")) : 0;
    }

    /// <summary>
    /// Gets the status name.
    /// </summary>
    /// <returns>The current status name.</returns>
    public string? GetStatusName()
    {
        return EmailCalendarValueParser.GetToken(GetFirstProperty("STATUS"));
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailCalendarStatus GetStatus()
    {
        return EmailCalendarValueParser.ParseStatus(GetStatusName());
    }

    /// <summary>
    /// Gets the time zone ID.
    /// </summary>
    /// <returns>The current time zone ID.</returns>
    public string? GetTimeZoneId()
    {
        return EmailCalendarValueParser.GetText(GetFirstProperty("TZID"));
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
            if (!EmailCalendarValueParser.IsKnownComponentProperty(properties[index].GetName()))
                result.Add(properties[index]);
            index++;
        }

        return result;
    }
}
