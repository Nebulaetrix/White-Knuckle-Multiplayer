using BepInEx.Logging;

namespace WhiteKnuckleMP.Utils
{
    /// <summary>
    /// Centralized logging with four contexts:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Type</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Base</term>
    ///         <description>plain BepInEx logger</description>
    ///     </item>
    ///     <item>
    ///         <term>Server</term>
    ///         <description>prefixes messages with [SERVER]</description>
    ///     </item>
    ///     <item>
    ///         <term>Client</term>
    ///         <description>prefixes messages with [CLIENT]</description>
    ///     </item>
    ///     <item>
    ///         <term>Net</term>
    ///         <description>prefixes messages with [NET]</description>
    ///     </item>
    ///     <item>
    ///         <term>Registry</term>
    ///         <description>prefixes messages with [Registry]</description>
    ///     </item>
    ///     <item>
    ///         <term>Framework</term>
    ///         <description>prefixes messages with [FRAMEWORK]</description>
    ///     </item>
    ///     <item>
    ///         <term>UI</term>
    ///         <description>prefixes messages with [UI]</description>
    ///     </item>
    ///     <item>
    ///         <term>StateManager</term>
    ///         <description>prefixes messages with [StateManager]</description> 
    ///     </item>
    /// </list>
    ///
    /// <para>
    /// Initialize once in plugin entrypoint:
    ///     <code>LogManager.Init(Logger);</code>
    /// </para>
    /// <para>
    /// Then to enable or disable debug logs at runtime:
    ///     <code>LogManager.DebugEnabled = true;</code>
    /// </para>
    /// <para>
    /// Then use anywhere like so:
    ///     <code>
    ///         LogManager.Server.Info("Started");
    ///         LogManager.Client.Debug("Value={0}", value);
    ///     </code>
    /// </para>
    /// </summary>
    public static class LogManager
    {
        private static ManualLogSource _baseLogger = null!;

        /// <summary>Enable or disable all Debug-level logging.</summary>
        public static bool DebugEnabled { get; set; } = false;

        /// <summary>Initialize the logger. Call in BepInEx plugin's Awake/OnEnable.</summary>
        public static void Init(ManualLogSource logger)
        {
            _baseLogger = logger;
        }

        /// <summary>Basic BepInEx logging methods.</summary>
        public static void Info(string message) => _baseLogger?.LogInfo(message);
        public static void Debug(string message)
        {
            if (DebugEnabled)
                _baseLogger?.LogDebug(message);
        }
        public static void Warn(string message) => _baseLogger?.LogWarning(message);
        public static void Error(string message) => _baseLogger?.LogError(message);

        /// <summary>Server-context logging.</summary>
        public static class Server
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[SERVER] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[SERVER] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[SERVER] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[SERVER] {message}");
        }

        /// <summary>Client-context logging.</summary>
        public static class Client
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[CLIENT] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[CLIENT] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[CLIENT] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[CLIENT] {message}");
        }

        /// <summary>Network-context logging.</summary>
        public static class Net
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[NET] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[NET] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[NET] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[NET] {message}");
        }
        
        /// <summary>Registry-context logging.</summary>
        public static class Registry
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[REGISTRY] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[REGISTRY] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[REGISTRY] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[REGISTRY] {message}");
        }

        /// <summary>Framework-context logging.</summary>
        public static class Framework
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[FRAMEWORK] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[FRAMEWORK] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[FRAMEWORK] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[FRAMEWORK] {message}");
        }

        /// <summary>UI-context logging.</summary>
        public static class UI
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[UI] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[UI] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[UI] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[UI] {message}");
        }
        
        /// <summary>StateManager-context logging.</summary>
        public static class StateManager
        {
            public static void Info(string message) => _baseLogger?.LogInfo($"[StateManager] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    _baseLogger?.LogDebug($"[StateManager] {message}");
            }
            public static void Warn(string message) => _baseLogger?.LogWarning($"[StateManager] {message}");
            public static void Error(string message) => _baseLogger?.LogError($"[StateManager] {message}");
        }
    }
}
