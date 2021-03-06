﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IllusionPlugin;
using IllusionInjector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace PracticePlugin
{
    public class Plugin : IPlugin
    {
        public string Name
        {
            get { return "Practice Plugin"; }
        }

        public string Version
        {
            get { return "v4.1.0"; }
        }

        public const float SpeedMaxSize = 5.05f;
        public const float SpeedStepSize = 0.05f;

        public const int NjsMaxSize = 100;
        public const int NjstepSize = 1;

        public const string MenuSceneName = "Menu";
        public const string GameSceneName = "GameCore";
        public const string ContextSceneName = "GameplayCore";

        public static GameObject SpeedSettingsObject { get; private set; }
        public static GameObject NjsSettingsObject { get; private set; }
        public static GameObject SpawnOffsetSettingsObject { get; private set; }
        public static bool multiActive;
        public static float TimeScale
        {
            get { return _timeScale; }
            private set
            {
                _timeScale = value;
         //       AudioTimeSync.SetPrivateField("_timeScale", value);
        //        AudioTimeSync.Init(_songAudio.clip, _songAudio.time, AudioTimeSync.GetPrivateField<float>("_songTimeOffset"), value);
                if (_timeScale == 1f)
                    _mixer.musicPitch = 1;
                else
                    _mixer.musicPitch = 1f / _timeScale;
                if (!IsEqualToOne(_timeScale))
                {
                    HasTimeScaleChanged = true;

                    if (AudioTimeSync != null)
                    {
                        AudioTimeSync.forcedAudioSync = true;
                    }
                }
                else
                {
                    if (AudioTimeSync != null)
                    {
                        AudioTimeSync.forcedAudioSync = false;
                    }
                }

                if (_songAudio != null)
                {
                    _songAudio.pitch = _timeScale;
                }
            }
        }

        private static float _timeScale = 1;

        public static bool PracticeMode { get; private set; }

        public static bool HasTimeScaleChanged { get; private set; }

        public static bool PlayingNewSong { get; private set; }

        private static bool _init;
        public static StandardLevelSceneSetupDataSO _levelData { get; private set; }
        public static BeatmapObjectSpawnController _spawnController { get; private set; }
        public static AudioTimeSyncController AudioTimeSync { get; private set; }
        private static AudioMixerSO _mixer;
        private static AudioSource _songAudio;
        private static GameplayCoreSceneSetup _gameCoreSceneSetup;
        private static string _lastLevelId;
        private static UIElementsCreator _uiElementsCreator;
        private static bool _resetNoFail;

        public void OnApplicationStart()
        {
            if (_init) return;
            _init = true;
            SceneManager.activeSceneChanged += OneSceneChanged;

            NoFailGameEnergy.limitLevelFail = ModPrefs.GetBool("PracticePlugin", "limitLevelFailDisplay", false, true);
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= OneSceneChanged;
        }

        private void OneSceneChanged(Scene oldScene, Scene newScene)
        {
            Object.Destroy(Resources.FindObjectsOfTypeAll<UIElementsCreator>().FirstOrDefault()?.gameObject);
            if (newScene.name == MenuSceneName)
            {
                if (_resetNoFail)
                {
                    var resultsViewController =
                        Resources.FindObjectsOfTypeAll<ResultsViewController>().FirstOrDefault();
                    if (resultsViewController != null)
                        resultsViewController.continueButtonPressedEvent +=
                            ResultsViewControllerOnContinueButtonPressedEvent;
                }

                if (SpeedSettingsObject != null) return;

                var volumeSettings = Resources.FindObjectsOfTypeAll<VolumeSettingsController>().FirstOrDefault();

                if (volumeSettings == null) return;

                volumeSettings.gameObject.SetActive(false);
                SpeedSettingsObject = Object.Instantiate(volumeSettings.gameObject);
                SpeedSettingsObject.SetActive(false);
                volumeSettings.gameObject.SetActive(true);

                if (SpeedSettingsObject == null) return;

                var volume = SpeedSettingsObject.GetComponent<VolumeSettingsController>();
                ReflectionUtil.CopyComponent(volume, typeof(IncDecSettingsController),
                    typeof(SpeedSettingsController), SpeedSettingsObject);
                Object.DestroyImmediate(volume);

                SpeedSettingsObject.GetComponentInChildren<TMP_Text>().text = "SPEED";
                Object.DontDestroyOnLoad(SpeedSettingsObject);


                //NJS Object
                if (NjsSettingsObject != null) return;

                var volumeSettings2 = Resources.FindObjectsOfTypeAll<VolumeSettingsController>().FirstOrDefault();

                if (volumeSettings2 == null) return;

                volumeSettings2.gameObject.SetActive(false);
                NjsSettingsObject = Object.Instantiate(volumeSettings2.gameObject);
                NjsSettingsObject.SetActive(false);
                volumeSettings2.gameObject.SetActive(true);

                if (NjsSettingsObject == null) return;

                var volume2 = NjsSettingsObject.GetComponent<VolumeSettingsController>();
                ReflectionUtil.CopyComponent(volume2, typeof(IncDecSettingsController),
                    typeof(NjsSettingsController), NjsSettingsObject);
                Object.DestroyImmediate(volume2);

                NjsSettingsObject.GetComponentInChildren<TMP_Text>().text = "NJS";
                Object.DontDestroyOnLoad(NjsSettingsObject);


                //Spawn Offset Object
                if (SpawnOffsetSettingsObject != null) return;

                var volumeSettings3 = Resources.FindObjectsOfTypeAll<VolumeSettingsController>().FirstOrDefault();

                if (volumeSettings3 == null) return;

                volumeSettings3.gameObject.SetActive(false);
                SpawnOffsetSettingsObject = Object.Instantiate(volumeSettings3.gameObject);
                SpawnOffsetSettingsObject.SetActive(false);
                volumeSettings3.gameObject.SetActive(true);

                if (SpawnOffsetSettingsObject == null) return;

                var volume3 = SpawnOffsetSettingsObject.GetComponent<VolumeSettingsController>();
                ReflectionUtil.CopyComponent(volume3, typeof(IncDecSettingsController),
                    typeof(SpawnOffsetController), SpawnOffsetSettingsObject);
                Object.DestroyImmediate(volume3);

                SpawnOffsetSettingsObject.GetComponentInChildren<TMP_Text>().text = "Spawn Offset";
                Object.DontDestroyOnLoad(SpawnOffsetSettingsObject);

            }
            else if (newScene.name == GameSceneName)
            {
                CustomEffectPoolsInstaller customEffectPoolsInstaller = null;
                var effectPoolsInstaller = Resources.FindObjectsOfTypeAll<EffectPoolsInstaller>().FirstOrDefault();
                if (effectPoolsInstaller != null)
                {
                    customEffectPoolsInstaller = (CustomEffectPoolsInstaller)ReflectionUtil.CopyComponent(effectPoolsInstaller,
                        typeof(EffectPoolsInstaller), typeof(CustomEffectPoolsInstaller), effectPoolsInstaller.gameObject);
                }

                SceneContext sceneContext = null;
                SceneDecoratorContext sceneDecoratorContext = null;
                try
                {
                    Console.WriteLine("Custom effect Pool Installer Made");
                    foreach (var gameObject in newScene.GetRootGameObjects())
                    {
                        if (sceneContext == null)
                        {
                            sceneContext = gameObject.GetComponentInChildren<SceneContext>(true);
                        }
                    }

                    foreach (var gameObject in SceneManager.GetSceneByName(ContextSceneName).GetRootGameObjects())
                    {
                        if (sceneDecoratorContext == null)
                        {
                            sceneDecoratorContext = gameObject.GetComponentInChildren<SceneDecoratorContext>(true);
                        }
                    }


                    if (sceneContext != null && sceneDecoratorContext != null)
                    {
                        var prop = typeof(Context).GetField("_installers", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        var installersList = (List<MonoInstaller>)prop.GetValue(sceneDecoratorContext);
                        installersList.Remove(effectPoolsInstaller);
                        Object.DestroyImmediate(effectPoolsInstaller);
                        installersList.Add(customEffectPoolsInstaller);
                        Console.WriteLine("Custom effect Pool Installer Added");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }


                if (_levelData == null)
                {
                    _levelData = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
                    if (_levelData == null) return;
                    _levelData.didFinishEvent += MainGameSceneSetupDataOnDidFinishEvent;
                }

                if (_spawnController == null)
                {
                    _spawnController = Resources.FindObjectsOfTypeAll<BeatmapObjectSpawnController>().FirstOrDefault();

                }


                if (_lastLevelId != _levelData.difficultyBeatmap.level.levelID &&
                    !string.IsNullOrEmpty(_lastLevelId))
                {
                    PlayingNewSong = true;
                    HasTimeScaleChanged = false;
                    TimeScale = 1;
                    _lastLevelId = _levelData.difficultyBeatmap.level.levelID;
                }
                else
                {
                    PlayingNewSong = false;
                }

                if (IsEqualToOne(TimeScale))
                {
                    HasTimeScaleChanged = false;
                }

                _lastLevelId = _levelData.difficultyBeatmap.level.levelID;
                _gameCoreSceneSetup = Resources.FindObjectsOfTypeAll<GameplayCoreSceneSetup>().FirstOrDefault();
                AudioTimeSync = Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault();
                _songAudio = AudioTimeSync.GetPrivateField<AudioSource>("_audioSource");
                _mixer = _gameCoreSceneSetup.GetPrivateField<AudioMixerSO>("_audioMixer");
                PracticeMode = (_levelData.gameplayCoreSetupData.practiceSettings != null && !BS_Utils.Gameplay.Gamemode.IsIsolatedLevel);
                //Check if Multiplayer is active, disable accordingly
                if (PluginManager.Plugins.Any(x => x.Name == "Beat Saber Multiplayer"))
                {
                    GameObject client = GameObject.Find("MultiplayerClient");
                    if (client != null)
                    {
                        Console.WriteLine("[PracticePlugin] Found MultiplayerClient game object!");
                        multiActive = true;

                    }
                    else
                    {
                        Console.WriteLine("[PracticePlugin] MultiplayerClient game object not found!");
                    }
                }
                if (multiActive == true)
                    PracticeMode = false;


                if (!PracticeMode)
                {
                    TimeScale = Mathf.Clamp(TimeScale, 1, SpeedMaxSize);
                }
                if (PracticeMode)
                {
                    if (_levelData.gameplayCoreSetupData.practiceSettings.songSpeedMul != 1f)
                        _timeScale = _levelData.gameplayCoreSetupData.practiceSettings.songSpeedMul;
                    else
                        _timeScale = _levelData.gameplayCoreSetupData.gameplayModifiers.songSpeedMul;
                    SharedCoroutineStarter.instance.StartCoroutine(DelayedUI());
                }

            }
        }

        public System.Collections.IEnumerator DelayedUI()
        {
            yield return new WaitForSeconds(0.5f);
            try
            {
                Console.WriteLine("Atemmpting Practice Plugin UI");
                var canvas = GameObject.Find("PauseMenu").transform.Find("Wrapper").transform.Find("UI").transform.Find("Canvas");

                if (canvas == null)
                {
                    Console.WriteLine("Canvas Null");

                }
                _uiElementsCreator = canvas.gameObject.AddComponent<UIElementsCreator>();
                _uiElementsCreator.ValueChangedEvent += UIElementsCreatorOnValueChangedEvent;
                _uiElementsCreator.Init();
                TimeScale = TimeScale;

                var bg = GameObject.Find("PauseMenu").transform.Find("Wrapper").transform.Find("UI").transform.Find("BG");
          //      bg.transform.localScale = new Vector3(bg.transform.localScale.x * 1f, bg.transform.localScale.y * 1.2f, bg.transform.localScale.z * 1f);
                bg.transform.localPosition = new Vector3(bg.transform.localPosition.x, bg.transform.localPosition.y - 0.35f, bg.transform.localPosition.z);
                var pauseMenu = GameObject.Find("PauseMenu");
                pauseMenu.transform.localPosition = new Vector3(pauseMenu.transform.localPosition.x, pauseMenu.transform.localPosition.y + 0.175f, pauseMenu.transform.localPosition.z);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void ResultsViewControllerOnContinueButtonPressedEvent(ResultsViewController obj)
        {
            /*
			PersistentSingleton<GameDataModel>.instance.gameDynamicData.GetCurrentPlayerDynamicData()
				.gameplayOptions.noEnergy = false;
                */
        }

        private void MainGameSceneSetupDataOnDidFinishEvent(StandardLevelSceneSetupDataSO levelData, LevelCompletionResults results)
        {
         
            /*
             * 
			if (!NoFail && HasTimeScaleChanged && results != null &&
			    results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared)
			{
				levelData.gameplayCoreSetupData.gameplayModifiers.noFail = true;
				_resetNoFail = true;
			}
            */
        }

        private void UIElementsCreatorOnValueChangedEvent(float timeScale)
        {
            if (!IsEqualToOne(timeScale))
            {
                HasTimeScaleChanged = true;
            }

            TimeScale = timeScale;
        }

        private static bool IsEqualToOne(float value)
        {
            return Math.Abs(value - 1) < 0.000000001f;
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {

        }

        public void OnUpdate()
        {
            if (_uiElementsCreator == null || _uiElementsCreator.SongSeeker == null) return;
            _uiElementsCreator.SongSeeker.OnUpdate();
        }

        public void OnFixedUpdate()
        {

        }

        public static void AdjustNJS(float njs)
        {

                float halfJumpDur = 4f;
                float maxHalfJump = _spawnController.GetPrivateField<float>("_maxHalfJumpDistance");
                float noteJumpStartBeatOffset = _levelData.difficultyBeatmap.noteJumpStartBeatOffset;
                float moveSpeed = _spawnController.GetPrivateField<float>("_moveSpeed");
                float moveDir = _spawnController.GetPrivateField<float>("_moveDurationInBeats");
                float jumpDis;
                float spawnAheadTime;
                float moveDis;
                float bpm = _spawnController.GetPrivateField<float>("_beatsPerMinute");
                float num = 60f / bpm;
                moveDis = moveSpeed * num * moveDir;
                while (njs * num * halfJumpDur > maxHalfJump)
                {
                    halfJumpDur /= 2f;
                }
                halfJumpDur += noteJumpStartBeatOffset;
                if (halfJumpDur < 1f) halfJumpDur = 1f;
                //        halfJumpDur = spawnController.GetPrivateField<float>("_halfJumpDurationInBeats");
                jumpDis = njs * num * halfJumpDur * 2f;
                spawnAheadTime = moveDis / moveSpeed + jumpDis * 0.5f / njs;
            _spawnController.SetPrivateField("_halfJumpDurationInBeats", halfJumpDur);
            _spawnController.SetPrivateField("_spawnAheadTime", spawnAheadTime);
            _spawnController.SetPrivateField("_jumpDistance", jumpDis);
            _spawnController.SetPrivateField("_noteJumpMovementSpeed", njs);
            _spawnController.SetPrivateField("_moveDistance", moveDis);

            
        }
        public static void AdjustSpawnOffset(float offset)
        {
            float njs = _spawnController.GetPrivateField<float>("_noteJumpMovementSpeed");
            float halfJumpDur = 4f;
            float maxHalfJump = _spawnController.GetPrivateField<float>("_maxHalfJumpDistance");
            float noteJumpStartBeatOffset = offset;
            float moveSpeed = _spawnController.GetPrivateField<float>("_moveSpeed");
            float moveDir = _spawnController.GetPrivateField<float>("_moveDurationInBeats");
            float jumpDis;
            float spawnAheadTime;
            float moveDis;
            float bpm = _spawnController.GetPrivateField<float>("_beatsPerMinute");
            float num = 60f / bpm;
            moveDis = moveSpeed * num * moveDir;
            while (njs * num * halfJumpDur > maxHalfJump)
            {
                halfJumpDur /= 2f;
            }
            halfJumpDur += noteJumpStartBeatOffset;
            if (halfJumpDur < 1f) halfJumpDur = 1f;
            //        halfJumpDur = spawnController.GetPrivateField<float>("_halfJumpDurationInBeats");
            jumpDis = njs * num * halfJumpDur * 2f;
            spawnAheadTime = moveDis / moveSpeed + jumpDis * 0.5f / njs;
            _spawnController.SetPrivateField("_halfJumpDurationInBeats", halfJumpDur);
            _spawnController.SetPrivateField("_spawnAheadTime", spawnAheadTime);
            _spawnController.SetPrivateField("_jumpDistance", jumpDis);
            _spawnController.SetPrivateField("_noteJumpMovementSpeed", njs);
            _spawnController.SetPrivateField("_moveDistance", moveDis);


        }
    }
}