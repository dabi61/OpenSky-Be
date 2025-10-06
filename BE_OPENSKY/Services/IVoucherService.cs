using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IVoucherService
    {
        Task<Guid> CreateVoucherAsync(CreateVoucherDTO createVoucherDto);
        Task<VoucherResponseDTO?> GetVoucherByIdAsync(Guid voucherId);
        Task<VoucherListResponseDTO> GetVouchersAsync(int page = 1, int size = 10);
        Task<VoucherListResponseDTO> GetVouchersByTableTypeAsync(TableType tableType, int page = 1, int size = 10);
        Task<VoucherListResponseDTO> GetActiveVouchersAsync(int page = 1, int size = 10);
        Task<VoucherListResponseDTO> GetActiveVouchersExcludingSavedByUserAsync(Guid userId, int page = 1, int size = 10);
        Task<bool> UpdateVoucherAsync(Guid voucherId, UpdateVoucherDTO updateVoucherDto);
        Task<bool> DeleteVoucherAsync(Guid voucherId);
        Task<bool> IsVoucherCodeExistsAsync(string code);
        Task<VoucherListResponseDTO> SearchVouchersForAdminAsync(AdminVoucherSearchDTO searchDto);
    }
}
