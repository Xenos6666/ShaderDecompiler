
#ifndef SUB_PROGRAM_BLOCK_HPP
# define SUB_PROGRAM_BLOCK_HPP

#include <string>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <list>

#include "Parser.class.hpp"
#include "BlockParser.class.hpp"
#include "Line.class.hpp"

class SubProgramBlock : public BlockParser {
public:
	SubProgramBlock(std::istream &in, std::string _type, int _indent = 1);
	~SubProgramBlock(void);

	std::string Run(void);
	std::string toString(void) const;

protected:
	std::string type;
	std::map<std::string,std::string> uniforms;
	std::vector<std::string> input;
	std::vector<std::string> output;
	std::vector<std::string> temp;
	bool instancing = false;

	std::string BlockRoutine(void);
	void ProcessSingleGlobal(std::string line);
	void ProcessSingleInput(std::string line);
	void ProcessSingleOutput(std::string line);
	void ProcessSingleTemp(std::string line);

private:
	bool ProcessLine(std::string &line);
	bool ProcessVarLine(std::string &line);
	bool ProcessBlock(std::string &line);
	void InsertTemp(void);
};

#endif
