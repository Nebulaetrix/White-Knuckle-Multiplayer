using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace White_Knuckle_Multiplayer.Utils;

/// <summary>
/// Support state for a mod version.
/// </summary>
public enum ModSupportState
{
    Unsupported = 0,
    Unknown     = 1,
    Supported   = 2
}

/// <summary>
/// Specifies where a mod is required.
/// </summary>
public enum ModRequirement
{
    Server,
    Client,
    Both
}

/// <summary>
/// A version constraint or exception rule for a mod GUID.
/// </summary>
public class VersionRule
{
    public string Operator { get; }
    public Version TargetVersion { get; }
    public ModSupportState StateIfMatch { get; }

    public VersionRule(string op, Version version, ModSupportState state)
    {
        Operator = op;
        TargetVersion = version;
        StateIfMatch = state;
    }
    

    /// <summary>
    /// Checks if the given version matches this rule (for non-exact operators).
    /// </summary>
    public bool Matches(Version v)
    {
        return Operator switch
        {
            ">"   => v.CompareTo(TargetVersion) > 0,
            ">="  => v.CompareTo(TargetVersion) >= 0,
            "<"   => v.CompareTo(TargetVersion) < 0,
            "<="  => v.CompareTo(TargetVersion) <= 0,
            "any" => true,
            _      => false,
        };
    }
}

/// <summary>
/// Global registry for mod compatibility rules, including exceptions.
/// </summary>
public static class ModCompability
{
    static ModCompability()
    {
        // Nebulaetrix.WK_Extra_Modes,0.1.2
        AddRule("Nebulaetrix.WK_Extra_Modes", "*", ModSupportState.Unsupported);
        AddRequirement("Nebulaetrix.WK_Extra_Modes", ModRequirement.Both);
        AddRule("White_Knuckle_Multiplayer", "*", ModSupportState.Supported);
        AddRequirement("White_Knuckle_Multiplayer", ModRequirement.Both);
        AddRequirement("com.sinai.unityexplorer", ModRequirement.Client);

        var rawMods = ModListHelper.GetLoadedModsList();
        var states = CheckModList(rawMods);

        var modTexts = new List<string>(states.Count);
        foreach (var mod in states)
        {
            string version = rawMods
                                 .FirstOrDefault(r => r.StartsWith(mod.Key + ","))
                                 ?.Split(',')[1]
                             ?? "unknown";
            
            
            modTexts.Add($"{mod.Key} v{version}:\n\t{mod.Value.State} ({mod.Value.Requirement})");
        }
        LogManager.Info($"Mods Loaded:\n{string.Join("\n", modTexts)}");
    }
    
    // GUID -> list of version rules (in registration order)
    private static readonly Dictionary<string, List<VersionRule>> Rules =
        new Dictionary<string, List<VersionRule>>(StringComparer.OrdinalIgnoreCase);
    
    // GUID -> requirement (Server, Client, or Both)
    private static readonly Dictionary<string, ModRequirement> Requirements =
        new Dictionary<string, ModRequirement>(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Attribute you can apply on a plugin class to declare compatibility and requirement.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CompatibleWithMultiplayerAttribute : Attribute
    {
        public bool IsCompatible { get; }
        public ModRequirement Requirement { get; }
        public CompatibleWithMultiplayerAttribute(bool compatible, ModRequirement requirement = ModRequirement.Both)
        {
            IsCompatible = compatible;
            Requirement = requirement;
        }
    }
    
    /// <summary>
    /// Register a version constraint or exception for a mod GUID.
    /// Constraint examples: <c>"=1.2.3"</c>, <c>">1.0.0"</c>, <c>"&lt;=2.5.0"</c>, <c>"*"</c> (any version).
    /// </summary>
    public static void AddRule(string guid, string constraint, ModSupportState state)
    {
        if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(constraint))
        {
            throw new ArgumentException("GUID and constraint must be non-empty");
        }

        // parse operator
        var op = string.Empty;
        string versionPart;

        // '*' shorthand for any version
        if (constraint.Trim() == "*")
        {
            op = "any";
            versionPart = "0.0.0.0";
        }
        else
        {
            op = new[] { ">=", "<=", ">", "<", "=" }
                .FirstOrDefault(o => constraint.StartsWith(o, StringComparison.Ordinal));

            if (op == null)
                throw new ArgumentException("Invalid version operator in constraint: " + constraint);

            versionPart = constraint.Substring(op.Length).Trim();
            if (string.IsNullOrWhiteSpace(versionPart))
                throw new ArgumentException("Missing version in constraint: " + constraint);
        }

        if (!Version.TryParse(versionPart, out var version))
            throw new ArgumentException("Invalid version format: " + versionPart);

        var rule = new VersionRule(op, version, state);
        if (!Rules.TryGetValue(guid, out var list))
        {
            list = new List<VersionRule>();
            Rules[guid] = list;
        }
        list.Add(rule);
    }
    
    /// <summary>
    /// Register a requirement for a mod GUID.
    /// </summary>
    public static void AddRequirement(string guid, ModRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(guid))
            throw new ArgumentException("GUID must be non-empty");
        Requirements[guid] = requirement;
    }

    /// <summary>
    /// Gets the requirement for a mod GUID (default Both).
    /// </summary>
    public static ModRequirement GetRequirement(string guid)
    {
        var result = Requirements.GetValueOrDefault(guid, ModRequirement.Both);
        foreach (var requirement in Requirements)
        {
            //LogManager.Info($"{requirement.Key} -> {requirement.Value}");
        }
        //LogManager.Info($"{guid} -> {result}");
        
        return result;
    }
    
    /// <summary>
    /// Gets the support state for a specific mod GUID and version.
    /// First checks plugin metadata (Chainloader.PluginInfos) for a key "CompatibleWithMultiplayer" ("true" or "false").
    /// If present, true =&gt; Supported, false =&gt; Unsupported.
    /// Otherwise, falls back to registered rules.
    /// </summary>
    public static ModSupportState GetSupportState(string guid, string version)
    {
        if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(version))
            return ModSupportState.Unknown;
        
        
        // 0) Check plugin metadata for explicit compatibility flag
        if (Chainloader.PluginInfos.TryGetValue(guid, out var info))
        {
            if (info != null && info.Instance != null)
            {
                var pluginType = info.Instance.GetType();
            
                var attr = pluginType.GetCustomAttributes(
                        typeof(CompatibleWithMultiplayerAttribute), false)
                    .FirstOrDefault() as CompatibleWithMultiplayerAttribute;
                if (attr != null)
                {
                    Requirements[guid] = attr.Requirement;
                    return attr.IsCompatible ? ModSupportState.Supported : ModSupportState.Unsupported;
                }

                try
                {
                    // Define default values
                    ModRequirement defaultRequirement = ModRequirement.Both;

                    if (info.Instance?.Config != null)
                    {
                        // Bind configuration entries
                        var compatibleEntry = info.Instance.Config.Bind("Multiplayer", "multiplayerCompatible",
                            false, "Indicates if the mod is compatible with multiplayer.");
                        var requirementEntry = info.Instance.Config.Bind("Multiplayer", "multiplayerRequirement",
                            defaultRequirement, "Specifies the mod's requirement: Server, Client, or Both.");

                        // Store requirement from configuration
                        Requirements[guid] = requirementEntry.Value;
                
                        if (compatibleEntry.Value)
                            return compatibleEntry.Value ? ModSupportState.Supported : ModSupportState.Unsupported;
                    }
                }
                catch
                {
                    LogManager.Warn($"Couldn't determine Multiplayer compability for: {info.Metadata.Name}");
                }
            }
        }
        
        // 1) No explicit metadata, use rules
        if (!Rules.TryGetValue(guid, out var rules) || rules.Count == 0)
            return ModSupportState.Unknown;

        if (!Version.TryParse(version, out var ver))
            return ModSupportState.Unknown;

        // 2) Exact-match exceptions: operator '='
        foreach (var rule in rules.Where(r => r.Operator == "="))
        {
            if (ver.CompareTo(rule.TargetVersion) == 0)
                return rule.StateIfMatch;
        }

        // 3) 'any' operator rule
        foreach (var rule in rules.Where(r => r.Operator == "any"))
        {
            return rule.StateIfMatch;
        }

        // 4) Range rules
        var state = ModSupportState.Unknown;
        foreach (var rule in rules.Where(r => r.Operator != "=" && r.Operator != "any"))
        {
            if (rule.Matches(ver))
                state = MaxState(state, rule.StateIfMatch);
        }

        return state == ModSupportState.Unknown ? ModSupportState.Unknown : state;
    }
    
    /// <summary>
    /// Evaluate a list of "GUID;Version" entries and return each's support state,
    /// filtered by requirement (Client-only can be ignored if needed).
    /// </summary>
    public static Dictionary<string, (ModSupportState State, ModRequirement Requirement)> CheckModList(string[] mods, bool includeClient = true)
    {
        var dict = new Dictionary<string, (ModSupportState, ModRequirement)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in mods)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            var parts = entry.Split([';', ','], 2);
            if (parts.Length != 2)
                continue;

            var guid = parts[0].Trim();
            var ver  = parts[1].Trim();
            var req  = GetRequirement(guid);
            if (!includeClient && req == ModRequirement.Client)
                continue;

            var state = GetSupportState(guid, ver);
            dict[guid] = (state, req);
        }
        return dict;
    }
    
    private static ModSupportState MaxState(ModSupportState a, ModSupportState b)
        => (ModSupportState)Math.Max((int)a, (int)b);
}

// Doesnt work
// and also should be checked when multiplayer UI is loaded
[HarmonyPatch(typeof(Chainloader))]
public static class ModCompatibilityBootstrap
{
    
    [HarmonyPatch(nameof(Chainloader.Start)), HarmonyPostfix]
    private static void OnAllPluginsLoaded()
    {
        // At this point, BepInEx has already populated Chainloader.PluginInfos
        var allGuids = Chainloader.PluginInfos.Keys.ToArray();
        var versions = allGuids
            .Select(guid => $"{guid};{Chainloader.PluginInfos[guid].Metadata.Version}")
            .ToArray();

        var states = ModCompability.CheckModList(versions, includeClient: true);

        // Gather the ones that are explicitly Unsupported
        var unsupported = states
            .Where(kv => kv.Value.State == ModSupportState.Unsupported)
            .Select(kv => $"{kv.Key} v{Chainloader.PluginInfos[kv.Key].Metadata.Version}")
            .ToList();

        if (unsupported.Count > 0)
        {
            // TODO: Save the unsupported mods
            LogManager.Warn(
                $"Incompatible mods detected:\n • {string.Join("\n • ", unsupported)}");
        }
    }
}