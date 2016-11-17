using System;
using System.Linq;
using OldSchool.Models;

namespace OldSchool.Data
{
    public interface IUserRepository : IDisposable
    {
        IQueryable<User> GetUsers();
        void Add(User user);
        void Save();
    }

    public class UserRepository : IUserRepository
    {
        private DataContext m_Context;

        public UserRepository(DataContext context)
        {
            m_Context = context;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IQueryable<User> GetUsers()
        {
            return m_Context.Users;
        }

        public void Add(User user)
        {
            m_Context.Users.Add(user);
        }

        public void Save()
        {
            m_Context.SaveChanges();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_Context != null)
            {
                m_Context.Dispose();
                m_Context = null;
            }
        }
    }
}