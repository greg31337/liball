using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Builds calendar from caller-supplied values.
/// </summary>
public class EmailCalendarBuilder : IEmailCalendarBuilderContract
{
    List<EmailCalendarProperty> calendarProperties;
    List<EmailCalendarProperty> componentProperties;
    List<EmailCalendarComponent> calendarComponents;
    List<EmailCalendarComponent> componentChildren;
    string componentName;
    /// <summary>
    /// Creates a new calendar builder.
    /// </summary>
    /// <param name = "productIdentifier">The product identifier used by the operation.</param>
    /// <param name = "componentName">The component name used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public EmailCalendarBuilder(string productIdentifier, string componentName)
    {
        if (productIdentifier == null || productIdentifier.Length == 0)
            throw new ArgumentException("A product identifier is required.", "productIdentifier");
        if (!EmailCalendarSyntax.IsName(componentName) || string.Equals(componentName, "VCALENDAR", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A non-VCALENDAR component name is required.", "componentName");
        calendarProperties = new List<EmailCalendarProperty>();
        calendarProperties.Add(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("VERSION", "2.0"));
        calendarProperties.Add(new EmailCalendarPropertyOperations().CreateText("PRODID", productIdentifier));
        componentProperties = new List<EmailCalendarProperty>();
        calendarComponents = new List<EmailCalendarComponent>();
        componentChildren = new List<EmailCalendarComponent>();
        this.componentName = componentName.ToUpperInvariant();
    }

    /// <summary>
    /// Sets the method used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMethod(string value)
    {
        RequireToken(value, "value");
        SetCalendarSingleton(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("METHOD", value.ToUpperInvariant()));
    }

    /// <summary>
    /// Clears the stored method value or values.
    /// </summary>
    public void ClearMethod()
    {
        Remove(calendarProperties, "METHOD");
    }

    /// <summary>
    /// Sets the uid used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public void SetUid(string value)
    {
        if (value == null || value.Length == 0)
            throw new ArgumentException("A UID is required.", "value");
        SetComponentSingleton(new EmailCalendarPropertyOperations().CreateText("UID", value));
    }

    /// <summary>
    /// Sets the organizer used by this instance.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    public void SetOrganizer(string calendarAddress)
    {
        SetOrganizerWithCommonName(calendarAddress, null);
    }

    /// <summary>
    /// Sets the organizer used by this instance.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    /// <param name = "commonName">The common name used by the operation.</param>
    public void SetOrganizerWithCommonName(string calendarAddress, string? commonName)
    {
        SetComponentSingleton(ParticipantProperty("ORGANIZER", calendarAddress, commonName, null, null, false, false));
    }

    /// <summary>
    /// Adds an attendee to the current collection or model.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    public void AddAttendee(string calendarAddress)
    {
        AddAttendeeWithDetails(calendarAddress, null, null, null, false, false);
    }

    /// <summary>
    /// Adds an attendee to the current collection or model.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    /// <param name = "commonName">The common name used by the operation.</param>
    /// <param name = "role">The role used by the operation.</param>
    /// <param name = "participationStatus">The participation status used by the operation.</param>
    /// <param name = "hasRsvp">Whether to enable the has rsvp behavior.</param>
    /// <param name = "rsvp">Whether to enable the rsvp behavior.</param>
    public void AddAttendeeWithDetails(string calendarAddress, string? commonName, string? role, string? participationStatus, bool hasRsvp, bool rsvp)
    {
        componentProperties.Add(ParticipantProperty("ATTENDEE", calendarAddress, commonName, role, participationStatus, hasRsvp, rsvp));
    }

    /// <summary>
    /// Sets the start used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetStart(EmailCalendarDateTime value)
    {
        SetDate("DTSTART", value);
    }

    /// <summary>
    /// Sets the end used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetEnd(EmailCalendarDateTime value)
    {
        SetDate("DTEND", value);
    }

    /// <summary>
    /// Sets the due used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetDue(EmailCalendarDateTime value)
    {
        SetDate("DUE", value);
    }

    /// <summary>
    /// Sets the stamp used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetStamp(EmailCalendarDateTime value)
    {
        SetDate("DTSTAMP", value);
    }

    /// <summary>
    /// Sets the sequence used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetSequence(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException("value");
        SetComponentSingleton(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("SEQUENCE", value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Sets the status used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetStatus(string value)
    {
        RequireToken(value, "value");
        SetComponentSingleton(new EmailCalendarPropertyOperations().CreateWithNameAndRawValue("STATUS", value.ToUpperInvariant()));
    }

    /// <summary>
    /// Adds a calendar extension to the current collection or model.
    /// </summary>
    /// <param name = "property">The property used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddCalendarExtension(EmailCalendarProperty property)
    {
        if (property == null)
            throw new ArgumentNullException("property");
        calendarProperties.Add(property);
    }

    /// <summary>
    /// Adds a component extension to the current collection or model.
    /// </summary>
    /// <param name = "property">The property used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddComponentExtension(EmailCalendarProperty property)
    {
        if (property == null)
            throw new ArgumentNullException("property");
        componentProperties.Add(property);
    }

    /// <summary>
    /// Adds a time zone to the current collection or model.
    /// </summary>
    /// <param name = "component">The component used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddTimeZone(EmailCalendarComponent component)
    {
        if (component == null)
            throw new ArgumentNullException("component");
        if (!string.Equals(component.GetName(), "VTIMEZONE", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A time-zone component must be VTIMEZONE.", "component");
        calendarComponents.Add(component);
    }

    /// <summary>
    /// Adds a child component to the current collection or model.
    /// </summary>
    /// <param name = "component">The component used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddChildComponent(EmailCalendarComponent component)
    {
        if (component == null)
            throw new ArgumentNullException("component");
        componentChildren.Add(component);
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <returns>The calendar produced by the operation.</returns>
    public EmailCalendar Build()
    {
        List<EmailCalendarComponent> components = new List<EmailCalendarComponent>(calendarComponents);
        components.Add(new EmailCalendarComponent(componentName, componentProperties, componentChildren));
        return new EmailCalendar(calendarProperties, components);
    }

    void SetDate(string name, EmailCalendarDateTime value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        List<EmailCalendarParameter> parameters = new List<EmailCalendarParameter>();
        if (value.GetValueType() == EmailCalendarValueType.Date)
            parameters.Add(new EmailCalendarParameterOperations().CreateWithNameAndValue("VALUE", "DATE"));
        else if (value.HasTimeZoneId())
            parameters.Add(new EmailCalendarParameterOperations().CreateWithNameAndValue("TZID", value.GetTimeZoneId() ?? throw new InvalidOperationException()));
        SetComponentSingleton(new EmailCalendarPropertyOperations().CreateWithNameAndParametersAndRawValue(name, parameters, value.GetOriginalValue() ?? throw new InvalidOperationException()));
    }

    static EmailCalendarProperty ParticipantProperty(string name, string calendarAddress, string? commonName, string? role, string? participationStatus, bool hasRsvp, bool rsvp)
    {
        if (calendarAddress == null || calendarAddress.Length == 0 || calendarAddress.IndexOf('\r') >= 0 || calendarAddress.IndexOf('\n') >= 0)
            throw new ArgumentException("A participant calendar address is required.", "calendarAddress");
        List<EmailCalendarParameter> parameters = new List<EmailCalendarParameter>();
        if (commonName != null)
            parameters.Add(new EmailCalendarParameterOperations().CreateWithNameAndValue("CN", commonName));
        if (role != null)
        {
            RequireToken(role, "role");
            parameters.Add(new EmailCalendarParameterOperations().CreateWithNameAndValue("ROLE", role.ToUpperInvariant()));
        }

        if (participationStatus != null)
        {
            RequireToken(participationStatus, "participationStatus");
            parameters.Add(new EmailCalendarParameterOperations().CreateWithNameAndValue("PARTSTAT", participationStatus.ToUpperInvariant()));
        }

        if (hasRsvp)
            parameters.Add(new EmailCalendarParameterOperations().CreateWithNameAndValue("RSVP", rsvp ? "TRUE" : "FALSE"));
        return new EmailCalendarPropertyOperations().CreateWithNameAndParametersAndRawValue(name, parameters, calendarAddress);
    }

    void SetCalendarSingleton(EmailCalendarProperty property)
    {
        Remove(calendarProperties, property.GetName());
        calendarProperties.Add(property);
    }

    void SetComponentSingleton(EmailCalendarProperty property)
    {
        Remove(componentProperties, property.GetName());
        componentProperties.Add(property);
    }

    static void Remove(List<EmailCalendarProperty> properties, string? name)
    {
        int index = properties.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(properties[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
                properties.RemoveAt(index);
            index--;
        }
    }

    static void RequireToken(string value, string parameterName)
    {
        if (!EmailCalendarSyntax.IsName(value))
            throw new ArgumentException("An iCalendar token is required.", parameterName);
    }
}
