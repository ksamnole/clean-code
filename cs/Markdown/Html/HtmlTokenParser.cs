﻿using System.Collections.Generic;
using System.Linq;
using Markdown.Enums;
using Markdown.Extensions;
using Markdown.Interfaces;
using Markdown.Rules;
using Markdown.Tags;
using Markdown.Tags.Html;
using Markdown.Tags.Markdown;

namespace Markdown.Html
{
    public class HtmlTokenParser : ITokenParser
    {
        private readonly char[] signs =
        {
            ',', '.', '!', '?', ' ', '—', '-', ':', '#', '\\'
        };

        public IEnumerable<Token> Parse(string data)
        {
            var tokens = new List<Token>();
            
            var dataLines = data.Split("\r\n");

            foreach (var line in dataLines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                
                var rootToken = GetRootToken(line);
                
                if (Equals(rootToken.Tag, HtmlTags.Heading))
                    rootToken.Index = 2;
                
                tokens.Add(ParseLine(rootToken, line));
            }

            return tokens;
        }

        private Token ParseLine(Token token, string line)
        {
            var tagString = string.Empty;
                
            while (token.Index < line.Length)
            {
                var symbol = line[token.Index];

                if (char.IsLetter(symbol) || char.IsDigit(symbol) || signs.Contains(symbol))
                {
                    if (token.CheckAndUpdateTokenIfInDifferentWords(symbol))
                        return null;
                    if (TryCreateHtmlTag(tagString, out var htmlTag) != null)
                    {
                        if (htmlTag.Equals(token.Tag))
                            return token;
                        token.TryAddToken(ParseLine(new Token(TokenType.Nested, htmlTag, token), line));
                        tagString = string.Empty;
                        continue;
                    }
                    if (TryAddTextToken(token, line)) 
                        continue;
                    if (token.Parent.Tag.IsNumberInHighlightingTag(symbol))
                        token.Parent.ToTextToken();
                    token.Content += symbol;
                }
                else if (token.Type == TokenType.Text)
                    return token;
                else
                    tagString += symbol;
                token.Index += 1;
            }
            return ProcessWhenLineEnd(token, tagString, line);
        }

        private Token ProcessWhenLineEnd(Token token, string tagString, string line)
        {
            if (!string.IsNullOrEmpty(tagString))
            {
                if (TryCreateHtmlTag(tagString, out var tag) != null)
                    if (token.Tag.Equals(tag))
                        return token;
                token.TryAddToken(ParseLine(new Token(TokenType.Text, Tag.Empty, token)
                {
                    Content = tagString
                }, line));
            }

            if (!Tag.IsHighlightingTag(token.Tag))
                return token;

            return token.Type != TokenType.Root ? token.ToTextToken() : token;
        }
        
        private bool TryAddTextToken(Token token, string line)
        {
            if (token.Type == TokenType.Text) return false;
            token.TryAddToken(ParseLine(new Token(TokenType.Text, Tag.Empty, token), line));
            return true;
        }

        private ITag TryCreateHtmlTag(string tagString, out ITag tag)
        {
            MarkdownToHtml.Rules.TryGetValue(new Tag(tagString), out var value);
            tag = value;
            return tag;
        }

        private Token GetRootToken(string line)
        {
            var tag = new Tag(line[0].ToString());

            if (!tag.Equals(MarkdownTags.Heading) || line[1] != ' ')
                return new Token(TokenType.Root, HtmlTags.LineBreak, null);
            
            return new Token(TokenType.Root, HtmlTags.Heading, null);

        }
    }
}