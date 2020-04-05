
#include <regex>

#include "SubProgramBlock.class.hpp"
#include "Line.class.hpp"

using namespace std;

SubProgramBlock::SubProgramBlock(istream &_in, std::string _type, int _indent):
	BlockParser(_in, _indent),
	type(_type)
{
	if (type != "vp" && type != "fp")
	{
		cerr << "Unsupported subprogram type: " << type << endl;
		throw -5;
	}
}

SubProgramBlock::~SubProgramBlock(void)
{
}

void SubProgramBlock::ProcessSingleInput(std::string line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^\\s*(layout\\(location = \\d\\)\\s+)?(in\\s+)([a-zA-Z0-9_]+\\s+(in_|vs_)(POSITION|NORMAL|[a-zA-Z0-9_]+)\\d*);$");
	reg = regex(regstr);

	LineMatch(line, match, reg, regstr);

	std::string tmp = regex_replace(line, reg, "$3 : $5;");
	input.push_back(tmp);
}

void SubProgramBlock::ProcessSingleOutput(std::string line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^(\\s*)(flat out\\s+)([a-z0-9_]+\\s+vs_SV_InstanceID\\d*);$");
	reg = regex(regstr);

	if (regex_match(line, match, reg))
	{
		input.push_back("UNITY_VERTEX_INPUT_INSTANCE_ID");
		instancing = true;
		return ;
	}

	regstr = string("^(\\s*)(layout\\(location = \\d\\)\\s+)?(flat\\s+)?(out\\s+)([a-zA-Z0-9_]+\\s+(vs_)?([a-zA-Z0-9_]+));$");
	reg = regex(regstr);

	LineMatch(line, match, reg, regstr);
	std::string tmp = regex_replace(line, reg, "$5 : $7;");
	output.push_back(tmp);
}

void SubProgramBlock::ProcessSingleTemp(std::string line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^(\\s*)[a-z0-9_]+\\s+u_xlat[a-z0-9_]*;$");
	reg = regex(regstr);

	LineMatch(line, match, reg, regstr);
	temp.push_back(line);
}

void SubProgramBlock::ProcessSingleGlobal(std::string line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^\\s*(layout\\(location = \\d\\)\\s+)?(uniform\\s+)?([a-zA-Z0-9_]+\\s+([a-zA-Z0-9_]+)(\\[.+\\])?;)$");
	reg = regex(regstr);

	LineMatch(line, match, reg, regstr);
	std::string tmp = "";
	if (match[2] == "")
	{
		tmp += "uniform ";
		tmp += match[3];
	}
	else
	{
		tmp += match[2];
		tmp += match[3];
	}
	if (ProcessVarLine(tmp))
		uniforms.insert({match[4],tmp});
}

bool SubProgramBlock::ProcessBlock(std::string &line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^\\s*layout\\([^)]*\\) uniform (.*) \\{$");
	reg = regex(regstr);

	if (regex_match(line, match, reg))
	{
		if (match[1] == "VGlobals" || match[1] == "PGlobals")
		{
			string line("");
			GetLine(in, line);
			while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
			{
				ProcessSingleGlobal(line);
				GetLine(in, line);
			}
		}
		else
		{
			BlockParser *del = new BlockParser(in);
			del->Run();
			delete del;
		}
		return false;
	}

	regstr = string("^\\s*struct unity_.* \\{$");
	reg = regex(regstr);

	if (regex_match(line, match, reg))
	{
		BlockParser *del = new BlockParser(in);
		del->Run();
		delete del;
		return false;
	}

	return true;
}

bool SubProgramBlock::ProcessVarLine(std::string &line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	/* Apparently unused variables ARE used
	regstr = string("^.* unused.*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;
		*/

	// I'm not sure anymore if this is needed or not
	regstr = string("^.* unity_([^P]|P[^r]|Pr[^o]|Pro[^j]).*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("^.* _ZBufferParams.*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("^.* _Time.*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("^.* _ProjectionParams.*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("^.* _ScreenParams.*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("(vec|mat)(\\d)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "float$2");

	regstr = string("sampler2DArray");
	reg = regex(regstr);
	line = regex_replace(line, reg, "sampler2D[]");

	return true;
}

void SubProgramBlock::InsertTemp(void)
{
	if (instancing)
		content.push_back(new Line("UNITY_SETUP_INSTANCE_ID(i);"));
	for (auto tmp : temp)
	{
		content.push_back(new Line(tmp));
	}
}

bool SubProgramBlock::ProcessLine(std::string &line)
{
	std::string regstr;
	std::regex reg;
	std::smatch match;

	regstr = string("^\\s*#(version|extension).*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("^\\s*void main\\(\\)$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
	{
		regstr = string("^\\s*\\{$");
		reg = regex(regstr);

		line = std::string(indent, '\t');
		if (type == "vp")
		{
			line += "inter vert(app_in i)";
			content.push_back(new Line(line));
			GetLine(in, line);
			LineMatch(line, match, reg, regstr);
			content.push_back(new Line(line));
			line = std::string(indent, '\t');
			line += "    inter o;";
			content.push_back(new Line(line));
			InsertTemp();
			line = BlockRoutine();
		}
		else if (type == "fp")
		{
			line += "target frag(inter i, bool isFrontFacing : SV_IsFrontFace)";
			content.push_back(new Line(line));
			GetLine(in, line);
			LineMatch(line, match, reg, regstr);
			content.push_back(new Line(line));
			line = std::string(indent, '\t');
			line += "    target o;";
			content.push_back(new Line(line));
			InsertTemp();
			line = BlockRoutine();
		}
	}

	regstr = string("^\\s*\\}\"$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		line.erase(line.length() - 1, 1);

	regstr = string("return;");
	reg = regex(regstr);
	line = regex_replace(line, reg, "return o;");

	regstr = string("ivec(\\d)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "int$1");

	regstr = string("uvec(\\d)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "uint$1");

	regstr = string("bvec(\\d)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "bool$1");

	regstr = string("(vec|mat)(\\d)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "float$2");

	// Doing these both because of some nested things
	regstr = string("(float|int|uint|bool)[2-4]\\(((\\d+(\\.\\d+)?)|([^,.()]+(\\.[xyzwargb])?))\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "$2");
	line = regex_replace(line, reg, "$2");

	regstr = string("^.*\\sin .*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
	{
		ProcessSingleInput(line);
		return false;
	}

	regstr = string("^.*\\sout .*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
	{
		ProcessSingleOutput(line);
		return false;
	}

	regstr = string("^.*\\s[a-z0-9]+\\s+u_xlat.*;$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
	{
		ProcessSingleTemp(line);
		return false;
	}

	regstr = string("^.*\\suniform .*$");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
	{
		ProcessSingleGlobal(line);
		return false;
	}

	regstr = string(".*vs_SV_InstanceID.*");
	reg = regex(regstr);
	if (regex_match(line, match, reg))
		return false;

	regstr = string("vs_[A-Za-z0-9_]+");
	reg = regex(regstr);
	if (type == "vp")
		line = regex_replace(line, reg, "o.$0");
	else if (type == "fp")
		line = regex_replace(line, reg, "i.$0");

	regstr = string("in_[A-Za-z0-9_]+");
	reg = regex(regstr);
	line = regex_replace(line, reg, "i.$0");

	regstr = string("SV_Target\\d");
	reg = regex(regstr);
	line = regex_replace(line, reg, "o.$0");

	regstr = string("unity_Builtins0Array\\.unity_Builtins0Array\\.unity_([A-Za-z]+)Array\\[\\([a-zA-Z0-9_]+ \\+ (\\d)\\)\\]");
	reg = regex(regstr);
	line = regex_replace(line, reg, "unity_$1[$2]");

	regstr = string("unity_Builtins0Array\\.unity_Builtins0Array\\.unity_([A-Za-z]+)Array\\[[a-zA-Z0-9_]+\\]");
	reg = regex(regstr);
	line = regex_replace(line, reg, "unity_$1[0]");

	regstr = string("unity_Builtins\\dArray\\.unity_Builtins\\dArray\\.unity_([A-Za-z]+)Array");
	reg = regex(regstr);
	line = regex_replace(line, reg, "unity_$1");

	regstr = string("(unity_[A-Za-z0-9_]+)\\[(\\d)\\]");
	reg = regex(regstr);
	line = regex_replace(line, reg, "float4($1[0][$2],$1[1][$2],$1[2][$2],$1[3][$2])");

	regstr = string("gl_InstanceID");
	reg = regex(regstr);
	line = regex_replace(line, reg, "0");

	regstr = string("gl_Position");
	reg = regex(regstr);
	line = regex_replace(line, reg, "o.vertex");

	regstr = string("gl_FragCoord");
	reg = regex(regstr);
	line = regex_replace(line, reg, "i.vertex");

	regstr = string("gl_FrontFacing");
	reg = regex(regstr);
	line = regex_replace(line, reg, "isFrontFacing");

	regstr = string("equal\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "(==)(");

	regstr = string("greaterThan\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "(>)(");

	regstr = string("greaterThanEqual\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "(>=)(");

	regstr = string("lessThan\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "(<)(");

	regstr = string("lessThanEqual\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "(<=)(");

	regstr = string("\\(([<>=]=?)\\)\\(u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\), u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\)\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "bool4($2 $1 $6, $3 $1 $7, $4 $1 $8, $5 $1 $9)");

	regstr = string("\\(([<>=]=?)\\)\\(\\(?([^,]*)\\)?, u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\)\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "bool4(($2).x $1 $3, ($2).y $1 $4, ($2).z $1 $5, ($2).w $1 $6)");

	regstr = string("\\(([<>=]=?)\\)\\(u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\), ?\\(?([^,]*)\\)?\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "bool4($2 $1 ($6).x, $3 $1 ($6).y, $4 $1 ($6).z, $5 $1 ($6).w)");

	regstr = string("\\(([<>=]=?)\\)\\(\\(?([^,]*)\\)?, ?([a-z0-9]*\\(?[^()]*\\)?)\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "bool4(($2).x $1 ($3).x, ($2).y $1 ($3).y, ($2).z $1 ($3).z, ($2).w $1 ($3).w)");

	regstr = string("inversesqrt");
	reg = regex(regstr);
	line = regex_replace(line, reg, "rsqrt");

	regstr = string("fract");
	reg = regex(regstr);
	line = regex_replace(line, reg, "frac");

	regstr = string("mix");
	reg = regex(regstr);
	line = regex_replace(line, reg, "lerp");

	regstr = string("roundEven");
	reg = regex(regstr);
	line = regex_replace(line, reg, "round");

	regstr = string("texture\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "tex2D(");

	regstr = string("textureLod\\(([^,]+), ?([^,]+), ([^()]+)?\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "tex2Dlod($1, float4($2, 0.0, $3))");

	regstr = string("textureLodOffset\\(([^,]+), ?([^,]+), ?([^,]+), ?((?:int2\\([^()]+\\))|(?:[^()]+))\\)");
	reg = regex(regstr);
	line = regex_replace(line, reg, "tex2Dlod($1, float4($2 + $4, 0.0, $3))");

	regstr = string("floatBitsToInt\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "asint(");

	regstr = string("floatBitsToUint\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "asuint(");

	regstr = string("u?intBitsToFloat\\(");
	reg = regex(regstr);
	line = regex_replace(line, reg, "asfloat(");

	return true;
}

std::string SubProgramBlock::BlockRoutine(void)
{
	string line("");

	GetLine(in, line);
	while (strchr(line.c_str(), '{') != NULL || strchr(line.c_str(), '}') == NULL)
	{
		if (strchr(line.c_str(), '{') != NULL && strchr(line.c_str(), '}') == NULL)
		{
			if (ProcessBlock(line))
			{
				if (ProcessLine(line))
					content.push_back(new Line(line));
				line = BlockRoutine();
				if (ProcessLine(line))
					content.push_back(new Line(line));
			}
		}
		else if (ProcessLine(line))
			content.push_back(new Line(line));
		GetLine(in, line);
	}
	return line;
}

std::string SubProgramBlock::Run(void)
{
	if (type == "vp")
		output.push_back("float4 vertex : SV_POSITION;");
	return BlockRoutine();
}

std::map<std::string,std::string> SubProgramBlock::GetDeclaredUniforms(void) const
{
	return uniforms;
}

string SubProgramBlock::toString(std::map<std::string,std::string> declaredUniforms) const
{
	string ret("");
	for (auto uni : uniforms)
	{
		if (declaredUniforms.find(uni.first) == declaredUniforms.end())
		{
			ret += string(indent, '\t');
			ret += uni.second + "\n";
		}
	}

	if (type == "vp")
	{
		ret += string(indent, '\t');
		ret += "struct app_in {\n";
		for (auto inp : input)
		{
			ret += string(indent + 1, '\t');
			ret += "    " + inp + "\n";
		}
		ret += string(indent, '\t');
		ret += "};\n";
	}

	ret += string(indent, '\t');
	if (type == "vp")
	{
		ret += "struct inter {\n";
	}
	else if (type == "fp")
		ret += "struct target {\n";
	for (auto out : output)
	{
		ret += string(indent + 1, '\t');
		ret += out + "\n";
	}
	ret += string(indent, '\t');
	ret += "};\n";

	for (IWritable *obj : content)
		ret += obj->toString();
	return ret;
}

string SubProgramBlock::toString(void) const
{
	return this->toString(std::map<std::string,std::string>());
}
