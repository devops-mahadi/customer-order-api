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
    }

    public const string ApplicationSchema = "customerorder";
    public const string MigrationHistoryTable = "__MigrationHistory";
}
