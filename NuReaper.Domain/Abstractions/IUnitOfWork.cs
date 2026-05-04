namespace NuReaper.Domain.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        IScanRepository Scans { get; }
        IPackageRepository Packages { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
