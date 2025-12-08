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

            CreateMap<Bird, BirdProfileDto>()
                .ForMember(dest => dest.CommonName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ScientificName, opt => opt.MapFrom(src => src.Species))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Emoji, opt => opt.MapFrom(src => "??"))
                .ForMember(dest => dest.Tagline, opt => opt.MapFrom(src => src.Tagline))
                .ForMember(dest => dest.Personality, opt => opt.Ignore())
                .ForMember(dest => dest.Conservation, opt => opt.Ignore())
                .ForMember(dest => dest.FunFacts, opt => opt.Ignore())
                .ForMember(dest => dest.LovedBy, opt => opt.MapFrom(src => src.LovedCount))
                .ForMember(dest => dest.SupportedBy, opt => opt.MapFrom(src => src.SupportedCount));

            CreateMap<Story, StorySummaryDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Content.Length > 30 ? src.Content.Substring(0, 30) + "..." : src.Content))
                .ForMember(dest => dest.Bird, opt => opt.MapFrom(src => src.Bird != null ? src.Bird.Name : string.Empty))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.CreatedAt.ToString("MMMM d, yyyy")))
                .ForMember(dest => dest.Preview, opt => opt.MapFrom(src => src.Content.Length > 140 ? src.Content.Substring(0, 140) + "..." : src.Content));

            CreateMap<Story, StoryReadDto>()
                .ForMember(dest => dest.Bird, opt => opt.MapFrom(src => src.Bird))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author));

            CreateMap<Bird, BirdSummaryDto>()
                .ForMember(dest => dest.BirdId, opt => opt.MapFrom(src => src.BirdId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Species, opt => opt.MapFrom(src => src.Species))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
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
