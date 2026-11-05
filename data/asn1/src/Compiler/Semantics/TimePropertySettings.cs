namespace asn1;

internal class TimePropertySettings
{
    internal string Source = "";
    internal TimeBasicSettingKind Basic;
    internal TimeDateSettingKind Date;
    internal TimeYearSettingKind Year;
    internal TimePrecisionSettingKind Time;
    internal TimeZoneSettingKind Zone;
    internal TimeMidnightSettingKind Midnight;
    internal TimeIntervalSettingKind Interval;
    internal TimeEndpointSettingKind Endpoint;
    internal TimeRecurrenceSettingKind Recurrence;
    internal int LargeYearDigits;
    internal int FractionalDigits;
    internal int RecurrenceDigits;
    internal NormalizedConstraintNode? SourceNode;
}
