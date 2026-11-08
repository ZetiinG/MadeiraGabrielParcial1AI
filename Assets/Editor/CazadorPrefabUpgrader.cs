using UnityEditor;
using UnityEngine;
using Boids;
using Core;
using Hunter;
using Navigation;
using Perception;
using POI;
using Steering;
using static EditorTools.BuilderUtils;
using Object = UnityEngine.Object;

namespace EditorTools
{
    internal static class CazadorPrefabUpgrader
    {
        private const string HunterPrefabPath = "Assets/Prefabs/Hunter.prefab";
        private const string BoidPrefabPath = "Assets/Prefabs/Boid.prefab";
        private const string PoiPrefabPath = "Assets/Prefabs/PointOfInterest.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/HunterProjectile.prefab";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        public const string BoidLayerName = "Boid";
        private const string BoidSensorName = "BoidSensor";
        private const int SensorLayer = 2; // Ignore Raycast: los sensores no deben bloquear líneas de visión ni detectarse entre sí.

        private const float FullCircle = 360f;
        private const float HunterWolfDetectionRadius = 8f;
        private const float HunterMass = 0.2f;
        private const float ProjectileScale = 0.25f;
        private const float BoidMaxSpeed = 3.2f;
        private const float BoidMaxForce = 8f;

        private static readonly Color HunterPathColor = new Color(0.1f, 0.9f, 0.9f);
        private static readonly Color WolfLineColor = new Color(1f, 0.1f, 0.1f);
        private static readonly Color ProjectileColor = Color.red;

        public static GameObject UpgradeHunterPrefab(int obstacleMask, int agentLayer, int collisionMask, Material lineMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HunterPrefabPath);

            Configure(GetOrAddComponent<SteeringMotor>(root), so => so.FindProperty("mass").floatValue = HunterMass);
            GetOrAddComponent<CapsuleCollider>(root);
            MakeSolid(root, agentLayer, collisionMask);
            Configure(GetOrAddComponent<FieldOfView>(root), so =>
            {
                so.FindProperty("viewRadius").floatValue = HunterWolfDetectionRadius;
                so.FindProperty("viewAngle").floatValue = FullCircle;
                so.FindProperty("obstacleMask").intValue = obstacleMask;
            });
            Configure(GetOrAddComponent<PathFollower>(root), so =>
                so.FindProperty("pathLine").objectReferenceValue = GetOrCreateLine(root.transform, "PathLine", HunterPathColor, lineMaterial));
            Configure(GetOrAddComponent<HunterPerception>(root), so =>
                so.FindProperty("boidSensor").objectReferenceValue = GetOrCreateBoidSensor(root.transform));
            Configure(GetOrAddComponent<POISpawner>(root), so =>
                so.FindProperty("poiPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PoiPrefabPath));
            Configure(GetOrAddComponent<HunterCombat>(root), so =>
                so.FindProperty("projectilePrefab").objectReferenceValue = GetOrCreateProjectilePrefab());
            GetOrAddComponent<HunterGathering>(root);
            GetOrAddComponent<HunterNavigator>(root);
            GetOrAddComponent<HunterLife>(root);
            ConfigureHunterFeedback(root, lineMaterial);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HunterPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return prefab;
        }

        public static GameObject UpgradeBoidPrefab(int collisionMask)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BoidPrefabPath);

            // El collider del cuerpo deja de ser trigger: los triggers quedan solo para los sensores y el motor los ignora.
            SphereCollider body = root.GetComponent<SphereCollider>();
            body.isTrigger = false;

            Configure(GetOrAddComponent<SteeringMotor>(root), so =>
            {
                so.FindProperty("maxSpeed").floatValue = BoidMaxSpeed;
                so.FindProperty("maxForce").floatValue = BoidMaxForce;
                so.FindProperty("obstacleMask").intValue = collisionMask;
                so.FindProperty("bodyRadius").floatValue = body.radius * root.transform.localScale.x;
            });
            GetOrAddComponent<BoidHealth>(root);
            Configure(GetOrAddComponent<BoidFlocking>(root), so =>
                so.FindProperty("neighborSensor").objectReferenceValue = GetOrCreateBoidSensor(root.transform));
            Configure(GetOrAddComponent<BoidVisualFeedback>(root), so =>
                so.FindProperty("bodyRenderer").objectReferenceValue = root.GetComponent<Renderer>());

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BoidPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return prefab;
        }

        private static void ConfigureHunterFeedback(GameObject root, Material lineMaterial)
        {
            Configure(root.GetComponent<HunterVisualFeedback>(), so =>
            {
                so.FindProperty("wolfLine").objectReferenceValue = GetOrCreateLine(root.transform, "WolfLine", WolfLineColor, lineMaterial);
                so.FindProperty("bodyRenderer").objectReferenceValue = root.GetComponent<Renderer>();
            });
            GetOrAddComponent<Billboard>(root.GetComponentInChildren<TextMesh>().gameObject);
        }

        // El sensor va en un hijo con su propio Rigidbody para no sumarse como collider del cuerpo del agente.
        private static BoidSensor GetOrCreateBoidSensor(Transform parent)
        {
            Transform existing = parent.Find(BoidSensorName);
            GameObject sensorObject = existing != null ? existing.gameObject : new GameObject(BoidSensorName);
            sensorObject.transform.SetParent(parent, false);
            sensorObject.layer = SensorLayer;

            GetOrAddComponent<SphereCollider>(sensorObject).isTrigger = true;
            Rigidbody body = GetOrAddComponent<Rigidbody>(sensorObject);
            body.isKinematic = true;
            body.useGravity = false;

            BoidSensor sensor = GetOrAddComponent<BoidSensor>(sensorObject);
            Configure(sensor, so => so.FindProperty("detectionMask").intValue = 1 << LayerMask.NameToLayer(BoidLayerName));
            return sensor;
        }

        private static HunterProjectile GetOrCreateProjectilePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<HunterProjectile>(ProjectilePrefabPath);
            if (existing != null)
                return existing;

            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "HunterProjectile";
            projectile.transform.localScale = Vector3.one * ProjectileScale;
            Object.DestroyImmediate(projectile.GetComponent<Collider>());
            projectile.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(MaterialsFolder, "Mat_Projectile", UnlitShaderName, ProjectileColor);
            projectile.AddComponent<HunterProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath);
            Object.DestroyImmediate(projectile);
            return prefab.GetComponent<HunterProjectile>();
        }
    }
}
