using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
internal class EmailForwardHeaderLine
{
    string name;
    string value;

    internal EmailForwardHeaderLine(string name, string value)
    {
        this.name = name;
        this.value = value;
    }

    internal string GetName()
    {
        return name;
    }

    internal string GetValue()
    {
        return value;
    }
}
