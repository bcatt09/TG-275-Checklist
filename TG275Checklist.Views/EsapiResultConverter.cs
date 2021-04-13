﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xml;

namespace TG275Checklist.Views
{
    public static class EsapiResultConverter
    {
        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached("FormattedText",
            typeof(string),
            typeof(EsapiResultConverter),
            new UIPropertyMetadata("", FormattedTextChanged));

        static Inline Traverse(string value)
        {
            // Get the sections/inlines
            string[] sections = SplitIntoSections(value);

            // Check for grouping
            if (sections.Length.Equals(1))
            {
                string section = sections[0];
                string token; // E.g <Bold>
                int tokenStart, tokenEnd; // Where the token/section starts and ends.

                // Check for token
                if (GetTokenInfo(section, out token, out tokenStart, out tokenEnd))
                {
                    // Get the content to further examination
                    string content = token.Length.Equals(tokenEnd - tokenStart) ?
                        null :
                        section.Substring(token.Length, section.Length - 1 - token.Length * 2);

                    switch (token.ToUpper())
                    {
                        case "<PASS>":
                            var pass = new Run(content);
                            pass.Foreground = System.Windows.Media.Brushes.LightGreen;
                            return pass;
                        case "<FAIL>":
                            var fail = new Run(content);
                            fail.Foreground = System.Windows.Media.Brushes.Salmon;
                            return fail;
                        case "<WARN>":
                            var warn = new Run(content);
                            warn.Foreground = System.Windows.Media.Brushes.Khaki;
                            return warn;
                        case "<BOLD>":
                            return new Bold(Traverse(content));
                        case "<ITALIC>":
                            return new Italic(Traverse(content));
                        case "<UNDERLINE>":
                            return new Underline(Traverse(content));
                        case "<LINEBREAK/>":
                            return new LineBreak();
                        default:
                            return new Run(section);
                    }
                }
                else return new Run(section);
            }
            else // Group together
            {
                Span span = new Span();

                foreach (string section in sections)
                    span.Inlines.Add(Traverse(section));

                return span;
            }
        }

        /// <summary>
        /// Examines the passed string and find the first token, where it begins and where it ends.
        /// </summary>
        /// <param name="value">The string to examine.</param>
        /// <param name="token">The found token.</param>
        /// <param name="startIndex">Where the token begins.</param>
        /// <param name="endIndex">Where the end-token ends.</param>
        /// <returns>True if a token was found.</returns>
        static bool GetTokenInfo(string value, out string token, out int startIndex, out int endIndex)
        {
            token = null;
            endIndex = -1;

            startIndex = value.IndexOf("<");
            int startTokenEndIndex = value.IndexOf(">");

            // No token here
            if (startIndex < 0)
                return false;

            // No token here
            if (startTokenEndIndex < 0)
                return false;

            token = value.Substring(startIndex, startTokenEndIndex - startIndex + 1);

            // Check for closed token. E.g. <LineBreak/>
            if (token.EndsWith("/>"))
            {
                endIndex = startIndex + token.Length;
                return true;
            }

            string endToken = token.Insert(1, "/");

            // Detect nesting;
            int nesting = 0;
            int temp_startTokenIndex = -1;
            int temp_endTokenIndex = -1;
            int pos = 0;
            do
            {
                temp_startTokenIndex = value.IndexOf(token, pos);
                temp_endTokenIndex = value.IndexOf(endToken, pos);

                if (temp_startTokenIndex >= 0 && temp_startTokenIndex < temp_endTokenIndex)
                {
                    nesting++;
                    pos = temp_startTokenIndex + token.Length;
                }
                else if (temp_endTokenIndex >= 0 && nesting > 0)
                {
                    nesting--;
                    pos = temp_endTokenIndex + endToken.Length;
                }
                else // Invalid tokenized string
                    return false;

            } while (nesting > 0);

            endIndex = pos;

            return true;
        }

        /// <summary>
        /// Splits the string into sections of tokens and regular text.
        /// </summary>
        /// <param name="value">The string to split.</param>
        /// <returns>An array with the sections.</returns>
        static string[] SplitIntoSections(string value)
        {
            List<string> sections = new List<string>();

            while (!string.IsNullOrEmpty(value))
            {
                string token;
                int tokenStartIndex, tokenEndIndex;

                // Check if this is a token section
                if (GetTokenInfo(value, out token, out tokenStartIndex, out tokenEndIndex))
                {
                    // Add pretext if the token isn't from the start
                    if (tokenStartIndex > 0)
                        sections.Add(value.Substring(0, tokenStartIndex));

                    sections.Add(value.Substring(tokenStartIndex, tokenEndIndex - tokenStartIndex));
                    value = value.Substring(tokenEndIndex); // Trim away
                }
                else
                { // No tokens, just add the text
                    sections.Add(value);
                    value = null;
                }
            }

            return sections.ToArray();
        }

        private static void FormattedTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            string value = e.NewValue as string;

            TextBlock textBlock = sender as TextBlock;

            if (textBlock != null)
                textBlock.Inlines.Add(Traverse(value));
        }
    }
}
