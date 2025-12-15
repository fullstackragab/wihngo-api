using AutoMapper;
using Wihngo.Dtos;
using Wihngo.Models;
using Wihngo.Models.Payout;
using Wihngo.Models.Enums;

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
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set by controller
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
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set by controller
                .ForMember(dest => dest.Tagline, opt => opt.MapFrom(src => src.Tagline))
                .ForMember(dest => dest.LovedBy, opt => opt.MapFrom(src => src.LovedCount))
                .ForMember(dest => dest.SupportedBy, opt => opt.MapFrom(src => src.SupportedCount))
                .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.OwnerId));

            CreateMap<User, UserSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            // Story Like mappings
            CreateMap<StoryLike, StoryLikeDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
                .ForMember(dest => dest.UserProfileImage, opt => opt.MapFrom(src => src.User != null ? src.User.ProfileImage : null));

            // Comment mappings
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
                .ForMember(dest => dest.UserProfileImage, opt => opt.MapFrom(src => src.User != null ? src.User.ProfileImage : null))
                .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore())
                .ForMember(dest => dest.ReplyCount, opt => opt.MapFrom(src => src.Replies.Count));

            CreateMap<Comment, CommentWithRepliesDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
                .ForMember(dest => dest.UserProfileImage, opt => opt.MapFrom(src => src.User != null ? src.User.ProfileImage : null))
                .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore())
                .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.Replies));

            // Comment Like mappings
            CreateMap<CommentLike, CommentLikeDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
                .ForMember(dest => dest.UserProfileImage, opt => opt.MapFrom(src => src.User != null ? src.User.ProfileImage : null));

            // Payout mappings
            CreateMap<PayoutMethod, PayoutMethodDto>()
                .ForMember(dest => dest.MethodType, opt => opt.MapFrom(src => MapMethodTypeToString(src.MethodType)))
                .ForMember(dest => dest.Iban, opt => opt.MapFrom(src => MaskIban(src.Iban)))
                .ForMember(dest => dest.WalletAddress, opt => opt.MapFrom(src => MaskWalletAddress(src.WalletAddress)));

            CreateMap<PayoutTransaction, PayoutTransactionDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString().ToLowerInvariant()))
                .ForMember(dest => dest.MethodType, opt => opt.Ignore()); // Set by service

            CreateMap<PayoutBalance, PayoutBalanceDto>()
                .ForMember(dest => dest.Summary, opt => opt.Ignore()); // Set by service

            // Memorial mappings
            CreateMap<MemorialMessage, MemorialMessageDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : string.Empty))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.Ignore()); // Set by service if needed

            CreateMap<MemorialFundRedirection, MemorialFundRedirectionDto>()
                .ForMember(dest => dest.BirdName, opt => opt.MapFrom(src => src.Bird != null ? src.Bird.Name : string.Empty));

            CreateMap<Bird, MemorialBirdDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.Stats, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.OwnerMessage, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.MessagesCount, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.RemainingBalance, opt => opt.Ignore()); // Set by service
        }

        // Helper methods for payout mappings
        private static string MapMethodTypeToString(PayoutMethodType methodType)
        {
            return methodType switch
            {
                PayoutMethodType.Iban => "iban",
                PayoutMethodType.PayPal => "paypal",
                PayoutMethodType.UsdcSolana => "usdc-solana",
                PayoutMethodType.EurcSolana => "eurc-solana",
                PayoutMethodType.UsdcBase => "usdc-base",
                PayoutMethodType.EurcBase => "eurc-base",
                _ => "unknown"
            };
        }

        private static string? MaskIban(string? iban)
        {
            if (string.IsNullOrEmpty(iban) || iban.Length < 8)
                return iban;

            return $"{iban[..4]}****{iban[^4..]}";
        }

        private static string? MaskWalletAddress(string? address)
        {
            if (string.IsNullOrEmpty(address) || address.Length < 12)
                return address;

            return $"{address[..6]}...{address[^4..]}";
        }
    }
}
