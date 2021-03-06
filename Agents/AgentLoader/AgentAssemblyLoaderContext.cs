﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Agent.Abstract;

namespace AgentLoader
{
    public class AgentAssemblyLoaderContext : AssemblyLoadContext
    {
        public static List<Type> AgentTypes { get; } = new List<Type>();
        private AssemblyDependencyResolver _resolver;

        // static AgentAssemblyLoaderContext()
        // {
        //     var agentsFolder = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Agents");
        //     if (Directory.Exists(agentsFolder))
        //     {
        //         AgentTypes =
        //             Directory.GetFiles(agentsFolder, "*Agent.dll")
        //                 .SelectMany(x =>
        //                 {
        //                     var assembly = new AgentLoadContext(x).LoadFromAssemblyPath(x);
        //                     return assembly
        //                         .GetTypes()
        //                         .Where(type => type.IsSubclassOf(typeof(AgentAbstract)));
        //                     //  .Select(type => (AgentAbstract) Activator.CreateInstance(type));
        //                 }).ToList();
        //     }
        //     else
        //     {
        //         AgentTypes =
        //             new List<Type>();
        //     }
        // }
        
        private readonly List<Assembly> _loadedAssemblies;
        private readonly Dictionary<string, Assembly> _sharedAssemblies;

        private readonly string _path;

        public AgentAssemblyLoaderContext(string path, params Type[] sharedTypes) : base(isCollectible:true)
        {
            _path = path;
            _resolver = new AssemblyDependencyResolver(path);

            _loadedAssemblies = new List<Assembly>();
            _sharedAssemblies = new Dictionary<string, Assembly>();

            foreach (var sharedType in sharedTypes)
                _sharedAssemblies[Path.GetFileName(sharedType.Assembly.Location)] = sharedType.Assembly;
            
            foreach (string dll in Directory.EnumerateFiles(_path, "*.dll"))
            {
                if (_sharedAssemblies.ContainsKey(Path.GetFileName(dll)))
                    continue;

                _loadedAssemblies.Add(this.LoadFromAssemblyPath(dll));
            }

            foreach (var assembly in _loadedAssemblies)
            {
                try
                {
                    var t = assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(AgentAbstract)))
                        .ToList();
                    AgentTypes.AddRange(t);
                    if(t.Count>0) Console.WriteLine($"Loaded {t.Count} agents from path {assembly.Location}");
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
            // var agentTypes = _loadedAssemblies.SelectMany(asm => asm.GetTypes()
            //     .Where(type => type.IsSubclassOf(typeof(AgentAbstract))))
            //     .ToList();
            //
            // AgentTypes.AddRange(agentTypes);
            

        }

        // public IEnumerable<T> GetImplementations<T>()
        // {
        //     return loadedAssemblies
        //         .SelectMany(a => a.GetTypes())
        //         .Where(t => typeof(T).IsAssignableFrom(t))
        //         .Select(t => Activator.CreateInstance(t))
        //         .Cast<T>();
        // }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                string filename = $"{assemblyName.Name}.dll";
                if (_sharedAssemblies.ContainsKey(filename))
                    return _sharedAssemblies[filename];
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }
    }
}