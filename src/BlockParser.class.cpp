
#include <regex>

#include "BlockParser.class.hpp"
#include "Line.class.hpp"

using namespace std;

BlockParser::BlockParser(istream &_in, int _indent):
	Parser(_in),
	indent(_indent)
{
}

BlockParser::~BlockParser(void)
{
	for (IWritable *obj : content)
		delete obj;
}

std::string BlockParser::BlockRoutine(void)
{
	string line("");

	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		content.push_back(new Line(line));
		if (strchr(line.c_str(), '{') != NULL && strchr(line.c_str(), '}') == NULL)
		{
			string lastline = BlockRoutine();
			content.push_back(new Line(lastline));
		}
		GetLine(in, line);
	}
	return line;
}

std::string BlockParser::Run(void)
{
	return BlockRoutine();
}

string BlockParser::toString(void) const
{
	string ret("");
	for (IWritable *obj : content)
		ret += obj->toString();
	return ret;
}
