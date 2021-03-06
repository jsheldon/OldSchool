﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OldSchool.Common;
using OldSchool.Extensibility;
using OldSchool.Ifx;
using OldSchool.Ifx.IoC;

namespace OldSchool.Service
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            var container = Bootstrapper.Init();
            Console.WriteLine("Initializing...");
            var templateProvider = container.GetInstance<ITemplateProvider>();
            var modules = container.GetAllInstances<IModule>();
            foreach (var module in modules)
            {
                Console.WriteLine($"...Registering Templates for: {module}");
                templateProvider.RegisterTemplates(module.GetType());
                Console.WriteLine($"...Initializing: {module}");
                module.Initialize();
            }

            Console.WriteLine("Initialization Complete");
            Console.WriteLine("Starting Services...");
            // TODO: Revisit, current plans are to add additional service layers for things such as web.
            var services = container.GetAllInstances<IService>().ToList();
            StartDebug(services);
            // ServiceBase.Run(services.OfType<ServiceBase>().ToArray());
        }

        /// <summary>
        ///     Fires off the service in Console mode
        /// </summary>
        /// <param name="services"></param>
        [Conditional("DEBUG")]
        private static void StartDebug(IList<IService> services)
        {
            // helpful for testing stop/start of the service
            services.Each(a => a.Start());
            while (true)
            {
                var data = Console.ReadLine();
                switch (data)
                {
                    case "stop":
                        services.Each(a => a.Stop());
                        break;
                    case "start":
                        services.Each(a => a.Start());
                        break;
                    case "quit":
                        return;
                }
            }
        }
    }
}