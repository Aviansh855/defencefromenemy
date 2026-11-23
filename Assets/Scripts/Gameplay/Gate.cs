using TMPro;
using UnityEngine;

namespace MobRush
{
    [RequireComponent(typeof(Collider))]
    public class Gate : MonoBehaviour
    {
        private static int nextUniqueId = 1;

        [Header("Gate Settings")]
        public AffectsTeam affectsTeam = AffectsTeam.Both;
        public GateModifierType modifierType = GateModifierType.Multiply;
        public int value = 2;
        public TMP_Text displayText;

        [Header("Sliding Movement")]
        public bool isMoving = false;
        public float moveRange = 1.8f;
        public float moveSpeed = 2f;

        [Header("Renderers & Colors")]
        public Renderer glassRenderer;
        public Color positiveColor = new Color(0.1f, 0.7f, 1f, 0.7f);
        public Color highPositiveColor = new Color(0.1f, 1f, 0.4f, 0.75f);
        public Color negativeColor = new Color(1f, 0.2f, 0.2f, 0.75f);

        private Vector3 startPosition;
        private int gateId;
        private Transform rootTransform;
        private int remainingAddPool = 0;
        private int remainingSubPool = 0;

        private void Awake()
        {
            gateId = nextUniqueId++;
            rootTransform = (transform.parent != null) ? transform.parent : transform;
            startPosition = rootTransform.position;

            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            UpdateVisuals();
        }

        private void Update()
        {
            if (rootTransform != null)
            {
                if (isMoving)
                {
                    float offset = (Mathf.PingPong(Time.time * moveSpeed, moveRange * 2) - moveRange);
                    rootTransform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
                }

                // Smooth recoil recovery from crowd pass punch
                rootTransform.localScale = Vector3.Lerp(rootTransform.localScale, Vector3.one, 12f * Time.deltaTime);
            }
        }

        public void Setup(GateModifierType type, int val, bool moving = false, float range = 1.8f, float speed = 2f)
        {
            modifierType = type;
            value = val;
            isMoving = moving;
            moveRange = range;
            moveSpeed = speed;

            rootTransform = (transform.parent != null) ? transform.parent : transform;
            startPosition = rootTransform.position;

            if (type == GateModifierType.Add) remainingAddPool = val;
            if (type == GateModifierType.Subtract) remainingSubPool = val;

            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            string label = "";
            Color c = positiveColor;

            switch (modifierType)
            {
                case GateModifierType.Multiply:
                    label = $"x{value}";
                    c = (value >= 3) ? highPositiveColor : positiveColor;
                    break;
                case GateModifierType.Add:
                    label = $"+{value}";
                    c = highPositiveColor;
                    break;
                case GateModifierType.Subtract:
                    label = $"-{value}";
                    c = negativeColor;
                    break;
                case GateModifierType.Divide:
                    label = $"÷{value}";
                    c = negativeColor;
                    break;
            }

            if (displayText != null)
            {
                displayText.text = label;
            }

            if (glassRenderer != null && glassRenderer.sharedMaterial != null)
            {
                glassRenderer.material.color = c;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<MobUnit>(out var unit))
            {
                if (affectsTeam == AffectsTeam.PlayerOnly && unit.team != MobTeam.Player) return;
                if (affectsTeam == AffectsTeam.EnemyOnly && unit.team != MobTeam.Enemy) return;

                if (unit.GetLastGateId() == gateId) return;
                unit.SetLastGateId(gateId);

                ApplyGateEffect(unit);
            }
        }

        private void ApplyGateEffect(MobUnit unit)
        {
            if (rootTransform != null)
            {
                rootTransform.localScale = Vector3.one * 1.10f;
            }

            switch (modifierType)
            {
                case GateModifierType.Multiply:
                    int extra = Mathf.Max(0, value - 1);
                    SpawnClones(unit, extra);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayGate();
                    break;

                case GateModifierType.Add:
                    if (remainingAddPool > 0)
                    {
                        SpawnClones(unit, 1);
                        remainingAddPool--;
                        if (displayText != null) displayText.text = $"+{remainingAddPool}";
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayGate();
                    }
                    break;

                case GateModifierType.Subtract:
                    if (remainingSubPool > 0)
                    {
                        remainingSubPool--;
                        unit.Die();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayPop();

                        if (remainingSubPool <= 0)
                        {
                            // Gate is fully depleted! Transform into a positive bonus gate (+5) for all following units
                            modifierType = GateModifierType.Add;
                            value = 5;
                            remainingAddPool = 5;
                            UpdateVisuals();

                            // Also play rewarding sound effect
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                        }
                        else
                        {
                            if (displayText != null) displayText.text = $"-{remainingSubPool}";
                        }
                    }
                    else
                    {
                        // Already broken / turned positive! Spawn reward clones
                        SpawnClones(unit, 1);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayGate();
                    }
                    break;

                case GateModifierType.Divide:
                    // 50% chance of unit destruction per pass for division
                    if (Random.value < 0.5f)
                    {
                        unit.Die();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayPop();
                    }
                    break;
            }
        }

        private void SpawnClones(MobUnit original, int count)
        {
            if (ObjectPooler.Instance == null) return;

            bool isChampion = original.transform.localScale.x > 1.2f;

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-0.55f, 0.55f),
                    0f,
                    Random.Range(-0.3f, 0.3f)
                );

                MobUnit clone = ObjectPooler.Instance.SpawnMob(original.team, original.transform.position + spawnOffset, original.transform.rotation);
                if (clone != null)
                {
                    clone.SetLastGateId(gateId);
                    if (isChampion)
                    {
                        clone.transform.localScale = original.transform.localScale;
                        clone.combatPower = original.combatPower;
                    }
                }
            }
        }
    }
}
