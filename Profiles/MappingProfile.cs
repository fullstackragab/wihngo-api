using AutoMapper;
using Wihngo.Dtos;
using Wihngo.Models;

namespace Wihngo.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserCreateDto, User>();
            CreateMap<User, UserReadDto>();

            CreateMap<BirdCreateDto, Bird>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Species, opt => opt.MapFrom(src => src.Species))
                .ForMember(dest => dest.Tagline, opt => opt.MapFrom(src => src.Tagline))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageS3Key))
                .ForMember(dest => dest.VideoUrl, opt => opt.MapFrom(src => src.VideoS3Key))
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.BirdId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<Bird, BirdProfileDto>()
                .ForMember(dest => dest.CommonName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ScientificName, opt => opt.MapFrom(src => src.Species))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Emoji, opt => opt.MapFrom(src => "??"))
                .ForMember(dest => dest.Tagline, opt => opt.MapFrom(src => src.Tagline))
                .ForMember(dest => dest.ImageS3Key, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.VideoS3Key, opt => opt.MapFrom(src => src.VideoUrl))
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set by controller
                .ForMember(dest => dest.VideoUrl, opt => opt.Ignore()) // Set by controller
                .ForMember(dest => dest.Personality, opt => opt.Ignore())
                .ForMember(dest => dest.Conservation, opt => opt.Ignore())
                .ForMember(dest => dest.FunFacts, opt => opt.Ignore())
                .ForMember(dest => dest.LovedBy, opt => opt.MapFrom(src => src.LovedCount))
                .ForMember(dest => dest.SupportedBy, opt => opt.MapFrom(src => src.SupportedCount));

            CreateMap<Bird, BirdSummaryDto>()
                .ForMember(dest => dest.BirdId, opt => opt.MapFrom(src => src.BirdId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Species, opt => opt.MapFrom(src => src.Species))
                .ForMember(dest => dest.ImageS3Key, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.VideoS3Key, opt => opt.MapFrom(src => src.VideoUrl))
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set by controller
                .ForMember(dest => dest.VideoUrl, opt => opt.Ignore()) // Set by controller
                .ForMember(dest => dest.Tagline, opt => opt.MapFrom(src => src.Tagline))
                .ForMember(dest => dest.LovedBy, opt => opt.MapFrom(src => src.LovedCount))
                .ForMember(dest => dest.SupportedBy, opt => opt.MapFrom(src => src.SupportedCount))
                .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId));

            CreateMap<User, UserSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
        }
    }
}
