CC = gcc
CFLAGS = -Wall -Wextra -std=c11 -D_POSIX_C_SOURCE=200809L
LDFLAGS = -pthread

all: server client

server: server.c common.h
	$(CC) $(CFLAGS) server.c -o server $(LDFLAGS)

client: client.c common.h
	$(CC) $(CFLAGS) client.c -o client $(LDFLAGS)

clean:
	rm -f server client
