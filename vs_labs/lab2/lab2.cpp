#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <synchapi.h>

#define BUFFER_SIZE 65536
#define MAX_OVERLAPPED 4

typedef struct {
    OVERLAPPED overlapped;
    char buffer[BUFFER_SIZE];
    DWORD bytesRead;
    BOOL inUse;
} IO_OPERATION;

void process_data(char* data, DWORD size) {
    DWORD count = 0;
    for (DWORD i = 0; i < size; i++) {
        if (data[i] == 'a')
            count++;
    }
    printf("Chunk processed, 'a' count = %lu\n", count);
}

int main() {
    HANDLE hFile = CreateFile(L"C:\\Users\\acryoz\\ossp\\bigdata.bin", GENERIC_READ, FILE_SHARE_READ,
                              NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);
    if (hFile == INVALID_HANDLE_VALUE) {
        printf("Unable to open file\n");
        return 1;
    }

    IO_OPERATION ioOps[MAX_OVERLAPPED] = {0};
    DWORD fileSize = GetFileSize(hFile, NULL);
    if (fileSize == INVALID_FILE_SIZE) {
        printf("Unable to get file size\n");
        CloseHandle(hFile);
        return 1;
    }

    printf("File size: %lu\n", fileSize);

    HANDLE hEvents[MAX_OVERLAPPED];
    for (int i = 0; i < MAX_OVERLAPPED; i++) {
        hEvents[i] = CreateEvent(NULL, TRUE, FALSE, NULL);
        ioOps[i].overlapped.hEvent = hEvents[i];
        ioOps[i].inUse = FALSE;
    }

    DWORD offset = 0;
    int activeOps = 0;

    LARGE_INTEGER freq, start, end;
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&start);

    while (offset < fileSize || activeOps > 0) {
        for (int i = 0; i < MAX_OVERLAPPED && offset < fileSize; i++) {
            if (!ioOps[i].inUse) {
                ZeroMemory(&ioOps[i].overlapped, sizeof(OVERLAPPED));
                ioOps[i].overlapped.Offset = offset & 0xFFFFFFFF;
                ioOps[i].overlapped.OffsetHigh = (offset >> 32);
                ioOps[i].inUse = TRUE;
                ResetEvent(hEvents[i]);
                ioOps[i].overlapped.hEvent = hEvents[i];
                DWORD bytesToRead = (fileSize - offset) > BUFFER_SIZE
                                        ? BUFFER_SIZE
                                        : (fileSize - offset);

                printf("Started reading chunk %d\n", i);

                if (!ReadFile(hFile, ioOps[i].buffer, bytesToRead, NULL,
                              &ioOps[i].overlapped)) {
                    DWORD err = GetLastError();
                    if (err != ERROR_IO_PENDING) {
                        printf("ReadFile failed with error %lu\n", err);
                        ioOps[i].inUse = FALSE;
                        break;
                    }
                }
                offset += bytesToRead;
                activeOps++;
            }
        }

        printf("Started waiting\n");

        DWORD waitResult =
            WaitForMultipleObjects(MAX_OVERLAPPED, hEvents, FALSE, INFINITE);

        if (waitResult >= WAIT_OBJECT_0 &&
            waitResult < WAIT_OBJECT_0 + MAX_OVERLAPPED) {
            int index = waitResult - WAIT_OBJECT_0;
            DWORD bytesTransferred = 0;
            if (GetOverlappedResult(hFile, &ioOps[index].overlapped,
                                    &bytesTransferred, FALSE)) {
                printf("Starting processing chunk %d\n", index);
                process_data(ioOps[index].buffer, bytesTransferred);
            } else {
                printf("GetOverlappedResult error %lu\n", GetLastError());
            }
            ioOps[index].inUse = FALSE;
            activeOps--;
        }
    }

    QueryPerformanceCounter(&end);
    double elapsedSec = (double)(end.QuadPart - start.QuadPart) / freq.QuadPart;
    printf("Total processing time: %.3f seconds\n", elapsedSec);

    for (int i = 0; i < MAX_OVERLAPPED; i++) {
        CloseHandle(hEvents[i]);
    }
    CloseHandle(hFile);

    return 0;
}
