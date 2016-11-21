using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx.Modules;

namespace OldSchool.Modules.Teleconference
{
    public class ChatModule : IModule
    {
        private  ITemplate m_ChatMessageTemplate;
        private  ITemplate m_EnterRoomTemplate;
        private  ITemplate m_HangUpTemplate;
        private  ITemplate m_LeaveChannelTemplate;
        private  ITemplate m_PromptTemplate;
        private readonly IDictionary<string, ChatRoom> m_Rooms;
        private readonly ISessionManager m_SessionManager;
        private readonly ITemplateProvider m_TemplateProvider;
        private  ITemplate m_HelpTemplate;

        public ChatModule(ISessionManager sessionManager, ITemplateProvider templateProvider)
        {
            m_Rooms = new Dictionary<string, ChatRoom> { { "MAIN", new ChatRoom(Guid.Empty) } };
            m_SessionManager = sessionManager;
            m_TemplateProvider = templateProvider;
            
        }

        public Task Initialize()
        {
            m_HelpTemplate = m_TemplateProvider.BuildTemplate("teleconference.help");
            m_PromptTemplate = m_TemplateProvider.BuildTemplate("teleconference.prompt");
            m_LeaveChannelTemplate = m_TemplateProvider.BuildTemplate("teleconference.leftchannel");
            m_ChatMessageTemplate = m_TemplateProvider.BuildTemplate("teleconference.chatmessage");
            m_EnterRoomTemplate = m_TemplateProvider.BuildTemplate("teleconference.enterroom");
            m_HangUpTemplate = m_TemplateProvider.BuildTemplate("teleconference.hangup");
            return Task.FromResult(0);
        }

        public async Task OnDataReceived(ISessionContext context)
        {
            var data = await context.Request.GetBytes();
            var text = Encoding.ASCII.GetString(data);
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (text.StartsWith("/"))
            {
                HandleSlashCommand(context, text);
                return;
            }

            if (text.StartsWith("?"))
            {
                await ShowHelp(context);
                return;
            }

            if (text.ToLower() == "x")
            {
                await ExitTeleconference(context);
                return;
            }

            var message = await m_ChatMessageTemplate.Render(new { username = context.Session.Username, message = text });
            await context.Response.Append(message);
            await Broadcast(message, "MAIN", context.Session.ClientId);
            await ShowPrompt(context);
        }

        public string Name { get; set; } = "Chat";

        public async Task Activate(ISessionContext context)
        {
            Guid? userRoomId = null;
            string userRoom = null; // TODO: = GetDatabaseCallToAccessUsersLastRoom

            if (string.IsNullOrWhiteSpace(userRoom))
            {
                userRoomId = Guid.Empty;
                userRoom = "MAIN";
            }

            var room = m_Rooms.Get(userRoom);
            if (room == null)
            {
                room = new ChatRoom(userRoomId.Value);
                m_Rooms.Add(userRoom, room);
            }

            room.Users.Add(context.Session.ClientId);
            var userCount = room.Users.Count;

            var templateName = "teleconference.welcome";
            var template = m_TemplateProvider.BuildTemplate(templateName);
            try
            {
                var templateBody = await template.Render(new
                                                         {
                                                             username = context.Session.Username,
                                                             room = userRoom,
                                                             count = userCount - 1, // Exclude Self
                                                             isEmpty = userCount == 1, // 1 = only you
                                                             isFull = userCount > 2 // > 2 = More than 1
                                                         });

                var message = await m_EnterRoomTemplate.Render(new { username = context.Session.Username });
                await Broadcast(message, "MAIN", context.Session.ClientId);
                await context.Response.Append(templateBody);
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"Template {templateName} is invalid : {ex.Message}");
            }
            await ShowPrompt(context);
        }

        public async Task OnSessionDisconnecting(ISessionContext context)
        {
            var message = await m_HangUpTemplate.Render(new { username = context.Session.Username });
            await Broadcast(message, "MAIN", context.Session.ClientId);
        }

        private async Task ExitTeleconference(ISessionContext context)
        {
            var response = await m_LeaveChannelTemplate.Render(new { username = context.Session.Username });
            await context.Response.Append(response);
            context.Session.ActiveModule = null;
        }

        // TODO: Make this a constant or static if it's going to be variable
        // Also, if we could find a way to make this more readable, that'd be great.
        private async Task ShowHelp(ISessionContext context)
        {
            try
            {
                var templateBody = await m_HelpTemplate.Render(new
                                                               {
                                                                   username = context.Session.Username
                                                               });

                await context.Response.Append(templateBody);
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"Template teleconference.help is invalid : {ex.Message}");
            }
        }

        // TODO: Make this a constant or static if it's going to be variable
        private async Task ShowPrompt(ISessionContext context)
        {
            // await context.Response.Append(AnsiBuilder.Parse("[[attr.bold]][[fg.green]]:"));
        }

        private void HandleSlashCommand(ISessionContext context, string command)
        {
        }

        private async Task Broadcast(string message, string roomName, params Guid[] exclusions)
        {
            var room = m_Rooms.Get(roomName);
            if (room == null)
                return;

            var query = from a in m_SessionManager.Sessions
                        where room.Users.Contains(a.ClientId)
                        select a;

            if (exclusions.Length > 0)
            {
                foreach (var exclusion in exclusions)
                    query = query.Where(a => a.ClientId != exclusion);
            }

            var clientList = query.ToList();
            foreach (var client in clientList)
            {
                var response = m_PromptTemplate.Render();
                await client.Notify("[[attr.bold]][[fg.green]]***\r\n");
                await client.Notify(message);
                await client.Notify(response);
            }
        }
    }
}