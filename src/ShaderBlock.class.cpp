
#include <regex>

#include "ShaderBlock.class.hpp"
#include "PassBlock.class.hpp"

using namespace std;

ShaderBlock::ShaderBlock(istream &_in, int _indent):
	BlockParser(_in, _indent)
{
}

ShaderBlock::~ShaderBlock(void)
{
}

std::string ShaderBlock::SubShaderRoutine(void)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	string line("");

	regstr = string("^\t{" + to_string(indent + 1) + "}Pass \\{$");
	reg = regex(regstr);

	std::string grabregstr = string("^\t{" + to_string(indent + 1) + "}GrabPass \\{$");
	std::regex grabreg = regex(grabregstr);

	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		content.push_back(new Line(line));

		if (regex_match(line, match, reg))
		{
			PassBlock *newBlock = new PassBlock(in, indent + 2);
			newBlock->Blend(src_blend, dst_blend);
			newBlock->Cull(cull);
			line = newBlock->Run();
			content.push_back(newBlock);
			content.push_back(new Line(line));
		}
		else if (regex_match(line, match, grabreg))
		{
			BlockParser *newBlock = new BlockParser(in, indent + 2);
			line = newBlock->Run();
			content.push_back(newBlock);
			content.push_back(new Line(line));
		}

		GetLine(in, line);
	}
	return line;
}

std::string ShaderBlock::PropertiesRoutine(void)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	string line("");

	regstr = string("^.*(_(Src|Dst)Blend(Float)?|_Cull).*$");
	reg = regex(regstr);

	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		content.push_back(new Line(line));

		if (regex_match(line, match, reg))
		{
			if (((string)match[1]).find("SrcBlend") != ((string)match[1]).npos)
				src_blend = match[1];
			if (((string)match[1]).find("DstBlend") != ((string)match[1]).npos)
				dst_blend = match[1];
			if (match[1] == "_Cull")
				cull = match[1];
		}

		GetLine(in, line);
	}
	return line;
}

std::string ShaderBlock::Run(void)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	string line("");

	GetLine(in, line);

	regstr = string("^\\t{" + to_string(indent) + "}Properties \\{$");
	reg = regex(regstr);
	LineMatch(line, match, reg, regstr);
	content.push_back(new Line(line));

	string lastline = PropertiesRoutine();
	content.push_back(new Line(lastline));

	regstr = string("^\t{" + to_string(indent) + "}SubShader \\{$");
	reg = regex(regstr);

	std::string fallregstr = string("^\\s*Fallback \".*\"$");
	std::regex fallreg = regex(fallregstr);

	std::string editregstr = string("^\\s*CustomEditor \".*\"$");
	std::regex editreg = regex(editregstr);

	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		if (regex_match(line, match, fallreg) || regex_match(line, match, editreg))
		{
			content.push_back(new Line(line));

			GetLine(in, line);
			continue ;
		}
		LineMatch(line, match, reg, regstr);
		content.push_back(new Line(line));

		string lastline = SubShaderRoutine();
		content.push_back(new Line(lastline));

		GetLine(in, line);
	}

	return line;
}
