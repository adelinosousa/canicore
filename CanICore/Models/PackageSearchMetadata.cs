using NuGet.Protocol.Core.Types;

namespace CanICore
{
    public class PackageSearchMetadata
    {
        public string PackageId { get; }

        public string PackageVersion { get; }

        public IPackageSearchMetadata PackageMetadata { get; }

        public PackageSearchMetadata(string packageId, string packageVersion, IPackageSearchMetadata packageMetadata)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageMetadata = packageMetadata;
        }
    }
}
