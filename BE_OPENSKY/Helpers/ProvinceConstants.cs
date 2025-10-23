namespace BE_OPENSKY.Helpers;

public static class ProvinceConstants
{
    public static readonly HashSet<string> PROVINCE_LIST = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Thành phố trực thuộc trung ương
        "Hà Nội",
        "Hồ Chí Minh",
        "Đà Nẵng",
        "Hải Phòng",
        "Cần Thơ",
        
        // Tỉnh miền Bắc
        "Hà Giang",
        "Cao Bằng",
        "Bắc Kạn",
        "Tuyên Quang",
        "Lào Cai",
        "Điện Biên",
        "Lai Châu",
        "Sơn La",
        "Yên Bái",
        "Hòa Bình",
        "Thái Nguyên",
        "Lạng Sơn",
        "Quảng Ninh",
        "Bắc Giang",
        "Phú Thọ",
        "Vĩnh Phúc",
        "Bắc Ninh",
        "Hải Dương",
        "Hưng Yên",
        "Thái Bình",
        "Hà Nam",
        "Nam Định",
        "Ninh Bình",
        
        // Tỉnh miền Trung
        "Thanh Hóa",
        "Nghệ An",
        "Hà Tĩnh",
        "Quảng Bình",
        "Quảng Trị",
        "Thừa Thiên Huế",
        "Quảng Nam",
        "Quảng Ngãi",
        "Bình Định",
        "Phú Yên",
        "Khánh Hòa",
        "Ninh Thuận",
        "Bình Thuận",
        "Kon Tum",
        "Gia Lai",
        "Đắk Lắk",
        "Đắk Nông",
        "Lâm Đồng",
        
        // Tỉnh miền Nam
        "Bình Phước",
        "Tây Ninh",
        "Bình Dương",
        "Đồng Nai",
        "Bà Rịa - Vũng Tàu",
        "Long An",
        "Tiền Giang",
        "Bến Tre",
        "Trà Vinh",
        "Vĩnh Long",
        "Đồng Tháp",
        "An Giang",
        "Kiên Giang",
        "Hậu Giang",
        "Sóc Trăng",
        "Bạc Liêu",
        "Cà Mau"
    };
    
    public static bool IsValidProvince(string? province)
    {
        if (string.IsNullOrWhiteSpace(province))
            return false;
            
        var trimmedProvince = province.Trim();
        
        // Normalize Unicode to handle different encodings
        var normalizedProvince = trimmedProvince.Normalize(System.Text.NormalizationForm.FormC);
        
        // Check against normalized list
        foreach (var validProvince in PROVINCE_LIST)
        {
            var normalizedValidProvince = validProvince.Normalize(System.Text.NormalizationForm.FormC);
            if (string.Equals(normalizedProvince, normalizedValidProvince, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
}

