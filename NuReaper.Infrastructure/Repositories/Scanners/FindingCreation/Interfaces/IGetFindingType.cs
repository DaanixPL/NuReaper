using NuReaper.Domain.Enums;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces
{
    public interface IGetFindingType
    {
        public ScanFindingType Execute(string apiCall);
    }
}
