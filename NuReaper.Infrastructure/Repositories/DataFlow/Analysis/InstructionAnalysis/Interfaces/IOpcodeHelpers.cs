using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Analysis.InstructionAnalysis.Interfaces
{
    public interface IOpcodeHelpers
    {
        public bool TryGetLocalIndex(Instruction instr, out int index);
        public bool TryGetStoreLocalIndex(Instruction instr, out int index);
        public bool TryGetArgSigIndex(Instruction instr, MethodDef method, out int sigIndex);
    }
}
