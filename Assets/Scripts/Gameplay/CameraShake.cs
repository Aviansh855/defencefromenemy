using UnityEngine;

namespace MobRush
{
    public class CameraShake : MonoBehaviour
    {
        private static CameraShake _instance;
        public static CameraShake Instance
        {
            get
            {
                if (_instance == null)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        _instance = cam.GetComponent<CameraShake>();
                        if (_instance == null)
                        {
                            _instance = cam.gameObject.AddComponent<CameraShake>();
                        }
                    }
                }
                return _instance;
            }
        }

        private Vector3 originalLocalPos;
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0.1f;
        private float dampingSpeed = 1.0f;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            originalLocalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            originalLocalPos = transform.localPosition;
        }

        private void Update()
        {
            if (shakeDuration > 0)
            {
                transform.localPosition = originalLocalPos + Random.insideUnitSphere * shakeMagnitude;
                shakeDuration -= Time.deltaTime * dampingSpeed;
                if (shakeDuration <= 0)
                {
                    shakeDuration = 0f;
                    transform.localPosition = originalLocalPos;
                }
            }
        }

        public static void Shake(float duration = 0.15f, float magnitude = 0.1f)
        {
            if (Instance != null)
            {
                Instance.TriggerShake(duration, magnitude);
            }
        }

        public void TriggerShake(float duration, float magnitude)
        {
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        }
    }
}
