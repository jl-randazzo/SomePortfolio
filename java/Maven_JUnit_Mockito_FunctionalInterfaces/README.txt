-------------- Maven project 2 ---------------

I wrote 100% of the code housed in these folders and put together the pom.xml

Skills demonstrated:

JUnit 5 (Jupiter API): View test classes in directory src/test/java/JLR/

JUnit 5 TestFactory: View the specific class
src/test/java/JLR/CameraTest.java

Mockito (mocking, stubbing): View the specific class
src/test/java/JLR/TriTest.java

Functional interface (design and use): View the class
src/test/java/JLR/Assertions.java

Polymorphism: see The Bufferable interface, Triple, Color, Tri, and Camera in
src/main/java/JLR

Retrieving Resources from ClassLoader in Package:
See the main method in Calculate.java
src/main/java/JLR

Purpose: I was trying to kill three birds with one stone here. I knew for
future graphics projects I was going to need these abstractions (Triple and
Camera in particular, for some reason we're using java and the lwjgl library), 
but I also had homework for which I needed to convert the coordinates of
points of world-space into coordinates in screen-space. I created the main
method and the Calculate.java class to do that. Initially I could feed in any
file I wanted through the command line, but for demonstration purposes, I
simplified it to just use the Input.txt file. Finally, although I have a good
amount of experience unit testing and mocking in C#, I wanted to get some more
solid experience with the most recent JUnit libraries as well as Mockito. 

Note: FloatBuffers and some other esoteric objects are used because that's
what LWJGL uses to interface with the OpenGL API (understandably, given it's
designed to work in C).

