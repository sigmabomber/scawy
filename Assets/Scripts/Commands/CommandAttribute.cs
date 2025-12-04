using System;

namespace Debugging
{
    /// <summary>
    /// Marks a method as a debug command that can be executed from the in-game console.
    /// 
    /// </summary>
    /// <remarks>
    /// Example: [Command("god", "Toggles invincibility", "god")]
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// The name of the command as typed in the console.
        /// </summary>
        /// <example>"god", "give_item", "teleport"</example>
        public string Name { get; }

        /// <summary>
        /// A brief description of what the command does.
        /// </summary>
        /// <example>"Toggles player invincibility"</example>
        public string Description { get; }

        /// <summary>
        /// The usage syntax showing parameters.
        /// </summary>
        /// <example>"god", "give_item [item_id] [quantity]"</example>
        public string Usage { get; }

        /// <summary>
        /// Creates a new debug command attribute.
        /// </summary>
        /// <param name="name">The command name (what users type).</param>
        /// <param name="description">What the command does.</param>
        /// <param name="usage">Usage syntax with parameters.</param>
        /// <example>
        /// [Command("heal", "Restores player health", "heal [amount=100]")]
        /// public void HealCommand(string[] args) { }
        /// </example>
        public CommandAttribute(string name, string description = "", string usage = "")
        {
            Name = name;
            Description = description;
            Usage = usage;
        }
    }
}