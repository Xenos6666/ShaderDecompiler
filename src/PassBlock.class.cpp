
#include <regex>

#include "PassBlock.class.hpp"

using namespace std;

PassBlock::PassBlock(istream &_in, int _indent):
	BlockParser(_in, _indent)
{
}

PassBlock::~PassBlock(void)
{
	for (auto obj : vertSubs)
		delete obj.second;
	for (auto obj : fragSubs)
		delete obj.second;
}

void PassBlock::Cull(string _cull)
{
	cull = _cull;
}

void PassBlock::Blend(string src, string dst)
{
	src_blend = src;
	dst_blend = dst;
}

bool PassBlock::ProcessLine(std::string &line) const {
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^\t{" + to_string(indent) + "}GpuProgramID \\d+$");
	reg = regex(regstr);

	if (regex_match(line, match, reg))
		return false;

	if (src_blend != "" && dst_blend != "")
	{
		regstr = string("^(\t{" + to_string(indent) + "}Blend) .*$");
		reg = regex(regstr);

		if (regex_match(line, match, reg))
			line = regex_replace(line, reg, "$1 [" + src_blend + "] [" + dst_blend + "], [" + src_blend + "] [" + dst_blend + "]");
	}

	if (cull != "")
	{
		regstr = string("^(\t{" + to_string(indent) + "}Cull) .*$");
		reg = regex(regstr);

		if (regex_match(line, match, reg))
			line = regex_replace(line, reg, "$1 [_Cull]");
	}

	return true;
}

std::string PassBlock::ProgramRoutine(std::string programType) {
	if (programType != "vp" && programType != "fp")
	{
		cerr << "Unsupported program type: " << programType << endl;
		throw -5;
	}
	std::string regstr = string("^\\s*SubProgram \"\\s*([a-z0-9]+)\\s*\" \\{$");
	std::regex reg = regex(regstr);

	std::string keyregstr = string("^\\s*Keywords \\{ (\"[A-Z0-9_]+\" )+\\}$");
	std::regex keyreg = regex(keyregstr);

	std::string keywordregstr = string("\"([A-Z0-9_]+)\"");
	std::regex keywordreg = regex(keywordregstr);

	std::string ptyperegstr = string("^\\s*\"(!!)?[a-z0-9_]+$");
	std::regex ptypereg = regex(ptyperegstr);

	std::smatch match;
	std::string line("");
	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		LineMatch(line, match, reg, regstr);
		if (match[1] != "d3d11")
		{
			cerr << "Unsupported subprogram type: " << match[1] << endl;
			throw -5;
		}
		//content.push_back(new Line(line));

		GetLine(in, line);
		std::string keywords_str(" ");
		if (regex_match(line, match, keyreg))
		{
			while (regex_search(line, match, keywordreg))
			{
				keywords_str += match[1];
				keywords_str += " ";
				if (keywords.find(match[1]) == keywords.end())
					keywords.insert({match[1],0});
				if (programType == "vp" && vertkeywords.find(match[1]) == vertkeywords.end())
					vertkeywords.insert({match[1],0});
				else if (programType == "fp" && fragkeywords.find(match[1]) == fragkeywords.end())
					fragkeywords.insert({match[1],0});
				line = match.suffix();
			}
			GetLine(in, line);
		}

		LineMatch(line, match, ptypereg, ptyperegstr);

		if (keywords_vars.find(keywords_str) == keywords_vars.end())
			keywords_vars.insert({keywords_str,0});
		SubProgramBlock *subprogram = new SubProgramBlock(in, programType, indent + 2);
		if (programType == "vp")
			vertSubs.insert({keywords_str, subprogram});
		else if (programType == "fp")
			fragSubs.insert({keywords_str, subprogram});
		line = subprogram->Run();
		//content.push_back(new Line(line));

		GetLine(in, line);
	}

	return line;
}

std::string PassBlock::StencilRoutine(void) {
	std::string compregstr = string("^(\\s*Comp) Disabled$");
	std::regex compreg = regex(compregstr);

	std::smatch match;
	std::string line("");
	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		if (regex_match(line, match, compreg))
		{
			line = match[1];
			line += " Never";
		}
		content.push_back(new Line(line));

		GetLine(in, line);
	}

	return line;
}

std::string PassBlock::Run(void)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	string line("");

	regstr = string("^\t{" + to_string(indent) + "}Program \"([a-z]+)\" \\{$");
	reg = regex(regstr);

	std::string stregstr = string("^\t{" + to_string(indent) + "}Stencil \\{$");
	std::regex streg = regex(stregstr);

	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		if (strchr(line.c_str(), '{') != NULL && strchr(line.c_str(), '}') == NULL)
		{
			if (regex_match(line, match, reg))
				string lastline = ProgramRoutine(match[1]);
			else if (regex_match(line, match, streg))
			{
				content.push_back(new Line(line));
				line = StencilRoutine();
				content.push_back(new Line(line));
			}
			else
			{
				content.push_back(new Line(line));
				line = BlockRoutine();
				content.push_back(new Line(line));
			}
		}
		else if (ProcessLine(line))
			content.push_back(new Line(line));

		GetLine(in, line);
	}

	return line;
}

std::string PassBlock::makeProgram(void) const
{
	string ret("");
	ret += std::string(indent, '\t') + "CGPROGRAM\n\n";
	ret += "#include \"UnityCG.cginc\"\n\n";
	ret += "#pragma vertex vert\n";
	ret += "#pragma fragment frag\n\n";

	std::map<std::string,int> keywords_cpy(keywords);
	std::vector<std::map<std::string,int>> variants;
	for (auto obj : keywords_cpy)
	{
		if (obj.second == 0)
		{
			/* Probably stupid and doesn't work on some edge-cases */
			std::map<std::string,int> alts(keywords_cpy);
			for (auto prog : keywords_vars)
			{
				std::map<std::string,int> tmp(alts);
				if (prog.first.find(obj.first) != prog.first.npos)
				{
					for (auto alt : tmp)
						if (alt.first != obj.first
								&& prog.first.find(" " + alt.first + " ") != prog.first.npos)
							alts.erase(alt.first);
				}
			}
			for (auto prog : keywords_vars)
			{
				for (auto alt : alts)
					if (prog.first.find(" " + alt.first + " ") != prog.first.npos)
						goto outofloop;
				alts.insert({"__", 0});
				break;
outofloop:
				continue;
			}
			for (auto alt : alts)
				keywords_cpy[alt.first] = 1;
			variants.push_back(alts);
		}
	}

	for (auto prag : variants)
	{
		ret += "#pragma multi_compile_local";
		for (auto var : prag)
		{
			ret += " ";
			ret += var.first;
		}
		ret += "\n";
	}

	for (auto vert : vertSubs)
	{
		ret += "\n#if 1";
		string keys = vert.first;
		for (auto key : vertkeywords)
		{
			if (keys.find(" " + key.first + " ") == keys.npos)
				ret += " && !defined (" + key.first + ")";
			else
				ret += " && defined (" + key.first + ")";
		}
		ret += "\n\n";
		ret += vert.second->toString();
		ret += "\n#endif\n";
	}

	for (auto frag : fragSubs)
	{
		ret += "\n#if 1";
		string keys = frag.first;
		auto vert = vertSubs.find(keys);
		if (vert != vertSubs.end())
		{
			for (auto key : fragkeywords)
			{
				if (keys.find(" " + key.first + " ") == keys.npos)
					ret += " && !defined (" + key.first + ")";
				else
					ret += " && defined (" + key.first + ")";
			}
			ret += "\n\n";
			ret += frag.second->toString(vert->second->GetDeclaredUniforms());
			ret += "\n#endif\n";
		}
		else
		{
			for (auto key : fragkeywords)
			{
				if (keys.find(" " + key.first + " ") == keys.npos)
					ret += " && !defined (" + key.first + ")";
				else
					ret += " && defined (" + key.first + ")";
			}
			ret += "\n\n";
			ret += frag.second->toString();
			ret += "\n#endif\n";
		}
	}
	ret += std::string(indent, '\t') + "ENDCG\n";
	return ret;
}

std::string PassBlock::toString(void) const
{
	string ret("");
	ret += BlockParser::toString();
	ret += makeProgram();
	return ret;
}
