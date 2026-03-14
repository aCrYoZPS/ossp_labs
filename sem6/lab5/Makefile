CC = gcc
CFLAGS = -Wall -Wextra -pthread -g
TARGET = lab5

all: $(TARGET)

$(TARGET): main.c
	$(CC) $(CFLAGS) -o $(TARGET) main.c

clean:
	rm -f $(TARGET)

run: all
	./$(TARGET)
