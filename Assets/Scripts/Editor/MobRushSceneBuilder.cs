#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace MobRush.Editor
{
    [InitializeOnLoad]
    public static class MobRushSceneBuilder
    {
        static MobRushSceneBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfEmpty;
        }

        private static void AutoBuildIfEmpty()
        {
            if (GameObject.Find("MobRush_Track") == null)
            {
                BuildCompleteGame();
            }
        }

        [MenuItem("Mob Rush/Build Complete Playable Game")]
        public static void BuildCompleteGame()
        {
            // 0. Clean Up Previous Objects
            string[] cleanupNames = new string[]
            {
                "MobRush_Track", "Track_Floor", "Wall_Left", "Wall_Right", "Curb_Left", "Curb_Right", "Curb_L", "Curb_R",
                "MobUnit_Template", "MobUnit_Prefab", "Gate_Prefab", "Hazard_Prefab", "Player_Cannon",
                "Player_Defense_Line", "Enemy_Base", "Enemy_Spawner", "Enemy_Citadel", "Gate_Multiply_2",
                "Gate_Multiply_3", "Gate_Multiply_5", "GameSystems", "MobRush_CoreSystems"
            };

            var activeScene = EditorSceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.name == "Main Camera" || root.name == "Directional Light" || root.name == "Global Volume")
                    continue;

                foreach (var cName in cleanupNames)
                {
                    if (root.name.StartsWith(cName))
                    {
                        Object.DestroyImmediate(root);
                        break;
                    }
                }
            }

            // 1. Setup Camera (Portrait Follow Framing)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            mainCam.transform.position = new Vector3(0f, 13.0f, -3.0f);
            mainCam.transform.rotation = Quaternion.Euler(53f, 0f, 0f);
            mainCam.fieldOfView = 60f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.11f, 0.14f, 0.19f);

            // 2. Setup Sun Light
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            sun.color = new Color(1f, 0.98f, 0.94f);
            sun.intensity = 1.3f;

            // 3. Create Core Materials
            Material groundMat = GetOrCreateMaterial("Mat_Ground", new Color(0.16f, 0.19f, 0.25f));
            Material wallMat = GetOrCreateMaterial("Mat_Wall", new Color(0.1f, 0.12f, 0.16f));
            Material curbMat = GetOrCreateMaterial("Mat_CurbGlow", new Color(0f, 0.85f, 1f));
            Material playerMat = GetOrCreateMaterial("Mat_PlayerBlue", new Color(0f, 0.65f, 1f));
            Material enemyMat = GetOrCreateMaterial("Mat_EnemyRed", new Color(1f, 0.22f, 0.26f));
            Material gateGlassMat = GetOrCreateMaterial("Mat_GateGlass", new Color(0.1f, 1f, 0.45f, 0.7f), true);
            Material gatePillarMat = GetOrCreateMaterial("Mat_GatePillar", new Color(0.14f, 0.17f, 0.23f));
            Material towerMat = GetOrCreateMaterial("Mat_CitadelRed", new Color(0.85f, 0.12f, 0.25f));
            Material cannonMetalMat = GetOrCreateMaterial("Mat_CannonMetal", new Color(0.2f, 0.24f, 0.32f));
            Material hazardMat = GetOrCreateMaterial("Mat_HazardOrange", new Color(1f, 0.55f, 0.1f));

            // 4. Create Track Floor & Walls
            GameObject trackRoot = new GameObject("MobRush_Track");
            trackRoot.transform.position = Vector3.zero;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Track_Floor";
            ground.transform.parent = trackRoot.transform;
            ground.transform.position = new Vector3(0f, -0.5f, 10f);
            ground.transform.localScale = new Vector3(9f, 1f, 25f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Wall_Left";
            leftWall.transform.parent = trackRoot.transform;
            leftWall.transform.position = new Vector3(-4.7f, 0.6f, 10f);
            leftWall.transform.localScale = new Vector3(0.4f, 1.2f, 25f);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Wall_Right";
            rightWall.transform.parent = trackRoot.transform;
            rightWall.transform.position = new Vector3(4.7f, 0.6f, 10f);
            rightWall.transform.localScale = new Vector3(0.4f, 1.2f, 25f);
            rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Glowing Curbs
            GameObject curbL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbL.name = "Curb_L";
            curbL.transform.parent = trackRoot.transform;
            curbL.transform.position = new Vector3(-4.45f, 0.05f, 10f);
            curbL.transform.localScale = new Vector3(0.15f, 0.1f, 25f);
            curbL.GetComponent<Renderer>().sharedMaterial = curbMat;

            GameObject curbR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbR.name = "Curb_R";
            curbR.transform.parent = trackRoot.transform;
            curbR.transform.position = new Vector3(4.45f, 0.05f, 10f);
            curbR.transform.localScale = new Vector3(0.15f, 0.1f, 25f);
            curbR.GetComponent<Renderer>().sharedMaterial = curbMat;

            // 5. Create Mob Prefab Template
            GameObject mobTemplate = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mobTemplate.name = "MobUnit_Prefab";
            mobTemplate.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            mobTemplate.transform.position = new Vector3(0f, -100f, 0f);
            
            // Set trigger to prevent hard PhysX solver contacts
            CapsuleCollider capCol = mobTemplate.GetComponent<CapsuleCollider>();
            if (capCol != null) capCol.isTrigger = true;

            Rigidbody templateRb = mobTemplate.GetComponent<Rigidbody>();
            if (templateRb == null) templateRb = mobTemplate.AddComponent<Rigidbody>();
            templateRb.useGravity = false;
            templateRb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            templateRb.interpolation = RigidbodyInterpolation.None;
            templateRb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            MobUnit mobComp = mobTemplate.AddComponent<MobUnit>();
            mobComp.playerMaterial = playerMat;
            mobComp.enemyMaterial = enemyMat;
            mobComp.bodyRenderer = mobTemplate.GetComponent<Renderer>();
            mobTemplate.SetActive(false);

            // 6. Create Gate Prefab Template
            GameObject gatePrefab = CreateGatePrefab(gatePillarMat, gateGlassMat);
            gatePrefab.SetActive(false);

            // 7. Create Hazard Prefab Template
            GameObject hazardPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazardPrefab.name = "Hazard_Prefab";
            hazardPrefab.transform.position = new Vector3(0f, -100f, 0f);
            hazardPrefab.transform.localScale = new Vector3(3f, 0.8f, 0.5f);
            hazardPrefab.GetComponent<Renderer>().sharedMaterial = hazardMat;
            hazardPrefab.AddComponent<Hazard>();
            hazardPrefab.SetActive(false);

            // 8. Player Cannon Setup
            GameObject cannonObj = new GameObject("Player_Cannon");
            cannonObj.transform.position = new Vector3(0f, 0.5f, -1.5f);
            CannonController cannon = cannonObj.AddComponent<CannonController>();
            cannon.xLimit = 3.6f;

            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chassis.name = "Chassis";
            chassis.transform.parent = cannonObj.transform;
            chassis.transform.localPosition = Vector3.zero;
            chassis.transform.localScale = new Vector3(1.3f, 0.25f, 1.3f);
            chassis.GetComponent<Renderer>().sharedMaterial = cannonMetalMat;

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "BarrelVisual";
            barrel.transform.parent = cannonObj.transform;
            barrel.transform.localPosition = new Vector3(0f, 0.25f, 0.7f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.65f, 0.7f, 0.65f);
            barrel.GetComponent<Renderer>().sharedMaterial = playerMat;
            cannon.barrelVisual = barrel.transform;

            GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            muzzle.name = "MuzzleRing";
            muzzle.transform.parent = barrel.transform;
            muzzle.transform.localPosition = new Vector3(0f, 1f, 0f);
            muzzle.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
            muzzle.GetComponent<Renderer>().sharedMaterial = curbMat;

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.parent = cannonObj.transform;
            firePoint.transform.localPosition = new Vector3(0f, 0.25f, 1.6f);
            cannon.firePoint = firePoint.transform;

            // Player Defense Line
            GameObject defenseObj = new GameObject("Player_Defense_Line");
            defenseObj.transform.position = new Vector3(0f, 0.5f, -3.2f);
            BoxCollider defCol = defenseObj.AddComponent<BoxCollider>();
            defCol.size = new Vector3(10f, 2f, 1f);
            defCol.isTrigger = true;
            PlayerDefenseLine defenseLine = defenseObj.AddComponent<PlayerDefenseLine>();

            // 9. Enemy Base (Citadel)
            GameObject baseObj = new GameObject("Enemy_Base");
            baseObj.transform.position = new Vector3(0f, 0f, 18f);

            GameObject baseModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseModel.name = "Model_Base";
            baseModel.transform.parent = baseObj.transform;
            baseModel.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            baseModel.transform.localScale = new Vector3(4f, 3f, 3.5f);
            baseModel.GetComponent<Renderer>().sharedMaterial = towerMat;

            GameObject baseTurret = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseTurret.name = "Model_Turret";
            baseTurret.transform.parent = baseObj.transform;
            baseTurret.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            baseTurret.transform.localScale = new Vector3(2.2f, 1f, 2.2f);
            baseTurret.GetComponent<Renderer>().sharedMaterial = wallMat;

            BaseController baseCtrl = baseModel.AddComponent<BaseController>();
            baseModel.GetComponent<BoxCollider>().isTrigger = true;
            baseCtrl.modelRoot = baseObj.transform;

            // Floating HP UI
            GameObject hpTextObj = new GameObject("HP_Display");
            hpTextObj.transform.parent = baseObj.transform;
            hpTextObj.transform.localPosition = new Vector3(0f, 4.8f, 0f);
            hpTextObj.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            TextMeshPro hpText = hpTextObj.AddComponent<TextMeshPro>();
            hpText.text = "100 / 100";
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.fontSize = 8;
            hpText.fontStyle = FontStyles.Bold;
            hpText.color = Color.white;
            baseCtrl.healthText = hpText;

            // 10. Enemy Spawner
            GameObject spawnerObj = new GameObject("Enemy_Spawner");
            spawnerObj.transform.position = new Vector3(0f, 0.5f, 15.5f);
            EnemyCrowdSpawner spawner = spawnerObj.AddComponent<EnemyCrowdSpawner>();
            spawner.spawnPoints = new Transform[] { spawnerObj.transform };

            // 11. Core Systems & Managers
            GameObject systemsObj = new GameObject("MobRush_CoreSystems");
            GameManager gm = systemsObj.AddComponent<GameManager>();
            LevelManager lm = systemsObj.AddComponent<LevelManager>();
            UIManager ui = systemsObj.AddComponent<UIManager>();
            AudioManager audioMgr = systemsObj.AddComponent<AudioManager>();
            ObjectPooler pooler = systemsObj.AddComponent<ObjectPooler>();

            pooler.mobUnitPrefab = mobTemplate;

            lm.cannon = cannon;
            lm.defenseLine = defenseLine;
            lm.enemyBase = baseCtrl;
            lm.enemySpawner = spawner;
            lm.gatePrefab = gatePrefab;
            lm.hazardPrefab = hazardPrefab;
            lm.trackFloor = ground.transform;
            lm.leftWall = leftWall.transform;
            lm.rightWall = rightWall.transform;

            gm.levelManager = lm;
            gm.uiManager = ui;

            // Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[MobRush] Complete Game Scene Built and Saved successfully!");
        }

        private static GameObject CreateGatePrefab(Material pillarMat, Material glassMat)
        {
            GameObject gateRoot = new GameObject("Gate_Prefab");
            gateRoot.transform.position = new Vector3(0f, -100f, 0f);

            GameObject leftP = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftP.name = "Pillar_Left";
            leftP.transform.parent = gateRoot.transform;
            leftP.transform.localPosition = new Vector3(-1.4f, 1f, 0f);
            leftP.transform.localScale = new Vector3(0.25f, 1f, 0.25f);
            leftP.GetComponent<Renderer>().sharedMaterial = pillarMat;

            GameObject rightP = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightP.name = "Pillar_Right";
            rightP.transform.parent = gateRoot.transform;
            rightP.transform.localPosition = new Vector3(1.4f, 1f, 0f);
            rightP.transform.localScale = new Vector3(0.25f, 1f, 0.25f);
            rightP.GetComponent<Renderer>().sharedMaterial = pillarMat;

            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = "TopBar";
            topBar.transform.parent = gateRoot.transform;
            topBar.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            topBar.transform.localScale = new Vector3(3.1f, 0.3f, 0.3f);
            topBar.GetComponent<Renderer>().sharedMaterial = pillarMat;

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "GlassTrigger";
            glass.transform.parent = gateRoot.transform;
            glass.transform.localPosition = new Vector3(0f, 1f, 0f);
            glass.transform.localScale = new Vector3(2.6f, 2f, 0.15f);
            glass.GetComponent<Renderer>().sharedMaterial = glassMat;

            Gate gateComp = glass.AddComponent<Gate>();
            gateComp.glassRenderer = glass.GetComponent<Renderer>();

            GameObject textObj = new GameObject("GateText");
            textObj.transform.parent = glass.transform;
            textObj.transform.localPosition = new Vector3(0f, 0.1f, -0.65f);
            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.text = "x2";
            tm.alignment = TextAlignmentOptions.Center;
            tm.fontSize = 9;
            tm.fontStyle = FontStyles.Bold;
            tm.color = Color.white;
            gateComp.displayText = tm;

            return gateRoot;
        }

        private static Material GetOrCreateMaterial(string name, Color color, bool transparent = false)
        {
            string dir = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            string path = $"{dir}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;

            if (transparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
#endif
