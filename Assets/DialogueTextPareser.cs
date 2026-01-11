using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DialogueTextParser
{
    private readonly float defaultShakeIntensity;
    private readonly float defaultShakeSpeed;

    public DialogueTextParser(float defaultShakeIntensity, float defaultShakeSpeed)
    {
        this.defaultShakeIntensity = defaultShakeIntensity;
        this.defaultShakeSpeed = defaultShakeSpeed;
    }

    public List<TextToken> ParseText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
            return new List<TextToken>();

        Debug.Log($"[DialogueTextParser] Starting to parse: '{rawText}'");
        List<TextToken> tokens = new List<TextToken>();
        int i = 0;

        try
        {
            while (i < rawText.Length)
            {
                if (rawText[i] == '<')
                {
                    int endTagIndex = rawText.IndexOf('>', i);
                    if (endTagIndex == -1)
                    {
                        // No closing '>', treat as regular text
                        AddTextToken(tokens, "<", false, Color.white, 0, defaultShakeSpeed, 0);
                        i++;
                        continue;
                    }

                    string tagContent = rawText.Substring(i + 1, endTagIndex - i - 1);

                    // Try to parse as combined effects
                    TextToken combinedToken = ParseFlexibleCombinedEffects(tagContent);

                    if (combinedToken != null)
                    {
                        tokens.Add(combinedToken);
                        i = endTagIndex + 1;
                    }
                    else
                    {
                        // Try old-style individual tag parsing as fallback
                        i = ParseIndividualTag(rawText, i, tokens);
                    }
                }
                else
                {
                    // Collect regular text until we hit a tag or end
                    int start = i;
                    while (i < rawText.Length && rawText[i] != '<')
                    {
                        i++;
                    }

                    if (i > start)
                    {
                        string text = rawText.Substring(start, i - start);
                        AddTextToken(tokens, text, false, Color.white, 0, defaultShakeSpeed, 0);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing dialogue text: {ex.Message}");
            tokens.Clear();
            tokens.Add(new TextToken { text = rawText });
        }

        Debug.Log($"[DialogueTextParser] Finished parsing. Total tokens: {tokens.Count}");
        return tokens;
    }

    private TextToken ParseFlexibleCombinedEffects(string tagContent)
    {
        Debug.Log($"[ParseFlexibleCombinedEffects] Parsing: '{tagContent}'");

        // Initialize token with defaults
        TextToken token = new TextToken
        {
            text = "",
            delayBefore = 0,
            shakeIntensity = 0,
            shakeSpeed = defaultShakeSpeed,
            hasHighlight = false,
            highlightColor = Color.yellow
        };

        // We'll parse the tag content piece by piece
        int pos = 0;
        StringBuilder textBuilder = new StringBuilder();

        while (pos < tagContent.Length)
        {
            // Skip whitespace
            while (pos < tagContent.Length && char.IsWhiteSpace(tagContent[pos]))
            {
                pos++;
            }

            if (pos >= tagContent.Length)
                break;

            // Check for highlight
            if (pos + 10 <= tagContent.Length && tagContent.Substring(pos, 10) == "highlight(")
            {
                int closeParen = FindMatchingParenthesis(tagContent, pos + 9);
                if (closeParen != -1)
                {
                    string colorStr = tagContent.Substring(pos + 10, closeParen - pos - 10);
                    token.hasHighlight = true;
                    token.highlightColor = ParseColor(colorStr);
                    Debug.Log($"[ParseFlexibleCombinedEffects] Applied highlight: '{colorStr}'");
                    pos = closeParen + 1;
                }
                else
                {
                    pos++; // Skip and continue
                }
            }
            // Check for shake
            else if (pos + 6 <= tagContent.Length && tagContent.Substring(pos, 6) == "shake(")
            {
                int closeParen = FindMatchingParenthesis(tagContent, pos + 5);
                if (closeParen != -1)
                {
                    string paramsStr = tagContent.Substring(pos + 6, closeParen - pos - 6);
                    string[] shakeParams = paramsStr.Split(',');

                    if (shakeParams.Length >= 1)
                        float.TryParse(shakeParams[0].Trim(), out token.shakeIntensity);
                    if (shakeParams.Length >= 2)
                        float.TryParse(shakeParams[1].Trim(), out token.shakeSpeed);

                    Debug.Log($"[ParseFlexibleCombinedEffects] Applied shake: intensity={token.shakeIntensity}, speed={token.shakeSpeed}");
                    pos = closeParen + 1;
                }
                else
                {
                    pos++;
                }
            }
            // Check for delay
            else if (pos + 6 <= tagContent.Length && tagContent.Substring(pos, 6) == "delay(")
            {
                int closeParen = FindMatchingParenthesis(tagContent, pos + 5);
                if (closeParen != -1)
                {
                    string delayStr = tagContent.Substring(pos + 6, closeParen - pos - 6);
                    float.TryParse(delayStr.Trim(), out token.delayBefore);
                    Debug.Log($"[ParseFlexibleCombinedEffects] Applied delay: {token.delayBefore}");
                    pos = closeParen + 1;
                }
                else
                {
                    pos++;
                }
            }
            // Not an effect - must be text
            else
            {
                // This is text content, collect it
                int textStart = pos;
                while (pos < tagContent.Length &&
                       !(pos + 10 <= tagContent.Length && tagContent.Substring(pos, 10) == "highlight(") &&
                       !(pos + 6 <= tagContent.Length && tagContent.Substring(pos, 6) == "shake(") &&
                       !(pos + 6 <= tagContent.Length && tagContent.Substring(pos, 6) == "delay("))
                {
                    pos++;
                }

                string textSegment = tagContent.Substring(textStart, pos - textStart).Trim();
                if (!string.IsNullOrEmpty(textSegment))
                {
                    if (textBuilder.Length > 0)
                        textBuilder.Append(" ");
                    textBuilder.Append(textSegment);
                }
            }
        }

        token.text = textBuilder.ToString().Trim();

        if (string.IsNullOrEmpty(token.text))
        {
            Debug.LogWarning($"[ParseFlexibleCombinedEffects] No text found in combined effects");
            return null;
        }

        Debug.Log($"[ParseFlexibleCombinedEffects] Successfully parsed: text='{token.text}', highlight={token.hasHighlight}, shake={token.shakeIntensity}, delay={token.delayBefore}");
        return token;
    }

    private int ParseIndividualTag(string rawText, int startIndex, List<TextToken> tokens)
    {
        // Check for old-style individual tags for backward compatibility
        if (startIndex + 10 <= rawText.Length && rawText.Substring(startIndex, 10) == "<highlight")
        {
            return ParseOldStyleHighlightTag(rawText, startIndex, tokens);
        }
        else if (startIndex + 6 <= rawText.Length && rawText.Substring(startIndex, 6) == "<shake")
        {
            return ParseOldStyleShakeTag(rawText, startIndex, tokens);
        }
        else if (startIndex + 6 <= rawText.Length && rawText.Substring(startIndex, 6) == "<delay")
        {
            return ParseOldStyleDelayTag(rawText, startIndex, tokens);
        }
        else
        {
            // Not a valid tag, add the '<' as regular text
            AddTextToken(tokens, "<", false, Color.white, 0, defaultShakeSpeed, 0);
            return startIndex + 1;
        }
    }

    private int ParseOldStyleHighlightTag(string rawText, int startIndex, List<TextToken> tokens)
    {
        int openParenIndex = rawText.IndexOf('(', startIndex + 10);
        if (openParenIndex == -1)
        {
            Debug.LogWarning($"[ParseOldStyleHighlightTag] No '(' found for highlight tag!");
            return startIndex + 1;
        }

        int closeParenIndex = FindMatchingParenthesis(rawText, openParenIndex);
        if (closeParenIndex == -1)
        {
            Debug.LogWarning($"[ParseOldStyleHighlightTag] No matching ')' found for highlight tag!");
            return startIndex + 1;
        }

        string colorStr = rawText.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
        Color highlightColor = ParseColor(colorStr);

        // Check if there's a closing '>' right after the parenthesis
        if (closeParenIndex + 1 < rawText.Length && rawText[closeParenIndex + 1] == '>')
        {
            // Simple tag: <highlight(color)text>
            int contentEnd = closeParenIndex + 2;
            while (contentEnd < rawText.Length && rawText[contentEnd] != '<')
            {
                contentEnd++;
            }

            string text = rawText.Substring(closeParenIndex + 2, contentEnd - closeParenIndex - 2);
            AddTextToken(tokens, text, true, highlightColor, 0, defaultShakeSpeed, 0);
            return contentEnd;
        }
        else
        {
            // Nested tag: <highlight(color)<nested content>>
            int contentStart = closeParenIndex + 1;
            int tagEndIndex = FindMatchingTagEndForNested(rawText, contentStart);
            if (tagEndIndex == -1)
            {
                Debug.LogWarning($"[ParseOldStyleHighlightTag] No closing '>' found for nested highlight tag!");
                return startIndex + 1;
            }

            string nestedContent = rawText.Substring(contentStart, tagEndIndex - contentStart);
            List<TextToken> nestedTokens = ParseText(nestedContent);

            foreach (var token in nestedTokens)
            {
                token.hasHighlight = true;
                token.highlightColor = highlightColor;
                tokens.Add(token);
            }

            return tagEndIndex + 1;
        }
    }

    private int ParseOldStyleShakeTag(string rawText, int startIndex, List<TextToken> tokens)
    {
        int openParenIndex = rawText.IndexOf('(', startIndex + 6);
        if (openParenIndex == -1)
        {
            Debug.LogWarning($"[ParseOldStyleShakeTag] No '(' found for shake tag!");
            return startIndex + 1;
        }

        int closeParenIndex = FindMatchingParenthesis(rawText, openParenIndex);
        if (closeParenIndex == -1)
        {
            Debug.LogWarning($"[ParseOldStyleShakeTag] No matching ')' found for shake tag!");
            return startIndex + 1;
        }

        string paramsStr = rawText.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
        string[] parts = paramsStr.Split(',');

        float intensity = defaultShakeIntensity;
        float speed = defaultShakeSpeed;

        if (parts.Length >= 1)
            float.TryParse(parts[0].Trim(), out intensity);
        if (parts.Length >= 2)
            float.TryParse(parts[1].Trim(), out speed);

        if (closeParenIndex + 1 < rawText.Length && rawText[closeParenIndex + 1] == '>')
        {
            int contentEnd = closeParenIndex + 2;
            while (contentEnd < rawText.Length && rawText[contentEnd] != '<')
            {
                contentEnd++;
            }

            string text = rawText.Substring(closeParenIndex + 2, contentEnd - closeParenIndex - 2);
            AddTextToken(tokens, text, false, Color.white, intensity, speed, 0);
            return contentEnd;
        }
        else
        {
            int contentStart = closeParenIndex + 1;
            int tagEndIndex = FindMatchingTagEndForNested(rawText, contentStart);
            if (tagEndIndex == -1)
            {
                Debug.LogWarning($"[ParseOldStyleShakeTag] No closing '>' found for nested shake tag!");
                return startIndex + 1;
            }

            string nestedContent = rawText.Substring(contentStart, tagEndIndex - contentStart);
            List<TextToken> nestedTokens = ParseText(nestedContent);

            foreach (var token in nestedTokens)
            {
                token.shakeIntensity = intensity;
                token.shakeSpeed = speed;
                tokens.Add(token);
            }

            return tagEndIndex + 1;
        }
    }

    private int ParseOldStyleDelayTag(string rawText, int startIndex, List<TextToken> tokens)
    {
        int openParenIndex = rawText.IndexOf('(', startIndex + 6);
        if (openParenIndex == -1)
        {
            Debug.LogWarning($"[ParseOldStyleDelayTag] No '(' found for delay tag!");
            return startIndex + 1;
        }

        int closeParenIndex = FindMatchingParenthesis(rawText, openParenIndex);
        if (closeParenIndex == -1)
        {
            Debug.LogWarning($"[ParseOldStyleDelayTag] No matching ')' found for delay tag!");
            return startIndex + 1;
        }

        string delayStr = rawText.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
        float.TryParse(delayStr, out float delay);

        if (closeParenIndex + 1 < rawText.Length && rawText[closeParenIndex + 1] == '>')
        {
            int contentEnd = closeParenIndex + 2;
            while (contentEnd < rawText.Length && rawText[contentEnd] != '<')
            {
                contentEnd++;
            }

            string text = rawText.Substring(closeParenIndex + 2, contentEnd - closeParenIndex - 2);
            AddTextToken(tokens, text, false, Color.white, 0, defaultShakeSpeed, delay);
            return contentEnd;
        }
        else
        {
            int contentStart = closeParenIndex + 1;
            int tagEndIndex = FindMatchingTagEndForNested(rawText, contentStart);
            if (tagEndIndex == -1)
            {
                Debug.LogWarning($"[ParseOldStyleDelayTag] No closing '>' found for nested delay tag!");
                return startIndex + 1;
            }

            string nestedContent = rawText.Substring(contentStart, tagEndIndex - contentStart);
            List<TextToken> nestedTokens = ParseText(nestedContent);

            if (nestedTokens.Count > 0)
                nestedTokens[0].delayBefore = delay;

            tokens.AddRange(nestedTokens);
            return tagEndIndex + 1;
        }
    }

    private int FindMatchingParenthesis(string text, int openParenIndex)
    {
        int depth = 1;
        for (int i = openParenIndex + 1; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private int FindMatchingTagEndForNested(string text, int startIndex)
    {
        int depth = 0;

        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '<')
            {
                depth++;
            }
            else if (text[i] == '>')
            {
                if (depth == 0)
                {
                    return i; // Found the matching closing '>' for the outer tag
                }
                depth--;
            }
        }

        return -1;
    }

    private void AddTextToken(List<TextToken> tokens, string text, bool hasHighlight, Color highlightColor,
                              float shakeIntensity, float shakeSpeed, float delay)
    {
        if (!string.IsNullOrEmpty(text))
        {
            tokens.Add(new TextToken
            {
                text = text,
                delayBefore = delay,
                shakeIntensity = shakeIntensity,
                shakeSpeed = shakeSpeed,
                hasHighlight = hasHighlight,
                highlightColor = highlightColor
            });
        }
    }

    private Color ParseColor(string colorStr)
    {
        colorStr = colorStr.Trim();
        string colorStrLower = colorStr.ToLower();

        // Try hex format
        if (colorStr.StartsWith("#"))
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(colorStr, out color))
                return color;
        }

        // Try direct RGB/RGBA format
        if (colorStr.Contains(",") && !colorStrLower.StartsWith("rgb"))
        {
            string[] parts = colorStr.Split(',');

            if (parts.Length >= 3)
            {
                float r, g, b, a = 1f;

                if (float.TryParse(parts[0].Trim(), out r) &&
                    float.TryParse(parts[1].Trim(), out g) &&
                    float.TryParse(parts[2].Trim(), out b))
                {
                    // Check if values are in 0-255 range or 0-1 range
                    if (r > 1f) r /= 255f;
                    if (g > 1f) g /= 255f;
                    if (b > 1f) b /= 255f;

                    if (parts.Length >= 4)
                    {
                        float.TryParse(parts[3].Trim(), out a);
                        if (a > 1f) a /= 255f;
                    }

                    return new Color(r, g, b, a);
                }
            }
        }

        // Try rgb(r,g,b) or rgba(r,g,b,a) format
        if (colorStrLower.StartsWith("rgb"))
        {
            int start = colorStr.IndexOf('(') + 1;
            int end = colorStr.IndexOf(')');
            if (start > 0 && end > start)
            {
                string values = colorStr.Substring(start, end - start);
                string[] parts = values.Split(',');

                if (parts.Length >= 3)
                {
                    float r, g, b, a = 1f;

                    if (float.TryParse(parts[0].Trim(), out r) &&
                        float.TryParse(parts[1].Trim(), out g) &&
                        float.TryParse(parts[2].Trim(), out b))
                    {
                        if (r > 1f) r /= 255f;
                        if (g > 1f) g /= 255f;
                        if (b > 1f) b /= 255f;

                        if (parts.Length >= 4)
                        {
                            float.TryParse(parts[3].Trim(), out a);
                            if (a > 1f) a /= 255f;
                        }

                        return new Color(r, g, b, a);
                    }
                }
            }
        }

        // Try named colors
        switch (colorStrLower)
        {
            case "red": return Color.red;
            case "green": return Color.green;
            case "blue": return Color.blue;
            case "yellow": return Color.yellow;
            case "cyan": return Color.cyan;
            case "magenta": return Color.magenta;
            case "white": return Color.white;
            case "black": return Color.black;
            case "gray": case "grey": return Color.gray;
            case "orange": return new Color(1f, 0.5f, 0f);
            case "purple": return new Color(0.5f, 0f, 0.5f);
            case "pink": return new Color(1f, 0.75f, 0.8f);
            case "brown": return new Color(0.6f, 0.3f, 0f);
            default: return Color.yellow;
        }
    }
}
