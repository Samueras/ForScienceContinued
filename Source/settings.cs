using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP;

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
    public static bool doEVAonlyIfOnGroundWhenLanded = true;
    public static Rect windowPosition = new Rect(0f, 0f, 0f, 0f);
    private static string settingsPath = KSPUtil.ApplicationRootPath + "GameData/ForScience/PluginData/";
    private static string settingsFile = settingsPath+"settings.cfg";
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

      Directory.CreateDirectory(settingsPath);
      savenode.Save(settingsFile);
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

      float.TryParse(settings.scienceCutoffString, out settings.scienceCutoff);
      float.TryParse(settings.spriteAnimationFPSString, out settings.spriteAnimationFPS);
      ForScience.sprite.SetFramerate(settings.spriteAnimationFPS);
      ForScience.UpdateCurrent();
      ForScience.RunScience();
      settings.saveToDisk();
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
      try { 
        foreach (ConfigNode node in ConfigNode.Load(settingsFile).GetNodes())
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
      catch (NullReferenceException)
      {
      
      }
    }
  }
}
