using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

public static class IOSBuildSetup
{
    private const string CombinedScenePath = "Assets/CombinedLab.unity";
    private const string MarkerLibraryPath = "Assets/MyMarkers.asset";
    private const string MarkerTexturePath = "Assets/hiro.png";
    private const string PlanePrefabPath = "Assets/AR Default Plane.prefab";
    private const string PlacedObjectPath = "Assets/PlacedObject.prefab";
    private const string MarkerContentPath = "Assets/SpawnedObject.prefab";
    private const string BundleIdentifier = "com.xuany.assignment1.ar";
    private const string ARKitLoaderTypeName = "UnityEngine.XR.ARKit.ARKitLoader";
    private const string CameraUsageDescription =
        "Camera access is used to detect surfaces and reference images for this AR assignment.";

    [MenuItem("Assignment/Configure Combined iOS AR Build")]
    public static void ConfigureCombinedIOSBuild()
    {
        ConfigureMarkerLibrary();
        CreateCombinedScene();
        ConfigurePlayerSettings();
        ConfigureARKitLoader();
        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assignment/Build iOS Xcode Export")]
    public static void BuildIOSXcode()
    {
        ConfigureCombinedIOSBuild();

        string outputPath = GetCommandLineValue("-outputPath");
        if (string.IsNullOrWhiteSpace(outputPath))
            outputPath = "Builds/iOS-Xcode";

        Directory.CreateDirectory(outputPath);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { CombinedScenePath },
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"iOS Xcode export failed: {report.summary.result}");

        Debug.Log($"iOS Xcode export ready: {Path.GetFullPath(outputPath)}");
    }

    private static void CreateCombinedScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "CombinedLab";

        var arSessionObject = new GameObject("AR Session");
        arSessionObject.AddComponent<ARInputManager>();
        arSessionObject.AddComponent<ARSession>();

        var xrOriginObject = new GameObject("XR Origin (Mobile AR)");
        var xrOrigin = xrOriginObject.AddComponent<XROrigin>();

        var cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOriginObject.transform, false);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraOffset.transform, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.nearClipPlane = 0.2f;
        camera.farClipPlane = 20f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<TrackedPoseDriver>();
        cameraObject.AddComponent<ARCameraManager>();
        cameraObject.AddComponent<ARCameraBackground>();

        xrOrigin.Camera = camera;
        xrOrigin.CameraFloorOffsetObject = cameraOffset;

        var raycastManager = xrOriginObject.AddComponent<ARRaycastManager>();
        var planeManager = xrOriginObject.AddComponent<ARPlaneManager>();
        var imageManager = xrOriginObject.AddComponent<ARTrackedImageManager>();
        var tapToPlace = xrOriginObject.AddComponent<TapToPlace>();
        var markerContent = xrOriginObject.AddComponent<MarkerContentController>();
        var status = xrOriginObject.AddComponent<ARStatus>();

        SetSerializedObjectReference(planeManager, "m_PlanePrefab", PlanePrefabPath);
        SetSerializedObjectReference(tapToPlace, "objectToPlace", PlacedObjectPath);
        SetSerializedObjectReference(tapToPlace, "raycastManager", raycastManager);
        SetSerializedObjectReference(imageManager, "m_SerializedLibrary", MarkerLibraryPath);
        SetSerializedObjectReference(markerContent, "contentPrefab", MarkerContentPath);
        SetSerializedObjectReference(status, "planeManager", planeManager);
        SetSerializedObjectReference(status, "imageManager", imageManager);
        SetSerializedObjectReference(status, "placement", tapToPlace);

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        EditorSceneManager.SaveScene(scene, CombinedScenePath);
    }

    private static void ConfigureMarkerLibrary()
    {
        var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(MarkerLibraryPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerTexturePath);
        if (library == null || texture == null)
        {
            Debug.LogWarning("Marker library or hiro.png is missing; image tracking setup was skipped.");
            return;
        }

        var serializedLibrary = new SerializedObject(library);
        var images = serializedLibrary.FindProperty("m_Images");
        if (images == null)
        {
            Debug.LogWarning("Could not locate reference image list in MyMarkers.asset.");
            return;
        }

        if (images.arraySize == 0)
            images.InsertArrayElementAtIndex(0);

        var image = images.GetArrayElementAtIndex(0);
        image.FindPropertyRelative("m_Name").stringValue = "hiro";
        image.FindPropertyRelative("m_Texture").objectReferenceValue = texture;
        image.FindPropertyRelative("m_SpecifySize").boolValue = true;
        image.FindPropertyRelative("m_Size").vector2Value = new Vector2(0.16f, 0.16f);
        serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
    }

    private static void ConfigurePlayerSettings()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        PlayerSettings.companyName = "Xuany";
        PlayerSettings.productName = "Assignment1 AR";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleIdentifier);
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.cameraUsageDescription = CameraUsageDescription;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        EditorUserBuildSettings.SetPlatformSettings("iOS", "CreateXcodeProject", "true");
    }

    private static void ConfigureARKitLoader()
    {
        var xrSettingsAssets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
        if (xrSettingsAssets.Length == 0)
            throw new BuildFailedException("XR Plug-in Management per-build-target settings asset is missing.");

        var settingsPath = AssetDatabase.GUIDToAssetPath(xrSettingsAssets[0]);
        var buildTargetSettings =
            AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(settingsPath);
        if (buildTargetSettings == null)
            throw new BuildFailedException($"Could not load XR Plug-in Management settings: {settingsPath}");

        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

        var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.iOS);
        var managerSettings = buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
        if (generalSettings == null || managerSettings == null)
            throw new BuildFailedException("Failed to create XR Plug-in Management settings for iOS.");

        if (!XRPackageMetadataStore.IsLoaderAssigned(ARKitLoaderTypeName, BuildTargetGroup.iOS) &&
            !XRPackageMetadataStore.AssignLoader(managerSettings, ARKitLoaderTypeName, BuildTargetGroup.iOS))
        {
            throw new BuildFailedException("Failed to assign ARKit Loader for the iOS build target.");
        }

        generalSettings.InitManagerOnStart = true;
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(managerSettings);
        EditorUtility.SetDirty(buildTargetSettings);
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CombinedScenePath, true)
        };
    }

    private static void SetSerializedObjectReference(Object target, string propertyName, Object value)
    {
        var serializedObject = new SerializedObject(target);
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new MissingReferenceException($"Missing serialized property {propertyName} on {target.name}.");

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedObjectReference(Object target, string propertyName, string assetPath)
    {
        var value = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (value == null)
            throw new FileNotFoundException($"Missing asset for {propertyName}: {assetPath}");

        SetSerializedObjectReference(target, propertyName, value);
    }

    private static string GetCommandLineValue(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return null;
    }
}
