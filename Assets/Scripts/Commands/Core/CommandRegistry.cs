using Debugging;
using Doody.Framework.ObjectiveSystem;
using Doody.GameEvents;
using Doody.GameEvents.Health;
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

   
            // health
            RegisterCommand("addhealth", "Adds Health to Player", "addhealth [amount]",
               "Health", CommandPermission.Admin, AddHealthCommand, "addhp", "givehp");
          
            RegisterCommand("removehealth", "Remove Health to Player", "removehealth [amount]",
             "Health", CommandPermission.Admin, RemoveHealthCommand, "removehp", "takehp", "takehealth", "takedmg");

            RegisterCommand("maxhealth", "Adjusts Players Max Health", "maxhealth [amount]",
             "Health", CommandPermission.Admin, MaxHealthCommand, "maxhp");

            RegisterCommand("god", "Toggles god mode", "god",
                "Health", CommandPermission.Admin, GodCommand, "invincible");
            RegisterCommand("healthdata", "Gets Health Data", "healthdata",
             "Health", CommandPermission.Admin, GetHealthData, "hpdata");

   // movement


            RegisterCommand("walkspeed", "Adjust players walk speed", "walkspeed [speed]", 
                "Movement", CommandPermission.Admin, WalkSpeedCommand);
            RegisterCommand("sprintspeed", "Adjust players sprint speed", "sprintspeed [speed]",
              "Movement", CommandPermission.Admin, SprintSpeedCommand);
            RegisterCommand("crouchspeed", "Adjust players crouch speed", "crouchspeed [speed]",
              "Movement", CommandPermission.Admin, CrouchSpeedCommand);
            RegisterCommand("crouchspeed", "Adjust players crouch speed", "crouchspeed [speed]",
             "Movement", CommandPermission.Developer, GetMovementDataCommand);

            RegisterCommand("movementdata", "Get Movement Data", "movementdata",
        "Stamina", CommandPermission.Developer, GetMovementDataCommand);


            // stamina

            RegisterCommand("maxstamina", "Adjust players max stamina", "maxstamina [value]",
              "Stamina", CommandPermission.Admin, MaxStaminaCommand);

            RegisterCommand("staminadrainrate", "Adjust players stamina drain rate", "staminadrainrate [value]",
             "Stamina", CommandPermission.Admin, StaminaDrainRateCommand);

            RegisterCommand("staminaregenrate", "Adjust players stamina regen rate", "staminaregenrate [value]",
            "Stamina", CommandPermission.Admin, StaminaRegenRateCommand);

            RegisterCommand("staminaregendelay", "Adjust players stamina regen delay", "staminaregendelay [value]",
           "Stamina", CommandPermission.Admin, StaminaRegenDelayCommand);

            RegisterCommand("minstamina", "Adjust players minimum stamina to sprint", "minstamina [value]",
           "Stamina", CommandPermission.Admin, MinStaminaToSprintCommand);
            RegisterCommand("infinitestamina", "Infinite stamina", "infinitestamina",
       "Stamina", CommandPermission.Admin, InfiniteStaminaCommand);

            RegisterCommand("staminadata", "Get Stamina Data", "staminadata",
        "Stamina", CommandPermission.Developer, GetStaminaDataCommand);




            // objective

            RegisterCommand("addobjective", "Adds a new Objective", "addobjective [name] [amount] [type]",
                "Objective", CommandPermission.Developer, AddObjectiveCommand, "addtask");

            RegisterCommand("addnote", "Adds a new note to the journal", "addnote [title] [content] [date?]",
         "Journal", CommandPermission.Developer, AddNoteCommand, "createnote");


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
                return; 
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
                string errorMsg;
                if (commandName.ToLower() == "bleh")
                {

                     errorMsg = $"bleh :3";
                    ConsoleUI.PrintSystem(errorMsg);
                }
                else
                {
                    errorMsg = $"Unknown command: '{commandName}'";

                    if (suggestions.Count > 0)
                    {
                        errorMsg += $"\nDid you mean: {string.Join(", ", suggestions)}?";
                    }

                    ConsoleUI.PrintError(errorMsg);
                   
                }
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


        #region Health Commands
        private void GodCommand(string[] args)
        {
            bool activate = !HealthManager.instance.isInvincible;
            HealthManager.instance.isInvincible = activate;

            ConsoleUI.PrintSuccess($"God mode {(activate ? "on" : "off")}");
        }

        private void AddHealthCommand(string[] args)
        {
            if (args.Length == 0)
            {
                ConsoleUI.PrintError("Usage: addhealth [amount]");
                ConsoleUI.Print("Example: addhealth 2");
                return;
            }
            if (!int.TryParse(args[0], out var result))
            {
                ConsoleUI.PrintError($"Invalid number: {args[0]}");
                return;
            }

            ConsoleUI.PrintSuccess($"Successfully added {result}hp");
            Events.Publish(new AddHealthEvent(result));
        }

        private void MaxHealthCommand(string[] args)
        {
            if (args.Length == 0)
            {
                ConsoleUI.PrintError("Usage: maxhealth [amount]");
                ConsoleUI.Print("Example: maxhealth 2");
                return;
            }
            if (!int.TryParse(args[0], out var result))  
            {
                ConsoleUI.PrintError($"Invalid number: {args[0]}");
                return;
            }
            HealthManager.instance.maxHealth = result;


            ConsoleUI.PrintSuccess($"Successfully changed max health to {result}");
        }

        private void RemoveHealthCommand(string[] args)
        {
            if (args.Length == 0)
            {
                ConsoleUI.PrintError("Usage: removehealth [amount]");
                ConsoleUI.Print("Example: removehealth 2");
                return;
            }
            if (!int.TryParse(args[0], out var result))  
            {
                ConsoleUI.PrintError($"Invalid number: {args[0]}");
                return;
            }

            ConsoleUI.PrintSuccess($"Successfully removed {result}hp");
            Events.Publish(new RemoveHealthEvent(result));
        }


        private void GetHealthData(string[] args)
        {
            ConsoleUI.PrintSuccess("\nHealth Data:\n");
            ConsoleUI.PrintSuccess($"Max Health: {HealthManager.instance.maxHealth}\nCurrent Health: {HealthManager.instance.currentHealth}\nInvincible: {HealthManager.instance.isInvincible}");
        }
        #endregion


        #region Movement Commands
        private void WalkSpeedCommand(string[] args)
        {
            float newSpeed = args.Length > 0 ? float.Parse(args[0]) : PlayerController.Instance.GetBaseWalkSpeed();
            PlayerController.Instance.SetBaseMovementValues(
                newSpeed,
                PlayerController.Instance.GetBaseSprintSpeed(),
                PlayerController.Instance.GetBaseCrouchSpeed(),
                PlayerController.Instance.GetBaseMouseSensitivity()
            );
            ConsoleUI.PrintSuccess($"Successfully changed Players Walkspeed to {newSpeed}");
        }

        private void SprintSpeedCommand(string[] args)
        {
            float newSpeed = args.Length > 0 ? float.Parse(args[0]) : PlayerController.Instance.GetBaseSprintSpeed();
            PlayerController.Instance.SetBaseMovementValues(
                PlayerController.Instance.GetBaseWalkSpeed(),
                newSpeed,
                PlayerController.Instance.GetBaseCrouchSpeed(),
                PlayerController.Instance.GetBaseMouseSensitivity()
            );
            ConsoleUI.PrintSuccess($"Successfully changed Players SprintSpeed to {newSpeed}");
        }

        private void CrouchSpeedCommand(string[] args)
        {
            float newSpeed = args.Length > 0 ? float.Parse(args[0]) : PlayerController.Instance.GetBaseCrouchSpeed();
            PlayerController.Instance.SetBaseMovementValues(
                PlayerController.Instance.GetBaseWalkSpeed(),
                PlayerController.Instance.GetBaseSprintSpeed(),
                newSpeed,
                PlayerController.Instance.GetBaseMouseSensitivity()
            );
            ConsoleUI.PrintSuccess($"Successfully changed Players CrouchSpeed to {newSpeed}");
        }

        private void GetMovementDataCommand(string[] args)
        {
            ConsoleUI.PrintSuccess("\nMovement Data:\n");
            ConsoleUI.PrintSuccess($"Walk Speed: {PlayerController.Instance.GetBaseWalkSpeed()}\nCrouch Speed: {PlayerController.Instance.GetBaseCrouchSpeed()}\nSprint Speed: {PlayerController.Instance.GetBaseSprintSpeed()}");
        }
        #endregion




        #region Stamina Commands
        private void MaxStaminaCommand(string[] args)
        {
            PlayerController.Instance.maxStamina = args.Length > 0 ? float.Parse(args[0]) : 100f;
            ConsoleUI.PrintSuccess($"Successfully changed Players Max stamina to {PlayerController.Instance.maxStamina}");
        }

        private void StaminaDrainRateCommand(string[] args)
        {
            PlayerController.Instance.staminaDrainRate = args.Length > 0 ? float.Parse(args[0]) : 20f;
            ConsoleUI.PrintSuccess($"Successfully changed Players stamina drain rate to {PlayerController.Instance.staminaDrainRate}");
        }

        private void StaminaRegenRateCommand(string[] args)
        {
            PlayerController.Instance.staminaRegenRate = args.Length > 0 ? float.Parse(args[0]) : 15f;
            ConsoleUI.PrintSuccess($"Successfully changed Players stamina regen rate to {PlayerController.Instance.staminaRegenRate}");
        }

        private void StaminaRegenDelayCommand(string[] args)
        {
            PlayerController.Instance.staminaRegenDelay = args.Length > 0 ? float.Parse(args[0]) : 1f;
            ConsoleUI.PrintSuccess($"Successfully changed Players stamina regen delay to {PlayerController.Instance.staminaRegenDelay}");
        }

        private void MinStaminaToSprintCommand(string[] args)
        {
            PlayerController.Instance.minStaminaToSprint = args.Length > 0 ? float.Parse(args[0]) : 10f;
            ConsoleUI.PrintSuccess($"Successfully changed Players minimum stamina to sprint to {PlayerController.Instance.minStaminaToSprint}");
        }
        private void InfiniteStaminaCommand(string[] args)
        {
            bool activate = !PlayerController.Instance.infiniteStamina;
            PlayerController.Instance.infiniteStamina = activate;
            ConsoleUI.PrintSuccess($"Infinite stamina {(activate ? "on" : "off")}");



        }


        private void GetStaminaDataCommand(string[] args)
        {
            ConsoleUI.PrintSuccess("Stamina Data:\n");
            ConsoleUI.PrintSuccess($"Max Stamina: {PlayerController.Instance.maxStamina}\nStamina Drain Rate: {PlayerController.Instance.staminaDrainRate}\nStamina Regen Rate: {PlayerController.Instance.staminaRegenRate}\nStamina Regen Delay: {PlayerController.Instance.staminaRegenDelay}\nMinimum Stamina to Sprint: {PlayerController.Instance.minStaminaToSprint}\nInfinite Stamina: {PlayerController.Instance.infiniteStamina}");
        }
        #endregion


        #region Objective Commands 

        private void AddObjectiveCommand(string[] args)
        {
            if (args.Length < 2)
            {
                ConsoleUI.PrintError(
                    "Usage: giveobjective [name] [amount?] [type (count / collection / bool)]");
                ConsoleUI.Print(
                    "Examples:\n" +
                    "giveobjective Enemy Hunter 10 count\n" +
                    "giveobjective Gem Collector 5 collection\n" +
                    "giveobjective Boss Slayer bool");
                return;
            }

            string typeArg = args[^1].ToLower();

            switch (typeArg)
            {
                case "count":
                case "collection":
                    {
                        if (args.Length < 3)
                        {
                            ConsoleUI.PrintError("This objective type requires an amount.");
                            return;
                        }

                        if (!int.TryParse(args[^2], out int amount) || amount <= 0)
                        {
                            ConsoleUI.PrintError("Amount must be a positive number.");
                            return;
                        }

                        string name = string.Join(" ", args.Take(args.Length - 2));
                        
                        string description = name;

                        if (typeArg == "count")
                        {
                            Events.Publish(new CountObjective(
                                
                                name,
                                description,
                                amount
                            ));
                        }
                        else
                        {
                            Events.Publish(new CollectionObjective(
                                
                                name,
                                description,
                                amount
                            ));
                        }

                        ConsoleUI.PrintSuccess($"Objective added: {name}");
                        break;
                    }

                case "bool":
                case "boolean":
                    {
                        string name = string.Join(" ", args.Take(args.Length - 1));
                    
                        string description = $"Complete: {name}";

                        Events.Publish(new BooleanObjective(
                            
                            name,
                            description
                        ));

                        ConsoleUI.PrintSuccess($"Objective added: {name}");
                        break;
                    }

                default:
                    ConsoleUI.PrintError($"Unknown objective type: {typeArg}");
                    break;
            }
        }



        #endregion

        #region Notes Commands
        private void AddNoteCommand(string[] args)
        {
            if (args.Length < 2)
            {
                ConsoleUI.PrintError(
                    "Usage: addnote [title] [content] [date?]\n" +
                    "Note: Use quotes for multi-word titles/content: addnote \"Ancient Scroll\" \"Found in the crypt...\" \"Day 15\"");
                ConsoleUI.Print(
                    "Examples:\n" +
                    "addnote \"Mysterious Herb\" \"Found glowing blue herbs in cave. Need to identify.\" \"Day 7\"\n" +
                    "addnote \"Villager Rumor\" \"Old man mentioned missing children near the woods\"\n" +
                    "addnote \"Password\" \"Temple entrance code: 7429\"");
                return;
            }

            // Support for quoted strings
            List<string> parsedArgs = ParseQuotedArguments(args);

            if (parsedArgs.Count < 2)
            {
                ConsoleUI.PrintError("Title and content are required.");
                return;
            }

            string title = parsedArgs[0];
            string content = parsedArgs[1];
            string date = parsedArgs.Count > 2 ? parsedArgs[2] : "";

            // Optional: Title length limit
            if (title.Length > 50)
            {
                ConsoleUI.PrintWarning($"Note title is long ({title.Length} chars). Consider shortening.");
            }

            // Optional: Content length limit
            if (content.Length > 500)
            {
                ConsoleUI.PrintWarning($"Note content is long ({content.Length} chars). Consider splitting into multiple notes.");
            }

            // Check if note with similar title already exists
            var existingNotes = NoteManager.Instance?.GetAllNotes();
            if (existingNotes != null)
            {
                foreach (var note in existingNotes)
                {
                    if (note.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                    {
                        ConsoleUI.PrintWarning($"A note with title \"{title}\" already exists. Continue? (y/n)");
                        // You'd need to handle confirmation here, or just proceed
                        // For simplicity, we'll proceed with a warning
                        break;
                    }
                }
            }

            // Add the note using event system
            Events.Publish(new AddNoteEvent(title, content, date));

            ConsoleUI.PrintSuccess($"Note added: \"{title}\"");
            if (!string.IsNullOrEmpty(date))
            {
                ConsoleUI.Print($"Date: {date}");
            }
        }

        // Helper method to parse quoted arguments (supports spaces within quotes)
        private List<string> ParseQuotedArguments(string[] args)
        {
            List<string> result = new List<string>();
            string currentArg = "";
            bool inQuotes = false;

            foreach (string arg in args)
            {
                if (inQuotes)
                {
                    currentArg += " " + arg;
                    if (arg.EndsWith("\""))
                    {
                        result.Add(currentArg.Trim().Trim('"'));
                        currentArg = "";
                        inQuotes = false;
                    }
                }
                else if (arg.StartsWith("\""))
                {
                    if (arg.EndsWith("\"") && arg.Length > 1)
                    {
                        result.Add(arg.Substring(1, arg.Length - 2));
                    }
                    else
                    {
                        currentArg = arg.Substring(1);
                        inQuotes = true;
                    }
                }
                else
                {
                    result.Add(arg);
                }
            }

            // Add any remaining argument
            if (!string.IsNullOrEmpty(currentArg))
            {
                result.Add(currentArg.Trim('"'));
            }

            return result;
        }

        #endregion

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