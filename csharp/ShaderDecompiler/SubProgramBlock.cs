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

        protected void ProcessSingleGlobal(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^\\s*(layout\\(location = \\d\\)\\s+)?(uniform\\s+)?([a-zA-Z0-9_]+\\s+([a-zA-Z0-9_]+)(\\[.+\\])?;)$";
            reg = new Regex(regstr);

            LineMatch(line, out match, reg, regstr);
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

        protected void ProcessSingleInput(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^\\s*(layout\\(location = \\d\\)\\s+)?(in\\s+)([a-zA-Z0-9_]+\\s+(in_|vs_)(POSITION|NORMAL|[a-zA-Z0-9_]+)\\d*);$";
            reg = new Regex(regstr);

            LineMatch(line, out match, reg, regstr);

            string tmp = reg.Replace(line, "$3 : $5;");
            input.Add(tmp);
        }

        protected void ProcessSingleOutput(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^(\\s*)(flat out\\s+)([a-z0-9_]+\\s+vs_SV_InstanceID\\d*);$";
            reg = new Regex(regstr);

            if (reg.IsMatch(line))
            {
                input.Add("UNITY_VERTEX_INPUT_INSTANCE_ID");
                instancing = true;
                return;
            }

            regstr = "^(\\s*)(layout\\(location = \\d\\)\\s+)?(flat\\s+)?(out\\s+)([a-zA-Z0-9_]+\\s+(vs_)?([a-zA-Z0-9_]+));$";
            reg = new Regex(regstr);

            LineMatch(line, out match, reg, regstr);
            string tmp = reg.Replace(line, "$5 : $7;");
            output.Add(tmp);
        }

        protected void ProcessSingleTemp(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^(\\s*)[a-z0-9_]+\\s+u_xlat[a-z0-9_]*;$";
            reg = new Regex(regstr);

            LineMatch(line, out match, reg, regstr);
            temp.Add(line);
        }

        private bool ProcessLine(ref string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = ("^\\s*#(version|extension).*$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = ("^\\s*void main\\(\\)$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
            {
                regstr = ("^\\s*\\{$");
                reg = new Regex(regstr);

                line = indent(_indent);
                if (type == "vp")
                {
                    line += "inter vert(app_in i)";
                    content.Add(new Line(line));
                    GetLine(_input, out line);
                    LineMatch(line, out match, reg, regstr);
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
                    LineMatch(line, out match, reg, regstr);
                    content.Add(new Line(line));
                    line = indent(_indent);
                    line += "    target o;";
                    content.Add(new Line(line));
                    InsertTemp();
                    line = BlockRoutine();
                }
            }

            regstr = ("^\\s*\\}\"$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                line = line.Remove(line.Length - 1, 1);

            regstr = ("return;");
            reg = new Regex(regstr);
            line = reg.Replace(line, "return o;");

            regstr = ("ivec(\\d)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "int$1");

            regstr = ("uvec(\\d)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "uint$1");

            regstr = ("bvec(\\d)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "bool$1");

            regstr = ("(vec|mat)(\\d)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "float$2");

            // Doing these both because of some nested things
            regstr = ("(float|int|uint|bool)[2-4]\\(((\\d+(\\.\\d+)?)|([^,.()]+(\\.[xyzwargb])?))\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "$2");
            line = reg.Replace(line, "$2");

            regstr = ("^.*\\sin .*$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
            {
                ProcessSingleInput(line);
                return false;
            }

            regstr = ("^.*\\sout .*$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
            {
                ProcessSingleOutput(line);
                return false;
            }

            regstr = ("^.*\\s[a-z0-9]+\\s+u_xlat.*;$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
            {
                ProcessSingleTemp(line);
                return false;
            }

            regstr = ("^.*\\suniform .*$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
            {
                ProcessSingleGlobal(line);
                return false;
            }

            regstr = (".*vs_SV_InstanceID.*");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = ("vs_[A-Za-z0-9_]+");
            reg = new Regex(regstr);
            if (type == "vp")
                line = reg.Replace(line, "o.$0");
            else if (type == "fp")
                line = reg.Replace(line, "i.$0");

            regstr = ("in_[A-Za-z0-9_]+");
            reg = new Regex(regstr);
            line = reg.Replace(line, "i.$0");

            regstr = ("SV_Target\\d");
            reg = new Regex(regstr);
            line = reg.Replace(line, "o.$0");

            regstr = ("unity_Builtins0Array\\.unity_Builtins0Array\\.unity_([A-Za-z]+)Array\\[\\([a-zA-Z0-9_]+ \\+ (\\d)\\)\\]");
            reg = new Regex(regstr);
            line = reg.Replace(line, "unity_$1[$2]");

            regstr = ("unity_Builtins0Array\\.unity_Builtins0Array\\.unity_([A-Za-z]+)Array\\[[a-zA-Z0-9_]+\\]");
            reg = new Regex(regstr);
            line = reg.Replace(line, "unity_$1[0]");

            regstr = ("unity_Builtins\\dArray\\.unity_Builtins\\dArray\\.unity_([A-Za-z]+)Array");
            reg = new Regex(regstr);
            line = reg.Replace(line, "unity_$1");

            regstr = ("(unity_[A-Za-z0-9_]+)\\[(\\d)\\]");
            reg = new Regex(regstr);
            line = reg.Replace(line, "float4($1[0][$2],$1[1][$2],$1[2][$2],$1[3][$2])");

            regstr = ("gl_InstanceID");
            reg = new Regex(regstr);
            line = reg.Replace(line, "0");

            regstr = ("gl_Position");
            reg = new Regex(regstr);
            line = reg.Replace(line, "o.vertex");

            regstr = ("gl_FragCoord");
            reg = new Regex(regstr);
            line = reg.Replace(line, "i.vertex");

            regstr = ("gl_FrontFacing");
            reg = new Regex(regstr);
            line = reg.Replace(line, "isFrontFacing");

            regstr = ("equal\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "(==)(");

            regstr = ("greaterThan\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "(>)(");

            regstr = ("greaterThanEqual\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "(>=)(");

            regstr = ("lessThan\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "(<)(");

            regstr = ("lessThanEqual\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "(<=)(");

            regstr = ("\\(([<>=]=?)\\)\\(u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\), u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\)\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "bool4($2 $1 $6, $3 $1 $7, $4 $1 $8, $5 $1 $9)");

            regstr = ("\\(([<>=]=?)\\)\\(\\(?([^,]*)\\)?, u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\)\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "bool4(($2).x $1 $3, ($2).y $1 $4, ($2).z $1 $5, ($2).w $1 $6)");

            regstr = ("\\(([<>=]=?)\\)\\(u?(?:float|int)4\\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+)\\), ?\\(?([^,]*)\\)?\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "bool4($2 $1 ($6).x, $3 $1 ($6).y, $4 $1 ($6).z, $5 $1 ($6).w)");

            regstr = ("\\(([<>=]=?)\\)\\(\\(?([^,]*)\\)?, ?([a-z0-9]*\\(?[^()]*\\)?)\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "bool4(($2).x $1 ($3).x, ($2).y $1 ($3).y, ($2).z $1 ($3).z, ($2).w $1 ($3).w)");

            regstr = ("inversesqrt");
            reg = new Regex(regstr);
            line = reg.Replace(line, "rsqrt");

            regstr = ("fract");
            reg = new Regex(regstr);
            line = reg.Replace(line, "frac");

            regstr = ("mix");
            reg = new Regex(regstr);
            line = reg.Replace(line, "lerp");

            regstr = ("roundEven");
            reg = new Regex(regstr);
            line = reg.Replace(line, "round");

            regstr = ("texture\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "tex2D(");

            regstr = ("textureLod\\(([^,]+), ?([^,]+), ([^()]+)?\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "tex2Dlod($1, float4($2, 0.0, $3))");

            regstr = ("textureLodOffset\\(([^,]+), ?([^,]+), ?([^,]+), ?((?:int2\\([^()]+\\))|(?:[^()]+))\\)");
            reg = new Regex(regstr);
            line = reg.Replace(line, "tex2Dlod($1, float4($2 + $4, 0.0, $3))");

            regstr = ("floatBitsToInt\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "asint(");

            regstr = ("floatBitsToUint\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "asuint(");

            regstr = ("u?intBitsToFloat\\(");
            reg = new Regex(regstr);
            line = reg.Replace(line, "asfloat(");

            return true;
        }

        private bool ProcessVarLine(ref string line)
        {
            string regstr;
            Regex reg;
            Match match;

            /* Apparently unused variables ARE used
            regstr = ("^.* unused.*$");
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;
                */

            // I'm not sure anymore if this is needed or not
            regstr = "^.* unity_([^P]|P[^r]|Pr[^o]|Pro[^j]).*$";
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = "^.* _ZBufferParams.*$";
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = "^.* _Time.*$";
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = "^.* _ProjectionParams.*$";
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = "^.* _ScreenParams.*$";
            reg = new Regex(regstr);
            if (reg.IsMatch(line))
                return false;

            regstr = "(vec|mat)(\\d)";
            reg = new Regex(regstr);
            line = reg.Replace(line, "float$2");

            regstr = "sampler2DArray";
            reg = new Regex(regstr);
            line = reg.Replace(line, "sampler2D[]");

            return true;
        }

        private bool ProcessBlock(ref string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^\\s*layout\\([^)]*\\) uniform (.*) \\{$";
            reg = new Regex(regstr);

            match = reg.Match(line);
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
            reg = new Regex(regstr);


            if (reg.IsMatch(line))
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