using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis
{
    public class ReconstructStringFromChars : IReconstructStringFromChars
    {
        public string Execute(IList<Instruction> instructions, int startIndex)
        {
            var result = new System.Text.StringBuilder();
            const int maxLookback = 30;

            int backwardIndex = startIndex;
            while (backwardIndex >= 0 && backwardIndex >= startIndex - maxLookback)
            {
                var instr = instructions[backwardIndex];

                if (instr.OpCode == OpCodes.Ldc_I4 && instr.Operand is int charCode && charCode >= 32 && charCode <= 126)
                {
                    result.Insert(0, (char)charCode);
                }

                backwardIndex--;
            }

            return result.ToString();
        }
    }
}
