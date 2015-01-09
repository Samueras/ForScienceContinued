/**
 *		ForScience
 *		Original code from WaveFunctionP(http://forum.kerbalspaceprogram.com/members/107709)
 *		This code is licensed under the Attribution-ShareAlike 4.0 (CC BY-SA 4.0)
 *		creative commons license. See (http://creativecommons.org/licenses/by-sa/4.0/)
 *		for full details.
 *		Modified by	SpaceTiger(http://forum.kerbalspaceprogram.com/members/137260) and
 *		by	SpaceKitty(http://forum.kerbalspaceprogram.com/members/137262)
 *		
 *		Original Thread: http://forum.kerbalspaceprogram.com/threads/76437
 *		Origianl GitHub: https://github.com/WaveFunctionP/ForScience
 *		
 *  Modified Github: https://github.com/Xarun/ForScience
 *		
 * 
 *		to-do: 
 *		  fix bugs that i didnt find yet
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ForScience
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class ForScience : MonoBehaviour
  {

    //GUI
    private GUIStyle windowStyle, labelStyle, toggleStyle, textStyle;
    private bool initStyle = false;

    //states

    //bool initState = false;

    //current

    static Vessel currentVessel = null;
    static CelestialBody currentBody = null;
    static string currentBiome = null;
    static ExperimentSituations currentSituation = 0;
    static List<ModuleScienceContainer> containerList = null;
    static List<ModuleScienceExperiment> experimentList = null;
    static List<KerbalEVA> kerbalEVAParts = null;
    static List<String> startedExperiments = new List<String>();
    static Dictionary<string, double> runningExperiments = new Dictionary<string, double>();
    static Dictionary<string, int> shipCotainsExperiments = new Dictionary<string, int>();
    static ModuleScienceContainer container = null;
    static KerbalEVA kerbalEVAPart = null;
    public static PackedSprite sprite; // animations: Spin, Unlit
    //thread control
    static bool IsDataToCollect = false;
    static bool dataIsInContainer = false;


    private ApplicationLauncherButton button;
    private GUIStyle numberFieldStyle;
    private GUIStyle buttonStyle;
    private static double lastUpdate;
    private static ScienceExperiment experiment;
    private GUIStyle tooltipStyle;
    private int toolbarInt;
    private static List<String> toolbarStrings = new List<String>();
    private float tooltipHeight = 0;
    private string CurrentTooltip;
    private Rect tooltipRect = new Rect(0, 0, 230, 20);
    private float lastWindowHeight;
    private bool windowShrinked;
    private bool setWindowShrinked;
    private GUIStyle containerStyle;
    private static int experimentNumber;
    private static int experimentLimit;
    public static Vessel parentVessel { get; set; }

    private void Awake()
    {
      GameEvents.onGUIApplicationLauncherReady.Add(this.OnGuiAppLauncherReady);
    }

    private void Start()
    {
      GameEvents.onCrewOnEva.Add(GoingEva);
      settings.load();
      RenderingManager.AddToPostDrawQueue(0, OnDraw);
    }
    private void OnDestroy()
    {
      GameEvents.onCrewOnEva.Remove(GoingEva);
      settings.showSettings = false;
      GameEvents.onGUIApplicationLauncherReady.Remove(this.OnGuiAppLauncherReady);
      settings.saveToDisk();
      if (this.button != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(this.button);
      }
    }
    void GoingEva(GameEvents.FromToAction<Part, Part> parts)
    {
      print(parts.to.protoModuleCrew.First().name + " got out of " + parts.from.vessel.vesselName);
      parentVessel = parts.from.vessel;
    }
    public void OnGUI()
    {
      //if (HighLogic.LoadedScene == GameScenes.FLIGHT && settings.showSettings && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) && !initStyle)
      if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) && !initStyle) InitStyle();
    }

    private void OnGuiAppLauncherReady()
    {
      sprite = PackedSprite.Create("ForScience.Button.Sprite", Vector3.zero);
      sprite.SetMaterial(new Material(Shader.Find("Sprite/Vertex Colored")) { mainTexture = GameDatabase.Instance.GetTexture("ForScience/Textures/icon", false) });
      sprite.renderer.sharedMaterial.mainTexture.filterMode = FilterMode.Point;
      sprite.Setup(38f, 38f);
      sprite.SetFramerate(settings.spriteAnimationFPS);
      sprite.SetAnchor(SpriteRoot.ANCHOR_METHOD.UPPER_LEFT);
      sprite.gameObject.layer = LayerMask.NameToLayer("EzGUI_UI");
      // normal state
      UVAnimation normal = new UVAnimation() { name = "Stopped", loopCycles = 0, framerate = settings.spriteAnimationFPS };
      normal.BuildUVAnim(sprite.PixelCoordToUVCoord(0 * 38, 9 * 38), sprite.PixelSpaceToUVSpace(38, 38), 1, 1, 1);

      // animated state
      UVAnimation anim = new UVAnimation() { name = "Spinning", loopCycles = -1, framerate = settings.spriteAnimationFPS };
      anim.BuildWrappedUVAnim(new Vector2(0, sprite.PixelCoordToUVCoord(0, 38).y), sprite.PixelSpaceToUVSpace(38, 38), 56);


      // add animations to button
      sprite.AddAnimation(normal);
      sprite.AddAnimation(anim);

      if (settings.autoScience)
      {
        setAppLauncherAnimation("on");
      }
      else
      {
        setAppLauncherAnimation("off");
      }
      this.button = ApplicationLauncher.Instance.AddModApplication(
        //ButtonState(true), //RUIToggleButton.onTrue
        //ButtonState(false), //RUIToggleButton.onFalse
          toggleAutoScience, 	//RUIToggleButton.onTrue
          toggleAutoScience,	//RUIToggleButton.onFalse
          null, //RUIToggleButton.OnHover
          null, //RUIToggleButton.onHoverOut
          null, //RUIToggleButton.onEnable
          null, //RUIToggleButton.onDisable
          ApplicationLauncher.AppScenes.FLIGHT, //visibleInScenes
          sprite//GameDatabase.Instance.GetTexture("ForScience/icon_off", false) //texture
      );
    }

    private void toggleAutoScience()
    {
      if (Input.GetMouseButtonUp(0))
      {//left mouse button
        if (settings.autoScience)
        {
          settings.autoScience = false;
          setAppLauncherAnimation("off");
        }
        else
        {
          settings.autoScience = true;
          setAppLauncherAnimation("on");
        }
      }
      else if (Input.GetMouseButtonUp(1))//right mouse button
      {
        if (settings.showSettings)
        {
          settings.showSettings = false;
        }
        else
        {
          //only move window when the position was not set in the settings
          if (settings.windowPosition.x == 0 && settings.windowPosition.y == 0)
          {
            settings.windowPosition.x = Input.mousePosition.x;
            settings.windowPosition.y = 38;
          }
          settings.showSettings = true;
        }
      }
      settings.save();
    }
    public static void setAppLauncherAnimation(string type)
    {
      if (type == "on")
      {
        sprite.PlayAnim("Spinning");
        sprite.SetFramerate(settings.spriteAnimationFPS);
      }
      else
      {
        sprite.PlayAnim("Stopped");
        sprite.PauseAnim();
      }
    }

    //private void Update()
    private void FixedUpdate()
    {
      Utilities.LogDebugMessage("Run!" + FlightGlobals.ActiveVessel.name);
      if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) &&
          settings.autoScience &&
          FlightGlobals.ready &&
          Planetarium.GetUniversalTime() > lastUpdate)
      {
        lastUpdate = Planetarium.GetUniversalTime() + 1;

        UpdateCurrent();
        if (IsDataToCollect && settings.transferScience && !currentVessel.isEVA)
          TransferScience();

        RunScience();
      }
    }
    private void TransferScience()
    {
      Utilities.LogDebugMessage("Tranfering science to container.");
      container.StoreData(experimentList.Cast<IScienceDataContainer>().ToList(), false);

      IsDataToCollect = false;
    }

    public static void RunScience()
    {
      if (!settings.autoScience)
        return;
      shipCotainsExperiments.Clear();
      if (currentVessel.isEVA && kerbalEVAPart == null)
      {
        kerbalEVAParts = currentVessel.FindPartModulesImplementing<KerbalEVA>();
        kerbalEVAPart = kerbalEVAParts.First();
      }

      if (currentVessel.isEVA && settings.doEVAonlyIfOnGroundWhenLanded && (parentVessel.Landed || parentVessel.Splashed) && (kerbalEVAPart.OnALadder || (!currentVessel.Landed && !currentVessel.Splashed)))
      {
        return;
      }
      foreach (ModuleScienceExperiment currentExperiment in experimentList)
      {
        var fixBiome = string.Empty;

        experiment = ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID);
        if (experiment.BiomeIsRelevantWhile(currentSituation))
        {
          fixBiome = currentBiome;
        }
        var currentScienceSubject = ResearchAndDevelopment.GetExperimentSubject(experiment, currentSituation, currentBody, fixBiome);
        Utilities.LogDebugMessage("currentScienceSubject.id: " + currentScienceSubject.id);
        Utilities.LogDebugMessage("experiment.id: " + experiment.id);
        Utilities.LogDebugMessage("currentExperiment.experimentID: " + currentExperiment.experimentID);

        float currentScienceValue = 0;
        if (shipCotainsExperiments.ContainsKey(experiment.id))
        {
          currentScienceValue = ResearchAndDevelopment.GetNextScienceValue(experiment.baseValue * experiment.dataScale, currentScienceSubject) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
          if (shipCotainsExperiments[experiment.id] >= 2)//taken from scienceAlert to get somewhat accurate science values after the second experiment
            currentScienceValue = currentScienceValue / Mathf.Pow(4f, shipCotainsExperiments[experiment.id] - 2);
          shipCotainsExperiments[experiment.id]++;
        }
        else
        {
          currentScienceValue = ResearchAndDevelopment.GetScienceValue(experiment.baseValue * experiment.dataScale, currentScienceSubject) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
          shipCotainsExperiments.Add(experiment.id, 1);
        }
        Utilities.LogDebugMessage("Science available is " + currentScienceValue);

        if (((!currentExperiment.rerunnable && settings.runOneTimeScience) || currentExperiment.rerunnable) &&
            experiment.IsAvailableWhile(currentSituation, currentBody) &&
            currentScienceValue >= settings.scienceCutoff &&
            !currentExperiment.Inoperable &&
            !currentExperiment.Deployed &&
            !(runningExperiments.ContainsKey(experiment.id) && runningExperiments[experiment.id] > lastUpdate))
        {
          if (runningExperiments.ContainsKey(experiment.id))
          {
            runningExperiments.Remove(experiment.id);
          }

          dataIsInContainer = false;
          checkDataInContainer(currentScienceSubject, container.GetData());
          Utilities.LogDebugMessage("Checking experiment: " + experiment.id);
          checkDataInContainer(currentScienceSubject, currentExperiment.GetData());
          try
          {
            var conductMethod = currentExperiment.GetType().GetMethod("conduct");//thanks Sephiroth018 for this part
            if (conductMethod != null) { 
              Utilities.LogDebugMessage("Experiment {0} is a DMagic Orbital Science experiment.", experiment.id);

              var conductResult = (bool)conductMethod.Invoke(null, new object[] { currentExperiment });

              if (!conductResult)
              {
                Utilities.LogDebugMessage("Experiment {0} can't be conducted.", experiment.id);
                continue;
              }
            }
            if (!int.TryParse(currentExperiment.GetType().GetField("experimentNumber").GetValue(currentExperiment).ToString(), out  experimentNumber) ||
                !int.TryParse(currentExperiment.GetType().GetField("experimentLimit").GetValue(currentExperiment).ToString(), out  experimentLimit) ||
                ((experimentNumber >= experimentLimit) && experimentLimit >= 1))
            {
              Utilities.LogDebugMessage("Experiment {0} can't be conducted cause the experimentLimit is reached!", experiment.id);
              continue;
            }
          }
          catch (Exception e)
          {
            Debug.LogException(e);
          }

          if (!dataIsInContainer)
          {
            Utilities.LogDebugMessage("Deploying! ");
            Utilities.LogDebugMessage("Science available is " + currentScienceValue);
            Utilities.LogDebugMessage("Running experiment: " + experiment.id);

            runningExperiments.Add(experiment.id, (lastUpdate + 10));
            //currentExperiment.DeployExperiment();
            try//taken from ScienceAlert since .DeployExperiment() didnt work and i didnt know about this 
            {
              // Get the most-derived type and use its DeployExperiment so we don't
              // skip any plugin-derived versions
              currentExperiment.GetType().InvokeMember("DeployExperiment", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreReturn | System.Reflection.BindingFlags.InvokeMethod, null, currentExperiment, null);
            }
            catch (Exception e)
            {
              Debug.LogError("Failed to invoke \"DeployExperiment\" using GetType(), falling back to base type after encountering exception " + e);
              currentExperiment.DeployExperiment();
            }
            IsDataToCollect = true;
          }

        }
      }
    }
    private static void checkDataInContainer(ScienceSubject ScienceSubject, ScienceData[] ScienceData)
    {
      if (dataIsInContainer)
        return;
      foreach (ScienceData data in ScienceData)
      {
        Utilities.LogDebugMessage("data.subjectID: " + data.subjectID);
        if (ScienceSubject.id.Contains(data.subjectID))
        {
          dataIsInContainer = true;
          break;
        }
      }
    }

    static public void UpdateCurrent()
    {
      currentVessel = FlightGlobals.ActiveVessel;
      currentBody = currentVessel.mainBody;
      currentSituation = ScienceUtil.GetExperimentSituation(currentVessel);
      if (currentVessel.landedAt != string.Empty)
      {
        currentBiome = currentVessel.landedAt;
      }
      else
        currentBiome = ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);

      experimentList = currentVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
      if (container == null)
      {
        toolbarStrings.Clear();
        containerList = currentVessel.FindPartModulesImplementing<ModuleScienceContainer>();
        foreach (ModuleScienceContainer current in containerList)
        {
          toolbarStrings.Add(current.part.partInfo.title);
        }
        container = containerList[0];
      }
    }


    private void OnDraw()
    {
      if (HighLogic.LoadedScene == GameScenes.FLIGHT && settings.showSettings && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
      {
        if (settings.windowPosition.height != 0 && settings.windowPosition.height != lastWindowHeight || !windowShrinked)
        {
          settings.windowPosition.height = 0;
          windowShrinked = true;
        }
        settings.windowPosition = GUILayout.Window(104234, settings.windowPosition, MainWindow, "For Science", windowStyle);
        Utilities.clampToScreen(ref settings.windowPosition);
        if (settings.windowPosition.height != 0)
        {
          lastWindowHeight = settings.windowPosition.height;
        }
        if (!String.IsNullOrEmpty(CurrentTooltip))
        {
          tooltipRect.x = Input.mousePosition.x + 10;
          tooltipRect.y = Screen.height - Input.mousePosition.y + 10;
          Utilities.clampToScreen(ref tooltipRect);
          tooltipRect.height = tooltipHeight;
          GUI.Label(tooltipRect, CurrentTooltip, tooltipStyle);
          GUI.depth = 0;
        }
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

      tooltipStyle = new GUIStyle(HighLogic.Skin.label);
      tooltipStyle.fixedWidth = 230f;
      tooltipStyle.padding.top = 5;
      tooltipStyle.padding.left = 5;
      tooltipStyle.padding.right = 5;
      tooltipStyle.padding.bottom = 5;
      tooltipStyle.fontSize = 10;
      //tooltipStyle.normal.background = GameDatabase.Instance.GetTexture("ForScience/img_BarBlue", false);//windowStyle.normal.background;
      Texture2D texBarBlue = new Texture2D(13, 13, TextureFormat.ARGB32, false);
      LoadImageFromFile(ref texBarBlue, "tooltipBG.png", KSPUtil.ApplicationRootPath + "GameData/ForScience/Textures");
      tooltipStyle.normal.background = texBarBlue;
      tooltipStyle.normal.textColor = Color.white;
      tooltipStyle.border.top = 1;
      tooltipStyle.border.bottom = 1;
      tooltipStyle.border.left = 8;
      tooltipStyle.border.right = 8;
      tooltipStyle.stretchHeight = true;


      initStyle = true;
    }

    private void MainWindow(int windowID)
    {
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      GUILayout.Label(new GUIContent("Animation FPS:", "set to 0 to disable"), textStyle);
      settings.spriteAnimationFPSString = GUILayout.TextField(settings.spriteAnimationFPSString, 5, numberFieldStyle);
      GUILayout.EndHorizontal();
      GUILayout.BeginHorizontal();
      GUILayout.Label(new GUIContent("Science cutoff:", "Doesn't run any science experiment if it's value less than this number."), textStyle);
      settings.scienceCutoffString = GUILayout.TextField(settings.scienceCutoffString, 5, numberFieldStyle);
      GUILayout.EndHorizontal();
      if (GUILayout.Toggle(settings.doEVAonlyIfOnGroundWhenLanded, new GUIContent("Restrict EVA-Report", "If this option is turned on and the vessel is landed/splashed the kerbal wont do the EVA-Report if he isnt on the ground."), toggleStyle))
      {
        settings.doEVAonlyIfOnGroundWhenLanded = true;
      }
      else
      {
        settings.doEVAonlyIfOnGroundWhenLanded = false;
      }
      if (GUILayout.Toggle(settings.runOneTimeScience, new GUIContent("Run one-time only science", "To run experiments like goo container"), toggleStyle))
      {
        settings.runOneTimeScience = true;
      }
      else
      {
        settings.runOneTimeScience = false;
      }
      if (GUILayout.Toggle(settings.transferScience, new GUIContent("Transfer science to container", "Transfers all the science from experiments to the selected container.\nWARNING: makes experiments unoperable if used with \"Run one-time only science\""), toggleStyle))
      {
        settings.transferScience = true;
        GUILayout.BeginVertical();
        toolbarInt = GUILayout.SelectionGrid(toolbarInt, toolbarStrings.ToArray(), 1, containerStyle);
        container = containerList[toolbarInt];
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
        settings.transferScience = false;
      }
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Save", buttonStyle))
      {
        settings.save();
        settings.showSettings = false;
      }
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      tooltipHeight = tooltipStyle.CalcHeight(new GUIContent(GUI.tooltip), tooltipStyle.fixedWidth);
      CurrentTooltip = GUI.tooltip;
      GUILayout.EndVertical();
      GUI.DragWindow();
    }
    /**
     * LoadImageFromFile 
     * Author: TriggerAu 
     * The MIT License (MIT)
     *
     * Copyright (c) 2014, David Tregoning
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
     * copies of the Software, and to permit persons to whom the Software is
     * furnished to do so, subject to the following conditions:
     *
     * The above copyright notice and this permission notice shall be included in
     * all copies or substantial portions of the Software.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
     * THE SOFTWARE.
     *
     */
    public static Boolean LoadImageFromFile(ref Texture2D tex, String FileName, String FolderPath = "")
    {
      //DebugLogFormatted("{0},{1}",FileName, FolderPath);
      Boolean blnReturn = false;
      try
      {
        //if (FolderPath == "") FolderPath = PathPluginTextures;
        //File Exists check
        if (System.IO.File.Exists(String.Format("{0}/{1}", FolderPath, FileName)))
        {
          try
          {
            //MonoBehaviourExtended.LogFormatted_DebugOnly("Loading: {0}", String.Format("{0}/{1}", FolderPath, FileName));
            tex.LoadImage(System.IO.File.ReadAllBytes(String.Format("{0}/{1}", FolderPath, FileName)));
            blnReturn = true;
          }
          catch (Exception ex)
          {
            Debug.LogException(ex);
            //MonoBehaviourExtended.LogFormatted("Failed to load the texture:{0} ({1})", String.Format("{0}/{1}", FolderPath, FileName), ex.Message);
          }
        }
        //else
        //{
        //MonoBehaviourExtended.LogFormatted("Cannot find texture to load:{0}", String.Format("{0}/{1}", FolderPath, FileName));
        //}
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
        //MonoBehaviourExtended.LogFormatted("Failed to load (are you missing a file):{0} ({1})", String.Format("{0}/{1}", FolderPath, FileName), ex.Message);
      }
      return blnReturn;
    }
  }
}