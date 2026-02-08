using AutoMapper;
using FiscalDocAPI.Application.DTOs;
using FiscalDocAPI.Domain.Entities;

namespace FiscalDocAPI.Application.Mappings;

public class MappingProfile : Profile
{
  public MappingProfile()
  {
    CreateMap<FiscalDocument, DocumentSummaryDto>()
        .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.Id));

    CreateMap<FiscalDocument, DocumentDetailDto>();
  }
}
