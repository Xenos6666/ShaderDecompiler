
#include "File.class.hpp"

using namespace std;

File::File(string file):
	filePath(file),
	stream(file.c_str(), ios::in),
	shaderParser(stream)
{
	if (!stream.is_open())
	{
		cerr << "File.class: couldn't open file for reading" << endl;
		throw -1;
	}

	shaderParser.Run();
}

File::~File(void)
{
	if (stream.is_open())
		stream.close();
}

void File::Write(ofstream &out) const {
	out << shaderParser.toString();
}
