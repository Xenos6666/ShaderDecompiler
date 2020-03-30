
#ifndef BLOCK_PARSER_HPP
# define BLOCK_PARSER_HPP

#include <string>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <list>

#include "Parser.class.hpp"

class BlockParser : public Parser {
public:
	BlockParser(std::istream &in, int _indent = 0);
	~BlockParser(void);

	std::string Run(void);
	std::string toString() const;

protected:
	std::list<IWritable*> content;
	int indent = 0;

	std::string BlockRoutine(void);
};

#endif
