using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Helpers.Interfaces
{
    public static class Checks
    {
        public static bool IsStelem(Code op) => op
            is Code.Stelem_Ref or Code.Stelem_I   or Code.Stelem_I1
            or Code.Stelem_I2  or Code.Stelem_I4  or Code.Stelem_I8
            or Code.Stelem_R4  or Code.Stelem_R8  or Code.Stelem;
        public static bool IsLdelem(Code op) => op
            is Code.Ldelem_Ref or Code.Ldelem_I   or Code.Ldelem_I1
            or Code.Ldelem_I2  or Code.Ldelem_I4  or Code.Ldelem_I8
            or Code.Ldelem_R4  or Code.Ldelem_R8  or Code.Ldelem_U1
            or Code.Ldelem_U2  or Code.Ldelem_U4  or Code.Ldelem;
    }
}
