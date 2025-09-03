namespace BE_OPENSKY.Helpers;

public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Supervisor = "Supervisor";
    public const string TourGuide = "TourGuide";
    public const string Hotel = "Hotel";
    public const string Customer = "Customer";

    public static readonly string[] AllRoles = { Admin, Supervisor, TourGuide, Hotel, Customer };
}
