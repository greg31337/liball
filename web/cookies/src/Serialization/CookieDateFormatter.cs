namespace cookies;

internal static class CookieDateFormatter
{
    internal static string FormatExpires(global::System.DateTimeOffset expires)
    {
        global::System.DateTimeOffset utc = expires.ToUniversalTime();
        string weekday = GetWeekdayName(utc.DayOfWeek);
        string month = GetMonthName(utc.Month);
        int day = utc.Day;
        int year = utc.Year;
        int hour = utc.Hour;
        int minute = utc.Minute;
        int second = utc.Second;
        char dayTens = (char)('0' + day / 10);
        char dayOnes = (char)('0' + day % 10);
        char yearThousands = (char)('0' + year / 1000);
        char yearHundreds = (char)('0' + year / 100 % 10);
        char yearTens = (char)('0' + year / 10 % 10);
        char yearOnes = (char)('0' + year % 10);
        char hourTens = (char)('0' + hour / 10);
        char hourOnes = (char)('0' + hour % 10);
        char minuteTens = (char)('0' + minute / 10);
        char minuteOnes = (char)('0' + minute % 10);
        char secondTens = (char)('0' + second / 10);
        char secondOnes = (char)('0' + second % 10);
        return $"{weekday}, {dayTens}{dayOnes} {month} {yearThousands}{yearHundreds}{yearTens}{yearOnes} " +
            $"{hourTens}{hourOnes}:{minuteTens}{minuteOnes}:{secondTens}{secondOnes} GMT";
    }

    static string GetWeekdayName(global::System.DayOfWeek weekday)
    {
        switch (weekday)
        {
            case global::System.DayOfWeek.Sunday:
                return "Sun";
            case global::System.DayOfWeek.Monday:
                return "Mon";
            case global::System.DayOfWeek.Tuesday:
                return "Tue";
            case global::System.DayOfWeek.Wednesday:
                return "Wed";
            case global::System.DayOfWeek.Thursday:
                return "Thu";
            case global::System.DayOfWeek.Friday:
                return "Fri";
            case global::System.DayOfWeek.Saturday:
                return "Sat";
            default:
                return "";
        }
    }

    static string GetMonthName(int month)
    {
        switch (month)
        {
            case 1:
                return "Jan";
            case 2:
                return "Feb";
            case 3:
                return "Mar";
            case 4:
                return "Apr";
            case 5:
                return "May";
            case 6:
                return "Jun";
            case 7:
                return "Jul";
            case 8:
                return "Aug";
            case 9:
                return "Sep";
            case 10:
                return "Oct";
            case 11:
                return "Nov";
            case 12:
                return "Dec";
            default:
                return "";
        }
    }
}
