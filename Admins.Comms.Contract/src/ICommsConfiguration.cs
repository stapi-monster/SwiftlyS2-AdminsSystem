namespace Admins.Comms.Contract;

public interface ICommsConfiguration
{
    /// <summary>
    /// Whether to enable admin chat functionality.
    /// </summary>
    public bool EnableAdminChat { get; set; }
    /// <summary>
    /// The character that starts an admin chat message.
    /// </summary>
    public string AdminChatStartCharacter { get; set; }
    /// <summary>
    /// The reasons for communications.
    /// </summary>
    public List<string> CommsReasons { get; set; }
    /// <summary>
    /// The durations for communications in seconds.
    /// </summary>
    public List<int> CommsDurationsInSeconds { get; set; }
}