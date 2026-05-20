using UnityEngine;

namespace WhiteKnuckleMP.Utils;


public static class TransformUtils
{
    /// <summary>
    /// Finds a child transform recursively within a parent transform.
    /// </summary>
    /// <param name="parent">The parent transform to search within.</param>
    /// <param name="childName">The name of the child transform to find.</param>
    /// <returns>The found child transform, or null if not found.</returns>
    public static Transform? FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            var result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}