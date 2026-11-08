using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Steering;
using Object = UnityEngine.Object;

namespace EditorTools
{
    internal static class BuilderUtils
    {
        public const string MaterialsFolder = "Assets/Materials";
        public const string LevelMaterialsFolder = "Assets/Materials/Level";
        public const string LitShaderName = "Universal Render Pipeline/Lit";

        private const string LabelFontName = "LegacyRuntime.ttf";
        private const float LabelCharacterSize = 0.2f;
        private const int LabelFontSize = 48;
        private const float LineWidth = 0.15f;
        private const int FirstUserLayer = 8;

        public static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0)
                return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = FirstUserLayer; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;

                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }

            throw new InvalidOperationException($"No hay layers libres para crear '{layerName}'.");
        }

        public static void MakeSolid(GameObject agent, int agentLayer, int collisionMask)
        {
            agent.layer = agentLayer;
            Configure(agent.GetComponent<SteeringMotor>(), so => so.FindProperty("obstacleMask").intValue = collisionMask);
        }

        public static Material GetOrCreateMaterial(string folder, string name, string shaderName, Color color)
        {
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                int separator = folder.LastIndexOf('/');
                AssetDatabase.CreateFolder(folder.Substring(0, separator), folder.Substring(separator + 1));
            }

            material = new Material(Shader.Find(shaderName)) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        public static void Configure(Object target, Action<SerializedObject> apply)
        {
            var serializedObject = new SerializedObject(target);
            apply(serializedObject);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        public static TextMesh CreateLabel(Transform parent, Vector3 localPosition, string text)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            Font font = Resources.GetBuiltinResource<Font>(LabelFontName);
            labelObject.AddComponent<MeshRenderer>().sharedMaterial = font.material;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.font = font;
            label.text = text;
            label.characterSize = LabelCharacterSize;
            label.fontSize = LabelFontSize;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            return label;
        }

        public static LineRenderer GetOrCreateLine(Transform parent, string name, Color color, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject lineObject = existing != null ? existing.gameObject : new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = GetOrAddComponent<LineRenderer>(lineObject);
            line.sharedMaterial = material;
            line.widthMultiplier = LineWidth;
            line.startColor = color;
            line.endColor = color;
            line.positionCount = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
    }
}
