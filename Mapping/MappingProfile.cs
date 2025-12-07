namespace Wihngo.Mapping
{
    using AutoMapper;
    using Wihngo.Dtos;
    using Wihngo.Models;

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<User, UserReadDto>();
        }
    }
}
