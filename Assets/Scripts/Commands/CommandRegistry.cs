using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debugging;
public class CommandRegistry : MonoBehaviour
{
    private static CommandRegistry _instance;
    public static CommandRegistry Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CommandRegistry>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[CommandRegistry]");
                    _instance = go.AddComponent<CommandRegistry>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private Dictionary<string, CommandData> commands = new();

    [System.Serializable]
    public class CommandData
    {
        public string name;
        public string description;
        public string usage;
        public System.Action<string[]> action;

        public CommandData(string name, string description, string usage, System.Action<string[]> action)
        {
            this.name = name;
            this.description = description;
            this.usage = usage;
            this.action = action;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        RegisterDefaultCommands();
    }

    private void RegisterDefaultCommands()
    {
        RegisterAttributeCommands();

        RegisterCommand("help", "Shows all available commands", "help [command_name]", HelpCommand);
        RegisterCommand("clear", "Clears console output", "clear", ClearCommand);
    }

    public void RegisterCommand(string name, string description, string usage, System.Action<string[]> action)
    {
        if (commands.ContainsKey(name.ToLower()))
        {
            Debug.LogWarning($"Command '{name}' is already registered!");
            return;
        }

        commands.Add(name.ToLower(), new CommandData(name, description, usage, action));
    }

    private void RegisterAttributeCommands()
    {
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)));

        foreach (var type in allTypes)
        {
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public |
                                         System.Reflection.BindingFlags.NonPublic |
                                         System.Reflection.BindingFlags.Static |
                                         System.Reflection.BindingFlags.Instance);

            foreach (var method in methods)
            {
                var commandAttr = method.GetCustomAttributes(typeof(CommandAttribute), false)
                    .FirstOrDefault() as CommandAttribute;

                if (commandAttr != null)
                {
                    System.Action<string[]> action = (args) =>
                    {
                        object instance = null;
                        if (!method.IsStatic)
                        {
                            instance = FindObjectOfType(type);
                            if (instance == null)
                            {
                                Debug.LogError($"No instance of {type.Name} found for command '{commandAttr.Name}'");
                                return;
                            }
                        }

                        method.Invoke(instance, new object[] { args });
                    };

                    RegisterCommand(commandAttr.Name, commandAttr.Description,
                                  commandAttr.Usage, action);
                }
            }
        }
    }

    public bool ExecuteCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        string commandName = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts.Skip(1).ToArray() : new string[0];

        if (commands.TryGetValue(commandName, out CommandData command))
        {
            try
            {
                command.action?.Invoke(args);
                return true;
            }
            catch (Exception e)
            {
                ConsoleUI.PrintError($"Error executing command '{commandName}': {e.Message}");
                return false;
            }
        }

        ConsoleUI.PrintError($"Unknown command: {commandName}. Type 'help' for available commands.");
        return false;
    }

    public List<CommandData> GetAllCommands()
    {
        return new List<CommandData>(commands.Values);
    }

    public CommandData GetCommand(string name)
    {
        commands.TryGetValue(name.ToLower(), out CommandData command);
        return command;
    }

    // Built-in commands
    private void HelpCommand(string[] args)
    {
        if (args.Length > 0)
        {
            var cmd = GetCommand(args[0]);
            if (cmd != null)
            {
                ConsoleUI.Print($"{cmd.name}: {cmd.description}\nUsage: {cmd.usage}");
            }
            else
            {
                ConsoleUI.Print($"Command '{args[0]}' not found.");
            }
            return;
        }

        ConsoleUI.Print("=== Available Commands ===");
        foreach (var cmd in commands.Values.OrderBy(c => c.name))
        {
            ConsoleUI.Print($"{cmd.name.PadRight(15)} - {cmd.description}");
        }
        ConsoleUI.Print("Use 'help [command]' for more info.");
    }

    private void ClearCommand(string[] args)
    {
        ConsoleUI.Instance.ClearOutput();


    }
}