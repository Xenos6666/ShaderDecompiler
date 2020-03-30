
#ifndef SHADER_BLOCK_HPP
# define SHADER_BLOCK_HPP

#include <string>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <list>

#include "Parser.class.hpp"
#include "BlockParser.class.hpp"
#include "Line.class.hpp"

class ShaderBlock : public BlockParser {
public:
	ShaderBlock(std::istream &in, int _indent = 1);
	~ShaderBlock(void);

	std::string Run(void);

private:
	std::string SubShaderRoutine(void);
	std::string PropertiesRoutine(void);
	std::string src_blend = "";
	std::string dst_blend = "";
	std::string cull = "";
};

#endif
