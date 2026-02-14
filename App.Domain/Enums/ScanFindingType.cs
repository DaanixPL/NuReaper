using System.ComponentModel;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using NuReaper.Domain.Entities;

namespace NuReaper.Domain.Enums
{
    public enum ScanFindingType
    {
        // Network APIs
        HttpClientCall,
        WebClientCall,
        DnsCall,
        
        // Code Execution
        ProcessStartCall,
        AssemblyLoadCall,
        AssemblyLoadFromCall,
        AssemblyLoadFileCall,
        ActivatorCreateInstanceCall,
        
        // Low-level / P/Invoke
        DllImportAttribute,
        VirtualAllocCall,
        CreateThreadCall,
        NtCreateThreadExCall,
        
        // WMI / System
        ManagementObjectSearcherCall,
        
        // String building (suspicious patterns)
        StringConcatenation,        // "http://" + var
        CharArrayConstruction,      // char[] + new string()
        StringInterpolation,        // $"{var1}{var2}..."
        StringBuilderUsage,         // StringBuilder.Append()
        
        // Suspicious strings detected
        SuspiciousUrl,
        SuspiciousIpAddress,
        SuspiciousOnionAddress,
        SuspiciousBase64,
        
        Unknown
    }
}