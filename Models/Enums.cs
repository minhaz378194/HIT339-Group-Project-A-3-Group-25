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
}
