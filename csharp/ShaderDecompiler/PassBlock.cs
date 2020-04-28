using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class PassBlock : BlockParser
    {
        private StreamReader @in;
        private int _indent;

        public PassBlock(StreamReader @in, int indent) : base(@in, indent)
        {
            this.@in = @in;
            this._indent = indent;
        }

        protected Dictionary<string, SubProgramBlock> vertSubs = new Dictionary<string, SubProgramBlock>(); 
        protected Dictionary<string, SubProgramBlock> fragSibs = new Dictionary<string, SubProgramBlock>();
        protected Dictionary<string, int> keywords_vars = new Dictionary<string, int>();
        protected Dictionary<string, int> keywords = new Dictionary<string, int>();
        protected Dictionary<string, int> vertkeywords = new Dictionary<string, int>();
        protected Dictionary<string, int> fragkeywords = new Dictionary<string, int>();

        private string src_blend = string.Empty;
        private string dst_blend = string.Empty;
        private string cull = string.Empty;

        public override string toString()
        {
            string ret = "";
            ret += base.toString();
            ret += makeProgram();
            return ret;
        }


        internal void Blend(string src_blend, string dst_blend)
        {
            this.src_blend = src_blend;
            this.dst_blend = dst_blend;
        }

        internal void Cull(string cull)
        {
            this.cull = cull;
        }

        internal string Run()
        {
            string regstr;
            Regex reg;
            Match match;

            string line = "";

            regstr = "^\t{" + _indent + "}Program \"([a-z]+)\" \\{$";
            reg = new Regex(regstr);

            string stregstr = "^\t{" + _indent + "}Stencil \\{$";
            Regex streg = new Regex(stregstr);

            GetLine(@in, out line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                if (line.Contains('{') && !line.Contains('}'))
                {
                    match = reg.Match(line);
                    if (match.Success)
                    {
                        string lastline = ProgramRoutine(match.Groups[1].Value);
                    }
                    else if (streg.IsMatch(line))
                    {
                        content.Add(new Line(line));
                        line = StencilRoutine();
                        content.Add(new Line(line));
                    }
                    else
                    {
                        content.Add(new Line(line));
                        line = BlockRoutine();
                        content.Add(new Line(line));
                    }
                }
                else if (ProcessLine(line))
                    content.Add(new Line(line));

                GetLine(@in, out line);
            }

            return line;
        }

        bool ProcessLine(string line)
        {
            string regstr;
            Regex reg;
            Match match;

            regstr = "^\t{" + _indent + "}GpuProgramID \\d+$";
            reg = new Regex(regstr);

            match = reg.Match(line);
            if (match.Success)
                return false;

            if (src_blend != "" && dst_blend != "")
            {
                regstr = "^(\t{" + _indent + "}Blend) .*$";
                reg = new Regex(regstr);

                match = reg.Match(line);
                if (match.Success)
                    line = reg.Replace(line, "$1 [" + src_blend + "] [" + dst_blend + "], [" + src_blend + "] [" + dst_blend + "]");
            }

            if (cull != "")
            {
                regstr = "^(\t{" + _indent + "}Cull) .*$";
                reg = new Regex(regstr);

                match = reg.Match(line);
                if (match.Success)
                    line = reg.Replace(line, "$1 [_Cull]");
            }

            return true;
        }

        string ProgramRoutine(string programType)
        {
            if (programType != "vp" && programType != "fp")
            {
                Console.Error.WriteLine($"Unsupported program type: {programType}");
                throw new Exception("-5");
            }
            
            string regstr = "^\\s*SubProgram \"\\s*([a-z0-9]+)\\s*\" \\{$";
            var reg = new Regex(regstr);

            string keyregstr = "^\\s*Keywords \\{ (\"[A-Z0-9_]+\" )+\\}$";
            Regex keyreg = new Regex(keyregstr);

            string keywordregstr = "\"([A-Z0-9_]+)\""; 
            Regex keywordreg = new Regex(keywordregstr);

            string ptyperegstr = "^\\s*\"(!!)?[a-z0-9_]+$";
            Regex ptypereg = new Regex(ptyperegstr);

            Match match;
            string line = "";
            GetLine(@in, out line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                LineMatch(line, out match, reg, regstr);
                if (match.Groups[1].Value != "d3d11")
                {
                    Console.Error.WriteLine($"Unsupported subprogram type: {match.Groups[1].Value}");
                    throw new Exception("-5");
                }

                GetLine(@in, out line);
                string keywords_str = " ";
                match = keyreg.Match(line);
                if (match.Success)
                {
                    //possible wrong conversion
                    match = keywordreg.Match(line);
                    while (match.Success)
                    {
                        keywords_str += match.Groups[1].Value;
                        keywords_str += " ";
                        if (!keywords.ContainsKey(match.Groups[1].Value))
                            keywords.Add(match.Groups[1].Value,0);
                        if (programType == "vp" && !vertkeywords.ContainsKey(match.Groups[1].Value))
                            vertkeywords.Add(match.Groups[1].Value,0);

                        else if (programType == "fp" && !fragkeywords.ContainsKey(match.Groups[1].Value))
                            fragkeywords.Add(match.Groups[1].Value,0);
                        line = match.Groups[match.Groups.Count - 1].Value;
                        match = match.NextMatch();
                    }

                    GetLine(@in, out line);
                }

                LineMatch(line, out match, ptypereg, ptyperegstr);

                if (keywords_vars.ContainsKey(keywords_str))
                    keywords_vars.Add(keywords_str,0);
                SubProgramBlock subprogram = new SubProgramBlock(@in, programType, _indent + 2);
                if (programType == "vp")
                    vertSubs.Add(keywords_str, subprogram);
                else if (programType == "fp")
                    fragSibs.Add(keywords_str, subprogram);
                line = subprogram.Run();

                GetLine(@in, out line);
            }

            return line;
        }

        string StencilRoutine()
        {
            string compregstr = "^(\\s*Comp) Disabled$";
            Regex compreg = new Regex(compregstr);

            Match match;
            string line = "";
            GetLine(@in, out line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                match = compreg.Match(line);
                if (match.Success)
                {
                    line = match.Groups[1].Value;
                    line += " Never";
                }
                content.Add(new Line(line));

                GetLine(@in, out line);
            }

            return line;
        }

        string indent(int indent)
        {
            string ret = "";
            for (int i = 0; i < indent; i++)
            {
                ret += "\t";
            }

            return ret;
        }

        string makeProgram()
        {
            string ret = "";
            ret += indent(_indent) + "CGPROGRAM\n\n";
            ret += "#include \"UnityCG.cginc\"\n\n";
            ret += "#pragma vertex vert\n";
            ret += "#pragma fragment frag\n\n";

            Dictionary<string,int> keywords_cpy = new Dictionary<string, int>(keywords);
            List<Dictionary<string, int>> variants = new List<Dictionary<string, int>>();
            foreach (var obj in keywords_cpy)
            {
                if (obj.Value == 0)
                {
                    /* Probably stupid and doesn't work on some edge-cases */ //from original code
                    Dictionary<string, int> alts = new Dictionary<string, int>(keywords_cpy);
                    foreach (var prog in keywords_vars)
                    {
                        Dictionary<string, int> tmp = new Dictionary<string, int>(alts);
                        if (prog.Key.Contains(obj.Key))
                        {
                            foreach (var alt in tmp)
                            {
                                if (alt.Key != obj.Key && prog.Key.Contains(" " + alt.Key + " "))
                                {
                                    alts.Remove(alt.Key);
                                }
                            }
                        }
                    }


                    //something is broken here
                    foreach (var prog in keywords_vars)
                    {
                        foreach (var alt in alts)
                        {
                            if (prog.Key.Contains(" " + alt.Key + " "))
                            {
                                goto outofloop;
                            }
                        }

                        alts.Add("__", 0);
                        break;
                    outofloop:
                        continue;
                    }

                    variants.Add(alts);
                }
            }

	        foreach (var prag in variants)
	        {
                foreach (var alt in prag)
                {
                    keywords_cpy[alt.Key] = 1;
                }


                ret += "#pragma multi_compile_local";
		        foreach (var vart in prag)
		        {
			        ret += " ";
			        ret += vart.Key;
		        }
		        ret += "\n";
            }

	        foreach (var vert in vertSubs)
	        {
		        ret += "\n#if 1";
		        string keys = vert.Key;
		        foreach (var key in vertkeywords)
                {
			        if (!keys.Contains(" " + key.Key + " "))
				        ret += " && !defined (" + key.Key + ")";
			        else
				        ret += " && defined (" + key.Key + ")";
		        }
		        ret += "\n\n";
		        ret += vert.Value.toString();
                ret += "\n#endif\n";
            }

	        foreach (var frag in fragSibs)
	        {
		        ret += "\n#if 1";
		        string keys = frag.Key;
                var vert = vertSubs.ContainsKey(keys);
                if (vert)
		        {
			        foreach (var key in fragkeywords)
			        {
				        if (!keys.Contains(" " + key.Key + " "))
					        ret += " && !defined (" + key.Key + ")";
				        else
					        ret += " && defined (" + key.Key + ")";
			        }
			        ret += "\n\n";
			        ret += frag.Value.toString(vertSubs[keys].GetDeclaredUniforms());
                    ret += "\n#endif\n";
		        }
		        else
		        {
			        foreach (var key in fragkeywords)
			        {
				        if (!keys.Contains(" " + key.Key + " "))
					        ret += " && !defined (" + key.Key + ")";
				        else
					        ret += " && defined (" + key.Key + ")";
			        }
			        ret += "\n\n";
			        ret += frag.Value.toString();
                    ret += "\n#endif\n";
		        }
	        }
            ret += indent(_indent) + "ENDCG\n";
	        return ret;
        }
    }
}