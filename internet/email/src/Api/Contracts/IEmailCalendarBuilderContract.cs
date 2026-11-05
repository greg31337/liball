using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarBuilder"/>.
/// </summary>
public interface IEmailCalendarBuilderContract
{
    /// <summary>
    /// Sets the method used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMethod(string value);
    /// <summary>
    /// Clears the stored method value or values.
    /// </summary>
    void ClearMethod();
    /// <summary>
    /// Sets the uid used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    void SetUid(string value);
    /// <summary>
    /// Sets the organizer used by this instance.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    void SetOrganizer(string calendarAddress);
    /// <summary>
    /// Sets the organizer used by this instance.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    /// <param name = "commonName">The common name used by the operation.</param>
    void SetOrganizerWithCommonName(string calendarAddress, string? commonName);
    /// <summary>
    /// Adds an attendee to the current collection or model.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    void AddAttendee(string calendarAddress);
    /// <summary>
    /// Adds an attendee to the current collection or model.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    /// <param name = "commonName">The common name used by the operation.</param>
    /// <param name = "role">The role used by the operation.</param>
    /// <param name = "participationStatus">The participation status used by the operation.</param>
    /// <param name = "hasRsvp">Whether to enable the has rsvp behavior.</param>
    /// <param name = "rsvp">Whether to enable the rsvp behavior.</param>
    void AddAttendeeWithDetails(string calendarAddress, string? commonName, string? role, string? participationStatus, bool hasRsvp, bool rsvp);
    /// <summary>
    /// Sets the start used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetStart(EmailCalendarDateTime value);
    /// <summary>
    /// Sets the end used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetEnd(EmailCalendarDateTime value);
    /// <summary>
    /// Sets the due used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetDue(EmailCalendarDateTime value);
    /// <summary>
    /// Sets the stamp used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetStamp(EmailCalendarDateTime value);
    /// <summary>
    /// Sets the sequence used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetSequence(int value);
    /// <summary>
    /// Sets the status used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetStatus(string value);
    /// <summary>
    /// Adds a calendar extension to the current collection or model.
    /// </summary>
    /// <param name = "property">The property used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddCalendarExtension(EmailCalendarProperty property);
    /// <summary>
    /// Adds a component extension to the current collection or model.
    /// </summary>
    /// <param name = "property">The property used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddComponentExtension(EmailCalendarProperty property);
    /// <summary>
    /// Adds a time zone to the current collection or model.
    /// </summary>
    /// <param name = "component">The component used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddTimeZone(EmailCalendarComponent component);
    /// <summary>
    /// Adds a child component to the current collection or model.
    /// </summary>
    /// <param name = "component">The component used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void AddChildComponent(EmailCalendarComponent component);
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <returns>The calendar produced by the operation.</returns>
    EmailCalendar Build();
}
