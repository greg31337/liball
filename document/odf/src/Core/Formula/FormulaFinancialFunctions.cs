using System.Collections.Generic;

namespace odf;

internal static class FormulaFinancialFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        if (FormulaFinancialSecurities.IsSecurityFunction(name))
        {
            return FormulaFinancialSecurities.Evaluate(
                name,
                arguments,
                settings);
        }
        if (name == OdfFormulaNames.FV
            || name == OdfFormulaNames.PV
            || name == OdfFormulaNames.PMT
            || name == OdfFormulaNames.NPER)
        {
            return TimeValue(name, arguments);
        }
        if (name == OdfFormulaNames.RATE)
        {
            return Rate(arguments);
        }
        if (name == OdfFormulaNames.IPMT
            || name == OdfFormulaNames.PPMT)
        {
            return PaymentPart(name, arguments);
        }
        if (name == OdfFormulaNames.CUMIPMT
            || name == OdfFormulaNames.CUMPRINC)
        {
            return CumulativePayment(name, arguments);
        }
        if (name == OdfFormulaNames.ISPMT)
        {
            return IsPayment(arguments);
        }
        if (name == OdfFormulaNames.NPV)
        {
            return NetPresentValue(arguments);
        }
        if (name == OdfFormulaNames.IRR)
        {
            return InternalRate(arguments);
        }
        if (name == OdfFormulaNames.MIRR)
        {
            return ModifiedRate(arguments);
        }
        if (name == OdfFormulaNames.FVSCHEDULE)
        {
            return FutureSchedule(arguments);
        }
        if (name == OdfFormulaNames.XNPV)
        {
            return ExtendedPresentValue(arguments, settings);
        }
        if (name == OdfFormulaNames.XIRR)
        {
            return ExtendedRate(arguments, settings);
        }
        if (name == OdfFormulaNames.EFFECT
            || name == OdfFormulaNames.NOMINAL)
        {
            return InterestConversion(name, arguments);
        }
        if (name == OdfFormulaNames.RRI
            || name == OdfFormulaNames.PDURATION)
        {
            return GrowthMeasure(name, arguments);
        }
        if (name == OdfFormulaNames.DOLLARDE
            || name == OdfFormulaNames.DOLLARFR)
        {
            return DollarConversion(name, arguments);
        }
        return Depreciation(name, arguments, settings);
    }

    static OdfFormulaResult TimeValue(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult rateValue = NumberAt(arguments, 0, 0);
        OdfFormulaResult periodsValue = NumberAt(arguments, 1, 0);
        OdfFormulaResult paymentValue = NumberAt(arguments, 2, 0);
        if (rateValue.Error != OdfFormulaError.None)
        {
            return rateValue;
        }
        if (periodsValue.Error != OdfFormulaError.None)
        {
            return periodsValue;
        }
        if (paymentValue.Error != OdfFormulaError.None)
        {
            return paymentValue;
        }
        double rate = rateValue.NumericValue;
        double periods = periodsValue.NumericValue;
        double third = paymentValue.NumericValue;
        double fourth = OptionalNumber(arguments, 3, 0);
        double type = OptionalNumber(arguments, 4, 0);
        if (!double.IsFinite(fourth) || !ValidPaymentType(type))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (name == OdfFormulaNames.FV)
        {
            double factor = AnnuityFactor(rate, periods);
            double result = -(fourth * global::System.Math.Pow(1 + rate, periods)
                + third * (1 + rate * type) * factor);
            return FormulaValueModel.CreateNumber(result);
        }
        if (name == OdfFormulaNames.PV)
        {
            double power = global::System.Math.Pow(1 + rate, periods);
            double result = -(fourth
                    + third * (1 + rate * type)
                        * AnnuityFactor(rate, periods))
                / power;
            return FormulaValueModel.CreateNumber(result);
        }
        if (name == OdfFormulaNames.PMT)
        {
            if (periods == 0)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.DivisionByZero);
            }
            double power = global::System.Math.Pow(1 + rate, periods);
            double denominator = (1 + rate * type)
                * AnnuityFactor(rate, periods);
            return FormulaValueModel.CreateNumber(
                -(fourth + third * power) / denominator);
        }
        double payment = periods;
        double present = third;
        double future = fourth;
        type = OptionalNumber(arguments, 4, 0);
        if (rate == 0)
        {
            return payment == 0
                ? FormulaValueModel.CreateError(OdfFormulaError.DivisionByZero)
                : FormulaValueModel.CreateNumber(
                    -(present + future) / payment);
        }
        double adjusted = payment * (1 + rate * type);
        double numerator = adjusted - future * rate;
        double denominatorValue = present * rate + adjusted;
        double ratio = numerator / denominatorValue;
        if (ratio <= 0 || rate <= -1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Log(ratio)
            / global::System.Math.Log(1 + rate));
    }

    static OdfFormulaResult Rate(OdfFormulaResult[] arguments)
    {
        double periods = RequiredNumber(arguments, 0);
        double payment = RequiredNumber(arguments, 1);
        double present = RequiredNumber(arguments, 2);
        double future = OptionalNumber(arguments, 3, 0);
        double type = OptionalNumber(arguments, 4, 0);
        double guess = OptionalNumber(arguments, 5, 0.1);
        if (!AllFinite([periods, payment, present, future, type, guess])
            || periods <= 0 || !ValidPaymentType(type))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double rate = guess;
        int iteration = 0;
        while (iteration < 100)
        {
            double value = RateEquation(
                rate, periods, payment, present, future, type);
            double step = 0.000001 * (global::System.Math.Abs(rate) + 1);
            double derivative = (RateEquation(
                    rate + step,
                    periods,
                    payment,
                    present,
                    future,
                    type) - value) / step;
            if (!double.IsFinite(value) || derivative == 0)
            {
                break;
            }
            double next = rate - value / derivative;
            if (next <= -0.999999999)
            {
                next = (rate - 0.999999999) / 2;
            }
            if (global::System.Math.Abs(next - rate) < 0.0000000001)
            {
                return FormulaValueModel.CreateNumber(next);
            }
            rate = next;
            iteration = iteration + 1;
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Number);
    }

    static double RateEquation(
        double rate,
        double periods,
        double payment,
        double present,
        double future,
        double type)
    {
        double power = global::System.Math.Pow(1 + rate, periods);
        return present * power + payment * (1 + rate * type)
            * AnnuityFactor(rate, periods) + future;
    }

    static OdfFormulaResult PaymentPart(
        string name,
        OdfFormulaResult[] arguments)
    {
        double rate = RequiredNumber(arguments, 0);
        double periodValue = RequiredNumber(arguments, 1);
        double periods = RequiredNumber(arguments, 2);
        double present = RequiredNumber(arguments, 3);
        double future = OptionalNumber(arguments, 4, 0);
        double type = OptionalNumber(arguments, 5, 0);
        if (!AllFinite([rate, periodValue, periods, present, future, type])
            || periodValue < 1 || periodValue > periods
            || !ValidPaymentType(type))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double payment = Payment(rate, periods, present, future, type);
        double interest = InterestPayment(
            rate, periodValue, periods, present, future, type, payment);
        return FormulaValueModel.CreateNumber(
            name == OdfFormulaNames.IPMT
                ? interest
                : payment - interest);
    }

    static OdfFormulaResult CumulativePayment(
        string name,
        OdfFormulaResult[] arguments)
    {
        double rate = RequiredNumber(arguments, 0);
        double periods = RequiredNumber(arguments, 1);
        double present = RequiredNumber(arguments, 2);
        int start = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 3));
        int end = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 4));
        double type = RequiredNumber(arguments, 5);
        if (!AllFinite([rate, periods, present, type])
            || rate <= 0 || periods <= 0 || present <= 0
            || start < 1 || end < start || end > periods
            || !ValidPaymentType(type))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double payment = Payment(rate, periods, present, 0, type);
        double result = 0;
        int period = start;
        while (period <= end)
        {
            double interest = InterestPayment(
                rate, period, periods, present, 0, type, payment);
            result = result + (name == OdfFormulaNames.CUMIPMT
                ? interest
                : payment - interest);
            period = period + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult IsPayment(OdfFormulaResult[] arguments)
    {
        double rate = RequiredNumber(arguments, 0);
        double period = RequiredNumber(arguments, 1);
        double periods = RequiredNumber(arguments, 2);
        double present = RequiredNumber(arguments, 3);
        if (!AllFinite([rate, period, periods, present]) || periods == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return FormulaValueModel.CreateNumber(
            present * rate * (period / periods - 1));
    }

    static double Payment(
        double rate,
        double periods,
        double present,
        double future,
        double type)
    {
        if (rate == 0)
        {
            return -(present + future) / periods;
        }
        double power = global::System.Math.Pow(1 + rate, periods);
        return -(future + present * power)
            / ((1 + rate * type) * AnnuityFactor(rate, periods));
    }

    static double InterestPayment(
        double rate,
        double period,
        double periods,
        double present,
        double future,
        double type,
        double payment)
    {
        if (rate == 0 || (type == 1 && period == 1))
        {
            return 0;
        }
        double previous = period - 1;
        double balance = present * global::System.Math.Pow(1 + rate, previous)
            + payment * (1 + rate * type)
                * AnnuityFactor(rate, previous);
        return -balance * rate;
    }

    static OdfFormulaResult NetPresentValue(OdfFormulaResult[] arguments)
    {
        double rate = RequiredNumber(arguments, 0);
        if (!double.IsFinite(rate) || rate <= -1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        List<double> values = new List<double>();
        int argument = 1;
        while (argument < arguments.Length)
        {
            CollectNumbers(arguments[argument], values);
            argument = argument + 1;
        }
        double result = 0;
        int index = 0;
        while (index < values.Count)
        {
            result = result + values[index]
                / global::System.Math.Pow(1 + rate, index + 1);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult InternalRate(OdfFormulaResult[] arguments)
    {
        List<double> values = new List<double>();
        CollectNumbers(arguments[0], values);
        double guess = OptionalNumber(arguments, 1, 0.1);
        if (values.Count < 2 || !HasBothSigns(values)
            || !double.IsFinite(guess))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double rate = guess;
        int iteration = 0;
        while (iteration < 100)
        {
            double value = CashFlowValue(values, rate);
            double step = 0.000001 * (global::System.Math.Abs(rate) + 1);
            double derivative = (CashFlowValue(values, rate + step) - value)
                / step;
            if (!double.IsFinite(value) || derivative == 0)
            {
                break;
            }
            double next = rate - value / derivative;
            if (next <= -0.999999999)
            {
                next = (rate - 0.999999999) / 2;
            }
            if (global::System.Math.Abs(next - rate) < 0.0000000001)
            {
                return FormulaValueModel.CreateNumber(next);
            }
            rate = next;
            iteration = iteration + 1;
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Number);
    }

    static double CashFlowValue(List<double> values, double rate)
    {
        if (rate <= -1)
        {
            return double.NaN;
        }
        double result = 0;
        int index = 0;
        while (index < values.Count)
        {
            result = result + values[index]
                / global::System.Math.Pow(1 + rate, index);
            index = index + 1;
        }
        return result;
    }

    static OdfFormulaResult ModifiedRate(OdfFormulaResult[] arguments)
    {
        List<double> values = new List<double>();
        CollectNumbers(arguments[0], values);
        double finance = RequiredNumber(arguments, 1);
        double reinvest = RequiredNumber(arguments, 2);
        if (values.Count < 2 || finance <= -1 || reinvest <= -1
            || !HasBothSigns(values))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double presentNegative = 0;
        double futurePositive = 0;
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] < 0)
            {
                presentNegative = presentNegative + values[index]
                    / global::System.Math.Pow(1 + finance, index);
            }
            else
            {
                futurePositive = futurePositive + values[index]
                    * global::System.Math.Pow(
                        1 + reinvest,
                        values.Count - 1 - index);
            }
            index = index + 1;
        }
        double result = global::System.Math.Pow(
                -futurePositive / presentNegative,
                1.0 / (values.Count - 1)) - 1;
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult FutureSchedule(OdfFormulaResult[] arguments)
    {
        double result = RequiredNumber(arguments, 0);
        List<double> rates = new List<double>();
        CollectNumbers(arguments[1], rates);
        int index = 0;
        while (index < rates.Count)
        {
            result = result * (1 + rates[index]);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult ExtendedPresentValue(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double rate = RequiredNumber(arguments, 0);
        List<double> values = new List<double>();
        List<double> dates = new List<double>();
        CollectNumbers(arguments[1], values);
        CollectDateNumbers(arguments[2], dates, settings);
        if (rate <= -1 || values.Count == 0 || values.Count != dates.Count)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double result = ExtendedValue(values, dates, rate);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult ExtendedRate(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        List<double> values = new List<double>();
        List<double> dates = new List<double>();
        CollectNumbers(arguments[0], values);
        CollectDateNumbers(arguments[1], dates, settings);
        double rate = OptionalNumber(arguments, 2, 0.1);
        if (values.Count < 2 || values.Count != dates.Count
            || !HasBothSigns(values))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int iteration = 0;
        while (iteration < 100)
        {
            double value = ExtendedValue(values, dates, rate);
            double step = 0.000001 * (global::System.Math.Abs(rate) + 1);
            double derivative = (ExtendedValue(
                    values, dates, rate + step) - value) / step;
            if (!double.IsFinite(value) || derivative == 0)
            {
                break;
            }
            double next = rate - value / derivative;
            if (next <= -0.999999999)
            {
                next = (rate - 0.999999999) / 2;
            }
            if (global::System.Math.Abs(next - rate) < 0.0000000001)
            {
                return FormulaValueModel.CreateNumber(next);
            }
            rate = next;
            iteration = iteration + 1;
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Number);
    }

    static double ExtendedValue(
        List<double> values,
        List<double> dates,
        double rate)
    {
        if (rate <= -1)
        {
            return double.NaN;
        }
        double result = 0;
        int index = 0;
        while (index < values.Count)
        {
            result = result + values[index]
                / global::System.Math.Pow(
                    1 + rate,
                    (dates[index] - dates[0]) / 365);
            index = index + 1;
        }
        return result;
    }

    static OdfFormulaResult InterestConversion(
        string name,
        OdfFormulaResult[] arguments)
    {
        double rate = RequiredNumber(arguments, 0);
        double periods = global::System.Math.Truncate(
            RequiredNumber(arguments, 1));
        if (rate <= 0 || periods < 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double result = name == OdfFormulaNames.EFFECT
            ? global::System.Math.Pow(1 + rate / periods, periods) - 1
            : periods * (global::System.Math.Pow(1 + rate, 1 / periods) - 1);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult GrowthMeasure(
        string name,
        OdfFormulaResult[] arguments)
    {
        double periodsOrRate = RequiredNumber(arguments, 0);
        double present = RequiredNumber(arguments, 1);
        double future = RequiredNumber(arguments, 2);
        if (present <= 0 || future <= 0 || periodsOrRate <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double result = name == OdfFormulaNames.RRI
            ? global::System.Math.Pow(future / present, 1 / periodsOrRate) - 1
            : global::System.Math.Log(future / present)
                / global::System.Math.Log(1 + periodsOrRate);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult DollarConversion(
        string name,
        OdfFormulaResult[] arguments)
    {
        double value = RequiredNumber(arguments, 0);
        int denominator = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 1));
        if (denominator <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        int digits = 1;
        int scale = 10;
        while (scale < denominator)
        {
            digits = digits + 1;
            scale = scale * 10;
        }
        double sign = value < 0 ? -1 : 1;
        double absolute = global::System.Math.Abs(value);
        double whole = global::System.Math.Floor(absolute);
        double fraction = absolute - whole;
        double result = name == OdfFormulaNames.DOLLARDE
            ? whole + fraction * scale / denominator
            : whole + global::System.Math.Round(
                fraction * denominator,
                12) / scale;
        return FormulaValueModel.CreateNumber(sign * result);
    }

    static OdfFormulaResult Depreciation(
        string name,
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double cost = RequiredNumber(arguments, 0);
        double salvage = name == OdfFormulaNames.AMORLINC
            ? RequiredNumber(arguments, 3)
            : RequiredNumber(arguments, 1);
        if (name == OdfFormulaNames.SLN)
        {
            double life = RequiredNumber(arguments, 2);
            return life == 0
                ? FormulaValueModel.CreateError(OdfFormulaError.DivisionByZero)
                : FormulaValueModel.CreateNumber((cost - salvage) / life);
        }
        if (name == OdfFormulaNames.SYD)
        {
            double life = RequiredNumber(arguments, 2);
            double period = RequiredNumber(arguments, 3);
            if (life <= 0 || period < 1 || period > life)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            return FormulaValueModel.CreateNumber(
                (cost - salvage) * (life - period + 1) * 2
                / (life * (life + 1)));
        }
        if (name == OdfFormulaNames.AMORLINC)
        {
            return AmorLinear(arguments, settings);
        }
        if (name == OdfFormulaNames.DB)
        {
            return FixedDeclining(arguments);
        }
        if (name == OdfFormulaNames.DDB)
        {
            return DoubleDeclining(arguments);
        }
        return VariableDeclining(arguments);
    }

    static OdfFormulaResult AmorLinear(
        OdfFormulaResult[] arguments,
        OdfSpreadsheetSettings settings)
    {
        double cost = RequiredNumber(arguments, 0);
        double salvage = RequiredNumber(arguments, 3);
        int period = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 4));
        double rate = RequiredNumber(arguments, 5);
        int basis = (int)global::System.Math.Truncate(
            OptionalNumber(arguments, 6, 0));
        double first = FormulaFinancialSecurities.YearFraction(
            arguments[1], arguments[2], basis, settings);
        if (!double.IsFinite(first) || period < 0 || rate < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double firstAmount = global::System.Math.Min(
            cost - salvage,
            cost * rate * first);
        if (period == 0)
        {
            return FormulaValueModel.CreateNumber(firstAmount);
        }
        double remaining = cost - firstAmount - salvage;
        int elapsed = 1;
        while (elapsed < period && remaining > 0)
        {
            remaining = remaining - global::System.Math.Min(
                remaining,
                cost * rate);
            elapsed = elapsed + 1;
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Max(0,
                global::System.Math.Min(remaining, cost * rate)));
    }

    static OdfFormulaResult FixedDeclining(OdfFormulaResult[] arguments)
    {
        double cost = RequiredNumber(arguments, 0);
        double salvage = RequiredNumber(arguments, 1);
        int life = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 2));
        int period = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 3));
        int month = (int)global::System.Math.Truncate(
            OptionalNumber(arguments, 4, 12));
        if (cost <= 0 || salvage < 0 || life <= 0
            || period < 1 || period > life + 1
            || month < 1 || month > 12)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double rate = global::System.Math.Round(
            1 - global::System.Math.Pow(salvage / cost, 1.0 / life),
            3,
            global::System.MidpointRounding.AwayFromZero);
        double accumulated = 0;
        double depreciation = 0;
        int index = 1;
        while (index <= period)
        {
            if (index == 1)
            {
                depreciation = cost * rate * month / 12;
            }
            else if (index == life + 1)
            {
                depreciation = (cost - accumulated) * rate
                    * (12 - month) / 12;
            }
            else
            {
                depreciation = (cost - accumulated) * rate;
            }
            depreciation = global::System.Math.Min(
                depreciation,
                cost - salvage - accumulated);
            accumulated = accumulated + depreciation;
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Max(0, depreciation));
    }

    static OdfFormulaResult DoubleDeclining(OdfFormulaResult[] arguments)
    {
        double cost = RequiredNumber(arguments, 0);
        double salvage = RequiredNumber(arguments, 1);
        double life = RequiredNumber(arguments, 2);
        int period = (int)global::System.Math.Truncate(
            RequiredNumber(arguments, 3));
        double factor = OptionalNumber(arguments, 4, 2);
        if (cost < 0 || salvage < 0 || life <= 0
            || period < 1 || factor < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double accumulated = 0;
        double depreciation = 0;
        int index = 1;
        while (index <= period)
        {
            depreciation = global::System.Math.Min(
                (cost - accumulated) * factor / life,
                cost - salvage - accumulated);
            depreciation = global::System.Math.Max(0, depreciation);
            accumulated = accumulated + depreciation;
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(depreciation);
    }

    static OdfFormulaResult VariableDeclining(OdfFormulaResult[] arguments)
    {
        double cost = RequiredNumber(arguments, 0);
        double salvage = RequiredNumber(arguments, 1);
        double life = RequiredNumber(arguments, 2);
        double start = RequiredNumber(arguments, 3);
        double end = RequiredNumber(arguments, 4);
        double factor = OptionalNumber(arguments, 5, 2);
        double noSwitch = OptionalNumber(arguments, 6, 0);
        if (cost < 0 || salvage < 0 || life <= 0
            || start < 0 || end < start || factor < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double accumulated = 0;
        double result = 0;
        int period = 0;
        int last = (int)global::System.Math.Ceiling(end);
        while (period < last)
        {
            double declining = global::System.Math.Max(0,
                global::System.Math.Min(
                    (cost - accumulated) * factor / life,
                    cost - salvage - accumulated));
            double remainingPeriods = life - period;
            double straight = remainingPeriods > 0
                ? global::System.Math.Max(0,
                    (cost - salvage - accumulated) / remainingPeriods)
                : 0;
            double depreciation = noSwitch == 0
                ? global::System.Math.Max(declining, straight)
                : declining;
            double segmentStart = global::System.Math.Max(start, period);
            double segmentEnd = global::System.Math.Min(end, period + 1);
            if (segmentEnd > segmentStart)
            {
                result = result + depreciation
                    * (segmentEnd - segmentStart);
            }
            accumulated = accumulated + depreciation;
            period = period + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    internal static double RequiredNumber(
        OdfFormulaResult[] arguments,
        int index)
    {
        OdfFormulaResult value = FormulaValueModel.ConvertToNumber(
            arguments[index]);
        return value.Error == OdfFormulaError.None
            ? value.NumericValue
            : double.NaN;
    }

    internal static double OptionalNumber(
        OdfFormulaResult[] arguments,
        int index,
        double fallback)
    {
        if (index >= arguments.Length
            || arguments[index].Kind == OdfValueKind.Empty)
        {
            return fallback;
        }
        return RequiredNumber(arguments, index);
    }

    static OdfFormulaResult NumberAt(
        OdfFormulaResult[] arguments,
        int index,
        double fallback)
    {
        if (index >= arguments.Length
            || arguments[index].Kind == OdfValueKind.Empty)
        {
            return FormulaValueModel.CreateNumber(fallback);
        }
        return FormulaValueModel.ConvertToNumber(arguments[index]);
    }

    internal static void CollectNumbers(
        OdfFormulaResult value,
        List<double> result)
    {
        if (value.Kind == OdfValueKind.Array
            || value.Kind == OdfValueKind.Reference)
        {
            int index = 0;
            while (index < value.Values.Length)
            {
                CollectNumbers(value.Values[index], result);
                index = index + 1;
            }
            return;
        }
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        if (number.Error == OdfFormulaError.None)
        {
            result.Add(number.NumericValue);
        }
    }

    static void CollectDateNumbers(
        OdfFormulaResult value,
        List<double> result,
        OdfSpreadsheetSettings settings)
    {
        if (value.Kind == OdfValueKind.Array
            || value.Kind == OdfValueKind.Reference)
        {
            int index = 0;
            while (index < value.Values.Length)
            {
                CollectDateNumbers(value.Values[index], result, settings);
                index = index + 1;
            }
            return;
        }
        double serial = FormulaFinancialSecurities.DateSerial(value, settings);
        if (double.IsFinite(serial))
        {
            result.Add(serial);
        }
    }

    static bool HasBothSigns(List<double> values)
    {
        bool positive = false;
        bool negative = false;
        int index = 0;
        while (index < values.Count)
        {
            positive = positive || values[index] > 0;
            negative = negative || values[index] < 0;
            index = index + 1;
        }
        return positive && negative;
    }

    static bool AllFinite(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!double.IsFinite(values[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ValidPaymentType(double value)
    {
        return value == 0 || value == 1;
    }

    static double AnnuityFactor(double rate, double periods)
    {
        return rate == 0
            ? periods
            : (global::System.Math.Pow(1 + rate, periods) - 1) / rate;
    }
}
