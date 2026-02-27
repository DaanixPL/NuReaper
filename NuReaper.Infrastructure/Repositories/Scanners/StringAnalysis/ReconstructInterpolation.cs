using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis
{
    public class ReconstructInterpolation : IReconstructInterpolation
    {
        private readonly ICollectStackArguments _collectStackArguments;
        private readonly IIsNetworkApiCall _isNetworkApiCall;
        public ReconstructInterpolation(ICollectStackArguments collectStackArguments, IIsNetworkApiCall isNetworkApiCall)
        {
            _collectStackArguments = collectStackArguments;
            _isNetworkApiCall = isNetworkApiCall;
        }

        public (bool IsInterpolated, string ReconstructedString, int ConcatIndex, List<int> ProcessedIndices) Execute(IList<Instruction> instructions, int startIndex)
        {
            var processedIndices = new List<int>();

            for (int i = startIndex; i < instructions.Count; i++)
            {
                var instr = instructions[i];

                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method)
                {
                    if (method.DeclaringType?.FullName == "System.String" && 
                        (method.Name == "Concat" || method.Name == "Format"))
                    {
                        // Get parameter count
                        int paramCount = method.MethodSig?.Params.Count ?? 0;
                        
                        var args = _collectStackArguments.Execute(instructions, i, paramCount);
                        var reconstructed = string.Join("", args);

                        if (!string.IsNullOrEmpty(reconstructed))
                        {
                            // Mark all Ldstr between startIndex and concatIndex as processed
                            for (int j = startIndex; j <= i; j++)
                            {
                                if (instructions[j].OpCode == OpCodes.Ldstr)
                                    processedIndices.Add(j);
                            }

                            return (true, reconstructed, i, processedIndices);
                        }
                    }

                    if (_isNetworkApiCall.Execute(method.FullName))
                        break;
                }
            }

            return (false, string.Empty, -1, processedIndices);
        }
    }
}
