using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Mustache;
using OldSchool.Extensibility;

namespace OldSchool.Ifx.Templating
{
    public class HtmlTemplateProvider : ITemplateProvider
    {
        private static Dictionary<string, ITemplate> m_Templates;

        static HtmlTemplateProvider()
        {
            InitializeTemplates();
        }

        public ITemplate BuildTemplate(string name)
        {
            if (m_Templates.ContainsKey(name))
                return m_Templates[name];
            return null;
        }

        private static void InitializeTemplates()
        {
            m_Templates = new Dictionary<string, ITemplate>();
            // For now, we only have a single template, in the future, we want to pull these from available resources
            var t = new StreamReader(typeof(HtmlTemplateProvider).Assembly.GetManifestResourceStream("OldSchool.Ifx.Templating.Templates.template.teleconference.welcome.html")).ReadToEnd();
            m_Templates.Add("template.teleconference.welcome", new HtmlTemplate(t));
        }
    }

    public class HtmlTemplate : ITemplate
    {
        public HtmlTemplate(string templateHtml)
        {
            TemplateHtml = templateHtml;
            ProcessedTemplate = AnsiHtmlParser.Parse(templateHtml);
        }

        public string ProcessedTemplate { get; set; }

        public string TemplateHtml { get; set; }

        public string Render(dynamic obj)
        {
            var compiler = new FormatCompiler { RemoveNewLines = false };
            var template = compiler.Compile(ProcessedTemplate);
            return template.Render(obj);
        }
    }


    public static class AnsiHtmlParser
    {
        public enum AnsiAttribute
        {
            Normal = 0,
            Bold = 1,
            Underline = 4,
            Blink = 5,
            Reverse = 7
        }

        // Background Colors              
        public enum AnsiBackgroundColor
        {
            Black = 40,
            Red = 41,
            Green = 42,
            Yellow = 43,
            Blue = 44,
            Magenta = 45,
            Cyan = 46,
            White = 47
        }

        public enum AnsiForegroundColor
        {
            Black = 30,
            Red = 31,
            Green = 32,
            Yellow = 33,
            Blue = 34,
            Magenta = 35,
            Cyan = 36,
            White = 37
        }

        private const string ESC = "\x1B";
        public static string ClearScreenAndHomeCursor = ESC + "[2J";
        private static readonly HtmlParser m_Parser = new HtmlParser(new Configuration().WithCss());

        public static string Parse(string templateHtml)
        {
            var sb = new StringBuilder();
            var document = m_Parser.Parse(templateHtml);
            var root = (from a in document.All
                        where a.NodeName == "DIV"
                        select a).FirstOrDefault();

            VisitParser(root as IHtmlDivElement, sb);
            return sb.ToString();
        }

        private static void VisitParser(IHtmlDivElement element, StringBuilder sb)
        {
            if (element == null)
                return;

            var attributes = new AnsiTextAttribute();
            ParseAnsiClass(element.ClassList, attributes);
            if (attributes.IsCls)
                sb.Append(ClearScreenAndHomeCursor);

            foreach (var child in element.Children)
            {
                VisitParser(child as IHtmlSpanElement, sb);
                VisitParser(child as IHtmlDivElement, sb);
            }

            sb.Append(Environment.NewLine);
        }

        private static void VisitParser(IHtmlSpanElement element, StringBuilder sb)
        {
            var ws = new Regex(@"\s+", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

            if (element == null)
                return;

            var attribute = new AnsiTextAttribute();
            ParseAnsiClass(element.ClassList, attribute);
            sb.Append(attribute);
            sb.Append(ws.Replace(element.TextContent.TrimStart(), " "));
        }

        private static void ParseAnsiClass(ITokenList tokens, AnsiTextAttribute attributes)
        {
            foreach (var token in tokens)
            {
                if (!token.StartsWith("ansi-"))
                    continue;

                if (token.StartsWith("ansi-bg"))
                {
                    SetAnsiBackground(token, attributes);
                    continue;
                }
                if (token.StartsWith("ansi-fg"))
                {
                    SetAnsiForeground(token, attributes);
                    continue;
                }
                if (token.StartsWith("ansi-text"))
                {
                    SetAnsiAttribute(token, attributes);
                    continue;
                }

                switch (token)
                {
                    case "ansi-cls":
                        attributes.IsCls = true;
                        break;
                }
            }
        }

        private static void SetAnsiBackground(string token, AnsiTextAttribute attributes)
        {
            var color = token.Replace("ansi-bg-", string.Empty);
            if (string.IsNullOrWhiteSpace(color))
                return;

            AnsiBackgroundColor backgroundColor;
            if (!Enum.TryParse(color, true, out backgroundColor))
                return;

            attributes.BackgroundColor = (int)backgroundColor;
        }

        private static void SetAnsiForeground(string token, AnsiTextAttribute attributes)
        {
            var color = token.Replace("ansi-fg-", string.Empty);
            if (string.IsNullOrWhiteSpace(color))
                return;

            AnsiForegroundColor foregroundColor;
            if (!Enum.TryParse(color, true, out foregroundColor))
                return;

            attributes.ForegroundColor = (int)foregroundColor;
        }

        private static void SetAnsiAttribute(string token, AnsiTextAttribute attributes)
        {
            var value = token.Replace("ansi-text-", string.Empty);
            if (string.IsNullOrWhiteSpace(value))
                return;

            AnsiAttribute attribute;
            if (!Enum.TryParse(value, true, out attribute))
                return;

            switch (attribute)
            {
                case AnsiAttribute.Bold:
                    attributes.IsBold = true;
                    return;
                case AnsiAttribute.Underline:
                    attributes.IsUnderline = true;
                    return;
            }
        }
    }


    public class AnsiTextAttribute
    {
        public int ForegroundColor { get; set; }
        public int BackgroundColor { get; set; }
        public bool IsCls { get; set; }
        public bool IsBold { get; set; }
        public bool IsUnderline { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (IsCls)
                sb.Append("\x1B[2J");

            sb.Append("\x1B[");
            sb.Append(IsBold ? "1;" : "0;");
            if (ForegroundColor > 0)
                sb.AppendFormat("{0}m;", ForegroundColor);

            if (BackgroundColor > 0)
                sb.AppendFormat("{0}m;", BackgroundColor);

            sb.Remove(sb.Length - 1, 1);
            if (sb.Length == 2)
                return string.Empty;
            return sb.ToString();
        }
    }
}