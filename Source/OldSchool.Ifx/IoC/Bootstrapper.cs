using System;
using System.Linq;
using OldSchool.Extensibility;
using OldSchool.Ifx.Managers;
using OldSchool.Ifx.Networking;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Graph.Scanning;
using StructureMap.TypeRules;

namespace OldSchool.Ifx.IoC
{
    public static class Bootstrapper
    {
        public static IContainer Init()
        {
            var container = new Container(a =>
                                          {
                                              a.Scan(b =>
                                                     {
                                                         b.With(new SingletonConvention<IModule>());
                                                         b.AddAllTypesOf<IModule>();
                                                         b.AddAllTypesOf<IService>();

                                                         b.AssembliesAndExecutablesFromPath(AppDomain.CurrentDomain.BaseDirectory, c => c.FullName.StartsWith("OldSchool"));
                                                         b.WithDefaultConventions();
                                                     });


                                              a.For<ISocketService>().Singleton().Use<SocketService>();
                                              a.For<ISessionManager>().Singleton().Use<SessionManager>();
                                          });


            return container;
        }
    }

    internal class SingletonConvention<T> : IRegistrationConvention
    {
        public void ScanTypes(TypeSet types, Registry registry)
        {
            foreach (var type in types.AllTypes())
            {
                if (!type.IsConcrete() || !type.CanBeCreated() || !type.AllInterfaces().Contains(typeof(T))) continue;
                registry.For(typeof(T)).Singleton().Use(type);
            }
        }
    }
}