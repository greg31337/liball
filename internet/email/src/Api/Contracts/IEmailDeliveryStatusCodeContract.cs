using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatusCode"/>.
/// </summary>
public interface IEmailDeliveryStatusCodeContract
{
    /// <summary>
    /// Gets the status class.
    /// </summary>
    /// <returns>The current status class.</returns>
    int GetStatusClass();
    /// <summary>
    /// Gets the subject.
    /// </summary>
    /// <returns>The current subject.</returns>
    int GetSubject();
    /// <summary>
    /// Gets the detail.
    /// </summary>
    /// <returns>The current detail.</returns>
    int GetDetail();
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
}
