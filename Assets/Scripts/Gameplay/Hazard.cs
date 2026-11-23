using UnityEngine;

namespace MobRush
{
    public enum AffectsTeam
    {
        Both,
        PlayerOnly,
        EnemyOnly
    }

    [RequireComponent(typeof(Collider))]
    public class Hazard : MonoBehaviour
    {
        [Header("Targeting")]
        public AffectsTeam affectsTeam = AffectsTeam.Both;

        [Header("Movement")]
        public bool isRotating = false;
        public bool isOscillating = true;
        public float speed = 2.5f;
        public float range = 2.5f;

        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (isOscillating)
            {
                float offset = (Mathf.PingPong(Time.time * speed, range * 2) - range);
                transform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
            }

            if (isRotating)
            {
                transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<MobUnit>(out var unit))
            {
                HandleUnit(unit);
            }
        }

        public void HandleUnit(MobUnit unit)
        {
            if (unit == null) return;

            // Check if this hazard affects the unit's team
            if (affectsTeam == AffectsTeam.PlayerOnly && unit.team != MobTeam.Player)
            {
                return; // Enemy unit survives PlayerOnly object!
            }
            if (affectsTeam == AffectsTeam.EnemyOnly && unit.team != MobTeam.Enemy)
            {
                return; // Player unit survives EnemyOnly object!
            }

            unit.Die();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPop();
            }
        }
    }
}
