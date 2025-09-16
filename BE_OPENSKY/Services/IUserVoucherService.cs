using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IUserVoucherService
    {
        Task<Guid> SaveVoucherAsync(Guid userId, SaveVoucherDTO saveVoucherDto);
        Task<UserVoucherListResponseDTO> GetUserVouchersByUserIdAsync(Guid userId, int page = 1, int size = 10);
        Task<UserVoucherListResponseDTO> GetUserVouchersAsync(int page = 1, int size = 10);
        Task<bool> IsVoucherAlreadySavedAsync(Guid userId, Guid voucherId);
    }
}
