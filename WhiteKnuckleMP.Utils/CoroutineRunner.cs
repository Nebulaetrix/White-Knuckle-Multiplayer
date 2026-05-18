using System.Collections;
using UnityEngine;

namespace WhiteKnuckleMP.Utils;

public static class CoroutineRunner
{
    // A hidden, nested MonoBehaviour that will actually run the coroutines
    private class RunnerBehaviour : MonoBehaviour { }

    private static RunnerBehaviour? _runnerInstance;

    // Ensures the runner object exists in the scene before we try to use it
    private static void EnsureRunnerExists()
    {
        if (_runnerInstance == null)
        {
            GameObject runnerObj = new GameObject("[WhiteKnuckle_CoroutineRunner]");
            Object.DontDestroyOnLoad(runnerObj);
            _runnerInstance = runnerObj.AddComponent<RunnerBehaviour>();
        }
    }

    /// <summary>
    /// Starts a coroutine from anywhere in your mod (even static methods).
    /// </summary>
    public static Coroutine StartCoroutine(IEnumerator routine)
    {
        EnsureRunnerExists();
        return _runnerInstance!.StartCoroutine(routine);
    }

    /// <summary>
    /// Stops a previously started coroutine.
    /// </summary>
    public static void StopCoroutine(Coroutine? routine)
    {
        if (_runnerInstance != null && routine != null)
        {
            _runnerInstance.StopCoroutine(routine);
        }
    }
}