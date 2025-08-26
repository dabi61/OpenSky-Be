namespace BE_OPENSKY.Helpers;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // User mappings
        CreateMap<User, UserResponseDTO>();
        CreateMap<UserRegisterDTO, User>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => 
                string.IsNullOrEmpty(src.Role) ? RoleConstants.Customer : src.Role));
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

        // Mapping cho Voucher - Mã giảm giá
        CreateMap<Voucher, VoucherResponseDTO>()
            .ForMember(dest => dest.UsedCount, opt => opt.MapFrom(src => src.UserVouchers.Count(uv => uv.IsUsed))) // Số lần đã dùng
            .ForMember(dest => dest.RemainingUsage, opt => opt.MapFrom(src => src.MaxUsage - src.UserVouchers.Count(uv => uv.IsUsed))) // Số lần còn lại
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => 
                src.StartDate <= DateTime.UtcNow && src.EndDate >= DateTime.UtcNow)) // Còn hiệu lực không
            .ForMember(dest => dest.RelatedItemName, opt => opt.Ignore()) // Sẽ set thủ công trong service
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow)); // Placeholder - có thể thêm vào model

        CreateMap<VoucherCreateDTO, Voucher>()
            .ForMember(dest => dest.VoucherID, opt => opt.MapFrom(src => Guid.NewGuid())) // Tạo ID mới
            .ForMember(dest => dest.UserVouchers, opt => opt.MapFrom(src => new List<UserVoucher>())); // Khởi tạo danh sách rỗng

        CreateMap<UserVoucher, UserVoucherResponseDTO>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName)) // Tên khách hàng
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email)) // Email khách hàng
            .ForMember(dest => dest.VoucherCode, opt => opt.MapFrom(src => src.Voucher.Code)) // Mã voucher
            .ForMember(dest => dest.VoucherPercent, opt => opt.MapFrom(src => src.Voucher.Percent)) // Phần trăm giảm
            .ForMember(dest => dest.VoucherDescription, opt => opt.MapFrom(src => src.Voucher.Description ?? "")) // Mô tả voucher
            .ForMember(dest => dest.VoucherEndDate, opt => opt.MapFrom(src => src.Voucher.EndDate)) // Ngày hết hạn
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.Voucher.EndDate < DateTime.UtcNow)); // Đã hết hạn chưa
    }
}
