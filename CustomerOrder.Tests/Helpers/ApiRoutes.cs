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

        // GDPR endpoints
        public static string Export(string email) => $"{Base}/{email}/export";
        public static string Anonymize(string email) => $"{Base}/{email}/anonymize";
        public static string Consents(string email) => $"{Base}/{email}/consents";
        public static string ConsentByType(string email, string consentType) => $"{Base}/{email}/consents/{consentType}";
    }

    public static class Orders
    {
        public const string Base = "/api/orders";
        public static string ByOrderNumber(string orderNumber) => $"{Base}/{orderNumber}";
        public static string ByCustomerEmail(string email) => $"{Base}/customer/{email}";
    }
}
