using System.Threading;

namespace log;

internal class LogScopeStorage
{
    internal AsyncLocal<LogScopeFrame?> CurrentFrame = new AsyncLocal<LogScopeFrame?>();

    internal LogScopeFrame? Current()
    {
        return CurrentFrame.Value;
    }

    internal void SetCurrent(LogScopeFrame? frame)
    {
        CurrentFrame.Value = frame;
    }

    internal void Clear()
    {
        CurrentFrame.Value = null;
    }

    internal LogScopeFrame Push(LogField[] fields)
    {
        LogScopeFrame frame = new LogScopeFrame(CurrentFrame.Value, fields);
        CurrentFrame.Value = frame;
        return frame;
    }

    internal LogScope Begin(LogField[] fields)
    {
        LogScopeFrame frame = Push(fields);
        return new LogScope(this, frame);
    }

    internal LogScope BeginField(string name, object? value)
    {
        LogField field = LogField.ValueField(name, LogTemplateBinder.ToLogValue(value));
        return Begin(new LogField[] { field });
    }

    internal LogScope BeginLogField(LogField field)
    {
        return Begin(new LogField[] { field });
    }

    internal void Pop(LogScopeFrame? frame)
    {
        if (frame == null)
        {
            return;
        }

        frame.IsDisposed = true;

        if (CurrentFrame.Value == frame)
        {
            CurrentFrame.Value = LogScopeStorage.FirstActiveParent(frame.Parent);
        }
    }

    internal LogField[] GetCurrentFields()
    {
        return LogScopeStorage.Flatten(CurrentFrame.Value);
    }

    internal static LogScopeFrame? FirstActiveParent(LogScopeFrame? frame)
    {
        LogScopeFrame? current = frame;

        while (current != null)
        {
            if (!current.IsDisposed)
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    internal static LogField[] Flatten(LogScopeFrame? frame)
    {
        int fieldCount = LogScopeStorage.CountFields(frame);
        LogField[] fields = new LogField[fieldCount];
        int index = fieldCount;
        LogScopeFrame? current = frame;

        while (current != null)
        {
            if (!current.IsDisposed)
            {
                LogField[] currentFields = Logger.CopyFields(current.Fields);
                int currentIndex = currentFields.Length - 1;
                while (currentIndex >= 0)
                {
                    index = index - 1;
                    fields[index] = currentFields[currentIndex];
                    currentIndex = currentIndex - 1;
                }
            }

            current = current.Parent;
        }

        return fields;
    }

    internal static int CountFields(LogScopeFrame? frame)
    {
        int count = 0;
        LogScopeFrame? current = frame;

        while (current != null)
        {
            if (!current.IsDisposed && current.Fields != null)
            {
                count = count + current.Fields.Length;
            }

            current = current.Parent;
        }

        return count;
    }
}
