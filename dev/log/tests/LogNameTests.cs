using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogNameTests
{
    [Test]
    public void ValidateFieldName_AcceptsDottedFieldNames()
    {
        Assert.That(LogName.IsValidFieldName("user_id"), Is.True);
        Assert.That(LogName.IsValidFieldName("http.status"), Is.True);
        Assert.That(LogName.IsValidFieldName("_internal.value2"), Is.True);
        Assert.That(LogName.ValidateFieldName("request.id"), Is.EqualTo(""));
    }

    [Test]
    public void ValidateFieldName_RejectsUnsafeFieldNames()
    {
        Assert.That(LogName.ValidateFieldName(null), Is.EqualTo("field name is required"));
        Assert.That(LogName.ValidateFieldName(""), Is.EqualTo("field name is required"));
        Assert.That(LogName.ValidateFieldName(" user"), Is.EqualTo("field name cannot contain leading or trailing whitespace"));
        Assert.That(LogName.ValidateFieldName("1user"), Is.EqualTo("field name must start with a letter or underscore"));
        Assert.That(LogName.ValidateFieldName("user-name"), Is.EqualTo("field name can contain only letters, digits, underscores, and dots"));
        Assert.That(LogName.ValidateFieldName("user..id"), Is.EqualTo("field name cannot contain empty segments"));
        Assert.That(LogName.ValidateFieldName("user."), Is.EqualTo("field name cannot contain empty segments"));
    }

    [Test]
    public void ValidateLoggerName_AcceptsDottedCategoryNames()
    {
        Assert.That(LogName.IsValidLoggerName("Application"), Is.True);
        Assert.That(LogName.IsValidLoggerName("Application.Services.Worker2"), Is.True);
        Assert.That(LogName.IsValidLoggerName("_internal.Logger"), Is.True);
        Assert.That(LogName.ValidateLoggerName("Application.Services"), Is.EqualTo(""));
    }

    [Test]
    public void ValidateLoggerName_RejectsUnsafeLoggerNames()
    {
        Assert.That(LogName.ValidateLoggerName(null), Is.EqualTo("logger name is required"));
        Assert.That(LogName.ValidateLoggerName(""), Is.EqualTo("logger name is required"));
        Assert.That(LogName.ValidateLoggerName(" Application"), Is.EqualTo("logger name cannot contain leading or trailing whitespace"));
        Assert.That(LogName.ValidateLoggerName("1Application"), Is.EqualTo("logger name must start with a letter or underscore"));
        Assert.That(LogName.ValidateLoggerName("Application Worker"), Is.EqualTo("logger name can contain only letters, digits, underscores, and dots"));
        Assert.That(LogName.ValidateLoggerName("Application..Worker"), Is.EqualTo("logger name cannot contain empty segments"));
        Assert.That(LogName.ValidateLoggerName("Application."), Is.EqualTo("logger name cannot contain empty segments"));
    }

    [Test]
    public void NormalizeLoggerName_TrimsNullAndWhitespace()
    {
        Assert.That(LogName.NormalizeLoggerName(null), Is.EqualTo(""));
        Assert.That(LogName.NormalizeLoggerName(" Application.Services "), Is.EqualTo("Application.Services"));
    }

    [Test]
    public void LogField_UsesSharedFieldNameValidation()
    {
        Assert.That(LogField.ValidateName("bad name"), Is.EqualTo(LogName.ValidateFieldName("bad name")));
        Assert.That(LogField.IsValidName("request.id"), Is.EqualTo(LogName.IsValidFieldName("request.id")));
    }
}
