using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis
{
    public class TryGetCharVariableValue : ITryGetCharVariableValue
    {
        private readonly IVariableIndexExtractor _variableIndexExtractor;
        private readonly IIsStoreToVariable _isStoreToVariable;
        private readonly IIsCharacterCode _isCharacterCode;
        public TryGetCharVariableValue(IVariableIndexExtractor variableIndexExtractor, IIsStoreToVariable isStoreToVariable, IIsCharacterCode isCharacterCode)
        {
            _variableIndexExtractor = variableIndexExtractor;
            _isStoreToVariable = isStoreToVariable;
            _isCharacterCode = isCharacterCode;
        }
        public char? Execute(IList<Instruction> instructions, int loadIndex)
        {
            var loadInstr = instructions[loadIndex];
            var varIndex = _variableIndexExtractor.Execute(loadInstr);
            if (varIndex == -1)
                return null;

            int startSearchFrom = Math.Max(0, loadIndex - 300);
            
            for (int i = loadIndex - 1; i >= startSearchFrom; i--)
            {
                var instr = instructions[i];

                if (_isStoreToVariable.Execute(instr, varIndex))
                {
                    // Szukaj Ldc_I4 tuż przed store
                    for (int j = i - 1; j >= Math.Max(0, i - 5); j--)
                    {
                        var checkInstr = instructions[j];
                        
                        if (checkInstr.OpCode == OpCodes.Ldc_I4_S || 
                            checkInstr.OpCode == OpCodes.Ldc_I4)
                        {
                            int? charCode = null;
                            
                            if (checkInstr.Operand is sbyte sb)
                                charCode = sb;
                            else if (checkInstr.Operand is int i32)
                                charCode = i32;
                            else if (checkInstr.Operand is byte b)
                                charCode = b;
                            
                            if (charCode.HasValue && _isCharacterCode.Execute(charCode.Value))
                            {
                                return (char)charCode.Value;
                            }
                        }
                    }
                    
                    // Znaleziono store ale bez Ldc_I4 - zmienna może być parametrem lub field
                    // STOP tutaj (nie szukaj dalej)
                    return null;
                }
            }
            return null;
        }
    }
}
