#include <arpa/inet.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <threads.h>
#include <unistd.h>
#include "common.h"

int recv_handler(void* arg) {
    int socket = *(int*)arg;
    char buffer[BUFFER_SIZE];
    int n;

    while ((n = recv(socket, buffer, sizeof(buffer) - 1, 0)) > 0) {
        buffer[n] = '\0';
        printf("%s", buffer);
        fflush(stdout);
    }

    if (n == 0) {
        printf("[Client] Server closed the connection.\n");
    } else if (n < 0) {
        perror("recv failed");
    }

    exit(0);
}

int main() {
    int client_fd;
    struct sockaddr_in server_addr;
    char buffer[BUFFER_SIZE];
    thrd_t thread;

    client_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (client_fd < 0) {
        perror("socket failed");
        exit(1);
    }

    server_addr.sin_family = AF_INET;
    server_addr.sin_addr.s_addr = inet_addr("127.0.0.1");
    server_addr.sin_port = htons(PORT);

    if (connect(client_fd, (struct sockaddr*)&server_addr,
                sizeof(server_addr)) < 0) {
        perror("connect failed");
        exit(1);
    }

    printf("Enter nickname: ");
    if (fgets(buffer, NAME_LEN, stdin) != NULL) {
        send(client_fd, buffer, strlen(buffer), 0);
    }

    printf("[Client] Connected to server at 127.0.0.1:%d.\n", PORT);
    printf(
        "[Usage] Type message for broadcast, or @name message for private msg, "
        "or /exit to disconnect.\n");

    if (thrd_create(&thread, recv_handler, (void*)&client_fd) != thrd_success) {
        perror("thrd_create failed");
        exit(1);
    }

    while (fgets(buffer, sizeof(buffer), stdin) != NULL) {
        if (send(client_fd, buffer, strlen(buffer), 0) < 0) {
            perror("send failed");
            break;
        }

        if (strcmp(buffer, "/exit\n") == 0 ||
            strcmp(buffer, "/exit\r\n") == 0) {
            break;
        }
    }

    close(client_fd);
    return 0;
}
