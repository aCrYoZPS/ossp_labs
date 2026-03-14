#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <threads.h>
#include <time.h>
#include <unistd.h>

#define NUM_WORKERS 3
#define FAILURE_PROBABILITY 0.25
#define WORK_DURATION_MS 500
#define MAX_RESTARTS 5

typedef struct {
  int id;
  thrd_t thread;
  bool is_running;
  bool is_finished;
  mtx_t mutex;
  int exit_status;
  int restart_count;
} worker_info_t;

int worker_func(void *worker_info) {
  worker_info_t *worker = (worker_info_t *)worker_info;
  int id = worker->id;
  int iters = (rand() % 9) + 3;

  printf("[Worker %d] Starting work cycle...\n", id);

  for (int i = 0; i < iters; ++i) {
    printf("[Worker %d] Working... (Step %d/%d)\n", id, i + 1, iters);
    usleep(WORK_DURATION_MS * 1000);

    if (((double)rand() / RAND_MAX) < FAILURE_PROBABILITY) {
      printf("[Worker %d] Encountered a critical error!\n", id);
      mtx_lock(&worker->mutex);
      worker->is_finished = true;
      worker->exit_status = -1;
      mtx_unlock(&worker->mutex);
      return -1;
    }
  }
  printf("[Worker %d] Work cycle completed successfully.\n", id);
  mtx_lock(&worker->mutex);
  worker->is_finished = true;
  worker->exit_status = 0;
  mtx_unlock(&worker->mutex);
  return 0;
}

void start_worker(worker_info_t *worker) {
  worker->is_running = true;
  worker->is_finished = false;
  if (thrd_create(&worker->thread, worker_func, (void *)worker) !=
      thrd_success) {
    perror("thrd_create failed");
    worker->is_running = false;
  }
}

int main() {
  srand(time(NULL));
  worker_info_t workers[NUM_WORKERS];
  printf("[Manager] Initializing system with %d workers\n", NUM_WORKERS);

  for (int i = 0; i < NUM_WORKERS; ++i) {
    workers[i].id = i;
    workers[i].restart_count = 0;
    mtx_init(&workers[i].mutex, mtx_plain);
    start_worker(&workers[i]);
  }

  bool active = true;
  while (active) {
    active = false;
    for (int i = 0; i < NUM_WORKERS; ++i) {
      worker_info_t *worker = &workers[i];
      mtx_lock(&worker->mutex);
      if (worker->is_running) {
        if (worker->is_finished) {
          int res;
          thrd_join(worker->thread, &res);
          if (worker->exit_status != 0) {
            printf("[Manager] Thread %d failed (restart attempts left: %d).\n",
                   worker->id, MAX_RESTARTS - worker->restart_count);
            if (worker->restart_count >= MAX_RESTARTS) {
              printf("[Manager] Thread %d exhausted restart attempts and is "
                     "stopped permanently.\n",
                     worker->id);
              worker->is_running = false;
            } else {
              printf("[Manager] Restarting thread %d.\n", worker->id);
              worker->restart_count += 1;
              start_worker(worker);
              active = true;
            }
          } else {
            printf("[Manager] Thread %d finished successfully.\n", worker->id);
            worker->is_running = false;
          }
        } else {
          active = true;
        }
      }
      mtx_unlock(&worker->mutex);
    }
    usleep(100 * 1000);
  }

  printf("[Manager] All workers have stopped. Cleaning up.\n");
  for (int i = 0; i < NUM_WORKERS; i++) {
    mtx_destroy(&workers[i].mutex);
  }
  return 0;
}
