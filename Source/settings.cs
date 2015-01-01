using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

/**
 *		ForScience.settings
 *		Addition to the ForScience mod
 *		Original code from SpaceTiger(http://forum.kerbalspaceprogram.com/members/137260)
 *		and	SpaceKitty(http://forum.kerbalspaceprogram.com/members/137262)
 *		
 *		This code is licensed under the Attribution-ShareAlike 4.0 (CC BY-SA 4.0)
 *		creative commons license. See (http://creativecommons.org/licenses/by-sa/4.0/)
 *		for full details.
 *		
 *  Original Github: https://github.com/Xarun/ForScience
 *	
 *		to-do: 
 *						currently nothing
 */
namespace ForScience
{
  class settings
  {
    public static string scienceCutoffString = "2";
    public static string spriteAnimationFPSString = "25";
    public static float scienceCutoff = 2;
    public static float spriteAnimationFPS = 25;
    public static bool autoScience = false;
    public static bool runOneTimeScience = false;
    public static bool transferScience = false;
    public static bool showSettings = false;
    public static Rect windowPosition = new Rect(0f, 0f, 0f, 0f);
    private static string settingsPath = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/ForScience/settings.cfg";
    private static string ForScienceSettingsNode = "ForScienceSettings";
    private static ConfigNode settingsContainer = new ConfigNode(ForScienceSettingsNode);

    public static void saveToDisk()
    {
      ConfigNode savenode = new ConfigNode();
      addValueToContainer("scienceCutoff", scienceCutoffString);
      addValueToContainer("spriteAnimationFPS", spriteAnimationFPSString);
      addValueToContainer("autoScience", autoScience.ToString());
      addValueToContainer("runOneTimeScience", runOneTimeScience.ToString());
      addValueToContainer("transferScience", transferScience.ToString());

      addValueToContainer("windowPositionX", windowPosition.x.ToString());
      addValueToContainer("windowPositionY", windowPosition.y.ToString());

      savenode.AddNode(settingsContainer);
      savenode.Save(settingsPath);
    }

    public static void save()
    {
      if (String.IsNullOrEmpty(settings.spriteAnimationFPSString))
      {
        settings.spriteAnimationFPSString = "0";
      }
      if (String.IsNullOrEmpty(settings.scienceCutoffString))
      {
        settings.scienceCutoffString = "0";
      }

      settings.scienceCutoff = int.Parse(settings.scienceCutoffString);
      settings.spriteAnimationFPS = int.Parse(settings.spriteAnimationFPSString);
      ForScience.sprite.SetFramerate(settings.spriteAnimationFPS); ;
      ForScience.resetStates();
      ForScience.RunScience();
      foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes(ForScienceSettingsNode))
      {
        if (!node.HasValue("scienceCutoff"))
          node.AddValue("scienceCutoff", "");
        if (!node.HasValue("spriteAnimationFPS"))
          node.AddValue("spriteAnimationFPS", "");
        if (!node.HasValue("autoScience"))
          node.AddValue("autoScience", "");
        if (!node.HasValue("runOneTimeScience"))
          node.AddValue("runOneTimeScience", "");
        if (!node.HasValue("transferScience"))
          node.AddValue("transferScience", "");
        if (!node.HasValue("windowPositionX"))
          node.AddValue("windowPositionX", "");
        if (!node.HasValue("windowPositionY"))
          node.AddValue("windowPositionY", "");

        node.AddValue("scienceCutoff", scienceCutoffString);
        node.AddValue("spriteAnimationFPS", spriteAnimationFPSString);
        node.AddValue("autoScience", autoScience.ToString());
        node.AddValue("runOneTimeScience", runOneTimeScience.ToString());
        node.AddValue("transferScience", transferScience.ToString());

        node.AddValue("windowPositionX", windowPosition.x.ToString());
        node.AddValue("windowPositionY", windowPosition.y.ToString());
      }
    }
    private static void addValueToContainer(string name, string value)
    {
      if (settingsContainer.HasValue(name))
      {
        settingsContainer.SetValue(name, value);
      }
      else
      {
        settingsContainer.AddValue(name, value);
      }
    }
    public static void load()
    {
      //Debug.Log("ForScience: Loading settings");
      foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes(ForScienceSettingsNode))
      {
        if (node.HasValue("scienceCutoff"))
        {
          scienceCutoffString = node.GetValue("scienceCutoff");
        }
        if (node.HasValue("spriteAnimationFPS"))
        {
          spriteAnimationFPSString = node.GetValue("spriteAnimationFPS");
        }
        if (node.HasValue("runOneTimeScience"))
        {
          runOneTimeScience = Convert.ToBoolean(node.GetValue("runOneTimeScience"));
        }
        if (node.HasValue("transferScience"))
        {
          transferScience = Convert.ToBoolean(node.GetValue("transferScience"));
        }
        if (node.HasValue("autoScience"))
        {
          autoScience = Convert.ToBoolean(node.GetValue("autoScience"));
        }
        if (node.HasValue("windowPositionX"))
        {
          windowPosition.x = float.Parse(node.GetValue("windowPositionX"));
        }
        if (node.HasValue("windowPositionY"))
        {
          windowPosition.y = float.Parse(node.GetValue("windowPositionY"));
        }
      }
    }
  }
}
