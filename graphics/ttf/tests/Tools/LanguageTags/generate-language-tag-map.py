#!/usr/bin/env python3

import argparse
from html.parser import HTMLParser
from pathlib import Path
import re
import struct
import sys


MAGIC = b"TTFLNG\0\0"
VERSION = 1

ISO_639_1 = """
af=afr ak=aka am=amh ar=ara as=asm az=aze ba=bak be=bel bg=bul bm=bam
bn=ben bo=bod br=bre bs=bos ca=cat ce=che co=cos cs=ces cv=chv cy=cym
da=dan de=deu dv=div dz=dzo ee=ewe el=ell en=eng eo=epo es=spa et=est
eu=eus fa=fas ff=ful fi=fin fo=fao fr=fra fy=fry ga=gle gd=gla gl=glg
gn=grn gu=guj gv=glv ha=hau he=heb hi=hin hr=hrv ht=hat hu=hun hy=hye
ia=ina id=ind ie=ile ig=ibo ii=iii io=ido is=isl it=ita iu=iku ja=jpn
jv=jav ka=kat ki=kik kk=kaz kl=kal km=khm kn=kan ko=kor ks=kas ku=kur
kw=cor ky=kir lb=ltz lg=lug ln=lin lo=lao lt=lit lu=lub lv=lav mg=mlg
mi=mri mk=mkd ml=mal mn=mon mr=mar ms=msa mt=mlt my=mya nb=nob nd=nde
ne=nep nl=nld nn=nno no=nor nr=nbl nv=nav ny=nya oc=oci om=orm or=ori
os=oss pa=pan pl=pol ps=pus pt=por qu=que rm=roh rn=run ro=ron ru=rus
rw=kin sa=san sc=srd sd=snd se=sme sg=sag si=sin sk=slk sl=slv sm=smo
sn=sna so=som sq=sqi sr=srp ss=ssw st=sot su=sun sv=swe sw=swa ta=tam
te=tel tg=tgk th=tha ti=tir tk=tuk tn=tsn to=ton tr=tur ts=tso tt=tat
ug=uig uk=ukr ur=urd uz=uzb ve=ven vi=vie wa=wln wo=wol xh=xho yi=yid
yo=yor za=zha zh=zho zu=zul
"""

DEFAULT_OVERRIDES = {
    "ron": "ROM ",
    "zho": "ZHS ",
}


class LanguageTableParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.in_cell = False
        self.cell = ""
        self.cells = []
        self.rows = []

    def handle_starttag(self, tag, attrs):
        if tag == "td":
            self.in_cell = True
            self.cell = ""

    def handle_endtag(self, tag):
        if tag == "td":
            self.in_cell = False
            self.cells.append(" ".join(self.cell.split()))
        elif tag == "tr":
            if len(self.cells) >= 3:
                self.rows.append(self.cells)
            self.cells = []

    def handle_data(self, data):
        if self.in_cell:
            self.cell += data


def parse_arguments():
    root = Path(__file__).resolve().parents[3]
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--source",
        type=Path,
        default=root / "specs/opentype/languagetags.html")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--check", action="store_true")
    return parser.parse_args()


def parse_iso_codes(value):
    match = re.match(r"^([a-z]{3}(?:\s*,\s*[a-z]{3})*)\b", value)
    if match is not None:
        return re.findall(r"[a-z]{3}", match.group(1))
    attached = re.match(r"^([a-z]{3})(?=[A-Z])", value)
    return [] if attached is None else [attached.group(1)]


def choose_default(code, choices):
    override = DEFAULT_OVERRIDES.get(code)
    if override is not None:
        return override
    exact = code.upper()
    for tag in choices:
        if tag.rstrip() == exact:
            return tag
    return choices[0]


def build(source):
    parser = LanguageTableParser()
    parser.feed(source.read_text(encoding="utf-8"))
    choices = {}
    for row in parser.rows:
        tag_match = re.fullmatch(r"'(.{4})'\*?", row[1])
        if tag_match is None:
            continue
        tag = tag_match.group(1)
        if not all("A" <= character <= "Z" or character == " "
                   for character in tag):
            continue
        for code in parse_iso_codes(row[2]):
            values = choices.setdefault(code, [])
            if tag not in values:
                values.append(tag)
    mappings = {code: choose_default(code, values)
                for code, values in choices.items()}
    for item in ISO_639_1.split():
        short, long = item.split("=")
        if long in mappings:
            mappings[short] = mappings[long]
    ordered = sorted(mappings.items())
    output = bytearray(struct.pack(">8sHH", MAGIC, VERSION, len(ordered)))
    for key, tag in ordered:
        encoded = key.encode("ascii")
        output.append(len(encoded))
        output.extend(encoded)
        while len(encoded) < 3:
            output.append(0)
            encoded += b"\0"
        output.extend(tag.encode("ascii"))
    return bytes(output)


def main():
    arguments = parse_arguments()
    generated = build(arguments.source)
    if arguments.check:
        if not arguments.output.exists() or arguments.output.read_bytes() != generated:
            print("language tag map is not reproducible", file=sys.stderr)
            return 1
    else:
        arguments.output.parent.mkdir(parents=True, exist_ok=True)
        arguments.output.write_bytes(generated)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
