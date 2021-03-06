﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerboKatz
{
  partial class ForScienceContinued : KerboKatzBase
  {
    private GUIStyle buttonStyle;
    private GUIStyle containerStyle;
    private GUIStyle numberFieldStyle;
    private GUIStyle windowStyle, labelStyle, toggleStyle, textStyle;
    private Rect windowPosition = new Rect(0f, 0f, 0f, 0f);
    private float lastWindowHeight;
    private bool doEVAonlyIfOnGroundWhenLanded;
    private bool initStyle = false;
    private string scienceCutoff;
    private string spriteAnimationFPS;
    private bool windowShrinked;
    private bool runOneTimeScience;
    private bool setWindowShrinked;
    private bool transferScience;
    private int toolbarInt;

    private void OnGUI()
    {
      if (!initStyle)
        InitStyle();
      if (HighLogic.LoadedScene == GameScenes.FLIGHT && currentSettings.getBool("showSettings"))
      {
        if (windowPosition.height != 0 && windowPosition.height != lastWindowHeight || !windowShrinked)
        {
          windowPosition.height = 0;
          windowShrinked = true;
        }
        windowPosition = GUILayout.Window(104234, windowPosition, MainWindow, "For Science", windowStyle);
        Utilities.clampToScreen(ref windowPosition);
        if (windowPosition.height != 0)
        {
          lastWindowHeight = windowPosition.height;
        }
        Utilities.showTooltip();
      }
    }

    private void InitStyle()
    {
      labelStyle = new GUIStyle(HighLogic.Skin.label);
      labelStyle.stretchWidth = true;

      windowStyle = new GUIStyle(HighLogic.Skin.window);
      windowStyle.fixedWidth = 250f;
      windowStyle.padding.left = 0;

      toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
      toggleStyle.normal.textColor = labelStyle.normal.textColor;
      toggleStyle.active.textColor = labelStyle.normal.textColor;

      textStyle = new GUIStyle(HighLogic.Skin.label);
      textStyle.fixedWidth = 100f;
      textStyle.margin.left = 10;

      containerStyle = new GUIStyle(GUI.skin.button);
      containerStyle.fixedWidth = 230f;
      containerStyle.margin.left = 10;

      numberFieldStyle = new GUIStyle(HighLogic.Skin.box);
      numberFieldStyle.fixedWidth = 52f;
      numberFieldStyle.fixedHeight = 22f;
      numberFieldStyle.alignment = TextAnchor.MiddleCenter;
      numberFieldStyle.margin.left = 95;
      numberFieldStyle.padding.right = 7;

      buttonStyle = new GUIStyle(GUI.skin.button);
      buttonStyle.fixedWidth = 127f;

      initStyle = true;
    }

    private void MainWindow(int windowID)
    {
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      Utilities.createLabel("Animation FPS:", textStyle, "set to 0 to disable");
      spriteAnimationFPS = Utilities.getOnlyNumbers(GUILayout.TextField(spriteAnimationFPS, 5, numberFieldStyle));
      GUILayout.EndHorizontal();
      GUILayout.BeginHorizontal();
      Utilities.createLabel("Science cutoff:", textStyle, "Doesn't run any science experiment if it's value less than this number.");
      scienceCutoff = Utilities.getOnlyNumbers(GUILayout.TextField(scienceCutoff, 5, numberFieldStyle));
      GUILayout.EndHorizontal();
      if (GUILayout.Toggle(doEVAonlyIfOnGroundWhenLanded, new GUIContent("Restrict EVA-Report", "If this option is turned on and the vessel is landed/splashed the kerbal wont do the EVA-Report if he isnt on the ground."), toggleStyle))
      {
        doEVAonlyIfOnGroundWhenLanded = true;
      }
      else
      {
        doEVAonlyIfOnGroundWhenLanded = false;
      }
      if (GUILayout.Toggle(runOneTimeScience, new GUIContent("Run one-time only science", "To run experiments like goo container"), toggleStyle))
      {
        runOneTimeScience = true;
      }
      else
      {
        runOneTimeScience = false;
      }
      if (GUILayout.Toggle(transferScience, new GUIContent("Transfer science to container", "Transfers all the science from experiments to the selected container.\nWARNING: makes experiments unoperable if used with \"Run one-time only science\""), toggleStyle))
      {
        transferScience = true;
        GUILayout.BeginVertical();
        if (toolbarStrings != null)
          toolbarInt = GUILayout.SelectionGrid(toolbarInt, toolbarStrings.ToArray(), 1, containerStyle);
        GUILayout.EndVertical();
        setWindowShrinked = false;
      }
      else
      {
        if (!setWindowShrinked)
        {
          windowShrinked = false;
          setWindowShrinked = true;
        }
        transferScience = false;
      }
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Save", buttonStyle))
      {
        currentSettings.set("scienceCutoff", scienceCutoff);
        currentSettings.set("spriteAnimationFPS", spriteAnimationFPS);
        currentSettings.set("transferScience", transferScience);
        currentSettings.set("doEVAonlyIfOnGroundWhenLanded", doEVAonlyIfOnGroundWhenLanded);
        currentSettings.set("runOneTimeScience", runOneTimeScience);
        currentSettings.save();
        currentSettings.set("showSettings", false);
        if (containerList != null)
          container = containerList[toolbarInt];
        sprite.SetFramerate(currentSettings.getFloat("spriteAnimationFPS"));
      }
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      GUILayout.EndVertical();
      Utilities.updateTooltipAndDrag();
    }
  }
}
