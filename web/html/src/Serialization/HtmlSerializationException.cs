using System;

namespace html;

internal class HtmlSerializationException : System.Exception
{
    internal HtmlSerializationException(string message) : base(message)
    {
    }
}
