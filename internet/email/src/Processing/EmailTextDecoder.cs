using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;

class EmailTextCharsetSelection
    {
        EmailCharsetDecodeResult result;
        EmailTextCharsetSource source;
        bool usedFallback;
        bool replacedUnsupportedDeclaration;

        internal EmailTextCharsetSelection(EmailCharsetDecodeResult result, EmailTextCharsetSource source, bool usedFallback, bool replacedUnsupportedDeclaration)
        {
            this.result = result;
            this.source = source;
            this.usedFallback = usedFallback;
            this.replacedUnsupportedDeclaration = replacedUnsupportedDeclaration;
        }

        internal EmailCharsetDecodeResult GetResult() { return result; }
        internal EmailTextCharsetSource GetSource() { return source; }
        internal bool UsedFallback() { return usedFallback; }
        internal bool ReplacedUnsupportedDeclaration() { return replacedUnsupportedDeclaration; }
    }

class EmailTextTransform
    {
        string text;
        bool changed;

        internal EmailTextTransform(string text, bool changed)
        {
            this.text = text;
            this.changed = changed;
        }

        internal string GetText() { return text; }
        internal bool WasChanged() { return changed; }
    }

class EmailTextCharacterScan
    {
        string text;
        int controlCharacterCount;
        int bidiControlCount;
        bool controlCharactersRemoved;
        bool bidiControlsRemoved;

        internal EmailTextCharacterScan(string text, int controlCharacterCount, int bidiControlCount, bool controlCharactersRemoved, bool bidiControlsRemoved)
        {
            this.text = text;
            this.controlCharacterCount = controlCharacterCount;
            this.bidiControlCount = bidiControlCount;
            this.controlCharactersRemoved = controlCharactersRemoved;
            this.bidiControlsRemoved = bidiControlsRemoved;
        }

        internal string GetText() { return text; }
        internal int GetControlCharacterCount() { return controlCharacterCount; }
        internal int GetBidiControlCount() { return bidiControlCount; }
        internal bool WereControlCharactersRemoved() { return controlCharactersRemoved; }
        internal bool WereBidiControlsRemoved() { return bidiControlsRemoved; }
}
