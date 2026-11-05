using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Stores one <c>srcset</c> image candidate URL and its optional width or pixel-density descriptor.</summary>
public class HtmlSrcsetCandidate : IHtmlSrcsetCandidate
{
    internal string url;
    internal bool hasWidth;
    internal bool hasDensity;
    internal int width;
    internal double density;
    internal HtmlSrcsetCandidate(string urlValue, bool hasWidthValue, bool hasDensityValue, int widthValue, double densityValue)
    {
        this.url = urlValue;
        this.hasWidth = hasWidthValue;
        this.hasDensity = hasDensityValue;
        this.width = widthValue;
        this.density = densityValue;
    }

    /// <summary>Creates a candidate without a width or pixel-density descriptor.</summary>
    /// <param name = "url">The nonempty candidate URL.</param>
    /// <returns>A new candidate containing only the supplied URL.</returns>
    internal static HtmlSrcsetCandidate Create(string url)
    {
        ValidateUrl(url);
        return new HtmlSrcsetCandidate(url, false, false, 0, 0);
    }

    /// <summary>Creates a candidate with a positive width descriptor.</summary>
    /// <param name = "url">The nonempty candidate URL.</param>
    /// <param name = "width">The intrinsic width in CSS pixels.</param>
    /// <returns>A new candidate containing the supplied URL and width descriptor.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    internal static HtmlSrcsetCandidate CreateWidth(string url, int width)
    {
        ValidateUrl(url);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", width, "A srcset width descriptor must be greater than zero.");
        }

        return new HtmlSrcsetCandidate(url, true, false, width, 0);
    }

    /// <summary>Creates a candidate with a positive finite pixel-density descriptor.</summary>
    /// <param name = "url">The nonempty candidate URL.</param>
    /// <param name = "density">The device-pixel density multiplier.</param>
    /// <returns>A new candidate containing the supplied URL and pixel-density descriptor.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    internal static HtmlSrcsetCandidate CreateDensity(string url, double density)
    {
        ValidateUrl(url);
        if (density <= 0 || double.IsInfinity(density) || double.IsNaN(density))
        {
            throw new ArgumentOutOfRangeException("density", density, "A srcset density descriptor must be finite and positive.");
        }

        return new HtmlSrcsetCandidate(url, false, true, 0, density);
    }

    /// <summary>Gets the candidate URL exactly as parsed or supplied.</summary>
    /// <returns>The non-empty URL text without surrounding HTML whitespace.</returns>
    public string GetUrl()
    {
        return this.url;
    }

    /// <summary>Reports whether this candidate uses an intrinsic-width descriptor.</summary>
    /// <returns><c>true</c> when the candidate has a width descriptor; otherwise, <c>false</c>.</returns>
    public bool HasWidth()
    {
        return this.hasWidth;
    }

    /// <summary>Gets the candidate's intrinsic-width descriptor.</summary>
    /// <returns>The positive width in CSS pixels, or <c>0</c> when <see cref = "HasWidth"/> is <c>false</c>.</returns>
    public int GetWidth()
    {
        return this.width;
    }

    /// <summary>Reports whether this candidate uses a pixel-density descriptor.</summary>
    /// <returns><c>true</c> when the candidate has a pixel-density descriptor; otherwise, <c>false</c>.</returns>
    public bool HasDensity()
    {
        return this.hasDensity;
    }

    /// <summary>Gets the candidate's device-pixel-density descriptor.</summary>
    /// <returns>The positive finite density multiplier, or <c>0</c> when <see cref = "HasDensity"/> is <c>false</c>.</returns>
    public double GetDensity()
    {
        return this.density;
    }

    internal static void ValidateUrl(string url)
    {
        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        if (url.Length == 0 || url[0] == ',' || url[url.Length - 1] == ',')
        {
            throw new ArgumentException("A srcset URL must be nonempty and cannot start or end with a comma.", "url");
        }

        int index = 0;
        while (index < url.Length)
        {
            if (Ascii.IsHtmlWhitespace(url[index]))
            {
                throw new ArgumentException("A srcset URL cannot contain HTML whitespace.", "url");
            }

            index = index + 1;
        }
    }
}
