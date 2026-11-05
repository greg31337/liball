using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarParticipant"/>.
/// </summary>
public interface IEmailCalendarParticipantContract
{
    /// <summary>
    /// Gets the calendar address.
    /// </summary>
    /// <returns>The current calendar address.</returns>
    string? GetCalendarAddress();
    /// <summary>
    /// Determines whether the current value or state is mailto.
    /// </summary>
    /// <returns><c>true</c> when the value or state is mailto; otherwise, <c>false</c>.</returns>
    bool IsMailto();
    /// <summary>
    /// Determines whether the common name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the common name is available; otherwise, <c>false</c>.</returns>
    bool HasCommonName();
    /// <summary>
    /// Gets the common name.
    /// </summary>
    /// <returns>The current common name.</returns>
    string? GetCommonName();
    /// <summary>
    /// Determines whether the role is present or available.
    /// </summary>
    /// <returns><c>true</c> when the role is available; otherwise, <c>false</c>.</returns>
    bool HasRole();
    /// <summary>
    /// Gets the role.
    /// </summary>
    /// <returns>The current role.</returns>
    string? GetRole();
    /// <summary>
    /// Determines whether the participation status is present or available.
    /// </summary>
    /// <returns><c>true</c> when the participation status is available; otherwise, <c>false</c>.</returns>
    bool HasParticipationStatus();
    /// <summary>
    /// Gets the participation status.
    /// </summary>
    /// <returns>The current participation status.</returns>
    string? GetParticipationStatus();
    /// <summary>
    /// Determines whether the rsvp is present or available.
    /// </summary>
    /// <returns><c>true</c> when the rsvp is available; otherwise, <c>false</c>.</returns>
    bool HasRsvp();
    /// <summary>
    /// Gets the rsvp.
    /// </summary>
    /// <returns><c>true</c> when the rsvp setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetRsvp();
    /// <summary>
    /// Gets the property.
    /// </summary>
    /// <returns>The current property.</returns>
    EmailCalendarProperty GetProperty();
}
