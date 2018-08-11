#include <stdio.h>
#include <signal.h>
#include <string.h>
#include <assert.h>
#include <unistd.h>
int child2parent;
int parent2child;
char message[1024];

/*
 * This program was written completely by me. It's designed to interface with the parent
 * program, passing messages back and forth
*/
void waiter()
{

	long sum = 1;
	for(long i = 499999999; i > 0; i--)
	{
		sum = sum + 1;
	}
}

int message_to_parent(int routine, char* words)
{
	message[0] = (char)routine;
	message[1] = 0;

	if(routine == 4)
	{
		int j = 1;
		for(char* i = words; *i != 0; i++)
		{
			message[j] = *i;
			message[++j] = 0;
		}
	}

	assert(write(child2parent, message, 1024) >= 0); 
	assert(kill(getppid(), SIGTRAP) == 0);
	
	if(routine != 4)
	{
		char buffer[4096];
		int readLength = read(parent2child, buffer, sizeof(buffer));
		assert(readLength >= 0);
		printf("%s\n", buffer);
	}
	return 1;
}

int main()
{

	child2parent = 4;
	parent2child = 3;

	char* words = "THIS IS THE MESSAGE SENT FROM THE CHILD\n\n";
	for(int i = 1; i < 5; i++)
	{
		assert(message_to_parent(i, words) == 1);
		waiter();
	}

	return 0;
}

