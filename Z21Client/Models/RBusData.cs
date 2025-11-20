namespace Z21Client.Models;

/// <summary>
/// Represents a set of feedback data from a group of 10 R-Bus modules.
/// </summary>
public sealed record RBusData
{
    /// <summary>
    /// The group index (e.g., 0 for modules 1-10, 1 for modules 11-20).
    /// </summary>
    public int GroupIndex { get; }

    /// <summary>
    /// A 10-byte array representing the state of 80 inputs (10 modules * 8 inputs).
    /// </summary>
    public byte[] FeedbackStatus { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RBusData"/> class.
    /// </summary>
    /// <param name="groupIndex">The group index.</param>
    /// <param name="feedbackData">A 10-byte span containing the feedback status.</param>
    public RBusData(int groupIndex, ReadOnlySpan<byte> feedbackData)
    {
        GroupIndex = groupIndex;
        FeedbackStatus = feedbackData.ToArray();
    }

    /// <summary>
    /// Checks if a specific sensor input is active.
    /// </summary>
    /// <param name="moduleAddress">The address of the feedback module (1-based, e.g., 1 to 20).</param>
    /// <param name="inputPort">The input port on the module (1-based, 1 to 8).</param>
    /// <returns>True if the sensor is active, false otherwise. Returns false if the address is outside the group's range.</returns>
    public bool IsSensorActive(int moduleAddress, int inputPort)
    {
        int firstModuleInGroup = (GroupIndex * 10) + 1;
        if (moduleAddress < firstModuleInGroup || moduleAddress >= firstModuleInGroup + 10)
        {
            return false; // This group does not contain the requested module.
        }
        if (inputPort < 1 || inputPort > 8)
        {
            return false; // Invalid port number.
        }

        int byteIndex = moduleAddress - firstModuleInGroup;
        int bitIndex = inputPort - 1;

        if (byteIndex < 0 || byteIndex >= FeedbackStatus.Length)
        {
            return false;
        }

        return (FeedbackStatus[byteIndex] & (1 << bitIndex)) != 0;
    }
}
