using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;

namespace App.Infrastructure.Repositories.Scanners.Finders
{
    public class FindNetworkApiCall : IFindNetworkApiCall
    {
        private readonly IIsNetworkApiCall _isNetworkApiCall;
        public FindNetworkApiCall(IIsNetworkApiCall isNetworkApiCall)
        {
            _isNetworkApiCall = isNetworkApiCall;
        }
        public string? Execute(IList<Instruction> instructions, int startIndex)
        {
            const int window = 50;
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                var instr = instructions[i];
                if (instr == null) continue;

                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt || instr.OpCode == OpCodes.Newobj) &&
                    instr.Operand is IMethod method)
                {
                    if (_isNetworkApiCall.Execute(method.FullName))
                        return method.FullName;
                }
            }

            return null;
        }
    }
}
