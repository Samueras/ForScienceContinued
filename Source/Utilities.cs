using UnityEngine;

namespace ForScience
{
  public static class Utilities
  {
    public static void LogDebugMessage(string message, params string[] strings)//thanks Sephiroth018 for this part
    {
#if DEBUG
      Debug.Log(string.Format("[For Science] " + message, strings));
#endif
    }
    public static void clampToScreen(ref Rect rect)
    {
      rect.x = Mathf.Clamp(rect.x, 0, Screen.width - rect.width);
      rect.y = Mathf.Clamp(rect.y, 0, Screen.height - rect.height);
    }
  }
}
