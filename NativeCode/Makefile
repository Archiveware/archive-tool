CC=gcc
CFLAGS=-fpic -std=c99 -lpthread

libNativeCode.so: crc32c.o galois.o  Interface.o  jerasure.o  lz4.o  reed_sol.o
	$(CC) -shared -o libNativeCode.so crc32c.o galois.o Interface.o jerasure.o lz4.o reed_sol.o

%.o: %.c 
	$(CC) -c -o $@ $< $(CFLAGS)

clean:
	rm libNativeCode.so crc32c.o galois.o Interface.o jerasure.o lz4.o reed_sol.o

install:
	cp libNativeCode.so ../bin/
