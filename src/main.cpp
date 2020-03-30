
#include <string>
#include <iostream>
#include <fstream>

#include "File.class.hpp"

using namespace std;

int		main(int argc, char* argv[])
{
	if (argc < 3)
	{
		cerr << "shaderReconstructor inputFile outputFile" << endl;
		return -1;
	}
	string in = argv[1];
	string out = argv[2];

	try {
		File shaderFile(in);

		ofstream outFile(out);

		if (!outFile.is_open())
		{
			cerr << "Couldn't open output file for reading" << endl;
			throw -1;
		}

		shaderFile.Write(outFile);
		outFile.close();

	} catch (int e) {
		return e;
	}

}
