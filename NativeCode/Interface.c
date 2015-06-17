#include <stdlib.h>
#include "jerasure.h"
#include "reed_sol.h"

__declspec(dllexport) int Decode(int k, int m, int w, char *datablock, char *codingblock, int blocksize, int erasures[]) {
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

__declspec(dllexport) int Decompress(const char *source, char *dest, int compressedSize, int maxDecompressedSize){
	return LZ4_decompress_safe(source, dest, compressedSize, maxDecompressedSize);
}
