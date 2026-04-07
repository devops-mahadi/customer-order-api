namespace CustomerOrder.Tests.Helpers;

/// <summary>
/// Constants for API routes used in integration tests
/// </summary>
public static class ApiRoutes
{
    public static class Customers
    {
        public const string Base = "/api/customers";
        public static string ByEmail(string email) => $"{Base}/{email}";
    }

    public static class Orders
    {
        public const string Base = "/api/orders";
        public static string ByOrderNumber(string orderNumber) => $"{Base}/{orderNumber}";
        public static string ByCustomerEmail(string email) => $"{Base}/customer/{email}";
    }
}
