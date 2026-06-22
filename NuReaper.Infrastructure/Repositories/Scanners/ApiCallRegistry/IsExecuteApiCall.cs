namespace NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry
{
    public static class IsExecuteApiCall
    {
        private static readonly string[] ExecutionSinks = new[]
        {
            // ========== Process / Shell Execution ==========
            "Process::Start",
            "Process::BeginOutputReadLine",
            "Process::BeginErrorReadLine",
            "ProcessStartInfo::.ctor",
            "ProcessStartInfo::set_FileName",
            "ProcessStartInfo::set_Arguments",
            "ProcessStartInfo::set_UseShellExecute",
            "ProcessStartInfo::set_RedirectStandardOutput",
            "ProcessStartInfo::set_RedirectStandardInput",
            "ProcessStartInfo::set_RedirectStandardError",
            "ProcessStartInfo::set_CreateNoWindow",
            "ProcessStartInfo::set_WindowStyle",

            // ========== P/Invoke / Native Execution (WinAPI) ==========
            "VirtualAlloc",
            "VirtualAllocEx",
            "VirtualProtect",
            "VirtualProtectEx",
            "WriteProcessMemory",
            "ReadProcessMemory",
            "CreateRemoteThread",
            "CreateRemoteThreadEx",
            "NtCreateThreadEx",
            "RtlCreateUserThread",
            "QueueUserAPC",
            "SetThreadContext",
            "ResumeThread",
            "SuspendThread",
            "OpenProcess",
            "OpenThread",
            "ShellExecute",
            "ShellExecuteEx",
            "ShellExecuteA",
            "ShellExecuteW",
            "CreateProcess",
            "CreateProcessA",
            "CreateProcessW",
            "CreateProcessAsUser",
            "WinExec",
            "LoadLibrary",
            "LoadLibraryA",
            "LoadLibraryW",
            "LoadLibraryEx",
            "GetProcAddress",
            "NtWriteVirtualMemory",
            "NtAllocateVirtualMemory",
            "NtProtectVirtualMemory",
            "NtCreateSection",
            "NtMapViewOfSection",
            "NtUnmapViewOfSection",
            "MapViewOfFile",
            "MapViewOfFileEx",
            "CreateFileMapping",
            "ImpersonateLoggedOnUser",
            "DuplicateToken",
            "DuplicateTokenEx",
            "OpenProcessToken",
            "AdjustTokenPrivileges",
            "SetThreadToken",
            "CreateProcessWithTokenW",
            "CreateProcessWithLogonW",
            "LogonUser",
            "LogonUserA",
            "LogonUserW",

            // ========== Reflection-based Execution ==========
            "MethodBase::Invoke",
            "MethodInfo::Invoke",
            "ConstructorInfo::Invoke",
            "Delegate::DynamicInvoke",
            "Activator::CreateInstance",
            "Activator::CreateInstanceFrom",
            "Assembly::Load",
            "Assembly::LoadFrom",
            "Assembly::LoadFile",
            "Assembly::LoadWithPartialName",
            "AppDomain::Load",
            "AppDomain::ExecuteAssembly",
            "AppDomain::CreateInstanceAndUnwrap",
            "CSharpCodeProvider::CompileAssemblyFromSource",
            "CodeDomProvider::CompileAssemblyFromSource",
            "CSharpCodeProvider::CompileAssemblyFromFile",
            "CompilerParameters::.ctor",

            // ========== Scripting / Dynamic Code Execution ==========
            "PowerShell::Create",
            "PowerShell::AddScript",
            "PowerShell::AddCommand",
            "PowerShell::Invoke",
            "PowerShell::InvokeAsync",
            "RunspaceFactory::CreateRunspace",
            "Runspace::Open",
            "Pipeline::Invoke",
            "CSharpScript::EvaluateAsync",
            "CSharpScript::RunAsync",
            "ScriptOptions::WithReferences",
            "BuildManager::Build",
            "Project::Build",

            // ========== COM / OLE Execution ==========
            "Type::GetTypeFromProgID",
            "Type::GetTypeFromCLSID",
            "Activator::CreateInstance",
            "Marshal::GetActiveObject",
            "Marshal::BindToMoniker",
            "Marshal::GetComObjectData",
            "CoCreateInstance",
            "CoGetObject",
            "CLRCreateInstance",

            // ========== Scheduled Tasks / Persistence ==========
            "ITaskService::Connect",
            "ITaskFolder::RegisterTaskDefinition",
            "ITaskDefinition::Actions",
            "IExecAction::set_Path",
            "RegistryKey::SetValue",
            "Registry::SetValue",
            "RegSetValueEx",
            "RegSetValueExA",
            "RegSetValueExW",
            "RegCreateKeyEx",
            "RegCreateKeyExA",
            "RegCreateKeyExW",

            // ========== Memory Manipulation / Unsafe Execution ==========
            "Marshal::Copy",
            "Marshal::AllocHGlobal",
            "Marshal::AllocCoTaskMem",
            "Marshal::WriteByte",
            "Marshal::WriteInt32",
            "Marshal::WriteInt64",
            "Marshal::GetFunctionPointerForDelegate",
            "GCHandle::AddrOfPinnedObject",
            "Buffer::MemoryCopy",

            // ========== Deserialization (RCE) ==========
            "BinaryFormatter::Deserialize",
            "BinaryFormatter::UnsafeDeserialize",
            "SoapFormatter::Deserialize",
            "NetDataContractSerializer::Deserialize",
            "LosFormatter::Deserialize",
            "ObjectStateFormatter::Deserialize",
            "JavaScriptSerializer::Deserialize",
            "XmlSerializer::Deserialize",
            "DataContractSerializer::ReadObject",
            "JsonConvert::DeserializeObject",
            "JsonSerializer::Deserialize",
            "XamlReader::Load",
            "XamlReader::Parse",
            "ActivitySurrogateSelector",

            // ========== File-based Execution Triggers ==========
            "File::WriteAllBytes",
            "File::WriteAllText",
            "File::Copy",
            "File::Move",
            "FileInfo::CopyTo",
            "FileInfo::MoveTo",
            "ZipFile::ExtractToDirectory",
            "ZipArchive::ExtractToFile",
            "NativeLibrary::Load",
            "NativeLibrary::TryLoad",
        };

        private static readonly HashSet<string> ExecutionSinksSet;
        static IsExecuteApiCall()
        {
            ExecutionSinksSet = new HashSet<string>(ExecutionSinks, StringComparer.OrdinalIgnoreCase);
        }
        public static bool Execute(string methodFullName)
        {
            Console.WriteLine("Checking if node is execution sink: " + methodFullName);
            if (string.IsNullOrEmpty(methodFullName))
            {
                Console.WriteLine("Method full name is null or empty.");
                return false;
            }

            if (ExecutionSinksSet.Contains(methodFullName))
                return true;

            int parenIndex = methodFullName.IndexOf('(');
            string withoutParams = parenIndex >= 0
                ? methodFullName.Substring(0, parenIndex)
                : methodFullName;

            int lastSeparator = withoutParams.LastIndexOf("::", StringComparison.Ordinal);
            if (lastSeparator != -1)
            {
                // Extract just the method name (after ::)
                string cleanMethodName = withoutParams.Substring(lastSeparator + 2);
                if (ExecutionSinksSet.Contains(cleanMethodName))
                    return true;

                // Extract "ClassName::MethodName" (last two segments before params)
                string afterReturnType = withoutParams;
                int spaceBeforeClass = withoutParams.LastIndexOf(' ');
                if (spaceBeforeClass >= 0)
                    afterReturnType = withoutParams.Substring(spaceBeforeClass + 1);

                // e.g. "Process::Start"
                string classAndMethod = afterReturnType;
                int classStart = afterReturnType.LastIndexOf('.', lastSeparator - (withoutParams.Length - afterReturnType.Length));

                // Build "ShortClass::Method" for sink matching
                // Walk back to find the last dot before ::
                int separatorInShort = afterReturnType.LastIndexOf("::", StringComparison.Ordinal);
                if (separatorInShort > 0)
                {
                    int dotBeforeClass = afterReturnType.LastIndexOf('.', separatorInShort - 1);
                    string shortForm = dotBeforeClass >= 0
                        ? afterReturnType.Substring(dotBeforeClass + 1)
                        : afterReturnType;

                    if (ExecutionSinksSet.Contains(shortForm))
                        return true;
                }
            }
            return false;
        }
    }
}
