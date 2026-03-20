using System.Collections;
using UnityEngine;

namespace ARNavigation
{
    public class ARNPCSpawner : MonoBehaviour
    {
        [SerializeField] private ARPlaneWalkableManager walkableManager;
        [SerializeField] private GameObject npcPrefab;

        [Header("Spawn")]
        [SerializeField] private bool spawnAutomatically = true;
        [SerializeField] private float spawnHeightOffset = 0.05f;
        [SerializeField] private float retryInterval = 1f;
        [SerializeField] private int maxRetries = 20;

        private GameObject currentNpcInstance;
        private ARWalkablePlaneData currentPlaneData;
        private Coroutine spawnCoroutine;

        private void Start()
        {
            if (spawnAutomatically)
            {
                spawnCoroutine = StartCoroutine(TrySpawnWhenReady());
            }
        }

        public IEnumerator TrySpawnWhenReady()
        {
            int attempts = 0;

            while (attempts < maxRetries)
            {
                attempts++;

                if (walkableManager == null)
                {
                    Debug.LogWarning("[ARNPCSpawner] WalkableManager es null.");
                    yield return new WaitForSeconds(retryInterval);
                    continue;
                }

                ARWalkablePlaneData selectedPlane = walkableManager.GetRandomPlane();

                if (selectedPlane == null)
                {
                    Debug.Log($"[ARNPCSpawner] Intento {attempts}: no hay planos válidos todavía.");
                    yield return new WaitForSeconds(retryInterval);
                    continue;
                }

                if (selectedPlane.WalkablePoints == null || selectedPlane.WalkablePoints.Count == 0)
                {
                    Debug.Log($"[ARNPCSpawner] Intento {attempts}: el plano no tiene puntos caminables.");
                    yield return new WaitForSeconds(retryInterval);
                    continue;
                }

                SpawnOnPlane(selectedPlane);
                yield break;
            }

            Debug.LogWarning("[ARNPCSpawner] No se pudo instanciar el NPC tras varios intentos.");
        }

        public void SpawnNPC()
        {
            if (walkableManager == null)
            {
                Debug.LogWarning("[ARNPCSpawner] WalkableManager no asignado.");
                return;
            }

            ARWalkablePlaneData selectedPlane = walkableManager.GetRandomPlane();

            if (selectedPlane == null)
            {
                Debug.LogWarning("[ARNPCSpawner] No hay planos válidos para spawnear.");
                return;
            }

            if (selectedPlane.WalkablePoints == null || selectedPlane.WalkablePoints.Count == 0)
            {
                Debug.LogWarning("[ARNPCSpawner] El plano seleccionado no tiene puntos caminables.");
                return;
            }

            SpawnOnPlane(selectedPlane);
        }

        private void SpawnOnPlane(ARWalkablePlaneData selectedPlane)
        {
            if (npcPrefab == null)
            {
                Debug.LogWarning("[ARNPCSpawner] npcPrefab no asignado.");
                return;
            }

            Vector3 spawnPoint = walkableManager.GetRandomPointFromPlane(selectedPlane);
            spawnPoint.y += spawnHeightOffset;

            if (currentNpcInstance != null)
            {
                Destroy(currentNpcInstance);
            }

            currentNpcInstance = Instantiate(npcPrefab, spawnPoint, Quaternion.identity);
            currentPlaneData = selectedPlane;

            Debug.Log($"[ARNPCSpawner] NPC instanciado en {spawnPoint}");

            ARNPCSimpleMover mover = currentNpcInstance.GetComponent<ARNPCSimpleMover>();

            if (mover != null)
            {
                mover.Initialize(walkableManager, currentPlaneData);
            }
            else
            {
                Debug.LogWarning("[ARNPCSpawner] El prefab no tiene ARNPCSimpleMover.");
            }
        }

        public GameObject GetCurrentNpc()
        {
            return currentNpcInstance;
        }
    }
}