#include <stdio.h>
#include <stdlib.h>
#include <synchapi.h>
#include <windows.h>

#include <stdio.h>
#include <windows.h>

#define BUFFER_SIZE 65536
#define MAX_OVERLAPPED 4
#define MAX_WORKER_THREADS 1024

HANDLE hWorkerThreads[MAX_WORKER_THREADS];
int workerThreadCount = 0;

typedef struct {
    OVERLAPPED overlapped;
    char buffer[BUFFER_SIZE];
    DWORD bytesRead;
    BOOL inUse;
    DWORD chunkIndex;
} IO_OPERATION;

HANDLE hFile;
IO_OPERATION ioOps[MAX_OVERLAPPED];
DWORD fileSize;
DWORD64 offset = 0;
CRITICAL_SECTION cs;

void process_data(char* data, DWORD size, DWORD chunkIndex) {
    DWORD count = 0;
    for (DWORD i = 0; i < size; i++) {
        if (data[i] == 'a')
            count++;
    }
    printf("Chunk %lu processed, 'a' count = %lu\n", chunkIndex, count);
}

DWORD WINAPI worker_thread(LPVOID param) {
    IO_OPERATION* ioOp = (IO_OPERATION*)param;
    process_data(ioOp->buffer, ioOp->bytesRead, ioOp->chunkIndex);

    // Mark ioOp free for reuse
    EnterCriticalSection(&cs);
    ioOp->inUse = FALSE;
    LeaveCriticalSection(&cs);

    return 0;
}

int main() {
    InitializeCriticalSection(&cs);

    hFile = CreateFile("C:\\Users\\acryoz\\ossp\\bigdata.bin", GENERIC_READ,
                       FILE_SHARE_READ, NULL, OPEN_EXISTING,
                       FILE_FLAG_OVERLAPPED, NULL);
    if (hFile == INVALID_HANDLE_VALUE) {
        printf("Unable to open file\n");
        return 1;
    }

    fileSize = GetFileSize(hFile, NULL);
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

    int activeOps = 0;
    LARGE_INTEGER freq, start, end;
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&start);

    while (offset < fileSize || activeOps > 0) {
        for (int i = 0; i < MAX_OVERLAPPED && offset < fileSize; i++) {
            EnterCriticalSection(&cs);
            if (!ioOps[i].inUse) {
                ZeroMemory(&ioOps[i].overlapped, sizeof(OVERLAPPED));
                ioOps[i].overlapped.Offset = (DWORD)(offset & 0xFFFFFFFF);
                ioOps[i].overlapped.OffsetHigh = (DWORD)(offset >> 32);
                ioOps[i].overlapped.hEvent = hEvents[i];
                ioOps[i].inUse = TRUE;
                ioOps[i].chunkIndex = (DWORD)(offset / BUFFER_SIZE);

                DWORD bytesToRead = (fileSize - offset) > BUFFER_SIZE
                                        ? BUFFER_SIZE
                                        : (fileSize - offset);

                ResetEvent(hEvents[i]);

                if (!ReadFile(hFile, ioOps[i].buffer, bytesToRead, NULL,
                              &ioOps[i].overlapped)) {
                    DWORD err = GetLastError();
                    if (err != ERROR_IO_PENDING) {
                        printf("ReadFile failed with error %lu\n", err);
                        ioOps[i].inUse = FALSE;
                        LeaveCriticalSection(&cs);
                        break;
                    }
                }
                offset += bytesToRead;
                activeOps++;
            }
            LeaveCriticalSection(&cs);
        }

        DWORD waitResult =
            WaitForMultipleObjects(MAX_OVERLAPPED, hEvents, FALSE, INFINITE);

        if (waitResult >= WAIT_OBJECT_0 &&
            waitResult < WAIT_OBJECT_0 + MAX_OVERLAPPED) {
            int index = waitResult - WAIT_OBJECT_0;
            DWORD bytesTransferred = 0;
            if (GetOverlappedResult(hFile, &ioOps[index].overlapped,
                                    &bytesTransferred, FALSE)) {
                ioOps[index].bytesRead = bytesTransferred;

                HANDLE hThread = CreateThread(NULL, 0, worker_thread,
                                              &ioOps[index], 0, NULL);
                if (hThread) {
                    if (workerThreadCount < MAX_WORKER_THREADS) {
                        hWorkerThreads[workerThreadCount++] = hThread;
                    } else {
                        CloseHandle(hThread);
                    }
                }

                EnterCriticalSection(&cs);
                activeOps--;
                LeaveCriticalSection(&cs);
            } else {
                printf("GetOverlappedResult error %lu\n", GetLastError());
                EnterCriticalSection(&cs);
                ioOps[index].inUse = FALSE;
                activeOps--;
                LeaveCriticalSection(&cs);
            }
        }
    }
    if (workerThreadCount > 0) {
        WaitForMultipleObjects(workerThreadCount, hWorkerThreads, TRUE,
                               INFINITE);
        for (int i = 0; i < workerThreadCount; i++) {
            CloseHandle(hWorkerThreads[i]);
        }
    }

    QueryPerformanceCounter(&end);
    double elapsedSec = (double)(end.QuadPart - start.QuadPart) / freq.QuadPart;
    printf("Total processing time: %.3f seconds\n", elapsedSec);

    for (int i = 0; i < MAX_OVERLAPPED; i++)
        CloseHandle(hEvents[i]);
    CloseHandle(hFile);
    DeleteCriticalSection(&cs);

    return 0;
}
