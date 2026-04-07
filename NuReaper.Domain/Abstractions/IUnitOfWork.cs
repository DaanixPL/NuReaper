namespace NuReaper.Domain.Abstractions
{
    public interface IUnitOfWork : IDisposable
    {
        public IScanRepository ScanRepository { get; }
        public IDependencyRepository DependencyRepository { get; }
        public IPackageRepository PackageRepository { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
