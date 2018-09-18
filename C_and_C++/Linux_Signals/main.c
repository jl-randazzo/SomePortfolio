#include <assert.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>

void HandleSig(int a);

_Bool sigusr1received = 0;
_Bool sigusr2received = 0;
_Bool sigillreceived = 0;

int main()
{
	struct sigaction sa;
	sa.sa_handler = HandleSig;
	sa.sa_flags = SA_RESTART;
	sigemptyset (&sa.sa_mask); 
	assert( sigaction(SIGUSR1, &sa, NULL) == 0 );
	assert( sigaction(SIGUSR2, &sa, NULL) == 0 );
	assert( sigaction(SIGILL, &sa, NULL) == 0 );
	
	int pid = fork();
	assert( pid != -1);
	if(pid == 0)
	{
		assert( execl("./child", "child", NULL) == 0 );
	}
	
	int wstatus;
	int done;
	
	do
	{
		done = waitpid( pid, &wstatus, WNOHANG);
		assert( done != -1 );
		if (sigusr1received == 1)
		{
			assert( printf("SIGUSR1 handled\n") >= 0);
			sigusr1received = 0;
		}
		if (sigusr2received == 1)
		{
			assert( printf("SIGUSR2 handled\n") >= 0);
			sigusr2received = 0;
		}
		if (sigillreceived == 1)
		{
			assert( printf("SIGILL handled\n") >= 0);
			sigillreceived = 0;
		}
	}
	while (done == 0);
	exit(0);	
}

void HandleSig(int a)
{
	switch (a)
	{
		case SIGUSR1:
			sigusr1received = 1;
			break;
		case SIGUSR2:
			sigusr2received = 1;
			break;
		case SIGILL:
			sigillreceived = 1;
			break;
		default:
			break;
	}
}
