using AutoMapper;
using NuReaper.Application.DTOs;
using NuReaper.Application.DTOs.Graph;
using NuReaper.Application.Responses;
using NuReaper.Domain.Entities;
 
namespace NuReaper.Application.Mappings
{
    public class ScanPackageMappingProfile : Profile
    {
        public ScanPackageMappingProfile()
        {
            CreateMap<ScanPackageResult, ScanPackageResultResponse>();
            CreateMap<Package, PackageDto>()
                .ForMember(dest => dest.ScannedTime, opt => opt.MapFrom(src => src.LastScanDate))
                .ForMember(dest => dest.Findings, opt => opt.MapFrom(src => src.GetLatestFindings()));
            CreateMap<Domain.Entities.Graph.DependencyGraph, DependencyGraphDto>();
            CreateMap<Domain.Entities.Graph.GraphNode, GraphNodeDto>();
            CreateMap<Domain.Entities.Graph.GraphEdge, GraphEdgeDto>();
            CreateMap<Domain.Entities.Graph.Cycle, CycleDto>();
        }
    }
}