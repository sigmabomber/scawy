using Debugging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace Doody.Debugging
{
    public class CommandRegistry : MonoBehaviour
    {
        #region Singleton
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
        #endregion

        #region Command Data Classes
        [System.Serializable]
        public class CommandData
        {
            public string name;
            public string[] aliases;
            public string description;
            public string usage;
            public string category;
            public CommandPermission permission;
            public System.Action<string[]> action;
            public Func<string[], Task> asyncAction;

            public bool IsAsync => asyncAction != null;

            public CommandData(string name, string description, string usage, string category,
                             CommandPermission permission, string[] aliases,
                             System.Action<string[]> action, Func<string[], Task> asyncAction)
            {
                this.name = name;
                this.description = description;
                this.usage = usage;
                this.category = category;
                this.permission = permission;
                this.aliases = aliases ?? new string[0];
                this.action = action;
                this.asyncAction = asyncAction;
            }

            public bool Matches(string commandName)
            {
                if (name.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                    return true;

                foreach (var alias in aliases)
                {
                    if (alias.Equals(commandName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        }

        [System.Serializable]
        public class CommandHistoryEntry
        {
            public string command;
            public DateTime timestamp;
            public bool success;

            public CommandHistoryEntry(string command, bool success)
            {
                this.command = command;
                this.timestamp = DateTime.Now;
                this.success = success;
            }
        }
        #endregion

        #region Fields
        private Dictionary<string, CommandData> commands = new Dictionary<string, CommandData>();
        private List<CommandHistoryEntry> commandHistory = new List<CommandHistoryEntry>();
        private const int MAX_HISTORY = 100;

        [Header("Settings")]
        [SerializeField] private bool enableLogging = false; 
        [SerializeField] private bool requireCheatCode = false;
        [SerializeField] private string adminPassword = "admin123";
        [SerializeField] private bool isAdminMode = false;
        [SerializeField] private bool autoOpenConsoleOnError = true;
        [SerializeField] private bool logToUnityConsole = false; 

        public event Action<string> OnCommandExecuted;
        public event Action<string, bool> OnCommandCompleted;

        // Tab completion
        private List<string> currentSuggestions = new List<string>();
        private int suggestionIndex = -1;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);


            RegisterDefaultCommands();
        }

        void Start()
        {
            StartCoroutine(DeferredInitialization());
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleUnityLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleUnityLog;
        }
        #endregion

        #region Initialization
        private IEnumerator DeferredInitialization()
        {

            yield return null;

            yield return StartCoroutine(RegisterAttributeCommandsAsync());

            if (enableLogging && logToUnityConsole)
            {
            }
        }

        private IEnumerator RegisterAttributeCommandsAsync()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            int assemblyCount = 0;

            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("Unity") ||
                    assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("Mono") ||
                    assembly.FullName.StartsWith("mscorlib"))
                {
                    continue;
                }

                Type[] types = null;

                try
                {
                    types = assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(MonoBehaviour)) || t.IsClass)
                        .ToArray();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                if (types != null)
                {
                    foreach (var type in types)
                    {
                        RegisterCommandsFromType(type);
                    }

                    assemblyCount++;
                    if (assemblyCount % 5 == 0)
                    {
                        yield return null;
                    }
                }
            }
        }

        private void RegisterDefaultCommands()
        {
            RegisterCommand("help", "Shows all available commands", "help [command_name]",
                "System", CommandPermission.Player, HelpCommand, "?", "commands");

            RegisterCommand("clear", "Clears console output", "clear",
                "System", CommandPermission.Player, ClearCommand, "cls");

            RegisterCommand("history", "Shows command history", "history [count]",
                "System", CommandPermission.Player, HistoryCommand, "hist");

            RegisterCommand("echo", "Prints a message", "echo <message>",
                "System", CommandPermission.Player, EchoCommand);

            RegisterAsyncCommand("admin", "Toggles admin mode", "admin [password]",
                "Admin", CommandPermission.Admin, AdminCommand, "sudo");

            RegisterCommand("time", "Gets or sets game time scale", "time [scale]",
                "Debug", CommandPermission.Debug, TimeScaleCommand, "timescale");

            RegisterCommand("fps", "Shows FPS information", "fps [target]",
                "Debug", CommandPermission.Debug, FPSCommand, "frame");

            RegisterCommand("mem", "Shows memory usage", "mem",
                "Debug", CommandPermission.Debug, MemoryCommand, "memory");

            RegisterCommand("pause", "Pauses or resumes the game", "pause",
                "Game", CommandPermission.Player, PauseCommand, "p");

            RegisterAsyncCommand("quit", "Quits the game", "quit",
                "Game", CommandPermission.Admin, QuitCommand, "exit");

            RegisterCommand("god", "Toggles god mode", "god",
                "Player", CommandPermission.Admin, GodCommand, "invincible");

            RegisterCommand("infinitestamina", "Infinite stamina", "infinitestamina",
                "Player", CommandPermission.Admin, InfiniteStaminaCommand, "run forever");
        }

        private void RegisterCommandsFromType(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Static | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var commandAttr = method.GetCustomAttributes(typeof(CommandAttribute), false)
                    .FirstOrDefault() as CommandAttribute;

                if (commandAttr == null)
                    continue;

                // Create the action
                System.Action<string[]> action = null;
                Func<string[], Task> asyncAction = null;

                if (method.ReturnType == typeof(Task))
                {
                    // Async method
                    asyncAction = (args) =>
                    {
                        object instance = null;
                        if (!method.IsStatic)
                        {
                            instance = FindObjectOfType(type);
                            if (instance == null)
                            {
                                ConsoleUI.PrintError($"No instance of {type.Name} found for command '{commandAttr.Name}'");
                                return Task.CompletedTask;
                            }
                        }

                        try
                        {
                            var result = method.Invoke(instance, new object[] { args }) as Task;
                            return result ?? Task.CompletedTask;
                        }
                        catch (Exception e)
                        {
                            ConsoleUI.PrintError($"Error invoking async command '{commandAttr.Name}': {e.Message}");
                            return Task.CompletedTask;
                        }
                    };
                }
                else
                {
                    // Sync method
                    action = (args) =>
                    {
                        object instance = null;
                        if (!method.IsStatic)
                        {
                            instance = FindObjectOfType(type);
                            if (instance == null)
                            {
                                ConsoleUI.PrintError($"No instance of {type.Name} found for command '{commandAttr.Name}'");
                                return;
                            }
                        }

                        try
                        {
                            method.Invoke(instance, new object[] { args });
                        }
                        catch (Exception e)
                        {
                            ConsoleUI.PrintError($"Error invoking command '{commandAttr.Name}': {e.Message}");
                        }
                    };
                }

                // Register the command
                if (asyncAction != null)
                {
                    RegisterAsyncCommand(commandAttr.Name, commandAttr.Description,
                        commandAttr.Usage, commandAttr.Category, commandAttr.Permission,
                        asyncAction, commandAttr.Aliases);
                }
                else
                {
                    RegisterCommand(commandAttr.Name, commandAttr.Description,
                        commandAttr.Usage, commandAttr.Category, commandAttr.Permission,
                        action, commandAttr.Aliases);
                }
            }
        }
        #endregion

        #region Command Registration
        public void RegisterCommand(string name, string description, string usage,
                                   string category, CommandPermission permission,
                                   System.Action<string[]> action, params string[] aliases)
        {
            string key = name.ToLower();

            if (commands.ContainsKey(key))
            {
                return; // Silently skip duplicates
            }

            var commandData = new CommandData(name, description, usage, category,
                permission, aliases, action, null);

            commands.Add(key, commandData);

            // Also register aliases for quick lookup
            foreach (var alias in aliases)
            {
                if (!string.IsNullOrEmpty(alias) && !commands.ContainsKey(alias.ToLower()))
                {
                    commands.Add(alias.ToLower(), commandData);
                }
            }
        }

        public void RegisterAsyncCommand(string name, string description, string usage,
                                        string category, CommandPermission permission,
                                        Func<string[], Task> asyncAction, params string[] aliases)
        {
            string key = name.ToLower();

            if (commands.ContainsKey(key))
            {
                return; // Silently skip duplicates
            }

            var commandData = new CommandData(name, description, usage, category,
                permission, aliases, null, asyncAction);

            commands.Add(key, commandData);

            foreach (var alias in aliases)
            {
                if (!string.IsNullOrEmpty(alias) && !commands.ContainsKey(alias.ToLower()))
                {
                    commands.Add(alias.ToLower(), commandData);
                }
            }
        }
        #endregion

        #region Command Execution
        public async Task<bool> ExecuteCommandAsync(string input, bool silent = false)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!silent)
            {
                OnCommandExecuted?.Invoke(input);
            }

            string[] parts = CommandParser.SplitArguments(input);
            if (parts.Length == 0)
                return false;

            string commandName = parts[0].ToLower();
            string[] args = parts.Length > 1 ? parts.Skip(1).ToArray() : new string[0];

            // Find command
            if (!commands.TryGetValue(commandName, out CommandData command))
            {
                // Try to find by partial match for better UX
                var suggestions = GetCommandSuggestions(commandName).Take(3).ToList();
                string errorMsg = $"Unknown command: '{commandName}'";

                if (suggestions.Count > 0)
                {
                    errorMsg += $"\nDid you mean: {string.Join(", ", suggestions)}?";
                }

                ConsoleUI.PrintError(errorMsg);
                AddToHistory(input, false);

                if (autoOpenConsoleOnError && ConsoleUI.Instance != null && !ConsoleUI.Instance.consolePanel.activeSelf)
                {
                    ConsoleUI.Instance.OpenConsole();
                }

                return false;
            }

            // Check permissions
            if (!CheckPermission(command))
            {
                ConsoleUI.PrintError($"Insufficient permissions for command: {command.name}");
                AddToHistory(input, false);
                return false;
            }

            try
            {
                bool success = false;

                if (command.IsAsync)
                {
                    await command.asyncAction.Invoke(args);
                    success = true;
                }
                else
                {
                    command.action?.Invoke(args);
                    success = true;
                }

                AddToHistory(input, success);

                if (!silent)
                {
                    OnCommandCompleted?.Invoke(input, success);
                }

                return success;
            }
            catch (Exception e)
            {
                ConsoleUI.PrintError($"Error executing command '{commandName}': {e.Message}");

                if (logToUnityConsole)
                {
                    Debug.LogError($"[Command] Failed: {input}\nError: {e}");
                }

                AddToHistory(input, false);
                OnCommandCompleted?.Invoke(input, false);

                return false;
            }
        }

        public bool ExecuteCommand(string input, bool silent = false)
        {
            _ = ExecuteCommandAsync(input, silent);
            return true;
        }
        #endregion

        #region Permission System
        private bool CheckPermission(CommandData command)
        {
            switch (command.permission)
            {
                case CommandPermission.Developer:
                    return Debug.isDebugBuild;

                case CommandPermission.Admin:
                    return isAdminMode || !requireCheatCode;

                case CommandPermission.Debug:
                    return Debug.isDebugBuild || isAdminMode;

                case CommandPermission.Player:
                default:
                    return true;
            }
        }

        public void SetAdminMode(bool enabled, string password = "")
        {
            if (requireCheatCode && !string.IsNullOrEmpty(adminPassword))
            {
                if (password != adminPassword)
                {
                    ConsoleUI.PrintError("Invalid admin password!");
                    return;
                }
            }

            isAdminMode = enabled;
            ConsoleUI.PrintSuccess($"Admin mode {(enabled ? "enabled" : "disabled")}");
        }

        public bool IsAdminMode => isAdminMode;
        #endregion

        #region History Management
        private void AddToHistory(string command, bool success)
        {
            var entry = new CommandHistoryEntry(command, success);
            commandHistory.Insert(0, entry);

            if (commandHistory.Count > MAX_HISTORY)
            {
                commandHistory.RemoveAt(MAX_HISTORY);
            }
        }

        public List<CommandHistoryEntry> GetHistory(int count = 10)
        {
            return commandHistory.Take(count).ToList();
        }

        public void ClearHistory()
        {
            commandHistory.Clear();
            ConsoleUI.PrintSystem("Command history cleared.");
        }
        #endregion

        #region Command Suggestions and Tab Completion
        public IEnumerable<string> GetCommandSuggestions(string partialInput)
        {
            if (string.IsNullOrWhiteSpace(partialInput))
            {
                return commands.Values
                    .Where(c => CheckPermission(c))
                    .Select(c => c.name)
                    .Distinct()
                    .OrderBy(n => n);
            }

            string partial = partialInput.ToLower();

            return commands.Values
                .Where(c => CheckPermission(c) && (
                    c.name.StartsWith(partial, StringComparison.OrdinalIgnoreCase) ||
                    c.aliases.Any(a => a.StartsWith(partial, StringComparison.OrdinalIgnoreCase)) ||
                    c.name.Contains(partial, StringComparison.OrdinalIgnoreCase)
                ))
                .Select(c => c.name)
                .Distinct()
                .OrderBy(n => n);
        }

        public string GetTabCompletion(string currentInput)
        {
            if (string.IsNullOrWhiteSpace(currentInput))
                return "";

            var parts = CommandParser.SplitArguments(currentInput);
            if (parts.Length == 0)
                return "";

            if (parts.Length == 1)
            {
                var suggestions = GetCommandSuggestions(parts[0]).ToList();
                if (suggestions.Count == 0)
                    return currentInput;

                if (currentSuggestions.Count == 0 || !currentSuggestions.SequenceEqual(suggestions))
                {
                    currentSuggestions = suggestions;
                    suggestionIndex = 0;
                }
                else
                {
                    suggestionIndex = (suggestionIndex + 1) % currentSuggestions.Count;
                }

                return currentSuggestions[suggestionIndex];
            }
            else
            {
                string commandName = parts[0].ToLower();
                if (commands.TryGetValue(commandName, out CommandData command))
                {
                    return currentInput;
                }
            }

            return currentInput;
        }

        public void ResetTabCompletion()
        {
            currentSuggestions.Clear();
            suggestionIndex = -1;
        }
        #endregion

        #region Built-in Commands
        private void HelpCommand(string[] args)
        {
            if (args.Length > 0)
            {
                string commandName = args[0].ToLower();
                var command = commands.Values.FirstOrDefault(c => c.Matches(commandName));

                if (command != null)
                {
                    if (!CheckPermission(command))
                    {
                        ConsoleUI.PrintError($"Insufficient permissions to view command: {commandName}");
                        return;
                    }

                    ConsoleUI.PrintSystem($"=== {command.name.ToUpper()} ===");
                    ConsoleUI.Print($"Description: {command.description}");
                    ConsoleUI.Print($"Usage: {command.usage}");
                    ConsoleUI.Print($"Category: {command.category}");
                    ConsoleUI.Print($"Permission: {command.permission}");

                    if (command.aliases.Length > 0)
                        ConsoleUI.Print($"Aliases: {string.Join(", ", command.aliases)}");
                }
                else
                {
                    ConsoleUI.PrintError($"Command '{args[0]}' not found.");
                }
                return;
            }

            var groupedCommands = GetAllCommands()
                .GroupBy(c => c.category)
                .OrderBy(g => g.Key);

            bool hasCommands = false;
            foreach (var group in groupedCommands)
            {
                hasCommands = true;
                ConsoleUI.PrintSystem($"\n=== {group.Key.ToUpper()} ===");

                foreach (var cmd in group.OrderBy(c => c.name))
                {
                    string aliasText = cmd.aliases.Length > 0 ? $" (aliases: {string.Join(", ", cmd.aliases)})" : "";
                    ConsoleUI.Print($"{cmd.name.PadRight(20)} - {cmd.description}{aliasText}");
                }
            }

            if (!hasCommands)
            {
                ConsoleUI.PrintWarning("No commands available with current permissions.");
            }
            else
            {
                ConsoleUI.PrintSystem($"\nTotal: {groupedCommands.Sum(g => g.Count())} commands");
                ConsoleUI.Print("Use 'help [command]' for detailed information.");
            }
        }


        private void ClearCommand(string[] args)
        {
            ConsoleUI.Instance.ClearOutput();
        }

        private void HistoryCommand(string[] args)
        {
            int count = 10;
            if (args.Length > 0 && int.TryParse(args[0], out int parsedCount))
            {
                count = Mathf.Clamp(parsedCount, 1, 50);
            }

            var history = GetHistory(count);

            if (history.Count == 0)
            {
                ConsoleUI.Print("No command history.");
                return;
            }

            ConsoleUI.PrintSystem($"=== COMMAND HISTORY (Last {history.Count}) ===");
            for (int i = 0; i < history.Count; i++)
            {
                var entry = history[i];

                string status = entry.success ? "[OK]" : "[X]";
                string time = entry.timestamp.ToString("HH:mm:ss");
                ConsoleUI.Print($"[{time}] {status} {entry.command}");
            }
        }

        private void EchoCommand(string[] args)
        {
            if (args.Length == 0)
            {
                ConsoleUI.Print("Usage: echo <message>");
                return;
            }

            ConsoleUI.Print(string.Join(" ", args));
        }

        private async Task AdminCommand(string[] args)
        {
            if (args.Length > 0)
            {
                SetAdminMode(!isAdminMode, args[0]);
            }
            else
            {
                SetAdminMode(!isAdminMode);
            }

            await Task.CompletedTask;
        }

        private void TimeScaleCommand(string[] args)
        {
            if (args.Length > 0)
            {
                if (CommandParser.TryParseFloat(args[0], out float scale))
                {
                    Time.timeScale = Mathf.Clamp(scale, 0f, 100f);
                    ConsoleUI.PrintSuccess($"Time scale set to: {Time.timeScale}");
                }
                else
                {
                    ConsoleUI.PrintError("Invalid time scale value.");
                }
            }
            else
            {
                ConsoleUI.Print($"Current time scale: {Time.timeScale}");
                ConsoleUI.Print($"Real time since startup: {Time.realtimeSinceStartup:F2}s");
            }
        }

        private void FPSCommand(string[] args)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            ConsoleUI.Print($"FPS: {fps:F1}");
            ConsoleUI.Print($"Target FPS: {Application.targetFrameRate}");

            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int targetFps))
                {
                    Application.targetFrameRate = targetFps;
                    ConsoleUI.PrintSuccess($"Target FPS set to: {targetFps}");
                }
            }
        }

        private void MemoryCommand(string[] args)
        {
            long totalMemory = System.GC.GetTotalMemory(false) / 1024 / 1024;
            ConsoleUI.Print($"Memory usage: {totalMemory} MB");

#if UNITY_EDITOR
            ConsoleUI.Print("(Editor mode - memory stats may not be accurate)");
#endif
        }

        private void PauseCommand(string[] args)
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
            bool isPaused = Time.timeScale == 0f;
            ConsoleUI.PrintSuccess($"Game {(isPaused ? "paused" : "resumed")}");
        }

        private async Task QuitCommand(string[] args)
        {
            ConsoleUI.PrintWarning("Quitting...");
            await Task.Delay(100);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void GodCommand(string[] args)
        {
            ConsoleUI.PrintSuccess($"God mode toggled");
        }

        private void InfiniteStaminaCommand(string[] args)
        {
            bool activate = !PlayerController.Instance.infiniteStamina;
            PlayerController.Instance.infiniteStamina = activate;
            ConsoleUI.PrintSuccess($"Infinite stamina {(activate ? "on" : "off")}");



        }
        #endregion

        #region Utility Methods
        public List<CommandData> GetAllCommands()
        {
            return commands.Values
                .Where(c => CheckPermission(c))
                .GroupBy(c => c.name.ToLower())
                .Select(g => g.First())
                .OrderBy(c => c.category)
                .ThenBy(c => c.name)
                .ToList();
        }

        public CommandData GetCommand(string name)
        {
            commands.TryGetValue(name.ToLower(), out CommandData command);
            return command != null && CheckPermission(command) ? command : null;
        }

        public bool HasCommand(string name)
        {
            return commands.ContainsKey(name.ToLower());
        }

        public void UnregisterCommand(string name)
        {
            if (commands.TryGetValue(name.ToLower(), out CommandData command))
            {
                commands.Remove(name.ToLower());

                foreach (var alias in command.aliases)
                {
                    commands.Remove(alias.ToLower());
                }

                ConsoleUI.PrintSystem($"Command '{name}' unregistered.");
            }
        }

        public void PrintCommandList()
        {
            HelpCommand(new string[0]);
        }



        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {

            if (logString.Contains("Unicode") || logString.Contains("font asset"))
                return;

            if (ConsoleUI.Instance == null || !ConsoleUI.Instance.consolePanel.activeSelf)
                return;

            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    ConsoleUI.PrintError(logString);
                    if (autoOpenConsoleOnError)
                    {
                        ConsoleUI.Instance.OpenConsole();
                    }
                    break;

                case LogType.Warning:
                    break;
            }
        }

        public void ExecuteCommandSilent(string command)
        {
            ExecuteCommand(command, true);
        }

        public void ExecuteCommands(params string[] commands)
        {
            foreach (var cmd in commands)
            {
                ExecuteCommand(cmd);
            }
        }

        public void ExecuteScript(string[] scriptLines)
        {
            StartCoroutine(ExecuteScriptCoroutine(scriptLines));
        }

        private IEnumerator ExecuteScriptCoroutine(string[] scriptLines)
        {
            foreach (var line in scriptLines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                ExecuteCommand(line.Trim());
                yield return new WaitForSeconds(0.1f);
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Command System/Clear All Commands")]
        public static void EditorClearAllCommands()
        {
            if (Instance != null)
            {
                Instance.commands.Clear();
                Instance.commandHistory.Clear();
                Debug.Log("[CommandRegistry] All commands cleared.");
            }
        }
#endif
        #endregion
    }
}