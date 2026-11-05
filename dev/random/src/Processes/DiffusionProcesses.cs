using System;

namespace random;

internal static class DiffusionProcesses
{
    internal static void FillBrownianIncrements(IRandomSource generator, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        ValidateCommon(generator, timeStep, pathCount, stepCount);
        double scale = Sqrt(generator, timeStep);
        NormalSampler normal = new NormalSampler(generator, 0.0, 1.0);
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = scale * normal.Sample();
            index++;
        }
    }

    internal static void FillBrownianMotion(IRandomSource generator, double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        ValidateCommon(generator, timeStep, pathCount, stepCount);
        Guard.Finite(initialState, "initialState");
        Guard.Finite(drift, "drift");
        ValidateNonnegative(volatility, "volatility");
        double driftStep = drift * timeStep;
        double randomScale = volatility * Sqrt(generator, timeStep);
        ValidateFiniteTransition(driftStep, "drift");
        ValidateFiniteTransition(randomScale, "volatility");
        NormalSampler normal = new NormalSampler(generator, 0.0, 1.0);
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            double state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step <= stepCount)
            {
                state += driftStep;
                if (volatility != 0.0)
                {
                    state += randomScale * normal.Sample();
                }

                destination[offset + step] = state;
                step++;
            }

            path++;
        }
    }

    internal static void FillBrownianBridge(IRandomSource generator, double initialState, double finalState, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        ValidateCommon(generator, timeStep, pathCount, stepCount);
        Guard.Finite(initialState, "initialState");
        Guard.Finite(finalState, "finalState");
        ValidateNonnegative(volatility, "volatility");
        if (stepCount <= 0)
        {
            throw RandomArgumentException.Create("stepCount", "A Brownian bridge requires at least one step between its endpoints.");
        }

        double maximumRandomScale = volatility * Sqrt(generator, timeStep);
        ValidateFiniteTransition(maximumRandomScale, "volatility");
        NormalSampler normal = new NormalSampler(generator, 0.0, 1.0);
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            double state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step < stepCount)
            {
                int remaining = stepCount - step + 1;
                double previousWeight = (double)(remaining - 1) / (double)remaining;
                double finalWeight = 1.0 / (double)remaining;
                double mean = previousWeight * state + finalWeight * finalState;
                if (volatility != 0.0)
                {
                    double varianceTime = timeStep * (double)(remaining - 1) / (double)remaining;
                    mean += volatility * Sqrt(generator, varianceTime) * normal.Sample();
                }

                state = mean;
                destination[offset + step] = state;
                step++;
            }

            destination[offset + stepCount] = finalState;
            path++;
        }
    }

    internal static void FillGeometricBrownianMotion(IRandomSource generator, double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        ValidateCommon(generator, timeStep, pathCount, stepCount);
        Guard.Finite(initialState, "initialState");
        if (initialState <= 0.0)
        {
            throw RandomArgumentException.Create("initialState", "Geometric Brownian motion requires a positive initial state.");
        }

        Guard.Finite(drift, "drift");
        ValidateNonnegative(volatility, "volatility");
        double volatilitySquared = volatility * volatility;
        if (!double.IsFinite(volatilitySquared))
        {
            throw RandomArgumentException.Create("volatility", "The volatility is too large for the exact transition.");
        }

        double exponentDrift = (drift - 0.5 * volatilitySquared) * timeStep;
        double exponentScale = volatility * Sqrt(generator, timeStep);
        ValidateFiniteTransition(exponentDrift, "drift");
        ValidateFiniteTransition(exponentScale, "volatility");
        NormalSampler normal = new NormalSampler(generator, 0.0, 1.0);
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            double state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step <= stepCount)
            {
                double exponent = exponentDrift;
                if (volatility != 0.0)
                {
                    exponent += exponentScale * normal.Sample();
                }

                state *= Exp(generator, exponent);
                destination[offset + step] = state;
                step++;
            }

            path++;
        }
    }

    internal static void FillOrnsteinUhlenbeck(IRandomSource generator, double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        ValidateCommon(generator, timeStep, pathCount, stepCount);
        Guard.Finite(initialState, "initialState");
        Guard.Finite(longTermMean, "longTermMean");
        ValidateNonnegative(meanReversion, "meanReversion");
        ValidateNonnegative(volatility, "volatility");
        double decay;
        double varianceTime;
        if (meanReversion == 0.0)
        {
            decay = 1.0;
            varianceTime = timeStep;
        }
        else
        {
            double rateTime = meanReversion * timeStep;
            decay = Exp(generator, -rateTime);
            double numerator = -Expm1(generator, -2.0 * rateTime);
            double denominator = 2.0 * meanReversion;
            if (double.IsPositiveInfinity(denominator))
            {
                varianceTime = 0.5 / meanReversion;
            }
            else
            {
                varianceTime = numerator / denominator;
            }
        }

        double randomScale = volatility * Sqrt(generator, varianceTime);
        ValidateFiniteTransition(randomScale, "volatility");
        NormalSampler normal = new NormalSampler(generator, 0.0, 1.0);
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            double state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step <= stepCount)
            {
                state = decay * state + (1.0 - decay) * longTermMean;
                if (volatility != 0.0)
                {
                    state += randomScale * normal.Sample();
                }

                destination[offset + step] = state;
                step++;
            }

            path++;
        }
    }

    static void ValidateCommon(IRandomSource generator, double timeStep, int pathCount, int stepCount)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(timeStep, "timeStep");
        if (timeStep <= 0.0)
        {
            throw RandomArgumentException.Create("timeStep", "The time step must be positive.");
        }

        Guard.Count(pathCount, "pathCount");
        Guard.Count(stepCount, "stepCount");
    }

    static void ValidateNonnegative(double value, string argumentName)
    {
        Guard.Finite(value, argumentName);
        if (value < 0.0)
        {
            throw RandomArgumentException.Create(argumentName, "The value must not be negative.");
        }
    }

    static void ValidateFiniteTransition(double value, string argumentName)
    {
        if (!double.IsFinite(value))
        {
            throw RandomArgumentException.Create(argumentName, "The parameters produce a non-finite transition coefficient.");
        }
    }

    static double Sqrt(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    static double Exp(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
    }

    static double Expm1(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (value < -0.5 || value > 0.5)
        {
            return Math.Exp(value) - 1.0;
        }

        double half = 0.5 * value;
        return 2.0 * Math.Exp(half) * Math.Sinh(half);
    }
}
