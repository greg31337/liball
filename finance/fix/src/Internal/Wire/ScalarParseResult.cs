namespace fix;

internal class ScalarParseResult
{
    internal bool Success;
    internal FixFieldType FieldType;
    internal int IntegerValue;
    internal decimal DecimalValue;
    internal int FractionalDigits;
    internal char CharacterValue;
    internal bool BooleanValue;
    internal string TextValue = "";
    internal string[] Values = [];
    internal int Consumed;
    internal int Required;
    internal int ErrorIndex = -1;
    internal string ErrorCode = "";
}
