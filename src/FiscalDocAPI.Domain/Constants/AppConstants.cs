namespace FiscalDocAPI.Domain.Constants;

public static class AppConstants
{
    public static class Pagination
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
    }

    public static class RoutingKeys
    {
        public const string DocumentProcessed = "fiscal.document.processed";
    }

    public static class ProcessingStatus
    {
        public const string Pending = "Pending";
        public const string Processed = "Processed";
        public const string Failed = "Failed";
    }

    public static class ValidationMessages
    {
        public const string XmlFileRequired = "XML file not provided or empty";
        public const string InvalidXmlExtension = "File must be of type XML";
        public const string DocumentNotFound = "Document not found";
        public const string DocumentUpdatedSuccessfully = "Document updated successfully";
        public const string DocumentDeletedSuccessfully = "Document deleted successfully";
    }
}
