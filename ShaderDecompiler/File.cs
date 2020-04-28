namespace ShaderDecompiler
{
    internal class File
    {
        private string filePath;
        private System.IO.StreamReader stream;
        private ShaderParser shaderParser;

        public File(string input)
        {
            this.filePath = input;
            stream = new System.IO.StreamReader(System.IO.File.OpenRead(input));
            shaderParser = new ShaderParser(stream);
            shaderParser.Run();
        }

        public void Write(System.IO.StreamWriter @out)
        {
            @out.Write(shaderParser.toString());
        }
    }
}