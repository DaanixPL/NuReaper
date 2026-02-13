using MediatR;
using NuReaper.Application.DTOs;
using NuReaper.Application.Responses;

namespace NuReaper.Application.Commands.ScanPackage
{
    public class ScanPackageCommandHandler : IRequestHandler<ScanPackageCommand, ScanPackageResultResponse>
    {
        public Task<ScanPackageResultResponse> Handle(ScanPackageCommand request, CancellationToken cancellationToken)
        {
            string urlToDownload = "";
            // Dodac logike skanowania jej i zwracania wyniku
            try
            {
                urlToDownload = request.url.Replace("nuget.org/packages", "nuget.org/api/v2/package");

                if (!urlToDownload.StartsWith("https://www.nuget.org/api/v2/package/"))
                {
                    throw new ArgumentException("Invalid URL format. Expected format: https://www.nuget.org/packages/{packageId}/{version}");
                }
            }
            catch (Exception ex)
            {
                // Logowanie błędu
                Console.WriteLine($"Error processing ScanPackageCommand: {ex.Message}"); // zanueb na logger
                throw; // Rzucenie wyjątku dalej, aby mógł być obsłużony przez globalny handler
            }

            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaper/1.0");

            HttpResponseMessage response = httpClient.GetAsync(urlToDownload, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result;
            response.EnsureSuccessStatusCode();

            string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                    ?? response.Content.Headers.ContentDisposition?.FileName
                    ?? Path.GetFileName(urlToDownload) + ".nupkg";


            string tempDir = Path.Combine(Path.GetTempPath(), "NuReaperScans");
            Directory.CreateDirectory(tempDir);

            string tempFilePath = Path.Combine(tempDir, fileName);

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                response.Content.CopyToAsync(fileStream).Wait(cancellationToken);
            }
            Console.WriteLine($"Package downloaded to: {tempFilePath}");
            // Tutaj dodac logike skanowania paczki i zwracania wyniku

            return Task.FromResult(new ScanPackageResultResponse
            {
                PackageName = "ExamplePackage",
                Version = "1.0.0",
                Author = "ExampleAuthor",

                Sha256Hash = "example-sha256-hash",
                Downloads = 1000,
                FileSize = 1024 * 1024,

                ThreatLevel = 0.75f,

                Findings = new List<FindingSummaryDto>
                {
                   new FindingSummaryDto
                    {
                        Type           = "ObfuscatedCode",
                        DangerousLevel = 85.0f,
                        Evidence       = "Wykryto mocno zaciemniony kod w assembly lib/net8.0/malware.dll",
                        RawData        = "Il9pbmdyZXNzXCI6ICJodHRwczovL21hbGljaW91cy5leGFtcGxlLmNvbSIs",
                        Location       = "lib/net8.0/malware.dll:method:ObfuscatedMethod"
                    },
                    new FindingSummaryDto
                    {
                        Type           = "NetworkBeacon",
                        DangerousLevel = 92.0f,
                        Evidence       = "Podejrzane połączenie C2 do domeny .onion",
                        RawData        = null,
                        Location       = "lib/netstandard2.0/stealer.exe"
                    }
                }
            });
        }
    }
}