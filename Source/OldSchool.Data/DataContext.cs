using System;
using System.Data.Entity;
using OldSchool.Models;

namespace OldSchool.Data
{
    [DbConfigurationType(typeof(SqlServerConfiguration))] 
    public class DataContext : DbContext
    {
        static DataContext()
        {
            Database.SetInitializer(new DbInitializer());
        }

        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<User> Users { get; set; }
    }

    public class DbInitializer : CreateDatabaseIfNotExists<DataContext>
    {
        protected override void Seed(DataContext context)
        {
            context.Users.Add(new User
                              {
                                  Id = Guid.NewGuid(),
                                  Password = "F4DA9CF6412B3311D97920C4EA91BAB99B036D5F0380A6A095107ADD847279BF",
                                  Username = "Sysop",
                                  Seed = Guid.Parse("977c65c9-f074-4a70-a1c3-eaccc158a843"),
                                  PropertiesBlob = "{ }",
                                  DateAdded = DateTime.Now,
                                  IpAddress = "0.0.0.0"
                              });
        }
    }
}