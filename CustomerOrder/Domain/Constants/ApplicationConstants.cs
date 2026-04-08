namespace CustomerOrder.Domain.Constants;

public static class ApplicationConstants
{
    // Customer related constants
    public static class Customer
    {
        public const int FirstNameMaxLength = 64;
        public const int LastNameMaxLength = 32;
        public const int EmailMaxLength = 255;
        public const int PhoneNumberMaxLength = 20;
    }

    // Order related constants
    public static class Order
    {
        public const int OrderNumberMaxLength = 50;
        public const int ShippingAddressMaxLength = 500;
        public const int NotesMaxLength = 1000;
        public const int StatusMaxLength = 50;

        public static class Status
        {
            public const string Pending = "Pending";
            public const string Confirmed = "Confirmed";
            public const string Shipped = "Shipped";
            public const string Delivered = "Delivered";
            public const string Cancelled = "Cancelled";
        }
    }

    // Pagination constants
    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
        public const int MinPageNumber = 1;
    }

    // Validation messages
    public static class ValidationMessages
    {
        public const string CustomerNotFound = "Customer with email '{0}' not found";
        public const string CustomerAlreadyExists = "Customer with email '{0}' already exists";
        public const string CustomerAlreadyDeleted = "Customer with email '{0}' has been deleted";
    }

    // Audit logging constants
    public static class Audit
    {
        public const int EntityTypeMaxLength = 50;
        public const int EntityIdMaxLength = 255;
        public const int ActionMaxLength = 50;
        public const int UserEmailMaxLength = 255;
        public const int IpAddressMaxLength = 45; // IPv6 max length
        public const int UserAgentMaxLength = 500;
        public const int HttpMethodMaxLength = 10;
        public const int EndpointMaxLength = 500;

        public static class Actions
        {
            public const string Created = "Created";
            public const string Updated = "Updated";
            public const string Deleted = "Deleted";
            public const string Viewed = "Viewed";
            public const string Exported = "Exported";
            public const string Anonymized = "Anonymized";
            public const string ConsentGranted = "ConsentGranted";
            public const string ConsentRevoked = "ConsentRevoked";
        }

        public static class EntityTypes
        {
            public const string Customer = "Customer";
            public const string Order = "Order";
            public const string Consent = "Consent";
        }
    }

    // GDPR Consent constants
    public static class Consent
    {
        public const int ConsentTypeMaxLength = 50;
        public const int IpAddressMaxLength = 45;
        public const int UserAgentMaxLength = 500;
        public const int ConsentVersionMaxLength = 20;
        public const int DeletedReasonMaxLength = 500;

        public static class Types
        {
            public const string Marketing = "Marketing";
            public const string DataProcessing = "DataProcessing";
            public const string Profiling = "Profiling";
            public const string ThirdPartySharing = "ThirdPartySharing";
        }
    }

    public const string ApplicationSchema = "customerorder";
    public const string MigrationHistoryTable = "__MigrationHistory";
}
