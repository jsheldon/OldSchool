using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OldSchool.Common;
using OldSchool.Data;
using OldSchool.Models;

namespace OldSchool.Engines
{
    public interface IUserEngine : IDisposable
    {
        User GetUser(Guid id);
        User GetUser(string username, string password);
        bool DoesUserExist(string username);
        void CreateUser(User user);
    }

    public class UserEngine : IUserEngine
    {
        private readonly ICryptoProvider m_CryptoProvider;
        private IUserRepository m_UserRepository;

        public UserEngine(IUserRepository userRepository, ICryptoProvider cryptoProvider)
        {
            m_UserRepository = userRepository;
            m_CryptoProvider = cryptoProvider;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public User GetUser(Guid id)
        {
            var user = (from a in m_UserRepository.GetUsers()
                        where a.Id == id
                        select a).FirstOrDefault();

            if (user == null)
                return null;

            user.Properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(user.PropertiesBlob);
            return user;
        }

        public User GetUser(string username, string password)
        {
            var user = (from a in m_UserRepository.GetUsers()
                        where a.Username == username
                        select a).FirstOrDefault();

            if (user == null)
                return null;

            var passwordHash = m_CryptoProvider.Hash(password, user.Seed.ToByteArray());
            if (user.Password != passwordHash)
                return null;

            user.Properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(user.PropertiesBlob);
            return user;
        }

        public bool DoesUserExist(string username)
        {
            return m_UserRepository.GetUsers().Any(a => a.Username == username);
        }

        public void CreateUser(User user)
        {
            m_UserRepository.Add(user);
            m_UserRepository.Save();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_UserRepository != null)
            {
                m_UserRepository.Dispose();
                m_UserRepository = null;
            }
        }
    }
}