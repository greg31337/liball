namespace icc;

/// <summary>Compiles profile sequences into DeviceLink profiles.</summary>
public class IccProfileLinker : IIccProfileLinker
{
    internal ProfileLinkerCore Core;

    /// <summary>Initializes a profile linker.</summary>
    public IccProfileLinker()
    {
        Core = new ProfileLinkerCore();
    }

    /// <summary>Links a profile sequence.</summary>
    /// <param name="profiles">The profile sequence.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The DeviceLink profile.</returns>
    public IccProfile LinkProfileSequence(IccProfile[] profiles, IccRenderingIntent intent, IccWriteOptions options)
    {
        IccProfileLinkOptions linkOptions = new IccProfileLinkOptions();
        linkOptions.RenderingIntent = intent;
        return Core.Link(profiles, linkOptions, options);
    }

    /// <summary>Links a profile sequence using explicit DeviceLink construction options.</summary>
    /// <param name="profiles">The profile sequence.</param>
    /// <param name="linkOptions">The DeviceLink construction options.</param>
    /// <param name="writeOptions">The writing options and resource limits.</param>
    /// <returns>The encoded and decoded DeviceLink profile.</returns>
    public IccProfile LinkConfiguredProfileSequence(IccProfile[] profiles, IccProfileLinkOptions linkOptions, IccWriteOptions writeOptions)
    {
        return Core.Link(profiles, linkOptions, writeOptions);
    }

    /// <summary>Probes profile-sequence compatibility.</summary>
    /// <param name="profiles">The profile sequence.</param>
    /// <param name="intent">The rendering intent.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeProfileSequence(IccProfile[] profiles, IccRenderingIntent intent)
    {
        return Core.Probe(profiles, intent);
    }
}
