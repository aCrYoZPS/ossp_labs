import os
import sys


def generate_random_binary_file(filename, size_in_mb):
    chunk_size = 1024 * 1024
    total_bytes = int(size_in_mb * chunk_size)

    with open(filename, 'wb') as f:
        bytes_written = 0
        while bytes_written < total_bytes:
            to_write = min(chunk_size, total_bytes - bytes_written)
            data = os.urandom(to_write)
            f.write(data)
            bytes_written += to_write


def main():
    generate_random_binary_file("bigdata.bin", float(sys.argv[1]))


if __name__ == '__main__':
    main()
