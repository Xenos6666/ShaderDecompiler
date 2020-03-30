
#include <regex>

#include "ShaderParser.class.hpp"

using namespace std;

ShaderParser::ShaderParser(istream &_in):
	Parser(_in),
	shaderBlock(_in)
{
}

ShaderParser::~ShaderParser(void)
{
}

/*
 * because I'm a madlad, this parser doesn't follow standard syntax and only works on
 * shaders decompiled by uTinyRipper
 */
string ShaderParser::Run(void)
{
	std::string labelstr;
	std::regex label;
	std::smatch match;

	std::string tmp("");
	GetLine(in, tmp);

	labelstr = string("^Shader \"([^\"]+)\" \\{$");
	label = regex(labelstr);
	LineMatch(tmp, match, label, labelstr);
	firstLine(tmp);

	std::string lastline = shaderBlock.Run();

	labelstr = string("^}$");
	label = regex(labelstr);
	LineMatch(lastline, match, label, labelstr);
	lastLine(lastline);

	return "";
}

string ShaderParser::toString(void) const
{
	string ret("");
	ret += firstLine.toString();
	ret += shaderBlock.toString();
	ret += lastLine.toString();
	return ret;
}
