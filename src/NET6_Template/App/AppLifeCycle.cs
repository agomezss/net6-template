using System;

namespace NET6_Template.App
{
    public static class AppLifeCycle
    {
        public static bool ShutdownReceived { get; private set; }
        public static bool ReadyToShutdown { get { return JobCount <= 0; } }
        static int JobCount { get; set; }

        static event Action TerminationCallbacks;

        public static void Terminate()
        {
            if (ShutdownReceived) return;

            ShutdownReceived = true;

            if (TerminationCallbacks != null)
            {
                TerminationCallbacks.Invoke();
            }
        }

        public static void Register(Action terminationCallback)
        {
            if (ShutdownReceived) return;

            JobCount++;

            TerminationCallbacks += () =>
            {
                try
                {
                    terminationCallback.Invoke();
                }
                catch
                {
                }

                JobCount--;
            };
        }

        public static void Unregister(Action unregisterCallback)
        {
            JobCount--;

            try
            {
                TerminationCallbacks -= unregisterCallback;
            }
            catch
            {
            }
        }
    }
}
