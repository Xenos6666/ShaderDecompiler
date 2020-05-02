using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class PassBlock : BlockParser
    {
        private StreamReader @in;
        private const int _indent = 3;

        public PassBlock(StreamReader @in) : base(@in, _indent)
        {
            this.@in = @in;
        }

        protected Dictionary<string, SubProgramBlock> vertSubs = new Dictionary<string, SubProgramBlock>(); 
        protected Dictionary<string, SubProgramBlock> fragSubs = new Dictionary<string, SubProgramBlock>();
        protected List<string> keywords = new List<string>();
        protected List<string> vertkeywords = new List<string>();
        protected List<string> fragkeywords = new List<string>();

        private string _srcBlend = string.Empty;
        private string _dstBlend = string.Empty;
        private string _cull = string.Empty;

        public override string toString()
        {
            var ret = base.toString();
            ret += makeProgram();
            return ret;
        }

        internal void Blend(string src_blend, string dst_blend)
        {
            this._srcBlend = src_blend;
            this._dstBlend = dst_blend;
        }

        internal void Cull(string cull)
        {
            this._cull = cull;
        }

        private static readonly string _programRunRegexStr = "^\t{" + _indent + "}Program \"([a-z]+)\" \\{$";
        private static readonly Regex _programRunRegex = new Regex(_programRunRegexStr);
        private static readonly string _stencilRunRegexStr = "^\t{" + _indent + "}Stencil \\{$";
        private static readonly Regex _stencilRunRegex = new Regex(_stencilRunRegexStr);
        internal string Run()
        {
            GetLine(@in, out var line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                if (line.Contains('{') && !line.Contains('}'))
                {
                    var match = _programRunRegex.Match(line);
                    if (match.Success)
                    {
                        _ = ProgramRoutine(match.Groups[1].Value);
                    }
                    else if (_stencilRunRegex.IsMatch(line))
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
                else if (ProcessLine(ref line))
                {
                    content.Add(new Line(line));
                }

                GetLine(@in, out line);
            }

            return line;
        }

        private static readonly string _idProcRegexStr = "^\t{" + _indent + "}GpuProgramID \\d+$";
        private static readonly Regex _idProcRegex = new Regex(_idProcRegexStr);
        private static readonly string _blendProcRegexStr = "^(\t{" + _indent + "}Blend) .*$";
        private static readonly Regex _blendProcRegex = new Regex(_blendProcRegexStr);
        private static readonly string _cullProcRegexStr = "^(\t{" + _indent + "}Cull) .*$";
        private static readonly Regex _cullProcRegex = new Regex(_cullProcRegexStr);
        bool ProcessLine(ref string line)
        {
            if (_idProcRegex.IsMatch(line))
                return false;

            if (_srcBlend != "" && _dstBlend != "")
            {
                if (_blendProcRegex.IsMatch(line))
                {
                    line = _blendProcRegex.Replace(line, "$1 [" + _srcBlend + "] [" + _dstBlend + "], [" + _srcBlend + "] [" + _dstBlend + "]");
                }
            }

            if (_cull != "")
            {
                if (_cullProcRegex.IsMatch(line))
                {
                    line = _cullProcRegex.Replace(line, "$1 [_Cull]");
                }
            }

            return true;
        }


        private static readonly string _subProgRegexStr = "^\\s*SubProgram \"\\s*([a-z0-9]+)\\s*\" \\{$";
        private static readonly Regex _subProgRegex = new Regex(_subProgRegexStr);
        private static readonly string _keyProgRegexStr = "^\\s*Keywords \\{ (\"[A-Z0-9_]+\" )+\\}$";
        private static readonly Regex _keyProgRegex = new Regex(_keyProgRegexStr);
        private static readonly string _keywordProgRegexStr = "\"([A-Z0-9_]+)\"";
        private static readonly Regex _keywordProgRegex = new Regex(_keywordProgRegexStr);
        private static readonly string _ptypeProgRegexStr = "^\\s*\"(!!)?[a-z0-9_]+$";
        private static readonly Regex _ptypeProgRegex = new Regex(_ptypeProgRegexStr);
        string ProgramRoutine(string programType)
        {
            if (programType != "vp" && programType != "fp")
            {
                Console.Error.WriteLine($"Unsupported program type: {programType}");
                throw new Exception("-5");
            }

            GetLine(@in, out var line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                Match match;
                LineMatch(line, out match, _subProgRegexStr);
                if (match.Groups[1].Value != "d3d11")
                {
                    Console.Error.WriteLine($"Unsupported subprogram type: {match.Groups[1].Value}");
                    throw new Exception("-5");
                }

                GetLine(@in, out line);
                string keywords_str = " ";
                match = _keyProgRegex.Match(line);
                if (match.Success)
                {
                    //possible wrong conversion expect to refactor this
                    match = _keywordProgRegex.Match(line);
                    while (match.Success)
                    {
                        keywords_str += match.Groups[1].Value;
                        keywords_str += " ";

                        if (!keywords.Contains(match.Groups[1].Value))
                            keywords.Add(match.Groups[1].Value);

                        if (programType == "vp" && !vertkeywords.Contains(match.Groups[1].Value))
                            vertkeywords.Add(match.Groups[1].Value);
                        else if (programType == "fp" && !fragkeywords.Contains(match.Groups[1].Value))
                            fragkeywords.Add(match.Groups[1].Value);

                        line = match.Groups[match.Groups.Count - 1].Value;
                        match = match.NextMatch();
                    }

                    GetLine(@in, out line);
                }

                LineMatch(line, out match, _ptypeProgRegex, _ptypeProgRegexStr);

                SubProgramBlock subprogram = new SubProgramBlock(@in, programType, _indent + 2);
                if (programType == "vp")
                    vertSubs.Add(keywords_str, subprogram);
                else if (programType == "fp")
                    fragSubs.Add(keywords_str, subprogram);
                line = subprogram.Run();

                GetLine(@in, out line);
            }

            return line;
        }

        private static readonly string _compStenRegexStr = "^(\\s*Comp) Disabled$";
        private static readonly Regex _compStenRegex = new Regex(_compStenRegexStr);
        string StencilRoutine()
        {
            GetLine(@in, out var line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                var match = _compStenRegex.Match(line);
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
            StringBuilder ret = new StringBuilder("");
            ret.Append(indent(_indent) + "CGPROGRAM\n\n");
            ret.Append("#include \"UnityCG.cginc\"\n\n");
            ret.Append("#pragma vertex vert\n");
            ret.Append("#pragma fragment frag\n\n");

            List<string> used_keywords = new List<string>();
            List<KeyValuePair<List<string>,List<string>>> variants = new List<KeyValuePair<List<string>,List<string>>>();
            foreach (string currentKeyword in keywords)
            {
                if (!used_keywords.Contains(currentKeyword))
                {
                    List<string> alts = new List<string>(keywords);
                    if (vertkeywords.Contains(currentKeyword))
                    {
                        foreach (var prog in vertSubs)
                        {
                            var variantKeywords = prog.Key;
                            List<string> tmp = new List<string>(alts);
                            if (variantKeywords.Contains(" " + currentKeyword + " "))
                                foreach (string alt in tmp)
                                    if (alt != currentKeyword && variantKeywords.Contains(" " + alt + " "))
                                        alts.Remove(alt);
                        }

                        foreach (var prog in vertSubs)
                        {
                            var variantKeywords = prog.Key;
                            bool off = true;
                            foreach (var alt in alts)
                            {
                                if (variantKeywords.Contains(" " + alt + " "))
                                {
                                    off = false;
                                    break ;
                                }
                            }

                            if (!off)
                                continue ;

                            alts.Add("__");
                            break;
                        }
                    }

                    if (fragkeywords.Contains(currentKeyword))
                    {
                        foreach (var prog in fragSubs)
                        {
                            var variantKeywords = prog.Key;
                            List<string> tmp = new List<string>(alts);
                            if (variantKeywords.Contains(" " + currentKeyword + " "))
                                foreach (string alt in tmp)
                                    if (alt != currentKeyword && variantKeywords.Contains(" " + alt + " "))
                                        alts.Remove(alt);
                        }

                        if (!alts.Contains("__"))
                        {
                            foreach (var prog in fragSubs)
                            {
                                var variantKeywords = prog.Key;
                                bool off = true;
                                foreach (var alt in alts)
                                {
                                    if (variantKeywords.Contains(" " + alt + " "))
                                    {
                                        off = false;
                                        break ;
                                    }
                                }

                                if (!off)
                                    continue ;

                                alts.Add("__");
                                break;
                            }
                        }
                    }

                    List<string> tmp2 = new List<string>(alts);
                    List<string> conditions = new List<string>();
                    foreach (string alt in tmp2)
                    {
                        if (alt != "__")
                        {
                            if (!used_keywords.Contains(alt))
                                used_keywords.Add(alt);
                            else
                            {
                                alts.Remove(alt);
                                conditions.Add(alt);
                            }
                        }
                    }
                    variants.Add(new KeyValuePair<List<string>,List<string>>(conditions, alts));
                }
            }

            foreach (var prag in variants)
            {
                if (prag.Key.Count > 0)
                {
                    ret.Append("#if 1");
                    foreach (var condition in prag.Key)
                        ret.Append(" && !defined (" + condition + ")");
                    ret.Append("\n");
                }
                ret.Append("#pragma multi_compile");
                foreach (var key in prag.Value)
                    ret.Append(" " + key);
                ret.Append("\n");
                if (prag.Key.Count > 0)
                    ret.Append("#endif\n");
            }

            foreach (var vert in vertSubs)
            {
                ret.Append("\n#if 1");
                string keys = vert.Key;
                foreach (string key in vertkeywords)
                {
                    if (!keys.Contains(" " + key + " "))
                        ret.Append(" && !defined (" + key + ")");
                    else
                        ret.Append(" && defined (" + key + ")");
                }
                ret.Append("\n\n");
                ret.Append(vert.Value.toString());
                ret.Append("\n#endif\n");
            }

            foreach (var frag in fragSubs)
            {
                ret.Append("\n#if 1");
                string keys = frag.Key;
                var vert = vertSubs.ContainsKey(keys);
                if (vert)
                {
                    foreach (string key in fragkeywords)
                    {
                        if (!keys.Contains(" " + key + " "))
                            ret.Append(" && !defined (" + key + ")");
                        else
                            ret.Append(" && defined (" + key + ")");
                    }
                    ret.Append("\n\n");
                    ret.Append(frag.Value.toString(vertSubs[keys].GetDeclaredUniforms()));
                    ret.Append("\n#endif\n");
                }
                else
                {
                    foreach (string key in fragkeywords)
                    {
                        if (!keys.Contains(" " + key + " "))
                            ret.Append(" && !defined (" + key + ")");
                        else
                            ret.Append(" && defined (" + key + ")");
                    }
                    ret.Append("\n\n");
                    ret.Append(frag.Value.toString());
                    ret.Append("\n#endif\n");
                }
            }
            ret.Append(indent(_indent) + "ENDCG\n");
            return ret.ToString();
        }
    }
}
