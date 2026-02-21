using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class FindNetworkApiCallAfterIndex : IFindNetworkApiCallAfterIndex
    {
        private readonly IIsNetworkApiCall _isNetworkApiCall;
        public FindNetworkApiCallAfterIndex(IIsNetworkApiCall isNetworkApiCall)
        {
            _isNetworkApiCall = isNetworkApiCall ?? throw new ArgumentNullException(nameof(isNetworkApiCall));
        }
        public string? Execute(IList<Instruction> instructions, int startIndex, int window = 50)
        {
             for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                var instr = instructions[i];
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