using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows;
using System.Windows.Media;
using System.IO;

namespace AvalonEditViewer
{
    public static class AvalonThemeService
    {

        public static void ApplyTheme(TextEditor editor, string filePath)
        {
            string extension = Path.GetExtension(filePath);

            IHighlightingDefinition? highlighting = HighlightingManager.Instance.GetDefinitionByExtension(extension);

            if (highlighting is null)
            {
                editor.SyntaxHighlighting = null;
                return;
            }

            foreach (HighlightingColor highlightingColor in highlighting.NamedHighlightingColors)
            {
                string resourceKey = HighlightingColorResources.TryGetValue(highlightingColor.Name ?? string.Empty, out string? mappedResourceKey)
                    ? mappedResourceKey
                    : "App.Text";

                if (Application.Current.TryFindResource(resourceKey) is SolidColorBrush themeBrush)
                {
                    highlightingColor.Foreground = new SimpleHighlightingBrush(themeBrush.Color);
                }
            }

            editor.SyntaxHighlighting = highlighting;
            editor.TextArea.TextView.Redraw();
        }

        #region Dictionary

        private static readonly Dictionary<string, string> HighlightingColorResources = CreateHighlightingColorResources();

        private static Dictionary<string, string> CreateHighlightingColorResources()
        {
            Dictionary<string, string> resources = new(StringComparer.OrdinalIgnoreCase);

            AddAliases(resources, "App.TextMuted",
                "Comment", "DocComment", "CommentTags", "JavaDocTags", "LineComment", "BlockQuote", "UnchangedText");

            AddAliases(resources, "App.StatusSuccess",
                "String", "Char", "Character", "Regex", "XmlString", "CData", "AttributeValue",
                "Entity", "Entities", "EntityReference", "Image", "AddedText", "FileName");

            AddAliases(resources, "App.StatusWarning",
                "Digits", "Number", "NumberLiteral", "DateLiteral", "Bool", "TrueFalse",
                "BooleanConstants", "Constants", "Literals", "JavaScriptLiterals",
                "Null", "NullOrValueKeywords", "Value", "Position");

            AddAliases(resources, "App.Accent",
                "FieldName", "AttributeName", "Attributes", "Property", "Selector", "Class",
                "Heading", "Link", "MethodCall", "MethodName", "FunctionCall", "Command", "Variable",
                "Keywords", "Keyword1", "JavaScriptKeyWords", "JavaScriptIntrinsics",
                "JavaScriptGlobalFunctions", "ValueTypeKeywords", "ReferenceTypeKeywords",
                "GotoKeywords", "ContextKeywords", "ExceptionKeywords", "CheckedKeyword",
                "UnsafeKeywords", "ParameterModifiers", "Modifiers", "Visibility",
                "NamespaceKeywords", "GetSetAddRemove", "TypeKeywords", "SemanticKeywords",
                "AccessKeywords", "OperatorKeywords", "SelectionStatements",
                "IterationStatements", "JumpStatements", "ControlStatements",
                "ValueTypes", "OtherTypes", "AccessModifiers", "CompoundKeywords",
                "LoopKeywords", "JumpKeywords", "ExceptionHandling", "ControlFlow",
                "ExceptionHandlingStatements", "ReferenceTypes", "Void", "Package",
                "DataTypes", "FunctionKeywords", "Preprocessor", "Namespace", "Friend",
                "XmlTag", "DocType", "XmlDeclaration", "KnownDocTags",
                "ScriptTag", "JavaScriptTag", "JScriptTag", "VBScriptTag", "HtmlTag", "Tags", "Header");

            AddAliases(resources, "App.Text",
                "Punctuation", "StringInterpolation", "ThisOrBaseReference", "This",
                "Operators", "CurlyBraces", "Colon", "Slash", "Assignment",
                "XmlPunctuation", "Emphasis", "StrongEmphasis", "Code", "LineBreak",
                "KeyWords2", "MathMode", "LatexMathMode");

            AddAliases(resources, "App.StatusError",
                "BrokenEntity", "UnknownAttribute", "UnknownScriptTag", "RemovedText");

            return resources;
        }

        private static void AddAliases(Dictionary<string, string> resources, string resourceKey, params string[] aliases)
        {
            foreach (string alias in aliases)
            {
                resources[alias] = resourceKey;
            }
        }

        #endregion
    }
}