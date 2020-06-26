namespace CanICore
{
    public class Package
    {
        public string Id { get; }

        public string Version { get; }

        public Package(string id, string version)
        {
            Id = id;
            Version = version;
        }
    }
}
