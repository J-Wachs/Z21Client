using Z21Client.Models;

namespace Z21ClientTest.Models;

public class LocoInfoTests
{
    [Fact]
    public void Constructor_ParsesAddressAndDirection()
    {
        // Arrange
        const ushort address = 3;
        byte db2 = 0x00;
        byte db3 = 0x80; // forward
        byte db4 = 0x00;
        byte db5 = 0x00;
        byte db6 = 0x00;
        byte db7 = 0x00;
        byte? db8 = null;
        var firmwareVersion = new FirmwareVersion(1, 43);

        // Act
        var locoInfo = new LocoInfo(address, db2, db3, db4, db5, db6, db7, db8, firmwareVersion);

        // Assert
        Assert.Equal(address, locoInfo.Address);
        Assert.Equal(DrivingDirection.Forward, locoInfo.Direction);
    }

    [Fact]
    public void CopyConstructor_UpdatesLocoMode()
    {
        // Arrange
        var original = new LocoInfo(3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, null, new FirmwareVersion(1, 43));

        // Act
        var updated = new LocoInfo(original, LocoMode.MM);

        // Assert
        Assert.Equal(LocoMode.MM, updated.LocomotiveMode);
    }
}
