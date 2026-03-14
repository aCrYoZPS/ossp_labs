#include <arpa/inet.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <threads.h>
#include <unistd.h>
#include "common.h"

typedef struct {
    int socket;
    struct sockaddr_in address;
    char name[NAME_LEN];
} client_t;

client_t* clients[MAX_CLIENTS];
mtx_t clients_mutex;

bool send_message(const char* message, int sender_fd, const char* target_name) {
    bool found = false;
    mtx_lock(&clients_mutex);
    for (int i = 0; i < MAX_CLIENTS; ++i) {
        if (clients[i]) {
            if (target_name == NULL) {
                if (clients[i]->socket != sender_fd) {
                    send(clients[i]->socket, message, strlen(message), 0);
                }
                found = true;
            } else if (strcmp(clients[i]->name, target_name) == 0) {
                send(clients[i]->socket, message, strlen(message), 0);
                found = true;
                break;
            }
        }
    }
    mtx_unlock(&clients_mutex);
    return found;
}

int handle_client(void* arg) {
    client_t* cli = (client_t*)arg;
    char buffer[BUFFER_SIZE];
    char msg[BUFFER_SIZE + NAME_LEN + 50];
    int n;

    if ((n = recv(cli->socket, buffer, sizeof(buffer) - 1, 0)) <= 0) {
        close(cli->socket);
        free(cli);
        return 0;
    }
    buffer[strcspn(buffer, "\n\r")] = 0;
    strncpy(cli->name, buffer, NAME_LEN - 1);
    cli->name[NAME_LEN - 1] = '\0';

    printf("[Server] Client '%s' connected from %s:%d\n", cli->name,
           inet_ntoa(cli->address.sin_addr), ntohs(cli->address.sin_port));

    snprintf(msg, sizeof(msg), ">>> %s joined the chat\n", cli->name);
    send_message(msg, cli->socket, NULL);

    while ((n = recv(cli->socket, buffer, sizeof(buffer) - 1, 0)) > 0) {
        buffer[n] = '\0';
        if (strcmp(buffer, "/exit\n") == 0 ||
            strcmp(buffer, "/exit\r\n") == 0) {
            break;
        }

        if (buffer[0] == '@') {
            char* target_name = buffer + 1;
            char* space = strchr(target_name, ' ');
            if (space) {
                *space = '\0';
                char* content = space + 1;
                snprintf(msg, sizeof(msg), "[Private from %s]: %s", cli->name,
                         content);
                if (!send_message(msg, cli->socket, target_name)) {
                    snprintf(msg, sizeof(msg), "[Server] No such user: %s\n",
                             target_name);
                    send(cli->socket, msg, strlen(msg), 0);
                }
            } else {
                snprintf(msg, sizeof(msg),
                         "Invalid private message format. Use @name message\n");
                send(cli->socket, msg, strlen(msg), 0);
            }
        } else {
            printf("[%s]: %s", cli->name, buffer);
            snprintf(msg, sizeof(msg), "[%s]: %s", cli->name, buffer);
            send_message(msg, cli->socket, NULL);
        }
    }

    printf("[Server] Client '%s' disconnected\n", cli->name);
    snprintf(msg, sizeof(msg), "<<< %s left the chat\n", cli->name);
    send_message(msg, cli->socket, NULL);

    close(cli->socket);
    mtx_lock(&clients_mutex);
    for (int i = 0; i < MAX_CLIENTS; ++i) {
        if (clients[i] == cli) {
            clients[i] = NULL;
            break;
        }
    }
    mtx_unlock(&clients_mutex);
    free(cli);

    return 0;
}

int main() {
    int server_fd, client_fd;
    struct sockaddr_in server_addr, client_addr;
    socklen_t client_len = sizeof(client_addr);
    thrd_t thread;

    server_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (server_fd < 0) {
        perror("socket failed");
        exit(1);
    }

    int opt = 1;
    setsockopt(server_fd, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt));

    server_addr.sin_family = AF_INET;
    server_addr.sin_addr.s_addr = INADDR_ANY;
    server_addr.sin_port = htons(PORT);

    if (bind(server_fd, (struct sockaddr*)&server_addr, sizeof(server_addr)) <
        0) {
        perror("bind failed");
        exit(1);
    }

    if (listen(server_fd, 5) < 0) {
        perror("listen failed");
        exit(1);
    }

    mtx_init(&clients_mutex, mtx_plain);
    printf("[Server] Listening on port %d...\n", PORT);

    while (1) {
        client_fd =
            accept(server_fd, (struct sockaddr*)&client_addr, &client_len);
        if (client_fd < 0) {
            perror("accept failed");
            continue;
        }

        mtx_lock(&clients_mutex);
        int i;
        for (i = 0; i < MAX_CLIENTS; ++i) {
            if (!clients[i]) {
                clients[i] = (client_t*)malloc(sizeof(client_t));
                clients[i]->socket = client_fd;
                clients[i]->address = client_addr;
                if (thrd_create(&thread, handle_client, (void*)clients[i]) !=
                    thrd_success) {
                    perror("thrd_create failed");
                    close(client_fd);
                    free(clients[i]);
                    clients[i] = NULL;
                } else {
                    thrd_detach(thread);
                }
                break;
            }
        }
        if (i == MAX_CLIENTS) {
            printf("[Server] Max clients reached. Rejecting connection.\n");
            close(client_fd);
        }
        mtx_unlock(&clients_mutex);
    }

    close(server_fd);
    mtx_destroy(&clients_mutex);
    return 0;
}
