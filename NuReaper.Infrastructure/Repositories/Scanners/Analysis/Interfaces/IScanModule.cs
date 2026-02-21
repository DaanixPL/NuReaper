using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface IScanModule
    {
        public List<FindingSummaryDto> Execute(string filePath);
    }
}
