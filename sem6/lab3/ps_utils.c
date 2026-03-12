#define _DEFAULT_SOURCE
#include "ps_utils.h"
#include <ctype.h>
#include <dirent.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>

static int is_number(const char *str) {
  if (!*str)
    return 0;
  while (*str) {
    if (!isdigit(*str))
      return 0;
    str++;
  }
  return 1;
}

void list_processes() {
  DIR *dir = opendir("/proc");
  if (!dir) {
    perror("opendir /proc");
    return;
  }
  printf("%-8s %-s\n", "PID", "COMMAND");
  struct dirent *entry;
  while ((entry = readdir(dir)) != NULL) {
    if (entry->d_type == DT_DIR && is_number(entry->d_name)) {
      char comm_path[256], comm[256];
      snprintf(comm_path, sizeof(comm_path), "/proc/%s/comm", entry->d_name);
      FILE *f = fopen(comm_path, "r");
      if (f) {
        if (fgets(comm, sizeof(comm), f)) {
          comm[strcspn(comm, "\n")] = 0;
          printf("%-8s %-s\n", entry->d_name, comm);
        }
        fclose(f);
      }
    }
  }
  closedir(dir);
}

void print_process_info(pid_t pid) {
  char status_path[256], line[256];
  snprintf(status_path, sizeof(status_path), "/proc/%d/status", pid);
  FILE *f = fopen(status_path, "r");
  if (!f) {
    perror("fopen status");
    return;
  }
  printf("Detailed info for PID %d:\n", pid);
  while (fgets(line, sizeof(line), f)) {
    if (strncmp(line, "Name:", 5) == 0 || strncmp(line, "State:", 6) == 0 ||
        strncmp(line, "Pid:", 4) == 0 || strncmp(line, "PPid:", 5) == 0 ||
        strncmp(line, "VmSize:", 7) == 0 || strncmp(line, "Threads:", 8) == 0) {
      printf("%s", line);
    }
  }
  fclose(f);
}
