using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx.Session;

namespace OldSchool.Ifx.Providers
{
    public class MenuProvider : IProvider
    {
        private readonly List<IModule> m_Modules;

        public MenuProvider(IDependencyManager dependencyManager)
        {
            m_Modules = dependencyManager.GetAllInstances<IModule>().ToList();
        }

        public IProvider Next { get; set; }

        public async Task OnDataReceived(ISessionContext context)
        {
            if (context.Session.ActiveModule != null)
            {
                await Next.OnDataReceived(context);
                return;
            }

            var showMenu = await HandleUserInput(context);

            if (showMenu)
                ShowMenu(context);
        }

        public async Task OnSessionCreated(ISessionContext context)
        {
            if (Next != null)
                await Next.OnSessionCreated(context);
        }

        public async Task OnSessionDisconnecting(ISessionContext context)
        {
            if (Next != null)
                await Next.OnSessionDisconnecting(context);
        }

        public async Task OnModulesProcessed(ISessionContext context)
        {
            // We don't show the main menu to people not logged in
            if (!(bool)context.Session.Properties.Get(SessionConstants.IsAuthenticated))
            {
                await Next.OnDataReceived(context);
                return;
            }

            // Already have an active module, then no need to see the menu
            if (context.Session.ActiveModule != null)
            {
                await Next.OnModulesProcessed(context);
                return;
            }

            ShowMenu(context);
        }

        private async Task<bool> HandleUserInput(ISessionContext context)
        {
            var data = await context.Request.Body.GetBytes();
            if (data.Length == 0)
            {
                ShowMenu(context);
                return true;
            }

            if ((data.Length == 1) && ((data[0] == 'X') || (data[0] == 'x')))
            {
                context.Session.IsLoggingOff = true;
                if (Next != null)
                    await Next.OnDataReceived(context);

                return false;
            }

            int selectedModule;
            if (!int.TryParse(Encoding.ASCII.GetString(data), out selectedModule))
                return true;

            if ((m_Modules.Count <= selectedModule) && (selectedModule > 0))
            {
                context.Session.ActiveModule = m_Modules[selectedModule - 1];
                context.Request.Clear();
                await context.Session.ActiveModule.Activate(context);
                return false;
            }

            return true;
        }

        private void ShowMenu(ISessionContext context)
        {
            var sb = new StringBuilder();
            sb.Append(AnsiBuilder.Parse("[[action.cls]][[fg.green]][[attr.bold]][[bg.black]]Please select from one of the following choices.\r\n"));


            sb.Append(AnsiBuilder.Parse("[[action.cls]]"));
            sb.Append(AnsiBuilder.Parse("\r\n\r\n[[attr.bold]][[fg.cyan]][[bg.black]]MAIN MENU\r\n\r\n"));

            for (var x = 0; x < m_Modules.Count; x++)
                sb.Append(AnsiBuilder.Parse($"[[attr.bold]][[fg.green]]{x + 1} [[fg.white]]- [[fg.green]]{m_Modules[x].Name}\r\n"));

            sb.Append(AnsiBuilder.Parse("[[attr.bold]][[fg.green]]X [[fg.white]]- [[attr.normal]][[fg.green]]Log Off\r\n\r\n"));
            sb.Append(AnsiBuilder.Parse("\r\n\r\n[[attr.bold]][[fg.yellow]]Your Selection? [[bg.white]][[fg.black]] [[bg.black]][[action.moveback]]"));
            context.Response.Append(sb.ToString());
        }
    }
}