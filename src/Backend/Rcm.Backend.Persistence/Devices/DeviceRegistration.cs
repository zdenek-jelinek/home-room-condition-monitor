namespace Rcm.Backend.Persistence.Devices;

public class DeviceRegistration
{
    public string Name { get; }
    public string Identifier { get; }
    public string Key { get; }

    public DeviceRegistration(string name, string identifier, string key)
    {
        Name = name;
        Identifier = identifier;
        Key = key;
    }
}