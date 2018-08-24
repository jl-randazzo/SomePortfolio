------ child.c and main.c README ------

These are very simple classes written to demonstrate my understanding of a
variety of Linux systems calls (fork, kill, waitpid, etc). The implementation
of booleans for the signal handlers and the performing functionality in a spin
lock is designed so that the non-asyn-safe properties of printf don't create
any problems. Obviously there are many different ways of doing this, but I
wanted to demonstrate that I understand different ways of dealing with signals
and process asynchronus calls. 

I've done a good bit more in c and C++. This is a quick intro. 
