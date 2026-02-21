using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis
{
    public class CollectStackArguments : ICollectStackArguments
    {
        private readonly IIsVariableLoad _isVariableLoad;
        private readonly ITryGetVariableValue _tryGetVariableValue;
        private readonly ITryGetCharVariableValue _tryGetCharVariableValue;
        private readonly IIsStackChangingOperation _isStackChangingOperation;

        public CollectStackArguments(IIsVariableLoad isVariableLoad, ITryGetVariableValue tryGetVariableValue, ITryGetCharVariableValue tryGetCharVariableValue, IIsStackChangingOperation isStackChangingOperation)
        {
            _isVariableLoad = isVariableLoad;
            _tryGetVariableValue = tryGetVariableValue;
            _tryGetCharVariableValue = tryGetCharVariableValue;
            _isStackChangingOperation = isStackChangingOperation;
        }

        public List<string> Execute(IList<Instruction> instructions, int callIndex, int paramCount)
        {
            var args = new List<string>();
            int depth = 0;
            
            // ✅ Cofnij się od call instruction i zbierz argumenty
            for (int i = callIndex - 1; i >= 0 && depth < paramCount; i--)
            {
                var instr = instructions[i];

                // ✅ Bezpośredni string literal
                if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string str)
                {
                    args.Insert(0, str); // Insert at beginning to preserve order
                    depth++;
                    continue;
                }

                // ✅ Zmienna - spróbuj znaleźć jej wartość
                if (_isVariableLoad.Execute(instr))
                {
                    var value = _tryGetVariableValue.Execute(instructions, i);
                    if (!string.IsNullOrEmpty(value))
                    {
                        args.Insert(0, value);
                    }
                    else
                    {
                        // ✅ Jeśli nie możemy znaleźć wartości, może to być char
                        var charValue = _tryGetCharVariableValue.Execute(instructions, i);
                        if (charValue.HasValue)
                            args.Insert(0, charValue.Value.ToString());
                    }
                    depth++;
                    continue;
                }

                // ✅ Stop jeśli natrafimy na inną operację która zmienia stos
                if (_isStackChangingOperation.Execute(instr))
                    break;
            }

            return args;
        }
    }
}
