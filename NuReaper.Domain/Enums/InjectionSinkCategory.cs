namespace NuReaper.Domain.Enums
{
    public enum InjectionSinkCategory
    {
        ProcessManipulation,    // Process create, shell exec
        MemoryManipulation,     // VirtualAlloc, WriteProcessMemory, Marshal.Copy
        CodeExecution,          // CreateRemoteThread, CreateThread
        DynamicLoading,         // LoadLibrary, Assembly.Load
        ScriptExecution,        // PowerShell, CodeDom
        HandleAccess,           // OpenProcess, DuplicateHandle
        FiberInjection,         // CreateFiber, ConvertThreadToFiber
        SectionMapping,         // NtCreateSection, MapViewOfFile
    }
}