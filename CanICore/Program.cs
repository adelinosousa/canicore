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

    public class Program
    {
        static readonly SourceCacheContext sourceCacheContext = new SourceCacheContext();
        static readonly SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        static readonly string[] supportedFrameworks = new string[] { ".NETCoreApp", ".NETStandard" };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptions);
        }

        public static string Run(string projectPath, bool consoleOutput)
        {
            var packageFiles = Directory.GetFiles(projectPath, "packages.config", SearchOption.AllDirectories);

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
                        if(consoleOutput)
                            Console.Write(".");

                        PackageSearchMetadata packageSearchMetadata = x.Result;
                        packageMetadatas.TryAdd(FormatPackageSearchMetadataId(packageSearchMetadata.PackageId, packageSearchMetadata.PackageVersion), packageSearchMetadata.PackageMetadata);
                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());

            return consoleOutput ? OutputConsole(packageMetadatas) : OutputCsv(packageMetadatas);
        }

        static void RunOptions(Options options)
        {
            var output = Run(options.Path, !options.Output);

            if(options.Output)
            {
                File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}canicore.csv", output.ToString());
            }
            else
            {
                Console.Write(output);
            }
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

        static string OutputConsole(ConcurrentDictionary<string, IPackageSearchMetadata> packageMetadatas)
        {
            var format = "| {0,-70} | {1,-70} |";

            var output = new StringBuilder();
            output.AppendLine();
            output.AppendLine(string.Format(format, "Package", "Is compatible"));

            foreach (var packageMetadata in packageMetadatas)
            {
                if (packageMetadata.Value == null)
                {
                    output.AppendLine(string.Format(format, packageMetadata.Key, "Not found"));
                    continue;
                }

                var dependencySet = packageMetadata.Value.DependencySets.FirstOrDefault(x => IsCompatible(x.TargetFramework));
                if (dependencySet != null)
                {
                    output.AppendLine(string.Format(format, packageMetadata.Key, $"Yes ({dependencySet.TargetFramework.DotNetFrameworkName})"));
                }
                else
                {
                    output.AppendLine(string.Format(format, packageMetadata.Key, "No"));
                }
            }

            return output.ToString();
        }

        static string OutputCsv(ConcurrentDictionary<string, IPackageSearchMetadata> packageMetadatas)
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

            return output.ToString();
        }
    }
}
