namespace ftp;

/// <summary>
/// Identifies standard, security, historic, and compatibility FTP commands.
/// </summary>
public enum FtpCommandKind
{
    /// <summary>
    /// Represents an unknown or extension-owned command.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Identifies ABOR.
    /// </summary>
    Abor = 1,
    /// <summary>
    /// Identifies ACCT.
    /// </summary>
    Acct = 2,
    /// <summary>
    /// Identifies ALLO.
    /// </summary>
    Allo = 3,
    /// <summary>
    /// Identifies APPE.
    /// </summary>
    Appe = 4,
    /// <summary>
    /// Identifies CDUP.
    /// </summary>
    Cdup = 5,
    /// <summary>
    /// Identifies CWD.
    /// </summary>
    Cwd = 6,
    /// <summary>
    /// Identifies DELE.
    /// </summary>
    Dele = 7,
    /// <summary>
    /// Identifies HELP.
    /// </summary>
    Help = 8,
    /// <summary>
    /// Identifies LIST.
    /// </summary>
    List = 9,
    /// <summary>
    /// Identifies MKD.
    /// </summary>
    Mkd = 10,
    /// <summary>
    /// Identifies MODE.
    /// </summary>
    Mode = 11,
    /// <summary>
    /// Identifies NLST.
    /// </summary>
    Nlst = 12,
    /// <summary>
    /// Identifies NOOP.
    /// </summary>
    Noop = 13,
    /// <summary>
    /// Identifies PASS.
    /// </summary>
    Pass = 14,
    /// <summary>
    /// Identifies PASV.
    /// </summary>
    Pasv = 15,
    /// <summary>
    /// Identifies PORT.
    /// </summary>
    Port = 16,
    /// <summary>
    /// Identifies QUIT.
    /// </summary>
    Quit = 17,
    /// <summary>
    /// Identifies REIN.
    /// </summary>
    Rein = 18,
    /// <summary>
    /// Identifies REST.
    /// </summary>
    Rest = 19,
    /// <summary>
    /// Identifies RETR.
    /// </summary>
    Retr = 20,
    /// <summary>
    /// Identifies RMD.
    /// </summary>
    Rmd = 21,
    /// <summary>
    /// Identifies RNFR.
    /// </summary>
    Rnfr = 22,
    /// <summary>
    /// Identifies RNTO.
    /// </summary>
    Rnto = 23,
    /// <summary>
    /// Identifies SITE.
    /// </summary>
    Site = 24,
    /// <summary>
    /// Identifies SMNT.
    /// </summary>
    Smnt = 25,
    /// <summary>
    /// Identifies STAT.
    /// </summary>
    Stat = 26,
    /// <summary>
    /// Identifies STOR.
    /// </summary>
    Stor = 27,
    /// <summary>
    /// Identifies STOU.
    /// </summary>
    Stou = 28,
    /// <summary>
    /// Identifies STRU.
    /// </summary>
    Stru = 29,
    /// <summary>
    /// Identifies SYST.
    /// </summary>
    Syst = 30,
    /// <summary>
    /// Identifies TYPE.
    /// </summary>
    Type = 31,
    /// <summary>
    /// Identifies USER.
    /// </summary>
    User = 32,
    /// <summary>
    /// Identifies PWD.
    /// </summary>
    Pwd = 33,
    /// <summary>
    /// Identifies FEAT.
    /// </summary>
    Feat = 40,
    /// <summary>
    /// Identifies OPTS.
    /// </summary>
    Opts = 41,
    /// <summary>
    /// Identifies EPRT.
    /// </summary>
    Eprt = 50,
    /// <summary>
    /// Identifies EPSV.
    /// </summary>
    Epsv = 51,
    /// <summary>
    /// Identifies LANG.
    /// </summary>
    Lang = 60,
    /// <summary>
    /// Identifies MDTM.
    /// </summary>
    Mdtm = 70,
    /// <summary>
    /// Identifies MLST.
    /// </summary>
    Mlst = 71,
    /// <summary>
    /// Identifies MLSD.
    /// </summary>
    Mlsd = 72,
    /// <summary>
    /// Identifies SIZE.
    /// </summary>
    Size = 73,
    /// <summary>
    /// Identifies AUTH.
    /// </summary>
    Auth = 80,
    /// <summary>
    /// Identifies PBSZ.
    /// </summary>
    Pbsz = 81,
    /// <summary>
    /// Identifies PROT.
    /// </summary>
    Prot = 82,
    /// <summary>
    /// Identifies CCC.
    /// </summary>
    Ccc = 83,
    /// <summary>
    /// Identifies ADAT.
    /// </summary>
    Adat = 84,
    /// <summary>
    /// Identifies MIC.
    /// </summary>
    Mic = 85,
    /// <summary>
    /// Identifies CONF.
    /// </summary>
    Conf = 86,
    /// <summary>
    /// Identifies ENC.
    /// </summary>
    Enc = 87,
    /// <summary>
    /// Identifies HOST.
    /// </summary>
    Host = 90,
    /// <summary>
    /// Identifies ALGS.
    /// </summary>
    Algs = 91,
    /// <summary>
    /// Identifies the obsolete LPRT command.
    /// </summary>
    Lprt = 100,
    /// <summary>
    /// Identifies the obsolete LPSV command.
    /// </summary>
    Lpsv = 101,
    /// <summary>
    /// Identifies the historic XCUP alias.
    /// </summary>
    Xcup = 102,
    /// <summary>
    /// Identifies the historic XCWD alias.
    /// </summary>
    Xcwd = 103,
    /// <summary>
    /// Identifies the historic XMKD alias.
    /// </summary>
    Xmkd = 104,
    /// <summary>
    /// Identifies the historic XPWD alias.
    /// </summary>
    Xpwd = 105,
    /// <summary>
    /// Identifies the historic XRMD alias.
    /// </summary>
    Xrmd = 106,
    /// <summary>
    /// Identifies the PRET compatibility command.
    /// </summary>
    Pret = 120,
    /// <summary>
    /// Identifies the CLNT compatibility command.
    /// </summary>
    Clnt = 121,
    /// <summary>
    /// Identifies the MFMT compatibility command.
    /// </summary>
    Mfmt = 122,
    /// <summary>
    /// Identifies the HASH compatibility command.
    /// </summary>
    Hash = 123,
    /// <summary>
    /// Identifies the MD5 compatibility command.
    /// </summary>
    Md5 = 124,
    /// <summary>
    /// Identifies the XMD5 compatibility command.
    /// </summary>
    Xmd5 = 125,
    /// <summary>
    /// Identifies the XSHA1 compatibility command.
    /// </summary>
    Xsha1 = 126,
    /// <summary>
    /// Identifies the XSHA256 compatibility command.
    /// </summary>
    Xsha256 = 127,
    /// <summary>
    /// Identifies the XSHA512 compatibility command.
    /// </summary>
    Xsha512 = 128,
    /// <summary>
    /// Identifies the XCRC compatibility command.
    /// </summary>
    Xcrc = 129,
    /// <summary>
    /// Identifies the RMDA compatibility command.
    /// </summary>
    Rmda = 130
}
