namespace LibraTrack.Models
{
    /// <summary>
    /// Represents the current physical/operational status of a library item.
    /// </summary>
    public enum ItemStatus
    {
        Available = 0,
        Borrowed = 1,
        Damaged = 2,
        Destroyed = 3
    }

    /// <summary>
    /// Authenticated staff user roles.
    /// </summary>
    public enum UserRole
    {
        Admin,
        Reception,
        Manager
    }

    /// <summary>
    /// Discriminator category for library catalog items.
    /// </summary>
    public enum ItemType
    {
        Book,
        Music,
        Toy
    }

    /// <summary>
    /// Condition assessment when an item is returned.
    /// </summary>
    public enum ReturnCondition
    {
        Good,
        Damaged,
        Destroyed
    }

    /// <summary>
    /// Operational status of an inter-branch inventory transfer.
    /// </summary>
    public enum TransferStatus
    {
        Requested = 0,
        InTransit = 1,
        Received = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Delivery channel for automated patron notifications.
    /// </summary>
    public enum NotificationChannel
    {
        Email = 0,
        SMS = 1
    }

    /// <summary>
    /// Notification event classification.
    /// </summary>
    public enum NotificationType
    {
        Borrowed = 0,
        DueSoon = 1,
        Overdue = 2,
        FineIssued = 3,
        ReservationReady = 4
    }

    /// <summary>
    /// Delivery status of the notification.
    /// </summary>
    public enum NotificationStatus
    {
        Sent = 0
    }

    /// <summary>
    /// Lifecycle status of an item hold / reservation queue entry.
    /// </summary>
    public enum ReservationStatus
    {
        Waiting = 0,
        Ready = 1,
        Fulfilled = 2,
        Cancelled = 3,
        Expired = 4
    }
}
