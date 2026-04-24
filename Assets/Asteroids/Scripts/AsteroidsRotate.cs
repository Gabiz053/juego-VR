using UnityEngine;
using System.Collections; // Добавлено для использования IEnumerator
namespace Asteroids
{

    public class AsteroidOrbitAndRotate : MonoBehaviour
    {
        [Tooltip("The target around which the rotation will occur.")]
        public Transform sun;

        [Tooltip("Base rotation speed around the sun.")]
        public float orbitSpeed = 5f;

        [Tooltip("Minimum rotation speed around its own axis.")]
        public float minRotationSpeed = 10f; 

        [Tooltip("Maximum rotation speed around its own axis.")]
        public float maxRotationSpeed = 50f; 

        [Tooltip("Whether to consider collisions or not.")]
        public bool checkCollisions = false; 

        [Tooltip("Range for random additional orbit speed.")]
        public float additionalOrbitSpeedRange = 2f; 

        [Tooltip("Dust Blust prefab.")]
        public GameObject dustBlustPrefab; // Dust Cloud prefab
        private float radius;
        private Vector3 rotationSpeed; 
        private float currentAdditionalOrbitSpeed = 0f; 

        void Start()
        {
            // calculate orbit radius
            radius = Vector3.Distance(transform.position, sun.position);

            // Generate random additional speed
            rotationSpeed = new Vector3(
                Random.Range(minRotationSpeed, maxRotationSpeed),
                Random.Range(minRotationSpeed, maxRotationSpeed),
                Random.Range(minRotationSpeed, maxRotationSpeed)
            );

            // additional speed for collisions
            if (checkCollisions)
            {
                
                currentAdditionalOrbitSpeed = Random.Range(-additionalOrbitSpeedRange, additionalOrbitSpeedRange);
            }
        }

        void Update()
        {
            // self rotate
            transform.Rotate(rotationSpeed * Time.deltaTime);

            // sun orbital
            float effectiveOrbitSpeed = orbitSpeed + (checkCollisions ? currentAdditionalOrbitSpeed : 0);
            transform.RotateAround(sun.position, Vector3.up, effectiveOrbitSpeed * Time.deltaTime);
        }

        void OnCollisionEnter(Collision other)
        {
            
            if (checkCollisions && other.gameObject.CompareTag("Asteroid"))
            {
                //Debug.Log("Collision Detected with " + other.gameObject.name);
                Vector3 collisionPoint = other.contacts[0].point;
                SpawnAndAnimateDustCloud(collisionPoint);

                Vector3 pushDirection = (transform.position - other.contacts[0].point).normalized;
                float sizeFactor = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);

                // get collision asteroid script
                AsteroidOrbitAndRotate otherAsteroidScript = other.gameObject.GetComponent<AsteroidOrbitAndRotate>();
                if (otherAsteroidScript != null)
                {
                    // calculate position and speed
                    AdjustSpeedAndPositionAfterCollision(otherAsteroidScript, pushDirection, sizeFactor);
                }
            }
        }

        private void AdjustSpeedAndPositionAfterCollision(AsteroidOrbitAndRotate otherAsteroid, Vector3 pushDirection, float sizeFactor)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            Rigidbody otherRb = otherAsteroid.GetComponent<Rigidbody>();

            if (currentAdditionalOrbitSpeed < otherAsteroid.currentAdditionalOrbitSpeed)
            {
               
                otherRb.AddForce(pushDirection * sizeFactor * 3, ForceMode.Impulse);
                otherAsteroid.currentAdditionalOrbitSpeed -= 1f;

                
                rb.AddForce(-pushDirection * sizeFactor * 2, ForceMode.Impulse);
                currentAdditionalOrbitSpeed += 1f;
            }
            else
            {
                
                rb.AddForce(pushDirection * sizeFactor * 3, ForceMode.Impulse);
                currentAdditionalOrbitSpeed -= 1f;

                otherRb.AddForce(-pushDirection * sizeFactor * 2, ForceMode.Impulse);
                otherAsteroid.currentAdditionalOrbitSpeed += 1f;
            }
        }

        private void SpawnAndAnimateDustCloud(Vector3 position)
        {
            //Debug.Log("SpawnAndAnimateDustCloud");
            // Create collision animation
            GameObject dustCloud = Instantiate(dustBlustPrefab, position, Quaternion.identity);
            dustCloud.SetActive(true);
            StartCoroutine(AnimateDustCloudSize(dustCloud));
        }

        private IEnumerator AnimateDustCloudSize(GameObject dustCloud)
        {
            //Debug.Log("AnimateDustCloudSize");
            float targetSize = 17f;
            float currentTime = 0f;
            float duration = 7f; // Animation len

            while (currentTime < duration)
            {
                float scale = Mathf.Lerp(1f, targetSize, currentTime / duration);
                dustCloud.transform.localScale = new Vector3(scale, scale, scale);
                currentTime += Time.deltaTime;
                yield return null;
            }
            // Destroy cloud
            Destroy(dustCloud);
        }

    }
}