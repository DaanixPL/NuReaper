using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.VariableAnalysis
{
    public class TryGetVariableValue : ITryGetVariableValue
    {
        private readonly IExtractVariableIndex _extractVariableIndex;
        private readonly IIsStoreToVariable _isStoreToVariable;
        public TryGetVariableValue(IExtractVariableIndex extractVariableIndex, IIsStoreToVariable isStoreToVariable)
        {
            _extractVariableIndex = extractVariableIndex;
            _isStoreToVariable = isStoreToVariable;
        }
        public string? Execute(IList<Instruction> instructions, int loadIndex)
        {
            var loadInstr = instructions[loadIndex];
            var varIndex = _extractVariableIndex.Execute(loadInstr);

            if (varIndex == -1)
                return null;

            // Cofnij się i szukaj Stloc/Stloc_X tego samego indeksu
            for (int i = loadIndex - 1; i >= 0 && i >= loadIndex - 30; i--)
            {
                var instr = instructions[i];

                // Szukaj store do tej samej zmiennej
                if (_isStoreToVariable.Execute(instr, varIndex))
                {
                    // Cofnij się jeszcze bardziej i szukaj Ldstr tuż przed store
                    for (int j = i - 1; j >= 0 && j >= i - 5; j--)
                    {
                        if (instructions[j].OpCode == OpCodes.Ldstr && instructions[j].Operand is string str)
                            return str;
                    }
                    break;
                }
            }

            return null;
        }
    }
}
