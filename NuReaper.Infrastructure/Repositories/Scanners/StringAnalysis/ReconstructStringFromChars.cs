using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis
{
    public class ReconstructStringFromChars : IReconstructStringFromChars
    {
        private readonly ILogger<ReconstructStringFromChars> _logger;

        public ReconstructStringFromChars(ILogger<ReconstructStringFromChars> logger)
        {
            _logger = logger;
        }

        public string Execute(IList<Instruction> instructions, int startIndex)
        {
            var result = new System.Text.StringBuilder();
            const int maxLookback = 100;
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

            _logger.LogTrace("[RECONSTRUCT] Reconstructed: {Reconstructed} from {StartIndex} looking back {Lookback} instructions.", result, startIndex, startIndex - backwardIndex);

            return result.ToString();
        }
    }
}
