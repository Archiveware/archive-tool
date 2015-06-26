CC=gcc
CFLAGS=-fpic -std=c99

libNativeCode.so: galois.o  Interface.o  jerasure.o  lz4.o  reed_sol.o
	$(CC) -shared -o libNativeCode.so galois.o  Interface.o  jerasure.o  lz4.o  reed_sol.o

%.o: %.c 
	$(CC) -c -o $@ $< $(CFLAGS)

clean:
	rm libNativeCode.so galois.o  Interface.o  jerasure.o  lz4.o  reed_sol.o
