using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx.Managers;

namespace OldSchool.Ifx.Modules
{
    public class ChatModule : IModule
    {
        private readonly IDictionary<string, ChatRoom> m_Rooms;
        private readonly ISessionManager m_SessionManager;
        private readonly ITemplateProvider m_TemplateProvider;

        public ChatModule(ISessionManager sessionManager, ITemplateProvider templateProvider)
        {
            m_Rooms = new Dictionary<string, ChatRoom> { { "MAIN", new ChatRoom(Guid.Empty) } };
            m_SessionManager = sessionManager;
            m_TemplateProvider = templateProvider;
        }

        public async Task OnDataReceived(ISessionContext context)
        {
            var data = await context.Request.Body.GetBytes();
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
                await Broadcast(AnsiBuilder.Parse($"[[attr.bold]][[fg.cyan]]{context.Session.Username} [[fg.cyan]]has left the channel!"), "MAIN", context.Session.ClientId);
                context.Session.ActiveModule = null;
                return;
            }

            var message = AnsiBuilder.Parse($"[[attr.bold]][[fg.white]]From [[fg.cyan]]{context.Session.Username}[[fg.yellow]]: [[fg.white]]{text}\r\n");
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

            var templateName = "template.teleconference.welcome";
            var template = m_TemplateProvider.BuildTemplate(templateName);
            try
            {
                var templateBody = template.Render(new
                {
                    username = context.Session.Username,
                    room = userRoom,
                    count = userCount - 1, // Exclude Self
                    isEmpty = userCount == 1, // 1 = only you
                    isFull = userCount > 2 // > 2 = More than 1
                });
                await Broadcast(AnsiBuilder.Parse($"[[attr.bold]][[fg.yellow]]{context.Session.Username} has entered the room.\r\n"), "MAIN", context.Session.ClientId);

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
            await Broadcast(AnsiBuilder.Parse($"[[fg.cyan]]{context.Session.Username} just hung up!"), "MAIN", context.Session.ClientId);
        }

        // TODO: Make this a constant or static if it's going to be variable
        // Also, if we could find a way to make this more readable, that'd be great.
        private async Task ShowHelp(ISessionContext context)
        {
            await context.Response.Append(AnsiBuilder.Parse("[[action.reset]]\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[attr.bold]][[fg.cyan]]In Teleconference, anything you type is broadcast to everyone on the same\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("\"[[fg.white]]channel[[fg.cyan]]\" with you[[fg.white]].  [[fg.cyan]]Also[[fg.white]], [[fg.cyan]]use these special commands[[fg.white]]:         [[fg.cyan]]--[[fg.white]]SHORTHAND[[fg.cyan]]--\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("PAGE [[fg.white]]<[[fg.cyan]]who[[fg.white]]> ...        [[fg.yellow]]\"pages\" the user with a message           [[fg.cyan]]/P [[fg.white]]<[[fg.cyan]]who[[fg.white]]> [[fg.cyan]]...\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("PAGE ON[[fg.white]]/[[fg.cyan]]OFF[[fg.white]]/[[fg.cyan]]OK        [[fg.yellow]]allows/prevents/encourages others to page [[fg.cyan]]/P ON[[fg.white]]/[[fg.cyan]]OFF[[fg.white]]/[[fg.cyan]]OK\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("WHISPER TO [[fg.white]]<[[fg.cyan]]who[[fg.white]]> ...  [[fg.yellow]]sends a private message to another user   [[fg.cyan]]/[[fg.white]]<[[fg.cyan]]who[[fg.white]]> ...\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]CHAT [[fg.white]]<[[fg.cyan]]who[[fg.white]]>            [[fg.yellow]]request or join in a \"chat\" with someone  [[fg.cyan]]/C [[fg.white]]<[[fg.cyan]]who[[fg.white]]>\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]SCAN                  [[fg.yellow]]shows where everyone is in Teleconference [[fg.cyan]]/S\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("LIST                  [[fg.yellow]]lists all public channels you can join\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]USERS                 [[fg.yellow]]shows where everyone is on the system     [[fg.cyan]]#\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("EDIT                  [[fg.yellow]]edit your Teleconference profile          [[fg.cyan]]/E\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("JOIN[[fg.white]]/[[fg.cyan]]JOIN [[fg.white]]<[[fg.cyan]]who[[fg.white]]>       [[fg.yellow]]join a private, public, or forum channel  [[fg.cyan]]/J [[fg.white]]<[[fg.cyan]]who[[fg.white]]>\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]INVITE [[fg.white]]<[[fg.cyan]]who[[fg.white]]>          [[fg.yellow]]invites a user to your private channel    [[fg.cyan]]/I [[fg.white]]<[[fg.cyan]]who[[fg.white]]>\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]UNINVITE [[fg.white]]<[[fg.cyan]]who[[fg.white]]>        [[fg.yellow]]uninvites a user from your channel        [[fg.cyan]]/U [[fg.white]]<[[fg.cyan]]who[[fg.white]]>\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]TOPIC [[fg.white]]...             [[fg.yellow]]list a topic for your private channel     [[fg.cyan]]/T [[fg.white]]...\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("FORGET/REMEMBER [[fg.white]]<[[fg.cyan]]who[[fg.white]]> [[fg.yellow]]\"forget\" (ignore) a user annoying you     [[fg.cyan]]/F or /REM [[fg.white]]<[[fg.cyan]]who[[fg.white]]>\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]IGNORE/NOTICE [[fg.white]]<[[fg.cyan]]who[[fg.white]]>   [[fg.yellow]]ignore or notice a user's action words    [[fg.cyan]]/G or /N [[fg.white]]<[[fg.cyan]]who[[fg.white]]>\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[fg.cyan]]%c[[fg.white]]<[[fg.cyan]]who[[fg.white]]> <[[fg.cyan]]message[[fg.white]]>      [[fg.yellow]]direct a message to a user\r\n"));
        }

        // TODO: Make this a constant or static if it's going to be variable
        private async Task ShowPrompt(ISessionContext context)
        {
            await context.Response.Append(AnsiBuilder.Parse("[[attr.bold]][[fg.green]]:"));
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
                await client.Notify("[[attr.bold]][[fg.green]]***\r\n");
                await client.Notify(message);
                await client.Notify(AnsiBuilder.Parse("[[attr.bold]][[fg.green]]:"));
            }
        }
    }
}