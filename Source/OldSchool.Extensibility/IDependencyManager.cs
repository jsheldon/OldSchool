using System.Collections.Generic;

namespace OldSchool.Extensibility
{
    public interface IDependencyManager
    {
        T GetInstance<T>();
        void Eject<T>();
        IDependencyManager CreateChildContainer();
        IEnumerable<T> GetAllInstances<T>();
    }
}