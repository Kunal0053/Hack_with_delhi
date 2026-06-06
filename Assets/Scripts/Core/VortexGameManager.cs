using System.Collections.Generic;
using UnityEngine;
using VortexGame.AI;
using VortexGame.Input;
using VortexGame.UI;

namespace VortexGame.Core
{
    public sealed class VortexGameManager : MonoBehaviour
    {
        private readonly List<VortexEntity> vortexes = new List<VortexEntity>(8);
        private readonly List<WorldObject> activeObjects = new List<WorldObject>(160);
        private readonly List<WorldObject> inactiveObjects = new List<WorldObject>(160);
        private readonly List<CaptureZone> zones = new List<CaptureZone>(4);
        private readonly List<VortexAIController> aiControllers = new List<VortexAIController>(4);

        private MobileHud hud;
        private Camera mainCamera;
        private float spawnTimer;
        private float matchTimer;
        private float survivalRamp;
        private GameModeType mode = GameModeType.TimedCollection;
        private bool initialized;

        public IReadOnlyList<VortexEntity> Vortexes => vortexes;
        public IReadOnlyList<WorldObject> ActiveObjects => activeObjects;
        public float ArenaRadius => 22f;
        public float AbsorbDelay => 1.9f;
        public string ModeLabel => mode switch
        {
            GameModeType.TimedCollection => "Timed Collection",
            GameModeType.Survival => "Survival",
            _ => "Zone Control"
        };
        public string TimeRemainingLabel => mode == GameModeType.ZoneControl ? "Pressure wins" : $"{Mathf.CeilToInt(matchTimer)}s";

        private void Awake()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            BuildRuntime();
        }

        private void Update()
        {
            if (!initialized || vortexes.Count == 0)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            spawnTimer -= deltaTime;
            matchTimer -= deltaTime;
            survivalRamp += deltaTime;

            for (int i = 0; i < vortexes.Count; i++)
            {
                vortexes[i].Tick(deltaTime, this);
            }

            for (int i = 0; i < aiControllers.Count; i++)
            {
                aiControllers[i].Tick(deltaTime, this);
            }

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                WorldObject worldObject = activeObjects[i];
                worldObject.Tick(deltaTime, this);
                if (worldObject.State == WorldObjectState.Inactive)
                {
                    activeObjects.RemoveAt(i);
                    inactiveObjects.Add(worldObject);
                }
            }

            for (int i = 0; i < zones.Count; i++)
            {
                zones[i].Tick(deltaTime, this);
            }

            if (spawnTimer <= 0f)
            {
                spawnTimer = mode == GameModeType.Survival ? Mathf.Max(0.18f, 0.65f - survivalRamp * 0.01f) : 0.45f;
                SpawnObject();
            }

            if (mode != GameModeType.ZoneControl && matchTimer <= 0f)
            {
                AdvanceMode();
            }
        }

        public void AdvanceMode()
        {
            mode = (GameModeType)(((int)mode + 1) % 3);
            survivalRamp = 0f;

            foreach (VortexEntity vortex in vortexes)
            {
                vortex.ResetZoneScore();
            }

            matchTimer = mode == GameModeType.ZoneControl ? 999f : 90f;
        }

        public WorldObject FindNearestFreeObject(Vector3 origin)
        {
            WorldObject nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < activeObjects.Count; i++)
            {
                WorldObject candidate = activeObjects[i];
                if (candidate.State != WorldObjectState.Free)
                {
                    continue;
                }

                float distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private void BuildRuntime()
        {
            ClearScene();
            CreateArena();
            CreateCamera();
            CreateHud();
            CreateVortexes();
            CreateZones();
            CreatePool(100);
            SpawnInitialObjects(36);
            matchTimer = 90f;
        }

        private void CreateArena()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(transform, false);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(4.4f, 1f, 4.4f);
            floor.GetComponent<Renderer>().material.color = new Color(0.05f, 0.08f, 0.14f);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ArenaRing";
            ring.transform.SetParent(transform, false);
            ring.transform.position = new Vector3(0f, -0.45f, 0f);
            ring.transform.localScale = new Vector3(ArenaRadius * 2f, 0.05f, ArenaRadius * 2f);
            ring.GetComponent<Renderer>().material.color = new Color(0.08f, 0.4f, 0.55f);

            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.GetComponent<Camera>();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.015f, 0.02f, 0.045f);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
        }

        private void CreateHud()
        {
            hud = MobileHud.Create(this);
            hud.transform.SetParent(transform, false);
        }

        private void CreateVortexes()
        {
            VortexEntity player = CreateVortex("Player", new Vector3(0f, 0.5f, -4f), new Color(0.2f, 0.95f, 1f), true, 34f);
            hud.BindPlayer(player);

            PlayerVortexInput playerInput = player.gameObject.AddComponent<PlayerVortexInput>();
            playerInput.Initialize(player, hud.Joystick);
            hud.BoostButton.HoldChanged += player.SetBoost;
            hud.ShieldButton.HoldChanged += player.SetShield;
            hud.ShockButton.Clicked += () => player.ActivateShockwave(this);
            hud.ReleaseButton.Clicked += player.ReleaseOrbitingObjects;
            hud.ModeButton.Clicked += player.ToggleMode;
            hud.MatchButton.Clicked += AdvanceMode;

            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
            follow.Target = player.transform;

            CreateAI("Rift Lynx", new Vector3(10f, 0.5f, 8f), new Color(1f, 0.45f, 0.2f), 28f);
            CreateAI("Nova Eel", new Vector3(-12f, 0.5f, 3f), new Color(1f, 0.3f, 0.55f), 24f);
            CreateAI("Pulse Manta", new Vector3(7f, 0.5f, -10f), new Color(0.55f, 1f, 0.35f), 32f);
        }

        private void CreateZones()
        {
            CreateZone(new Vector3(-12f, 0.02f, -10f));
            CreateZone(new Vector3(13f, 0.02f, -2f));
            CreateZone(new Vector3(-2f, 0.02f, 12f));
        }

        private void CreatePool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject objectRoot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                objectRoot.name = $"WorldObject_{i:000}";
                objectRoot.transform.SetParent(transform, false);
                objectRoot.transform.position = new Vector3(0f, 0.5f, 0f);
                WorldObject pooledObject = objectRoot.AddComponent<WorldObject>();
                pooledObject.Initialize(objectRoot.GetComponent<Renderer>());
                inactiveObjects.Add(pooledObject);
            }
        }

        private void SpawnInitialObjects(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnObject();
            }
        }

        private void SpawnObject()
        {
            if (inactiveObjects.Count == 0)
            {
                return;
            }

            WorldObject worldObject = inactiveObjects[inactiveObjects.Count - 1];
            inactiveObjects.RemoveAt(inactiveObjects.Count - 1);

            Vector2 circle = Random.insideUnitCircle * (ArenaRadius - 2f);
            float mass = Random.Range(1f, mode == GameModeType.Survival ? 11f : 8f);
            float energyValue = Mathf.Lerp(3f, 15f, Mathf.InverseLerp(1f, 11f, mass));
            Color color = Color.Lerp(new Color(0.9f, 0.95f, 1f), new Color(1f, 0.8f, 0.2f), Mathf.InverseLerp(1f, 11f, mass));
            worldObject.Activate(new Vector3(circle.x, 0.5f, circle.y), mass, energyValue, color);
            activeObjects.Add(worldObject);
        }

        private VortexEntity CreateVortex(string displayName, Vector3 position, Color color, bool isPlayer, float startingEnergy)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = displayName;
            root.transform.SetParent(transform, false);
            root.transform.position = position;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            core.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            core.GetComponent<Collider>().enabled = false;

            VortexEntity entity = root.AddComponent<VortexEntity>();
            entity.Initialize(displayName, color, isPlayer, startingEnergy);
            vortexes.Add(entity);
            return entity;
        }

        private void CreateAI(string displayName, Vector3 position, Color color, float startingEnergy)
        {
            VortexEntity entity = CreateVortex(displayName, position, color, false, startingEnergy);
            VortexAIController ai = entity.gameObject.AddComponent<VortexAIController>();
            ai.Initialize(entity);
            aiControllers.Add(ai);
        }

        private void CreateZone(Vector3 position)
        {
            GameObject zoneObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            zoneObject.name = "CaptureZone";
            zoneObject.transform.SetParent(transform, false);
            zoneObject.transform.position = position;
            CaptureZone zone = zoneObject.AddComponent<CaptureZone>();
            zone.Initialize(4f);
            zones.Add(zone);
        }

        private void ClearScene()
        {
            vortexes.Clear();
            aiControllers.Clear();
            activeObjects.Clear();
            inactiveObjects.Clear();
            zones.Clear();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
