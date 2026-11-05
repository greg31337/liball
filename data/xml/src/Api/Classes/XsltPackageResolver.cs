using System;

namespace xml;

/// <summary>Resolves and compiles reusable XSLT packages and stylesheet modules.</summary>
public class XsltPackageResolver : IXsltPackageResolver
{
    /// <summary>Creates an XSLT package resolver.</summary>
    public XsltPackageResolver()
    {
    }

    /// <summary>Resolves an XSLT package.</summary>
    public XsltExecutable ResolveXsltPackage(XsltPackageReference package, XsltCompileOptions options)
    {
        Guard.NotNull(package, "package");
        Guard.NotNull(options, "options");
        new XsltCompiler().ValidateXsltCompileOptions(options);
        ValidateXsltPackageRequest(package);
        string location = package.LocationUri == null ? "" : package.LocationUri.Trim();

        XmlDefaultResolver resolver = new XmlDefaultResolver();
        XsltPackageCompilationContext context = new XsltPackageCompilationContext();
        XsltStylesheetLoader loader = new XsltStylesheetLoader(options, resolver, context, true);
        XmlResolvedResource resource = loader.Resolver.ResolveXmlResource(XmlResourceKind.Package, null, location, loader.Options.BaseUri, loader.Policy);
        XmlInput? input = resource.Input;
        if (input == null || resource.AbsoluteUri == null || resource.AbsoluteUri.Length == 0)
        {
            throw new InvalidOperationException("The resolved XSLT package has no input or absolute URI.");
        }

        XmlInput compilationInput = loader.CopyXsltInput(input, resource.AbsoluteUri);
        if (resource.OwnsResource)
        {
            compilationInput.OwnsResource = true;
        }

        XsltExecutable executable = loader.CompileXsltStylesheet(compilationInput);
        ValidateXsltResolvedPackageReference(package, executable);
        return executable;
    }

    /// <summary>Resolves an XSLT module.</summary>
    public XmlResolvedResource ResolveXsltModule(string uri, string? baseUri, XmlResolverPolicy policy)
    {
        Guard.NotNull(uri, "uri");
        Guard.NotNull(policy, "policy");
        string value = uri.Trim();
        if (value.Length == 0)
        {
            throw new ArgumentException("An XSLT module URI is required.", "uri");
        }

        XsltCompileOptions options = new XsltCompileOptions();
        options.ResolverPolicy = policy;
        XsltStylesheetLoader loader = new XsltStylesheetLoader(options, new XmlDefaultResolver(), new XsltPackageCompilationContext(), true);
        return loader.Resolver.ResolveXmlResource(XmlResourceKind.Stylesheet, null, value, baseUri, loader.Policy);
    }

    internal void ValidateXsltPackageRequest(XsltPackageReference package)
    {
        string? name = ReadXsltPackageReferenceName(package.Name);
        if (name == null || name.Trim().Length == 0 || !Uri.IsWellFormedUriString(name.Trim(), UriKind.Absolute))
        {
            throw new ArgumentException("An XSLT package request requires an absolute package name URI.", "package");
        }

        string? location = package.LocationUri;
        if (location == null || location.Trim().Length == 0)
        {
            throw new ArgumentException("An XSLT package request requires a location URI.", "package");
        }

        string version = package.Version == null || package.Version.Trim().Length == 0 ? "*" : package.Version.Trim();
        if (!new XsltPackageVersionRules().IsValidXsltPackageVersionRange(version))
        {
            throw new ArgumentException("The requested XSLT package version range is invalid.", "package");
        }
    }

    internal void ValidateXsltResolvedPackageReference(XsltPackageReference reference, XsltExecutable executable)
    {
        XsltStylesheetPlan? stylesheet = executable.CompiledPlan == null ? executable.Plan as XsltStylesheetPlan : executable.CompiledPlan;
        XsltPackagePlan? package = stylesheet == null ? null : stylesheet.Package;
        string? requestedName = ReadXsltPackageReferenceName(reference.Name);
        string? actualName = ReadXsltPackageReferenceName(executable.PackageName);
        string requestedVersion = reference.Version == null || reference.Version.Trim().Length == 0 ? "*" : reference.Version.Trim();
        string actualVersion = executable.PackageVersion == null ? "1" : executable.PackageVersion;
        XsltPackageVersionRules versions = new XsltPackageVersionRules();
        if (package == null || !package.IsExplicit || !package.IsLibrary || executable.CompiledPlan == null && (!executable.IsImmutable || !executable.IsThreadSafe) || (requestedName != null && requestedName != actualName) || !versions.MatchesXsltPackageVersionRange(actualVersion, requestedVersion))
        {
            string message = "The resolved XSLT package does not match the requested name and version.";
            XmlDiagnostic diagnostic = new XmlDiagnostic();
            diagnostic.Code = XsltErrorCodes.XTSE3000;
            diagnostic.Category = XmlErrorCategory.XsltStatic;
            diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
            diagnostic.Message = message;
            throw new XsltStaticException(message, diagnostic, null);
        }
    }

    internal string? ReadXsltPackageReferenceName(XmlName? name)
    {
        if (name == null)
        {
            return null;
        }

        if (name.NamespaceUri != null && name.NamespaceUri.Length != 0)
        {
            return name.NamespaceUri;
        }

        if (name.QualifiedName != null && name.QualifiedName.Length != 0)
        {
            return name.QualifiedName;
        }

        return name.LocalName;
    }
}
