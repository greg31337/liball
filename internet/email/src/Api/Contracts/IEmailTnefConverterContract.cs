using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefConverter"/>.
/// </summary>
public interface IEmailTnefConverterContract
{
    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    EmailTnefConversionResult ConvertWithEntity(MimeEntity entity);
    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTnefConversionResult ConvertWithEntityAndOptions(MimeEntity entity, EmailTnefConversionOptions options);
    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "parsed">The parsed used by the operation.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    EmailTnefConversionResult ConvertWithParsed(EmailTnefParseResult parsed);
    /// <summary>
    /// Converts the supplied value.
    /// </summary>
    /// <param name = "parsed">The parsed used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The TNEF conversion result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailTnefConversionResult ConvertWithParsedAndOptions(EmailTnefParseResult parsed, EmailTnefConversionOptions options);
}
