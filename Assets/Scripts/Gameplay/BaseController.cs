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

            // Spawn explosive bricks effect when base breaks
            SpawnBrickExplosion();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWin();
            }

            gameObject.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelWon();
            }
        }

        private void SpawnBrickExplosion()
        {
            // Visual pop of falling bricks
            for (int i = 0; i < 12; i++)
            {
                GameObject brick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brick.name = "Loot_Brick";
                brick.transform.position = transform.position + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.5f, 2f), Random.Range(-1f, 1f));
                brick.transform.localScale = new Vector3(0.4f, 0.25f, 0.6f);

                Renderer r = brick.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.9f, 0.5f, 0.2f); // Golden Brick
                }

                Collider c = brick.GetComponent<Collider>();
                if (c != null) c.enabled = false;

                Destroy(brick, 1.5f);
            }
        }
    }
}
