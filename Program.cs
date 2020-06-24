using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace HAIconJsBuilder
{
    class Program
    {
        static int Main(string[] args)
        {
            var path = GetIconPath(args);

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Path not found.");
                PathHelp();
                return -1;
            }

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Invalid path supplied.");
                PathHelp();
                return -1;
            }

            var builder = new JsBuilder(path);

            builder.JustDoIt();

            Console.WriteLine("done");
            return 0;
        }

        private static void PathHelp()
        {
            Console.WriteLine("Please provide the path using the following format:  HAIconJsBuilder.exe -path=c:\\homeassistant\\myicons\\");
        }

        private static string GetIconPath(string[] args)
        {
            var pathArg = args.FirstOrDefault(x => x.ToLower().StartsWith("-path="));

            if (string.IsNullOrEmpty(pathArg))
                return null;

            return pathArg.Split("=")[1];
        }
    }

    public class JsBuilder
    {
        private readonly string _iconPath;
        private MemoryStream _outputStream;

        public JsBuilder(string iconPath)
        {
            _iconPath = iconPath;
        }

        public void JustDoIt()
        {
            var files = GetSvgFileNames();

            if (files.Length <= 0)
            {
                Console.WriteLine("No svgs found.");
                return;
            }

            _outputStream = new MemoryStream();

            using (var sWriter = new StreamWriter(_outputStream, Encoding.UTF8))
            {
                sWriter.WriteLine("const ICON = {");

                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];

                    var svgBody = GetSvgBody(file);

                    if (string.IsNullOrEmpty(svgBody))
                        continue;

                    var fileName = Path.GetFileName(file);

                    var svgName = BuildSvgName(fileName);

                    Console.WriteLine($"Processing {fileName} - custom:{svgName}");

                    sWriter.Write($"\t{svgName}: '");
                    sWriter.Write(svgBody);
                    sWriter.Write("'");

                    sWriter.WriteLine(i < files.Length - 1 ? "," : string.Empty);
                }
                sWriter.WriteLine("};\n");

                sWriter.WriteLine(@"  async function getIcon(name) {
    return {
      path: ICON[name],
      viewBox: ""0 0 24 24""
    };
  }

  window.customIconsets = window.customIconsets || { };
  window.customIconsets['custom'] = getIcon;");

                sWriter.Flush();

                OutputStreamToFile();
            }
        }

        private string BuildSvgName(string fileName)
        {
            var svgName = Path.GetFileName(fileName).Split(".svg")[0];

            var onlyLetters = new string(svgName.Where(char.IsLetter).ToArray());

            return onlyLetters.ToLower();
        }

        private string GetSvgBody(string fileName)
        {
            try
            {
                var svgFile = XElement.Load(fileName);
                var svgPath = svgFile.Elements().First(x => x.Name.LocalName == "path");
                var attribute = svgPath.Attributes().First(x => x.Name.LocalName == "d");

                return attribute.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine($"Error while reading file: {fileName}");
                
                return string.Empty;
            }
        }

        private void OutputStreamToFile()
        {
            var outputFile = Path.Combine(_iconPath, "custom_icons.js");

            using (var fileStream = File.Create(outputFile))
            {
                _outputStream.Seek(0, SeekOrigin.Begin);
                _outputStream.CopyTo(fileStream);
            }
        }

        private string[] GetSvgFileNames()
        {
            return Directory.GetFiles(_iconPath, "*.svg");
        }
    }
}
