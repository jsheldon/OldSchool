using System;
using System.Collections.Generic;

namespace OldSchool.Ifx.Modules
{
    public class ChatRoom
    {
        public ChatRoom(Guid id)
        {
            Id = id;
            Users = new List<Guid>();
        }

        public Guid Id { get; private set; }
        public List<Guid> Users { get; }
    }
}