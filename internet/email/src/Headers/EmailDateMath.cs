namespace email;

internal class EmailDateMath
{
    internal static bool IsLeapYear(int year)
    {
        return year % 4 == 0 && year % 100 != 0 || year % 400 == 0;
    }

    internal static int GetDaysInMonth(int year, int month)
    {
        if (month == 2)
        {
            return IsLeapYear(year) ? 29 : 28;
        }

        if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            return 30;
        }

        return 31;
    }

    internal static EmailDateDayOfWeek GetDayOfWeek(int year, int month, int day)
    {
        int[] offsets = new int[] { 0, 3, 2, 5, 0, 3, 5, 1, 4, 6, 2, 4 };
        long adjustedYear = year;
        if (month < 3)
        {
            adjustedYear--;
        }

        long value = adjustedYear + adjustedYear / 4 - adjustedYear / 100 + adjustedYear / 400 + offsets[month - 1] + day;
        return (EmailDateDayOfWeek)(value % 7);
    }
}
