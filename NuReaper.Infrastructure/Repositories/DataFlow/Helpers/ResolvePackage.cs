using dnlib.DotNet;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Helpers
{
    public static class ResolvePackage
    {
        public static int Execute(IMethod m, MultiPackageResolver resolver, int fallbackPackageId)
        {
            var asm = m.DeclaringType?.DefinitionAssembly?.Name.String;
            return asm != null ? resolver.ResolvePackageId(asm) : fallbackPackageId;
        }
    }
}
