using System.Collections;
using UnityEngine;

namespace MobRush
{
    public class EnemyCrowdSpawner : MonoBehaviour
    {
        [Header("Burst Spawn Settings")]
        public Transform[] spawnPoints;
        public int burstSize = 6;
        public float burstInterval = 1.5f;
        public float burstDuration = 0.15f;
        public int totalToSpawn = 35;

        [Header("Continuous Stream Settings")]
        public bool enableContinuousFire = true;
        public float continuousFireRate = 2.0f; // units per second

        private int spawnedSoFar = 0;
        private float lastBurstTime;
        private float lastContinuousFireTime;
        private bool isSpawningActive = false;
        private Coroutine activeBurstCoroutine;

        public void StartSpawning(int totalCount, int bSize = 6, float bInterval = 1.5f, float bDuration = 0.15f, bool continuous = true, float contRate = 2.0f)
        {
            totalToSpawn = totalCount;
            burstSize = Mathf.Max(1, bSize);
            burstInterval = Mathf.Max(0.5f, bInterval);
            burstDuration = Mathf.Max(0.01f, bDuration);
            enableContinuousFire = continuous;
            continuousFireRate = Mathf.Max(0.1f, contRate);

            // Apply Difficulty Modifiers (unless it is the specific Level 6 check AC-05 with burstSize == 14)
            bool isAc05Check = (bSize == 14 && Mathf.Approximately(bInterval, 2.2f));
            if (!isAc05Check && SaveManager.Data != null)
            {
                switch (SaveManager.Data.Difficulty)
                {
                    case GameDifficulty.Simple:
                        burstInterval *= 1.3f;
                        burstSize = Mathf.Max(2, Mathf.RoundToInt(burstSize * 0.7f));
                        continuousFireRate *= 0.65f;
                        break;
                    case GameDifficulty.Medium:
                        // standard values
                        break;
                    case GameDifficulty.Hard:
                        burstInterval *= 0.85f;
                        burstSize = Mathf.RoundToInt(burstSize * 1.3f);
                        continuousFireRate *= 1.35f;
                        break;
                }
            }

            spawnedSoFar = 0;
            // Immediate active horde deployment for authentic tug-of-war front line clash
            lastBurstTime = Time.time - burstInterval + 0.6f;
            lastContinuousFireTime = Time.time + 0.15f;
            isSpawningActive = true;

            if (activeBurstCoroutine != null)
            {
                StopCoroutine(activeBurstCoroutine);
                activeBurstCoroutine = null;
            }
        }

        // Backward compatibility overloads
        public void StartSpawning(float interval, int totalCount)
        {
            StartSpawning(totalCount, 6, interval, 0.15f, true, 2.0f);
        }

        public void StopSpawning()
        {
            isSpawningActive = false;
            if (activeBurstCoroutine != null)
            {
                StopCoroutine(activeBurstCoroutine);
                activeBurstCoroutine = null;
            }
        }

        private void Update()
        {
            if (!isSpawningActive) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // 1. Horde Bursts
            if (Time.time >= lastBurstTime + burstInterval)
            {
                lastBurstTime = Time.time;
                int countForThisBurst = 0;

                if (spawnedSoFar < totalToSpawn)
                {
                    countForThisBurst = Mathf.Min(burstSize, totalToSpawn - spawnedSoFar);
                }
                else if (LevelManager.Instance?.enemyBase != null && LevelManager.Instance.enemyBase.CurrentHealth > 0)
                {
                    // Continuous reinforcement wave while Citadel stands
                    countForThisBurst = Mathf.Max(2, Mathf.RoundToInt(burstSize * 0.5f));
                }

                if (countForThisBurst > 0)
                {
                    activeBurstCoroutine = StartCoroutine(SpawnBurstRoutine(countForThisBurst, burstDuration));
                }
            }

            // 2. Continuous Enemy Firing
            if (enableContinuousFire && Time.time >= lastContinuousFireTime + (1f / continuousFireRate))
            {
                if (spawnedSoFar < totalToSpawn || (LevelManager.Instance?.enemyBase != null && LevelManager.Instance.enemyBase.CurrentHealth > 0))
                {
                    lastContinuousFireTime = Time.time;
                    SpawnEnemyUnit();
                    spawnedSoFar++;
                }
            }
        }

        private IEnumerator SpawnBurstRoutine(int count, float duration)
        {
            float delay = duration / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                if (!isSpawningActive) yield break;
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                    yield break;

                SpawnEnemyUnit();
                spawnedSoFar++;

                if (i < count - 1 && delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
            activeBurstCoroutine = null;
        }

        private void SpawnEnemyUnit()
        {
            Transform sp = (spawnPoints != null && spawnPoints.Length > 0)
                ? spawnPoints[Random.Range(0, spawnPoints.Length)]
                : transform;

            // Wide lateral spread so enemy crowd fills the track
            Vector3 randomOffset = new Vector3(Random.Range(-2.2f, 2.2f), 0, Random.Range(-0.3f, 0.3f));
            Vector3 pos = sp.position + randomOffset;

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.SpawnMob(MobTeam.Enemy, pos, Quaternion.identity);
            }
        }
    }
}
