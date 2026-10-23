using System.IO;
using TrayMatch.Core;
using TrayMatch.Generator;
using UnityEditor;
using UnityEngine;

namespace TrayMatch.EditorTools
{
    // TrayMatch → Bake Levels: builds every RecipeBook level and writes them into one level pack.
    // A level that is already in the pack is never changed silently: players may know it.
    public static class LevelBaker
    {
        public const string PackPath = "Assets/Project/Levels/levels.json";

        [MenuItem("TrayMatch/Bake Levels")]
        static void Bake()
        {
            LevelPack pack;
            try
            {
                pack = PackBuilder.Build();
            }
            catch (System.InvalidOperationException e)
            {
                EditorUtility.DisplayDialog("Bake Levels", e.Message, "OK");
                return;
            }

            if (File.Exists(PackPath))
            {
                var changed = PackBuilder.ChangedLevels(
                    LevelSerializer.PackFromJson(File.ReadAllText(PackPath)), pack);

                if (changed.Count > 0 && !EditorUtility.DisplayDialog(
                    "Bake Levels",
                    $"This changes levels {string.Join(", ", changed)}, which are already in the pack. " +
                    "Only continue if you meant to change them.",
                    "Bake anyway",
                    "Cancel"))
                    return;
            }

            File.WriteAllText(PackPath, LevelSerializer.ToJson(pack));
            AssetDatabase.ImportAsset(PackPath);
            Debug.Log($"Baked {pack.Levels.Count} levels into {PackPath}");
        }
    }
}