using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCalendarWriter"/>.
/// </summary>
public interface IEmailCalendarWriterContract
{
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] WriteWithCalendar(EmailCalendar calendar);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    byte[] WriteWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options);
    /// <summary>
    /// Creates an entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity CreateEntityWithCalendar(EmailCalendar calendar);
    /// <summary>
    /// Creates an entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    MimeEntity CreateEntityWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options);
    /// <summary>
    /// Creates an imip entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    MimeEntity CreateImipEntityWithCalendar(EmailCalendar calendar);
    /// <summary>
    /// Creates an imip entity configured for the requested behavior.
    /// </summary>
    /// <param name = "calendar">The calendar used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The MIME entity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    MimeEntity CreateImipEntityWithCalendarAndOptions(EmailCalendar calendar, EmailCalendarWriteOptions options);
}
