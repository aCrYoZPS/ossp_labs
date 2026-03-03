#include "ps_utils.h"
#include <getopt.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

int main(int argc, char *argv[]) {
  int opt;
  int show_all = 0;
  pid_t target_pid = -1;

  if (argc < 2) {
    fprintf(stderr, "Usage: %s -l or -p <pid>\n", argv[0]);
    return 1;
  }

  while ((opt = getopt(argc, argv, "lp:")) != -1) {
    switch (opt) {
    case 'l':
      show_all = 1;
      break;
    case 'p':
      target_pid = atoi(optarg);
      if (target_pid <= 0) {
        fprintf(stderr, "Invalid PID: %s\n", optarg);
        return 1;
      }
      break;
    default:
      fprintf(stderr, "Usage: %s -l or -p <pid>\n", argv[0]);
      return 1;
    }
  }

  if (show_all) {
    list_processes();
  } else if (target_pid != -1) {
    print_process_info(target_pid);
  } else {
    fprintf(stderr, "Usage: %s -l or -p <pid>\n", argv[0]);
    return 1;
  }

  return 0;
}
