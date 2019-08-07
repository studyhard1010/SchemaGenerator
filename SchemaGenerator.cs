using System;
using System.Collections.Generic;
using System.Reflection;
using NJsonSchema;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace Data.SchemaGenerator
{
    public class SchemaGenerator
    {
       
        private const string SchemaFileFormat = "schema.json";
       
        private const string FrameworkAssemblyNamePattern = "Framework";
        private const string SystemPrefix = "System";
        private const string DllSearchPattern = "*.dll";
        public static string InputDir;
        public static string OutputDir;

        public static void Main(string[] args)
        {
            Console.Write("-InputDir= ");
            InputDir = Console.ReadLine();

            Console.Write("-OutputDir= ");
            OutputDir = Console.ReadLine();

            var files = LocateDll(InputDir);

            foreach (string file in files)
            {
                if (string.IsNullOrEmpty(file)) continue;
                try
                {
                    Assembly a = Assembly.LoadFrom(file);
                    var prefix = GetSchemaFilePrefix(a.GetName().Name);
                    foreach (Type type in a.ExportedTypes)
                    {
                        if (string.IsNullOrEmpty(prefix)) continue;
                        if (type.IsClass && !type.IsAbstract)
                        {
                            if (type?.BaseType.Name == BaseTypeName)
                            {
                                var derived = type;
                                do
                                {
                                    if (derived == null) continue;
                                    var combined = Path.Combine(OutputDir, $"{prefix}_{type.Name}_{SchemaFileFormat}");
                                    var schema = JsonSchema4.FromTypeAsync(type);
                                    var schemaJson = schema.Result.ToJson();

                                    if (File.Exists(combined))
                                    {
                                        using (var sr = new StreamReader(combined, Encoding.ASCII))
                                        {
                                            var fileContents = sr.ReadToEnd();
                                            if (String.Compare(fileContents, schemaJson) != 0)
                                            {
                                                throw new Exception("schemas differ: \n" + fileContents + "\n" + schemaJson);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (var sw = new StreamWriter(combined, false, Encoding.ASCII))
                                        {
                                            sw.Write(schemaJson);
                                            sw.Close();
                                        }
                                    }
                                    derived = derived.BaseType;
                                }
                                while (derived != null);
                                Console.WriteLine();
                            }
                        }
                    }
                    Debug.WriteLine($"Succeeded to load file {file}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load file {file}, error: {ex.Message}");
                }
            }
        }

        private static List<string> LocateDll(string rootPath)
        {
            var list = new List<string>();

            list.AddRange(Directory.GetFiles(rootPath, DllSearchPattern));

            var dirs = Directory.GetDirectories(rootPath);

            foreach (var dir in dirs)
                list.AddRange(LocateDll(dir));
            return list;
        }
    }
}
