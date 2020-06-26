using System.Collections.Generic;
using System.Xml;

namespace CanICore
{
    public class PackageXmlDocument : XmlDocument
    {
        public PackageXmlDocument(string packageFileContent)
        {
            LoadXml(packageFileContent);
        }

        public IEnumerable<Package> GetPackages()
        {
            var nodes = SelectNodes("/packages/package");
            foreach (XmlElement element in nodes)
            {
                yield return new Package(element.GetAttribute("id"), element.GetAttribute("version"));
            }
        }
    }
}
