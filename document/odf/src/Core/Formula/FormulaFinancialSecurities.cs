using System.Collections.Generic;
using System.Numerics;

namespace odf;

internal class FormulaCouponSchedule
{
    internal OdfTemporalValue Settlement = new OdfTemporalValue();
    internal OdfTemporalValue Maturity = new OdfTemporalValue();
    internal OdfTemporalValue Previous = new OdfTemporalValue();
    internal OdfTemporalValue Next = new OdfTemporalValue();
    internal int Frequency;
    internal int Basis;
    internal int Coupons;
    internal bool IsValid;
}

internal static class FormulaFinancialSecurities
{
    internal static bool IsSecurityFunction(string name)
    {
        return name == OdfFormulaNames.ACCRINT
            || name == OdfFormulaNames.ACCRINTM
            || name == OdfFormulaNames.COUPDAYBS
            || name == OdfFormulaNames.COUPDAYS
            || name == OdfFormulaNames.COUPDAYSNC
            || name == OdfFormulaNames.COUPNCD
            || name == OdfFormulaNames.COUPNUM
            || name == OdfFormulaNames.COUPPCD
            || name == OdfFormulaNames.DISC
            || name == OdfFormulaNames.DURATION
            || name == OdfFormulaNames.INTRATE
            || name == OdfFormulaNames.MDURATION
            || name == OdfFormulaNames.ODDFPRICE
            || name == OdfFormulaNames.ODDFYIELD
            || name == OdfFormulaNames.ODDLPRICE
            || name == OdfFormulaNames.ODDLYIELD
            || name == OdfFormulaNames.PRICE
            || name == OdfFormulaNames.PRICEDISC
            || name == OdfFormulaNames.PRICEMAT
            || name == OdfFormulaNames.RECEIVED
            || name == OdfFormulaNames.TBILLEQ
            || name == OdfFormulaNames.TBILLPRICE
            || name == OdfFormulaNames.TBILLYIELD
            || name == OdfFormulaNames.YIELD
            || name == OdfFormulaNames.YIELDDISC
            || name == OdfFormulaNames.YIELDMAT;
    }

    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        if (name == OdfFormulaNames.ACCRINT
            || name == OdfFormulaNames.ACCRINTM)
        {
            return AccruedInterest(name, arguments, settings);
        }
        if (name.StartsWith("COUP", global::System.StringComparison.Ordinal))
        {
            return CouponInformation(name, arguments, settings);
        }
        if (name == OdfFormulaNames.DISC
            || name == OdfFormulaNames.INTRATE
            || name == OdfFormulaNames.RECEIVED
            || name == OdfFormulaNames.PRICEDISC
            || name == OdfFormulaNames.YIELDDISC)
        {
            return DiscountSecurity(name, arguments, settings);
        }
        if (name == OdfFormulaNames.TBILLEQ
            || name == OdfFormulaNames.TBILLPRICE
            || name == OdfFormulaNames.TBILLYIELD)
        {
            return TreasuryBill(name, arguments, settings);
        }
        if (name == OdfFormulaNames.PRICEMAT
            || name == OdfFormulaNames.YIELDMAT)
        {
            return MaturitySecurity(name, arguments, settings);
        }
        if (name == OdfFormulaNames.DURATION
            || name == OdfFormulaNames.MDURATION)
        {
            return BondDuration(name, arguments, settings);
        }
        if (name == OdfFormulaNames.PRICE)
        {
            return BondPrice(arguments, settings);
        }
        if (name == OdfFormulaNames.YIELD)
        {
            return BondYield(arguments, settings);
        }
        return OddSecurity(name, arguments, settings);
    }

    static OdfFormulaResult AccruedInterest(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        if (name == OdfFormulaNames.ACCRINTM)
        {
            double coupon = FormulaFinancialFunctions.RequiredNumber(
                arguments, 2);
            double par = FormulaFinancialFunctions.RequiredNumber(
                arguments, 3);
            int basis = (int)global::System.Math.Truncate(
                FormulaFinancialFunctions.OptionalNumber(arguments, 4, 0));
            double fraction = YearFraction(
                arguments[0], arguments[1], basis, settings);
            if (!double.IsFinite(fraction) || coupon < 0 || par <= 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            return FormulaValueModel.CreateNumber(par * coupon * fraction);
        }
        double rate = FormulaFinancialFunctions.RequiredNumber(arguments, 3);
        double parValue = FormulaFinancialFunctions.RequiredNumber(arguments, 4);
        int frequency = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.RequiredNumber(arguments, 5));
        int basisValue = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.OptionalNumber(arguments, 6, 0));
        double method = FormulaFinancialFunctions.OptionalNumber(
            arguments, 7, 1);
        if (!ValidFrequency(frequency) || !ValidBasis(basisValue)
            || rate < 0 || parValue <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        OdfFormulaResult start = method == 0 ? arguments[1] : arguments[0];
        double fractionValue = YearFraction(
            start,
            arguments[2],
            basisValue,
            settings);
        return double.IsFinite(fractionValue) && fractionValue >= 0
            ? FormulaValueModel.CreateNumber(
                parValue * rate * fractionValue)
            : FormulaValueModel.CreateError(OdfFormulaError.Number);
    }

    static OdfFormulaResult CouponInformation(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        FormulaCouponSchedule schedule = CreateSchedule(
            arguments[0],
            arguments[1],
            arguments[2],
            arguments.Length > 3 ? arguments[3] : null,
            settings);
        if (!schedule.IsValid)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        if (name == OdfFormulaNames.COUPNUM)
        {
            return FormulaValueModel.CreateNumber(schedule.Coupons);
        }
        if (name == OdfFormulaNames.COUPNCD)
        {
            return DateResult(schedule.Next, settings);
        }
        if (name == OdfFormulaNames.COUPPCD)
        {
            return DateResult(schedule.Previous, settings);
        }
        double elapsed = BasisDays(
            schedule.Previous,
            schedule.Settlement,
            schedule.Basis);
        double period = CouponPeriodDays(schedule);
        if (name == OdfFormulaNames.COUPDAYBS)
        {
            return FormulaValueModel.CreateNumber(elapsed);
        }
        if (name == OdfFormulaNames.COUPDAYS)
        {
            return FormulaValueModel.CreateNumber(period);
        }
        return FormulaValueModel.CreateNumber(period - elapsed);
    }

    static OdfFormulaResult DiscountSecurity(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double first = FormulaFinancialFunctions.RequiredNumber(arguments, 2);
        double second = FormulaFinancialFunctions.RequiredNumber(arguments, 3);
        int basis = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.OptionalNumber(arguments, 4, 0));
        double fraction = YearFraction(
            arguments[0], arguments[1], basis, settings);
        if (!double.IsFinite(fraction) || fraction <= 0
            || first <= 0 || second <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        if (name == OdfFormulaNames.DISC)
        {
            return FormulaValueModel.CreateNumber(
                (second - first) / second / fraction);
        }
        if (name == OdfFormulaNames.INTRATE)
        {
            return FormulaValueModel.CreateNumber(
                (second - first) / first / fraction);
        }
        if (name == OdfFormulaNames.RECEIVED)
        {
            double denominator = 1 - second * fraction;
            return denominator <= 0
                ? FormulaValueModel.CreateError(OdfFormulaError.Number)
                : FormulaValueModel.CreateNumber(first / denominator);
        }
        if (name == OdfFormulaNames.PRICEDISC)
        {
            double price = second * (1 - first * fraction);
            return price < 0
                ? FormulaValueModel.CreateError(OdfFormulaError.Number)
                : FormulaValueModel.CreateNumber(price);
        }
        return FormulaValueModel.CreateNumber(
            (second - first) / first / fraction);
    }

    static OdfFormulaResult TreasuryBill(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double settlement = DateSerial(arguments[0], settings);
        double maturity = DateSerial(arguments[1], settings);
        double value = FormulaFinancialFunctions.RequiredNumber(arguments, 2);
        double days = global::System.Math.Truncate(maturity)
            - global::System.Math.Truncate(settlement);
        if (!double.IsFinite(days) || days <= 0 || days > 365 || value <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        if (name == OdfFormulaNames.TBILLPRICE)
        {
            return FormulaValueModel.CreateNumber(
                100 * (1 - value * days / 360));
        }
        if (name == OdfFormulaNames.TBILLYIELD)
        {
            return FormulaValueModel.CreateNumber(
                (100 - value) / value * 360 / days);
        }
        double denominator = 360 - value * days;
        return denominator <= 0
            ? FormulaValueModel.CreateError(OdfFormulaError.Number)
            : FormulaValueModel.CreateNumber(365 * value / denominator);
    }

    static OdfFormulaResult MaturitySecurity(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double rate = FormulaFinancialFunctions.RequiredNumber(arguments, 3);
        double priceOrYield = FormulaFinancialFunctions.RequiredNumber(
            arguments, 4);
        int basis = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.OptionalNumber(arguments, 5, 0));
        double issueToMaturity = YearFraction(
            arguments[2], arguments[1], basis, settings);
        double issueToSettlement = YearFraction(
            arguments[2], arguments[0], basis, settings);
        double settlementToMaturity = YearFraction(
            arguments[0], arguments[1], basis, settings);
        if (!double.IsFinite(issueToMaturity)
            || issueToMaturity <= 0 || settlementToMaturity <= 0
            || rate < 0 || priceOrYield <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double total = 100 + 100 * rate * issueToMaturity;
        double accrued = 100 * rate * issueToSettlement;
        if (name == OdfFormulaNames.PRICEMAT)
        {
            return FormulaValueModel.CreateNumber(
                total / (1 + priceOrYield * settlementToMaturity) - accrued);
        }
        return FormulaValueModel.CreateNumber(
            (total / (priceOrYield + accrued) - 1)
            / settlementToMaturity);
    }

    static OdfFormulaResult BondPrice(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double coupon = FormulaFinancialFunctions.RequiredNumber(arguments, 2);
        double yield = FormulaFinancialFunctions.RequiredNumber(arguments, 3);
        double redemption = FormulaFinancialFunctions.RequiredNumber(
            arguments, 4);
        FormulaCouponSchedule schedule = CreateSchedule(
            arguments[0],
            arguments[1],
            arguments[5],
            arguments.Length > 6 ? arguments[6] : null,
            settings);
        if (!schedule.IsValid || coupon < 0 || yield < 0 || redemption <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(
            PriceValue(schedule, coupon, yield, redemption));
    }

    static double PriceValue(
        FormulaCouponSchedule schedule,
        double coupon,
        double yield,
        double redemption)
    {
        double period = CouponPeriodDays(schedule);
        double accruedDays = BasisDays(
            schedule.Previous,
            schedule.Settlement,
            schedule.Basis);
        double remainingDays = period - accruedDays;
        double fraction = remainingDays / period;
        double couponPayment = 100 * coupon / schedule.Frequency;
        double rate = yield / schedule.Frequency;
        double price = 0;
        int couponIndex = 1;
        while (couponIndex <= schedule.Coupons)
        {
            double cash = couponPayment;
            if (couponIndex == schedule.Coupons)
            {
                cash = cash + redemption;
            }
            price = price + cash / global::System.Math.Pow(
                1 + rate,
                couponIndex - 1 + fraction);
            couponIndex = couponIndex + 1;
        }
        return price - couponPayment * accruedDays / period;
    }

    static OdfFormulaResult BondYield(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double coupon = FormulaFinancialFunctions.RequiredNumber(arguments, 2);
        double target = FormulaFinancialFunctions.RequiredNumber(arguments, 3);
        double redemption = FormulaFinancialFunctions.RequiredNumber(
            arguments, 4);
        FormulaCouponSchedule schedule = CreateSchedule(
            arguments[0],
            arguments[1],
            arguments[5],
            arguments.Length > 6 ? arguments[6] : null,
            settings);
        if (!schedule.IsValid || coupon < 0 || target <= 0 || redemption <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double yield = 0.1;
        int iteration = 0;
        while (iteration < 100)
        {
            double value = PriceValue(schedule, coupon, yield, redemption)
                - target;
            double step = 0.000001 * (global::System.Math.Abs(yield) + 1);
            double derivative = (PriceValue(
                    schedule,
                    coupon,
                    yield + step,
                    redemption) - target - value) / step;
            if (!double.IsFinite(value) || derivative == 0)
            {
                break;
            }
            double next = yield - value / derivative;
            if (next <= -schedule.Frequency + 0.000000001)
            {
                next = (yield - schedule.Frequency) / 2;
            }
            if (global::System.Math.Abs(next - yield) < 0.0000000001)
            {
                return FormulaValueModel.CreateNumber(next);
            }
            yield = next;
            iteration = iteration + 1;
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Number);
    }

    static OdfFormulaResult BondDuration(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double coupon = FormulaFinancialFunctions.RequiredNumber(arguments, 2);
        double yield = FormulaFinancialFunctions.RequiredNumber(arguments, 3);
        FormulaCouponSchedule schedule = CreateSchedule(
            arguments[0],
            arguments[1],
            arguments[4],
            arguments.Length > 5 ? arguments[5] : null,
            settings);
        if (!schedule.IsValid || coupon < 0 || yield < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double period = CouponPeriodDays(schedule);
        double remaining = period - BasisDays(
            schedule.Previous,
            schedule.Settlement,
            schedule.Basis);
        double fraction = remaining / period;
        double couponPayment = 100 * coupon / schedule.Frequency;
        double periodicYield = yield / schedule.Frequency;
        double present = 0;
        double weighted = 0;
        int index = 1;
        while (index <= schedule.Coupons)
        {
            double time = index - 1 + fraction;
            double cash = couponPayment
                + (index == schedule.Coupons ? 100 : 0);
            double value = cash / global::System.Math.Pow(
                1 + periodicYield,
                time);
            present = present + value;
            weighted = weighted + time * value;
            index = index + 1;
        }
        double duration = weighted / present / schedule.Frequency;
        if (name == OdfFormulaNames.MDURATION)
        {
            duration = duration / (1 + periodicYield);
        }
        return FormulaValueModel.CreateNumber(duration);
    }

    static OdfFormulaResult OddSecurity(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        bool first = name == OdfFormulaNames.ODDFPRICE
            || name == OdfFormulaNames.ODDFYIELD;
        bool priceFunction = name == OdfFormulaNames.ODDFPRICE
            || name == OdfFormulaNames.ODDLPRICE;
        int rateIndex = first ? 4 : 3;
        int valueIndex = first ? 5 : 4;
        int redemptionIndex = first ? 6 : 5;
        int frequencyIndex = first ? 7 : 6;
        int basisIndex = first ? 8 : 7;
        double rate = FormulaFinancialFunctions.RequiredNumber(
            arguments, rateIndex);
        double priceOrYield = FormulaFinancialFunctions.RequiredNumber(
            arguments, valueIndex);
        double redemption = FormulaFinancialFunctions.RequiredNumber(
            arguments, redemptionIndex);
        int frequency = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.RequiredNumber(
                arguments, frequencyIndex));
        int basis = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.OptionalNumber(
                arguments, basisIndex, 0));
        OdfTemporalValue? settlement = DateParts(arguments[0], settings);
        OdfTemporalValue? maturity = DateParts(arguments[1], settings);
        OdfTemporalValue? boundary = DateParts(arguments[2], settings);
        OdfTemporalValue? firstCoupon = first
            ? DateParts(arguments[3], settings)
            : null;
        if (settlement == null || maturity == null || boundary == null
            || (first && firstCoupon == null)
            || !ValidFrequency(frequency) || !ValidBasis(basis)
            || rate < 0 || priceOrYield < 0 || redemption <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double targetPrice = priceFunction ? 0 : priceOrYield;
        double annualYield = priceFunction ? priceOrYield : 0.1;
        if (priceFunction)
        {
            return FormulaValueModel.CreateNumber(OddPriceValue(
                settlement,
                maturity,
                boundary,
                firstCoupon,
                rate,
                annualYield,
                redemption,
                frequency,
                basis,
                first));
        }
        int iteration = 0;
        while (iteration < 100)
        {
            double value = OddPriceValue(
                settlement, maturity, boundary, firstCoupon,
                rate, annualYield, redemption, frequency, basis, first)
                - targetPrice;
            double step = 0.000001
                * (global::System.Math.Abs(annualYield) + 1);
            double derivative = (OddPriceValue(
                    settlement, maturity, boundary, firstCoupon,
                    rate, annualYield + step, redemption,
                    frequency, basis, first) - targetPrice - value) / step;
            if (!double.IsFinite(value) || derivative == 0)
            {
                break;
            }
            double next = annualYield - value / derivative;
            if (next <= -frequency + 0.000000001)
            {
                next = (annualYield - frequency) / 2;
            }
            if (global::System.Math.Abs(next - annualYield)
                < 0.0000000001)
            {
                return FormulaValueModel.CreateNumber(next);
            }
            annualYield = next;
            iteration = iteration + 1;
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Number);
    }

    static double OddPriceValue(
        OdfTemporalValue settlement,
        OdfTemporalValue maturity,
        OdfTemporalValue boundary,
        OdfTemporalValue? firstCoupon,
        double coupon,
        double annualYield,
        double redemption,
        int frequency,
        int basis,
        bool first)
    {
        int months = 12 / frequency;
        List<OdfTemporalValue> dates = new List<OdfTemporalValue>();
        OdfTemporalValue accruedStart;
        if (first && firstCoupon != null)
        {
            accruedStart = boundary;
            OdfTemporalValue date = firstCoupon;
            while (FormulaTemporalModel.DayNumber(date)
                < FormulaTemporalModel.DayNumber(maturity))
            {
                dates.Add(date);
                date = AddCalendarMonths(date, months);
            }
            dates.Add(maturity);
        }
        else
        {
            FormulaCouponSchedule schedule = CreateSchedule(
                DateResult(settlement, new OdfSpreadsheetSettings()),
                DateResult(boundary, new OdfSpreadsheetSettings()),
                FormulaValueModel.CreateNumber(frequency),
                FormulaValueModel.CreateNumber(basis),
                new OdfSpreadsheetSettings());
            accruedStart = schedule.IsValid
                ? schedule.Previous
                : AddCalendarMonths(boundary, -months);
            OdfTemporalValue date = schedule.IsValid
                ? schedule.Next
                : boundary;
            while (FormulaTemporalModel.DayNumber(date)
                <= FormulaTemporalModel.DayNumber(boundary))
            {
                dates.Add(date);
                date = AddCalendarMonths(date, months);
            }
            dates.Add(maturity);
        }
        double price = 0;
        OdfTemporalValue periodStart = accruedStart;
        int index = 0;
        while (index < dates.Count)
        {
            OdfTemporalValue cashDate = dates[index];
            double cash = 100 * coupon
                * BasisFraction(periodStart, cashDate, basis);
            if (index == dates.Count - 1)
            {
                cash = cash + redemption;
            }
            double exponent = frequency
                * BasisFraction(settlement, cashDate, basis);
            price = price + cash / global::System.Math.Pow(
                1 + annualYield / frequency,
                exponent);
            periodStart = cashDate;
            index = index + 1;
        }
        double accrued = 100 * coupon
            * BasisFraction(accruedStart, settlement, basis);
        return price - accrued;
    }

    static FormulaCouponSchedule CreateSchedule(
        OdfFormulaResult settlementValue,
        OdfFormulaResult maturityValue,
        OdfFormulaResult frequencyValue,
        OdfFormulaResult? basisValue,
        OdfSpreadsheetSettings settings)
    {
        FormulaCouponSchedule result = new FormulaCouponSchedule();
        OdfTemporalValue? settlement = DateParts(settlementValue, settings);
        OdfTemporalValue? maturity = DateParts(maturityValue, settings);
        int frequency = (int)global::System.Math.Truncate(
            FormulaFinancialFunctions.RequiredNumber(
                [frequencyValue], 0));
        int basis = basisValue == null
            ? 0
            : (int)global::System.Math.Truncate(
                FormulaFinancialFunctions.RequiredNumber([basisValue], 0));
        if (settlement == null || maturity == null
            || !ValidFrequency(frequency) || !ValidBasis(basis)
            || FormulaTemporalModel.DayNumber(settlement)
                >= FormulaTemporalModel.DayNumber(maturity))
        {
            return result;
        }
        int months = 12 / frequency;
        OdfTemporalValue next = maturity;
        OdfTemporalValue previous = AddCalendarMonths(next, -months);
        int coupons = 1;
        while (FormulaTemporalModel.DayNumber(previous)
            > FormulaTemporalModel.DayNumber(settlement))
        {
            next = previous;
            previous = AddCalendarMonths(previous, -months);
            coupons = coupons + 1;
        }
        result.Settlement = settlement;
        result.Maturity = maturity;
        result.Previous = previous;
        result.Next = next;
        result.Frequency = frequency;
        result.Basis = basis;
        result.Coupons = coupons;
        result.IsValid = true;
        return result;
    }

    static double CouponPeriodDays(FormulaCouponSchedule schedule)
    {
        if (schedule.Basis == 1)
        {
            return (double)(FormulaTemporalModel.DayNumber(schedule.Next)
                - FormulaTemporalModel.DayNumber(schedule.Previous));
        }
        if (schedule.Basis == 3)
        {
            return 365.0 / schedule.Frequency;
        }
        return 360.0 / schedule.Frequency;
    }

    internal static double YearFraction(
        OdfFormulaResult startValue,
        OdfFormulaResult endValue,
        int basis,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue? start = DateParts(startValue, settings);
        OdfTemporalValue? end = DateParts(endValue, settings);
        if (start == null || end == null || !ValidBasis(basis))
        {
            return double.NaN;
        }
        double days = BasisDays(start, end, basis);
        if (basis == 0 || basis == 2 || basis == 4)
        {
            return days / 360;
        }
        if (basis == 3)
        {
            return days / 365;
        }
        return ActualActual(start, end);
    }

    static double BasisDays(
        OdfTemporalValue start,
        OdfTemporalValue end,
        int basis)
    {
        if (basis == 0 || basis == 4)
        {
            int startDay = start.Day == 31 ? 30 : start.Day;
            int endDay = end.Day;
            if (basis == 0
                && start.Month == 2
                && start.Day == DaysInMonth(start.Year, start.Month))
            {
                startDay = 30;
            }
            if (endDay == 31 && (basis == 4 || startDay == 30))
            {
                endDay = 30;
            }
            return (double)((end.Year - start.Year) * 360
                + (end.Month - start.Month) * 30
                + endDay - startDay);
        }
        return (double)(FormulaTemporalModel.DayNumber(end)
            - FormulaTemporalModel.DayNumber(start));
    }

    static double BasisFraction(
        OdfTemporalValue start,
        OdfTemporalValue end,
        int basis)
    {
        double days = BasisDays(start, end, basis);
        if (basis == 0 || basis == 2 || basis == 4)
        {
            return days / 360;
        }
        if (basis == 3)
        {
            return days / 365;
        }
        return ActualActual(start, end);
    }

    static double ActualActual(
        OdfTemporalValue start,
        OdfTemporalValue end)
    {
        BigInteger startDay = FormulaTemporalModel.DayNumber(start);
        BigInteger endDay = FormulaTemporalModel.DayNumber(end);
        if (start.Year == end.Year)
        {
            return (double)(endDay - startDay)
                / (FormulaTemporalModel.IsLeapYear(start.Year) ? 366 : 365);
        }
        OdfTemporalValue next = ValidDate(start.Year + 1, 1, 1);
        double result = (double)(FormulaTemporalModel.DayNumber(next) - startDay)
            / (FormulaTemporalModel.IsLeapYear(start.Year) ? 366 : 365);
        BigInteger year = start.Year + 1;
        while (year < end.Year)
        {
            result = result + 1;
            year = year + 1;
        }
        OdfTemporalValue first = ValidDate(end.Year, 1, 1);
        return result + (double)(endDay - FormulaTemporalModel.DayNumber(first))
            / (FormulaTemporalModel.IsLeapYear(end.Year) ? 366 : 365);
    }

    internal static double DateSerial(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings)
    {
        if (value.Kind == OdfValueKind.String)
        {
            OdfFormulaResult parsed = FormulaTemporalModel.CreateTemporal(
                value.StringValue,
                settings);
            return parsed.Error == OdfFormulaError.None
                ? global::System.Math.Truncate(parsed.NumericValue)
                : double.NaN;
        }
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        return number.Error == OdfFormulaError.None
            ? global::System.Math.Truncate(number.NumericValue)
            : double.NaN;
    }

    static OdfTemporalValue? DateParts(
        OdfFormulaResult value,
        OdfSpreadsheetSettings settings)
    {
        double serial = DateSerial(value, settings);
        OdfTemporalValue nullDate = FormulaTemporalModel.ParseNullDate(settings);
        if (!double.IsFinite(serial) || !nullDate.IsValid)
        {
            return null;
        }
        return FormulaTemporalModel.DateFromDayNumber(
            FormulaTemporalModel.DayNumber(nullDate)
            + new BigInteger(serial));
    }

    static OdfFormulaResult DateResult(
        OdfTemporalValue date,
        OdfSpreadsheetSettings settings)
    {
        OdfTemporalValue nullDate = FormulaTemporalModel.ParseNullDate(settings);
        double serial = (double)(FormulaTemporalModel.DayNumber(date)
            - FormulaTemporalModel.DayNumber(nullDate));
        return FormulaTemporalModel.TemporalFromSerial(
            serial,
            OdfValueKind.Date,
            settings,
            0,
            false);
    }

    static OdfTemporalValue AddCalendarMonths(
        OdfTemporalValue value,
        int months)
    {
        BigInteger total = value.Year * 12 + value.Month - 1 + months;
        BigInteger year = total / 12;
        if (total % 12 < 0)
        {
            year = year - 1;
        }
        int month = (int)(total - year * 12) + 1;
        int day = value.Day;
        int maximum = DaysInMonth(year, month);
        if (day > maximum)
        {
            day = maximum;
        }
        return ValidDate(year, month, day);
    }

    static OdfTemporalValue ValidDate(
        BigInteger year,
        int month,
        int day)
    {
        OdfTemporalValue result = new OdfTemporalValue();
        result.Year = year;
        result.Month = month;
        result.Day = day;
        result.HasDate = true;
        result.IsValid = true;
        return result;
    }

    static int DaysInMonth(BigInteger year, int month)
    {
        if (month == 12)
        {
            return 31;
        }
        return FormulaTemporalModel.MonthStartForYear(year, month + 1)
            - FormulaTemporalModel.MonthStartForYear(year, month);
    }

    static bool ValidFrequency(int value)
    {
        return value == 1 || value == 2 || value == 4;
    }

    static bool ValidBasis(int value)
    {
        return value >= 0 && value <= 4;
    }
}
