using BepInEx.Logging;

namespace White_Knuckle_Multiplayer
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
        private static ManualLogSource baseLogger;

        /// <summary>Enable or disable all Debug-level logging.</summary>
        public static bool DebugEnabled { get; set; } = false;

        /// <summary>Initialize the logger. Call in BepInEx plugin's Awake/OnEnable.</summary>
        public static void Init(ManualLogSource logger)
        {
            baseLogger = logger;
        }

        /// <summary>Basic BepInEx logging methods.</summary>
        public static void Info(string message) => baseLogger?.LogInfo(message);
        public static void Debug(string message)
        {
            if (DebugEnabled)
                baseLogger?.LogDebug(message);
        }
        public static void Warn(string message) => baseLogger?.LogWarning(message);
        public static void Error(string message) => baseLogger?.LogError(message);

        /// <summary>Server-context logging.</summary>
        public static class Server
        {
            public static void Info(string message) => baseLogger?.LogInfo($"[SERVER] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    baseLogger?.LogDebug($"[SERVER] {message}");
            }
            public static void Warn(string message) => baseLogger?.LogWarning($"[SERVER] {message}");
            public static void Error(string message) => baseLogger?.LogError($"[SERVER] {message}");
        }

        /// <summary>Client-context logging.</summary>
        public static class Client
        {
            public static void Info(string message) => baseLogger?.LogInfo($"[CLIENT] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    baseLogger?.LogDebug($"[CLIENT] {message}");
            }
            public static void Warn(string message) => baseLogger?.LogWarning($"[CLIENT] {message}");
            public static void Error(string message) => baseLogger?.LogError($"[CLIENT] {message}");
        }

        /// <summary>Network-context logging.</summary>
        public static class Net
        {
            public static void Info(string message) => baseLogger?.LogInfo($"[NET] {message}");
            public static void Debug(string message)
            {
                if (DebugEnabled)
                    baseLogger?.LogDebug($"[NET] {message}");
            }
            public static void Warn(string message) => baseLogger?.LogWarning($"[NET] {message}");
            public static void Error(string message) => baseLogger?.LogError($"[NET] {message}");
        }
    }
}
