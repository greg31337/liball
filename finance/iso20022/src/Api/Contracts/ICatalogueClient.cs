namespace iso20022;

/// <summary>Downloads bounded ISO catalogue artifacts using explicit request policy.</summary>
public interface ICatalogueClient
{
    /// <summary>Downloads one catalogue artifact and captures its integrity provenance.</summary>
    /// <param name="request">The URI, conditional headers, limits, and provenance inputs.</param><returns>The download result.</returns>
    CatalogueDownloadResult DownloadCatalogue(CatalogueDownloadRequest? request);
}
