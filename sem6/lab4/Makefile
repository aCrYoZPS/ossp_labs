CC = gcc
CFLAGS = -Wall -Wextra -D_GNU_SOURCE
TARGET = mydaemon

all: $(TARGET)

$(TARGET): main.c
	$(CC) $(CFLAGS) -o $(TARGET) main.c

clean:
	rm -f $(TARGET) daemon.log daemon.pid
