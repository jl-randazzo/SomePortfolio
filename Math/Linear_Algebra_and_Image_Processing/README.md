AUTHOR: Luke Randazzo

This code was extracted from a larger image-processing focused project. It's also the best demonstration available of my C++ skills. 

Of particular interest here is the LowerLevelLinearFunctions, both .h and .cpp. I implemented my own matrix template.
I considered using one of the other Matrix libraries available, but in the end, I thought it would be most useful to design something
that would play nicely with the other libraries I was already using. 

There is some design at play here: Abstract Vectors and LMat's (Luke Matrices) both extend the 'Reduceable' pure virtual class,
and then Reduceables can be augmented onto other Reduceables using the LAugMat class. The code is pretty involved and is designed
to even allow row and addition operators on matrices of pointers (a feature that believe it or not, I absolutely made use of in some of
my image processing). 

Although some of the code is rather complex, the ultimate goal was to create an interface that was really simple. If you look at 
linearfunctionsandlogicalimagetests.cpp, I believe that I did an effective job of creating a simple-to-use interface. 

There is definitely more I want to do with this. LU factorization, solving for EigenValues, and more, but I think this is a nice start.
