using AutoMapper;
using NuReaper.Application.DTOs;
using NuReaper.Application.Responses;
using NuReaper.Domain.Entities;

namespace NuReaper.Application.Mappings
{
    public class PackagesMappingProfile : Profile
    {
        public PackagesMappingProfile()
        {
            CreateMap<PackageDto, Package>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            
            CreateMap<ScanPackageResultResponse, List<Package>>()
                .ConvertUsing((src, dest, context) => 
                    context.Mapper.Map<List<Package>>(src.Packages));
        }
    }
}
