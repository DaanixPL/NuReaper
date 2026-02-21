using NuReaper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Context.Configurations
{
    public class PackageConfiguration : IEntityTypeConfiguration<Package>
    {
        public void Configure(EntityTypeBuilder<Package> builder)
        {
        // dodac konfiguracje encji Package i utworzyc nowy plik z konfiguracja dla Scan i ScanFinding 
        }
    }
} 