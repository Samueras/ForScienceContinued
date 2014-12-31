using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/**
 *		ForScience
 *		Original code from WaveFunctionP(http://forum.kerbalspaceprogram.com/members/107709)
 *		This code is licensed under the Attribution-ShareAlike 4.0 (CC BY-SA 4.0)
 *		creative commons license. See (http://creativecommons.org/licenses/by-sa/4.0/)
 *		for full details.
 *		Modified by	SpaceTiger(http://forum.kerbalspaceprogram.com/members/137260)
 *		Original Thread: http://forum.kerbalspaceprogram.com/threads/76437
 *		
 * 
 *		to-do: 
 *						currently nothing
 */
namespace ForScience
{
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class ForScience : MonoBehaviour
  {

    //GUI
    private GUIStyle windowStyle, labelStyle, toggleStyle, textStyle;
    private bool initStyle = false;

    //states

    bool initState = false;
    static Vessel stateVessel = null;
    static CelestialBody stateBody = null;
    static string stateBiome = null;
    static ExperimentSituations stateSituation = 0;

    //current

    static Vessel currentVessel = null;
    static CelestialBody currentBody = null;
    static string currentBiome = null;
    static ExperimentSituations currentSituation = 0;
    List<ModuleScienceContainer> containerList = null;
    static List<ModuleScienceExperiment> experimentList = null;
    static List<String> startedExperiments = new List<String>();
    static Dictionary<string, long> runningExperiments = new Dictionary<string, long>();
    static ModuleScienceContainer container = null;
    public static PackedSprite sprite; // animations: Spin, Unlit
    //thread control
    bool runOnce = false;
    static bool IsDataToCollect = false;
    static bool dataIsInContainer = false;


    private ApplicationLauncherButton button;
    private GUIStyle numberFieldStyle;
    private GUIStyle buttonStyle;
    private static long timestamp;

    private void Awake()
    {
      GameEvents.onGUIApplicationLauncherReady.Add(this.OnGuiAppLauncherReady);
    }

    private void OnDestroy()
    {
      settings.showSettings = false;
      GameEvents.onGUIApplicationLauncherReady.Remove(this.OnGuiAppLauncherReady);
      settings.saveToDisk();
      if (this.button != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(this.button);
      }
    }

    private void OnGuiAppLauncherReady()
    {
      sprite = PackedSprite.Create("ForScience.Button.Sprite", Vector3.zero);
      sprite.SetMaterial(new Material(Shader.Find("Sprite/Vertex Colored")) { mainTexture = GameDatabase.Instance.GetTexture("ForScience/icon_on", false) });
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

      /*sprite.PlayAnim("Stopped");

      sprite.PauseAnim();*/
      if (settings.autoScience)
      {
        ForScience.setAppLauncherAnimation("on");
      }
      else
      {
        ForScience.setAppLauncherAnimation("off");
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
      if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER | HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
      {
        if (initState)
        {
          UpdateCurrent();
          if (!currentVessel.isEVA & settings.autoScience)
          {

            if (IsDataToCollect) TransferScience();
            else if (runOnce | !IsDataToCollect & (currentVessel != stateVessel | currentSituation != stateSituation | currentBody != stateBody | currentBiome != stateBiome))
            {
              //Debug.Log("[For Science] Vessel in new experimental situation.");
              RunScience();
              UpdateStates();
              runOnce = false;
            }
            else FindDataToTransfer();

          }

        }
        else
        {
          UpdateCurrent();
          UpdateStates();
          TransferScience();
          initState = true;
        }
      }
    }


    private void FindDataToTransfer()
    {
      foreach (ModuleScienceExperiment currentExperiementCollectData in experimentList)
      {
        if (currentExperiementCollectData.GetData().Count() == 1) IsDataToCollect = true;
      }
    }

    private void TransferScience()
    {

      containerList = currentVessel.FindPartModulesImplementing<ModuleScienceContainer>();
      experimentList = currentVessel.FindPartModulesImplementing<ModuleScienceExperiment>();

      if (container == null) container = containerList[0];

      //Debug.Log("[For Science] Tranfering science to container.");
      if (settings.transferScience)
        container.StoreData(experimentList.Cast<IScienceDataContainer>().ToList(), true);

      IsDataToCollect = false;
    }
    public static long UnixTimeStamp()
    {
      var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
      return (long)timeSpan.TotalSeconds;
    }
    public static void RunScience()
    {
      if (!settings.autoScience)
        return;
      timestamp = UnixTimeStamp();
      foreach (ModuleScienceExperiment currentExperiment in experimentList)
      {
        var fixBiome = string.Empty;

        if (currentExperiment.experiment.BiomeIsRelevantWhile(currentSituation)) fixBiome = currentBiome;

        var currentScienceSubject = ResearchAndDevelopment.GetExperimentSubject(currentExperiment.experiment, currentSituation, currentBody, fixBiome);
        //Debug.Log("[For Science] Checking experiment: " + currentScienceSubject.id);

        var currentScienceValue = ResearchAndDevelopment.GetScienceValue(currentExperiment.experiment.baseValue * currentExperiment.experiment.dataScale * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier, currentScienceSubject);

        // //Debug.Log("[For Science] currentScienceSubject.id:" + currentScienceSubject.id);
        if (((!currentExperiment.rerunnable && settings.runOneTimeScience) ||
            currentExperiment.rerunnable) &&
            !startedExperiments.Contains(currentScienceSubject.id) &&
            currentExperiment.experiment.IsAvailableWhile(currentSituation, currentBody) &&
            currentScienceValue > settings.scienceCutoff &&
            !currentExperiment.Inoperable &&
            !currentExperiment.Deployed &&
            !(runningExperiments.ContainsKey(currentExperiment.experiment.id) && runningExperiments[currentExperiment.experiment.id] > (timestamp)))
        {
          if (runningExperiments.ContainsKey(currentExperiment.experiment.id))
          {
            runningExperiments.Remove(currentExperiment.experiment.id);
          }
          foreach (ScienceData data in container.GetData())
          {
            if (currentScienceSubject.id.Contains(data.subjectID))
            {
              //Debug.Log("[For Science] Skipping: Found existing experiment data: " + data.subjectID);
              dataIsInContainer = true;
              break;
            }

            else
            {
              dataIsInContainer = false;
              UpdateCurrent();
            }
          }

          if (!dataIsInContainer)
          {
            //Debug.Log("[For Science] Science available is " + currentScienceValue);
            //Debug.Log("[For Science] Running experiment: " + currentExperiment.experiment.id);
            startedExperiments.Add(currentScienceSubject.id);
            runningExperiments.Add(currentExperiment.experiment.id, (timestamp + 10));
            currentExperiment.DeployExperiment();
            IsDataToCollect = true;
          }

        }
      }
    }

    static private void UpdateCurrent()
    {
      currentVessel = FlightGlobals.ActiveVessel;
      currentBody = currentVessel.mainBody;
      currentSituation = ScienceUtil.GetExperimentSituation(currentVessel);
      if (currentVessel.landedAt != string.Empty)
      {
        currentBiome = currentVessel.landedAt;
      }
      else currentBiome = ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);
    }

    private void UpdateStates()
    {
      stateVessel = currentVessel;
      stateBody = currentBody;
      stateSituation = currentSituation;
      stateBiome = currentBiome;
    }

    public static void resetStates()
    {
      stateVessel = null;
      stateBody = null;
      stateSituation = 0;
      stateBiome = null;
    }

    private void Start()
    {
      settings.load();
      RenderingManager.AddToPostDrawQueue(0, OnDraw);
    }
    public void OnGUI()
    {
      if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER | HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) & !initStyle) InitStyle();
    }
    private void OnDraw()
    {
      if (!settings.showSettings)
        return;
      if (HighLogic.LoadedScene == GameScenes.FLIGHT & (HighLogic.CurrentGame.Mode == Game.Modes.CAREER | HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
      {
        settings.windowPosition = GUILayout.Window(104234, settings.windowPosition, MainWindow, "For Science", windowStyle);
        settings.windowPosition.x = Mathf.Clamp(settings.windowPosition.x, 0, Screen.width - settings.windowPosition.width);
        settings.windowPosition.y = Mathf.Clamp(settings.windowPosition.y, 0, Screen.height - settings.windowPosition.height);
      }
    }
    private void InitStyle()
    {
      labelStyle = new GUIStyle(HighLogic.Skin.label);
      labelStyle.stretchWidth = true;

      windowStyle = new GUIStyle(HighLogic.Skin.window);
      windowStyle.fixedWidth = 250f;

      toggleStyle = new GUIStyle(HighLogic.Skin.toggle);

      textStyle = new GUIStyle(HighLogic.Skin.label);
      textStyle.fixedWidth = 100f;

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
      GUILayout.Label("Animation FPS:", textStyle);
      settings.spriteAnimationFPSString = GUILayout.TextField(settings.spriteAnimationFPSString, 5, numberFieldStyle);
      GUILayout.EndHorizontal();
      GUILayout.BeginHorizontal();
      GUILayout.Label("Science cutoff:", textStyle);
      settings.scienceCutoffString = GUILayout.TextField(settings.scienceCutoffString, 5, numberFieldStyle);
      GUILayout.EndHorizontal();
      if (GUILayout.Toggle(settings.runOneTimeScience, "Run one-time only science", toggleStyle))
      {
        settings.runOneTimeScience = true;
      }
      else
      {
        settings.runOneTimeScience = false;
      }
      if (GUILayout.Toggle(settings.transferScience, "Transfer science to container", toggleStyle))
      {
        settings.transferScience = true;
      }
      else
      {
        settings.transferScience = false;
      }
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Save", buttonStyle))
      {
        settings.save();
      }
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      GUILayout.EndVertical();

      GUI.DragWindow();
    }
  }
}
