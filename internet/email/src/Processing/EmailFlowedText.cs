using System;
using System.Collections.Generic;
using System.Text;

namespace email;

class EmailFlowedPhysicalLine
    {
        string content;
        string ending;
        int quoteDepth;
        bool flowed;
        bool signatureSeparator;

        internal EmailFlowedPhysicalLine(string content, string ending, int quoteDepth, bool flowed, bool signatureSeparator)
        {
            this.content = content;
            this.ending = ending;
            this.quoteDepth = quoteDepth;
            this.flowed = flowed;
            this.signatureSeparator = signatureSeparator;
        }

        internal string GetContent() { return content; }
        internal string GetEnding() { return ending; }
        internal int GetQuoteDepth() { return quoteDepth; }
        internal bool IsFlowed() { return flowed; }
        internal bool IsSignatureSeparator() { return signatureSeparator; }
    }

class EmailFlowedSourceLine
    {
        string value;

        internal EmailFlowedSourceLine(string value)
        {
            this.value = value;
        }

        internal string GetValue() { return value; }
}
