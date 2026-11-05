using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents date group item data used by the public workbook API.
/// </summary>
public class XlsxDateGroupItem
{
    /// <summary>
    /// Stores the year value used by this model.
    /// </summary>
    public uint Year;
    /// <summary>
    /// Indicates whether year is present and should be written.
    /// </summary>
    public bool HasYear;
    /// <summary>
    /// Stores the month value used by this model.
    /// </summary>
    public uint Month;
    /// <summary>
    /// Indicates whether month is present and should be written.
    /// </summary>
    public bool HasMonth;
    /// <summary>
    /// Stores the day value used by this model.
    /// </summary>
    public uint Day;
    /// <summary>
    /// Indicates whether day is present and should be written.
    /// </summary>
    public bool HasDay;
    /// <summary>
    /// Stores the hour value used by this model.
    /// </summary>
    public uint Hour;
    /// <summary>
    /// Indicates whether hour is present and should be written.
    /// </summary>
    public bool HasHour;
    /// <summary>
    /// Stores the minute value used by this model.
    /// </summary>
    public uint Minute;
    /// <summary>
    /// Indicates whether minute is present and should be written.
    /// </summary>
    public bool HasMinute;
    /// <summary>
    /// Stores the second value used by this model.
    /// </summary>
    public uint Second;
    /// <summary>
    /// Indicates whether second is present and should be written.
    /// </summary>
    public bool HasSecond;
    /// <summary>
    /// Stores the date time grouping value used by this model.
    /// </summary>
    public string DateTimeGrouping;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new date group item with its default state.
    /// </summary>
    public XlsxDateGroupItem()
    {
        Year = 0;
        HasYear = false;
        Month = 0;
        HasMonth = false;
        Day = 0;
        HasDay = false;
        Hour = 0;
        HasHour = false;
        Minute = 0;
        HasMinute = false;
        Second = 0;
        HasSecond = false;
        DateTimeGrouping = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
