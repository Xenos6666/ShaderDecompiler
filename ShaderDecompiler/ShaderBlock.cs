using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class ShaderBlock : BlockParser
    {
        private StreamReader _in;
		const int _indent = 1;

        string _srcBlend = string.Empty;
        string _dstBlend = string.Empty;
        string _cull = string.Empty;

		public ShaderBlock(StreamReader _in) : base(_in, _indent)
        {
            this._in = _in;
        }

        private static readonly string _startRunRegexStr = "^\\t{" + _indent + "}Properties \\{$";
        private static readonly Regex _startRunRegex = new Regex(_startRunRegexStr);
        private static readonly string _loopRunRegexStr = "^\t{" + _indent + "}SubShader \\{$";
		private static readonly Regex _loopRunRegex = new Regex(_loopRunRegexStr);
        private static readonly string fallRunRegexStr = "^\\s*Fallback \".*\"$";
        private static readonly Regex _fallRunRegex = new Regex(fallRunRegexStr);
		private static readonly string _editRunRegexStr = "^\\s*CustomEditor \".*\"$";
		private static readonly Regex _editRunRegex = new Regex(_editRunRegexStr);

		public string Run()
        {
            GetLine(_in, out var line);

			LineMatch(line, out _, _startRunRegex, _startRunRegexStr);
			content.Add(new Line(line));

			string lastline = PropertiesRoutine();
			content.Add(new Line(lastline));

			GetLine(_in, out line);
			while (line.Contains('{') || !line.Contains('}'))
			{
				if (_fallRunRegex.IsMatch(line) || _editRunRegex.IsMatch(line))
				{
					content.Add(new Line(line));

					GetLine(_in, out line);
					continue;
				}

				LineMatch(line, out _, _loopRunRegex, _loopRunRegexStr);
				content.Add(new Line(line));

				lastline = SubShaderRoutine();
				content.Add(new Line(lastline));

				GetLine(_in, out line);
			}

			return line;
		}

        private static readonly string _passSubRegexStr = "^\t{" + (_indent + 1) + "}Pass \\{$";
		private static readonly Regex _passSubRegex = new Regex(_passSubRegexStr);
        private static readonly string _grabSubRegexStr = "^\t{" + (_indent + 1) + "}GrabPass \\{$";
		private static readonly Regex _grabSubRegex = new Regex(_grabSubRegexStr);

		string SubShaderRoutine()
        {
			GetLine(_in, out var line);
			while (line.Contains('{') || !line.Contains('}'))
			{
				content.Add(new Line(line));

				if (_passSubRegex.IsMatch(line))
				{
					PassBlock newBlock = new PassBlock(_in);
					newBlock.Blend(_srcBlend, _dstBlend);
					newBlock.Cull(_cull);
					line = newBlock.Run();
					content.Add(newBlock);
					content.Add(new Line(line));
				}
				else
				{
					if (_grabSubRegex.IsMatch(line))
					{
						BlockParser newBlock = new BlockParser(_in, _indent + 2);
						line = newBlock.Run();
						content.Add(newBlock);
						content.Add(new Line(line));
					}
				}

				GetLine(_in, out line);
			}
			return line;
		}

        private const string _loopPropRegexStr = "^.*(_(Src|Dst)Blend(Float)?|_Cull).*$";
		private static readonly Regex _loopPropRegex = new Regex(_loopPropRegexStr);

        string PropertiesRoutine()
        {
            GetLine(_in, out var line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                content.Add(new Line(line));

                var match = _loopPropRegex.Match(line);
                if (match.Success)
                {
                    if (match.Groups[1].Value.Contains("SrcBlend"))
                        _srcBlend = match.Groups[1].Value;
                    if (match.Groups[1].Value.Contains("DstBlend"))
                        _dstBlend = match.Groups[1].Value;
                    if (match.Groups[1].Value == "_Cull")
                        _cull = match.Groups[1].Value;
                }

                GetLine(_in, out line);
            }

            return line;
        }
    }
}