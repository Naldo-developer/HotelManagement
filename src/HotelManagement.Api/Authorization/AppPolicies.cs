namespace HotelManagement.Api.Authorization;

public static class AppRoles
{
    public const string Admin = "ADMIN";
    public const string Reception = "RECEPTION";
    public const string Cashier = "CASHIER";
    public const string Maintenance = "MAINTENANCE";
}

public static class AppPolicies
{
    public const string Operations = "Operations";
    public const string Configuration = "Configuration";
    public const string Financial = "Financial";
}