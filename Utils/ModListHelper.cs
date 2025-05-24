using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;

namespace White_Knuckle_Multiplayer.Utils;

public static class ModListHelper
{
    public class ModComparisonResult
    {
        /// <summary>True if GUID sets and versions are identical.</summary>
        public bool AllMatch { get; set; }

        /// <summary>Entries present in list B, but not in list A.</summary>
        public List<string> MissingOnA { get; set; } = [];

        /// <summary>Entries present in list A, but not in list B.</summary>
        public List<string> MissingOnB { get; set; } = [];

        /// <summary>GUIDs present in both but with differing versions.</summary>
        public Dictionary<string, (string VersionA, string VersionB)> VersionMismatches { get; set; }
            = new Dictionary<string, (string, string)>();
        
        /// <summary>Any entries that didn’t parse as "GUID{;,}Version".</summary>
        public List<string> MalformedEntries { get; } = [];
    }
    
    #region Helper Functions
    /// <summary>
    /// Returns an array of strings, like:
    /// <c>["GUID1;1.0.0", "GUID2;1.2.3"]</c>
    /// for all loaded BepInEx plugins.
    /// </summary>
    public static string[] GetLoadedModsList()
    {
        // Chainloader.PluginInfos: IDictionary<string, PluginInfo>
        //   Key = plugin GUID
        //   Value = PluginInfo.Metadata.Version
        return Chainloader.PluginInfos
            .Select(kvp => $"{kvp.Key},{kvp.Value.Metadata.Version}").ToArray();
    }
    #endregion
    
    #region Comparison Functions
    /// <summary>
    /// Compare two mod‐lists (array of "GUID;Version" or "GUID,Version") in any order.
    /// </summary>
    /// <param name="listA">First array, each element "GUID;Version".</param>
    /// <param name="listB">Second array, each element "GUID;Version".</param>
    /// <returns>A ModComparisonResult describing matches and mismatches.</returns>
    public static ModComparisonResult CompareModLists(string[] listA, string[] listB)
    {
        
        var result = new ModComparisonResult();

        // local function to parse a list into dict, recording malformed
        Dictionary<string,string> Parse(string[] list, string listName)
        {
            var dict = new Dictionary<string, string>();
            foreach (var raw in list)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                // split on either ';' or ',' (first occurrence)
                var sepIndex = raw.IndexOf(';');
                if (sepIndex < 0) sepIndex = raw.IndexOf(',');
                if (sepIndex < 0)
                {
                    result.MalformedEntries.Add($"[{listName}] \"{raw}\"");
                    continue;
                }

                var guid    = raw.Substring(0, sepIndex).Trim();
                var version = raw.Substring(sepIndex + 1).Trim();
                if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(version))
                {
                    result.MalformedEntries.Add($"[{listName}] \"{raw}\"");
                    continue;
                }

                // if duplicate GUIDs, last one wins
                dict[guid] = version;
            }
            return dict;
        }

        var dictA = Parse(listA, "A");
        var dictB = Parse(listB, "B");

        // detect missing
        foreach (var guid in dictB.Keys.Except(dictA.Keys))
            result.MissingOnA.Add($"{guid};{dictB[guid]}");
        foreach (var guid in dictA.Keys.Except(dictB.Keys))
            result.MissingOnB.Add($"{guid};{dictA[guid]}");

        // detect version mismatches
        foreach (var guid in dictA.Keys.Intersect(dictB.Keys))
        {
            var vA = dictA[guid];
            var vB = dictB[guid];
            if (!vA.Equals(vB, StringComparison.Ordinal))
                result.VersionMismatches[guid] = (vA, vB);
        }

        // all match if nothing is wrong and no malformed
        result.AllMatch =
            result.MissingOnA.Count         == 0 &&
            result.MissingOnB.Count         == 0 &&
            result.VersionMismatches.Count  == 0 &&
            result.MalformedEntries.Count   == 0;

        return result;
    }
    #endregion
}