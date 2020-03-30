
#ifndef PARSER_HPP
# define PARSER_HPP

#include <string>
#include <iostream>
#include <fstream>
#include <regex>

#include "IWritable.class.hpp"

class Parser : public IWritable {

public:
	Parser(std::istream &in);
	~Parser(void);

	virtual std::string Run(void) = 0;

protected:
	static void GetLine(std::istream &in, std::string &out);
	static void LastLine(std::istream &in);
	static void LineMatch(const std::string &in, std::smatch &match, const std::regex &expected,
							const std::string &expectedstr);
	static void GetLineMatch(std::istream &in, std::smatch &match, const std::regex &expected,
							const std::string &expectedstr);

	std::istream &in;
};

#endif
