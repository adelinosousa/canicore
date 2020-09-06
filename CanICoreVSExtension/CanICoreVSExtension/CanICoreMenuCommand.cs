using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace CanICoreVSExtension
{
    internal sealed class CanICoreMenuCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("eba5461f-2d3a-4736-8f47-ddb03c556e38");
        private readonly DTE2 dte;

        private CanICoreMenuCommand(OleMenuCommandService commandService, DTE2 dTE2)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.dte = dTE2 ?? throw new ArgumentNullException(nameof(dTE2));

            var menuCommandID = new CommandID(CommandSet, CommandId);

            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static CanICoreMenuCommand Instance
        {
            get;
            private set;
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;

            Instance = new CanICoreMenuCommand(commandService, dte);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outputWindowPane = GetOutputWindowPane();

            var solutionFullName = dte.Solution.FullName;

            ThreadHelper.JoinableTaskFactory.Run(async delegate {
                await TaskScheduler.Default;

                var output = CanICore.Program.Run(Directory.GetParent(solutionFullName).ToString(), true);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                outputWindowPane.OutputString(output);
            });
        }

        OutputWindowPane GetOutputWindowPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const string outputWindowPaneName = "CanICore";

            var panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;
            OutputWindowPane pane;

            try
            {
                pane = panes.Item(outputWindowPaneName);
                pane.Clear();
            }
            catch
            {
                pane = panes.Add(outputWindowPaneName);
            }

            pane.Activate();
            dte.Windows.Item(Constants.vsWindowKindOutput).Activate();

            return pane;
        }
    }
}
