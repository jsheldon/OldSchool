using System;
using System.Threading.Tasks;

namespace OldSchool.Extensibility
{
    public interface ITemplateProvider
    {
        ITemplate BuildTemplate(string name);
        void RegisterTemplates(Type type);
    }

    public interface ITemplate
    {
        Task<string> Render(dynamic obj);
        string Render();
    }
}