using System;

namespace random;

/// <summary>
/// Samples through a caller-supplied inverse cumulative distribution transform.
/// </summary>
/// <remarks>
/// Each call supplies exactly one uniform value strictly inside <c>(0, 1)</c> to the
/// built-in <c>Func&lt;double, double&gt;</c> transform. The delegate is then invoked exactly
/// once. Delegate state remains caller-owned, and delegate exceptions are propagated
/// unchanged. The sampler retains the supplied mutable generator and is not safe for
/// concurrent use without external synchronization.
/// </remarks>
public class InverseTransformSampler : IInverseTransformSampler
{
    IRandomSource generator;
    Func<double, double> inverseCdf;
    InverseTransformOutputValidation outputValidation;

    /// <summary>
    /// Creates an inverse-transform sampler with explicit output validation.
    /// </summary>
    /// <param name="generator">The generator that supplies open-unit inputs.</param>
    /// <param name="inverseCdf">The non-null inverse cumulative distribution transform.</param>
    /// <param name="outputValidation">The validation applied after each delegate invocation.</param>
    /// <remarks>
    /// Rejection occurs after the open-unit draw and delegate call, so a rejected output
    /// consumes the corresponding generator state. Use <see cref="InverseTransformOutputValidation.None"/>
    /// only when non-finite outputs are an intentional part of the caller's contract.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator or transform is null, or the validation option is unsupported.</exception>
    public InverseTransformSampler(IRandomSource generator, Func<double, double> inverseCdf, InverseTransformOutputValidation outputValidation)
    {
        ValidateParameters(generator, inverseCdf, outputValidation);
        this.generator = generator;
        this.inverseCdf = inverseCdf;
        this.outputValidation = outputValidation;
    }

    /// <summary>
    /// Returns the configured transform-output validation policy.
    /// </summary>
    /// <returns>The validation policy supplied at construction.</returns>
    public InverseTransformOutputValidation OutputValidation()
    {
        return outputValidation;
    }

    /// <summary>
    /// Applies the inverse transform to one open-unit random input.
    /// </summary>
    /// <returns>The validated transform output.</returns>
    /// <exception cref="RandomStateException">The transform output violates the configured validation policy.</exception>
    public double Sample()
    {
        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double value = inverseCdf(unit);
        if (outputValidation == InverseTransformOutputValidation.RejectNaN)
        {
            if (double.IsNaN(value))
            {
                throw RandomStateException.Create("The inverse transform returned NaN.");
            }
        }
        else if (outputValidation == InverseTransformOutputValidation.RequireFinite)
        {
            if (!double.IsFinite(value))
            {
                throw RandomStateException.Create("The inverse transform returned a non-finite value.");
            }
        }

        return value;
    }

    /// <summary>
    /// Fills a destination through repeated inverse-transform sampling.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>
    /// The result and generator state match repeated calls to <see cref="Sample()"/>.
    /// If validation or the delegate throws, earlier destination entries remain written.
    /// </remarks>
    /// <exception cref="RandomStateException">A transform output violates the configured validation policy.</exception>
    public void Fill(Span<double> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    static void ValidateParameters(IRandomSource generator, Func<double, double> inverseCdf, InverseTransformOutputValidation outputValidation)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(inverseCdf, "inverseCdf");
        if (outputValidation != InverseTransformOutputValidation.None &&
            outputValidation != InverseTransformOutputValidation.RejectNaN &&
            outputValidation != InverseTransformOutputValidation.RequireFinite)
        {
            throw RandomArgumentException.Create("outputValidation", "The inverse-transform output validation option is not supported.");
        }
    }
}
