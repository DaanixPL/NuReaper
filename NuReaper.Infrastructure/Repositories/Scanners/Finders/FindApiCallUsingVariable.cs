using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class FindApiCallUsingVariable : IFindApiCallUsingVariable
    {
        private readonly IIsVariableLoad _isVariableLoad;
        private readonly IIsNetworkApiCall _isNetworkApiCall;

        public FindApiCallUsingVariable(IIsVariableLoad isVariableLoad, IIsNetworkApiCall isNetworkApiCall)
        {
            _isVariableLoad = isVariableLoad;
            _isNetworkApiCall = isNetworkApiCall;
        }

        public string? Execute(IList<Instruction> instructions, int startIndex, int window)
        {
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                var instr = instructions[i];

                // Check if previous instruction loads a variable
                if (i > 0 && _isVariableLoad.Execute(instructions[i - 1]))
                {
                    if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt || instr.OpCode == OpCodes.Newobj) &&
                        instr.Operand is IMethod method)
                    {
                        if (_isNetworkApiCall.Execute(method.FullName))
                            return method.FullName;
                    }
                }
            }

            return null;
        }
    }
}
