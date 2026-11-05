using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a delivery status code.
/// </summary>
public class EmailDeliveryStatusCode : IEmailDeliveryStatusCodeContract
{
    int statusClass;
    int subject;
    int detail;
    string text;
    /// <summary>
    /// Creates a new delivery status code.
    /// </summary>
    /// <param name = "statusClass">The status class used by the operation.</param>
    /// <param name = "subject">The subject used by the operation.</param>
    /// <param name = "detail">The detail used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDeliveryStatusCode(int statusClass, int subject, int detail)
    {
        if (statusClass != 2 && statusClass != 4 && statusClass != 5)
            throw new ArgumentOutOfRangeException("statusClass");
        if (subject < 0 || subject > 999)
            throw new ArgumentOutOfRangeException("subject");
        if (detail < 0 || detail > 999)
            throw new ArgumentOutOfRangeException("detail");
        this.statusClass = statusClass;
        this.subject = subject;
        this.detail = detail;
        text = statusClass + "." + subject + "." + detail;
    }

    /// <summary>
    /// Gets the status class.
    /// </summary>
    /// <returns>The current status class.</returns>
    public int GetStatusClass()
    {
        return statusClass;
    }

    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    public int GetSubject()
    {
        return subject;
    }

    /// <summary>
    /// Gets the detail.
    /// </summary>
    /// <returns>The current detail.</returns>
    public int GetDetail()
    {
        return detail;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string GetText()
    {
        return text;
    }
}
