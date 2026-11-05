using System;

namespace log;

/// <summary>
/// Defines convenience writes for the built-in log levels.
/// </summary>
public interface ILoggerLevels
{
    /// <summary>Writes a trace event.</summary>
    LogResult Trace(string messageTemplate);
    /// <summary>Writes a trace event with values.</summary>
    LogResult TraceWithValues(string messageTemplate, object?[]? values);
    /// <summary>Writes a trace event with an exception.</summary>
    LogResult TraceWithException(Exception exceptionValue, string messageTemplate);
    /// <summary>Writes a trace event with an exception and values.</summary>
    LogResult TraceWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values);
    /// <summary>Writes a debug event.</summary>
    LogResult Debug(string messageTemplate);
    /// <summary>Writes a debug event with values.</summary>
    LogResult DebugWithValues(string messageTemplate, object?[]? values);
    /// <summary>Writes a debug event with an exception.</summary>
    LogResult DebugWithException(Exception exceptionValue, string messageTemplate);
    /// <summary>Writes a debug event with an exception and values.</summary>
    LogResult DebugWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values);
    /// <summary>Writes an informational event.</summary>
    LogResult Info(string messageTemplate);
    /// <summary>Writes an informational event with values.</summary>
    LogResult InfoWithValues(string messageTemplate, object?[]? values);
    /// <summary>Writes an informational event with an exception.</summary>
    LogResult InfoWithException(Exception exceptionValue, string messageTemplate);
    /// <summary>Writes an informational event with an exception and values.</summary>
    LogResult InfoWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values);
    /// <summary>Writes a warning event.</summary>
    LogResult Warn(string messageTemplate);
    /// <summary>Writes a warning event with values.</summary>
    LogResult WarnWithValues(string messageTemplate, object?[]? values);
    /// <summary>Writes a warning event with an exception.</summary>
    LogResult WarnWithException(Exception exceptionValue, string messageTemplate);
    /// <summary>Writes a warning event with an exception and values.</summary>
    LogResult WarnWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values);
    /// <summary>Writes an error event.</summary>
    LogResult Error(string messageTemplate);
    /// <summary>Writes an error event with values.</summary>
    LogResult ErrorWithValues(string messageTemplate, object?[]? values);
    /// <summary>Writes an error event with an exception.</summary>
    LogResult ErrorWithException(Exception exceptionValue, string messageTemplate);
    /// <summary>Writes an error event with an exception and values.</summary>
    LogResult ErrorWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values);
    /// <summary>Writes a fatal event.</summary>
    LogResult Fatal(string messageTemplate);
    /// <summary>Writes a fatal event with values.</summary>
    LogResult FatalWithValues(string messageTemplate, object?[]? values);
    /// <summary>Writes a fatal event with an exception.</summary>
    LogResult FatalWithException(Exception exceptionValue, string messageTemplate);
    /// <summary>Writes a fatal event with an exception and values.</summary>
    LogResult FatalWithExceptionAndValues(Exception? exceptionValue, string messageTemplate, object?[]? values);
}
