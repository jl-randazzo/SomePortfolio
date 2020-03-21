AUTHOR: Luke Randazzo

This is part of a larger image processing project. 

This is the component that interfaces directly with the QT framework. There isn't that much to explain here; signals and slots are used for
basic updating across the program.

The most significant customization takes place in the animationsessionbuilder files. This singleton inherits from the QAbstractListModel and 
interfaces with the OpenGL session that's used to draw the animation frames to the window. It was necessary for maintaining the session
data in a way that was visually pleasing to the user but also met my criteria for data accessibility within the project. 
