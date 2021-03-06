﻿using System.Threading.Tasks;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Providers
{
    public class LogOffProvider : IProvider
    {
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
            await CheckForLogoff(context);
        }

        public async Task OnDataReceived(ISessionContext context)
        {
            await CheckForLogoff(context);
        }

        public IProvider Next { get; set; }

        private async Task CheckForLogoff(ISessionContext context)
        {
            if (context.Session.IsLoggingOff)
                await context.Response.Append("Good bye!\r\n");

            if (Next != null)
                await Next.OnDataReceived(context);
        }
    }
}