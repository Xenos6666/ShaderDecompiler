
#ifndef SHADER_PARSER_HPP
# define SHADER_PARSER_HPP

#include <string>
#include <iostream>
#include <fstream>

#include "Parser.class.hpp"
#include "ShaderBlock.class.hpp"
#include "Line.class.hpp"

class ShaderParser : public Parser {
public:
	ShaderParser(std::istream &in);
	~ShaderParser(void);

	std::string Run(void);
	std::string toString() const;

private:
	Line firstLine;
	ShaderBlock shaderBlock;
	Line lastLine;
};

#endif
