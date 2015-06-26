#include <stdlib.h>
#include "jerasure.h"
#include "reed_sol.h"

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT
#endif

EXPORT int Decode(int k, int m, int w, char *datablock, char *codingblock, int blocksize, int erasures[]) {
	int i;
	int *matrix = reed_sol_vandermonde_coding_matrix(k, m, w);
	char **data = (char **)malloc(sizeof(char*)*k);
	char **coding = (char **)malloc(sizeof(char*)*m);

	if (coding == NULL || data == NULL || matrix == NULL)
		return -1;

	for (i = 0; i < k; i++) {
		data[i] = datablock + (i * blocksize);
	}

	for (i = 0; i < m; i++) {
		coding[i] = codingblock + (i * blocksize);
	}

	i = jerasure_matrix_decode(k, m, w, matrix, 1, erasures, data, coding, blocksize);

	free(coding);
	free(data);
	free(matrix);

	return i;
}

EXPORT int Decompress(const char *source, char *dest, int compressedSize, int maxDecompressedSize){
	return LZ4_decompress_safe(source, dest, compressedSize, maxDecompressedSize);
}


EXPORT int Test(char bytes[]) {	
	union bytes_to_int_u {
		char bytes[];
		int result;
	} bytes_to_int;

	for (int i = 0; i < 4; i++)
		bytes_to_int.bytes[i] = bytes[i];

	return bytes_to_int.result;
}
