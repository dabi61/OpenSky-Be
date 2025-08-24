namespace BE_OPENSKY.Helpers;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // User mappings
        CreateMap<User, UserResponseDTO>();
        CreateMap<UserRegisterDTO, User>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "User"));
        CreateMap<UserUpdateDTO, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Tour mappings
        CreateMap<Tour, TourResponseDTO>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName));
        CreateMap<Tour, TourListDTO>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName));
        CreateMap<TourCreateDTO, Tour>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"));
        CreateMap<TourUpdateDTO, Tour>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Hotel mappings
        CreateMap<Hotel, HotelResponseDTO>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName));
        CreateMap<Hotel, HotelListDTO>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName));
        CreateMap<HotelCreateDTO, Hotel>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"));
        CreateMap<HotelUpdateDTO, Hotel>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
