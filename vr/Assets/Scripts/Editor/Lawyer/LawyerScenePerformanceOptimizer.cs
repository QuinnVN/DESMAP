using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NCKH.VR.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NCKH.VR.Lawyer.Editor
{
    internal static class LawyerScenePerformanceOptimizer
    {
        private const string OfficeScenePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string CourtroomScenePath = "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";
        private const string OptimizedPrefabFolder = "Assets/Generated/Lawyer/NPC/Optimized";
        private const int TextureCandidatesPerScene = 64;

        private static readonly string[] ScenePaths = { OfficeScenePath, CourtroomScenePath };
        private static readonly string[] PrefabArrayNames =
        {
            "femaleModelPrefabs",
            "maleModelPrefabs",
            "hairstylePrefabs",
            "maleHairstylePrefabs"
        };

        [MenuItem("Tools/NCKH/Performance/Optimize Lawyer Scenes")]
        public static void OptimizeLawyerScenes()
        {
            if (HasUnsavedSceneChanges())
            {
                Debug.LogError("Hãy lưu các cảnh đang chỉnh sửa trước khi tối ưu cảnh luật sư.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                int prefabCount = OptimizeCharacterPrefabs();
                int textureCount = OptimizeTextureImports();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"Đã tối ưu cảnh luật sư: {prefabCount} prefab nhân vật và "
                    + $"{textureCount} texture Android/streaming.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        private static int OptimizeCharacterPrefabs()
        {
            EnsureAssetFolder(OptimizedPrefabFolder);
            Dictionary<string, GameObject> optimizedBySource = new Dictionary<string, GameObject>();
            int generatedCount = 0;

            for (int sceneIndex = 0; sceneIndex < ScenePaths.Length; sceneIndex++)
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePaths[sceneIndex], OpenSceneMode.Single);
                NpcAppearanceRandomizer[] randomizers = GetSceneRandomizers(scene);
                bool sceneChanged = false;
                for (int randomizerIndex = 0; randomizerIndex < randomizers.Length; randomizerIndex++)
                {
                    SerializedObject serialized = new SerializedObject(randomizers[randomizerIndex]);
                    for (int arrayIndex = 0; arrayIndex < PrefabArrayNames.Length; arrayIndex++)
                    {
                        SerializedProperty array = serialized.FindProperty(PrefabArrayNames[arrayIndex]);
                        if (array == null || !array.isArray)
                            continue;

                        for (int elementIndex = 0; elementIndex < array.arraySize; elementIndex++)
                        {
                            SerializedProperty element = array.GetArrayElementAtIndex(elementIndex);
                            GameObject source = element.objectReferenceValue as GameObject;
                            if (source == null)
                                continue;

                            string sourcePath = AssetDatabase.GetAssetPath(source);
                            if (sourcePath.StartsWith(OptimizedPrefabFolder, StringComparison.Ordinal))
                                continue;

                            if (!optimizedBySource.TryGetValue(sourcePath, out GameObject optimized))
                            {
                                optimized = CreateOptimizedPrefab(source, sourcePath);
                                if (optimized == null)
                                    continue;
                                optimizedBySource.Add(sourcePath, optimized);
                                generatedCount++;
                            }

                            element.objectReferenceValue = optimized;
                            sceneChanged = true;
                        }
                    }

                    if (sceneChanged)
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            return generatedCount;
        }

        private static GameObject CreateOptimizedPrefab(GameObject source, string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return null;

            string guid = AssetDatabase.AssetPathToGUID(sourcePath);
            string safeName = SanitizeFileName(source.name);
            string destination = $"{OptimizedPrefabFolder}/{safeName}_{guid.Substring(0, 8)}.prefab";
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(source, previewScene) as GameObject;
                if (instance == null)
                    return null;

                if (PrefabUtility.IsPartOfPrefabInstance(instance))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                StripLowerLods(instance);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, destination, out bool success);
                return success ? saved : null;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void StripLowerLods(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            HashSet<GameObject> lowerLodObjects = new HashSet<GameObject>();
            for (int index = 0; index < renderers.Length; index++)
            {
                if (NpcAppearanceRandomizer.ParseLodIndex(renderers[index].name) > 0)
                    lowerLodObjects.Add(renderers[index].gameObject);
            }

            GameObject[] ordered = lowerLodObjects
                .OrderByDescending(candidate => Depth(candidate.transform))
                .ToArray();
            for (int index = 0; index < ordered.Length; index++)
            {
                GameObject candidate = ordered[index];
                if (candidate == null)
                    continue;
                if (candidate == root)
                {
                    Renderer renderer = candidate.GetComponent<Renderer>();
                    MeshFilter filter = candidate.GetComponent<MeshFilter>();
                    if (renderer != null)
                        Object.DestroyImmediate(renderer);
                    if (filter != null)
                        Object.DestroyImmediate(filter);
                }
                else
                {
                    Object.DestroyImmediate(candidate);
                }
            }

            LODGroup[] lodGroups = root.GetComponentsInChildren<LODGroup>(true);
            for (int index = 0; index < lodGroups.Length; index++)
                Object.DestroyImmediate(lodGroups[index]);
        }

        private static int OptimizeTextureImports()
        {
            HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < ScenePaths.Length; index++)
            {
                List<TextureCandidate> candidates = GetTextureCandidates(ScenePaths[index]);
                for (int candidateIndex = 0;
                     candidateIndex < Mathf.Min(TextureCandidatesPerScene, candidates.Count);
                     candidateIndex++)
                {
                    selected.Add(candidates[candidateIndex].Path);
                }
            }

            int changedCount = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in selected)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                        continue;

                    // Baked lightmaps and shadow masks are generated lighting data, not ordinary
                    // material textures. Forcing them to ASTC/ASTC HDR can corrupt their editor
                    // preview and changes the precision that the bake depends on.
                    if (IsBakedLightingTexture(path))
                        continue;

                    bool isNpc = IsNpcTexture(path);
                    TextureImporterPlatformSettings android =
                        importer.GetPlatformTextureSettings("Android");
                    android.name = "Android";
                    android.overridden = true;
                    android.maxTextureSize = isNpc ? 2048 : 1024;
                    android.format = TextureImporterFormat.ASTC_6x6;
                    android.textureCompression = TextureImporterCompression.Compressed;
                    android.compressionQuality = 50;
                    importer.SetPlatformTextureSettings(android);

                    if (!isNpc && importer.mipmapEnabled && importer.textureShape == TextureImporterShape.Texture2D)
                        importer.streamingMipmaps = true;

                    if (AssetDatabase.WriteImportSettingsIfDirty(path))
                        changedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return changedCount;
        }

        private static List<TextureCandidate> GetTextureCandidates(string scenePath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);
            List<TextureCandidate> candidates = new List<TextureCandidate>();
            for (int index = 0; index < dependencies.Length; index++)
            {
                string path = dependencies[index];
                if (ShouldSkipTexture(path))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || !importer.mipmapEnabled)
                    continue;

                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                if (Mathf.Max(width, height) < 2048)
                    continue;
                candidates.Add(new TextureCandidate(path, width, height));
            }

            return candidates
                .OrderByDescending(candidate => (long)candidate.Width * candidate.Height)
                .ThenBy(candidate => candidate.Path, StringComparer.Ordinal)
                .ToList();
        }

        private static bool ShouldSkipTexture(string path)
        {
            return path.StartsWith("Assets/ThirdParty/TextMesh Pro/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Assets/XR/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Assets/XRI/", StringComparison.OrdinalIgnoreCase)
                || IsBakedLightingTexture(path)
                || path.IndexOf("/UI/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("Font", StringComparison.OrdinalIgnoreCase) >= 0
                || path.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBakedLightingTexture(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.StartsWith("Lightmap-", StringComparison.OrdinalIgnoreCase)
                || fileName.IndexOf("_comp_light", StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("_comp_dir", StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("_comp_shadowmask", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsNpcTexture(string path)
        {
            return path.IndexOf("TirgamesAssets", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("npc_casual_set", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("Character", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static NpcAppearanceRandomizer[] GetSceneRandomizers(Scene scene)
        {
            List<NpcAppearanceRandomizer> result = new List<NpcAppearanceRandomizer>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                result.AddRange(roots[index].GetComponentsInChildren<NpcAppearanceRandomizer>(true));
            return result.ToArray();
        }

        private static bool HasUnsavedSceneChanges()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                    return true;
            }
            return false;
        }

        private static int Depth(Transform target)
        {
            int depth = 0;
            while (target.parent != null)
            {
                depth++;
                target = target.parent;
            }
            return depth;
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] segments = folder.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private readonly struct TextureCandidate
        {
            public TextureCandidate(string path, int width, int height)
            {
                Path = path;
                Width = width;
                Height = height;
            }

            public string Path { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
