using System.Collections.Generic;
using OldSchool.Models;

namespace OldSchool.Extensibility
{
    /// <summary>
    /// </summary>
    public interface ISessionContext
    {
        ISessionRequest Request { get; set; }
        ISessionResponse Response { get; set; }
        ISession Session { get; set; }
        IDictionary<string, object> Environment { get; set; }
        User User { get; set; }
    }
}