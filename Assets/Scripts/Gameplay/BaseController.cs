using TMPro;
using UnityEngine;

namespace MobRush
{
    public class BaseController : MonoBehaviour
    {
        [Header("Stats")]
        public int maxHealth = 100;
        public int currentHealth;
        public int CurrentHealth => currentHealth;

        [Header("UI & Visuals")]
        public TMP_Text healthText;
        public Transform healthFillBar;
        public Transform modelRoot;

        private bool isDestroyed = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            if (currentHealth <= 0) currentHealth = maxHealth;
        }

        private void Start()
        {
            if (currentHealth <= 0) currentHealth = maxHealth;
            UpdateUI();
        }

        public void Initialize(int health)
        {
            maxHealth = health;
            currentHealth = maxHealth;
            isDestroyed = false;
            gameObject.SetActive(true);
            UpdateUI();
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleHit(collision.gameObject);
        }

        private void HandleHit(GameObject hitObj)
        {
            if (isDestroyed) return;

            if (hitObj.TryGetComponent<MobUnit>(out var unit) && unit.team == MobTeam.Player)
            {
                int damage = unit.combatPower;
                unit.Die();
                TakeDamage(damage);
            }
        }

        public void TakeDamage(int damage)
        {
            if (isDestroyed) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            UpdateUI();

            if (damage > 0 && UIManager.Instance != null)
            {
                UIManager.Instance.TriggerTutorialStep("T6");
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBaseHit();
            }

            // Spawn chip brick debris on each impact
            SpawnChipBrick(transform.position);
            CameraShake.Shake(0.06f, 0.04f);

            // Punch scale animation
            if (modelRoot != null)
            {
                modelRoot.localScale = Vector3.one * 1.08f;
            }

            if (currentHealth <= 0)
            {
                OnBaseDestroyed();
            }
        }

        private void Update()
        {
            if (modelRoot != null && modelRoot.localScale != Vector3.one)
            {
                modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, Vector3.one, 10f * Time.deltaTime);
            }
        }

        private void UpdateUI()
        {
            if (healthText != null)
            {
                healthText.text = $"{currentHealth} / {maxHealth}";
            }

            if (healthFillBar != null)
            {
                float pct = (float)currentHealth / maxHealth;
                healthFillBar.localScale = new Vector3(pct, 1f, 1f);
            }
        }

        private void OnBaseDestroyed()
        {
            isDestroyed = true;

            // Big Screen Shake & Boom Explosion
            CameraShake.Shake(0.5f, 0.35f);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBoomExplosion();
                AudioManager.Instance.PlayWin();
            }

            // Spawn massive 3D physical golden brick explosion
            SpawnBrickExplosion();

            gameObject.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelWon();
            }
        }

        private void SpawnChipBrick(Vector3 pos)
        {
            GameObject brick = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brick.transform.position = pos + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.4f, 1.2f), Random.Range(-0.6f, 0.6f));
            brick.transform.localScale = Vector3.one * Random.Range(0.22f, 0.35f);

            Renderer r = brick.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.85f, 0.15f);

            Rigidbody rb = brick.AddComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(3f, 5f), Random.Range(-3f, -1f));
            rb.angularVelocity = Random.insideUnitSphere * 12f;

            Destroy(brick, 1.2f);
        }

        private void SpawnBrickExplosion()
        {
            // 30 physics golden cubes burst into the air, showering toward player camera
            for (int i = 0; i < 30; i++)
            {
                GameObject brick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brick.name = "Loot_Gold_Brick";
                brick.transform.position = transform.position + new Vector3(
                    Random.Range(-1.8f, 1.8f),
                    Random.Range(0.5f, 3.0f),
                    Random.Range(-1.2f, 1.2f)
                );
                brick.transform.localScale = Vector3.one * Random.Range(0.35f, 0.6f);

                Renderer r = brick.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(1f, 0.82f, 0.1f); // Pure Gleaming Gold
                }

                Rigidbody rb = brick.AddComponent<Rigidbody>();
                rb.mass = 0.4f;
                Vector3 burstDir = new Vector3(
                    Random.Range(-3.5f, 3.5f),
                    Random.Range(5.5f, 10.0f),
                    Random.Range(-6.0f, -1.5f) // Arcs toward camera
                );
                rb.linearVelocity = burstDir;
                rb.angularVelocity = Random.insideUnitSphere * 20f;

                Destroy(brick, 2.5f);
            }
        }
    }
}
