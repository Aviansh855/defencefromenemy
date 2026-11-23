using UnityEngine;

namespace MobRush
{
    [RequireComponent(typeof(Collider))]
    public class PlayerDefenseLine : MonoBehaviour
    {
        public CannonController cannon;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            if (cannon == null) cannon = FindAnyObjectByType<CannonController>();
        }

        public void ResetLine()
        {
            if (cannon == null) cannon = FindAnyObjectByType<CannonController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<MobUnit>(out var unit) && unit.team == MobTeam.Enemy)
            {
                int dmg = Mathf.Max(1, unit.combatPower * 4);
                unit.Die();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPop();
                }

                if (cannon == null) cannon = FindAnyObjectByType<CannonController>();
                if (cannon != null)
                {
                    cannon.TakeDamage(dmg);
                }
            }
        }
    }
}
