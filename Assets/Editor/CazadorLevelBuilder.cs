using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Boids;
using Core;
using Hunter;
using Navigation;
using Perception;
using POI;
using Steering;
using Wolves;
using static EditorTools.BuilderUtils;
using static EditorTools.LevelLayout;
using Object = UnityEngine.Object;

namespace EditorTools
{
    public static class CazadorLevelBuilder
    {
        private const string ScenePath = "Assets/Scenes/CazadorScene.unity";
        private const string LineShaderName = "Sprites/Default";
        private const string ObstacleLayerName = "Obstacle";
        private const string AgentLayerName = "Agent";

        private const float WallHeight = 2.5f;
        private const float FloorThickness = 0.1f;
        private const float AgentHeight = 1f;
        private const float BoidHeight = 0.6f;
        private const int BoidCount = 8;

        private const float CameraHeight = 60f;
        private const float CameraMargin = 2f;
        private const float LightPitch = 50f;
        private const float LightYaw = -30f;

        private const float LabelHeight = 1.6f;
        private const float ZoneLabelHeight = 0.05f;
        private const float RingHeightOffset = -0.9f;
        private const float MarkerDiameter = 0.8f;
        private const float MarkerThickness = 0.05f;

        private const float WolfViewRadius = 15f;
        private const float WolfViewAngle = 140f;
        private const float WolfMass = 0.3f;

        private static readonly (char Cell, string Name, Color Color)[] Floors =
        {
            (CorridorCell, "Corridor", new Color(0.78f, 0.76f, 0.7f)),
            (StartCell, "Start", new Color(0.45f, 0.7f, 0.5f)),
            (HuntingGroundCell, "HuntingGround", new Color(0.85f, 0.7f, 0.45f)),
            (SafeCell, "Safe", new Color(0.65f, 0.55f, 0.8f)),
            (ExitCell, "Exit", new Color(0.5f, 0.65f, 0.85f)),
        };

        private static readonly Color WallColor = new Color(0.2f, 0.22f, 0.27f);
        private static readonly Color BackgroundColor = new Color(0.1f, 0.11f, 0.13f);
        private static readonly Color AmbientColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color WolfPathColor = new Color(1f, 0.45f, 0.1f);
        private static readonly Color MarkerColor = new Color(0.9f, 0.1f, 0.1f);

        private sealed class LevelZones
        {
            public Zone Start;
            public Zone HuntingGround;
            public Zone Exit;
            public readonly List<Zone> Safe = new List<Zone>();
            public ArenaBounds Arena;
        }

        [MenuItem("Tools/Cazador/Build Level")]
        public static void BuildLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!EditorUtility.DisplayDialog("Build Cazador Level", $"Se va a regenerar {ScenePath}.", "Generar", "Cancelar"))
                return;

            int obstacleLayer = EnsureLayer(ObstacleLayerName);
            int obstacleMask = 1 << obstacleLayer;
            int agentLayer = EnsureLayer(AgentLayerName);
            // Los cuerpos chocan contra paredes y entre sí; percepción y grafo siguen mirando solo las paredes.
            int collisionMask = obstacleMask | 1 << agentLayer | 1 << EnsureLayer(CazadorPrefabUpgrader.BoidLayerName);
            Material lineMaterial = GetOrCreateMaterial(LevelMaterialsFolder, "Mat_DebugLine", LineShaderName, Color.white);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCameraAndLight();
            BuildGeometry(obstacleLayer);
            LevelZones zones = CreateZones();
            NavGraph graph = CreateGraph(obstacleMask);

            GameObject hunterPrefab = CazadorPrefabUpgrader.UpgradeHunterPrefab(obstacleMask, agentLayer, collisionMask, lineMaterial);
            GameObject boidPrefab = CazadorPrefabUpgrader.UpgradeBoidPrefab(collisionMask);
            HunterController hunter = CreateHunter(hunterPrefab, zones, graph);
            CreateBoids(boidPrefab, zones.Arena, hunter);
            CreateWolves(graph, hunter, obstacleMask, agentLayer, collisionMask, lineMaterial);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void CreateCameraAndLight()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height * CellSize * 0.5f + CameraMargin;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            cameraObject.transform.SetPositionAndRotation(Vector3.up * CameraHeight, Quaternion.Euler(90f, 0f, 0f));
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            lightObject.AddComponent<Light>().type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(LightPitch, LightYaw, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
        }

        private static void BuildGeometry(int obstacleLayer)
        {
            Transform root = new GameObject("Level").transform;

            Material wallMaterial = GetOrCreateMaterial(LevelMaterialsFolder, "Mat_Wall", LitShaderName, WallColor);
            foreach (RectInt rect in MergeCells(WallCell))
            {
                GameObject wall = CreateBlock("Wall", rect, 0f, WallHeight, wallMaterial, root);
                wall.layer = obstacleLayer;
                wall.isStatic = true;
            }

            foreach (var floor in Floors)
            {
                Material material = GetOrCreateMaterial(LevelMaterialsFolder, $"Mat_Floor{floor.Name}", LitShaderName, floor.Color);
                foreach (RectInt rect in MergeCells(floor.Cell))
                {
                    GameObject tile = CreateBlock("Floor_" + floor.Name, rect, -FloorThickness, FloorThickness, material, root);
                    Object.DestroyImmediate(tile.GetComponent<Collider>());
                }
            }
        }

        private static GameObject CreateBlock(string name, RectInt rect, float bottom, float height, Material material, Transform parent)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = RectCenter(rect) + Vector3.up * (bottom + height * 0.5f);
            block.transform.localScale = new Vector3(rect.width * CellSize, height, rect.height * CellSize);
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static LevelZones CreateZones()
        {
            Transform root = new GameObject("Zones").transform;
            RectInt huntingGroundRegion = FindRegions(HuntingGroundCell)[0];

            var zones = new LevelZones
            {
                Start = CreateZone("Zone_Start", "INICIO", ZoneType.Start, FindRegions(StartCell)[0], root),
                HuntingGround = CreateZone("Zone_HuntingGround", "COTO DE CAZA", ZoneType.HuntingGround, huntingGroundRegion, root),
                Exit = CreateZone("Zone_Exit", "SALIDA", ZoneType.Exit, FindRegions(ExitCell)[0], root),
            };

            foreach (RectInt region in FindRegions(SafeCell))
                zones.Safe.Add(CreateZone("Zone_Safe_" + Quadrant(region), "ZONA\nSEGURA", ZoneType.Safe, region, root));

            // Los bounds de los Boids quedan media celda por dentro del coto para que no se metan en las paredes.
            Vector2 arenaSize = ZoneSize(huntingGroundRegion) - Vector2.one * CellSize;
            zones.Arena = zones.HuntingGround.gameObject.AddComponent<ArenaBounds>();
            Configure(zones.Arena, so => so.FindProperty("size").vector2Value = arenaSize);

            return zones;
        }

        private static Zone CreateZone(string name, string displayName, ZoneType type, RectInt region, Transform parent)
        {
            var zoneObject = new GameObject(name);
            zoneObject.transform.SetParent(parent);
            zoneObject.transform.position = RectCenter(region);

            Zone zone = zoneObject.AddComponent<Zone>();
            Configure(zone, so =>
            {
                so.FindProperty("type").enumValueIndex = (int)type;
                so.FindProperty("size").vector2Value = ZoneSize(region);
            });

            TextMesh label = CreateLabel(zoneObject.transform, Vector3.up * ZoneLabelHeight, displayName);
            label.anchor = TextAnchor.MiddleCenter;
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return zone;
        }

        private static string Quadrant(RectInt region)
        {
            Vector3 center = RectCenter(region);
            return (center.z >= 0f ? "N" : "S") + (center.x >= 0f ? "E" : "W");
        }

        private static NavGraph CreateGraph(int obstacleMask)
        {
            var graphObject = new GameObject("NavGraph");
            NavGraph graph = graphObject.AddComponent<NavGraph>();
            Configure(graph, so => so.FindProperty("obstacleMask").intValue = obstacleMask);

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (!IsNodeCell(col, row))
                        continue;

                    var node = new GameObject($"Node_{col}_{row}");
                    node.transform.SetParent(graphObject.transform);
                    node.transform.position = CellToWorld(col, row) + Vector3.up * AgentHeight;
                    node.AddComponent<NavNode>();
                }
            }

            graph.Bake();
            return graph;
        }

        private static HunterController CreateHunter(GameObject prefab, LevelZones zones, NavGraph graph)
        {
            var hunter = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            hunter.transform.position = zones.Start.Center + Vector3.up * AgentHeight;

            Configure(hunter.GetComponent<HunterNavigator>(), so =>
            {
                so.FindProperty("huntingGround").objectReferenceValue = zones.HuntingGround;
                so.FindProperty("exitZone").objectReferenceValue = zones.Exit;
                SetArray(so.FindProperty("safeZones"), zones.Safe);
            });
            Configure(hunter.GetComponent<HunterLife>(), so => so.FindProperty("startZone").objectReferenceValue = zones.Start);
            Configure(hunter.GetComponent<POISpawner>(), so => so.FindProperty("arenaBounds").objectReferenceValue = zones.Arena);
            Configure(hunter.GetComponent<PathFollower>(), so => so.FindProperty("graph").objectReferenceValue = graph);

            return hunter.GetComponent<HunterController>();
        }

        private static void CreateBoids(GameObject prefab, ArenaBounds arena, HunterController hunter)
        {
            Transform root = new GameObject("Boids").transform;

            for (int i = 0; i < BoidCount; i++)
            {
                var boid = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                boid.name = $"Boid_{i}";

                Vector3 position = arena.GetRandomPoint();
                position.y = BoidHeight;
                boid.transform.position = position;

                Configure(boid.GetComponent<BoidAgent>(), so =>
                {
                    so.FindProperty("arenaBounds").objectReferenceValue = arena;
                    so.FindProperty("hunter").objectReferenceValue = hunter;
                });
                Configure(boid.GetComponent<BoidHealth>(), so => so.FindProperty("arenaBounds").objectReferenceValue = arena);
            }
        }

        private static void CreateWolves(NavGraph graph, HunterController hunter, int obstacleMask, int agentLayer, int collisionMask,
            Material lineMaterial)
        {
            Transform wolvesRoot = new GameObject("Wolves").transform;
            Transform waypointsRoot = new GameObject("WolfWaypoints").transform;

            foreach (WolfRoute route in WolfRoutes)
            {
                Transform[] waypoints = CreateWaypoints(route, waypointsRoot);
                GameObject wolf = CreateWolf(route, waypoints, graph, hunter, obstacleMask, lineMaterial, wolvesRoot);
                MakeSolid(wolf, agentLayer, collisionMask);
            }
        }

        private static Transform[] CreateWaypoints(WolfRoute route, Transform parent)
        {
            Transform routeRoot = new GameObject("Route_" + route.Name).transform;
            routeRoot.SetParent(parent);

            var waypoints = new Transform[route.Cells.Length];
            for (int i = 0; i < route.Cells.Length; i++)
            {
                Transform waypoint = new GameObject($"WP_{route.Name}_{i}").transform;
                waypoint.SetParent(routeRoot);
                waypoint.position = CellToWorld(route.Cells[i].x, route.Cells[i].y) + Vector3.up * AgentHeight;
                waypoints[i] = waypoint;
            }

            return waypoints;
        }

        private static GameObject CreateWolf(WolfRoute route, Transform[] waypoints, NavGraph graph, HunterController hunter, int obstacleMask,
            Material lineMaterial, Transform parent)
        {
            GameObject wolf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            wolf.name = "Wolf_" + route.Name;
            wolf.transform.SetParent(parent);
            wolf.transform.position = waypoints[0].position;

            Renderer body = wolf.GetComponent<Renderer>();
            body.sharedMaterial = GetOrCreateMaterial(LevelMaterialsFolder, "Mat_Wolf", LitShaderName, Color.white);

            Configure(wolf.AddComponent<SteeringMotor>(), so => so.FindProperty("mass").floatValue = WolfMass);
            Configure(wolf.AddComponent<PathFollower>(), so =>
            {
                so.FindProperty("graph").objectReferenceValue = graph;
                so.FindProperty("avoidSafeZones").boolValue = true;
                so.FindProperty("pathLine").objectReferenceValue = GetOrCreateLine(wolf.transform, "PathLine", WolfPathColor, lineMaterial);
            });
            Configure(wolf.AddComponent<FieldOfView>(), so =>
            {
                so.FindProperty("viewRadius").floatValue = WolfViewRadius;
                so.FindProperty("viewAngle").floatValue = WolfViewAngle;
                so.FindProperty("obstacleMask").intValue = obstacleMask;
            });

            Configure(wolf.AddComponent<WaypointRoute>(), so =>
            {
                SetArray(so.FindProperty("waypoints"), waypoints);
                so.FindProperty("traversalMode").enumValueIndex = (int)route.Mode;
            });
            Configure(wolf.AddComponent<WolfAwareness>(), so => so.FindProperty("hunter").objectReferenceValue = hunter);
            WolfController controller = wolf.AddComponent<WolfController>();

            AddWolfFeedback(wolf, controller, body, lineMaterial);
            return wolf;
        }

        private static void AddWolfFeedback(GameObject wolf, WolfController controller, Renderer body, Material lineMaterial)
        {
            TextMesh label = CreateLabel(wolf.transform, Vector3.up * LabelHeight, "Patrol");
            label.gameObject.AddComponent<Billboard>();

            LineRenderer ring = GetOrCreateLine(wolf.transform, "SignalRing", Color.white, lineMaterial);
            ring.transform.localPosition = Vector3.up * RingHeightOffset;
            ring.useWorldSpace = false;
            ring.loop = true;

            Transform marker = CreateLastKnownMarker(wolf.transform);

            Configure(wolf.AddComponent<WolfVisualFeedback>(), so =>
            {
                so.FindProperty("wolf").objectReferenceValue = controller;
                so.FindProperty("bodyRenderer").objectReferenceValue = body;
                so.FindProperty("label").objectReferenceValue = label;
                so.FindProperty("signalRing").objectReferenceValue = ring;
                so.FindProperty("lastKnownMarker").objectReferenceValue = marker;
            });
        }

        private static Transform CreateLastKnownMarker(Transform parent)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "LastKnownMarker";
            marker.transform.SetParent(parent, false);
            marker.transform.localScale = new Vector3(MarkerDiameter, MarkerThickness, MarkerDiameter);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(LevelMaterialsFolder, "Mat_LastKnownPosition", LitShaderName, MarkerColor);
            marker.SetActive(false);
            return marker.transform;
        }
    }
}
