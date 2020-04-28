using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class ShaderBlock : BlockParser
    {
        private StreamReader _in;
		private new int _indent;

        public ShaderBlock(StreamReader _in, int indent = 1) : base(_in, indent)
        {
            this._in = _in;
			this._indent = indent;
        }

        ~ShaderBlock()
        {

        }

        public string Run()
        {
			string regstr;
			Regex reg;
			Match match;

			string line = "";

			GetLine(_in, out line);

			regstr = "^\\t{" + _indent.ToString() + "}Properties \\{$";
			reg = new Regex(regstr);
			LineMatch(line, out match, reg, regstr);
			content.Add(new Line(line));

			string lastline = PropertiesRoutine();
			content.Add(new Line(lastline));

			regstr = "^\t{" + _indent.ToString() + "}SubShader \\{$";
			reg = new Regex(regstr);

			string fallregstr = "^\\s*Fallback \".*\"$";
			Regex fallreg = new Regex(fallregstr);

			string editregstr = "^\\s*CustomEditor \".*\"$";
			Regex editreg = new Regex(editregstr);

			GetLine(_in, out line);
			while (line.Contains('{') || !line.Contains('}'))
			{
				var match1 = fallreg.Match(line);
				var match2 = editreg.Match(line);
				if (match1.Success || match2.Success)
				{
					content.Add(new Line(line));

					GetLine(_in, out line);
					continue;
				}
				LineMatch(line, out match, reg, regstr);
				content.Add(new Line(line));

				lastline = SubShaderRoutine();
				content.Add(new Line(lastline));

				GetLine(_in, out line);
			}

			return line;
		}

        string SubShaderRoutine()
        {
			string regstr;
			Regex reg;
			Match match;

			string line = "";

			regstr = "^\t{" + (_indent + 1).ToString() + "}Pass \\{$";
			reg = new Regex(regstr);

			string grabregstr = "^\t{" + (_indent + 1).ToString() + "}GrabPass \\{$";
			Regex grabreg = new Regex(grabregstr);

			GetLine(_in, out line);
			while (line.Contains('{') || !line.Contains('}'))
			{
				content.Add(new Line(line));

				match = reg.Match(line);
				if (match.Success)
				{
					PassBlock newBlock = new PassBlock(_in, _indent + 2);
					newBlock.Blend(src_blend, dst_blend);
					newBlock.Cull(cull);
					line = newBlock.Run();
					content.Add(newBlock);
					content.Add(new Line(line));
				}
				else
				{
					match = grabreg.Match(line);
					if (match.Success)
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

        string PropertiesRoutine()
        {
			string regstr;
			Regex reg;
			Match match;

			string line = "";

			regstr = "^.*(_(Src|Dst)Blend(Float)?|_Cull).*$";
			reg = new Regex(regstr);

			GetLine(_in, out line);
			while (line.Contains('{') || !line.Contains('}'))
			{
				content.Add(new Line(line));

				match = reg.Match(line);
				if (match.Success)
				{
					if (((string)match.Groups[1].Value).Contains("SrcBlend"))
						src_blend = match.Groups[1].Value;
					if (((string)match.Groups[1].Value).Contains("DstBlend"))
						dst_blend = match.Groups[1].Value;
					if (match.Groups[1].Value == "_Cull")
						cull = match.Groups[1].Value;
				}

				GetLine(_in, out line);
			}
			return line;
		}

        string src_blend = string.Empty;
        string dst_blend = string.Empty;
        string cull = string.Empty;
    }
}