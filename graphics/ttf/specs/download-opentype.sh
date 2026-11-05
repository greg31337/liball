#!/bin/sh
set -eu

destination="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/opentype"
base="https://learn.microsoft.com/en-us/typography/opentype/spec"

mkdir -p "$destination"

for page in \
  overview otff recom changes chapter2 otvaroverview otvarcommonformats \
  featurelist features_ae features_fj features_ko features_pt features_uz \
  scripttags languagetags baselinetags \
  avar base cbdt cblc cff cff2 cmap colr cpal cvar cvt dsig ebdt eblc ebsc \
  fpgm fvar gasp gdef glyf gpos gsub gvar hdmx head hhea hmtx hvar jstf kern \
  loca ltsh math maxp merg meta mvar name os2 pclt post prep sbix stat svg \
  vdmx vhea vmtx vorg vvar
do
  curl -fL "$base/$page" -o "$destination/$page.html"
done
