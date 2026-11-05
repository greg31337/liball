using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a calendar participant.
/// </summary>
public class EmailCalendarParticipant : IEmailCalendarParticipantContract
{
    string? calendarAddress;
    string? commonName;
    string? role;
    string? participationStatus;
    bool hasRsvp;
    bool rsvp;
    EmailCalendarProperty property;
    /// <summary>
    /// Creates a new calendar participant.
    /// </summary>
    /// <param name = "calendarAddress">The calendar address used by the operation.</param>
    /// <param name = "commonName">The common name used by the operation.</param>
    /// <param name = "role">The role used by the operation.</param>
    /// <param name = "participationStatus">The participation status used by the operation.</param>
    /// <param name = "hasRsvp">Whether to enable the has rsvp behavior.</param>
    /// <param name = "rsvp">Whether to enable the rsvp behavior.</param>
    /// <param name = "property">The property used by the operation.</param>
    public EmailCalendarParticipant(string? calendarAddress, string? commonName, string? role, string? participationStatus, bool hasRsvp, bool rsvp, EmailCalendarProperty property)
    {
        this.calendarAddress = calendarAddress;
        this.commonName = commonName;
        this.role = role;
        this.participationStatus = participationStatus;
        this.hasRsvp = hasRsvp;
        this.rsvp = rsvp;
        this.property = property;
    }

    /// <summary>
    /// Gets the calendar address.
    /// </summary>
    /// <returns>The current calendar address.</returns>
    public string? GetCalendarAddress()
    {
        return calendarAddress;
    }

    /// <summary>
    /// Determines whether the current value or state is mailto.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mailto; otherwise, <c>false</c>.</returns>
    public bool IsMailto()
    {
        return (calendarAddress ?? throw new InvalidOperationException()).StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the common name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the common name is available; otherwise, <c>false</c>.</returns>
    public bool HasCommonName()
    {
        return commonName != null;
    }

    /// <summary>
    /// Gets the common name.
    /// </summary>
    /// <returns>The current common name.</returns>
    public string? GetCommonName()
    {
        return commonName;
    }

    /// <summary>
    /// Determines whether the role is present or available.
    /// </summary>
    /// <returns><c>true</c> when the role is available; otherwise, <c>false</c>.</returns>
    public bool HasRole()
    {
        return role != null;
    }

    /// <summary>
    /// Gets the role.
    /// </summary>
    /// <returns>The current role.</returns>
    public string? GetRole()
    {
        return role;
    }

    /// <summary>
    /// Determines whether the participation status is present or available.
    /// </summary>
    /// <returns><c>true</c> when the participation status is available; otherwise, <c>false</c>.</returns>
    public bool HasParticipationStatus()
    {
        return participationStatus != null;
    }

    /// <summary>
    /// Gets the participation status.
    /// </summary>
    /// <returns>The current participation status.</returns>
    public string? GetParticipationStatus()
    {
        return participationStatus;
    }

    /// <summary>
    /// Determines whether the rsvp is present or available.
    /// </summary>
    /// <returns><c>true</c> when the rsvp is available; otherwise, <c>false</c>.</returns>
    public bool HasRsvp()
    {
        return hasRsvp;
    }

    /// <summary>
    /// Gets the rsvp.
    /// </summary>
    /// <returns><c>true</c> when the rsvp setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetRsvp()
    {
        return rsvp;
    }

    /// <summary>
    /// Gets the property.
    /// </summary>
    /// <returns>The current property.</returns>
    public EmailCalendarProperty GetProperty()
    {
        return property;
    }
}
