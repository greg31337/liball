namespace template;

/// <summary>
/// Creates built-in syntax profiles and their matching options.
/// </summary>
public class TemplateProfileCatalog : ITemplateProfileCatalog
{
    /// <summary>
    /// Initializes a built-in profile catalog.
    /// </summary>
    public TemplateProfileCatalog()
    {
    }

    /// <summary>Creates the native profile.</summary>
    /// <returns>The profile.</returns>
    public TemplateProfile CreateNativeProfile()
    {
        return NativeProfile.Create();
    }

    /// <summary>Creates native options.</summary>
    /// <returns>The options.</returns>
    public TemplateOptions CreateNativeOptions()
    {
        return NativeProfile.CreateOptions();
    }

    /// <summary>Creates the Mustache profile.</summary>
    /// <returns>The profile.</returns>
    public TemplateProfile CreateMustacheProfile()
    {
        return MustacheProfile.Create();
    }

    /// <summary>Creates Mustache options.</summary>
    /// <returns>The options.</returns>
    public TemplateOptions CreateMustacheOptions()
    {
        return MustacheProfile.CreateOptions();
    }

    /// <summary>Creates the Handlebars profile.</summary>
    /// <returns>The profile.</returns>
    public TemplateProfile CreateHandlebarsProfile()
    {
        return HandlebarsProfile.Create();
    }

    /// <summary>Creates Handlebars options.</summary>
    /// <returns>The options.</returns>
    public TemplateOptions CreateHandlebarsOptions()
    {
        return HandlebarsProfile.CreateOptions();
    }

    /// <summary>Creates the Liquid profile.</summary>
    /// <returns>The profile.</returns>
    public TemplateProfile CreateLiquidProfile()
    {
        return LiquidProfile.Create();
    }

    /// <summary>Creates Liquid options.</summary>
    /// <returns>The options.</returns>
    public TemplateOptions CreateLiquidOptions()
    {
        return LiquidProfile.CreateOptions();
    }

    /// <summary>Creates the Jinja profile.</summary>
    /// <returns>The profile.</returns>
    public TemplateProfile CreateJinjaProfile()
    {
        return JinjaProfile.Create();
    }

    /// <summary>Creates Jinja options.</summary>
    /// <returns>The options.</returns>
    public TemplateOptions CreateJinjaOptions()
    {
        return JinjaProfile.CreateOptions();
    }

    /// <summary>Creates the Django profile.</summary>
    /// <returns>The profile.</returns>
    public TemplateProfile CreateDjangoProfile()
    {
        return DjangoProfile.Create();
    }

    /// <summary>Creates Django options.</summary>
    /// <returns>The options.</returns>
    public TemplateOptions CreateDjangoOptions()
    {
        return DjangoProfile.CreateOptions();
    }
}
