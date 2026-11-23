using System.Collections.Generic;
using UnityEngine;

namespace MobRush
{
    public class ObjectPooler : MonoBehaviour
    {
        private static ObjectPooler _instance;
        public static ObjectPooler Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<ObjectPooler>();
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Pool Setup")]
        public GameObject mobUnitPrefab;
        public int initialPlayerPoolSize = 250;
        public int initialEnemyPoolSize = 200;

        private readonly Queue<MobUnit> playerPool = new Queue<MobUnit>();
        private readonly Queue<MobUnit> enemyPool = new Queue<MobUnit>();
        private Transform poolContainer;

        private int activePlayerCount = 0;
        private int activeEnemyCount = 0;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                poolContainer = new GameObject("_PoolContainer").transform;
                poolContainer.SetParent(transform);
                if (mobUnitPrefab == null) mobUnitPrefab = GameObject.Find("MobUnit_Prefab");
                PrewarmPools();
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }

        private void PrewarmPools()
        {
            if (mobUnitPrefab == null) return;

            for (int i = 0; i < initialPlayerPoolSize; i++)
            {
                MobUnit unit = CreateNewUnit(MobTeam.Player);
                unit.gameObject.SetActive(false);
                playerPool.Enqueue(unit);
            }

            for (int i = 0; i < initialEnemyPoolSize; i++)
            {
                MobUnit unit = CreateNewUnit(MobTeam.Enemy);
                unit.gameObject.SetActive(false);
                enemyPool.Enqueue(unit);
            }
        }

        private MobUnit CreateNewUnit(MobTeam team)
        {
            if (poolContainer == null) poolContainer = transform;
            GameObject obj = Instantiate(mobUnitPrefab, poolContainer);
            MobUnit unit = obj.GetComponent<MobUnit>();
            if (unit == null) unit = obj.AddComponent<MobUnit>();
            unit.SetTeam(team);
            return unit;
        }

        public MobUnit SpawnMob(MobTeam team, Vector3 position, Quaternion rotation, int initialGateId = -1)
        {
            // Safety cap: Limit maximum living units to guarantee smooth 60fps & zero crash on Android
            int maxCap = (team == MobTeam.Player) ? 350 : 120;
            if (team == MobTeam.Player && activePlayerCount >= maxCap) return null;
            if (team == MobTeam.Enemy && activeEnemyCount >= maxCap) return null;

            Queue<MobUnit> pool = (team == MobTeam.Player) ? playerPool : enemyPool;
            MobUnit unit = null;

            if (pool != null)
            {
                while (pool.Count > 0)
                {
                    var candidate = pool.Dequeue();
                    if (candidate != null && candidate.gameObject != null)
                    {
                        unit = candidate;
                        break;
                    }
                }
            }

            if (unit == null)
            {
                unit = CreateNewUnit(team);
            }

            unit.transform.position = position;
            unit.transform.rotation = rotation;
            unit.SetTeam(team);
            unit.ResetUnit();

            // CRITICAL FIX: Set gateId BEFORE SetActive(true) so Unity PhysX never triggers recursive OnTriggerEnter!
            if (initialGateId != -1)
            {
                unit.SetLastGateId(initialGateId);
            }

            unit.gameObject.SetActive(true);

            if (team == MobTeam.Player) activePlayerCount++;
            else activeEnemyCount++;

            return unit;
        }

        public void ReturnMob(MobUnit unit)
        {
            if (unit == null) return;
            unit.gameObject.SetActive(false);
            if (poolContainer != null)
                unit.transform.SetParent(poolContainer);

            if (unit.team == MobTeam.Player)
            {
                activePlayerCount = Mathf.Max(0, activePlayerCount - 1);
                if (playerPool != null && !playerPool.Contains(unit))
                    playerPool.Enqueue(unit);
            }
            else
            {
                activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
                if (enemyPool != null && !enemyPool.Contains(unit))
                    enemyPool.Enqueue(unit);
            }
        }

        public int GetActivePlayerUnitCount() => activePlayerCount;
        public int GetActiveEnemyUnitCount() => activeEnemyCount;

        public void ClearActiveUnits()
        {
            activePlayerCount = 0;
            activeEnemyCount = 0;

            MobUnit[] activeUnits = FindObjectsByType<MobUnit>(FindObjectsSortMode.None);
            foreach (var u in activeUnits)
            {
                if (u != null && u.gameObject != null && u.gameObject.activeInHierarchy)
                {
                    u.gameObject.SetActive(false);
                    if (poolContainer != null) u.transform.SetParent(poolContainer);
                    if (u.team == MobTeam.Player && playerPool != null && !playerPool.Contains(u)) playerPool.Enqueue(u);
                    else if (u.team == MobTeam.Enemy && enemyPool != null && !enemyPool.Contains(u)) enemyPool.Enqueue(u);
                }
            }
        }
    }
}
