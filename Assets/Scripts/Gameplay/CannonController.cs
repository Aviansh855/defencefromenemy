using UnityEngine;
using UnityEngine.InputSystem;

namespace MobRush
{
    public class CannonController : MonoBehaviour
    {
        [Header("Movement")]
        public float xLimit = 3.6f;
        public float moveSensitivity = 0.022f;
        public float keyboardSpeed = 9f;
        public float smoothSpeed = 22f;

        [Header("Firing")]
        public Transform firePoint;
        public bool isFiringActive = true;
        public bool isPointerHeld { get; private set; } = false;

        [Header("Health")]
        public int maxHealth = 100;
        public int currentHealth = 100;

        [Header("Juice & Feel")]
        public Transform barrelVisual;
        public float recoilAmount = 0.22f;

        [Header("Champion Charge System (Mob Control Original)")]
        public float championCharge = 0f;
        public float maxChampionCharge = 100f;
        public float chargePerShot = 2.5f;

        public bool hasMovedSinceStart { get; private set; } = false;

        private float targetX;
        private float lastFireTime;
        private Vector3 initialBarrelLocalPos;
        private Vector2 lastPointerPos;
        private bool isDragging = false;

        private void Start()
        {
            targetX = transform.position.x;
            if (barrelVisual != null)
                initialBarrelLocalPos = barrelVisual.localPosition;
            if (firePoint == null)
                firePoint = transform;
        }

        public void ResetHealth(int hp = 100)
        {
            maxHealth = hp;
            currentHealth = maxHealth;
            championCharge = 0f;
        }

        public void TakeDamage(int dmg)
        {
            if (currentHealth <= 0) return;
            currentHealth = Mathf.Max(0, currentHealth - dmg);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBaseHit();
            }

            if (currentHealth <= 0)
            {
                isFiringActive = false;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnLevelLost();
                }
            }
        }

        public void SpawnStartingCrowdBurst()
        {
            // Initial burst of starting units
            int startCount = UpgradeConfig.GetStartingCrowd(SaveManager.Data.startingPushLevel);
            for (int i = 0; i < startCount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-0.35f, 0.35f),
                    0f,
                    Random.Range(0.4f, 1.8f)
                );
                SpawnSingleUnit(firePoint.position + offset);
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            HandleInput();
            HandleMovement();

            // Continuous firing active during fight
            if (isFiringActive)
            {
                HandleFiring();
            }

            UpdateBarrelRecoil();
        }

        private void HandleInput()
        {
            // 1. Keyboard Controls (A/D or Arrow keys)
            float hInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    hInput -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    hInput += 1f;
            }

            if (Mathf.Abs(hInput) > 0.01f)
            {
                targetX += hInput * keyboardSpeed * Time.deltaTime;
                hasMovedSinceStart = true;
            }

            // 2. Touch / Mouse Direct Drag Control (Exact Mob Control feel)
            bool pointerDown = false;
            bool pointerUp = false;
            bool pointerHeld = false;
            Vector2 currentPointerPos = Vector2.zero;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerHeld = true;
                currentPointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    pointerDown = true;
            }
            else if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    pointerDown = true;
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    pointerUp = true;
                if (Mouse.current.leftButton.isPressed)
                {
                    pointerHeld = true;
                    currentPointerPos = Mouse.current.position.ReadValue();
                }
            }

            isPointerHeld = pointerHeld;

            if (pointerDown)
            {
                isDragging = true;
                lastPointerPos = currentPointerPos;
            }
            else if (pointerUp || !pointerHeld)
            {
                isDragging = false;
            }

            if (isDragging && pointerHeld)
            {
                float deltaX = currentPointerPos.x - lastPointerPos.x;
                if (Mathf.Abs(deltaX) > 0.02f)
                {
                    hasMovedSinceStart = true;
                }

                // Dynamic responsive ratio based on screen width
                float screenRatio = Mathf.Max(300f, Screen.width);
                targetX += (deltaX / screenRatio) * (xLimit * 2.8f);
                lastPointerPos = currentPointerPos;
            }

            targetX = Mathf.Clamp(targetX, -xLimit, xLimit);
        }

        private void HandleMovement()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, targetX, smoothSpeed * Time.deltaTime);
            transform.position = pos;
        }

        private void HandleFiring()
        {
            float fireRate = UpgradeConfig.GetFireRate(SaveManager.Data.fireRateLevel);
            // Boost fire rate slightly when player is actively touching/holding
            if (isPointerHeld)
            {
                fireRate *= 1.25f;
            }

            float fireInterval = 1f / Mathf.Max(0.1f, fireRate);

            if (Time.time >= lastFireTime + fireInterval)
            {
                Vector3 spawnPos = firePoint.position + new Vector3(Random.Range(-0.16f, 0.16f), 0, 0);
                SpawnSingleUnit(spawnPos);
                lastFireTime = Time.time;

                // Add Champion charge
                championCharge += chargePerShot;
                if (championCharge >= maxChampionCharge)
                {
                    championCharge = 0f;
                    SpawnChampionUnit(firePoint.position + Vector3.forward * 1.5f);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayShoot();
                }

                if (barrelVisual != null)
                {
                    barrelVisual.localPosition = initialBarrelLocalPos - Vector3.forward * recoilAmount;
                    barrelVisual.localScale = new Vector3(1.15f, 0.82f, 1.15f);
                }
            }
        }

        private void SpawnSingleUnit(Vector3 pos)
        {
            if (ObjectPooler.Instance != null)
            {
                MobUnit unit = ObjectPooler.Instance.SpawnMob(MobTeam.Player, pos, Quaternion.identity);
                if (unit != null)
                {
                    unit.combatPower = UpgradeConfig.GetUnitStrength(SaveManager.Data.strengthLevel);
                }
            }
        }

        public void SpawnChampionUnit(Vector3 pos)
        {
            if (ObjectPooler.Instance != null)
            {
                MobUnit champ = ObjectPooler.Instance.SpawnMob(MobTeam.Player, pos, Quaternion.identity);
                if (champ != null)
                {
                    champ.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
                    champ.combatPower = UpgradeConfig.GetUnitStrength(SaveManager.Data.strengthLevel) * 10;

                    if (champ.bodyRenderer != null)
                    {
                        champ.bodyRenderer.material.color = new Color(1f, 0.85f, 0.15f); // Sirion Gold Champion
                    }

                    // Tactile Feedback: Screenshake & Champion Horn Fanfare
                    CameraShake.Shake(0.24f, 0.18f);

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayChampionSpawn();
                    }
                }
            }
        }

        private void UpdateBarrelRecoil()
        {
            if (barrelVisual != null)
            {
                barrelVisual.localPosition = Vector3.Lerp(barrelVisual.localPosition, initialBarrelLocalPos, 16f * Time.deltaTime);
                barrelVisual.localScale = Vector3.Lerp(barrelVisual.localScale, Vector3.one, 16f * Time.deltaTime);
            }
        }

        public void ResetPosition()
        {
            targetX = 0f;
            transform.position = new Vector3(0f, transform.position.y, transform.position.z);
            hasMovedSinceStart = false;
        }
    }
}
