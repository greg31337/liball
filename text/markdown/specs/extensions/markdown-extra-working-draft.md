
Markdown Extra: Syntax
======================

<strong>This is a working draft.</strong> You can take part in this work, 
[join the Markdown discussion list][md-discuss].

Availability
:	Latest version of this spec is available at  
	<http://michelf.com/specs/markdown-extra/>
:	Markdown-Extra-formatted version available at  
	<http://michelf.com/specs/markdown-extra/index.text>

Version history
:   Available by tracking the git repository for this document at  
	<http://git.michelf.com/md-x-spec>

Editor
:   Michel Fortin, <michel.fortin@michelf.com>

<small>Copyright © 2008 Michel Fortin.  
You are free to share and make adaptations of this document under the terms 
of the [Creative Commons Attribution 2.5 Canada License][cc-license].</small>

 [md-discuss]: http://daringfireball.net/projects/markdown/#discussion-list
 [cc-license]: http://creativecommons.org/licenses/by/2.5/ca/

<!-- should add rel="license" to the license link -->

 - - -


Abstract						{#abstract}
--------

This specification defines how to read a Markdown Extra document, how to 
construct the document model, and how to translate it to HTML. This document 
aims at being a superset of the [Markdown Syntax Documentation][df-syntax] 
from Daring Fireball.

 [df-syntax]: http://daringfireball.net/projects/markdown/syntax


Status of this document			{#status-of-this-document}
-----------------------

**This is an early draft!** Implementors should be reminded that this 
documentation is not stable. If you with to implement this spec, you should 
join the [Markdown discussion list][md-discuss] to be aware of the latest 
directions and development.

This specification intends to become a reference in how to parse Markdown 
Extra documents. In the absence of more precise [Markdown Syntax 
Documentation][df-syntax], it is also the intension that this specification 
can be used as a reference for how to parse a plain Markdown document.

The first goal of this document is not to add new features, nor redefine
how Markdown or Markdown Extra documents should be parsed, but to specify the
syntax in a way which can improve interoperability between implementations
while breaking the smallest number of existing Markdown and Markdown Extra
documents.


Table of contents				{#table-of-contents}
-----------------

[To be added]


1. Introduction					{#introduction}
---------------

*This section is non-normative.*

[Markdown][df-markdown] is originally two things: a lightweight markup syntax 
[introduced in 2004][df-dive-into-md] by John Gruber for writing on the web, 
and a converter tool of the same name also written by John Gruber. Markdown 
Extra, is a syntax based on Markdown and extending it with new "extra" features 
such as tables and definition lists.

While Markdown and, to some extent, Markdown Extra became widely popular 
as a formatting tool for blog entries and other web documents, it became 
apparent that the syntax specification was inadequate to create fully 
interoperable implementations of Markdown. Using the original implementation 
as a reference could provide some instincts, but obvious bugs were preventing 
its output from being a trustworthy reference.

This is the syntax specification for Markdown Extra which aims at fully 
defining how to parse the initial text document to build the document model.
Since Markdown Extra is based on Markdown, it can also be used as a reference 
as to how to do the same for a Markdown document if desired; this specification
aims at making this easy.

 [df-dive-into-md]: http://daringfireball.net/2004/03/dive_into_markdown
 [df-markdown]: http://daringfireball.com/projects/markdown/
 
### 1.1 Scope							{#scope}

*This section is non-normative.*

This specification describes the Markdown Extra document model and how to parse
a stream of characters to create the document model.

This specification imposes no requirement about how the document model should 
be implemented programatically.

This specification also suggest how to serialize the document model to other
formats, such as HTML4. There is no a requirement about the given output, only
conventions which implementers are encouraged to follow.

<div class="editor note" markdown="1">
**Note:** should there be requirements about the output?
</div>

### 1.2 Structure of this specification	{#structure-of-this-specification}

This specification comprise three main sections:

[Document model](#document-model)
:	Describes the general structure of a Markdown document, with its various
	syntax elements and their properties.

[Parsing](#document-parsing)
:	Defines how a Markdown Extra parser should read a document and extract the 
	various syntax elements.

[Output](#output)
:	Markdown Extra documents are usually meant to be converted to another
	format. This non-normative section describe a typical serialization to 
	HTML/XHTML.

### 1.3 Conformance requirements

Parsers for the Markdown Extra syntax must parse documents as described in this
specification, or in a way that produce the same document model. Since this 
specification has no requirement as to how the document model is represented
inside a program, implementors are also free to completely bypass the model
and generate the output directly as long as the output accurately represents
the model of the given document.

Markdown Extra doesn't define any conformance requirements for documents. Any
character input can form a Markdown Extra document and be sent to the parser,
which should accept to parse it until the end, and result in a Markdown Extra 
document model.


2. Document model				{#document-model}
---------------------

The Markdown Extra document model is a [tree structure][] where the root is 
the document itself, and children of the root are various syntax elements. Most
syntax elements may contain other syntax element as their children. For 
instance a list usually has many list item children; a paragraph may have many
text nodes, code span nodes, link nodes, etc. interleaved.

Here is a sample Markdown document:

 [tree structure]: http://en.wikipedia.org/wiki/Tree_structure

	Some text and [a link][1]

	<hr class="section-separator">

	*   List item 1
	*   List item 2

	[1]: http://example.com "Example web site"

This document starts with a paragraph containing some text and a link with some 
more text in it, followed by an HTML block containing a single `hr` HTML 
element, followed by a list containing two items each having some text in them,
and ending with a link reference.

This document's tree could be illustrated like this:

<object type="image/svg+xml" data="figures/document-tree.svg" 
	width="595" height="280" style="display: block; margin: auto">
	<a href="figures/document-tree.svg">[SVG image]</a>
</object>


### 2.1 The document root		{#the-document-root}

<div class="properties" markdown="1">
Context in which this element may appear:
:	At the root of the document model

Content model:
:	Any number of document elements and block elements in any order

Special attributes:
:	None
</div>

Each Markdown Extra document has one and only one document root containing
the whole content of the document.


### 2.2 Document elements		{#document-elements}

#### 2.2.1 Link reference		{#link-reference}

<div class="properties" markdown="1">
Context in which this element may appear:
:	As a direct child of the document

Content model:
:	None

Special attributes:
:	Reference name
:	URI
:	Title (optional)
</div>

Link references do not appear in the final output, but allow reference links
and images span elements to be given attributes by referencing them from 
elsewhere in the document.

A link reference is alone on a line. It begins with the reference name inside 
square brackets, optionally followed by a space or a no-break-space, a colon, 
a URI (either enclosed in angle brakets or not), and an optional title 
enclosed in single or double quotes, or in parenthesis (which can be preceded 
by a newline).

The reference name is matched case-insensitively.

#### 2.2.2 Abbreviation definition (extra)	{#abbreviation-definition}

<div class="properties" markdown="1">
Context in which this element may appear:
:	As a direct child of the document

Content model:
:	One text node

Special attributes:
:	Abbreviated word
</div>

Abbreviation definitions denote words with are an abbreviated from of another
word or group of word. The "abbreviated word" will be matched (case sensitively)
against the text in each text node and, if found, enclosed in an abbreviation 
element.

An abbreviation definition starts with an asterisk, followed by the 
abbreviated word inside square brakets, optionally followed by one space or
no-break-space, a colon and one or more words giving the full meaning of the
abbreviation.

#### 2.2.3 Footnote definition (extra)	{#footnote-definition}

<div class="properties" markdown="1">
Context in which this element may appear:
:	As a direct child of the document

Content model:
:	One or more block elements

Special attributes:
:	Reference name
</div>

Footnote definitions provide the content of a footnote to be used when a
footnote maker is found with a matching reference name.

A footnote definition starts with a footnote reference enclosed in square 
brakets with a caret character (`^`) just after the opening braket, an 
optional space or no-break-space character, and one or more block-level 
elements each having their first line, and optionally other lines, indented 
by one tab-length.


### 2.3 Block elements			{#block-elements}

#### 2.3.1 Paragraph			{#paragraph}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One or more span elements

Special attributes:
:	None
</div>

A paragraph starts after a blank line and ends at the first blank line. Newline
characters in the paragraph are considered to be soft-wrapped, meaning that
they do not bear significance until they're actually [Hard line 
break](#hard-line-break) elements.

#### 2.3.2 Blockquote			{#blockquote}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One or more of block elements

Special attributes:
:	None
</div>

A blockquote represents a quotation of a section of text from another source.
Blockquotes are created by prefixing paragraphs, or other block elements, with
a right-pointing angle bracket (`>`). You can nest blockquotes by adding more 
than one level of right-pointing angle brackets.

Inside a blockquote, you can prefix every line with an angle bracket, or only
those lines starting a new block element. Contiguous block elements are 
considered to be inside the same blockquote if they both share the same 
number of starting bracket.

#### 2.3.3 Header				{#header}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One or more of span elements

Special attributes:
:	Level
:	Id (extra)
</div>

Headers come in two forms: 

[Description forthcoming]

#### 2.3.4 Code block			{#code-block}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One text element

Special attributes:
:	None
</div>

[Description forthcoming]

#### 2.3.5 List					{#list}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One or more list item

Special attributes:
:	None
</div>

[Description forthcoming]

#### 2.3.6 Horizontal rule		{#horizontal-rule}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	None

Special attributes:
:	None
</div>

[Description forthcoming]

#### 2.3.7 Table (extra)				{#table}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One table row containing header table cells followed by zero or more
	table rows containing regular table cells.

Special attributes:
:	None
</div>

[Description forthcoming]

#### 2.3.8 Definition list (extra)		{#definition-list}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever block elements are allowed

Content model:
:	One table row containing header table cells followed by zero or more
	table rows containing regular table cells.

Special attributes:
:	None
</div>

[Description forthcoming]


### 2.4 Span elements			{#span-elements}

#### 2.4.1 Text					{#text}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed

Content model:
:	None (A text element doesn't contain other elements, although it contains
	text as an attribute).

Special attributes:
:	Text value
</div>

[Description forthcoming]

#### 2.4.2 Emphasis					{#emphasis}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed and there is no emphasis element 
	as an ancestor.

Content model:
:	One or more span elements, but no emphasis element.

Special attributes:
:	None
</div>

[Description forthcoming]

#### 2.4.3 Strong emphasis				{#strong-emphasis}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed and there is no strong emphasis element 
	as an ancestor.

Content model:
:	One or more span elements, but no strong emphasis element.

Special attributes:
:	None
</div>

[Description forthcoming]

#### 2.4.4 Link							{#link}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed and there is no link element 
	as an ancestor.

Content model:
:	One or more span elements, but no strong emphasis element.

Special attributes:
:	URI
:	Title (optional)
</div>

[Description forthcoming]

#### 2.4.5 Image						{#image}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed.

Content model:
:	None.

Special attributes:
:	Alternative text
:	URI
:	Title (optional)
</div>

[Description forthcoming]

#### 2.4.6 Hard line break				{#hard-line-break}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed.

Content model:
:	None.

Special attributes:
:	None
</div>

Hard line breaks are represented in Markdown source by having two spaces 
preceding a new line character. This can be useful if you need to force a line
break somewhere, as when writing an address:

	Santa Claus  
	North Pole  
	Canada  
	H0H 0H0

#### 2.4.7 Character entity				{#character-entity}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed.

Content model:
:	None.

Special attributes:
:	Represented character
</div>

[Description forthcoming]

#### 2.4.8 Abbreviation (extra)			{#abbreviation}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed.

Content model:
:	One text element.

Special attributes:
:	Title
</div>

Abbreviation elements are found by scanning the content of text elements for
abbreviations defined in the document's [abbreviation definitions](#abbreviation-definitions).

#### 2.4.9 Footnote marker (extra)		{#footnote-marker}

<div class="properties" markdown="1">
Context in which this element may appear:
:	Wherever span elements are allowed.

Content model:
:	None.

Special attributes:
:	Footnote content
</div>

[Description forthcoming]


3. Parsing								{#parsing}
----------

This section explains how to build the document model from a character stream 
following the Markdown Extra syntax.

### 3.1 Common constructs				{#common-constructs}

The following are definitions for basic syntax concepts which are reused 
at many places in the parsing section.

<dfn id="space">space</dfn>

:	One of:

	*	U+0009 Tabulation
	*	U+0020 Space
	
	<div class="editor note" markdown="1">
	**Editor note:** Should we extend this to include other unicode spaces
	as well? Candidates include:
	
	*	U+2000 En Quad
	*	U+2001 Em Quad
	*	U+2002 En Space
	*	U+2003 Em Space
	*	U+2004 Three-Per-Em Space
	*	U+2005 Four-Per-Em Space
	*	U+2006 Six-Per-Em Space
	*	U+2007 Figure Space
	*	U+2007 Punctuation Space
	*	U+2007 Thin Space
	*	U+2007 Hair Space
	*	U+2007 Medium Mathematical Space
	*	U+3000 Ideographic Space
	</div>

<dfn id="non-space">non-space</dfn>

:	Any character not matched by [space](#space)

<dfn id="end-of-line">end-of-line</dfn>

:	First match of:

	1.	U+000D Cariage Return (CR) followed by U+000A Line Feed (LF)
	2.	U+000D Cariage Return (CR)
	3.	U+000A Line Feed (LF)
	4.	End of file
	
	The end-of-line construct is part of the line it ends and must not be 
	counted as matching the line following it.
	
	<div class="editor note" markdown="1">
	**Editor note:** Perhaps we should follow the lead of XML 1.1 and add
	the following to our list:
	
	*	U+000D Cariage Return (CR) followed by U+0085 Next Line (NEL)
	*	U+0085 Next Line (NEL)
	*	U+2028 Line Separator
	
	<http://www.w3.org/TR/2006/REC-xml11-20060816/#sec-line-ends>
	</div>

<dfn id="indent">indent</dfn>

:	Any of:

	*	One U+0009 Tabulation
	*	Four U+0020 Space
	
	<div class="editor note" markdown="1">
	**Editor note:** This should be updated when/if the [space](#space) 
	construct is updated to add more characters
	</div>

<dfn id="insignificant-indent">insignificant-indent</dfn>

:	Any of:

	*	One, two, or three U+0020 Space
	
	<div class="editor note" markdown="1">
	**Editor note:** This should be updated when/if the [space](#space) 
	construct is updated to add more characters
	</div>

<dfn id="textrun">textrun</dfn>

:	A run of one or more characters having at least one [non-space](#non-space)
	character, and excluding any [end-of-line](#end-of-line).

<dfn id="blankline">blankline</dfn>

:	A sequence of:

	1.	Zero or more [space](#space)
	2.	One [end-of-line](#end-of-line)

<dfn id="textline">textline</dfn>

:	A sequence of:

	1.	One [textrun](#textrun)
	2.	One [end-of-line](#end-of-line)

<dfn id="refname">refname</dfn>

:	A run of one or more characters, excluding any 
	[end-of-line](#end-of-line) and U+005D Closing Square Bracket.
	
	<div class="editor note" markdown="1">
	**Editor note:** Should we allow closing square brakets inside when 
	they're correctly balanced with opening ones?
	</div>

<dfn id="identifier">identifier</dfn>

:	A run of one or more characters, excluding any 
	[end-of-line](#end-of-line) and U+007D Right Curly Bracket.
	
	<div class="editor note" markdown="1">
	**Editor note:** Should we allow closing square brakets inside when 
	they're correctly balanced with opening ones?
	</div>

<dfn id="quoted-textrun">quoted-textrun</dfn>

:	[To be defined]

<dfn id="singlequoted-textrun">singlequoted-textrun</dfn>

:	[To be defined]

<dfn id="parenthesed-textrun">parenthesed-textrun</dfn>

:	[To be defined]

<dfn id="url">url</dfn>

:	First of:

	1.	A sequence of:
		1.	"<"
		2.	Zero or more characters, but no ">" and no [blankline](#blankline)
		3.	">"
	2.	One or more [non-space](#non-space) character
	
	The extracted IRI is created from item 1.2 or item 2, depending of which
	one could actually match. The extracted IRI is stripped from any
	[end-of-line](#end-of-line) inside it.
	
	<div class="editor note" markdown="1">
	**Editor note:** 1.2 should be revised to clarify how the no blankline
	requirement can be parsed.
	</div>

<dfn id="block-element">block-element</dfn>

:	Using the [block element generator](#block-element-generator), attempt to
	create one element.

<dfn id="first-block-element">first-block-element</dfn>

:	Using the [block element generator](#block-element-generator), attempt to
	create one element, but change the hard-block-context-line-prefix rule
	so that it matches the empty string when applied to the first line.
	
	<div class="editor note" markdown="1">
	**Editor note:** This may need some clarification.
	</div>

<dfn id="block-element-run">block-element-run</dfn>

:	A sequence of:

	1.	One [first-block-element](#block-element)
	2.	Zero or more [block-element](#block-element)

<dfn id="hard-block-context-line-prefix">hard-block-context-line-prefix</dfn>

:	Using the current <var>context-line-prefix</var> stack of the block element 
	generator, attempt to match each rule in the stack in sequence, starting 
	from the first-inserted rule to the last one. If one match fail, matching
	this rule fails; otherwise, it matches.
	
	If the stack is empty, always matches without consuming any character.
	
<dfn id="soft-block-context-line-prefix">soft-block-context-line-prefix</dfn>

:	Using the current <var>context-line-prefix</var> stack of the block element 
	generator, attempt to optionally match each rule in the stack in sequence, 
	starting from the first-inserted rule and stopping at first one that
	doesn't match.
	
	This rule never fail to match.


### 3.2 Parsing a document					{#parsing-a-document}

A Markdown Extra character stream is parsable using three generators, one for
each of the three element categories of the document model. Parsing a document
is done in three steps:

1.	Running the [document element generator](#document-element-generator)
	on the whole document.
2.	Running the [block element generator](#block-element-generator) on the
	document ignoring the lines used to create document elements.
3.	Running the [span element generator](#span-element-generator) on all
	elements with text content flagged as needing span-level processing.

<div class="editor note" markdown="1">
**Editor note:** I think the document element generator and the block element 
generator steps could be merged eventually.

The span element generator step could also be merged if we change parsing of 
certain span elements to not depend on previously-encountered document 
elements (link references, footnotes and abbreviation definitions). But doing
this without causing breaking changes is perhaps not doable.
</div>

#### 3.2.1 Document element generator		{#document-element-generator}

With this generator, the whole document is scanned for
[document elements](#document-elements). At the start of each line, the parser
checks if the line matches one of the three following constructs. If it does, 
it creates the corresponding element, and attempts to match again starting on 
the first line not part of the previous match.

Footnote definition (extra)

:	A sequence of:
	
	1.	"[^"
	2.	One [refname](#refname)
	3.	"]"
	4.	One optional [space](#space)
	5.	":"
	6.	Zero or more [space](#space)
	7.	One optional [newline](#newline)
	8.	One [block-element-run](#block-element-run) by pushing the
		following sequence to the <var>context-line-prefix</var> stack:
		1.	One [indent](#indent)
	
	Creates a [footnote definition](#footnote-definition) element with 
	item 2 filling the reference name attribute and elements generated by
	parsing item 8 becoming the content.

Abbreviation definition (extra)

:	A sequence of:

	1.	"*["
	2.	One [refname](#refname)
	3.	"]"
	4.	One optional [space](#space)
	5.	":"
	6.	Zero or more [space](#space)
	7.	One [textrun](#textrun)
	
	Creates an [abbrevition definition](#abbreviation-definition) element
	with item 2 filling the abbreviated word attribute and item 7 becoming
	the textual content.

Link reference

:	A sequence of:
	
	1.	"["
	2.	One [refname](#refname)
	3.	"]"
	4.	One optional [space](#space)
	5.	":"
	6.	Zero or more [space](#space)
	7.	One [url](#url)
	8.	Zero or more [space](#space)
	9.	The optional sequence:
		1.	One of:
			*	One [newline](#newline)
			*	One or more [space](#space)
		2.	One of:
			*	One [quoted-textrun](#quoted-textrun)
			*	One [singlequoted-textrun](#singlequoted-textrun)
			*	One [parenthesed-textrun](#parenthesed-textrun)
	
	Creates a [link reference](#link-reference) element with item 2 filling
	the reference name attribute, the extracted IRI from item 7 filling the 
	URL attribute, and item 9.2 filling the title attribute.
	
	<div class="editor note" markdown="1">
	**Editor note:** Need rephrasing to exclude optional angle brakets around
	url and quotes or parenthesis around the title from being part of the
	actual content of attributes on the link reference element.
	</div>

After the document element generator, the document source is given to the
[block element generator](#block-element-generator) after being stripped of 
all lines which were part of a match that produced a document element in
this generator.


#### 3.2.2 Block element generator				{#block-element-generator}

The block element generator is run on the whole document after the
[document element generator](#document-element-generator). It is also
invoked when block elements need to be parsed outside of the main context
(such as when processing footnotes).

After each newline, the parser checks if the line matches one of the
following constructs. If it does, it creates the corresponding element, and 
attempt to match again starting on the first line not part of the previous
match.

The block element generator possess a
<dfn id="context-line-prefix-stack">context-line-prefix-stack</dfn> containing 
a series of rules to be matched before stopping the generator and returning to
the previous context. When the generator is told to ignore the context line
prefix for the first line (see [first-block-element](#first-block-element),
it means that while parsing the first line, the 
<var>context-line-prefix-stack</var> should be considered empty.

The block element generator is used as a parsing rule in the grammar of 
the document element generator and the block element generator. The block
element generator matches if it one of the following rule matches and creates
an element.

Code block

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	One or more sequence:
		1.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
		2.	One [indent](#indent)
		3.	One [textline](#textline)
		4.	Zero or more sequence:
			1.	One [soft-block-context-line-prefix](#hard-block-context-line-prefix)
			2.	[blankline](#blankline)
		5.	Zero or more sequence:
			1.	One [soft-block-context-line-prefix](#hard-block-context-line-prefix)
			2.	One [indent](#indent)
			3.	One [textline](#textline)
			4.	Zero or more sequence:
				1.	One [soft-block-context-line-prefix](#hard-block-context-line-prefix)
				2.	[blankline](#blankline)
	
	<div class="note" markdown="1">
	**Note:** a code block does not need to end with a blank line: any 
	non-blank line not stating with the proper indent ends the code block.
	</div>
	
	<div class="editor note" markdown="1">
	**Editor note:** Should we really allow soft block content line prefix
	here? Applying the lazy syntax of a parent container's on an indented code
	block seems rather silly and confusing. Here's an illustration:
	~~~
	>     Code block, Line 1
		Same code block, Line 2
	~~~
	</div>
	
	Creates a [code block](#code-block) element with the concatenation of all
	text lines in items 2.3, 2.4.3, and 2.4.4.2 as the content.

Fenced code block (extra)

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	3.	Three or more "~"
	4.	Zero or more [space](#space)
	5.	One [end-of-line](#end-of-line)
	6.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	7.	Zero or more of the following sequence, stopping at the first line
		capable of satisfying the remaining parts of the enclosing sequence:
		1.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
		2.	One [textline](#textline)
	8.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	9.	Same number of "~" as found in item 2
	10.	Zero or more [space](#space)
	11.	One [end-of-line](#end-of-line)
	
	Creates a [code block](#code-block) element with the concatenation of all
	text lines in item 5 as the content.

Blockquote

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	Zero or one [insignificant-indent](#insignificant-indent)
	3.	">"
	4.	Zero or one [space](#space)
	5.	One [block-element-run](#block-element-run) by pushing the following 
		sequence to the <var>context-line-prefix</var> stack:
		1.	Zero or one [insignificant-indent](#insignificant-indent)
		2.	">"
		3.	Zero or one [space](#space)
	
	Creates a [blockquote](#blockquote) element with elements generated by
	parsing item 5 becoming the content.

Horizontal Rule

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	3.	One of "_", "-", "*".
	4.	Two or more sequences of:
		1.	Zero, one, or two [space](#space)
		2.	One character identical the one found in item 3 above.
	5.	Zero or more [space](#space)
	6.	One [end-of-line)(#end-of-line)
	
	Creates a [horizontal rule](#horizontal-rule) element.

Header, Setext-style

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	3.	One [textrun](#textrun)
	4.	(Extra) Zero or one:
		1.	One "{#"
		2.	One or more [identifier](#identifier)
		3.	One "}"
		4.	Zero or more [space](#space)
	5.	One [end-of-line](#end-of-line)
	6.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	7.	One of:
		1.	One or more "="
		2.	One or more "-"
	8.	Zero or more [space](#space)
	9.	One [end-of-line](#end-of-line)
	
	Creates a [header](#header) element where the content is set to the result
	of applying the span element generator on item 3, the header level 
	attribute is set to one if item 7.1 was matched or two if item 7.2 was 
	matched, and the id attribute (extra) is set to item 4.1.

Header, Atx-style

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	3.	One or more "#"
	4.	One [textrun](#textrun)
	5.	Zero or more "#"
	6.	(Extra) Zero or one:
		1.	One "{#"
		2.	One or more [identifier](#identifier)
		3.	One "}"
		4.	Zero or more [space](#space)
	7.	One [end-of-line](#end-of-line)
	
	Creates a [header](#header) element where the content is set to the result
	of applying the span element generator on item 4, the header level 
	attribute is set to the number of characters in item 2, and the id 
	attribute (extra) is set to item 6.1.

List

:	[To be defined]

Definition List (extra)

:	[To be defined]

Table (extra)

:	[To be defined]

Paragraph

:	A sequence of:

	1.	Zero or more [blankline](#blankline)
	2.	One [hard-block-context-line-prefix](#hard-block-context-line-prefix)
	3.	One [textline](#textline)
	4.	Zero or more sequences of:
		1.	One [soft-block-context-line-prefix](#soft-block-context-line-prefix)
		2.	One [textline](#textline)
	3.	One [blankline]
	
	<div class="editor note" markdown="1">
	**Editor note:** Need a way to stop a paragraph when seeing certain
	constructs which should start other block-level elements without having
	a blank line, such as lists (when inside another list item), blockquotes,
	and fenced code blocks.
	</div>
	
	Creates a [paragraph][#paragraph] element where the content is obtained by
	running the span element generator on the concatenation of all text lines
	from item 2.


#### 3.2.3 Span element generator				{#span-element-generator}

[To be defined]


4. Output								{#output}
--------------

### 4.1 HTML Serialization				{#html-serialization}

[To be added]


<!--
Acknowledgements
----------------

Thanks for Allan Odgaard for explaining how a context-dependent stack of
line prefixes can help to parse Markdown blocks in one shot.

And thanks for John Gruber for creating the original Markdown, with the help
of Aaron Swartz and the comments of everybody on the Markdown discussion list.
-->

*[IRI]: Internationalized Resource Identifier