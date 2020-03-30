
#ifndef PASS_BLOCK_HPP
# define PASS_BLOCK_HPP

#include <string>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <list>

#include "Parser.class.hpp"
#include "BlockParser.class.hpp"
#include "SubProgramBlock.class.hpp"
#include "Line.class.hpp"

class PassBlock : public BlockParser {
public:
	PassBlock(std::istream &in, int _indent = 1);
	~PassBlock(void);

	std::string Run(void);
	void Blend(std::string src, std::string dst);
	void Cull(std::string _cull);

protected:
	std::map<std::string,SubProgramBlock*> vertSubs;
	std::map<std::string,SubProgramBlock*> fragSubs;
	std::map<std::string,int> keywords_vars;
	std::map<std::string,int> keywords;
	std::map<std::string,int> vertkeywords;
	std::map<std::string,int> fragkeywords;

	std::string toString(void) const;

private:
	std::string src_blend = "";
	std::string dst_blend = "";
	std::string cull = "";
	bool ProcessLine(std::string &line) const;
	std::string ProgramRoutine(std::string programType);
	std::string StencilRoutine(void);
	std::string makeProgram(void) const;
};

#endif
