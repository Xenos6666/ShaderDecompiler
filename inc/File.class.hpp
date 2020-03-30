
#include <string>
#include <iostream>
#include <fstream>

#include "ShaderParser.class.hpp"

class File {

public:
	File(std::string file);
	~File(void);

	void Write(std::ofstream &out) const;

private:
	std::string filePath;
	std::ifstream stream;
	ShaderParser shaderParser;
};
