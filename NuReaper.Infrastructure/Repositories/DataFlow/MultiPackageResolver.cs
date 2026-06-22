using dnlib.DotNet;

namespace NuReaper.Infrastructure.Repositories.DataFlow
{
    public class MultiPackageResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, ModuleDefMD> _modules
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, int> _assemblyToPackageId
            = new(StringComparer.OrdinalIgnoreCase);

        public void Register(ModuleDefMD module, int packageId)
        {
            var name = module.Assembly?.Name.String;
            if (name is null) return;

            _modules.TryAdd(name, module);
            _assemblyToPackageId.TryAdd(name, packageId);
        }

        public int ResolvePackageId(string assemblyName)
            => _assemblyToPackageId.TryGetValue(assemblyName, out var pkgId) ? pkgId : -1;

        public AssemblyDef? Resolve(IAssembly assembly, ModuleDef sourceModule)
        {
            return _modules.TryGetValue(assembly.Name, out var m) ? m.Assembly : null;
        }

        public bool AddToCache(AssemblyDef asm) => false;
        public bool Remove(AssemblyDef asm)     => false;
    }
}