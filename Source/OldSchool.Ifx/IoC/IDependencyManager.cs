using System;
using System.Collections.Generic;
using OldSchool.Extensibility;
using StructureMap;

namespace OldSchool.Ifx.IoC
{
    public class DependencyManager : IDependencyManager
    {
        private IContainer m_Container;

        public DependencyManager(IContainer container)
        {
            m_Container = container;
        }

        public IDependencyManager Parent { get; private set; }

        public T GetInstance<T>()
        {
            return m_Container.GetInstance<T>();
        }

        public IEnumerable<T> GetAllInstances<T>()
        {
            return m_Container.GetAllInstances<T>();
        }

        public IDependencyManager CreateChildContainer()
        {
            return new DependencyManager(m_Container.CreateChildContainer()) { Parent = this };
        }

        public void Eject<T>()
        {
            m_Container.EjectAllInstancesOf<T>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_Container != null)
            {
                m_Container.Dispose();
                m_Container = null;
            }
        }
    }
}