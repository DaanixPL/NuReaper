# NuReaper — NuGet Package Security Scanner

> ⚠️ **Work in Progress** — The project is actively developed. Core scanning engine is functional; database persistence, caching, and full test coverage are planned for upcoming iterations.

**NuReaper** is a .NET 8 backend API that automatically analyzes the security of NuGet packages. You provide a link to any NuGet package, and the system recursively downloads its entire dependency tree, decompiles the compiled code, and scans every method for malicious patterns — returning a detailed threat report.

> 🔍 **For non-technical readers:** Think of it like installing an app on your phone. Along with it, dozens of smaller libraries get installed automatically. NuReaper inspects every single one of them — looking at the actual compiled binary code — and detects whether any of them try to connect to suspicious servers, execute hidden code, or obfuscate their true behavior.

---

## 🎯 What does NuReaper do?

Send a single HTTP request with a NuGet package URL, and NuReaper will:

1. **Build the full dependency tree** — recursively fetches and maps all packages the target depends on (up to 20 levels deep), detecting circular dependencies along the way
2. **Download and verify each package** — fetches the `.nupkg` file and computes its SHA-256 hash
3. **Analyze compiled IL code** — decompiles .NET assemblies and scans every method of every class for suspicious patterns
4. **Score the threat level** — based on detected patterns, finding count, type diversity, confidence scores, and obfuscation depth
5. **Return a full report** — with per-package findings, code flow traces, and the complete dependency graph

---

## 🔬 Detected Malicious Code Patterns

The engine analyzes code at the IL (Intermediate Language) instruction level and recognizes **7 patterns** characteristic of malicious software:

| # | Pattern | Description |
|---|---------|-------------|
| 1 | **Direct API Call** | Suspicious string immediately used in a network call |
| 2 | **String Assigned** | URL assigned to a variable, then passed to a network call |
| 3 | **String Construction** | URL built by concatenation (`"http://" + var + "/path"`) — obfuscation technique |
| 4 | **String Interpolation** | URL assembled via interpolation (`$"{part1}{part2}"`) |
| 5 | **Char-Only Interpolation** | URL built char-by-char from a char array — advanced obfuscation |
| 6 | **DefaultInterpolatedStringHandler** | Low-level interpolation mechanism used to hide strings from static analysis |
| 7 | **Bare API Calls** | Direct network calls without explicit strings (`TcpClient.Connect`, `Socket.Connect`, `Process.Start`) |

### Detected Threat Types

```
Network:      HttpClient, WebClient, DNS, TcpClient, WebSocket
Execution:    Process.Start, Assembly.Load, Activator.CreateInstance
Low-level:    DllImport, VirtualAlloc, CreateThread, NtCreateThreadEx
System:       ManagementObjectSearcher (WMI)
String-based: Concatenation, CharArray, Interpolation, StringBuilder
Suspicious:   URL, IP address, .onion address, Base64
```

### Threat Level Score (0–100)

The scoring algorithm accounts for:
- Maximum danger level of any single finding
- Total number of findings (up to +30 pts)
- Diversity of threat types detected (up to +15 pts)
- Average confidence score across findings (+5 pts if >90%)
- Presence of obfuscation — multi-hop string construction (+10 pts)

---

## 📡 API

### `POST /api/PackageScan`

Scans the given NuGet package along with its full dependency tree.

**Request:**
```json
{
  "url": "https://www.nuget.org/packages/System.Net.Http/4.3.4"
}
```

**Response:**
```json
{
  "rootPackageName": "System.Net.Http",
  "rootPackageVersion": "4.3.4",
  "totalPackages": 12,
  "totalFindingsFromAllPackages": 5,
  "threatLevelAllPackages": 47.5,
  "scannedTimeAllPackages": "2026-03-25T12:00:00Z",
  "packages": [
    {
      "packageName": "System.Net.Http",
      "version": "4.3.4",
      "sha256Hash": "abc123...",
      "threatLevel": 35.0,
      "totalFindings": 3,
      "findings": [
        {
          "type": "HttpClientCall",
          "confidenceScore": 95.0,
          "dangerLevel": 60.0,
          "evidence": "http://example.com/payload",
          "location": "MyClass::SendData",
          "hopDepth": 0,
          "flowTrace": "[Pattern1] Direct API call using string..."
        }
      ]
    }
  ],
  "dependencyGraph": {
    "rootPackage": "System.Net.Http@4.3.4",
    "nodes": [...],
    "edges": [...],
    "cycles": [...]
  }
}
```

---

## 🏗️ Architecture

Built on **Clean Architecture** with a strict separation of concerns:

```
NuReaperBackend/
├── NuReaper.Api/               # API layer — controllers, middleware, startup
├── NuReaper.Application/       # Application layer — commands, interfaces, DTOs, validation
├── NuReaper.Domain/            # Domain core — entities, enums, abstractions
├── NuReaper.Infrastructure/    # Implementations — scanners, parsers, repositories
│   └── Repositories/
│       ├── Scanners/           # IL analysis engine
│       │   ├── Analysis/       # ScanModule, ScanMethod, NetworkApiCallScan
│       │   ├── Detectors/      # 7 pattern detectors (Pattern1–Pattern7)
│       │   ├── Patterns/       # PatternRegistry (regex: URL, IP, .onion, Base64)
│       │   ├── RiskCalculation/# Threat scoring algorithm
│       │   ├── Finders/        # Locating API calls within IL instructions
│       │   ├── VariableAnalysis/# Tracking variable values across IL
│       │   └── StringAnalysis/ # Reconstructing strings from fragments
│       ├── Parsers/            # .nuspec file parsing
│       ├── GraphBuilders/      # Dependency graph construction (BFS + recursion)
│       └── FileHelpers/        # Download, extract, SHA-256
└── Docker/
    ├── Dockerfile
    └── docker-compose.yml
```

### Stack & Patterns

| Area | Technology / Pattern |
|------|---------------------|
| Architecture | Clean Architecture |
| Inter-layer communication | CQRS + MediatR |
| IL code analysis | dnlib |
| Input validation | FluentValidation + MediatR Pipeline Behavior |
| Object mapping | AutoMapper |
| Logging | Serilog (console + rolling daily file) |
| API documentation | Swagger / OpenAPI |
| Database | Entity Framework Core + MySQL (Pomelo) |
| HTTP | IHttpClientFactory with connection pooling |
| Containerization | Docker + Docker Compose |
| Testing | xUnit + Moq |

---

## ⚙️ How the Scanning Engine Works

```
Package URL
    │
    ▼
DependencyGraphBuilder          ← builds dependency graph from .nuspec files
    │  (recursion + BFS, cycle detection)
    ▼
List of unique packages
    │
    ▼  (parallel, SemaphoreSlim — CPU-1 threads)
NetworkApiCallScan per package
    ├── DownloadPackageAsync     ← fetches .nupkg
    ├── CalculateSha256          ← integrity verification
    ├── ExtractNupkgAsync        ← unpacks the archive
    └── GetAssemblyFiles         ← lists .dll files
            │
            ▼  (Parallel.ForEachAsync)
        ScanModule (per .dll)
            │
            ▼  (Parallel.ForEach — all types and methods)
        ScanMethod (per method)
            │
            ▼  (7 pattern detectors)
        Pattern1..7 → FindingSummaryDto
            │
            ▼
    CalculateThreatLevel         ← score 0–100
    │
    ▼
ScanPackageResultResponse
```

---

## 📦 Key Dependencies

| Package | Purpose |
|---------|---------|
| `dnlib` | .NET IL decompilation and analysis |
| `MediatR` | CQRS — command/query pipeline |
| `FluentValidation` | Request validation |
| `AutoMapper` | Cross-layer object mapping |
| `Serilog` | Structured logging |
| `Pomelo.EntityFrameworkCore.MySql` | MySQL ORM |
| `Swashbuckle.AspNetCore` | Swagger UI |
| `xUnit` + `Moq` | Unit testing |

---


## 📄 License

This project is available under the PolyForm Noncommercial License 1.0.0, commercial use is prohibited.
For commercial licensing or inquiries, contact: vdanix.contact@gmail.com
