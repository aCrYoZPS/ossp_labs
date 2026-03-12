#include <fcntl.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <time.h>
#include <unistd.h>

static char conf_path[1024];
static char log_path[1024];
static char pid_path[1024];

static volatile sig_atomic_t should_exit = 0;
static volatile sig_atomic_t reload_config = 0;
static int monitored_signals[NSIG];

typedef struct {
  const char *name;
  int signum;
} SignalMap;

static SignalMap signal_map[] = {{"SIGHUP", SIGHUP},
                                 {"SIGINT", SIGINT},
                                 {"SIGQUIT", SIGQUIT},
                                 {"SIGILL", SIGILL},
                                 {"SIGTRAP", SIGTRAP},
                                 {"SIGABRT", SIGABRT},
                                 {"SIGBUS", SIGBUS},
                                 {"SIGFPE", SIGFPE},
                                 {"SIGUSR1", SIGUSR1},
                                 {"SIGSEGV", SIGSEGV},
                                 {"SIGUSR2", SIGUSR2},
                                 {"SIGPIPE", SIGPIPE},
                                 {"SIGALRM", SIGALRM},
                                 {"SIGTERM", SIGTERM},
                                 {"SIGCHLD", SIGCHLD},
                                 {"SIGCONT", SIGCONT},
                                 {"SIGTSTP", SIGTSTP},
                                 {"SIGTTIN", SIGTTIN},
                                 {"SIGTTOU", SIGTTOU},
                                 {"SIGURG", SIGURG},
                                 {"SIGXCPU", SIGXCPU},
                                 {"SIGXFSZ", SIGXFSZ},
                                 {"SIGVTALRM", SIGVTALRM},
                                 {"SIGPROF", SIGPROF},
                                 {"SIGWINCH", SIGWINCH},
                                 {"SIGIO", SIGIO},
                                 {"SIGPWR", SIGPWR},
                                 {"SIGSYS", SIGSYS},
                                 {NULL, 0}};

int get_signal_by_name(const char *name) {
  for (int i = 0; signal_map[i].name != NULL; i++) {
    if (strcmp(name, signal_map[i].name) == 0) {
      return signal_map[i].signum;
    }
  }
  return -1;
}

const char *get_signal_name(int signum) {
  for (int i = 0; signal_map[i].name != NULL; i++) {
    if (signal_map[i].signum == signum) {
      return signal_map[i].name;
    }
  }
  return "UNKNOWN";
}

void log_message(const char *message) {
  FILE *f = fopen(log_path, "a");
  if (f) {
    time_t now = time(NULL);
    char *timestamp = ctime(&now);
    timestamp[strlen(timestamp) - 1] = '\0';
    fprintf(f, "[%s] %s\n", timestamp, message);
    fclose(f);
  }
}

void load_config() {
  memset(monitored_signals, 0, sizeof(monitored_signals));
  FILE *f = fopen(conf_path, "r");
  if (!f) {
    log_message("Error: Could not open config file.");
    return;
  }

  char line[64];
  while (fgets(line, sizeof(line), f)) {
    line[strcspn(line, "\r\n")] = 0;
    int sig = get_signal_by_name(line);
    if (sig > 0 && sig < NSIG) {
      monitored_signals[sig] = 1;
    }
  }
  fclose(f);
  log_message("Configuration loaded.");
}

void signal_handler(int signum) {
  if (signum == SIGHUP) {
    reload_config = 1;
  } else if (signum == SIGTERM) {
    should_exit = 1;
  }

  if (signum > 0 && signum < NSIG && monitored_signals[signum]) {
    char msg[128];
    snprintf(msg, sizeof(msg), "Received monitored signal: %s (%d)",
             get_signal_name(signum), signum);
    log_message(msg);
  }
}

void daemonize() {
  pid_t pid = fork();
  if (pid < 0)
    exit(EXIT_FAILURE);
  if (pid > 0)
    exit(EXIT_SUCCESS);

  if (setsid() < 0)
    exit(EXIT_FAILURE);

  signal(SIGCHLD, SIG_IGN);
  signal(SIGHUP, SIG_IGN);

  pid = fork();
  if (pid < 0)
    exit(EXIT_FAILURE);
  if (pid > 0)
    exit(EXIT_SUCCESS);

  umask(0);
  chdir("/");

  for (int x = sysconf(_SC_OPEN_MAX); x >= 0; x--) {
    close(x);
  }

  open("/dev/null", O_RDWR);
  dup(0);
  dup(0);
}

int main(int argc, char *argv[]) {
  char cwd[1024];
  if (getcwd(cwd, sizeof(cwd)) == NULL) {
    perror("getcwd");
    return EXIT_FAILURE;
  }

  snprintf(conf_path, sizeof(conf_path), "%s/daemon.conf", cwd);
  snprintf(log_path, sizeof(log_path), "%s/daemon.log", cwd);
  snprintf(pid_path, sizeof(pid_path), "%s/daemon.pid", cwd);

  if (argc > 1 &&
      (strcmp(argv[1], "-q") == 0 || strcmp(argv[1], "--quit") == 0)) {
    FILE *f = fopen(pid_path, "r");
    if (f) {
      pid_t pid;
      if (fscanf(f, "%d", &pid) == 1) {
        if (kill(pid, SIGTERM) == 0) {
          printf("Sent SIGTERM to daemon (PID %d)\n", pid);
        } else {
          perror("kill");
        }
      }
      fclose(f);
    } else {
      fprintf(stderr, "Could not find PID file.\n");
    }
    return 0;
  }

  if (access(pid_path, F_OK) == 0) {
    fprintf(stderr, "Daemon is already running or PID file exists.\n");
    return EXIT_FAILURE;
  }

  daemonize();

  FILE *pf = fopen(pid_path, "w");
  if (pf) {
    fprintf(pf, "%d", getpid());
    fclose(pf);
  }

  load_config();

  struct sigaction sa;
  sa.sa_handler = signal_handler;
  sigemptyset(&sa.sa_mask);
  sa.sa_flags = 0;

  for (int i = 1; i < NSIG; i++) {
    if (i == SIGKILL || i == SIGSTOP)
      continue;
    sigaction(i, &sa, NULL);
  }

  log_message("Daemon started.");

  while (!should_exit) {
    if (reload_config) {
      reload_config = 0;
      load_config();
    }
    pause();
  }

  log_message("Daemon terminating.");
  unlink(pid_path);

  return 0;
}
