using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IUserVoucherService
    {
        Task<Guid> SaveVoucherAsync(Guid userId, SaveVoucherDTO saveVoucherDto);
        Task<UserVoucherResponseDTO?> GetUserVoucherByIdAsync(Guid userVoucherId);
        Task<UserVoucherListResponseDTO> GetUserVouchersByUserIdAsync(Guid userId, int page = 1, int size = 10);
        Task<UserVoucherListResponseDTO> GetUserVouchersAsync(int page = 1, int size = 10);
        Task<bool> UpdateUserVoucherAsync(Guid userVoucherId, UpdateUserVoucherDTO updateUserVoucherDto);
        Task<bool> DeleteUserVoucherAsync(Guid userVoucherId);
        Task<bool> IsVoucherAlreadySavedAsync(Guid userId, Guid voucherId);
        Task<bool> UseVoucherAsync(Guid userVoucherId);
    }
}
