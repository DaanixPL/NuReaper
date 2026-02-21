using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class ExtractApiCallArguments : IExtractApiCallArguments
    {
        private readonly IIsVariableLoad _isVariableLoad;
        public ExtractApiCallArguments(IIsVariableLoad isVariableLoad)
        {
            _isVariableLoad = isVariableLoad;
        }
        public List<string> Execute(IList<Instruction> instructions, int callIndex, IMethod method)
        {
             var args = new List<string>();
            int paramCount = method.MethodSig?.Params.Count ?? 0;

            // Walk backwards to find arguments
            for (int i = callIndex - 1; i >= Math.Max(0, callIndex - 10) && args.Count < paramCount; i--)
            {
                var instr = instructions[i];

                // String literal
                if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string str)
                {
                    args.Insert(0, $"\"{str}\"");
                }
                // Integer constant
                else if (instr.OpCode == OpCodes.Ldc_I4_S && instr.Operand is sbyte sb)
                {
                    args.Insert(0, sb.ToString());
                }
                else if (instr.OpCode == OpCodes.Ldc_I4 && instr.Operand is int i32)
                {
                    args.Insert(0, i32.ToString());
                }
                // Variable load (can't resolve easily, just note it)
                else if (_isVariableLoad.Execute(instr))
                {
                    args.Insert(0, "<variable>");
                }
            }

            return args;
        }
    }
}
