using UnityEngine;

namespace ForScience
{
    public static class Helper
    {
        public static void LogDebugMessage(string message)
        {
#if DEBUG
            Debug.Log(string.Format("[For Science] {0}", message));
#endif
        }
    }
}
