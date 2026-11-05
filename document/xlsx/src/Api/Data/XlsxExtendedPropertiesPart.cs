using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML extended properties part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxExtendedPropertiesPart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the template value used by this model.
    /// </summary>
    public string Template;
    /// <summary>
    /// Stores the manager value used by this model.
    /// </summary>
    public string Manager;
    /// <summary>
    /// Stores the company value used by this model.
    /// </summary>
    public string Company;
    /// <summary>
    /// Contains pages.
    /// </summary>
    public uint Pages;
    /// <summary>
    /// Indicates whether pages is present and should be written.
    /// </summary>
    public bool HasPages;
    /// <summary>
    /// Contains words.
    /// </summary>
    public uint Words;
    /// <summary>
    /// Indicates whether words is present and should be written.
    /// </summary>
    public bool HasWords;
    /// <summary>
    /// Contains characters.
    /// </summary>
    public uint Characters;
    /// <summary>
    /// Indicates whether characters is present and should be written.
    /// </summary>
    public bool HasCharacters;
    /// <summary>
    /// Stores the presentation format value used by this model.
    /// </summary>
    public string PresentationFormat;
    /// <summary>
    /// Contains lines.
    /// </summary>
    public uint Lines;
    /// <summary>
    /// Indicates whether lines is present and should be written.
    /// </summary>
    public bool HasLines;
    /// <summary>
    /// Contains paragraphs.
    /// </summary>
    public uint Paragraphs;
    /// <summary>
    /// Indicates whether paragraphs is present and should be written.
    /// </summary>
    public bool HasParagraphs;
    /// <summary>
    /// Contains slides.
    /// </summary>
    public uint Slides;
    /// <summary>
    /// Indicates whether slides is present and should be written.
    /// </summary>
    public bool HasSlides;
    /// <summary>
    /// Contains notes.
    /// </summary>
    public uint Notes;
    /// <summary>
    /// Indicates whether notes is present and should be written.
    /// </summary>
    public bool HasNotes;
    /// <summary>
    /// Stores the total time value used by this model.
    /// </summary>
    public uint TotalTime;
    /// <summary>
    /// Indicates whether total time is present and should be written.
    /// </summary>
    public bool HasTotalTime;
    /// <summary>
    /// Contains hidden slides.
    /// </summary>
    public uint HiddenSlides;
    /// <summary>
    /// Indicates whether hidden slides is present and should be written.
    /// </summary>
    public bool HasHiddenSlides;
    /// <summary>
    /// Contains mm clips.
    /// </summary>
    public uint MMClips;
    /// <summary>
    /// Indicates whether mm clips is present and should be written.
    /// </summary>
    public bool HasMMClips;
    /// <summary>
    /// Stores the scale crop value used by this model.
    /// </summary>
    public bool ScaleCrop;
    /// <summary>
    /// Indicates whether scale crop is present and should be written.
    /// </summary>
    public bool HasScaleCrop;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string HeadingPairsRawXml;
    /// <summary>
    /// Indicates whether heading pairs is present and should be written.
    /// </summary>
    public bool HasHeadingPairs;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string TitlesOfPartsRawXml;
    /// <summary>
    /// Indicates whether titles of parts is present and should be written.
    /// </summary>
    public bool HasTitlesOfParts;
    /// <summary>
    /// Stores the links up to date value used by this model.
    /// </summary>
    public bool LinksUpToDate;
    /// <summary>
    /// Indicates whether links up to date is present and should be written.
    /// </summary>
    public bool HasLinksUpToDate;
    /// <summary>
    /// Contains characters with spaces.
    /// </summary>
    public uint CharactersWithSpaces;
    /// <summary>
    /// Indicates whether characters with spaces is present and should be written.
    /// </summary>
    public bool HasCharactersWithSpaces;
    /// <summary>
    /// Stores the shared doc value used by this model.
    /// </summary>
    public bool SharedDoc;
    /// <summary>
    /// Indicates whether shared doc is present and should be written.
    /// </summary>
    public bool HasSharedDoc;
    /// <summary>
    /// Stores the hyperlinks changed value used by this model.
    /// </summary>
    public bool HyperlinksChanged;
    /// <summary>
    /// Indicates whether hyperlinks changed is present and should be written.
    /// </summary>
    public bool HasHyperlinksChanged;
    /// <summary>
    /// Stores the application value used by this model.
    /// </summary>
    public string Application;
    /// <summary>
    /// Stores the app version value used by this model.
    /// </summary>
    public string AppVersion;
    /// <summary>
    /// Stores the doc security value used by this model.
    /// </summary>
    public int DocSecurity;
    /// <summary>
    /// Indicates whether doc security is present and should be written.
    /// </summary>
    public bool HasDocSecurity;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new extended properties part with its default state.
    /// </summary>
    public XlsxExtendedPropertiesPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Template = "";
        Manager = "";
        Company = "";
        Pages = 0;
        HasPages = false;
        Words = 0;
        HasWords = false;
        Characters = 0;
        HasCharacters = false;
        PresentationFormat = "";
        Lines = 0;
        HasLines = false;
        Paragraphs = 0;
        HasParagraphs = false;
        Slides = 0;
        HasSlides = false;
        Notes = 0;
        HasNotes = false;
        TotalTime = 0;
        HasTotalTime = false;
        HiddenSlides = 0;
        HasHiddenSlides = false;
        MMClips = 0;
        HasMMClips = false;
        ScaleCrop = false;
        HasScaleCrop = false;
        HeadingPairsRawXml = "";
        HasHeadingPairs = false;
        TitlesOfPartsRawXml = "";
        HasTitlesOfParts = false;
        LinksUpToDate = false;
        HasLinksUpToDate = false;
        CharactersWithSpaces = 0;
        HasCharactersWithSpaces = false;
        SharedDoc = false;
        HasSharedDoc = false;
        HyperlinksChanged = false;
        HasHyperlinksChanged = false;
        Application = "";
        AppVersion = "";
        DocSecurity = 0;
        HasDocSecurity = false;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
