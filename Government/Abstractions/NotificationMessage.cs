//namespace NotificationService.Models
//{
//    public class NotificationMessage
//    {
//        public string Title { get; set; } = string.Empty;
//        public string Body { get; set; } = string.Empty;
//        public NotificationType Type { get; set; }
//        public List<ChannelType> Channels { get; set; } = new();
//        public List<string>? TargetUsers { get; set; }


//        // to identify the notification category (Update, Offer, Alert)
//        public NotificationCategory Category { get; set; }

//        // NEW: For users not in the system
//        public List<string>? ExternalEmails { get; set; }
//        public List<string>? ExternalPhoneNumbers { get; set; }
//    }

//    public enum NotificationType
//    {
//        SystemWide,
//        UserSpecific,
//        Group
//    }

//    public enum NotificationCategory
//    {
//        Update,  // For updates to existing information
//        Offer,   // For promotional offers or discounts
//        Alert    // For urgent alerts or notifications
//    }

//    public enum ChannelType
//    {
//        Email,
//        Push,
//        SMS,
//        Whatsapp,
//        // InApp
//    }

//}
