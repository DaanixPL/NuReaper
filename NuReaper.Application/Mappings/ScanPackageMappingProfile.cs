using AutoMapper;
using NuReaper.Application.DTOs;
using NuReaper.Application.DTOs.Graph;
using NuReaper.Application.Responses;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.Graph;

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
            CreateMap<DependencyGraph, DependencyGraphDto>();
            CreateMap<GraphNode, GraphNodeDto>();
            CreateMap<GraphEdge, GraphEdgeDto>();
            CreateMap<Cycle, CycleDto>();
        }
    }
}