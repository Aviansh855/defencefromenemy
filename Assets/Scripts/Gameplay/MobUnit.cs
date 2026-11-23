using UnityEngine;

namespace MobRush
{
    public enum MobTeam
    {
        Player, // Blue
        Enemy   // Red
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class MobUnit : MonoBehaviour
    {
        [Header("Team & Movement")]
        public MobTeam team = MobTeam.Player;
        public float moveSpeed = 6.8f;
        public float crowdSeparationRadius = 0.55f;
        public float crowdSeparationForce = 3.2f;

        [Header("Combat")]
        public int combatPower = 1;

        [Header("Visuals")]
        public Renderer bodyRenderer;
        public Material playerMaterial;
        public Material enemyMaterial;

        private Rigidbody rb;
        private bool isDead = false;
        private int lastGateId = -1;

        [Header("Juice & Animation")]
        private float animOffset;
        private float baseHeight;

        public bool IsDead => isDead;

        private void Awake()
        {
            animOffset = Random.Range(0f, 10f);
            baseHeight = transform.position.y;

            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            UpdateVisuals();
        }

        public void ResetUnit()
        {
            isDead = false;
            lastGateId = -1;
            transform.localScale = Vector3.one;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Apply unit strength upgrade and move speed
            if (team == MobTeam.Player)
            {
                combatPower = UpgradeConfig.GetUnitStrength(SaveManager.Data.strengthLevel);
                moveSpeed = 7.0f;
            }
            else
            {
                combatPower = 1;
                if (SaveManager.Data != null)
                {
                    switch (SaveManager.Data.Difficulty)
                    {
                        case GameDifficulty.Simple:
                            moveSpeed = 5.0f;
                            break;
                        case GameDifficulty.Medium:
                            moveSpeed = 5.8f;
                            break;
                        case GameDifficulty.Hard:
                            moveSpeed = 6.6f;
                            break;
                    }
                }
                else
                {
                    moveSpeed = 5.8f;
                }
            }

            UpdateVisuals();
        }

        public void SetTeam(MobTeam newTeam)
        {
            team = newTeam;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<Renderer>();

            if (bodyRenderer != null)
            {
                if (team == MobTeam.Player && playerMaterial != null)
                    bodyRenderer.sharedMaterial = playerMaterial;
                else if (team == MobTeam.Enemy && enemyMaterial != null)
                    bodyRenderer.sharedMaterial = enemyMaterial;
            }
        }

        public void SetLastGateId(int gateId) => lastGateId = gateId;
        public int GetLastGateId() => lastGateId;

        private void Update()
        {
            if (isDead) return;

            // Authentic Mob Control running animation (lean forward + lateral waddle bounce)
            float runCycle = (Time.time + animOffset) * (moveSpeed * 2.8f);
            float forwardTilt = (team == MobTeam.Player) ? 10f : -10f;
            float roll = Mathf.Sin(runCycle) * 7f;
            transform.rotation = Quaternion.Euler(forwardTilt, 0f, roll);
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            // Move forward (Player: +Z, Enemy: -Z)
            Vector3 forwardDirection = (team == MobTeam.Player) ? Vector3.forward : Vector3.back;
            Vector3 targetVelocity = forwardDirection * moveSpeed;

            // Simple crowd separation: avoid overlapping same-team mobs
            Collider[] neighbors = Physics.OverlapSphere(transform.position, crowdSeparationRadius);
            Vector3 pushForce = Vector3.zero;

            foreach (var col in neighbors)
            {
                if (col != null && col.gameObject != gameObject && col.TryGetComponent<MobUnit>(out var neighborUnit))
                {
                    if (neighborUnit.team == this.team && !neighborUnit.IsDead)
                    {
                        Vector3 away = transform.position - neighborUnit.transform.position;
                        away.y = 0f;
                        float dist = away.magnitude;
                        if (dist > 0.001f && dist < crowdSeparationRadius)
                        {
                            pushForce += away.normalized * ((crowdSeparationRadius - dist) / crowdSeparationRadius) * crowdSeparationForce;
                        }
                    }
                }
            }

            // Clamp max lateral push force to prevent solver explosions
            pushForce = Vector3.ClampMagnitude(pushForce, 4f);

            if (rb != null)
            {
                rb.linearVelocity = targetVelocity + pushForce;
            }
            else
            {
                transform.position += (targetVelocity + pushForce) * Time.fixedDeltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead || other == null) return;

            // Opposing unit collision: combat
            if (other.TryGetComponent<MobUnit>(out var otherUnit))
            {
                if (otherUnit.team != this.team && !otherUnit.IsDead)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPop();
                    }

                    int myPower = this.combatPower;
                    int otherPower = otherUnit.combatPower;

                    this.combatPower -= otherPower;
                    otherUnit.combatPower -= myPower;

                    if (this.combatPower <= 0)
                    {
                        Die();
                    }
                    if (otherUnit.combatPower <= 0)
                    {
                        otherUnit.Die();
                    }
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isDead || collision == null || collision.gameObject == null) return;

            // Fallback for non-trigger collisions
            if (collision.gameObject.TryGetComponent<MobUnit>(out var otherUnit))
            {
                if (otherUnit.team != this.team && !otherUnit.IsDead)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPop();
                    }

                    int myPower = this.combatPower;
                    int otherPower = otherUnit.combatPower;

                    this.combatPower -= otherPower;
                    otherUnit.combatPower -= myPower;

                    if (this.combatPower <= 0)
                    {
                        Die();
                    }
                    if (otherUnit.combatPower <= 0)
                    {
                        otherUnit.Die();
                    }
                }
            }
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;

            SpawnDeathPop();

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnMob(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SpawnDeathPop()
        {
            Color c = (team == MobTeam.Player) ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.25f, 0.25f);
            for (int i = 0; i < 3; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.transform.position = transform.position + Random.insideUnitSphere * 0.15f;
                p.transform.localScale = Vector3.one * 0.14f;

                Renderer r = p.GetComponent<Renderer>();
                if (r != null) r.material.color = c;

                Collider col = p.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                Rigidbody rb = p.AddComponent<Rigidbody>();
                rb.linearVelocity = (Vector3.up * 2.2f) + Random.insideUnitSphere * 1.8f;
                Destroy(p, 0.28f);
            }
        }
    }
}
