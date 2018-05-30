----DMPCWMTHELPER.CS README----

This class was the main subject of a Scrum-style sprint I wrote alongside 7
teammates (in addition to the implementation of this class' member methods and
functions into the data model for the MVC-structured web application we were
working on). 

This class was designed to interface with our modified version of Google's
open source DiffMatchPatch algorithm. The purpose of DiffMatchPatch is clear:
it parses two long strings and then formats the differences into HTML-encoded
strings complete with removed text highlighted in red and added text
highlighted in green. 

Our customer was interested in displaying differences between two bodies 
of text for users of his site. They were given the option to edit text in a
TinyMCE textbox, complete with options for rendering bold text, different
fonts, and even adding emoji's. 

Google's algorithm was insufficient for this purpose because it didn't take
the peculiarities of HTML into account. That's where this class comes into
play: it's specifically designed to locate and process the location of HTML
tags so that relevant stylistic data is preserved in the displayed
differences. See DMPCWMTHelperResults.jpg for a reference image.

The structuring of this class was chiefly my responsibility, and although I
didn't write every line of code in here, I can explain what every line
does. There is some outstanding technical debt in terms of the organization of
the class.
