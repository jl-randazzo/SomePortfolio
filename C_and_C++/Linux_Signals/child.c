#include <errno.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>

int main()
{
	pid_t ppid = getppid();
	kill(ppid, SIGUSR1);
	kill(ppid, SIGUSR1);
	kill(ppid, SIGUSR1);
	kill(ppid, SIGUSR2);
	kill(ppid, SIGILL);
}
