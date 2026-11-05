using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides date and time generation operations.
/// </summary>
public class DateProvider : IDateProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal DateProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("date");
    }

    /// <summary>
    /// Generates a <see cref="System.DateTime"/> from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum with the same <see cref="System.DateTimeKind"/>.</param>
    /// <returns>A value within the requested tick interval that preserves the minimum's kind.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the bounds use different kinds.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    public System.DateTime DateTime(System.DateTime minimum, System.DateTime maximum)
    {
        return DateTimeFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a <see cref="System.DateTime"/> from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum with the same <see cref="System.DateTimeKind"/>.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A value within the requested tick interval that preserves the minimum's kind.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the bounds use different kinds.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the boundary or interval is invalid.</exception>
    public System.DateTime DateTimeFromMinimumAndMaximumAndBoundary(System.DateTime minimum, System.DateTime maximum, DateBoundary boundary)
    {
        return BoundedTemporalGenerator.DateTime(RandomValue, minimum, maximum, boundary);
    }

    /// <summary>
    /// Generates a <see cref="System.DateTimeOffset"/> instant from an interval with an exclusive maximum.
    /// </summary>
    /// <remarks>Non-singleton results use a zero UTC offset so every valid runtime instant remains representable at the extreme bounds.</remarks>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The exclusive maximum instant.</param>
    /// <returns>A UTC-offset value within the requested instant interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    public System.DateTimeOffset DateTimeOffset(System.DateTimeOffset minimum, System.DateTimeOffset maximum)
    {
        return DateTimeOffsetFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a <see cref="System.DateTimeOffset"/> instant from an interval with an explicit maximum boundary.
    /// </summary>
    /// <remarks>Non-singleton results use a zero UTC offset. An inclusive singleton returns the exact minimum representation.</remarks>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The maximum instant.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A value within the requested instant interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the boundary or interval is invalid.</exception>
    public System.DateTimeOffset DateTimeOffsetFromMinimumAndMaximumAndBoundary(System.DateTimeOffset minimum, System.DateTimeOffset maximum, DateBoundary boundary)
    {
        return BoundedTemporalGenerator.DateTimeOffset(RandomValue, minimum, maximum, boundary);
    }

    /// <summary>
    /// Generates a <see cref="System.DateOnly"/> from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum date.</param>
    /// <param name="maximum">The exclusive maximum date.</param>
    /// <returns>A date within the requested day-number interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    public System.DateOnly DateOnly(System.DateOnly minimum, System.DateOnly maximum)
    {
        return DateOnlyFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a <see cref="System.DateOnly"/> from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum date.</param>
    /// <param name="maximum">The maximum date.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A date within the requested day-number interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the boundary or interval is invalid.</exception>
    public System.DateOnly DateOnlyFromMinimumAndMaximumAndBoundary(System.DateOnly minimum, System.DateOnly maximum, DateBoundary boundary)
    {
        return BoundedTemporalGenerator.DateOnly(RandomValue, minimum, maximum, boundary);
    }

    /// <summary>
    /// Generates a <see cref="System.TimeOnly"/> from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum time.</param>
    /// <param name="maximum">The exclusive maximum time.</param>
    /// <returns>A time within the requested tick interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    public System.TimeOnly TimeOnly(System.TimeOnly minimum, System.TimeOnly maximum)
    {
        return TimeOnlyFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a <see cref="System.TimeOnly"/> from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum time.</param>
    /// <param name="maximum">The maximum time.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A time within the requested tick interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the boundary or interval is invalid.</exception>
    public System.TimeOnly TimeOnlyFromMinimumAndMaximumAndBoundary(System.TimeOnly minimum, System.TimeOnly maximum, DateBoundary boundary)
    {
        return BoundedTemporalGenerator.TimeOnly(RandomValue, minimum, maximum, boundary);
    }

    /// <summary>
    /// Generates a <see cref="System.TimeSpan"/> from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum duration.</param>
    /// <param name="maximum">The exclusive maximum duration.</param>
    /// <returns>A duration within the requested tick interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    public System.TimeSpan TimeSpan(System.TimeSpan minimum, System.TimeSpan maximum)
    {
        return TimeSpanFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a <see cref="System.TimeSpan"/> from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum duration.</param>
    /// <param name="maximum">The maximum duration.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A duration within the requested tick interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the boundary or interval is invalid.</exception>
    public System.TimeSpan TimeSpanFromMinimumAndMaximumAndBoundary(System.TimeSpan minimum, System.TimeSpan maximum, DateBoundary boundary)
    {
        return BoundedTemporalGenerator.TimeSpan(RandomValue, minimum, maximum, boundary);
    }

    /// <summary>Generates a time of day from the complete representable range.</summary>
    /// <returns>A time from midnight through the final runtime tick of the day.</returns>
    public System.TimeOnly Time()
    {
        return TemporalCompletionGenerator.Time(RandomValue);
    }

    /// <summary>Generates a nonnegative duration shorter than seven days.</summary>
    /// <returns>A duration in the default convenience range.</returns>
    public System.TimeSpan Duration()
    {
        return TemporalCompletionGenerator.Duration(RandomValue);
    }

    /// <summary>
    /// Generates an instant from the complete representable <see cref="System.DateTimeOffset"/> range.
    /// </summary>
    /// <returns>A UTC-offset instant from the runtime minimum through maximum.</returns>
    public System.DateTimeOffset Anytime()
    {
        return BoundedTemporalGenerator.DateTimeOffset(RandomValue, System.DateTimeOffset.MinValue, System.DateTimeOffset.MaxValue, DateBoundary.Inclusive);
    }

    /// <summary>Generates a full-range instant aligned to a requested precision.</summary>
    /// <param name="precision">The required temporal precision.</param>
    /// <returns>A UTC-offset instant with no lower-order ticks.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the precision is invalid.</exception>
    public System.DateTimeOffset AnytimeFromPrecision(TemporalPrecision precision)
    {
        return TemporalCompletionGenerator.AnytimeFromPrecision(RandomValue, precision);
    }

    /// <summary>
    /// Generates an instant from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The exclusive maximum instant.</param>
    /// <returns>A UTC-offset instant within the interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    public System.DateTimeOffset Between(System.DateTimeOffset minimum, System.DateTimeOffset maximum)
    {
        return BetweenFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates an instant from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The maximum instant.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>An instant within the interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the boundary or interval is invalid.</exception>
    public System.DateTimeOffset BetweenFromMinimumAndMaximumAndBoundary(System.DateTimeOffset minimum, System.DateTimeOffset maximum, DateBoundary boundary)
    {
        return BoundedTemporalGenerator.DateTimeOffset(RandomValue, minimum, maximum, boundary);
    }

    /// <summary>Generates a precision-aligned instant with an exclusive maximum.</summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The exclusive maximum instant.</param>
    /// <param name="precision">The required temporal precision.</param>
    /// <returns>An aligned UTC-offset instant within the interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval, precision, or aligned result set is invalid.</exception>
    public System.DateTimeOffset BetweenFromMinimumAndMaximumAndPrecision(System.DateTimeOffset minimum, System.DateTimeOffset maximum, TemporalPrecision precision)
    {
        return BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
            minimum,
            maximum,
            DateBoundary.Exclusive,
            precision);
    }

    /// <summary>Generates a precision-aligned instant with an explicit maximum boundary.</summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The maximum instant.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <param name="precision">The required temporal precision.</param>
    /// <returns>An aligned UTC-offset instant within the interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval, boundary, precision, or aligned result set is invalid.</exception>
    public System.DateTimeOffset BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(System.DateTimeOffset minimum, System.DateTimeOffset maximum, DateBoundary boundary, TemporalPrecision precision)
    {
        return TemporalCompletionGenerator.BetweenFromMinimumAndMaximumAndBoundaryAndPrecision(
            RandomValue,
            minimum,
            maximum,
            boundary,
            precision);
    }

    /// <summary>
    /// Generates sorted instants from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The exclusive maximum instant.</param>
    /// <param name="count">The number of values to generate.</param>
    /// <returns>A sorted array whose values are within the interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count, output size, or interval is invalid.</exception>
    public System.DateTimeOffset[] Betweens(System.DateTimeOffset minimum, System.DateTimeOffset maximum, int count)
    {
        return BetweensFromMinimumAndMaximumAndCountAndBoundary(minimum, maximum, count, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates sorted instants from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The maximum instant.</param>
    /// <param name="count">The number of values to generate.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A sorted array whose values are within the interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count, output size, boundary, or interval is invalid.</exception>
    public System.DateTimeOffset[] BetweensFromMinimumAndMaximumAndCountAndBoundary(System.DateTimeOffset minimum, System.DateTimeOffset maximum, int count, DateBoundary boundary)
    {
        return RelativeDateGenerator.Betweens(RandomValue, minimum, maximum, count, boundary, Context.CapturedOptions.MaximumCount, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Generates an instant strictly before the captured reference time and no more than 365 days earlier.
    /// </summary>
    /// <returns>A UTC-offset past instant.</returns>
    /// <exception cref="FakeException">Thrown when the captured reference has no representable past instant.</exception>
    public System.DateTimeOffset Past()
    {
        return PastFromMaximumAge(System.TimeSpan.FromDays(365.0));
    }

    /// <summary>
    /// Generates an instant strictly before the captured reference time within a maximum age.
    /// </summary>
    /// <param name="maximumAge">The positive maximum distance into the past.</param>
    /// <returns>A UTC-offset past instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum age is not positive.</exception>
    /// <exception cref="FakeException">Thrown when the captured reference has no representable past instant.</exception>
    public System.DateTimeOffset PastFromMaximumAge(System.TimeSpan maximumAge)
    {
        return RelativeDateGenerator.Past(RandomValue, Context.ReferenceTime(), maximumAge);
    }

    /// <summary>
    /// Generates an instant strictly after the captured reference time and no more than 365 days later.
    /// </summary>
    /// <returns>A UTC-offset future instant.</returns>
    /// <exception cref="FakeException">Thrown when the captured reference has no representable future instant.</exception>
    public System.DateTimeOffset Future()
    {
        return FutureFromMaximumLead(System.TimeSpan.FromDays(365.0));
    }

    /// <summary>
    /// Generates an instant strictly after the captured reference time within a maximum lead.
    /// </summary>
    /// <param name="maximumLead">The positive maximum distance into the future.</param>
    /// <returns>A UTC-offset future instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum lead is not positive.</exception>
    /// <exception cref="FakeException">Thrown when the captured reference has no representable future instant.</exception>
    public System.DateTimeOffset FutureFromMaximumLead(System.TimeSpan maximumLead)
    {
        return RelativeDateGenerator.Future(RandomValue, Context.ReferenceTime(), maximumLead);
    }

    /// <summary>
    /// Generates an instant from the 24 hours ending at the captured reference time.
    /// </summary>
    /// <returns>A UTC-offset recent instant, possibly equal to the reference instant.</returns>
    public System.DateTimeOffset Recent()
    {
        return RecentFromWindow(System.TimeSpan.FromDays(1.0));
    }

    /// <summary>
    /// Generates an instant in a positive window ending at the captured reference time.
    /// </summary>
    /// <param name="window">The positive recent-time window.</param>
    /// <returns>A UTC-offset recent instant, possibly equal to the reference instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the window is not positive.</exception>
    public System.DateTimeOffset RecentFromWindow(System.TimeSpan window)
    {
        return RelativeDateGenerator.Recent(RandomValue, Context.ReferenceTime(), window);
    }

    /// <summary>
    /// Generates an instant strictly after the captured reference time within the next 24 hours.
    /// </summary>
    /// <returns>A UTC-offset soon instant.</returns>
    /// <exception cref="FakeException">Thrown when the captured reference has no representable future instant.</exception>
    public System.DateTimeOffset Soon()
    {
        return SoonFromWindow(System.TimeSpan.FromDays(1.0));
    }

    /// <summary>
    /// Generates an instant strictly after the captured reference time within a positive window.
    /// </summary>
    /// <param name="window">The positive future-time window.</param>
    /// <returns>A UTC-offset soon instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the window is not positive.</exception>
    /// <exception cref="FakeException">Thrown when the captured reference has no representable future instant.</exception>
    public System.DateTimeOffset SoonFromWindow(System.TimeSpan window)
    {
        return RelativeDateGenerator.Soon(RandomValue, Context.ReferenceTime(), window);
    }

    /// <summary>
    /// Generates a birth date whose completed age is between 18 and 80 at the captured reference date.
    /// </summary>
    /// <returns>A calendar birth date in the default adult age range.</returns>
    public System.DateOnly BirthDate()
    {
        return BirthDateFromMinimumAgeAndMaximumAge(18, 80);
    }

    /// <summary>
    /// Generates a birth date with an exact completed age at the captured reference date.
    /// </summary>
    /// <param name="age">The nonnegative completed age.</param>
    /// <returns>A birth date having the requested age.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the age is negative or cannot be represented.</exception>
    public System.DateOnly BirthDateFromAge(int age)
    {
        return BirthDateFromMinimumAgeAndMaximumAge(age, age);
    }

    /// <summary>
    /// Generates a birth date within an inclusive completed-age range at the captured reference date.
    /// </summary>
    /// <param name="minimumAge">The inclusive nonnegative minimum age.</param>
    /// <param name="maximumAge">The inclusive maximum age.</param>
    /// <returns>A birth date whose completed age is within the requested range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the ages are invalid or cannot be represented.</exception>
    public System.DateOnly BirthDateFromMinimumAgeAndMaximumAge(int minimumAge, int maximumAge)
    {
        return CalendarDateGenerator.BirthDate(RandomValue, CalendarDateGenerator.ReferenceDate(Context.ReferenceTime()), minimumAge, maximumAge);
    }

    /// <summary>
    /// Calculates completed age at the captured reference date.
    /// </summary>
    /// <param name="birthDate">The birth date to evaluate.</param>
    /// <returns>The number of completed calendar years.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the birth date is after the captured reference date.</exception>
    public int Age(System.DateOnly birthDate)
    {
        return CalendarDateGenerator.Age(birthDate, CalendarDateGenerator.ReferenceDate(Context.ReferenceTime()));
    }

    /// <summary>
    /// Generates a correlated birth date and completed age.
    /// </summary>
    /// <param name="minimumAge">The inclusive nonnegative minimum age.</param>
    /// <param name="maximumAge">The inclusive maximum age.</param>
    /// <returns>A birth-date and age pair evaluated at the captured reference date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the ages are invalid or cannot be represented.</exception>
    public BirthDateAge BirthDateAndAge(int minimumAge, int maximumAge)
    {
        System.DateOnly birthDate = BirthDateFromMinimumAgeAndMaximumAge(minimumAge, maximumAge);
        return new BirthDateAge(birthDate, Age(birthDate));
    }

    /// <summary>
    /// Returns the first anniversary on or after the captured reference date.
    /// </summary>
    /// <remarks>A February 29 anniversary follows the runtime calendar's February 28 adjustment in non-leap years.</remarks>
    /// <param name="originalDate">The original date, which cannot be after the captured reference date.</param>
    /// <returns>The next representable anniversary date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the original date is after the captured reference date.</exception>
    /// <exception cref="FakeException">Thrown when the next anniversary is outside the representable calendar.</exception>
    public System.DateOnly Anniversary(System.DateOnly originalDate)
    {
        return CalendarDateGenerator.Anniversary(originalDate, CalendarDateGenerator.ReferenceDate(Context.ReferenceTime()));
    }

    /// <summary>
    /// Returns a specified numbered anniversary of an original date.
    /// </summary>
    /// <remarks>A February 29 anniversary follows the runtime calendar's February 28 adjustment in non-leap years.</remarks>
    /// <param name="originalDate">The original calendar date.</param>
    /// <param name="years">The positive anniversary number.</param>
    /// <returns>The requested anniversary date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the anniversary number is invalid or unrepresentable.</exception>
    public System.DateOnly AnniversaryFromOriginalDateAndYears(System.DateOnly originalDate, int years)
    {
        return CalendarDateGenerator.Anniversary(originalDate, years);
    }

    /// <summary>
    /// Generates an anniversary from an inclusive range of anniversary numbers.
    /// </summary>
    /// <param name="originalDate">The original calendar date.</param>
    /// <param name="minimumYears">The inclusive positive minimum anniversary number.</param>
    /// <param name="maximumYears">The inclusive maximum anniversary number.</param>
    /// <returns>A randomly selected representable anniversary date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the anniversary range is invalid or unrepresentable.</exception>
    public System.DateOnly AnniversaryFromOriginalDateAndMinimumYearsAndMaximumYears(System.DateOnly originalDate, int minimumYears, int maximumYears)
    {
        return CalendarDateGenerator.Anniversary(RandomValue, originalDate, minimumYears, maximumYears);
    }

    /// <summary>
    /// Generates an expiry instant from one through 365 days after the captured reference time.
    /// </summary>
    /// <returns>A representable UTC-offset expiry instant.</returns>
    /// <exception cref="FakeException">Thrown when the requested default lead range is not representable.</exception>
    public System.DateTimeOffset ExpiryDate()
    {
        return ExpiryDateFromMinimumLeadAndMaximumLead(System.TimeSpan.FromDays(1.0), System.TimeSpan.FromDays(365.0));
    }

    /// <summary>
    /// Generates an expiry instant from an inclusive positive lead-time range after the captured reference time.
    /// </summary>
    /// <param name="minimumLead">The inclusive positive minimum lead time.</param>
    /// <param name="maximumLead">The inclusive positive maximum lead time.</param>
    /// <returns>A representable UTC-offset expiry instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the lead-time range is invalid.</exception>
    /// <exception cref="FakeException">Thrown when no requested expiry instant is representable.</exception>
    public System.DateTimeOffset ExpiryDateFromMinimumLeadAndMaximumLead(System.TimeSpan minimumLead, System.TimeSpan maximumLead)
    {
        return CalendarDateGenerator.ExpiryDate(RandomValue, Context.ReferenceTime(), minimumLead, maximumLead);
    }

    /// <summary>
    /// Generates a Monday-through-Friday date in the year beginning at the captured reference date.
    /// </summary>
    /// <returns>A business day from the default reference-date interval.</returns>
    /// <exception cref="FakeException">Thrown when the representable default interval contains no business day.</exception>
    public System.DateOnly BusinessDay()
    {
        System.DateOnly referenceDate = CalendarDateGenerator.ReferenceDate(Context.ReferenceTime());
        System.DateOnly maximum = referenceDate;
        if (referenceDate.DayNumber <= System.DateOnly.MaxValue.DayNumber - 365)
        {
            maximum = referenceDate.AddDays(365);
        }
        else
        {
            maximum = System.DateOnly.MaxValue;
        }

        return BusinessDayFromMinimumAndMaximumAndBoundary(referenceDate, maximum, DateBoundary.Inclusive);
    }

    /// <summary>
    /// Generates a Monday-through-Friday date from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum date.</param>
    /// <param name="maximum">The exclusive maximum date.</param>
    /// <returns>A business day within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    /// <exception cref="FakeException">Thrown when the interval contains no business day.</exception>
    public System.DateOnly BusinessDayFromMinimumAndMaximum(System.DateOnly minimum, System.DateOnly maximum)
    {
        return BusinessDayFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a Monday-through-Friday date from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum date.</param>
    /// <param name="maximum">The maximum date.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A business day within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval or boundary is invalid.</exception>
    /// <exception cref="FakeException">Thrown when the interval contains no business day.</exception>
    public System.DateOnly BusinessDayFromMinimumAndMaximumAndBoundary(System.DateOnly minimum, System.DateOnly maximum, DateBoundary boundary)
    {
        return CalendarDateGenerator.CalendarDay(RandomValue, minimum, maximum, boundary, true);
    }

    /// <summary>
    /// Generates a Saturday or Sunday in the year beginning at the captured reference date.
    /// </summary>
    /// <returns>A weekend date from the default reference-date interval.</returns>
    /// <exception cref="FakeException">Thrown when the representable default interval contains no weekend date.</exception>
    public System.DateOnly Weekend()
    {
        System.DateOnly referenceDate = CalendarDateGenerator.ReferenceDate(Context.ReferenceTime());
        System.DateOnly maximum = referenceDate;
        if (referenceDate.DayNumber <= System.DateOnly.MaxValue.DayNumber - 365)
        {
            maximum = referenceDate.AddDays(365);
        }
        else
        {
            maximum = System.DateOnly.MaxValue;
        }

        return WeekendFromMinimumAndMaximumAndBoundary(referenceDate, maximum, DateBoundary.Inclusive);
    }

    /// <summary>
    /// Generates a Saturday or Sunday from an interval with an exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum date.</param>
    /// <param name="maximum">The exclusive maximum date.</param>
    /// <returns>A weekend date within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or reversed.</exception>
    /// <exception cref="FakeException">Thrown when the interval contains no weekend date.</exception>
    public System.DateOnly WeekendFromMinimumAndMaximum(System.DateOnly minimum, System.DateOnly maximum)
    {
        return WeekendFromMinimumAndMaximumAndBoundary(minimum, maximum, DateBoundary.Exclusive);
    }

    /// <summary>
    /// Generates a Saturday or Sunday from an interval with an explicit maximum boundary.
    /// </summary>
    /// <param name="minimum">The inclusive minimum date.</param>
    /// <param name="maximum">The maximum date.</param>
    /// <param name="boundary">Whether the maximum is included.</param>
    /// <returns>A weekend date within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval or boundary is invalid.</exception>
    /// <exception cref="FakeException">Thrown when the interval contains no weekend date.</exception>
    public System.DateOnly WeekendFromMinimumAndMaximumAndBoundary(System.DateOnly minimum, System.DateOnly maximum, DateBoundary boundary)
    {
        return CalendarDateGenerator.CalendarDay(RandomValue, minimum, maximum, boundary, false);
    }

    /// <summary>
    /// Returns the calendar quarter containing the captured reference date.
    /// </summary>
    /// <returns>The current January-based fiscal quarter.</returns>
    public FiscalPeriod FiscalPeriod()
    {
        return FiscalPeriodFromFiscalYearStartMonth(1);
    }

    /// <summary>
    /// Returns the fiscal quarter containing the captured reference date.
    /// </summary>
    /// <param name="fiscalYearStartMonth">The one-based month in which the fiscal year starts.</param>
    /// <returns>The fiscal quarter containing the captured reference date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the start month is invalid.</exception>
    /// <exception cref="FakeException">Thrown when the complete period begins outside the representable calendar.</exception>
    public FiscalPeriod FiscalPeriodFromFiscalYearStartMonth(int fiscalYearStartMonth)
    {
        return CalendarDateGenerator.CurrentFiscalPeriod(CalendarDateGenerator.ReferenceDate(Context.ReferenceTime()), fiscalYearStartMonth);
    }

    /// <summary>
    /// Returns a specified fiscal quarter using the year in which its fiscal year starts.
    /// </summary>
    /// <param name="fiscalYear">The calendar year in which the fiscal year starts.</param>
    /// <param name="quarter">The one-based fiscal quarter number.</param>
    /// <param name="fiscalYearStartMonth">The one-based month in which the fiscal year starts.</param>
    /// <returns>The requested fiscal period with inclusive date bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a fiscal argument is invalid or unrepresentable.</exception>
    public FiscalPeriod FiscalPeriodFromFiscalYearAndQuarterAndFiscalYearStartMonth(int fiscalYear, int quarter, int fiscalYearStartMonth)
    {
        return CalendarDateGenerator.Fiscal(fiscalYear, quarter, fiscalYearStartMonth);
    }

    /// <summary>Generates a Gregorian calendar quarter number.</summary>
    /// <returns>An integer from one through four.</returns>
    public int Quarter()
    {
        return TemporalCompletionGenerator.Quarter(RandomValue);
    }

    /// <summary>Returns the Gregorian calendar quarter containing a date.</summary>
    /// <param name="value">The date to classify.</param>
    /// <returns>An integer from one through four.</returns>
    public int QuarterFromDate(System.DateOnly value)
    {
        return TemporalCompletionGenerator.QuarterFromDate(value);
    }

    /// <summary>
    /// Generates two ordered instants within an inclusive interval.
    /// </summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The inclusive maximum instant.</param>
    /// <returns>An interval whose start is not after its end.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the instant interval is reversed.</exception>
    public DateInterval OrderedInterval(System.DateTimeOffset minimum, System.DateTimeOffset maximum)
    {
        return CalendarDateGenerator.OrderedInterval(RandomValue, minimum, maximum);
    }

    /// <summary>
    /// Generates an ordered interval whose inclusive duration lies within requested bounds.
    /// </summary>
    /// <param name="minimum">The inclusive minimum instant.</param>
    /// <param name="maximum">The inclusive maximum instant.</param>
    /// <param name="minimumDuration">The inclusive nonnegative minimum duration.</param>
    /// <param name="maximumDuration">The inclusive nonnegative maximum duration.</param>
    /// <returns>An ordered interval fully contained by the requested instant bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the instant or duration interval is invalid.</exception>
    public DateInterval OrderedIntervalFromMinimumAndMaximumAndMinimumDurationAndMaximumDuration(System.DateTimeOffset minimum, System.DateTimeOffset maximum, System.TimeSpan minimumDuration, System.TimeSpan maximumDuration)
    {
        return CalendarDateGenerator.OrderedInterval(RandomValue, minimum, maximum, minimumDuration, maximumDuration);
    }

    /// <summary>
    /// Copies an ordered date interval into a general interval record.
    /// </summary>
    /// <param name="source">The ordered date interval to copy.</param>
    /// <returns>An independent interval record with the same bounds.</returns>
    public IntervalRecord IntervalRecordFromDateInterval(DateInterval source)
    {
        return IntervalRecord.FromDateInterval(source);
    }

    /// <summary>
    /// Generates a complete month name using the configured locale fallback chain.
    /// </summary>
    /// <returns>A localized wide month name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string MonthName()
    {
        return MonthNameFromStyle(DateNameStyle.Wide);
    }

    /// <summary>
    /// Generates a month name using a requested localized display width.
    /// </summary>
    /// <param name="style">The wide or abbreviated display style.</param>
    /// <returns>A localized month name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string MonthNameFromStyle(DateNameStyle style)
    {
        return DatePresentation.MonthName(Context, RandomValue, style);
    }

    /// <summary>
    /// Returns the complete localized name of a one-based month number.
    /// </summary>
    /// <param name="month">The month number from 1 through 12.</param>
    /// <returns>The localized wide month name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the month is invalid or the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string MonthNameFromMonth(int month)
    {
        return MonthNameFromMonthAndStyle(month, DateNameStyle.Wide);
    }

    /// <summary>
    /// Returns the localized name of a one-based month number using a requested display width.
    /// </summary>
    /// <param name="month">The month number from 1 through 12.</param>
    /// <param name="style">The wide or abbreviated display style.</param>
    /// <returns>The requested localized month name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the month or style is invalid or the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string MonthNameFromMonthAndStyle(int month, DateNameStyle style)
    {
        return DatePresentation.MonthName(Context, month, style);
    }

    /// <summary>
    /// Generates a complete weekday name using the configured locale fallback chain.
    /// </summary>
    /// <returns>A localized wide weekday name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string WeekdayName()
    {
        return WeekdayNameFromStyle(DateNameStyle.Wide);
    }

    /// <summary>
    /// Generates a weekday name using a requested localized display width.
    /// </summary>
    /// <param name="style">The wide or abbreviated display style.</param>
    /// <returns>A localized weekday name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the style is invalid or the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string WeekdayNameFromStyle(DateNameStyle style)
    {
        return DatePresentation.WeekdayName(Context, RandomValue, style);
    }

    /// <summary>
    /// Returns the complete localized name of a weekday.
    /// </summary>
    /// <param name="weekday">The runtime weekday value.</param>
    /// <returns>The localized wide weekday name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the weekday is invalid or the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string WeekdayNameFromWeekday(System.DayOfWeek weekday)
    {
        return WeekdayNameFromWeekdayAndStyle(weekday, DateNameStyle.Wide);
    }

    /// <summary>
    /// Returns the localized name of a weekday using a requested display width.
    /// </summary>
    /// <param name="weekday">The runtime weekday value.</param>
    /// <param name="style">The wide or abbreviated display style.</param>
    /// <returns>The requested localized weekday name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the weekday or style is invalid or the result exceeds the configured output limit.</exception>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public string WeekdayNameFromWeekdayAndStyle(System.DayOfWeek weekday, DateNameStyle style)
    {
        return DatePresentation.WeekdayName(Context, weekday, style);
    }

    /// <summary>
    /// Returns the numeric calendar date order from the configured locale fallback chain.
    /// </summary>
    /// <returns>The resolved month-day-year, day-month-year, or year-month-day order.</returns>
    /// <exception cref="FakeException">Thrown when resolved locale data is unavailable or malformed.</exception>
    public DateOrder DateOrder()
    {
        return DatePresentation.Order(Context);
    }

    /// <summary>Formats the captured reference date using the configured locale.</summary>
    /// <returns>A locale-formatted short date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string LocaleDate()
    {
        return LocaleDateFromValue(System.DateOnly.FromDateTime(Context.ReferenceTime().DateTime));
    }

    /// <summary>Formats a date using the configured locale.</summary>
    /// <param name="value">The date to format.</param>
    /// <returns>A locale-formatted short date.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string LocaleDateFromValue(System.DateOnly value)
    {
        return TemporalCompletionGenerator.LocaleDate(Context, value);
    }

    /// <summary>Formats the captured reference time of day using the configured locale.</summary>
    /// <returns>A locale-formatted long time.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string LocaleTime()
    {
        return LocaleTimeFromValue(System.TimeOnly.FromDateTime(Context.ReferenceTime().DateTime));
    }

    /// <summary>Formats a time of day using the configured locale.</summary>
    /// <param name="value">The time to format.</param>
    /// <returns>A locale-formatted long time.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string LocaleTimeFromValue(System.TimeOnly value)
    {
        return TemporalCompletionGenerator.LocaleTime(Context, value);
    }

    /// <summary>Formats the captured reference timestamp using the configured locale.</summary>
    /// <returns>A locale-formatted date and time followed by the numeric offset.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string LocaleDateTime()
    {
        return LocaleDateTimeFromValue(Context.ReferenceTime());
    }

    /// <summary>Formats a timestamp using the configured locale and preserves its offset text.</summary>
    /// <param name="value">The timestamp to format.</param>
    /// <returns>A locale-formatted date and time followed by the numeric offset.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string LocaleDateTimeFromValue(System.DateTimeOffset value)
    {
        return TemporalCompletionGenerator.LocaleDateTime(Context, value);
    }

    /// <summary>Describes an instant relative to the captured reference clock in the configured locale language.</summary>
    /// <param name="value">The instant to describe.</param>
    /// <returns>A localized rounded relative-time phrase.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string RelativePhraseFromValue(System.DateTimeOffset value)
    {
        return TemporalCompletionGenerator.RelativePhrase(Context, value);
    }

    /// <summary>
    /// Truncates the captured reference time to a requested precision.
    /// </summary>
    /// <param name="precision">The requested temporal precision.</param>
    /// <returns>The truncated reference value with its original offset.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the precision is invalid.</exception>
    public System.DateTimeOffset Truncate(TemporalPrecision precision)
    {
        return TruncateFromValueAndPrecision(Context.ReferenceTime(), precision);
    }

    /// <summary>
    /// Truncates an offset-aware timestamp to a requested precision without rounding.
    /// </summary>
    /// <param name="value">The timestamp to truncate.</param>
    /// <param name="precision">The requested temporal precision.</param>
    /// <returns>The truncated value with its original offset.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the precision is invalid.</exception>
    public System.DateTimeOffset TruncateFromValueAndPrecision(System.DateTimeOffset value, TemporalPrecision precision)
    {
        return DatePresentation.Truncate(value, precision);
    }

    /// <summary>
    /// Formats the captured reference time as RFC 3339 text at whole-second precision.
    /// </summary>
    /// <returns>An RFC 3339 timestamp.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string Rfc3339()
    {
        return Rfc3339FromValueAndPrecision(Context.ReferenceTime(), TemporalPrecision.Seconds);
    }

    /// <summary>
    /// Formats the captured reference time as RFC 3339 text at a requested precision.
    /// </summary>
    /// <param name="precision">The requested fractional-second precision.</param>
    /// <returns>An RFC 3339 timestamp.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the precision is invalid or the result exceeds the configured output limit.</exception>
    public string Rfc3339FromPrecision(TemporalPrecision precision)
    {
        return Rfc3339FromValueAndPrecision(Context.ReferenceTime(), precision);
    }

    /// <summary>
    /// Formats an offset-aware timestamp as RFC 3339 text at whole-second precision.
    /// </summary>
    /// <param name="value">The timestamp to format.</param>
    /// <returns>An RFC 3339 timestamp.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the result exceeds the configured output limit.</exception>
    public string Rfc3339FromValue(System.DateTimeOffset value)
    {
        return Rfc3339FromValueAndPrecision(value, TemporalPrecision.Seconds);
    }

    /// <summary>
    /// Formats an offset-aware timestamp as RFC 3339 text at a requested precision.
    /// </summary>
    /// <remarks>Discarded fractional ticks are truncated rather than rounded, and a zero offset is written as <c>Z</c>.</remarks>
    /// <param name="value">The timestamp to format.</param>
    /// <param name="precision">The requested fractional-second precision.</param>
    /// <returns>An RFC 3339 timestamp preserving the supplied offset.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the precision is invalid or the result exceeds the configured output limit.</exception>
    public string Rfc3339FromValueAndPrecision(System.DateTimeOffset value, TemporalPrecision precision)
    {
        return DatePresentation.Rfc3339(value, precision, Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>
    /// Converts the captured reference time to Unix seconds.
    /// </summary>
    /// <returns>Whole seconds since 1970-01-01T00:00:00Z using floor semantics.</returns>
    public long UnixSeconds()
    {
        return UnixSecondsFromValue(Context.ReferenceTime());
    }

    /// <summary>
    /// Converts an instant to Unix seconds.
    /// </summary>
    /// <param name="value">The instant to convert.</param>
    /// <returns>Whole seconds since 1970-01-01T00:00:00Z using floor semantics.</returns>
    public long UnixSecondsFromValue(System.DateTimeOffset value)
    {
        return value.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts the captured reference time to Unix milliseconds.
    /// </summary>
    /// <returns>Whole milliseconds since 1970-01-01T00:00:00Z using floor semantics.</returns>
    public long UnixMilliseconds()
    {
        return UnixMillisecondsFromValue(Context.ReferenceTime());
    }

    /// <summary>
    /// Converts an instant to Unix milliseconds.
    /// </summary>
    /// <param name="value">The instant to convert.</param>
    /// <returns>Whole milliseconds since 1970-01-01T00:00:00Z using floor semantics.</returns>
    public long UnixMillisecondsFromValue(System.DateTimeOffset value)
    {
        return value.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Converts Unix seconds to a UTC offset-aware timestamp.
    /// </summary>
    /// <param name="seconds">Whole seconds since 1970-01-01T00:00:00Z.</param>
    /// <returns>The represented UTC instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the value is outside the runtime timestamp range.</exception>
    public System.DateTimeOffset FromUnixSeconds(long seconds)
    {
        return System.DateTimeOffset.FromUnixTimeSeconds(seconds);
    }

    /// <summary>
    /// Converts Unix milliseconds to a UTC offset-aware timestamp.
    /// </summary>
    /// <param name="milliseconds">Whole milliseconds since 1970-01-01T00:00:00Z.</param>
    /// <returns>The represented UTC instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the value is outside the runtime timestamp range.</exception>
    public System.DateTimeOffset FromUnixMilliseconds(long milliseconds)
    {
        return System.DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }

    /// <summary>
    /// Returns every canonical IANA identifier in the bundled zone1970.tab metadata.
    /// </summary>
    /// <returns>A new array in bundled source order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured collection or output limit is too small.</exception>
    public string[] TimeZoneIds()
    {
        TimeZoneRegistryEntry[] entries = TimeZoneRegistryStore.Current().EntriesCopy();
        Guard.Count(entries.Length, Context.CapturedOptions.MaximumCount, "timeZoneIds");
        Guard.OutputSize(entries.Length, Context.CapturedOptions.MaximumOutputSize, "timeZoneIds");
        return TimeZoneGenerator.Ids(entries);
    }

    /// <summary>
    /// Returns canonical IANA identifiers associated with an ISO 3166 alpha-2 country code.
    /// </summary>
    /// <param name="countryCode">The case-insensitive two-letter country code.</param>
    /// <returns>A new array in bundled source order.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the country code is malformed or has no bundled zone.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured collection or output limit is too small.</exception>
    public string[] TimeZoneIdsFromCountryCode(string countryCode)
    {
        TimeZoneRegistryEntry[] entries = TimeZoneRegistryStore.Current().Country(countryCode);
        Guard.Count(entries.Length, Context.CapturedOptions.MaximumCount, "timeZoneIds");
        Guard.OutputSize(entries.Length, Context.CapturedOptions.MaximumOutputSize, "timeZoneIds");
        return TimeZoneGenerator.Ids(entries);
    }

    /// <summary>
    /// Selects a canonical IANA identifier from the bundled zone1970.tab metadata.
    /// </summary>
    /// <returns>A bundled IANA time-zone identifier.</returns>
    public string TimeZoneId()
    {
        return TimeZoneGenerator.Id(RandomValue, TimeZoneRegistryStore.Current().EntriesCopy());
    }

    /// <summary>
    /// Selects a canonical IANA identifier associated with an ISO 3166 alpha-2 country code.
    /// </summary>
    /// <param name="countryCode">The case-insensitive two-letter country code.</param>
    /// <returns>A bundled IANA time-zone identifier for the country.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the country code is malformed or has no bundled zone.</exception>
    public string TimeZoneIdFromCountryCode(string countryCode)
    {
        return TimeZoneGenerator.Id(RandomValue, TimeZoneRegistryStore.Current().Country(countryCode));
    }

    /// <summary>
    /// Selects a bundled IANA identifier and resolves it through the runtime time-zone database.
    /// </summary>
    /// <returns>The mapped runtime time zone.</returns>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the selected bundled identifier.</exception>
    public System.TimeZoneInfo TimeZone()
    {
        return TimeZoneFromIanaId(TimeZoneId());
    }

    /// <summary>
    /// Resolves a bundled IANA identifier through the runtime time-zone database.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <returns>The mapped runtime time zone.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public System.TimeZoneInfo TimeZoneFromIanaId(string ianaId)
    {
        return TimeZoneGenerator.Runtime(ianaId);
    }

    /// <summary>
    /// Returns the platform identifier produced by runtime mapping for a bundled IANA identifier.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <returns>The mapped runtime identifier, which may use IANA or Windows naming.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public string RuntimeTimeZoneId(string ianaId)
    {
        return TimeZoneFromIanaId(ianaId).Id;
    }

    /// <summary>
    /// Generates a runtime time-zone offset at the captured reference instant.
    /// </summary>
    /// <returns>The offset for a randomly selected bundled IANA zone.</returns>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the selected bundled identifier.</exception>
    public System.TimeSpan TimeZoneOffset()
    {
        return TimeZoneOffsetFromInstant(Context.ReferenceTime());
    }

    /// <summary>
    /// Generates a runtime time-zone offset at a supplied instant.
    /// </summary>
    /// <param name="instant">The instant at which the offset is evaluated.</param>
    /// <returns>The offset for a randomly selected bundled IANA zone.</returns>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the selected bundled identifier.</exception>
    public System.TimeSpan TimeZoneOffsetFromInstant(System.DateTimeOffset instant)
    {
        return TimeZoneGenerator.Offset(TimeZoneId(), instant);
    }

    /// <summary>
    /// Returns the runtime offset for a bundled IANA zone at the captured reference instant.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <returns>The runtime UTC offset at the captured reference instant.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public System.TimeSpan TimeZoneOffsetFromIanaId(string ianaId)
    {
        return TimeZoneOffsetFromIanaIdAndInstant(ianaId, Context.ReferenceTime());
    }

    /// <summary>
    /// Returns the runtime offset for a bundled IANA zone at a supplied instant.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <param name="instant">The instant at which the offset is evaluated.</param>
    /// <returns>The runtime UTC offset at the supplied instant.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public System.TimeSpan TimeZoneOffsetFromIanaIdAndInstant(string ianaId, System.DateTimeOffset instant)
    {
        return TimeZoneGenerator.Offset(ianaId, instant);
    }

    /// <summary>
    /// Converts an instant to its local representation in a bundled IANA time zone.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <param name="instant">The instant to convert.</param>
    /// <returns>The same instant with the selected zone's runtime offset.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public System.DateTimeOffset InTimeZone(string ianaId, System.DateTimeOffset instant)
    {
        return TimeZoneGenerator.Convert(ianaId, instant);
    }

    /// <summary>Returns the explicit local-time gap for a bundled IANA zone and year.</summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <param name="year">The runtime calendar year to search.</param>
    /// <returns>The first gap in the requested year.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the year is outside the supported search range.</exception>
    /// <exception cref="FakeException">Thrown when the runtime zone has no gap in that year or cannot be mapped.</exception>
    public DaylightSavingTransition DaylightSavingGapFromIanaIdAndYear(string ianaId, int year)
    {
        return TimeZoneGenerator.Transition(
            ianaId,
            year,
            DaylightSavingTransitionKind.Gap);
    }

    /// <summary>Returns the explicit local-time overlap for a bundled IANA zone and year.</summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <param name="year">The runtime calendar year to search.</param>
    /// <returns>The first overlap in the requested year.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the year is outside the supported search range.</exception>
    /// <exception cref="FakeException">Thrown when the runtime zone has no overlap in that year or cannot be mapped.</exception>
    public DaylightSavingTransition DaylightSavingOverlapFromIanaIdAndYear(string ianaId, int year)
    {
        return TimeZoneGenerator.Transition(
            ianaId,
            year,
            DaylightSavingTransitionKind.Overlap);
    }

    /// <summary>Generates a timestamp using the maximum supported positive UTC offset.</summary>
    /// <returns>A generated value whose offset is positive fourteen hours.</returns>
    public System.DateTimeOffset MaximumOffsetValue()
    {
        return TemporalCompletionGenerator.MaximumOffsetValue(RandomValue);
    }

    /// <summary>Generates a timestamp using the minimum supported negative UTC offset.</summary>
    /// <returns>A generated value whose offset is negative fourteen hours.</returns>
    public System.DateTimeOffset MinimumOffsetValue()
    {
        return TemporalCompletionGenerator.MinimumOffsetValue(RandomValue);
    }

    /// <summary>
    /// Selects a bundled IANA zone and describes it at the captured reference instant.
    /// </summary>
    /// <returns>Bundled metadata correlated with runtime mapping and offset information.</returns>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the selected bundled identifier.</exception>
    public TimeZoneDetails TimeZoneDetails()
    {
        TimeZoneRegistryEntry[] entries = TimeZoneRegistryStore.Current().EntriesCopy();
        TimeZoneRegistryEntry entry = entries[RandomValue.IntFromMaximumExclusive(entries.Length)];
        return TimeZoneGenerator.Details(entry, Context.ReferenceTime());
    }

    /// <summary>
    /// Describes a bundled IANA zone at the captured reference instant.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <returns>Bundled metadata correlated with runtime mapping and offset information.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public TimeZoneDetails TimeZoneDetailsFromIanaId(string ianaId)
    {
        return TimeZoneDetailsFromIanaIdAndInstant(ianaId, Context.ReferenceTime());
    }

    /// <summary>
    /// Describes a bundled IANA zone at a supplied instant.
    /// </summary>
    /// <param name="ianaId">The case-sensitive bundled IANA identifier.</param>
    /// <param name="instant">The instant at which runtime offset information is evaluated.</param>
    /// <returns>Bundled metadata correlated with runtime mapping and offset information.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the identifier is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the identifier is empty or is not bundled.</exception>
    /// <exception cref="FakeException">Thrown when the runtime cannot map the bundled identifier.</exception>
    public TimeZoneDetails TimeZoneDetailsFromIanaIdAndInstant(string ianaId, System.DateTimeOffset instant)
    {
        TimeZoneRegistryEntry entry = TimeZoneRegistryStore.Current().Find(ianaId);
        return TimeZoneGenerator.Details(entry, instant);
    }
}
