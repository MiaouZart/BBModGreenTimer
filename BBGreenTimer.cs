using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BBModMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BBGreenTimer
{
    public class BBGreenTimer : MelonMod
    {
        [HarmonyPatch(typeof(GameUI), "Awake")]
        private static class PatchGameUILightAwake
        {
            [HarmonyPrefix]
            private static void HarmonyPatchPrefixGameUi(GameUI __instance) {
                __instance.gameObject.AddComponent<BBGreenTimerComponent>();
            }
        }
    }

    class BBGreenTimerComponent : MonoBehaviour
    {
        private VisualElement _greenTimerRoot;
        private bool _enable = true;
        private static Color textColor = new Color(0.0f, 1f, 0.5f, 0.7f);
        private static Color textPauseColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        private float baseFontSize = 30f;
        private float baseMarginBottom = 10;
        private GameUI gameUI;
        private VisualElement HuDCustomMapVisualElement;
        private VisualElement HuDVisualElement;
        private ModMenu _modMenu;
        private Label _greenTimerLabel;
        private float timer = 0;
        private VisualElement currentParent;
        private PlayerStats _playerStats;
        private VisualElement screenVisualElement;
        private string currentMap = "";

        public void Start() {
            Debug.Log("GreenTimer Loaded Plugin Start");

            _greenTimerRoot = new VisualElement();
            _greenTimerRoot.style.position = Position.Absolute;
            _greenTimerRoot.style.unityTextAlign = TextAnchor.MiddleCenter;
            _greenTimerRoot.name = "GreenTimerRoot";
            _greenTimerRoot.transform.position = new Vector3(0f, 0f, 0f);


            _greenTimerLabel = new Label();
            _greenTimerLabel.text = 0f.TimeToString(true);
            _greenTimerLabel.style.color = textColor;

            _greenTimerRoot.Add(_greenTimerLabel);

            gameUI = GameObject.Find("GameUI").GetComponent<GameUI>();
            var screens = typeof(GameUI).GetField("screens", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(gameUI) as List<UIScreen>;

            if (screens != null)
            {
                var hudScreen = screens.FirstOrDefault(screen => screen is HUDScreen) as HUDScreen;
                var backingField = typeof(UIScreen).GetField($"<Screen>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                CustomMapHUDScreen customMapHUDScreen =
                    screens.FirstOrDefault(screen => screen is CustomMapHUDScreen) as CustomMapHUDScreen;
                if (backingField != null)
                {
                    var visualElementOfMainScreen = backingField.GetValue(hudScreen) as VisualElement;
                    HuDVisualElement = visualElementOfMainScreen;
                    HuDVisualElement?.Add(_greenTimerRoot);

                    var visualElementOfCustomMap = backingField.GetValue(customMapHUDScreen) as VisualElement;
                    HuDCustomMapVisualElement = visualElementOfCustomMap;

                }
            }

            _playerStats = GameObject.FindObjectOfType<PlayerStats>();

            _modMenu = screens?.FirstOrDefault(screen => screen is ModMenu) as ModMenu;

            if (_modMenu is null)
            {
                Debug.Log("ModMenu not found");
                return;
            }

            String categoryName = "GreenTimer";
            var greenTimerSetting = _modMenu.AddSetting(categoryName);

            var xSlider = _modMenu.CreateSlider(categoryName, "XPositon", 0, 100, 50, true);
            var ySlider = _modMenu.CreateSlider(categoryName, "YPosition", 0, 100, 50, true);
            var scaleSlider = _modMenu.CreateSlider(categoryName, "Scale", 0.1f, 3f, 1f, false);

            Action updatePosition = () => {
                _greenTimerRoot.style.top = Length.Percent(ySlider.value);
                _greenTimerRoot.style.left = Length.Percent(xSlider.value);
                DisplayInSetting();
            };

            Action updateScale = () => {
                float scale = scaleSlider.value;
                _greenTimerLabel.style.fontSize = baseFontSize * scale;
            };

            xSlider.RegisterValueChangedCallback(evt => updatePosition());
            ySlider.RegisterValueChangedCallback(evt => updatePosition());
            scaleSlider.RegisterValueChangedCallback(evt => updateScale());



            var position = _modMenu.CreateGroup("Position");
            var xWrapper = _modMenu.CreateWrapper();
            xWrapper.Add(_modMenu.CreateLabel("X position"));
            xWrapper.Add(xSlider);
            var yWrapper = _modMenu.CreateWrapper();
            yWrapper.Add(_modMenu.CreateLabel("Y position"));
            yWrapper.Add(ySlider);

            var scaleWrapper = _modMenu.CreateWrapper();
            scaleWrapper.Add(_modMenu.CreateLabel("Scale"));
            scaleWrapper.Add(scaleSlider);


            var control = _modMenu.CreateGroup("Control");
            var enableWrapper = _modMenu.CreateWrapper();
            enableWrapper.Add(_modMenu.CreateLabel("Enable"));
            var toggleOnOFf = _modMenu.CreateToggle(categoryName, "On", enabled);
            toggleOnOFf.RegisterValueChangedCallback(delegate(ChangeEvent<bool> b){
                enabled = b.newValue;
                _greenTimerRoot.visible = enabled;
            });
            enableWrapper.Add(toggleOnOFf);



            var resetWrapper = _modMenu.CreateWrapper();
            var resetBtn = _modMenu.CreateButton("Reset");
            resetWrapper.Add(_modMenu.CreateLabel("reset"));
            resetBtn.clicked += ResetTimer;
            resetWrapper.Add(resetBtn);



            position.Add(xWrapper);
            position.Add(yWrapper);
            position.Add(scaleWrapper);
            control.Add(resetWrapper);
            control.Add(enableWrapper);

            greenTimerSetting.Add(position);
            greenTimerSetting.Add(control);

            enabled = toggleOnOFf.value;
            _greenTimerRoot.visible = enabled;
            updatePosition();
            updateScale();
        }

        private void DisplayInSetting() {
            _modMenu.getRootVisualElement().Add(_greenTimerRoot);
            _greenTimerRoot.style.backgroundColor = Color.white;
        }

        private void Update() {
            if (!enabled)
            {
                return;
            }

            if (gameUI.ActiveScreen == null) return;
            var screenField = typeof(UIScreen).GetField("<Screen>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            screenVisualElement = screenField?.GetValue(gameUI.ActiveScreen) as VisualElement;
            if (screenVisualElement != null && screenVisualElement != currentParent)
            {

                _greenTimerRoot?.RemoveFromHierarchy();
                screenVisualElement.Add(_greenTimerRoot);
                currentParent = screenVisualElement;
                if (_greenTimerRoot != null)
                {
                    _greenTimerRoot.SendToBack();
                    _greenTimerRoot.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
                }
            }

            timer += !gameUI.player.IsFrozen ? Time.deltaTime : 0;
            _greenTimerLabel.style.color = !gameUI.player.IsFrozen ? textColor : textPauseColor;
            _greenTimerLabel.text = timer.TimeToString(true);

            string newMap = LoadCurrentMainStats();
            if (!string.IsNullOrEmpty(newMap) && newMap != currentMap)
            {
                SwitchMap();
            }

        }

        private void SaveTime()
        {
            if (string.IsNullOrEmpty(currentMap)) return;

            try
            {
                if (!BBSettings.HasEntry("GreenTimer", currentMap))
                {
                    BBSettings.AddEntry("GreenTimer", currentMap, timer);
                }
                else
                {
                    BBSettings.SetEntryValue("GreenTimer", currentMap, timer);
                }

                BBSettings.SavePref();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error saving time: {e}");
            }
        }


        private void SwitchMap()
        {
            SaveTime();

            currentMap = LoadCurrentMainStats();

            if (!BBSettings.HasEntry("GreenTimer", currentMap))
            {
                BBSettings.AddEntry("GreenTimer", currentMap, 0f);
            }

            timer = BBSettings.GetEntryValue<float>("GreenTimer", currentMap);
        }


        private void OnApplicationQuit() {
            SaveTime();
        }


      
        private void ResetTimer() {
            timer = 0;
        }
        
        
        
        public string LoadCurrentMainStats()
        {
            if (MapEditor.Instance.mapName != "Untitled")
            {
                return MapEditor.Instance.mapName;
            }
            switch (_playerStats.CurrentStatsMode)
            {
                case PlayerStats.StatsMode.Main:
                    return "main";
                case PlayerStats.StatsMode.DLC1:
                    return "DLC1";
                case PlayerStats.StatsMode.Birthday:
                    return "birthday";
                default:
                    return "";
            }
        }
            
        

        }
}
