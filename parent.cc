#include <string.h>
#include <iostream>
#include <stdio.h>
#include <list>
#include <iterator>
#include <unistd.h>
#include <signal.h>
#include <poll.h>
#include <errno.h>
#include <stdio.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <assert.h>
#include <fcntl.h>
#include <time.h>

/*

I did not write all of this program; it was written in an academic context. I have written
 notes throughout to be completely transparent about what was written COMPLETELY by me and 
what was partially or completely written by my professor, Dr. Stephen Beaty. I did write a 
lot of it, and I was responsible for reading the Linux programmers manual and figuring out 
how to accomplish kernel-related tasks. I know academic pseudo-problems don't always read 
well, but hopefully this demonstrates that I both know how to work with, read, and change 
established code and know how to follow hardware/OS documentation to solve a variety 
of problems. 

This program does the following.
//Dr. Beaty:1) Create handlers for two signals.
//Dr. Beaty:2) Create an idle process which will be executed when there is nothing
   else to do.
//Dr. Beaty:3) Create a send_signals process that sends a SIGALRM every so often.
4) Simulates CPU scheduling, including an interrupt-service routine, an interrupt service vector, and handler assignments for the various Linux signals designated in signal.h
5) Runs a series of test routines retrieved from children that send Sigtraps and data through pipes opened in the scheduling. Pathways to the file descriptors are stored in the PCB's

---------------------------------------------------------------------------
*/

//THE FOLLOWING MACROS WERE WRITTEN BY DR. STEPHEN BEATY
#define NUM_SECONDS 20
#define EVER ;;

#define assertsyscall(x, y) if(!((x) y)){int err = errno; \
	fprintf(stderr, "In file %s at line %d: ", __FILE__, __LINE__); \
		perror(#x); exit(err);}

#ifdef EBUG
#   define dmess(a) cout << "in " << __FILE__ << \
	" at " << __LINE__ << " " << a << endl;

#   define dprint(a) cout << "in " << __FILE__ << \
	" at " << __LINE__ << " " << (#a) << " = " << a << endl;

#   define dprintt(a,b) cout << "in " << __FILE__ << \
	" at " << __LINE__ << " " << a << " " << (#b) << " = " \
	<< b << endl
#else
#   define dmess(a)
#   define dprint(a)
#   define dprintt(a,b)
#endif

//MY MACROS
#define READ 0
#define WRITE 1

using namespace std;

// http://man7.org/linux/man-pages/man7/signal-safety.7.html

#define WRITES(a) { const char *foo = a; write(1, foo, strlen(foo)); }
#define WRITEI(a) { char buf[10]; assert(eye2eh(a, buf, 10, 10) != -1); WRITES(buf); }

//ENUMERATION INITIALLY WRITTEN BY DR. STEPHEN BEATY
enum STATE { NEW, RUNNING, WAITING, READY, TERMINATED };

//PCB STRUCTURE INITIALLY AUTHORED BY DR. STEPHEN BEATY
//JOSIAH L RANDAZZO ADDED PPES AS A LOCATION TO STORE THE FILE DESCRIPTORS FOR EACH
//OF THE PIPES USED TO COMMUNICATE WITH CHILDREN
struct PCB
{
	STATE state;
	const char *name;   // name of the executable
	int pid;			// process id from fork();
	int ppid;		   // parent process id
	int interrupts;	 // number of times interrupted
	int switches;	   // may be < interrupts
	int started;		// the time this process started
	int ppes[2];
};

PCB *running;
PCB *idle;
PCB *previous;

// http://www.cplusplus.com/reference/list/list/
list<PCB *> new_list;
list<PCB *> processes;

int sys_time;

//ASYNC-SAFE CONSOLE WRITE IMPLEMENTATION AUTHORED BY DR. STEPHEN BEATY
/*
** Async-safe integer to a string. i is assumed to be positive. The number
** of characters converted is returned; -1 will be returned if bufsize is
** less than one or if the string isn't long enough to hold the entire
** number. Numbers are right justified. The base must be between 2 and 16;
** otherwise the string is filled with spaces and -1 is returned.
*/
int eye2eh(int i, char *buf, int bufsize, int base)
{
	if(bufsize < 1) return(-1);
	buf[bufsize-1] = '\0';
	if(bufsize == 1) return(0);
	if(base < 2 || base > 16)
	{
		for(int j = bufsize-2; j >= 0; j--)
		{
			buf[j] = ' ';
		}
		return(-1);
	}

	int count = 0;
	const char *digits = "0123456789ABCDEF";
	for(int j = bufsize-2; j >= 0; j--)
	{
		if(i == 0)
		{
			buf[j] = ' ';
		}
		else
		{
			buf[j] = digits[i%base];
			i = i/base;
			count++;
		}
	}
	if(i != 0) return(-1);
	return(count);
}

//GRAB AND ISV WRITTEN BY BEATY
/*
** a signal handler for those signals delivered to this process, but
** not already handled.
*/
void grab(int signum) { WRITEI(signum); WRITES("\n"); }

// c++decl> declare ISV as array 32 of pointer to function(int) returning void
void(*ISV[32])(int) = {
/*		00	01	02	03	04	05	06	07	08	09 */
/*  0 */ grab, grab, grab, grab, grab, grab, grab, grab, grab, grab,
/* 10 */ grab, grab, grab, grab, grab, grab, grab, grab, grab, grab,
/* 20 */ grab, grab, grab, grab, grab, grab, grab, grab, grab, grab,
/* 30 */ grab, grab
};

/*
** stop the running process and index into the ISV to call the ISR
*/
//ISR MOSTLY WRITTEN BY DR. BEATY
void ISR(int signum)
{
	if(signum != SIGCHLD)
	{
		if(kill(running->pid, SIGSTOP) == -1)
		{
			WRITES("In ISR kill returned: ");
			WRITEI(errno);
			WRITES("\n");
			return;
		}

		WRITES("In ISR stopped: ");
		WRITEI(running->pid);
		WRITES("\n");
		running->state = READY;
	}

	ISV[signum](signum);
}

//OP OVERLOAD FOR OSTREAM WRITTEN BY DR. BEATY
/*
** an overloaded output operator that prints a PCB
*/
ostream& operator <<(ostream &os, struct PCB *pcb)
{
	os << "state:		" << pcb->state << endl;
	os << "name:		 " << pcb->name << endl;
	os << "pid:		  " << pcb->pid << endl;
	os << "ppid:		 " << pcb->ppid << endl;
	os << "interrupts:   " << pcb->interrupts << endl;
	os << "switches:	 " << pcb->switches << endl;
	os << "started:	  " << pcb->started << endl;
	os << "read fd:   " << pcb->ppes[READ] << endl;
	os << "write fd:  " << pcb->ppes[WRITE] << endl;
	return(os);
}

//WRITTEN BY JOSIAH L RANDAZZO
char * StateString(STATE s)
{
	switch(s)
	{
		case NEW:
			return "NEW";
		case RUNNING:
			return "RUNNING";
		case READY:
			return "READY";
		case WAITING:
			return "WAITING";
		case TERMINATED:
			return "TERMINATED";
	}
	return "";
}

//WRITTEN BY JOSIAH L RANDAZZO
/*
** brute force method for converting PCB to a string
*/
char * PCBString(struct PCB *pcb)
{
	char *buffer= new(char[1024]); 
	assert(sprintf(buffer,
	"state:             %s \n"
	"name:              %s \n"
	"pid:               %u \n"
	"ppid:              %u \n"
	"interrupts:        %u \n"
	"switches:          %u \n"
	"started:           %u \n",
	StateString(pcb->state), pcb->name, pcb->pid, pcb->ppid,
	 pcb->interrupts, pcb->switches, pcb->started) > 0);
	return buffer;
}

//WRITTEN BY DR. BEATY
/*
** an overloaded output operator that prints a list of PCBs
*/
ostream& operator <<(ostream &os, list<PCB *> which)
{
	list<PCB *>::iterator PCB_iter;
	for(PCB_iter = which.begin(); PCB_iter != which.end(); PCB_iter++)
	{
		os <<(*PCB_iter);
	}
	return(os);
}

//WRITTEN BY DR. BEATY
/*
**  send signal to process pid every interval for number of times.
*/
void send_signals(int signal, int pid, int interval, int number)
{
	dprintt("at beginning of send_signals", getpid());

	for(int i = 1; i <= number; i++)
	{
		assertsyscall(sleep(interval), == 0);
		dprintt("sending", signal);
		dprintt("to", pid);
		assertsyscall(kill(pid, signal), == 0)
	}

	dmess("at end of send_signals");
}

//WRITTEN BY DR. BEATY
struct sigaction *create_handler(int signum, void(*handler)(int))
{
	struct sigaction *action = new(struct sigaction);

	action->sa_handler = handler;

/*
**  SA_NOCLDSTOP
**  If  signum  is  SIGCHLD, do not receive notification when
**  child processes stop(i.e., when child processes  receive
**  one of SIGSTOP, SIGTSTP, SIGTTIN or SIGTTOU).
*/
	if(signum == SIGCHLD)
	{
		action->sa_flags = SA_NOCLDSTOP | SA_RESTART;
	}
	else
	{
		action->sa_flags =  SA_RESTART;
	}

	sigemptyset(&(action->sa_mask));
	assert(sigaction(signum, action, NULL) == 0);
	return(action);
}

//EXCEPT ON LINES WHERE OTHERWISE SPECIFIED, THIS CODE WAS WRITTEN BY JOSIAH L RANDAZZO
void scheduler(int signum)
{
	WRITES("---- entering scheduler\n");
	assert(signum == SIGALRM);
	sys_time++;
	bool found = false;
	previous = running;
	
	if(running->state != TERMINATED) 
	{
		running->interrupts++;
	}

	for(int i = 1; i <= processes.size(); i++)
	{
		running = processes.front();
		processes.pop_front();
		processes.push_back(running);
		if(running->state == NEW)
		{
			int child2parent[2];
			int parent2child[2];

			/* 
			*  The fcntl calls are used to query the currently set flags (F_GETFL)
			*  and then set the flags with the defaults (fl) bitwise-or'd with the
			*  non-blocking flag (O_NONBLOCK). 			
			*/

			assert(pipe(child2parent) == 0);
			assert(pipe(parent2child) == 0);
			int fl;
			fl = fcntl(child2parent[READ], F_GETFL);//this line from DR. BEATY
			assert(fl != -1);
			assert(fcntl(child2parent[READ], F_SETFL, fl | O_NONBLOCK) == 0);//this line written by Dr. Beaty
			int size;

			int child = fork();
			assert(child >= 0);
			if(child == 0)
			{
				assert(dup2(parent2child[READ], 3) == 3);
				assert(dup2(child2parent[WRITE], 4) == 4);
				assert(execl(running->name, running->name, nullptr) == 0);
			}

			assert(close(child2parent[WRITE]) == 0);
			assert(close(parent2child[READ]) == 0);
			running->ppes[READ] = child2parent[READ];
			running->ppes[WRITE] = parent2child[WRITE];
			running->pid = child;
			running->state = RUNNING;
			running->started = sys_time;

			found = true;
			WRITES("creating ");
			WRITEI(running->pid);
			WRITES("\n");
		}
		else if(running->state == READY)
		{
			found = true;	
			WRITES("continuing");
			WRITEI(running->pid);
			WRITES("\n");
			if(previous != running) running->switches++;
		}

		if(found) break;
	}

	if(!found) running = idle;

	running->state = RUNNING;
	if(kill(running->pid, SIGCONT) == -1)
	{
		WRITES("in sceduler kill error: ");
		WRITEI(errno);
		WRITES("\n");
		return;
	}
	WRITES("---- leaving scheduler\n");
}

//LARGELY WRITTEN BY DR. BEATY
void process_done(int signum)
{
	assert(signum == SIGCHLD);
	WRITES("---- entering process_done\n");
	cout << endl;
	
	// might have multiple children done.
	for(EVER)
	{
		int status, cpid;

		// we know we received a SIGCHLD so don't wait.
		cpid = waitpid(-1, &status, WNOHANG);

		if(cpid < 0)
		{
			WRITES("cpid < 0\n");
			assertsyscall(kill(0, SIGTERM), != 0);
		}
		else if(cpid == 0)
		{
			// no more children.
			break;
		}
		else
		{
			WRITES("process exited: ");
			WRITEI(cpid);
			WRITES("\n");
			cout << running;
			cout << "Process completed in " <<  sys_time - running->started << " seconds." << endl;
			running->state = TERMINATED;
			running = idle;
		}
	}

	cout << endl;
	WRITES("---- leaving process_done\n");
}

//WRITTEN BY JOSIAH L RANDAZZO
void message_routines(char * buffer, PCB* sender)
{
	char * message;	

	if(buffer[0]==1)
	{ 
		WRITES("\ncase 1: returning system time to sender\n");
		time_t t = time(nullptr);
		char * currenttime = ctime(&t);
		assert(write(sender->ppes[WRITE], currenttime, 4096) >= 0);
	}
	else if(buffer[0] == 2)
	{
		WRITES("\ncase 2: returning process data to sender\n");
		message = PCBString(sender);
		assert(write(sender->ppes[WRITE], message, 4096) >= 0);
		delete[](message);
	}
	else if(buffer[0] == 3)
	{
		WRITES("\ncase 3: returning string listing names of processes\n");
		char processList[1024];
		processList[0] = 0;
		for(PCB* j : processes)
		{
			strncat(processList, j->name, sizeof(processList) - strlen(processList) - 1);
			strncat(processList, ", ", sizeof(processList) - strlen(processList) - 1);
		}
		strncat(processList, "\n", sizeof(processList) - strlen(processList) - 1);
		assert(write(sender->ppes[WRITE], processList, 4096) >= 0);
	}
	else if(buffer[0] == 4)
	{
		WRITES("\ncase4: printing data transferred from sender\n");
		message = &buffer[1];
		WRITES(message);
	}
	else
	{
		perror("failure in message_receiving");
		assert(false);
	}
	
	assert(kill(sender->pid, SIGCONT) == 0);
}

//WRITTEN BY JOSIAH L RANDAZZO
/*
** finds children with data ready to read and passes their output to message_routines
*/
void message_receiving(int signum)
{
	WRITES("received signal: entering message_receiving\n");
	char buffer[1024];
	int readLength = 0;

	for(PCB* i : processes)
	{
		pollfd n;
		n.fd = i->ppes[READ];
		n.events = POLLIN;
		int pollResults = poll(&n, 1, 0);
		assert(pollResults >= 0);
		if((n.revents & POLLIN) == POLLIN)
		{
			readLength = read(i->ppes[READ], buffer, 1024);
			assert(readLength > 0);	
			buffer[readLength] = 0;
			message_routines(buffer, i);
		}
	}
}

//WRITTEN HALF BY DR. BEATY AND IMPROVED BY ME FOR MY PURPOSES
/*
** set up the "hardware"
*/
void boot()
{
	sys_time = 0;
	ISV[SIGALRM] = scheduler;
	ISV[SIGCHLD] = process_done;
	ISV[SIGTRAP] = message_receiving;
	struct sigaction *alarm = create_handler(SIGALRM, ISR);
	struct sigaction *child = create_handler(SIGCHLD, ISR);
	struct sigaction *trap = create_handler(SIGTRAP, ISR);
	// start up clock interrupt
	int ret;
	if((ret = fork()) == 0)
	{
		send_signals(SIGALRM, getppid(), 1, NUM_SECONDS);

		// once that's done, cleanup and really kill everything...
		delete(alarm);
		delete(child);
		delete(idle);
		delete(trap);
		sleep(1);
		kill(0, SIGTERM);
	}

	if(ret < 0)
	{
		perror("fork");
	}

	//According to Valgrind, these were sticking arond in the parent process, must be the copy-on-write duplicating them when the child moved to delete. After deleting here, no more leaks were found in valgrind
	delete(alarm);
	delete(child);
	delete(trap);
}

//WRITTEN BY DR. BEATY
void create_idle()
{
	idle = new(PCB);
	idle->state = READY;
	idle->name = "IDLE";
	idle->ppid = getpid();
	idle->interrupts = 0;
	idle->switches = 0;
	idle->started = sys_time;

	if((idle->pid = fork()) == 0)
	{
		pause();
		perror("pause in create_idle");
	}
}

//WRITTEN BY JOSIAH L RANDAZZO EXCEPT WHERE OTHERWISE SPECIFIED
int main(int argc, char **argv)
{
	//initialize PCBs	
	PCB* currentBlock;

	for(int i = 1; i < argc; i++)
	{
		currentBlock = new(PCB);
		currentBlock->state = NEW;
		currentBlock->name = argv[i];
		currentBlock->ppid = getpid();
		currentBlock->interrupts = 0;
		currentBlock->switches = 0;
		processes.push_back(currentBlock);
	}
	
	boot();
	create_idle();
	running = idle;
	previous = idle;
	cout << running;

	//FROM HERE ON WRITTEN BY DR. BEATY
	// we keep this process around so that the children don't die and
	// to keep the IRQs in place.
	for(EVER)
	{
		// "Upon termination of a signal handler started during a
		// pause(), the pause() call will return."
		pause();
	}
}
