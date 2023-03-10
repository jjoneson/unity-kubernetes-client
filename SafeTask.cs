using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityKubernetesClient
{
    public static class SafeTask
    {
        public static CancellationTokenSource cancellationTokenSource =
            new CancellationTokenSource();

        public static Task<TResult> Run<TResult>(Func<Task<TResult>> f) =>
            SafeTask.Run<TResult>((object)f);

        public static Task<TResult> Run<TResult>(Func<TResult> f) =>
            SafeTask.Run<TResult>((object)f);

        public static Task Run(Func<Task> f) => SafeTask.Run<object>((object)f);

        public static Task Run(Action f) => SafeTask.Run<object>((object)f);

        private static async Task<TResult> Run<TResult>(object f)
        {
            // We have to store a token and cannot simply query the source
            // after awaiting, as the token source is replaced with a new one
            // upon exiting play mode.
            CancellationToken token = SafeTask.cancellationTokenSource.Token;
            TResult result = default;

            try
            {
                // Pass token to Task.Run() as well, otherwise upon cancelling
                // its status will change to faulted instead of cancelled.
                // https://stackoverflow.com/a/72145763/2037431

                if (f is Func<Task<TResult>> g)
                {
                    result = await Task.Run(() => g(), token);
                }
                else if (f is Func<TResult> h)
                {
                    result = await Task.Run(() => h(), token);
                }
                else if (f is Func<Task> i)
                {
                    await Task.Run(() => i(), token);
                }
                else if (f is Action j)
                {
                    await Task.Run(() => j(), token);
                }
            }
            catch (Exception e)
            {
                // We log unobserved exceptions with an UnobservedTaskException handler, but those
                // are only handled when garbage collection happens.
                // We thus force exceptions to be logged here - at least for SafeTasks.
                // If a failing SafeTask is awaited, the exception will be logged twice, but that's
                // ok.
                UnityEngine.Debug.LogException(e);
                throw;
            }

            SafeTask.ThrowIfCancelled(token);

            return result;
        }

        private static void ThrowIfCancelled(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    "An asynchronous task has been cancelled due to exiting play mode.",
                    token
                );
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnLoad() {
            // Prevent unobserved task exceptions from being swallowed.
            // This happens when:
            //  * An unawaited Task fails;
            //  * A Task chained with `.ContinueWith()` fails and exceptions are
            //    not explicitly handled in the function passed to it.
            //
            // Note that this event handler works for both Tasks and SafeTasks.
            //
            // Also note that this handler may not fire right away. It seems to
            // only run when garbage collection happens (for example, in the
            // editor after script reloading).
            // Experimentally, calling `System.GC.Colect()` after the exception
            //  (using a small `Task.Delay()` to ensure it runs after the
            // exception is thrown) caused exceptions to be logged right away.
            TaskScheduler.UnobservedTaskException +=
                (_, e) => UnityEngine.Debug.LogException(e.Exception);

            // Cancel pending `Task.Run()` calls when exiting play mode, as
            // Unity won't do that for us.
            // See "Limitations of async and await tasks" (https://docs.unity3d.com/2022.2/Documentation/Manual/overview-of-dot-net-in-unity.html)
            // This only works in SafeTasks, so `Task.Run()` should never be
            // used directly.
            UnityEditor.EditorApplication.playModeStateChanged +=
                (change) => {
                    if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                        SafeTask.cancellationTokenSource.Cancel();
                        SafeTask.cancellationTokenSource.Dispose();
                        SafeTask.cancellationTokenSource = new CancellationTokenSource();
                    }
                };
        }
#endif
    }
}
