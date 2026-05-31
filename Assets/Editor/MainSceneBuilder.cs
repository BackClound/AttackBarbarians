using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class MainSceneBuilder
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";

    [MenuItem("Attack Barbarians/UI/Create MainScene")]
    public static void CreateMainScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene";

        CreateCamera();
        CreateEventSystem();
        CreateGameSystems();
        CreateMainSceneUi();

        EditorSceneManager.SaveScene(scene, MainScenePath);
        AddScenesToBuildSettings();
        AssetDatabase.Refresh();

        Debug.Log($"[MainSceneBuilder] MainScene created: {MainScenePath}");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.08f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 9.6f;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        Type inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null)
        {
            eventSystemObject.AddComponent(inputModuleType);
        }
        else
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private static void CreateGameSystems()
    {
        GameObject systemsObject = new GameObject("GameSystems");
        GameBootstrapper bootstrapper = systemsObject.AddComponent<GameBootstrapper>();

        SerializedObject serializedBootstrapper = new SerializedObject(bootstrapper);
        serializedBootstrapper.FindProperty("initializeOnAwake").boolValue = true;
        serializedBootstrapper.FindProperty("dontDestroyOnLoad").boolValue = false;
        serializedBootstrapper.FindProperty("postBootstrapFlow").enumValueIndex = (int)BootstrapPostFlow.OpenMainMenu;
        serializedBootstrapper.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateMainSceneUi()
    {
        GameObject uiObject = new GameObject("MainSceneUI");
        MainSceneView view = uiObject.AddComponent<MainSceneView>();

        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("battleSceneName").stringValue = "BattleScene";
        serializedView.FindProperty("loadBattleSceneOnStart").boolValue = true;
        serializedView.FindProperty("buildOnAwake").boolValue = true;
        serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(scene => scene != null && !string.IsNullOrWhiteSpace(scene.path))
            .ToList();

        UpsertScene(scenes, MainScenePath, insertAtStart: true);
        UpsertScene(scenes, BattleScenePath, insertAtStart: false);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void UpsertScene(List<EditorBuildSettingsScene> scenes, string path, bool insertAtStart)
    {
        int existingIndex = scenes.FindIndex(scene => scene.path == path);
        if (existingIndex >= 0)
        {
            scenes[existingIndex].enabled = true;
            if (insertAtStart && existingIndex != 0)
            {
                EditorBuildSettingsScene scene = scenes[existingIndex];
                scenes.RemoveAt(existingIndex);
                scenes.Insert(0, scene);
            }

            return;
        }

        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(path, enabled: true);
        if (insertAtStart)
        {
            scenes.Insert(0, newScene);
        }
        else
        {
            scenes.Add(newScene);
        }
    }
}
