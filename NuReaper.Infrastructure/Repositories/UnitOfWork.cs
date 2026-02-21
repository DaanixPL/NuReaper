using NuReaper.Domain.Abstractions;
using App.Infrastructure.Context;

namespace NuReaper.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
    {
        private readonly AppDbContext _context;
        private readonly IScanRepository _scanRepository;
        private readonly IPackageRepository _packageRepository;


        private bool _disposed = false;


        public UnitOfWork(AppDbContext context, IScanRepository scanRepository, IPackageRepository packageRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _scanRepository = scanRepository ?? throw new ArgumentNullException(nameof(scanRepository));
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }
        public IScanRepository Scans => _scanRepository;
        public IPackageRepository Packages => _packageRepository;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) 
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsyncCore();
        }
        private async ValueTask DisposeAsyncCore()
        {
            if (_disposed) return;

            await _context.DisposeAsync().ConfigureAwait(false);

            _disposed = true;
        }

        ~UnitOfWork()
        {
            Dispose(disposing: false);
        }
    }
}