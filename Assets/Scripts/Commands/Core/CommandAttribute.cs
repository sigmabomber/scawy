using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;


namespace Debugging
{
    /// <summary>
    /// Marks a method as a debug command that can be executed from the in-game console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public string Category { get; }
        public CommandPermission Permission { get; }
        public string[] Aliases { get; }

        /// <summary>
        /// Creates a new debug command attribute.
        /// </summary>
        public CommandAttribute(string name, string description = "", string usage = "",
                               string category = "General", CommandPermission permission = CommandPermission.Player,
                               params string[] aliases)
        {
            Name = name;
            Description = description;
            Usage = string.IsNullOrEmpty(usage) ? name : usage;
            Category = category;
            Permission = permission;
            Aliases = aliases ?? new string[0];
        }
    }

    /// <summary>
    /// Command permission levels.
    /// </summary>
    public enum CommandPermission
    {
        Developer,  // Only in dev builds
        Admin,      // Requires admin privileges
        Player,     // Available to all players
        Debug       // Only when debug mode is enabled
    }

    /// <summary>
    /// Utility class for parsing command arguments.
    /// </summary>
    public static class CommandParser
    {
        public static bool TryParseInt(string arg, out int value, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(arg))
            {
                value = defaultValue;
                return true;
            }
            return int.TryParse(arg, out value);
        }

        public static bool TryParseFloat(string arg, out float value, float defaultValue = 0f)
        {
            if (string.IsNullOrEmpty(arg))
            {
                value = defaultValue;
                return true;
            }
            return float.TryParse(arg, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParseBool(string arg, out bool value, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(arg))
            {
                value = defaultValue;
                return true;
            }

            if (bool.TryParse(arg, out value))
                return true;

            string lower = arg.ToLower();
            if (lower == "1" || lower == "on" || lower == "yes" || lower == "true" || lower == "y" || lower == "enable")
            {
                value = true;
                return true;
            }
            if (lower == "0" || lower == "off" || lower == "no" || lower == "false" || lower == "n" || lower == "disable")
            {
                value = false;
                return true;
            }

            return false;
        }

        public static bool TryParseVector3(string arg, out Vector3 value, Vector3 defaultValue = default)
        {
            value = defaultValue;

            if (string.IsNullOrEmpty(arg))
                return true;

            // Remove parentheses if present
            arg = arg.Trim('(', ')', ' ', '[', ']');

            string[] parts = arg.Split(',');
            if (parts.Length != 3)
                return false;

            if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float x))
                return false;

            if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float y))
                return false;

            if (!float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float z))
                return false;

            value = new Vector3(x, y, z);
            return true;
        }

        public static string[] SplitArguments(string input)
        {
            var args = new List<string>();
            bool inQuotes = false;
            string currentArg = "";

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(currentArg))
                    {
                        args.Add(currentArg);
                        currentArg = "";
                    }
                }
                else if (c == '\\' && i + 1 < input.Length)
                {
                    // Handle escape sequences
                    i++;
                    currentArg += input[i];
                }
                else
                {
                    currentArg += c;
                }
            }

            if (!string.IsNullOrEmpty(currentArg))
            {
                args.Add(currentArg);
            }

            return args.ToArray();
        }
    }

    
}