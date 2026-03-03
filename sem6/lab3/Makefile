CC = gcc
CFLAGS = -Wall -Wextra -std=c23
TARGET = myps
OBJS = main.o ps_utils.o

all: $(TARGET)

$(TARGET): $(OBJS)
	$(CC) $(CFLAGS) -o $(TARGET) $(OBJS)

main.o: main.c ps_utils.h
	$(CC) $(CFLAGS) -c main.c

ps_utils.o: ps_utils.c ps_utils.h
	$(CC) $(CFLAGS) -c ps_utils.c

clean:
	rm -f $(OBJS) $(TARGET)

rebuild: clean all

.PHONY: all clean rebuild
