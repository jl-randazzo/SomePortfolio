AUTHOR: Luke Randazzo

These are some simple utilities I created to do some basic rooted rotations, among other things. 

VectorRange
This class takes in two 2-d Vectors and generates a rotation matrix that roots the first vector at <1, 0>. Then, when any vector is
passed to check if it's within the 'Range,' the vector is rotated using that matrix, and then some basic checks are performed to determine
if the vector falls within the range of the first two vectors. 
