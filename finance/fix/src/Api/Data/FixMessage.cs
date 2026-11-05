using System.Collections.Generic;

namespace fix;

/// <summary>Represents a mutable FIX message with flat fields and structured groups.</summary>
public class FixMessage
{
    /// <summary>The decoded BeginString value.</summary>
    public string BeginString = "";
    /// <summary>The decoded MsgType value.</summary>
    public string MsgType = "";
    /// <summary>The standard-header fields in wire order, including duplicates.</summary>
    public List<FixField> HeaderFields = [];
    /// <summary>The application-body fields in wire order, including duplicates.</summary>
    public List<FixField> BodyFields = [];
    /// <summary>The standard-trailer fields in wire order, including duplicates.</summary>
    public List<FixField> TrailerFields = [];
    /// <summary>The structured header groups in wire order.</summary>
    public List<FixGroup> HeaderGroups = [];
    /// <summary>The structured body groups in wire order.</summary>
    public List<FixGroup> BodyGroups = [];
    /// <summary>The structured trailer groups in wire order.</summary>
    public List<FixGroup> TrailerGroups = [];
    /// <summary>The complete original wire bytes when preservation was requested.</summary>
    public byte[] OriginalBytes = [];
}
