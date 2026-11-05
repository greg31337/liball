using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarComponent"/>.
/// </summary>
public interface IEmailCalendarComponentContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
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
    /// Determines whether the uid is present or available.
    /// </summary>
    /// <returns><c>true</c> when the uid is available; otherwise, <c>false</c>.</returns>
    bool HasUid();
    /// <summary>
    /// Gets the uid.
    /// </summary>
    /// <returns>The current uid.</returns>
    string? GetUid();
    /// <summary>
    /// Gets the organizer.
    /// </summary>
    /// <returns>The current organizer.</returns>
    EmailCalendarParticipant? GetOrganizer();
    /// <summary>
    /// Gets the attendees.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attendees.</returns>
    List<EmailCalendarParticipant> GetAttendees();
    /// <summary>
    /// Gets the start.
    /// </summary>
    /// <returns>The current start.</returns>
    EmailCalendarDateTime? GetStart();
    /// <summary>
    /// Gets the end.
    /// </summary>
    /// <returns>The current end.</returns>
    EmailCalendarDateTime? GetEnd();
    /// <summary>
    /// Gets the due.
    /// </summary>
    /// <returns>The current due.</returns>
    EmailCalendarDateTime? GetDue();
    /// <summary>
    /// Gets the stamp.
    /// </summary>
    /// <returns>The current stamp.</returns>
    EmailCalendarDateTime? GetStamp();
    /// <summary>
    /// Gets the created.
    /// </summary>
    /// <returns>The current created.</returns>
    EmailCalendarDateTime? GetCreated();
    /// <summary>
    /// Gets the last modified.
    /// </summary>
    /// <returns>The current last modified.</returns>
    EmailCalendarDateTime? GetLastModified();
    /// <summary>
    /// Determines whether the sequence is present or available.
    /// </summary>
    /// <returns><c>true</c> when the sequence is available; otherwise, <c>false</c>.</returns>
    bool HasSequence();
    /// <summary>
    /// Gets the sequence.
    /// </summary>
    /// <returns>The current sequence.</returns>
    int GetSequence();
    /// <summary>
    /// Gets the status name.
    /// </summary>
    /// <returns>The current status name.</returns>
    string? GetStatusName();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailCalendarStatus GetStatus();
    /// <summary>
    /// Gets the time zone ID.
    /// </summary>
    /// <returns>The current time zone ID.</returns>
    string? GetTimeZoneId();
    /// <summary>
    /// Gets the extension properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extension properties.</returns>
    List<EmailCalendarProperty> GetExtensionProperties();
}
