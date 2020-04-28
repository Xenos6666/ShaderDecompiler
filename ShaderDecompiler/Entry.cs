#undef TIMING
#undef PARALLEL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShaderDecompiler
{
    class Entry
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("shaderReconstructor inputFile/directory outputFile/directory");
                return;
            }
#if TIMING
            Stopwatch test = new Stopwatch();
            test.Start();
#endif
            Regex.CacheSize = 256; //this needs to be replaced

            if (System.IO.File.Exists(args[0]))
            {
                ShaderProcess(args[0], args[1]);
            }

            else if (System.IO.Directory.Exists(args[0]))
            {
                var files = Directory.GetFiles(args[0], "*.shader", SearchOption.AllDirectories);
#if PARALLEL
                Parallel.ForEach(files, file =>
                {
                    var output = file.Replace(args[0], args[1]);
                    var path = new FileInfo(output).Directory.FullName;
                    if (!Directory.Exists(path))
                    {
                        Console.WriteLine("creating: " + path);
                        Directory.CreateDirectory(path);
                    }
                    ShaderProcess(file, output);
                });
#else
                foreach (var file in files)
                {
                    var output = file.Replace(args[0], args[1]);
                    var path = new FileInfo(output).Directory.FullName;
                    if (!Directory.Exists(path))
                    {
                        Console.WriteLine("creating: " + path);
                        Directory.CreateDirectory(path);
                    }
                    ShaderProcess(file, output);
                }
#endif
            }
            else
            {
                Console.WriteLine($"{args[0]} is not a file or directory");
            }
#if TIMING
            test.Stop();
            Console.WriteLine(test.ElapsedMilliseconds);
#endif
            Console.ReadLine();
        }

        private static void ShaderProcess(string input, string output)
        {
            try
            {
                File shaderFile = new File(input);

                using (var outFile = System.IO.File.Create(output))
                {
                    using (StreamWriter stream = new StreamWriter(outFile))
                    {
                        shaderFile.Write(stream);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (System.IO.File.Exists(output))
                {
                    System.IO.File.Delete(output);
                }
            }
        }
    }
}
