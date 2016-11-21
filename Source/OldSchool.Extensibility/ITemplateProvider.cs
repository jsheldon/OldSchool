namespace OldSchool.Extensibility
{
    public interface ITemplateProvider
    {
        ITemplate BuildTemplate(string name);
    }

    public interface ITemplate
    {
        string Render(dynamic obj);
    }
}