using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Application.DTOs;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis;
using NuReaper.Infrastructure.Repositories.Scanners.Base;

namespace NuReaper.Infrastructure.Repositories.Scanners
{
    /// <summary>
    /// Scans assemblies for suspicious network-related API calls
    /// Includes: HttpClient, WebClient, Dns, Socket operations
    /// Uses flow tracking to reduce false positives
    /// </summary>
    public class NetworkApiCallScanner : ScannerBase, IAssemblyScanner
    {
        private static readonly string[] NetworkApiCalls = new[]
        {
            // HttpClient - most common in modern .NET
            "HttpClient::GetAsync",
            "HttpClient::PostAsync",
            "HttpClient::PutAsync",
            "HttpClient::DeleteAsync",
            "HttpClient::SendAsync",

            "HttpRequestMessage::.ctor",

            // WebClient - legacy but still used in malware
            "WebClient::DownloadString",
            "WebClient::DownloadFile",
            "WebClient::OpenRead",

            // DNS - often used in C2 communication
            "Dns::GetHostEntry",
            "Dns::GetHostAddresses",
        };

        private readonly FlowAnalyzer _flowAnalyzer = new();

        public async Task<ScanPackageResultResponse> ScanPackageAsync(
            string packageName,
            string version,
            string sha256Hash,
            string extractedPath,
            CancellationToken cancellationToken)
        {
            var findings = new List<FindingSummaryDto>();
            var files = GetAssemblyFiles(extractedPath);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var moduleFindings = ScanModule(file);
                    findings.AddRange(moduleFindings);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning {file}: {ex.Message}");
                }
            }

            return new ScanPackageResultResponse
            {
                PackageName = packageName,
                Version = version,
                Author = "NuGet",
                Sha256Hash = sha256Hash,
                Findings = findings,
                TotalFindings = findings.Count,
                ScannedTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Gets all DLL and EXE files from the specified path
        /// </summary>
        private List<string> GetAssemblyFiles(string filePath)
        {
            var files = new List<string>();

            if (Directory.Exists(filePath))
            {
                files.AddRange(Directory.GetFiles(filePath, "*.dll", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(filePath, "*.exe", SearchOption.AllDirectories));
            }
            else if (File.Exists(filePath))
            {
                files.Add(filePath);
            }

            return files;
        }

        /// <summary>
        /// Scans a single module and returns all findings
        /// </summary>
        private List<FindingSummaryDto> ScanModule(string filePath)
        {
            var findings = new List<FindingSummaryDto>();
            ModuleDefMD? module = null;

            try
            {
                module = ModuleDefMD.Load(filePath);

                foreach (var type in module.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody || !method.Body.HasInstructions)
                            continue;

                        var methodFindings = ScanMethod(method, type);
                        findings.AddRange(methodFindings);
                    }
                }
            }
            finally
            {
                module?.Dispose();
            }

            return findings;
        }

        /// <summary>
        /// Scans a single method for suspicious network activity
        /// </summary>
        private List<FindingSummaryDto> ScanMethod(MethodDef method, TypeDef type)
        {
            var findings = new List<FindingSummaryDto>();
            var instructions = method.Body.Instructions;
            var processedIndices = new HashSet<int>(); // ✅ Deduplikacja
            var processedConcatIndices = new HashSet<int>(); // ✅ Deduplikacja dla Concat

            Console.WriteLine($"\n=== SCANNING METHOD: {type.FullName}::{method.Name} ===");
            Console.WriteLine($"Total instructions: {instructions.Count}");

            // 🔍 DODAJ TO: Dump wszystkich instrukcji dla CharObfuscation
            if (method.Name == "CharObfuscation")
            {
                Console.WriteLine("\n📋 FULL IL DUMP:");
                for (int idx = 0; idx < instructions.Count; idx++)
                {
                    var inst = instructions[idx];
                    string operandStr = inst.Operand switch
                    {
                        string s => $"\"{s}\"",
                        IMethod m => $"{m.DeclaringType?.Name}::{m.Name}",  // ✅ Pokaż pełną nazwę
                        ITypeDefOrRef t => t.Name,
                        int i => i.ToString(),
                        _ => inst.Operand?.ToString() ?? ""
                    };
                    Console.WriteLine($"  IL_{idx:X4}: {inst.OpCode.Name,-15} {operandStr}");
                }
                Console.WriteLine("📋 END IL DUMP\n");
            }

            // 🔍 LOG 1: Które metody są skanowane
            Console.WriteLine($"\n=== SCANNING METHOD: {type.FullName}::{method.Name} ===");
            Console.WriteLine($"Total instructions: {instructions.Count}");

            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];

                // ✅ Pattern 1, 2 & 4: String (literal, variable, or interpolated) → API call
                if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string stringValue)
                {
                    if (processedIndices.Contains(i))
                        continue; // ✅ Skip jeśli już przetworzone

                    // 🔍 LOG 2: Każdy Ldstr
                    Console.WriteLine($"\n[IL_{i:X4}] Found Ldstr: \"{stringValue.Substring(0, Math.Min(30, stringValue.Length))}...\"");

                    // ✅ Check if this is part of string interpolation (Pattern 4)
                    var interpolationResult = TryReconstructInterpolation(instructions, i);
                    if (interpolationResult.IsInterpolated)
                    {
                        // 🔍 LOG 3: Pattern 4 triggered
                        Console.WriteLine($"[IL_{i:X4}] Pattern 4 TRIGGERED!");
                        Console.WriteLine($"  → Reconstructed: \"{interpolationResult.ReconstructedString}\"");
                        Console.WriteLine($"  → ConcatIndex: IL_{interpolationResult.ConcatIndex:X4}");
                        Console.WriteLine($"  → IsSuspicious: {IsSuspiciousString(interpolationResult.ReconstructedString)}");
                        // Mark all indices as processed
                        foreach (var idx in interpolationResult.ProcessedIndices)
                            processedIndices.Add(idx);
                        
                        if (IsSuspiciousString(interpolationResult.ReconstructedString))
                        {
                            var apiCall = FindNetworkApiCallAfterIndex(instructions, interpolationResult.ConcatIndex, 50);

                            Console.WriteLine($"  → Found API call: {apiCall ?? "NONE"}");

                            if (!string.IsNullOrEmpty(apiCall))
                            {
                                findings.Add(CreateFinding(
                                    interpolationResult.ReconstructedString,
                                    apiCall,
                                    type,
                                    method,
                                    i,
                                    hopDepth: 1,
                                    isLiteral: false,
                                    flowTrace: new List<string> { "constructed via string interpolation" }
                                ));
                                processedConcatIndices.Add(interpolationResult.ConcatIndex); // Mark Concat as processed

                                Console.WriteLine($" <> Finding added, Concat marked as processed");
                            }
                            else
                            {
                                Console.WriteLine($"  → ❌ No API call found, Concat NOT marked as processed");
                            }
                            continue;
                        }
                    }

                    // ✅ Not interpolation - check if suspicious
                    if (!IsSuspiciousString(stringValue))
                        continue;

                    // ✅ Pattern 1: Direct usage - string immediately used in API call
                    var directApiCall = FindNetworkApiCall(instructions, i);
                    if (!string.IsNullOrEmpty(directApiCall))
                    {
                        findings.Add(CreateFinding(
                            stringValue,
                            directApiCall,
                            type,
                            method,
                            i,
                            hopDepth: 0,
                            isLiteral: true,
                            flowTrace: new List<string> { "direct usage in API call" }
                        ));
                        processedIndices.Add(interpolationResult.ConcatIndex); // ✅ Mark as processed
                        continue;
                    }

                    // ✅ Pattern 2: Variable flow - string assigned to variable, then used in API call
                    var nextStore = FindNextVariableStore(instructions, i, 5);
                    if (nextStore.HasValue)
                    {
                        var varName = GetVariableName(nextStore.Value.Instruction);
                        var apiCallWithVar = FindApiCallUsingVariable(instructions, nextStore.Value.Index, 50);

                        if (!string.IsNullOrEmpty(apiCallWithVar))
                        {
                            findings.Add(CreateFinding(
                                stringValue,
                                apiCallWithVar,
                                type,
                                method,
                                nextStore.Value.Index,
                                hopDepth: 1,
                                isLiteral: false,
                                flowTrace: new List<string>
                                {
                                    $"String assigned to {varName} at IL_{i:X4}",
                                    $"Used in API call at IL_{nextStore.Value.Index:X4}"
                                }
                            ));
                            processedIndices.Add(interpolationResult.ConcatIndex); // Mark as processed
                            continue;
                        }
                    }
                }

                // ✅ Pattern 3: String construction from characters
                if (instr.OpCode == OpCodes.Ldc_I4 && IsCharacterCode(instr.Operand))
                {
                    var constructedString = ReconstructStringFromChars(instructions, i);
                    if (!string.IsNullOrEmpty(constructedString) && IsSuspiciousString(constructedString))
                    {
                        var apiCall = FindNetworkApiCallAfterIndex(instructions, i);
                        if (!string.IsNullOrEmpty(apiCall))
                        {
                            findings.Add(CreateFinding(
                                constructedString,
                                apiCall,
                                type,
                                method,
                                i,
                                hopDepth: 1,
                                isLiteral: false,
                                flowTrace: new List<string> { "constructed from character codes" }
                            ));
                        }
                    }
                }

                // ✅ NEW Pattern 5: String.Concat without Ldstr (char-only interpolation)
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method2 &&
                    method2.DeclaringType?.FullName == "System.String" &&
                    method2.Name == "Concat")
                {
                    // 🔍 LOG 4: Pattern 5 triggered
                    Console.WriteLine($"\n[IL_{i:X4}] Pattern 5 TRIGGERED! (String.Concat)");
                    Console.WriteLine($"  → Already processed? {processedConcatIndices.Contains(i)}");

                    if (processedConcatIndices.Contains(i))
                    {
                        Console.WriteLine($"  → ⏭️ Skipping (already processed by Pattern 4)");
                        continue;
                    }

                    // ✅ Zbierz argumenty (może być mix Ldstr + char variables)
                    int paramCount = method2.MethodSig?.Params.Count ?? 0;

                    Console.WriteLine($"  → Param count: {paramCount}");

                    var args = CollectStackArguments(instructions, i, paramCount);
                    var reconstructed = string.Join("", args);

                    Console.WriteLine($"  → Collected args: [{string.Join(", ", args.Select(a => $"\"{a}\""))}]");
                    Console.WriteLine($"  → Reconstructed: \"{reconstructed}\"");
                    Console.WriteLine($"  → IsSuspicious: {IsSuspiciousString(reconstructed)}");


                    if (!string.IsNullOrEmpty(reconstructed) && IsSuspiciousString(reconstructed))
                    {
                        // ✅ Sprawdź czy po Concat jest variable store + API call
                        var nextStore = FindNextVariableStore(instructions, i, 5);

                        Console.WriteLine($"  → Next store found? {nextStore.HasValue}");

                        if (nextStore.HasValue)
                        {
                            Console.WriteLine($"  → Store at IL_{nextStore.Value.Index:X4}");

                            var apiCall = FindApiCallUsingVariable(instructions, nextStore.Value.Index, 50);

                            Console.WriteLine($"  → API call using var: {apiCall ?? "NONE"}");

                            if (!string.IsNullOrEmpty(apiCall))
                            {
                                findings.Add(CreateFinding(
                                    reconstructed,
                                    apiCall,
                                    type,
                                    method,
                                    i,
                                    hopDepth: 1,
                                    isLiteral: false,
                                    flowTrace: new List<string> { "constructed from char interpolation (Pattern 5)" }
                                ));
                                processedConcatIndices.Add(i);
                                Console.WriteLine($"  →  Finding added via variable flow");
                            }
                        }
                        else
                        {
                            // ✅ Może być bezpośrednie użycie (bez store)
                            var apiCall = FindNetworkApiCallAfterIndex(instructions, i);

                            Console.WriteLine($"  → API call direct: {apiCall ?? "NONE"}");

                            if (!string.IsNullOrEmpty(apiCall))
                            {
                                findings.Add(CreateFinding(
                                    reconstructed,
                                    apiCall,
                                    type,
                                    method,
                                    i,
                                    hopDepth: 1,
                                    isLiteral: false,
                                    flowTrace: new List<string> { "constructed from char interpolation (Pattern 5)" }
                                ));
                                processedConcatIndices.Add(i);
                                Console.WriteLine($"  → ✅ Finding added via direct call");
                            }
                            else                           {
                                Console.WriteLine($"  → ❌ No API call found after Concat");
                            }
                        }
                    }
                }
                // ✅ Pattern 6: DefaultInterpolatedStringHandler (C# 10+ string interpolation)
                if (instr.OpCode == OpCodes.Call &&
                    instr.Operand is IMethod ctor &&
                    ctor.DeclaringType?.Name == "DefaultInterpolatedStringHandler" &&
                    ctor.Name == ".ctor")
                {
                    Console.WriteLine($"\n[IL_{i:X4}] Pattern 6 TRIGGERED! (DefaultInterpolatedStringHandler)");
                    
                    // Zbierz wszystkie AppendLiteral/AppendFormatted calls
                    var reconstructedString = ReconstructFromInterpolatedHandler(instructions, i);
                    Console.WriteLine($"  → Reconstructed: \"{reconstructedString}\"");
                    Console.WriteLine($"  → IsSuspicious: {IsSuspiciousString(reconstructedString)}");
                    
                    if (!string.IsNullOrEmpty(reconstructedString) && IsSuspiciousString(reconstructedString))
                    {
                        // Szukaj ToStringAndClear + store + API call
                        var toStringIndex = FindToStringAndClear(instructions, i);
                        if (toStringIndex > 0)
                        {
                            Console.WriteLine($"  → Found ToStringAndClear at IL_{toStringIndex:X4}");
                            
                            var nextStore = FindNextVariableStore(instructions, toStringIndex, 3);
                            if (nextStore.HasValue)
                            {
                                var apiCall = FindApiCallUsingVariable(instructions, nextStore.Value.Index, 50);
                                Console.WriteLine($"  → API call: {apiCall ?? "NONE"}");
                                
                                if (!string.IsNullOrEmpty(apiCall))
                                {
                                    findings.Add(CreateFinding(
                                        reconstructedString,
                                        apiCall,
                                        type,
                                        method,
                                        i,
                                        hopDepth: 1,
                                        isLiteral: false,
                                        flowTrace: new List<string> { "constructed via DefaultInterpolatedStringHandler (C# 10+)" }
                                    ));
                                    Console.WriteLine($"  → ✅ Finding added");
                                }
                            }
                        }
                    }
                }


            }


            Console.WriteLine($"\n=== END SCAN: {type.FullName}::{method.Name} ===");
            Console.WriteLine($"Total findings: {findings.Count}");
            Console.WriteLine($"Processed Concat indices: [{string.Join(", ", processedConcatIndices.Select(x => $"IL_{x:X4}"))}]");

            return findings;
        }

        /// <summary>
        /// ✅ NEW: Tries to detect and reconstruct string interpolation
        /// Returns reconstructed string if interpolation detected, otherwise null
        /// </summary>
        private (bool IsInterpolated, string ReconstructedString, int ConcatIndex, List<int> ProcessedIndices) 
            TryReconstructInterpolation(IList<Instruction> instructions, int startIndex)
        {
            const int maxWindow = 50;
            var processedIndices = new List<int>();

            // Szukaj String.Concat/Format w oknie
            for (int i = startIndex; i < Math.Min(startIndex + maxWindow, instructions.Count); i++)
            {
                var instr = instructions[i];

                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method)
                {
                    // ✅ Check if it's String.Concat or String.Format
                    if (method.DeclaringType?.FullName == "System.String" && 
                        (method.Name == "Concat" || method.Name == "Format"))
                    {
                        // ✅ Get parameter count
                        int paramCount = method.MethodSig?.Params.Count ?? 0;
                        
                        // ✅ Cofnij się i zbierz TYLKO argumenty na stosie
                        var args = CollectStackArguments(instructions, i, paramCount);
                        var reconstructed = string.Join("", args);

                        if (!string.IsNullOrEmpty(reconstructed))
                        {
                            // Mark all Ldstr between startIndex and concatIndex as processed
                            for (int j = startIndex; j <= i; j++)
                            {
                                if (instructions[j].OpCode == OpCodes.Ldstr)
                                    processedIndices.Add(j);
                            }

                            return (true, reconstructed, i, processedIndices);
                        }
                    }

                    // Stop jeśli natrafimy na inny API call
                    if (IsNetworkApiCall(method.FullName))
                        break;
                }
            }

            return (false, string.Empty, -1, processedIndices);
        }

        /// <summary>
        /// ✅ NEW: Collects exactly N arguments from stack before the call
        /// Walks backwards and collects Ldstr and resolved Ldloc values
        /// </summary>
        private List<string> CollectStackArguments(IList<Instruction> instructions, int callIndex, int paramCount)
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
                if (IsVariableLoad(instr))
                {
                    var value = TryGetVariableValue(instructions, i);
                    if (!string.IsNullOrEmpty(value))
                    {
                        args.Insert(0, value);
                    }
                    else
                    {
                        // ✅ Jeśli nie możemy znaleźć wartości, może to być char
                        var charValue = TryGetCharVariableValue(instructions, i);
                        if (charValue.HasValue)
                            args.Insert(0, charValue.Value.ToString());
                    }
                    depth++;
                    continue;
                }

                // ✅ Stop jeśli natrafimy na inną operację która zmienia stos
                if (IsStackChangingOperation(instr))
                    break;
            }

            return args;
        }

        /// <summary>
        /// ✅ NEW: Tries to get char value from variable
        /// Useful for Pattern 3 (char building) in interpolations
        /// </summary>
        private char? TryGetCharVariableValue(IList<Instruction> instructions, int loadIndex)
{
    var loadInstr = instructions[loadIndex];
    var varIndex = ExtractVariableIndex(loadInstr);

    if (varIndex == -1)
        return null;

    // ✅ Adaptive: Szukaj do początku metody LUB max 300 instrukcji
    int startSearchFrom = Math.Max(0, loadIndex - 300);
    
    for (int i = loadIndex - 1; i >= startSearchFrom; i--)
    {
        var instr = instructions[i];

        if (IsStoreToVariable(instr, varIndex))
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
                    
                    if (charCode.HasValue && IsCharacterCode(charCode.Value))
                    {
                        return (char)charCode.Value;
                    }
                }
            }
            
            // ✅ Znaleziono store ale bez Ldc_I4 - zmienna może być parametrem lub field
            // STOP tutaj (nie szukaj dalej)
            return null;
        }
    }

    return null;
}

        /// <summary>
        /// ✅ NEW: Checks if instruction changes stack in non-trivial way
        /// </summary>
        private bool IsStackChangingOperation(Instruction instr)
        {
            return instr.OpCode == OpCodes.Call ||
                   instr.OpCode == OpCodes.Callvirt ||
                   instr.OpCode == OpCodes.Newobj ||
                   instr.OpCode == OpCodes.Ret ||
                   instr.OpCode == OpCodes.Br ||
                   instr.OpCode == OpCodes.Br_S;
        }

        /// <summary>
        /// Tries to find what value is stored in variable
        /// Cofnij się i szukaj Ldstr przed Stloc tego parametru
        /// </summary>
        private string? TryGetVariableValue(IList<Instruction> instructions, int loadIndex)
        {
            var loadInstr = instructions[loadIndex];
            var varIndex = ExtractVariableIndex(loadInstr);

            if (varIndex == -1)
                return null;

            // Cofnij się i szukaj Stloc/Stloc_X tego samego indeksu
            for (int i = loadIndex - 1; i >= 0 && i >= loadIndex - 30; i--)
            {
                var instr = instructions[i];

                // Szukaj store do tej samej zmiennej
                if (IsStoreToVariable(instr, varIndex))
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

        /// <summary>
        /// Extracts variable index from load instruction
        /// </summary>
        private int ExtractVariableIndex(Instruction instr)
        {
            return instr.OpCode.Name switch
            {
                "ldloc.0" => 0,
                "ldloc.1" => 1,
                "ldloc.2" => 2,
                "ldloc.3" => 3,
                "ldloc.s" => (instr.Operand as Local)?.Index ?? -1,
                "ldloc" => (instr.Operand as Local)?.Index ?? -1,
                _ => -1
            };
        }

        /// <summary>
        /// Checks if instruction stores to specific variable index
        /// </summary>
        private bool IsStoreToVariable(Instruction instr, int varIndex)
        {
            bool result = instr.OpCode.Name switch
            {
                "stloc.0" => varIndex == 0,
                "stloc.1" => varIndex == 1,
                "stloc.2" => varIndex == 2,
                "stloc.3" => varIndex == 3,
                "stloc.s" => (instr.Operand as Local)?.Index == varIndex,
                "stloc" => (instr.Operand as Local)?.Index == varIndex,
                _ => false
            };
            
            // ✅ DEBUG
            if (instr.OpCode.Name.StartsWith("stloc") && varIndex >= 4)
            {
                Console.WriteLine($"              → IsStoreToVariable check: opcode={instr.OpCode.Name}, operand={instr.Operand}, operandType={instr.Operand?.GetType().Name}, targetVarIndex={varIndex}, result={result}");
                if (instr.Operand is Local loc)
                    Console.WriteLine($"                Local.Index={loc.Index}");
            }
            
            return result;
        }

        /// <summary>
        /// Finds network API call within instruction window after index
        /// </summary>
        private string? FindNetworkApiCall(IList<Instruction> instructions, int startIndex)
        {
            const int window = 8;
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                var instr = instructions[i];
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt || instr.OpCode == OpCodes.Newobj) &&
                    instr.Operand is IMethod method)
                {
                    if (IsNetworkApiCall(method.FullName))
                        return method.FullName;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds next variable store instruction within window
        /// </summary>
        private (int Index, Instruction Instruction)? FindNextVariableStore(
            IList<Instruction> instructions,
            int startIndex,
            int window)
        {
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                if (IsVariableStore(instructions[i]))
                    return (i, instructions[i]);
            }

            return null;
        }

        /// <summary>
        /// Finds API call that uses a variable after index
        /// </summary>
        private string? FindApiCallUsingVariable(
            IList<Instruction> instructions,
            int startIndex,
            int window)
        {
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                var instr = instructions[i];

                // Check if previous instruction loads a variable
                if (i > 0 && IsVariableLoad(instructions[i - 1]))
                {
                    if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt || instr.OpCode == OpCodes.Newobj) &&
                        instr.Operand is IMethod method)
                    {
                        if (IsNetworkApiCall(method.FullName))
                            return method.FullName;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds network API call after a specific index
        /// </summary>
        private string? FindNetworkApiCallAfterIndex(IList<Instruction> instructions, int startIndex, int window = 50)
        {
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                var instr = instructions[i];
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt || instr.OpCode == OpCodes.Newobj) &&
                    instr.Operand is IMethod method)
                {
                    if (IsNetworkApiCall(method.FullName))
                        return method.FullName;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a method is a network API call
        /// </summary>
        private bool IsNetworkApiCall(string methodFullName)
        {
            return NetworkApiCalls.Any(api => methodFullName.Contains(api));
        }

        /// <summary>
        /// Checks if an operand is a character code (32-126 = printable ASCII)
        /// </summary>
        private bool IsCharacterCode(object? operand)
        {
            if (operand is int code)
                return code >= 32 && code <= 126;

            return false;
        }

        /// <summary>
        /// Attempts to reconstruct a string from character loads
        /// </summary>
        private string ReconstructStringFromChars(IList<Instruction> instructions, int startIndex)
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

        /// <summary>
        /// Gets variable name from store instruction
        /// </summary>
        private string GetVariableName(Instruction instr)
        {
            return instr.OpCode.Name switch
            {
                "stloc.0" => "var_0",
                "stloc.1" => "var_1",
                "stloc.2" => "var_2",
                "stloc.3" => "var_3",
                "stloc.s" => "var_s",
                "stloc" => "var_local",
                "stfld" => "field",
                "stsfld" => "static_field",
                _ => instr.OpCode.Name
            };
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
        /// Determines finding type based on API call
        /// </summary>
        private ScanFindingType GetFindingType(string apiCall)
        {
            if (apiCall.Contains("HttpClient"))
                return ScanFindingType.HttpClientCall;
            if (apiCall.Contains("WebClient"))
                return ScanFindingType.WebClientCall;
            if (apiCall.Contains("Dns.GetHost"))
                return ScanFindingType.DnsCall;

            return ScanFindingType.Unknown;
        }

        /// <summary>
        /// Determines suspicious string type
        /// </summary>
        private ScanFindingType GetStringType(string evidence)
        {
            var lower = evidence.ToLowerInvariant();

            if (OnionRegex.IsMatch(lower))
                return ScanFindingType.SuspiciousOnionAddress;
            if (IsPrivateIP(lower))
                return ScanFindingType.SuspiciousIpAddress;
            if (Base64Regex.IsMatch(lower))
                return ScanFindingType.SuspiciousBase64;
            if (UrlRegex.IsMatch(lower))
                return ScanFindingType.SuspiciousUrl;

            return ScanFindingType.Unknown;
        }

        private string ReconstructFromInterpolatedHandler(IList<Instruction> instructions, int newObjIndex)
        {
            var parts = new List<string>();
            const int maxWindow = 100;
            
            Console.WriteLine($"    → ReconstructFromInterpolatedHandler starting at IL_{newObjIndex:X4}");

            // Szukaj AppendLiteral i AppendFormatted calls po newobj
            for (int i = newObjIndex + 1; i < Math.Min(newObjIndex + maxWindow, instructions.Count); i++)
            {
                var instr = instructions[i];
                
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method)
                {
                    Console.WriteLine($"      [IL_{i:X4}] Found AppendLiteral call");
                    // AppendLiteral(string)
                    if (method.Name == "AppendLiteral")
                    {
                        Console.WriteLine($"      [IL_{i:X4}] Found AppendLiteral call");
                        // Cofnij się i znajdź Ldstr argument
                        if (i > 0 && instructions[i - 1].OpCode == OpCodes.Ldstr &&
                            instructions[i - 1].Operand is string literal)
                        {
                            parts.Add(literal);
                            Console.WriteLine($"      [IL_{i:X4}] AppendLiteral: \"{literal}\"");
                        }
                        else
                        {
                            Console.WriteLine($"      [IL_{i:X4}] AppendLiteral: NO LDSTR FOUND (prev instr: {instructions[i - 1].OpCode.Name})");
                        }
                    }
                    // AppendFormatted(T) - char
                    else if (method.Name == "AppendFormatted")
                    {
                        Console.WriteLine($"      [IL_{i:X4}] Found AppendFormatted call");
                        // Cofnij się i znajdź Ldloc argument
                        if (i > 0)
                        {
                            var prevInstr = instructions[i - 1];
                            Console.WriteLine($"        → Prev instr: {prevInstr.OpCode.Name} (IsVariableLoad: {IsVariableLoad(prevInstr)})");

                            if (IsVariableLoad(prevInstr)) 
                            {
                                Console.WriteLine($"        → Trying to get char value from IL_{i - 1:X4}");
                                var charValue = TryGetCharVariableValue(instructions, i - 1);
                                
                                if (charValue.HasValue)
                                {
                                    parts.Add(charValue.Value.ToString());
                                    Console.WriteLine($"      [IL_{i:X4}] AppendFormatted: '{charValue.Value}' (code: {(int)charValue.Value})");
                                }
                                else
                                {
                                    Console.WriteLine($"      [IL_{i:X4}] AppendFormatted: CHAR VALUE NOT FOUND!");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"        → Previous instruction is NOT variable load");
                            }
                        }
                    }
                    // ToStringAndClear = koniec
                    else if (method.Name == "ToStringAndClear")
                    {
                        Console.WriteLine($"      [IL_{i:X4}] Found ToStringAndClear - stopping");
                        break;
                    }
                }
            }
            
            Console.WriteLine($"    → Total parts collected: {parts.Count}");
            return string.Join("", parts);
        }

        private int FindToStringAndClear(IList<Instruction> instructions, int startIndex)
        {
            const int maxWindow = 100;
            
            for (int i = startIndex; i < Math.Min(startIndex + maxWindow, instructions.Count); i++)
            {
                var instr = instructions[i];
                
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method &&
                    method.Name == "ToStringAndClear")
                {
                    return i;
                }
            }
            
            return -1;
        }


        /// <summary>
        /// Creates a FindingSummaryDto object with all details
        /// </summary>
        private FindingSummaryDto CreateFinding(
            string evidence,
            string? apiCall,
            TypeDef type,
            MethodDef method,
            int instructionIndex,
            int hopDepth,
            bool isLiteral,
            List<string> flowTrace)
        {
            var findingType = GetFindingType(apiCall ?? string.Empty);
            if (findingType == ScanFindingType.Unknown)
                findingType = GetStringType(evidence);

            var dangerLevel = CalculateDangerLevel(findingType);
            var confidenceScore = CalculateConfidenceScore(hopDepth, isLiteral);

            return new FindingSummaryDto
            {
                Type = findingType,
                DangerLevel = dangerLevel,
                ConfidenceScore = confidenceScore,
                Evidence = evidence,
                Location = $"{type.FullName}::{method.Name}, IL_{instructionIndex:X4}",
                RawData = $"API Call: {apiCall}",
                FlowTrace = flowTrace,
                HopDepth = hopDepth
            };
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
    }
}