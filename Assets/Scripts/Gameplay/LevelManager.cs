using System.Collections.Generic;
using UnityEngine;

namespace MobRush
{
    public class LevelManager : MonoBehaviour
    {
        private static LevelManager _instance;
        public static LevelManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<LevelManager>();
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Prefabs & Templates")]
        public GameObject gatePrefab;
        public GameObject hazardPrefab;
        public BaseController enemyBase;
        public EnemyCrowdSpawner enemySpawner;
        public CannonController cannon;
        public PlayerDefenseLine defenseLine;
        public Transform trackFloor;
        public Transform leftWall;
        public Transform rightWall;

        private readonly List<GameObject> activeLevelObjects = new List<GameObject>();
        public LevelConfig CurrentConfig { get; private set; }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }

        private void EnsureReferences()
        {
            if (cannon == null) cannon = FindAnyObjectByType<CannonController>();
            if (defenseLine == null) defenseLine = FindAnyObjectByType<PlayerDefenseLine>();
            if (enemyBase == null) enemyBase = FindAnyObjectByType<BaseController>();
            if (enemySpawner == null) enemySpawner = FindAnyObjectByType<EnemyCrowdSpawner>();
            if (gatePrefab == null) gatePrefab = GameObject.Find("Gate_Prefab");
            if (hazardPrefab == null) hazardPrefab = GameObject.Find("Hazard_Prefab");
            if (trackFloor == null)
            {
                GameObject track = GameObject.Find("Track_Floor");
                if (track != null) trackFloor = track.transform;
            }
            if (leftWall == null)
            {
                GameObject lw = GameObject.Find("Wall_Left");
                if (lw != null) leftWall = lw.transform;
            }
            if (rightWall == null)
            {
                GameObject rw = GameObject.Find("Wall_Right");
                if (rw != null) rightWall = rw.transform;
            }
        }

        public void LoadLevel(int levelId)
        {
            EnsureReferences();
            ClearCurrentLevel();

            CurrentConfig = LevelDatabase.GetLevel(levelId);

            // Apply dynamic visual biome & aesthetic theme (Neon Cyber, Sunset Synthwave, Emerald Horizon)
            ApplyAestheticTheme(levelId);

            // 1. Adjust Track Length
            if (trackFloor != null)
            {
                float len = CurrentConfig.trackLength;
                trackFloor.localScale = new Vector3(trackFloor.localScale.x, trackFloor.localScale.y, len);
                trackFloor.position = new Vector3(0f, -0.5f, len * 0.5f - 2f);

                if (leftWall != null)
                {
                    leftWall.localScale = new Vector3(leftWall.localScale.x, leftWall.localScale.y, len);
                    leftWall.position = new Vector3(leftWall.position.x, leftWall.position.y, len * 0.5f - 2f);
                }

                if (rightWall != null)
                {
                    rightWall.localScale = new Vector3(rightWall.localScale.x, rightWall.localScale.y, len);
                    rightWall.position = new Vector3(rightWall.position.x, rightWall.position.y, len * 0.5f - 2f);
                }
            }

            // 2. Position Enemy Base at end of track
            float baseZ = CurrentConfig.trackLength - 4.5f;
            if (enemyBase != null)
            {
                Transform targetRoot = (enemyBase.modelRoot != null) ? enemyBase.modelRoot : enemyBase.transform;
                targetRoot.position = new Vector3(0f, 0f, baseZ);

                // Note: CurrentConfig.baseHealth already has difficulty applied via LevelDatabase.GetLevel()
                enemyBase.Initialize(CurrentConfig.baseHealth);
            }

            // 3. Configure Enemy Spawner
            if (enemySpawner != null)
            {
                enemySpawner.transform.position = new Vector3(0f, 0.5f, baseZ - 2.5f);
                enemySpawner.StartSpawning(
                    CurrentConfig.totalEnemiesToSpawn,
                    CurrentConfig.burstSize,
                    CurrentConfig.burstInterval,
                    CurrentConfig.burstDuration,
                    CurrentConfig.enableContinuousFire,
                    CurrentConfig.enemyContinuousRate
                );
            }

            // 4. Spawn Gates
            if (gatePrefab != null)
            {
                foreach (var gConfig in CurrentConfig.gates)
                {
                    GameObject gObj = Instantiate(gatePrefab, gConfig.position, Quaternion.identity);
                    gObj.SetActive(true);
                    Gate gateComp = gObj.GetComponentInChildren<Gate>();
                    if (gateComp != null)
                    {
                        gateComp.affectsTeam = gConfig.affectsTeam;
                        gateComp.Setup(gConfig.type, gConfig.value, gConfig.isMoving, gConfig.moveRange, gConfig.moveSpeed);
                    }
                    activeLevelObjects.Add(gObj);
                }
            }

            // 5. Spawn Hazards
            if (hazardPrefab != null)
            {
                foreach (var hConfig in CurrentConfig.hazards)
                {
                    GameObject hObj = Instantiate(hazardPrefab, hConfig.position, Quaternion.identity);
                    hObj.SetActive(true);
                    hObj.transform.localScale = hConfig.scale;
                    Hazard hazComp = hObj.GetComponent<Hazard>();
                    if (hazComp != null)
                    {
                        hazComp.affectsTeam = hConfig.affectsTeam;
                        hazComp.isRotating = hConfig.isRotating;
                        hazComp.isOscillating = hConfig.isOscillating;
                        hazComp.speed = hConfig.speed;
                        hazComp.range = hConfig.range;
                    }
                    activeLevelObjects.Add(hObj);
                }
            }

            // 6. Reset Player Cannon & Defense Line
            if (cannon != null)
            {
                cannon.ResetPosition();
                cannon.ResetHealth(CurrentConfig.playerCannonHealth);
                cannon.isFiringActive = true;
                cannon.SpawnStartingCrowdBurst();
            }

            if (defenseLine != null)
            {
                defenseLine.ResetLine();
            }
        }

        public void ClearCurrentLevel()
        {
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ClearActiveUnits();
            }

            if (enemySpawner != null)
            {
                enemySpawner.StopSpawning();
            }

            foreach (var obj in activeLevelObjects)
            {
                if (obj != null) Destroy(obj);
            }
            activeLevelObjects.Clear();
        }

        private void ApplyAestheticTheme(int levelId)
        {
            int themeIndex = (levelId - 1) % 3;

            Color floorColor;
            Color curbGlowColor;
            Color wallColor;
            Color skyColor;

            switch (themeIndex)
            {
                case 0:
                    // Theme 1: Neon Cyber Tokyo (Midnight Obsidian & Vivid Electric Cyan)
                    floorColor = new Color(0.09f, 0.11f, 0.16f);
                    curbGlowColor = new Color(0.0f, 0.95f, 1.0f);
                    wallColor = new Color(0.13f, 0.15f, 0.22f);
                    skyColor = new Color(0.06f, 0.08f, 0.12f);
                    break;

                case 1:
                    // Theme 2: Sunset Synthwave (Deep Violet Asphalt & Radiant Amber Gold)
                    floorColor = new Color(0.14f, 0.10f, 0.19f);
                    curbGlowColor = new Color(1.0f, 0.72f, 0.05f);
                    wallColor = new Color(0.20f, 0.14f, 0.28f);
                    skyColor = new Color(0.10f, 0.07f, 0.15f);
                    break;

                default:
                    // Theme 3: Emerald Horizon (Deep Slate Basalt & Radiant Mint Lime)
                    floorColor = new Color(0.08f, 0.13f, 0.11f);
                    curbGlowColor = new Color(0.1f, 1.0f, 0.45f);
                    wallColor = new Color(0.12f, 0.19f, 0.16f);
                    skyColor = new Color(0.05f, 0.09f, 0.08f);
                    break;
            }

            if (trackFloor != null)
            {
                Renderer r = trackFloor.GetComponent<Renderer>();
                if (r != null) r.material.color = floorColor;
            }

            if (leftWall != null)
            {
                Renderer r = leftWall.GetComponent<Renderer>();
                if (r != null) r.material.color = wallColor;
            }

            if (rightWall != null)
            {
                Renderer r = rightWall.GetComponent<Renderer>();
                if (r != null) r.material.color = wallColor;
            }

            GameObject curbL = GameObject.Find("Curb_L");
            if (curbL != null)
            {
                Renderer r = curbL.GetComponent<Renderer>();
                if (r != null) r.material.color = curbGlowColor;
            }

            GameObject curbR = GameObject.Find("Curb_R");
            if (curbR != null)
            {
                Renderer r = curbR.GetComponent<Renderer>();
                if (r != null) r.material.color = curbGlowColor;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = skyColor;
            }
        }
    }
}
