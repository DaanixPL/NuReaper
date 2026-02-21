using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis
{
    public class ReconstructFromInterpolatedHandler : IReconstructFromInterpolatedHandler
    {
        private readonly ITryGetCharVariableValue _tryGetCharVariableValue;
        private readonly IIsVariableLoad _isVariableLoad;
        public ReconstructFromInterpolatedHandler(ITryGetCharVariableValue tryGetCharVariableValue, IIsVariableLoad isVariableLoad)
        {
            _tryGetCharVariableValue = tryGetCharVariableValue;
            _isVariableLoad = isVariableLoad;
        }
        public string Execute(IList<Instruction> instructions, int newObjIndex)
        {
            var parts = new List<string>();
            const int maxWindow = 100;
            
            // Szukaj AppendLiteral i AppendFormatted calls po newobj
            for (int i = newObjIndex + 1; i < Math.Min(newObjIndex + maxWindow, instructions.Count); i++)
            {
                var instr = instructions[i];
                
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method)
                {
                    if (method.Name == "AppendLiteral")
                    {
                        // Cofnij się i znajdź Ldstr argument
                        if (i > 0 && instructions[i - 1].OpCode == OpCodes.Ldstr &&
                            instructions[i - 1].Operand is string literal)
                        {
                            parts.Add(literal);
                        }
                    }
                    // AppendFormatted(T) - char
                    else if (method.Name == "AppendFormatted")
                    {
                        // Cofnij się i znajdź Ldloc argument
                        if (i > 0)
                        {
                            var prevInstr = instructions[i - 1];

                            if (_isVariableLoad.Execute(prevInstr)) 
                            {
                                var charValue = _tryGetCharVariableValue.Execute(instructions, i - 1);
                                
                                if (charValue.HasValue)
                                {
                                    parts.Add(charValue.Value.ToString());
                                }
                            }
                        }
                    }
                    // ToStringAndClear = end
                    else if (method.Name == "ToStringAndClear")
                    {
                        break;
                    }
                }
            }
            return string.Join("", parts);
        }
    }
}
