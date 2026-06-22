using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Helpers
{
    internal static class ApplyGenericStackEffect
    {
        public static void Execute(Code op, Stack<StackSlot> stack)
        {
           if (op is Code.Add    or Code.Sub    or Code.Mul    or Code.Div    or Code.Rem
                    or Code.And   or Code.Or     or Code.Xor    or Code.Shl    or Code.Shr
                    or Code.Shr_Un or Code.Ceq   or Code.Cgt    or Code.Cgt_Un
                    or Code.Clt   or Code.Clt_Un
                    or Code.Add_Ovf or Code.Add_Ovf_Un
                    or Code.Sub_Ovf or Code.Sub_Ovf_Un
                    or Code.Mul_Ovf or Code.Mul_Ovf_Un)
            {
                stack.TryPop(out _);
                stack.TryPop(out _);
                stack.Push(StackSlot.Unknown);
                return;
            }

            // pop 1, push 1
            if (op is Code.Neg    or Code.Not
                    or Code.Conv_I  or Code.Conv_I1 or Code.Conv_I2  or Code.Conv_I4
                    or Code.Conv_I8 or Code.Conv_U  or Code.Conv_U1  or Code.Conv_U2
                    or Code.Conv_U4 or Code.Conv_U8 or Code.Conv_R4  or Code.Conv_R8
                    or Code.Conv_R_Un
                    or Code.Conv_Ovf_I  or Code.Conv_Ovf_I1 or Code.Conv_Ovf_I2
                    or Code.Conv_Ovf_I4 or Code.Conv_Ovf_I8
                    or Code.Conv_Ovf_U  or Code.Conv_Ovf_U1 or Code.Conv_Ovf_U2
                    or Code.Conv_Ovf_U4 or Code.Conv_Ovf_U8
                    or Code.Box   or Code.Unbox or Code.Unbox_Any
                    or Code.Castclass or Code.Isinst
                    or Code.Ldobj or Code.Ldind_I  or Code.Ldind_I1 or Code.Ldind_I2
                    or Code.Ldind_I4  or Code.Ldind_I8 or Code.Ldind_U1 or Code.Ldind_U2
                    or Code.Ldind_U4  or Code.Ldind_R4 or Code.Ldind_R8 or Code.Ldind_Ref
                    or Code.Ldlen     or Code.Ldvirtftn or Code.Ldftn)
            {
                stack.TryPop(out _);
                stack.Push(StackSlot.Unknown);
                return;
            }

            // pop 1, push 0
            if (op is Code.Stind_I  or Code.Stind_I1 or Code.Stind_I2 or Code.Stind_I4
                    or Code.Stind_I8 or Code.Stind_R4 or Code.Stind_R8 or Code.Stind_Ref
                    or Code.Stobj    or Code.Throw     or Code.Endfilter)
            {
                stack.TryPop(out _);
                return;
            }
        }
    }
}
