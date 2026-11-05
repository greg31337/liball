using System;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlParseDiagnosticBuffer
{
    internal HtmlDiagnostic[] values;
    internal int count;
    internal int maximum;
    internal long dropped;

    internal HtmlParseDiagnosticBuffer(int maximum)
    {
        this.maximum = maximum;
        int capacity = maximum < 8 ? maximum : 8;
        this.values = new HtmlDiagnostic[capacity];
        this.count = 0;
        this.dropped = 0;
    }

    internal void Add(HtmlDiagnostic value)
    {
        if (this.count == this.maximum)
        {
            this.dropped = this.dropped + 1;
            return;
        }

        this.EnsureCapacity(this.count + 1);
        this.values[this.count] = value;
        this.count = this.count + 1;
    }

    internal void AddDetails(HtmlDiagnosticCode code, HtmlDiagnosticSeverity severity,
        string message, HtmlSourceSpan span)
    {
        this.Add(HtmlDiagnostic.Create(code, severity, message, span));
    }

    internal void AddRange(HtmlDiagnostic[] additions)
    {
        int index = 0;
        while (index < additions.Length)
        {
            this.Add(additions[index]);
            index = index + 1;
        }
    }

    internal HtmlDiagnostic[] GetDiagnostics()
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[this.count];
        Array.Copy(this.values, result, this.count);
        return result;
    }

    internal bool WasTruncated()
    {
        return this.dropped > 0;
    }

    internal void EnsureCapacity(int required)
    {
        if (required <= this.values.Length)
        {
            return;
        }

        int capacity = BufferCapacity.GetNext(this.values.Length, required,
            this.maximum);

        HtmlDiagnostic[] expanded = new HtmlDiagnostic[capacity];
        Array.Copy(this.values, expanded, this.count);
        this.values = expanded;
    }
}
