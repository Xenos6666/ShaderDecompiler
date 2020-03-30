
#include "Parser.class.hpp"

using namespace std;

Parser::Parser(istream &_in):
	in(_in)
{
}

Parser::~Parser(void)
{
}

void Parser::GetLine(istream &in, string &out)
{
	const std::regex empty("^\\s*$");
	out = "";
	while (regex_match(out, empty))
	{
		if (!getline(in, out))
		{
			cerr << "Unexpected EOF hit" << endl;
			throw -2;
		}
	}
}

void Parser::LastLine(istream &in)
{
	const std::regex empty("^\\s*$");
	std::string out("");
	while (regex_match(out, empty))
	{
		if (!getline(in, out))
		{
			cerr << "EOF expected at line: " << out << endl;
			throw -2;
		}
	}
}

void Parser::LineMatch(const string &line, smatch &match, const regex &expected,
		const string &expectedstr)
{
	if (!regex_match(line, match, expected))
	{
		cerr << "Line doesn't match expected format" << endl;
		cerr << "Line: " << line << "" << endl;
		cerr << "Expected: /" << expectedstr << "/" << endl;
		throw -3;
	}
}

void Parser::GetLineMatch(istream &in, smatch &match, const regex &expected, const string &expectedstr)
{
	std::string line;
	GetLine(in, line);
	LineMatch(line, match, expected, expectedstr);
}
