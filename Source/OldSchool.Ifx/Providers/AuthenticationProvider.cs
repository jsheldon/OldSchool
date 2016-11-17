using System;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Common;
using OldSchool.Engines;
using OldSchool.Extensibility;
using OldSchool.Ifx.Session;
using OldSchool.Models;

namespace OldSchool.Ifx.Providers
{
    public class AuthenticationProvider : IProvider
    {
        private static readonly string m_UsernamePrompt = AnsiBuilder.Parse("[[attr.bold]][[fg.green]]If you already have an account on this\r\nsystem, type it in and press ENTER.\r\nOtherwise type \"[[fg.cyan]]new[[fg.green]]\": ");
        private static readonly string m_PasswordPrompt = AnsiBuilder.Parse("[[fg.green]]Enter Password: ");
        private static readonly string m_InvalidAuth = AnsiBuilder.Parse("[[fg.red]]Invalid Username or Password!\r\n");
        private readonly ICryptoProvider m_CryptoProvider;

        public AuthenticationProvider(ICryptoProvider cryptoProvider)
        {
            m_CryptoProvider = cryptoProvider;
        }

        public IProvider Next { get; set; }

        public async Task OnSessionCreated(ISessionContext context)
        {
            await context.Response.Append(AnsiBuilder.ClearScreenAndHomeCursor);
            await context.Response.Append(m_UsernamePrompt);
        }

        public Task OnSessionDisconnecting(ISessionContext context)
        {
            // Not Implemented
            return Task.FromResult(0);
        }

        public async Task OnDataReceived(ISessionContext context)
        {
            if ((bool)context.Session.Properties.Get(SessionConstants.IsAuthenticated))
            {
                await Next.OnDataReceived(context);
                return;
            }

            var value = Encoding.ASCII.GetString(await context.Request.Body.GetBytes());
            if (value == "new")
            {
                await BeginSignupProcess(context);
                return;
            }

            if (context.Session.Properties.Get(SessionConstants.SignupStatus) != null)
            {
                await ContinueSignupProcess(context);
                return;
            }

            var authenticated = await ProcessAuthentication(context);
            context.Session.Properties.Set(SessionConstants.IsAuthenticated, authenticated);
            if (authenticated)
                await Next.OnDataReceived(context);
        }

        // TODO: Revisit, really hating this.  Would be nice if it was dynamic
        private async Task ContinueSignupProcess(ISessionContext context)
        {
            var signupStatus = context.Session.Properties.Get(SessionConstants.SignupStatus).ToString();
            switch (signupStatus)
            {
                case "username": // They should have just entered they're username.
                    await ProcessUsername(context);
                    return;
                case "password":
                    await ProcessPassword(context);
                    return;
            }
        }

        private async Task ProcessUsername(ISessionContext context)
        {
            // TODO: Holy crap we do this a lot, helper methods needed.
            var value = Encoding.ASCII.GetString(await context.Request.Body.GetBytes());

            if (string.IsNullOrWhiteSpace(value))
            {
                await ShowError(context, "You must specify a username to continue.");
                await ShowUsernamePrompt(context);
                return;
            }

            using (var userEngine = context.Session.GetDependency<IUserEngine>())
            {
                var userExists = userEngine.DoesUserExist(value);
                if (userExists)
                {
                    await ShowError(context, "Sorry, but that username is unavailable, please try another.");
                    await ShowUsernamePrompt(context);
                    return;
                }
            }

            context.Session.Properties.Set(SessionConstants.SignupStatus, "password");
            context.Session.Properties.Set("signup.username", value);
            await ShowPasswordPrompt(context);
        }

        private async Task ShowPasswordPrompt(ISessionContext context)
        {
            await context.Response.Append(AnsiBuilder.Parse("\r\n\r\n[[bg.black]][[fg.green]]Please enter the password you will use to login to the system.\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[bg.white]]                                               \r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[action.moveup]][[bg.white]][[fg.black]]"));
            context.Session.Properties.Set(SessionConstants.MaskNextInput, true);
        }

        private async Task ShowError(ISessionContext context, string errorMessage)
        {
            await context.Response.Append(AnsiBuilder.Parse($"\r\n[[bg.black]][[attr.bold]][[fg.red]]{errorMessage}\r\n"));
        }

        private async Task ProcessPassword(ISessionContext context)
        {
            var value = Encoding.ASCII.GetString(await context.Request.Body.GetBytes());
            if (string.IsNullOrWhiteSpace(value))
            {
                await ShowError(context, "You must specify a password to continue.");
                await ShowPasswordPrompt(context);
                return;
            }

            if (value.Length < 6)
            {
                await ShowError(context, "Your password must be at least 6 characters.");
                await ShowPasswordPrompt(context);
                return;
            }

            var username = context.Session.Properties.Get("signup.username").ToString();
            var seed = Guid.NewGuid();
            var passwordHash = m_CryptoProvider.Hash(value, seed.ToByteArray());
            var userId = Guid.NewGuid();
            var user = new User
                       {
                           Id = userId,
                           Username = username,
                           Password = passwordHash,
                           Seed = seed,
                           DateAdded = DateTime.Now,
                           IpAddress = context.Session.ClientAddress.ToString(),
                           PropertiesBlob = "{ }"
                       };

            try
            {
                using (var userEngine = context.Session.GetDependency<IUserEngine>())
                {
                    userEngine.CreateUser(user);
                }

            }
            catch (Exception ex)
            {
                await ShowError(context, "There was an unknown error processing your request.  This has been logged, and the system operator has been notified.  Please try your request again later.");
                context.Session.IsLoggingOff = true;
                // Log Exception
                return;
            }

            await context.Response.Append(AnsiBuilder.Parse($"\r\n[[bg.black]][[fg.white]]Welcome {username}!\r\n"));
            context.Session.Properties.Set(SessionConstants.IsAuthenticated, true);
            context.User = user;
            await Next.OnDataReceived(context);
        }

        private async Task BeginSignupProcess(ISessionContext context)
        {
            context.Session.Properties.Set(SessionConstants.SignupStatus, "username");
            await context.Response.Append(AnsiBuilder.Parse("[[action.cls]][[attr.bold]][[fg.yellow]]Welcome newcomer!\r\n\r\n[[attr.normal]][[fg.green]]Some welcome text should go here.\r\n"));
            await ShowUsernamePrompt(context);
        }

        private async Task ShowUsernamePrompt(ISessionContext context)
        {
            await context.Response.Append(AnsiBuilder.Parse("\r\n\r\n[[bg.black]][[fg.green]]Please enter the username you would like to use.\r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[bg.white]]                                               \r\n"));
            await context.Response.Append(AnsiBuilder.Parse("[[action.moveup]][[bg.white]][[fg.black]]"));
        }

        private async Task<bool> ProcessAuthentication(ISessionContext context)
        {
            var value = Encoding.ASCII.GetString(await context.Request.Body.GetBytes());

            if (string.IsNullOrWhiteSpace(context.Session.Username))
            {
                // Username hasn't been set, so set that
                context.Session.Username = value;
            }
            // Then this should be the password.  Try to authenticate
            else
            {
                User user;
                using (var userEngine = context.Session.GetDependency<IUserEngine>())
                {
                    user = userEngine.GetUser(context.Session.Username, value);
                }

                if (user == null)
                {
                    await context.Response.Append(m_InvalidAuth);
                    context.Session.Username = null;
                }
                else
                {
                    context.Session.Properties.Set(SessionConstants.IsAuthenticated, true);
                    context.User = user;
                    return true;
                }
            }

            if (string.IsNullOrWhiteSpace(context.Session.Username))
                await context.Response.Append(m_UsernamePrompt);
            else
            {
                await context.Response.Append(m_PasswordPrompt);
                context.Session.Properties.Set(SessionConstants.MaskNextInput, true);
            }

            return false;
        }
    }
}