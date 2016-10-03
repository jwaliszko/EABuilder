using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EABuilder
{
    public static class Compiler
    {
        private static string _basePath;
        private static readonly AppDomain _loader = AppDomain.CurrentDomain;
        private static readonly Dictionary<string, Assembly> _projectsMap = new Dictionary<string, Assembly>();

        public static void Execute(string assemblyPath, out string[] errors) // reveals compile-time errors
        {
            errors = new string[0];

            try
            {
                _basePath = Path.GetDirectoryName(assemblyPath);
                _loader.AssemblyResolve += LoadAssembly;

                Assembly assembly;
                if (!_projectsMap.TryGetValue(assemblyPath, out assembly))
                {
                    var assemblyBytes = File.ReadAllBytes(assemblyPath);
                    assembly = _loader.Load(assemblyBytes);
                    //assembly.GetType("SampleErrors.CustomToolchain").GetMethod("Register").Invoke(null, null); // register your custom methods if you defined any

                    _projectsMap.Add(assemblyPath, assembly);
                }

                errors = assembly.CompileExpressiveAttributes().ToArray();
                errors.ToList().ForEach(e => Log.Instance.AppendLine(e));
            }
            catch (ReflectionTypeLoadException e)
            {
                var sb = new StringBuilder();
                sb.AppendLine(e.Message).AppendLine();
                sb.AppendLine("LoaderExceptions:").AppendLine();

                foreach (var loaderEx in e.LoaderExceptions)
                {
                    sb.AppendLine(loaderEx.Message).AppendLine();
                    var fileNotFoundEx = loaderEx as FileNotFoundException;
                    if (string.IsNullOrEmpty(fileNotFoundEx?.FusionLog))
                        continue;

                    sb.AppendLine("FusionLog:");
                    sb.AppendLine(fileNotFoundEx.FusionLog);
                }

                throw new Exception(sb.ToString());
            }
            finally
            {
                _loader.AssemblyResolve -= LoadAssembly;
            }
        }

        private static Assembly LoadAssembly(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            var assemblyPath = GetAssemblyLocation($"{new AssemblyName(args.Name).Name}.dll");
            if (!File.Exists(assemblyPath))
                return null;

            var assemblyBytes = File.ReadAllBytes(assemblyPath);
            return _loader.Load(assemblyBytes);
        }

        private static string GetAssemblyLocation(string assemblyName) // looks inside bin folder of sample project
        {
            return Path.GetFullPath(Path.Combine(_basePath, assemblyName));
        }
    }
}