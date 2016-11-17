using System.Collections.Generic;
using OldSchool.Extensibility;
using OldSchool.Models;

namespace OldSchool.Ifx.Session
{
    public class SessionContext : ISessionContext
    {
        public SessionContext()
        {
            Environment = new Dictionary<string, object>();
            Request = new SessionRequest(this, Environment);
            Response = new SessionResponse(this, Environment);
        }

        public ISessionRequest Request { get; set; }
        public ISessionResponse Response { get; set; }
        public ISession Session { get; set; }
        public IDictionary<string, object> Environment { get; set; }
        public User User { get; set; }
    }
}