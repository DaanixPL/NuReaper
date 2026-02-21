using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    /// <summary>
    /// Analyzes variable flow through method instructions
    /// Tracks how suspicious strings are used and passed to API calls
    /// </summary>
    public class FlowAnalyzer
    {
        private const int MAX_HOP_DEPTH = 5;
        private const int INSTRUCTION_WINDOW = 15;

        /// <summary>
        /// Tracks a variable from its definition through the instruction list
        /// Returns info about where it's used and in what context
        /// </summary>
        public FlowTrace TraceVariableUsage(
            int stringInstructionIndex,
            string variableName,
            IList<Instruction> instructions,
            IMethod? methodContext = null)
        {
            var trace = new FlowTrace
            {
                StartIndex = stringInstructionIndex,
                VariableName = variableName,
                HopDepth = 0
            };

            // Track forward from string definition
            for (int i = stringInstructionIndex + 1; i < Math.Min(stringInstructionIndex + INSTRUCTION_WINDOW, instructions.Count); i++)
            {
                var instr = instructions[i];

                // Check for variable assignments
                if (IsVariableStore(instr))
                {
                    trace.AssignedTo.Add(new VariableReference
                    {
                        Index = i,
                        Instruction = instr,
                        InstructionName = instr.OpCode.Name
                    });
                }

                // Check for API calls using this variable
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod calledMethod)
                {
                    // Check if the suspicious variable was just loaded/used
                    if (i > 0 && IsVariableLoad(instructions[i - 1]))
                    {
                        trace.UsedInApiCalls.Add(new ApiCallReference
                        {
                            Index = i,
                            MethodName = calledMethod.FullName,
                            HopCount = 0
                        });
                    }
                }
            }

            return trace;
        }

        /// <summary>
        /// Analyzes string construction patterns (concatenation, interpolation, char array)
        /// </summary>
        public StringConstructionInfo AnalyzeStringConstruction(
            int instructionIndex,
            IList<Instruction> instructions)
        {
            var info = new StringConstructionInfo
            {
                StartIndex = instructionIndex
            };

            // Look backwards and forwards for string building patterns
            int backwardWindow = Math.Max(0, instructionIndex - 10);
            int forwardWindow = Math.Min(instructions.Count, instructionIndex + 5);

            // Check for string concatenation (LDSTR + LDSTR + OpCodes.Add or similar)
            for (int i = backwardWindow; i < forwardWindow; i++)
            {
                var instr = instructions[i];

                if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string str)
                {
                    info.StringParts.Add(new StringPart
                    {
                        Value = str,
                        Index = i,
                        Type = StringPartType.Literal
                    });
                }
                // Detect char loads (char array building)
                else if (instr.OpCode == OpCodes.Ldc_I4 && i > 0)
                {
                    // This is a character code (0-127)
                    if (instr.Operand is int charCode && charCode >= 32 && charCode <= 126)
                    {
                        info.StringParts.Add(new StringPart
                        {
                            Value = $"char({(char)charCode})",
                            Index = i,
                            Type = StringPartType.CharCode
                        });
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Checks if instruction is a variable store operation
        /// </summary>
        private bool IsVariableStore(Instruction instr)
        {
            return instr.OpCode == OpCodes.Stloc_0 ||
                   instr.OpCode == OpCodes.Stloc_1 ||
                   instr.OpCode == OpCodes.Stloc_2 ||
                   instr.OpCode == OpCodes.Stloc_3 ||
                   instr.OpCode == OpCodes.Stloc_S ||
                   instr.OpCode == OpCodes.Stloc ||
                   instr.OpCode == OpCodes.Stfld ||
                   instr.OpCode == OpCodes.Stsfld;
        }

        /// <summary>
        /// Checks if instruction is a variable load operation
        /// </summary>
        private bool IsVariableLoad(Instruction instr)
        {
            return instr.OpCode == OpCodes.Ldloc_0 ||
                   instr.OpCode == OpCodes.Ldloc_1 ||
                   instr.OpCode == OpCodes.Ldloc_2 ||
                   instr.OpCode == OpCodes.Ldloc_3 ||
                   instr.OpCode == OpCodes.Ldloc_S ||
                   instr.OpCode == OpCodes.Ldloc ||
                   instr.OpCode == OpCodes.Ldfld ||
                   instr.OpCode == OpCodes.Ldsfld ||
                   instr.OpCode == OpCodes.Ldarg_0 ||
                   instr.OpCode == OpCodes.Ldarg_1 ||
                   instr.OpCode == OpCodes.Ldarg_2 ||
                   instr.OpCode == OpCodes.Ldarg_3 ||
                   instr.OpCode == OpCodes.Ldarg_S ||
                   instr.OpCode == OpCodes.Ldarg;
        }

        /// <summary>
        /// Represents a traced variable flow
        /// </summary>
        public class FlowTrace
        {
            public int StartIndex { get; set; }
            public string VariableName { get; set; } = string.Empty;
            public int HopDepth { get; set; }
            public List<VariableReference> AssignedTo { get; set; } = new();
            public List<ApiCallReference> UsedInApiCalls { get; set; } = new();
        }

        /// <summary>
        /// Information about how a string is constructed
        /// </summary>
        public class StringConstructionInfo
        {
            public int StartIndex { get; set; }
            public List<StringPart> StringParts { get; set; } = new();
        }

        public class VariableReference
        {
            public int Index { get; set; }
            public Instruction Instruction { get; set; } = null!;
            public string InstructionName { get; set; } = string.Empty;
        }

        public class ApiCallReference
        {
            public int Index { get; set; }
            public string MethodName { get; set; } = string.Empty;
            public int HopCount { get; set; }
        }

        public class StringPart
        {
            public string Value { get; set; } = string.Empty;
            public int Index { get; set; }
            public StringPartType Type { get; set; }
        }

        public enum StringPartType
        {
            Literal,
            CharCode,
            Variable,
            Interpolation
        }
    }
}