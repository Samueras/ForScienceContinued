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
using UnityEngine;

namespace KerboKatz
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  internal class ForScienceContinued : MonoBehaviour
  {
    private ApplicationLauncherButton button;
    private bool dataIsInContainer                         = false;
    private bool doEVAonlyIfOnGroundWhenLanded;
    private bool initStyle                                 = false;
    private bool IsDataToCollect                           = false;
    private bool runOneTimeScience;
    private bool setWindowShrinked;
    private bool transferScience;
    private bool windowShrinked;
    private CelestialBody currentBody                      = null;
    private Dictionary<string, double> runningExperiments  = new Dictionary<string, double>();
    private Dictionary<string, int> shipCotainsExperiments = new Dictionary<string, int>();
    private double lastUpdate                              = 0;
    private ExperimentSituations currentSituation          = 0;
    private float lastWindowHeight;
    private float tooltipHeight                            = 0;
    private GUIStyle buttonStyle;
    private GUIStyle containerStyle;
    private GUIStyle numberFieldStyle;
    private GUIStyle tooltipStyle;
    private GUIStyle windowStyle, labelStyle, toggleStyle, textStyle;
    private int experimentLimit;
    private int experimentNumber;
    private int toolbarInt;
    private KerbalEVA kerbalEVAPart                        = null;
    private List<KerbalEVA> kerbalEVAParts                 = null;
    private List<ModuleScienceContainer> containerList     = null;
    private List<ModuleScienceExperiment> experimentList   = null;
    private List<String> startedExperiments                = new List<String>();
    private List<String> toolbarStrings                    = new List<String>();
    private ModuleScienceContainer container               = null;
    private PackedSprite sprite; // animations: Spin, Unlit
    private Rect tooltipRect                               = new Rect(0, 0, 230, 20);
    private Rect windowPosition                            = new Rect(0f, 0f, 0f, 0f);
    private ScienceExperiment experiment;
    private settings currentSettings;
    private string currentBiome                            = null;
    private string CurrentTooltip;
    private string modName                                 = "ForScienceContinued";
    private string scienceCutoff;
    private string spriteAnimationFPS;
    private Vessel currentVessel                           = null;
    private Vessel parentVessel;

    private void Awake()
    {
      if (!Utilities.checkUtilitiesSupport(new Version(1, 0, 0), modName))
      {
        Destroy(this);
        return;
      }
      GameEvents.onGUIApplicationLauncherReady.Add(OnGuiAppLauncherReady);
    }

    private void Start()
    {
      currentSettings = new settings();
      currentSettings.load("ForScienceContinued", "settings", "ForScienceContinued");
      currentSettings.setDefault("scienceCutoff", "2");
      currentSettings.setDefault("spriteAnimationFPS", "25");
      currentSettings.setDefault("autoScience", "false");
      currentSettings.setDefault("runOneTimeScience", "false");
      currentSettings.setDefault("transferScience", "false");
      currentSettings.setDefault("showSettings", "false");
      currentSettings.setDefault("doEVAonlyIfOnGroundWhenLanded", "true");
      currentSettings.setDefault("windowX", "99999");
      currentSettings.setDefault("windowY", "38");
      windowPosition.x = currentSettings.getFloat("windowX");
      windowPosition.y = currentSettings.getFloat("windowY");

      scienceCutoff = currentSettings.getString("scienceCutoff");
      spriteAnimationFPS = currentSettings.getString("spriteAnimationFPS");
      transferScience = currentSettings.getBool("transferScience");
      doEVAonlyIfOnGroundWhenLanded = currentSettings.getBool("doEVAonlyIfOnGroundWhenLanded");
      runOneTimeScience = currentSettings.getBool("runOneTimeScience");

      GameEvents.onCrewOnEva.Add(GoingEva);
      RenderingManager.AddToPostDrawQueue(0, OnDraw);
    }

    private void OnDestroy()
    {
      GameEvents.onCrewOnEva.Remove(GoingEva);
      if (currentSettings != null)
      {
        currentSettings.set("showSettings", false);
        currentSettings.save();
      }
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
      if (button != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(button);
      }
    }

    private void GoingEva(GameEvents.FromToAction<Part, Part> parts)
    {
      parentVessel = parts.from.vessel;
    }

    private void OnGUI()
    {
      if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) && !initStyle)
        InitStyle();
    }

    private void OnGuiAppLauncherReady()
    {
      sprite = PackedSprite.Create("ForScience.Button.Sprite", Vector3.zero);
      sprite.SetMaterial(new Material(Shader.Find("Sprite/Vertex Colored")) { mainTexture = Utilities.getTexture("icon", "ForScienceContinued/Textures")});
      sprite.renderer.sharedMaterial.mainTexture.filterMode = FilterMode.Point;
      sprite.Setup(38f, 38f);
      sprite.SetFramerate(currentSettings.getFloat("spriteAnimationFPS"));
      sprite.SetAnchor(SpriteRoot.ANCHOR_METHOD.UPPER_LEFT);
      sprite.gameObject.layer = LayerMask.NameToLayer("EzGUI_UI");
      // normal state
      UVAnimation normal = new UVAnimation() { name = "Stopped", loopCycles = 0, framerate = currentSettings.getFloat("spriteAnimationFPS") };
      normal.BuildUVAnim(sprite.PixelCoordToUVCoord(0 * 38, 9 * 38), sprite.PixelSpaceToUVSpace(38, 38), 1, 1, 1);

      // animated state
      UVAnimation anim = new UVAnimation() { name = "Spinning", loopCycles = -1, framerate = currentSettings.getFloat("spriteAnimationFPS") };
      anim.BuildWrappedUVAnim(new Vector2(0, sprite.PixelCoordToUVCoord(0, 38).y), sprite.PixelSpaceToUVSpace(38, 38), 56);

      // add animations to button
      sprite.AddAnimation(normal);
      sprite.AddAnimation(anim);

      if (currentSettings.getBool("autoScience"))
      {
        setAppLauncherAnimation("on");
      }
      else
      {
        setAppLauncherAnimation("off");
      }
      button = ApplicationLauncher.Instance.AddModApplication(
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
        if (currentSettings.getBool("autoScience"))
        {
          currentSettings.set("autoScience", false);
          setAppLauncherAnimation("off");
        }
        else
        {
          currentSettings.set("autoScience", true);
          setAppLauncherAnimation("on");
        }
      }
      else if (Input.GetMouseButtonUp(1))//right mouse button
      {
        if (currentSettings.getBool("showSettings"))
        {
          currentSettings.set("showSettings", false);
        }
        else
        {
          //only move window when the position was not set in the settings
          if (windowPosition.x == 0 && windowPosition.y == 0)
          {
            windowPosition.x = Input.mousePosition.x;
            windowPosition.y = 38;
          }
          currentSettings.set("showSettings", true);
        }
      }
      currentSettings.save();
    }

    private void setAppLauncherAnimation(string type)
    {
      if (type == "on")
      {
        sprite.PlayAnim("Spinning");
        sprite.SetFramerate(currentSettings.getFloat("spriteAnimationFPS"));
      }
      else
      {
        sprite.PlayAnim("Stopped");
        sprite.PauseAnim();
      }
    }

    // void Update()
    private void FixedUpdate()
    {
      if (lastUpdate == 0)
      {//add some delay so it doesnt run as soon as the vehicle launches
        lastUpdate = Planetarium.GetUniversalTime() + 5;
        UpdateCurrent();
      }
      if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) &&
          FlightGlobals.ready &&
          currentSettings.getBool("autoScience") &&
          Planetarium.GetUniversalTime() > lastUpdate)
      {
        lastUpdate = Planetarium.GetUniversalTime() + 1;
        UpdateCurrent();
        if (Utilities.canVesselBeControlled(currentVessel))
        {
          Utilities.debug(modName, "Run!" + currentVessel.name);
          if (IsDataToCollect && currentSettings.getBool("transferScience") && !currentVessel.isEVA)
            TransferScience();

          RunScience();
        }
      }
    }

    private void TransferScience()
    {
      if (container == null)
        return;
      Utilities.debug(modName, "Tranfering science to container.");
      container.StoreData(experimentList.Cast<IScienceDataContainer>().ToList(), false);

      IsDataToCollect = false;
    }

    private void RunScience()
    {
      if (!currentSettings.getBool("autoScience"))
        return;
      if (currentVessel.isEVA && kerbalEVAPart == null)
      {
        kerbalEVAParts = currentVessel.FindPartModulesImplementing<KerbalEVA>();
        kerbalEVAPart = kerbalEVAParts.First();
      }

      if (currentVessel.isEVA && currentSettings.getBool("doEVAonlyIfOnGroundWhenLanded") && (parentVessel.Landed || parentVessel.Splashed) && (kerbalEVAPart.OnALadder || (!currentVessel.Landed && !currentVessel.Splashed)))
      {
        return;
      }
      foreach (ModuleScienceExperiment currentExperiment in experimentList)
      {
        addToExpermientedList(currentExperiment.GetData());
        var fixBiome = string.Empty;

        experiment = ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID);

        if (experiment.BiomeIsRelevantWhile(currentSituation))
        {
          fixBiome = currentBiome;
        }
        var currentScienceSubject = ResearchAndDevelopment.GetExperimentSubject(experiment, currentSituation, currentBody, fixBiome);
        float currentScienceValue = getScienceValue(experiment, currentScienceSubject);
        Utilities.debug(modName, "-------------------------------------\ncurrentScienceSubject.id: " + currentScienceSubject.id +
                                                          "\nexperiment.id: " + experiment.id +
                                                          "\ncurrentExperiment.experimentID: " + currentExperiment.experimentID +
                                                          "\nScience available is " + currentScienceValue);

        if (((!currentExperiment.rerunnable && currentSettings.getBool("runOneTimeScience")) || currentExperiment.rerunnable) &&
            experiment.IsAvailableWhile(currentSituation, currentBody) &&
            currentScienceValue >= currentSettings.getFloat("scienceCutoff") &&
            !currentExperiment.Inoperable &&
            !currentExperiment.Deployed &&
            experiment.IsUnlocked() &&
            !(runningExperiments.ContainsKey(experiment.id) && runningExperiments[experiment.id] > lastUpdate))
        {
          if (runningExperiments.ContainsKey(experiment.id))
          {
            runningExperiments.Remove(experiment.id);
          }

          dataIsInContainer = false;
          checkDataInContainer(currentScienceSubject, container.GetData());
          checkDataInContainer(currentScienceSubject, currentExperiment.GetData());

          #region try-catch for DMagic Orbital Science
          try
          {
            var conductMethod = currentExperiment.GetType().GetMethod("conduct");//thanks Sephiroth018 for this conduct part
            if (conductMethod != null)
            {
              Utilities.debug(modName, "Experiment {0} is a DMagic Orbital Science experiment.", experiment.id);

              var conductResult = (bool)conductMethod.Invoke(null, new object[] { currentExperiment });

              if (!conductResult)
              {
                Utilities.debug(modName, "Experiment {0} can't be conducted.", experiment.id);
                continue;
              }
            }
            if (int.TryParse(currentExperiment.GetType().GetField("experimentNumber").GetValue(currentExperiment).ToString(), out  experimentNumber) &&
                int.TryParse(currentExperiment.GetType().GetField("experimentLimit").GetValue(currentExperiment).ToString(), out  experimentLimit))
            {
              if ((experimentNumber >= experimentLimit) && experimentLimit >= 1)
              {
                Utilities.debug(modName, "Experiment {0} can't be conducted cause the experimentLimit is reached!", experiment.id);
                continue;
              }
              else if (experimentNumber > 0)
              {
                if (shipCotainsExperiments.ContainsKey(currentScienceSubject.id))
                  shipCotainsExperiments[currentScienceSubject.id] += experimentNumber;
                else
                  shipCotainsExperiments.Add(currentScienceSubject.id, experimentNumber + 1);
                currentScienceValue = getScienceValue(experiment, currentScienceSubject);
                Utilities.debug(modName, "Experiment is a DMagic Orbital Science experiment. Science value changed to: " + currentScienceValue);
              }
            }
          }
          catch (Exception)
          {
          }
          #endregion try-catch for DMagic Orbital Science

          if (!dataIsInContainer)
          {
            Utilities.debug(modName, "Deploying! " +
                                                              "\nScience available is " + currentScienceValue +
                                                              "\nRunning experiment: " + experiment.id);

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
            if (shipCotainsExperiments.ContainsKey(currentScienceSubject.id))
              shipCotainsExperiments[currentScienceSubject.id]++;
            else
              shipCotainsExperiments.Add(currentScienceSubject.id, 1);
            IsDataToCollect = true;
          }
        }
      }
    }

    private float getScienceValue(ScienceExperiment experiment, ScienceSubject currentScienceSubject)
    {
      float currentScienceValue;
      if (shipCotainsExperiments.ContainsKey(currentScienceSubject.id))
      {
        currentScienceValue = ResearchAndDevelopment.GetNextScienceValue(experiment.baseValue * experiment.dataScale, currentScienceSubject) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
        if (shipCotainsExperiments[currentScienceSubject.id] >= 2)//taken from scienceAlert to get somewhat accurate science values after the second experiment
          currentScienceValue = currentScienceValue / Mathf.Pow(4f, shipCotainsExperiments[currentScienceSubject.id] - 2);
      }
      else
      {
        currentScienceValue = ResearchAndDevelopment.GetScienceValue(experiment.baseValue * experiment.dataScale, currentScienceSubject) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
      }
      return currentScienceValue;
    }

    private void checkDataInContainer(ScienceSubject ScienceSubject, ScienceData[] ScienceData)
    {
      if (dataIsInContainer)
        return;
      foreach (ScienceData data in ScienceData)
      {
        Utilities.debug(modName, "data.subjectID: " + data.subjectID);
        if (ScienceSubject.id.Contains(data.subjectID))
        {
          dataIsInContainer = true;
          break;
        }
      }
    }

    private void UpdateCurrent()
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
      if (shipCotainsExperiments != null)
        shipCotainsExperiments.Clear();
      if (toolbarStrings != null)
        toolbarStrings.Clear();
      if (containerList != null)
        containerList.Clear();
      experimentList = currentVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
      containerList = currentVessel.FindPartModulesImplementing<ModuleScienceContainer>();
      foreach (ModuleScienceContainer currentContainer in containerList)
      {
        addToExpermientedList(currentContainer.GetData());
        toolbarStrings.Add(currentContainer.part.partInfo.title);
      }
      if (container == null && containerList != null)
        container = containerList[0];
    }

    private void addToExpermientedList(ScienceData[] data)
    {
      foreach (ScienceData currentData in data)
      {
        if (shipCotainsExperiments.ContainsKey(currentData.subjectID))
        {
          shipCotainsExperiments[currentData.subjectID] = shipCotainsExperiments[currentData.subjectID] + 1;
        }
        else
        {
          shipCotainsExperiments.Add(currentData.subjectID, 1);
        }
      }
    }

    private void OnDraw()
    {
      if (HighLogic.LoadedScene == GameScenes.FLIGHT && currentSettings.getBool("showSettings") && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
      {
        if (windowPosition.height != 0 && windowPosition.height != lastWindowHeight || !windowShrinked)
        {
          windowPosition.height = 0;
          windowShrinked        = true;
        }
        windowPosition          = GUILayout.Window(104234, windowPosition, MainWindow, "For Science", windowStyle);
        Utilities.clampToScreen(ref windowPosition);
        if (windowPosition.height != 0)
        {
          lastWindowHeight      = windowPosition.height;
        }
        if (!String.IsNullOrEmpty(CurrentTooltip))
        {
          tooltipRect.x         = Input.mousePosition.x + 10;
          tooltipRect.y         = Screen.height - Input.mousePosition.y + 10;
          Utilities.clampToScreen(ref tooltipRect);
          tooltipRect.height    = tooltipHeight;
          GUI.Label(tooltipRect, CurrentTooltip, tooltipStyle);
          GUI.depth             = 0;
        }
      }
    }

    private void InitStyle()
    {
      labelStyle                       = new GUIStyle(HighLogic.Skin.label);
      labelStyle.stretchWidth          = true;

      windowStyle                      = new GUIStyle(HighLogic.Skin.window);
      windowStyle.fixedWidth           = 250f;
      windowStyle.padding.left         = 0;

      toggleStyle                      = new GUIStyle(HighLogic.Skin.toggle);
      toggleStyle.normal.textColor     = labelStyle.normal.textColor;
      toggleStyle.active.textColor     = labelStyle.normal.textColor;

      textStyle                        = new GUIStyle(HighLogic.Skin.label);
      textStyle.fixedWidth             = 100f;
      textStyle.margin.left            = 10;

      containerStyle                   = new GUIStyle(GUI.skin.button);
      containerStyle.fixedWidth        = 230f;
      containerStyle.margin.left       = 10;

      numberFieldStyle                 = new GUIStyle(HighLogic.Skin.box);
      numberFieldStyle.fixedWidth      = 52f;
      numberFieldStyle.fixedHeight     = 22f;
      numberFieldStyle.alignment       = TextAnchor.MiddleCenter;
      numberFieldStyle.margin.left     = 95;
      numberFieldStyle.padding.right   = 7;

      buttonStyle                      = new GUIStyle(GUI.skin.button);
      buttonStyle.fixedWidth           = 127f;

      tooltipStyle                     = new GUIStyle(HighLogic.Skin.label);
      tooltipStyle.fixedWidth          = 230f;
      tooltipStyle.padding.top         = 5;
      tooltipStyle.padding.left        = 5;
      tooltipStyle.padding.right       = 5;
      tooltipStyle.padding.bottom      = 5;
      tooltipStyle.fontSize            = 10;
      tooltipStyle.normal.background   = Utilities.getTexture("tooltipBG", "Textures"); ;
      tooltipStyle.normal.textColor    = Color.white;
      tooltipStyle.border.top          = 1;
      tooltipStyle.border.bottom       = 1;
      tooltipStyle.border.left         = 8;
      tooltipStyle.border.right        = 8;
      tooltipStyle.stretchHeight       = true;

      initStyle                        = true;
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
      tooltipHeight = tooltipStyle.CalcHeight(new GUIContent(GUI.tooltip), tooltipStyle.fixedWidth);
      CurrentTooltip = GUI.tooltip;
      GUILayout.EndVertical();
      GUI.DragWindow();
    }
  }
}