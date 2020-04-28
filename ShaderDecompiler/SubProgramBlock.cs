using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class SubProgramBlock : BlockParser
    {
        public SubProgramBlock(StreamReader @in, string type, int indent) : base(@in, indent)
        {
            this.type = type;

            if (type != "vp" && type != "fp")
            {
                Console.Error.WriteLine($"Unsupported subprogram type {type}");
                throw new Exception("-5");
            }
        }

        protected string type;
        protected Dictionary<string, string> uniforms = new Dictionary<string, string>();
        protected List<string> input = new List<string>();
        protected List<string> output = new List<string>();
        protected List<string> temp = new List<string>();
        protected bool instancing = false;

        string indent(int indent)
        {
            string ret = "";
            for (int i = 0; i < indent; i++)
            {
                ret += "\t";
            }

            return ret;
        }

        public override string toString()
        {
            return this.toString(new Dictionary<string, string>());
        }

        public string toString(Dictionary<string, string> declaredUniforms)
        {
            string ret = "";
            foreach (var uni in uniforms)
            {
                if (!declaredUniforms.ContainsKey(uni.Key))
                {
                    ret += indent(_indent);
                    ret += uni.Value + "\n";
                }
            }

            if (type == "vp")
            {
                ret += indent(_indent);
                ret += "struct app_in {\n";
                foreach (var inp in input)
                {
                    ret += indent(_indent + 1);
                    ret += "    " + inp + "\n";
                }
                ret += indent(_indent);
                ret += "};\n";
            }

            ret += indent(_indent);
            if (type == "vp")
            {
                ret += "struct inter {\n";
            }
            else if (type == "fp")
                ret += "struct target {\n";
            foreach (var outp in output)
            {
                ret += indent(_indent + 1);
                ret += outp + "\n";
            }
            ret += indent(_indent);
            ret += "};\n";

            foreach (var obj in content)
                ret += obj.toString();
            return ret;
        }

        public string Run()
        {
            if (type == "vp")
            {
                output.Add("float4 vertex : SV_POSITION;");
            }

            return BlockRoutine();
        }

        public Dictionary<string, string> GetDeclaredUniforms()
        {
            return uniforms;
        }

        protected string BlockRoutine()
        {
            string line = "";

            GetLine(_input, out line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                if (line.Contains('{') && !line.Contains('}'))
                {
                    if (ProcessBlock(ref line))
                    {
                        if (ProcessLine(ref line))
                            content.Add(new Line(line));
                        line = BlockRoutine();
                        if (ProcessLine(ref line))
                            content.Add(new Line(line));
                    }
                }
                else if (ProcessLine(ref line))
                    content.Add(new Line(line));
                GetLine(_input, out line);
            }
            return line;
        }

        private static readonly string _layoutProcSingleRegexStr =
            "^\\s*(layout\\(location = \\d\\)\\s+)?(uniform\\s+)?([a-zA-Z0-9_]+\\s+([a-zA-Z0-9_]+)(\\[.+\\])?;)$";
        private static readonly Regex _layoutProcSingleRegex = new Regex(_layoutProcSingleRegexStr);
        protected void ProcessSingleGlobal(string line)
        {
            LineMatch(line, out var match, _layoutProcSingleRegex, _layoutProcSingleRegexStr);
            string tmp = "";
            if (match.Groups[2].Value == "")
            {
                tmp += "uniform ";
                tmp += match.Groups[3].Value;
            }
            else
            {
                tmp += match.Groups[2].Value;
                tmp += match.Groups[3].Value;
            }
            if (ProcessVarLine(ref tmp))
                uniforms.Add(match.Groups[4].Value,tmp);
        }

        private static readonly string _procSingleInRegexStr =
            "^\\s*(layout\\(location = \\d\\)\\s+)?(in\\s+)([a-zA-Z0-9_]+\\s+(in_|vs_)(POSITION|NORMAL|[a-zA-Z0-9_]+)\\d*);$";
        private static readonly Regex _procSingleInRegex = new Regex(_procSingleInRegexStr);
        protected void ProcessSingleInput(string line)
        {
            LineMatch(line, out _, _procSingleInRegex, _procSingleInRegexStr);

            string tmp = _procSingleInRegex.Replace(line, "$3 : $5;");
            input.Add(tmp);
        }

        private static readonly string _procSingleOutRegexStr = "^(\\s*)(layout\\(location = \\d\\)\\s+)?(flat\\s+)?(out\\s+)([a-zA-Z0-9_]+\\s+(vs_)?([a-zA-Z0-9_]+));$";
        private static readonly Regex _procSingleOutRegex = new Regex(_procSingleOutRegexStr);

        protected void ProcessSingleOutput(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^(\\s*)(flat out\\s+)([a-z0-9_]+\\s+vs_SV_InstanceID\\d*);$";

            if (Regex.IsMatch(line, regstr))
            {
                input.Add("UNITY_VERTEX_INPUT_INSTANCE_ID");
                instancing = true;
                return;
            }

            LineMatch(line, out match, _procSingleOutRegex, _procSingleOutRegexStr);
            string tmp = _procSingleOutRegex.Replace(line, "$5 : $7;");
            output.Add(tmp);
        }

        protected void ProcessSingleTemp(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^(\\s*)[a-z0-9_]+\\s+u_xlat[a-z0-9_]*;$";

            LineMatch(line, out match, regstr);
            temp.Add(line);
        }

        private bool ProcessLine(ref string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = ("^\\s*#(version|extension).*$");
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = ("^\\s*void main\\(\\)$");
            if (Regex.IsMatch(line,regstr))
            {
                regstr = ("^\\s*\\{$");

                line = indent(_indent);
                if (type == "vp")
                {
                    line += "inter vert(app_in i)";
                    content.Add(new Line(line));
                    GetLine(_input, out line);
                    LineMatch(line, out match, regstr);
                    content.Add(new Line(line));
                    line = indent(_indent);
                    line += "    inter o;";
                    content.Add(new Line(line));
                    InsertTemp();
                    line = BlockRoutine();
                }
                else if (type == "fp")
                {
                    line += "target frag(inter i, bool isFrontFacing : SV_IsFrontFace)";
                    content.Add(new Line(line));
                    GetLine(_input, out line);
                    LineMatch(line, out match, regstr);
                    content.Add(new Line(line));
                    line = indent(_indent);
                    line += "    target o;";
                    content.Add(new Line(line));
                    InsertTemp();
                    line = BlockRoutine();
                }
            }

            regstr = ("^\\s*\\}\"$");
            if (Regex.IsMatch(line, regstr))
                line = line.Remove(line.Length - 1, 1);

            regstr = ("return;");
            line = Regex.Replace(line, regstr, "return o;");

            regstr = ("ivec(\\d)");
            line = Regex.Replace(line, regstr, "int$1");

            regstr = ("uvec(\\d)");
            line = Regex.Replace(line, regstr, "uint$1");

            regstr = ("bvec(\\d)");
            line = Regex.Replace(line, regstr, "bool$1");

            regstr = ("(vec|mat)(\\d)");
            line = Regex.Replace(line, regstr, "float$2");

            // Doing these both because of some nested things
            regstr = ("(float|int|uint|bool)[2-4]\\(((\\d+(\\.\\d+)?)|([^,.()]+(\\.[xyzwargb])?))\\)");
            line = Regex.Replace(line, regstr, "$2");
            line = Regex.Replace(line, regstr, "$2");

            regstr = ("^.*\\sin .*$");
            if (Regex.IsMatch(line, regstr))
            {
                ProcessSingleInput(line);
                return false;
            }

            regstr = ("^.*\\sout .*$");
            if (Regex.IsMatch(line, regstr))
            {
                ProcessSingleOutput(line);
                return false;
            }

            regstr = ("^.*\\s[a-z0-9]+\\s+u_xlat.*;$");
            if (Regex.IsMatch(line, regstr))
            {
                ProcessSingleTemp(line);
                return false;
            }

            regstr = ("^.*\\suniform .*$");
            if (Regex.IsMatch(line, regstr))
            {
                ProcessSingleGlobal(line);
                return false;
            }

            regstr = (".*vs_SV_InstanceID.*");
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = ("vs_[A-Za-z0-9_]+");
            if (type == "vp")
                line = Regex.Replace(line, regstr, "o.$0");
            else if (type == "fp")
                line = Regex.Replace(line, regstr, "i.$0");

            regstr = ("in_[A-Za-z0-9_]+");
            line = Regex.Replace(line, regstr, "i.$0");

            regstr = ("SV_Target\\d");
            line = Regex.Replace(line, regstr, "o.$0");

            regstr = ("unity_Builtins0Array\\.unity_Builtins0Array\\.unity_([A-Za-z]+)Array\\[\\([a-zA-Z0-9_]+ \\+ (\\d)\\)\\]");
            line = Regex.Replace(line, regstr, "unity_$1[$2]");

            regstr = ("unity_Builtins0Array\\.unity_Builtins0Array\\.unity_([A-Za-z]+)Array\\[[a-zA-Z0-9_]+\\]");
            line = Regex.Replace(line, regstr, "unity_$1[0]");

            regstr = ("unity_Builtins\\dArray\\.unity_Builtins\\dArray\\.unity_([A-Za-z]+)Array");
            line = Regex.Replace(line, regstr, "unity_$1");

            regstr = ("(unity_[A-Za-z0-9_]+)\\[(\\d)\\]");
            line = Regex.Replace(line, regstr, "float4($1[0][$2],$1[1][$2],$1[2][$2],$1[3][$2])");

            regstr = ("gl_InstanceID");
            line = Regex.Replace(line, regstr, "0");

            regstr = ("gl_Position");
            line = Regex.Replace(line, regstr, "o.vertex");

            regstr = ("gl_FragCoord");
            line = Regex.Replace(line, regstr, "i.vertex");

            regstr = ("gl_FrontFacing");
            line = Regex.Replace(line, regstr, "isFrontFacing");

            regstr = ("equal\\(");
            line = Regex.Replace(line, regstr, "(==)(");

            regstr = ("greaterThan\\(");
            line = Regex.Replace(line, regstr, "(>)(");

            regstr = ("greaterThanEqual\\(");
            line = Regex.Replace(line, regstr, "(>=)(");

            regstr = ("lessThan\\(");
            line = Regex.Replace(line, regstr, "(<)(");

            regstr = ("lessThanEqual\\(");
            line = Regex.Replace(line, regstr, "(<=)(");

            regstr = ("\\(([<>=]=?)\\)\\(u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\), u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\)\\)");
            line = Regex.Replace(line, regstr, "bool4($2 $1 $6, $3 $1 $7, $4 $1 $8, $5 $1 $9)");

            regstr = ("\\(([<>=]=?)\\)\\(\\(?([^,]*)\\)?, u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\)\\)");
            line = Regex.Replace(line, regstr, "bool4(($2).x $1 $3, ($2).y $1 $4, ($2).z $1 $5, ($2).w $1 $6)");

            regstr = ("\\(([<>=]=?)\\)\\(u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\), ?\\(?([^,]*)\\)?\\)");
            line = Regex.Replace(line, regstr, "bool4($2 $1 ($6).x, $3 $1 ($6).y, $4 $1 ($6).z, $5 $1 ($6).w)");

            regstr = ("\\(([<>=]=?)\\)\\(\\(?([^,]*)\\)?, ?([a-z0-9]*\\(?[^()]*\\)?)\\)");
            line = Regex.Replace(line, regstr, "bool4(($2).x $1 ($3).x, ($2).y $1 ($3).y, ($2).z $1 ($3).z, ($2).w $1 ($3).w)");

            regstr = ("inversesqrt");
            line = Regex.Replace(line, regstr, "rsqrt");

            regstr = ("fract");
            line = Regex.Replace(line, regstr, "frac");

            regstr = ("mix");
            line = Regex.Replace(line, regstr, "lerp");

            regstr = ("roundEven");
            line = Regex.Replace(line, regstr, "round");

            regstr = ("texture\\(");
            line = Regex.Replace(line, regstr, "tex2D(");

            regstr = ("textureLod\\(([^,]+), ?([^,]+), ([^()]+)?\\)");
            line = Regex.Replace(line, regstr, "tex2Dlod($1, float4($2, 0.0, $3))");

            regstr = ("textureLodOffset\\(([^,]+), ?([^,]+), ?([^,]+), ?((?:int2\\([^()]+\\))|(?:[^()]+))\\)");
            line = Regex.Replace(line, regstr, "tex2Dlod($1, float4($2 + $4, 0.0, $3))");

            regstr = ("floatBitsToInt\\(");
            line = Regex.Replace(line, regstr, "asint(");

            regstr = ("floatBitsToUint\\(");
            line = Regex.Replace(line, regstr, "asuint(");

            regstr = ("u?intBitsToFloat\\(");
            line = Regex.Replace(line, regstr, "asfloat(");

            return true;
        }

        private bool ProcessVarLine(ref string line)
        {
            string regstr;

            /* Apparently unused variables ARE used
            regstr = ("^.* unused.*$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;
                */

            // I'm not sure anymore if this is needed or not
            regstr = "^.* unity_([^P]|P[^r]|Pr[^o]|Pro[^j]).*$";
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = "^.* _ZBufferParams.*$";
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = "^.* _Time.*$";
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = "^.* _ProjectionParams.*$";
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = "^.* _ScreenParams.*$";
            if (Regex.IsMatch(line, regstr))
                return false;

            regstr = "(vec|mat)(\\d)";
            line = Regex.Replace(line, regstr, "float$2");

            regstr = "sampler2DArray";
            line = Regex.Replace(line, regstr, "sampler2D[]");

            return true;
        }

        private bool ProcessBlock(ref string line)
        {
            string regstr;
            Match match;

            regstr = "^\\s*layout\\([^)]*\\) uniform (.*) \\{$";

            match = Regex.Match(line, regstr);
            if (match.Success)
            {
                if (match.Groups[1].Value == "VGlobals" || match.Groups[1].Value == "PGlobals")
                {
                    line = "";
                    GetLine(_input, out line);
                    while (line.Contains('{') || !line.Contains('}'))
                    {
                        ProcessSingleGlobal(line);
                        GetLine(_input, out line);
                    }
                }
                else
                {
                    BlockParser del = new BlockParser(_input);
                    del.Run();
                }
                return false;
            }

            regstr = "^\\s*struct unity_.* \\{$";
            if (Regex.IsMatch(line, regstr))
            {
                BlockParser del = new BlockParser(_input);
                del.Run();
                return false;
            }

            return true;
        }

        private void InsertTemp()
        {
            if (instancing)
            {
                content.Add(new Line("UNITY_SETUP_INSTANCE_ID(i);"));
            }

            foreach (var tmp in temp)
            {
                content.Add(new Line(tmp));    
            }
        }
    }
}