using CommandLine;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanICore
{
    class Options
    {
        [Option('p', "path", Required = true, HelpText = "Path to .net framework project directory")]
        public string Path { get; set; }

        [Option('o', "output", Required = false, HelpText = "Outputs result to a csv file")]
        public bool Output { get; set; }
    }

    class Program
    {
        static readonly SourceCacheContext sourceCacheContext = new SourceCacheContext();
        static readonly SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        static readonly string[] supportedFrameworks = new string[] { ".NETCoreApp", ".NETStandard" };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);
        }

        static void RunOptions(Options options)
        {
            var packageFiles = Directory.GetFiles(options.Path, "packages.config", SearchOption.AllDirectories);

            var tasks = new List<Task>();
            var packageMetadatas = new ConcurrentDictionary<string, IPackageSearchMetadata>();

            foreach (var packageFile in packageFiles)
            {
                var packageFileContent = File.ReadAllText(packageFile);

                var packages = new PackageXmlDocument(packageFileContent).GetPackages().ToArray();

                for (int i = 0; i < packages.Length; i++)
                {
                    if (packageMetadatas.ContainsKey(FormatPackageSearchMetadataId(packages[i].Id, packages[i].Version))) continue;

                    tasks.Add(GetPackageMetadataAsync(packages[i].Id, packages[i].Version).ContinueWith(x => 
                    {
                        Console.Write(".");

                        PackageSearchMetadata packageSearchMetadata = x.Result;
                        packageMetadatas.TryAdd(FormatPackageSearchMetadataId(packageSearchMetadata.PackageId, packageSearchMetadata.PackageVersion), packageSearchMetadata.PackageMetadata);
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            Output(options, packageMetadatas);
        }

        static string FormatPackageSearchMetadataId(string packageId, string packageVersion)
        {
            return $"{packageId} v{packageVersion}";
        }

        static async Task<PackageSearchMetadata> GetPackageMetadataAsync(string packageId, string packageVersion)
        {
            var resource = await repository.GetResourceAsync<PackageMetadataResource>();

            var packageMetadata = await resource.GetMetadataAsync(
                new PackageIdentity(packageId, new NuGetVersion(packageVersion)),
                sourceCacheContext,
                NullLogger.Instance,
                CancellationToken.None);

            return new PackageSearchMetadata(packageId, packageVersion, packageMetadata);
        }

        static bool IsCompatible(NuGetFramework nuGetFramework)
        {
            return supportedFrameworks.Contains(nuGetFramework.Framework) && nuGetFramework.Version.Major <= 2;
        }

        static void Output(Options options, ConcurrentDictionary<string, IPackageSearchMetadata> packageMetadatas)
        {
            if (options.Output)
            {
                var output = new StringBuilder();
                output.AppendLine("Package, Is compatible");

                foreach (var packageMetadata in packageMetadatas)
                {
                    if (packageMetadata.Value == null)
                    {
                        output.AppendLine($"{packageMetadata.Key}, Not found");
                        continue;
                    }

                    var dependencySet = packageMetadata.Value.DependencySets.FirstOrDefault(x => IsCompatible(x.TargetFramework));
                    if (dependencySet != null)
                    {
                        output.AppendLine($"{packageMetadata.Key}, Yes ({dependencySet.TargetFramework.DotNetFrameworkName.Replace(",", " ")})");
                    }
                    else
                    {
                        output.AppendLine($"{packageMetadata.Key}, No");
                    }
                }

                File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}canicore.csv", output.ToString());
            } 
            else
            {
                var format = "| {0,-70} | {1,-70} |";

                Console.WriteLine();
                Console.WriteLine(string.Format(format, "Package", "Is compatible"));

                foreach (var packageMetadata in packageMetadatas)
                {
                    if (packageMetadata.Value == null)
                    {
                        Console.WriteLine(string.Format(format, packageMetadata.Key, "Not found"));
                        continue;
                    }

                    var dependencySet = packageMetadata.Value.DependencySets.FirstOrDefault(x => IsCompatible(x.TargetFramework));
                    if (dependencySet != null)
                    {
                        Console.WriteLine(string.Format(format, packageMetadata.Key, $"Yes ({dependencySet.TargetFramework.DotNetFrameworkName})"));
                    }
                    else
                    {
                        Console.WriteLine(string.Format(format, packageMetadata.Key, "No"));
                    }
                }
            }
        }
    }
}
