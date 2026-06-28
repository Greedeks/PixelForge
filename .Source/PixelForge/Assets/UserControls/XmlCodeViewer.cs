using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PixelForge.Assets.UserControls
{
    public class XmlCodeViewer : TextEditor
    {
        public static readonly DependencyProperty CodeProperty =
            DependencyProperty.Register(nameof(Code), typeof(string), typeof(XmlCodeViewer), new PropertyMetadata(string.Empty, OnCodeChanged));

        public string Code
        {
            get => Document.Text;
            set => SetValue(CodeProperty, value);
        }

        public XmlCodeViewer()
        {
            SyntaxHighlighting = BuildHighlighting();
            ShowLineNumbers = true;
            TextArea.LeftMargins.Add(new System.Windows.Shapes.Rectangle { Width = 15 });
            IsReadOnly = true;
            WordWrap = false;
            Options.EnableHyperlinks = false;
            Options.EnableEmailHyperlinks = false;
        }

        private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XmlCodeViewer viewer = (XmlCodeViewer)d;
            viewer.Document.Text = (string)e.NewValue ?? string.Empty;
        }

        private static Color GetColor(string key)
        {
            if (Application.Current.Resources[key] is SolidColorBrush brush)
            {
                return brush.Color;
            }

            return Colors.White;
        }

        private static IHighlightingDefinition BuildHighlighting()
        {
            IHighlightingDefinition definition = HighlightingManager.Instance.GetDefinition("XML");

            foreach (HighlightingColor? color in definition.NamedHighlightingColors)
            {
                color.Foreground = color.Name switch
                {
                    "XmlTag" or "TagName" => new SimpleHighlightingBrush(GetColor("Syntax_Tag")),
                    "AttributeName" => new SimpleHighlightingBrush(GetColor("Syntax_AttrName")),
                    "AttributeValue" => new SimpleHighlightingBrush(GetColor("Syntax_AttrValue")),
                    "Comment" => new SimpleHighlightingBrush(GetColor("Syntax_Comment")),
                    _ => new SimpleHighlightingBrush(GetColor("Syntax_Default")),
                };
            }

            return definition;
        }
    }
}