namespace odf.Tests;

public class FormulaFinancialFunctionTests
{
    [Test]
    public void PublishesCompleteFinancialFunctionMetadata()
    {
        OdfFormulaFunctionInfo accrual = FormulaFunctionCatalog.Find(
            OdfFormulaNames.ACCRINT)!;
        OdfFormulaFunctionInfo presentValue = FormulaFunctionCatalog.Find(
            OdfFormulaNames.PV)!;
        OdfFormulaFunctionInfo duration = FormulaFunctionCatalog.Find(
            OdfFormulaNames.DURATION)!;
        OdfFormulaFunctionInfo paymentInterest = FormulaFunctionCatalog.Find(
            OdfFormulaNames.IPMT)!;

        Assert.Multiple(() =>
        {
            Assert.That(FormulaFunctionCatalog.Functions,
                Has.Length.EqualTo(393));
            Assert.That(accrual.Group, Is.EqualTo("Financial"));
            Assert.That(accrual.Conformance,
                Is.EqualTo(OdfFormulaConformance.Unknown));
            Assert.That(presentValue.Conformance,
                Is.EqualTo(OdfFormulaConformance.Small));
            Assert.That(duration.Conformance,
                Is.EqualTo(OdfFormulaConformance.Medium));
            Assert.That(paymentInterest.Conformance,
                Is.EqualTo(OdfFormulaConformance.Large));
            Assert.That(paymentInterest.IntroducedVersion,
                Is.EqualTo(OdfVersion.Version12));
        });
    }

    [Test]
    public void EvaluatesTimeValuePaymentsAndInverseRateFunctions()
    {
        FormulaFunctionContext context = CreateContext();
        double rate = 0.10 / 12;
        OdfFormulaResult payment = Evaluate(
            OdfFormulaNames.PMT,
            [Number(rate), Number(360), Number(100000)],
            context);
        OdfFormulaResult present = Evaluate(
            OdfFormulaNames.PV,
            [Number(rate), Number(360), payment],
            context);
        OdfFormulaResult future = Evaluate(
            OdfFormulaNames.FV,
            [Number(rate), Number(360), payment, Number(100000)],
            context);
        OdfFormulaResult periods = Evaluate(
            OdfFormulaNames.NPER,
            [Number(rate), payment, Number(100000)],
            context);
        OdfFormulaResult solvedRate = Evaluate(
            OdfFormulaNames.RATE,
            [Number(360), payment, Number(100000)],
            context);
        OdfFormulaResult interest = Evaluate(
            OdfFormulaNames.IPMT,
            [Number(rate), Number(1), Number(360), Number(100000)],
            context);
        OdfFormulaResult principal = Evaluate(
            OdfFormulaNames.PPMT,
            [Number(rate), Number(1), Number(360), Number(100000)],
            context);
        OdfFormulaResult cumulativeInterest = Evaluate(
            OdfFormulaNames.CUMIPMT,
            [Number(rate), Number(360), Number(100000),
                Number(1), Number(12), Number(0)],
            context);
        OdfFormulaResult cumulativePrincipal = Evaluate(
            OdfFormulaNames.CUMPRINC,
            [Number(rate), Number(360), Number(100000),
                Number(1), Number(12), Number(0)],
            context);
        OdfFormulaResult straightInterest = Evaluate(
            OdfFormulaNames.ISPMT,
            [Number(rate), Number(1), Number(360), Number(100000)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(payment.NumericValue,
                Is.EqualTo(-877.5715700888).Within(0.000001));
            Assert.That(present.NumericValue,
                Is.EqualTo(100000).Within(0.000001));
            Assert.That(future.NumericValue, Is.Zero.Within(0.000001));
            Assert.That(periods.NumericValue,
                Is.EqualTo(360).Within(0.000001));
            Assert.That(solvedRate.NumericValue,
                Is.EqualTo(rate).Within(0.000000001));
            Assert.That(interest.NumericValue,
                Is.EqualTo(-833.3333333333).Within(0.000001));
            Assert.That(interest.NumericValue + principal.NumericValue,
                Is.EqualTo(payment.NumericValue).Within(0.000001));
            Assert.That(cumulativeInterest.NumericValue, Is.LessThan(0));
            Assert.That(cumulativePrincipal.NumericValue, Is.LessThan(0));
            Assert.That(straightInterest.NumericValue, Is.LessThan(0));
        });
    }

    [Test]
    public void EvaluatesCashFlowsAndIrregularDateCashFlows()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult simpleFlows = Array(
            [-100.0, 110.0]);
        OdfFormulaResult npvFlows = Array([100.0, 200.0]);
        OdfFormulaResult npv = Evaluate(
            OdfFormulaNames.NPV,
            [Number(0.1), npvFlows],
            context);
        OdfFormulaResult irr = Evaluate(
            OdfFormulaNames.IRR,
            [simpleFlows],
            context);
        OdfFormulaResult mirr = Evaluate(
            OdfFormulaNames.MIRR,
            [Array([-1000.0, 300.0, 400.0, 500.0]),
                Number(0.08), Number(0.10)],
            context);
        OdfFormulaResult scheduled = Evaluate(
            OdfFormulaNames.FVSCHEDULE,
            [Number(1000), Array([0.1, 0.1])],
            context);
        OdfFormulaResult dates = FormulaValueModel.CreateArray(
            [Date(2024, 1, 1, context), Date(2024, 12, 31, context)],
            1,
            2);
        OdfFormulaResult xnpv = Evaluate(
            OdfFormulaNames.XNPV,
            [Number(0.1), simpleFlows, dates],
            context);
        OdfFormulaResult xirr = Evaluate(
            OdfFormulaNames.XIRR,
            [simpleFlows, dates],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(npv.NumericValue,
                Is.EqualTo(100 / 1.1 + 200 / 1.21).Within(0.000000001));
            Assert.That(irr.NumericValue, Is.EqualTo(0.1).Within(0.000000001));
            Assert.That(mirr.Error, Is.EqualTo(OdfFormulaError.None));
            Assert.That(scheduled.NumericValue, Is.EqualTo(1210));
            Assert.That(xnpv.NumericValue, Is.Zero.Within(0.000000001));
            Assert.That(xirr.NumericValue,
                Is.EqualTo(0.1).Within(0.000000001));
        });
    }

    [Test]
    public void EvaluatesRateConversionsGrowthAndFractionalDollars()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult effective = Evaluate(
            OdfFormulaNames.EFFECT,
            [Number(0.12), Number(12)],
            context);
        OdfFormulaResult nominal = Evaluate(
            OdfFormulaNames.NOMINAL,
            [effective, Number(12)],
            context);
        OdfFormulaResult rri = Evaluate(
            OdfFormulaNames.RRI,
            [Number(10), Number(100), Number(200)],
            context);
        OdfFormulaResult duration = Evaluate(
            OdfFormulaNames.PDURATION,
            [rri, Number(100), Number(200)],
            context);
        OdfFormulaResult decimalDollar = Evaluate(
            OdfFormulaNames.DOLLARDE,
            [Number(1.02), Number(16)],
            context);
        OdfFormulaResult fractionalDollar = Evaluate(
            OdfFormulaNames.DOLLARFR,
            [decimalDollar, Number(16)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(nominal.NumericValue,
                Is.EqualTo(0.12).Within(0.000000001));
            Assert.That(duration.NumericValue,
                Is.EqualTo(10).Within(0.000000001));
            Assert.That(decimalDollar.NumericValue, Is.EqualTo(1.125));
            Assert.That(fractionalDollar.NumericValue, Is.EqualTo(1.02));
        });
    }

    [Test]
    public void EvaluatesEveryDepreciationMethod()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult straight = Evaluate(
            OdfFormulaNames.SLN,
            [Number(1000), Number(100), Number(5)],
            context);
        OdfFormulaResult sumYears = Evaluate(
            OdfFormulaNames.SYD,
            [Number(1000), Number(100), Number(5), Number(1)],
            context);
        OdfFormulaResult doubleDeclining = Evaluate(
            OdfFormulaNames.DDB,
            [Number(1000), Number(100), Number(5), Number(1)],
            context);
        OdfFormulaResult fixedDeclining = Evaluate(
            OdfFormulaNames.DB,
            [Number(1000), Number(100), Number(5), Number(1)],
            context);
        OdfFormulaResult variable = Evaluate(
            OdfFormulaNames.VDB,
            [Number(1000), Number(100), Number(5), Number(0), Number(1)],
            context);
        OdfFormulaResult amor = Evaluate(
            OdfFormulaNames.AMORLINC,
            [Number(1000), Date(2024, 1, 1, context),
                Date(2024, 7, 1, context), Number(100),
                Number(0), Number(0.2), Number(1)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(straight.NumericValue, Is.EqualTo(180));
            Assert.That(sumYears.NumericValue, Is.EqualTo(300));
            Assert.That(doubleDeclining.NumericValue, Is.EqualTo(400));
            Assert.That(fixedDeclining.NumericValue,
                Is.EqualTo(369).Within(0.000001));
            Assert.That(variable.NumericValue, Is.EqualTo(400));
            Assert.That(amor.NumericValue,
                Is.EqualTo(1000 * 0.2 * 182 / 366.0).Within(0.000001));
        });
    }

    [Test]
    public void EvaluatesCouponDatesCountsAndAccruedInterest()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult settlement = Date(2024, 3, 1, context);
        OdfFormulaResult maturity = Date(2025, 1, 1, context);
        OdfFormulaResult previous = Evaluate(
            OdfFormulaNames.COUPPCD,
            [settlement, maturity, Number(2), Number(1)],
            context);
        OdfFormulaResult next = Evaluate(
            OdfFormulaNames.COUPNCD,
            [settlement, maturity, Number(2), Number(1)],
            context);
        OdfFormulaResult count = Evaluate(
            OdfFormulaNames.COUPNUM,
            [settlement, maturity, Number(2), Number(1)],
            context);
        OdfFormulaResult elapsed = Evaluate(
            OdfFormulaNames.COUPDAYBS,
            [settlement, maturity, Number(2), Number(1)],
            context);
        OdfFormulaResult period = Evaluate(
            OdfFormulaNames.COUPDAYS,
            [settlement, maturity, Number(2), Number(1)],
            context);
        OdfFormulaResult remaining = Evaluate(
            OdfFormulaNames.COUPDAYSNC,
            [settlement, maturity, Number(2), Number(1)],
            context);
        OdfFormulaResult maturityAccrual = Evaluate(
            OdfFormulaNames.ACCRINTM,
            [Date(2024, 1, 1, context), Date(2024, 7, 1, context),
                Number(0.1), Number(1000), Number(1)],
            context);
        OdfFormulaResult accrual = Evaluate(
            OdfFormulaNames.ACCRINT,
            [Date(2024, 1, 1, context), Date(2024, 7, 1, context),
                settlement, Number(0.1), Number(1000),
                Number(2), Number(1), Number(1)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(previous.StringValue, Is.EqualTo("2024-01-01"));
            Assert.That(next.StringValue, Is.EqualTo("2024-07-01"));
            Assert.That(count.NumericValue, Is.EqualTo(2));
            Assert.That(elapsed.NumericValue, Is.EqualTo(60));
            Assert.That(period.NumericValue, Is.EqualTo(182));
            Assert.That(remaining.NumericValue, Is.EqualTo(122));
            Assert.That(maturityAccrual.NumericValue,
                Is.EqualTo(1000 * 0.1 * 182 / 366.0).Within(0.000001));
            Assert.That(accrual.Error, Is.EqualTo(OdfFormulaError.None));
        });
    }

    [Test]
    public void EvaluatesDiscountAndTreasurySecurities()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult settlement = Date(2024, 1, 1, context);
        OdfFormulaResult maturity = Date(2024, 6, 29, context);
        OdfFormulaResult price = Evaluate(
            OdfFormulaNames.PRICEDISC,
            [settlement, maturity, Number(0.08), Number(100), Number(2)],
            context);
        OdfFormulaResult discount = Evaluate(
            OdfFormulaNames.DISC,
            [settlement, maturity, price, Number(100), Number(2)],
            context);
        OdfFormulaResult yield = Evaluate(
            OdfFormulaNames.YIELDDISC,
            [settlement, maturity, price, Number(100), Number(2)],
            context);
        OdfFormulaResult received = Evaluate(
            OdfFormulaNames.RECEIVED,
            [settlement, maturity, Number(96), Number(0.08), Number(2)],
            context);
        OdfFormulaResult interest = Evaluate(
            OdfFormulaNames.INTRATE,
            [settlement, maturity, Number(96), received, Number(2)],
            context);
        OdfFormulaResult billPrice = Evaluate(
            OdfFormulaNames.TBILLPRICE,
            [settlement, maturity, Number(0.08)],
            context);
        OdfFormulaResult billYield = Evaluate(
            OdfFormulaNames.TBILLYIELD,
            [settlement, maturity, billPrice],
            context);
        OdfFormulaResult billEquivalent = Evaluate(
            OdfFormulaNames.TBILLEQ,
            [settlement, maturity, Number(0.08)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(price.NumericValue, Is.EqualTo(96));
            Assert.That(discount.NumericValue,
                Is.EqualTo(0.08).Within(0.000000001));
            Assert.That(yield.NumericValue, Is.GreaterThan(0.08));
            Assert.That(interest.NumericValue,
                Is.EqualTo(1.0 / 12.0).Within(0.000000001));
            Assert.That(billPrice.NumericValue, Is.EqualTo(96));
            Assert.That(billYield.NumericValue, Is.GreaterThan(0.08));
            Assert.That(billEquivalent.NumericValue, Is.GreaterThan(0.08));
        });
    }

    [Test]
    public void EvaluatesRegularMaturityAndOddCouponBonds()
    {
        FormulaFunctionContext context = CreateContext();
        OdfFormulaResult settlement = Date(2024, 3, 1, context);
        OdfFormulaResult maturity = Date(2027, 1, 1, context);
        OdfFormulaResult price = Evaluate(
            OdfFormulaNames.PRICE,
            [settlement, maturity, Number(0.05), Number(0.06),
                Number(100), Number(2), Number(1)],
            context);
        OdfFormulaResult yield = Evaluate(
            OdfFormulaNames.YIELD,
            [settlement, maturity, Number(0.05), price,
                Number(100), Number(2), Number(1)],
            context);
        OdfFormulaResult duration = Evaluate(
            OdfFormulaNames.DURATION,
            [settlement, maturity, Number(0.05), Number(0.06),
                Number(2), Number(1)],
            context);
        OdfFormulaResult modified = Evaluate(
            OdfFormulaNames.MDURATION,
            [settlement, maturity, Number(0.05), Number(0.06),
                Number(2), Number(1)],
            context);
        OdfFormulaResult issue = Date(2024, 1, 1, context);
        OdfFormulaResult maturityPrice = Evaluate(
            OdfFormulaNames.PRICEMAT,
            [settlement, maturity, issue, Number(0.05), Number(0.06), Number(1)],
            context);
        OdfFormulaResult maturityYield = Evaluate(
            OdfFormulaNames.YIELDMAT,
            [settlement, maturity, issue, Number(0.05), maturityPrice, Number(1)],
            context);
        OdfFormulaResult oddFirstPrice = Evaluate(
            OdfFormulaNames.ODDFPRICE,
            [settlement, maturity, issue, Date(2024, 7, 1, context),
                Number(0.05), Number(0.06), Number(100), Number(2), Number(1)],
            context);
        OdfFormulaResult oddFirstYield = Evaluate(
            OdfFormulaNames.ODDFYIELD,
            [settlement, maturity, issue, Date(2024, 7, 1, context),
                Number(0.05), oddFirstPrice, Number(100), Number(2), Number(1)],
            context);
        OdfFormulaResult oddLastPrice = Evaluate(
            OdfFormulaNames.ODDLPRICE,
            [settlement, maturity, Date(2026, 7, 1, context),
                Number(0.05), Number(0.06), Number(100), Number(2), Number(1)],
            context);
        OdfFormulaResult oddLastYield = Evaluate(
            OdfFormulaNames.ODDLYIELD,
            [settlement, maturity, Date(2026, 7, 1, context),
                Number(0.05), oddLastPrice, Number(100), Number(2), Number(1)],
            context);

        Assert.Multiple(() =>
        {
            Assert.That(price.NumericValue, Is.GreaterThan(0));
            Assert.That(yield.NumericValue,
                Is.EqualTo(0.06).Within(0.000000001));
            Assert.That(duration.NumericValue, Is.GreaterThan(0));
            Assert.That(modified.NumericValue,
                Is.LessThan(duration.NumericValue));
            Assert.That(maturityYield.NumericValue,
                Is.EqualTo(0.06).Within(0.000000001));
            Assert.That(oddFirstYield.NumericValue,
                Is.EqualTo(0.06).Within(0.000000001));
            Assert.That(oddLastYield.NumericValue,
                Is.EqualTo(0.06).Within(0.000000001));
        });
    }

    static FormulaFunctionContext CreateContext()
    {
        FormulaFunctionContext context = new FormulaFunctionContext();
        context.SpreadsheetSettings.NullDate = "2024-01-01";
        return context;
    }

    static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        return FormulaFunctionEvaluator.Evaluate(name, arguments, context);
    }

    static OdfFormulaResult Number(double value)
    {
        return FormulaValueModel.CreateNumber(value);
    }

    static OdfFormulaResult Array(double[] values)
    {
        OdfFormulaResult[] results = new OdfFormulaResult[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            results[index] = Number(values[index]);
            index = index + 1;
        }
        return FormulaValueModel.CreateArray(results, 1, results.Length);
    }

    static OdfFormulaResult Date(
        int year,
        int month,
        int day,
        FormulaFunctionContext context)
    {
        return Evaluate(
            OdfFormulaNames.DATE,
            [Number(year), Number(month), Number(day)],
            context);
    }
}
