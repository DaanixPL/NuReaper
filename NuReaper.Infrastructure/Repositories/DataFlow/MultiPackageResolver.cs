using dnlib.DotNet;

namespace NuReaper.Infrastructure.Repositories.DataFlow
{
    public class MultiPackageResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, ModuleDefMD> _modules
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> _assemblyToPackage
            = new(StringComparer.OrdinalIgnoreCase);

        public void Register(ModuleDefMD module, string packageName)
        {
            var name = module.Assembly?.Name.String;
            if (name is null) return;

            _modules.TryAdd(name, module);
            _assemblyToPackage.TryAdd(name, packageName);
        }

        public string ResolvePackageName(string assemblyName)
            => _assemblyToPackage.TryGetValue(assemblyName, out var pkg) ? pkg : assemblyName;

        public AssemblyDef? Resolve(IAssembly assembly, ModuleDef sourceModule)
        {
            return _modules.TryGetValue(assembly.Name, out var m) ? m.Assembly : null;
        }

        public bool AddToCache(AssemblyDef asm) => false;
        public bool Remove(AssemblyDef asm)     => false;
    }
}