using System.Threading.Tasks;
using Mustache;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Templating
{
    public class HtmlTemplate : ITemplate
    {
        public HtmlTemplate(string templateHtml)
        {
            TemplateHtml = templateHtml;
            ProcessedTemplate = AnsiHtmlParser.Parse(templateHtml);
        }

        public string ProcessedTemplate { get; set; }

        public string TemplateHtml { get; set; }

        public Task<string> Render(dynamic obj)
        {
            // TODO: Pull this out to an injected interface to abstract this away.
            return Task.Factory.StartNew<string>(() =>
                                                 {
                                                     var compiler = new FormatCompiler { RemoveNewLines = false };
                                                     var template = compiler.Compile(ProcessedTemplate);
                                                     return template.Render(obj);
                                                 });
        }

        public string Render()
        {
            return ProcessedTemplate;
        }
    }
}