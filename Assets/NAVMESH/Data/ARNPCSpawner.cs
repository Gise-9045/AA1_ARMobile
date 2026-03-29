using System.Collections.Generic;
using UnityEngine;

namespace ARNavigation
{
    public class ARNPCSpawner : MonoBehaviour
    {
        [SerializeField] private ARPlaneWalkableManager walkableManager;
        [SerializeField] private GameObject npcPrefab;
        [SerializeField] private Camera arCamera;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnHeightOffset = 0.05f;
        [SerializeField] private float npcScale = 0.2f;
        [SerializeField] private float maxSpawnDistance = 5f;

        private readonly List<GameObject> spawnedNpcs = new List<GameObject>();

        public void SpawnNPCInFrontOfCamera()
        {
            if (walkableManager == null)
            {
                Debug.LogWarning("[ARNPCSpawner] No hay referencia a ARPlaneWalkableManager.");
                return;
            }

            if (npcPrefab == null)
            {
                Debug.LogWarning("[ARNPCSpawner] No hay prefab asignado.");
                return;
            }

            if (arCamera == null)
            {
                arCamera = Camera.main;

                if (arCamera == null)
                {
                    Debug.LogWarning("[ARNPCSpawner] No se encontró la cámara AR.");
                    return;
                }
            }

            Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxSpawnDistance))
            {
                Debug.LogWarning("[ARNPCSpawner] No se detectó ningún plano delante de la cámara.");
                return;
            }

            ARWalkablePlaneData selectedPlane = walkableManager.GetPlaneDataFromCollider(hit.collider);

            if (selectedPlane == null)
            {
                Debug.LogWarning("[ARNPCSpawner] El collider golpeado no pertenece a un plano navegable.");
                return;
            }

            Vector3 spawnPoint = GetClosestWalkablePoint(hit.point, selectedPlane);
            spawnPoint += Vector3.up * spawnHeightOffset;

            GameObject newNpc = Instantiate(npcPrefab, spawnPoint, Quaternion.identity);
            newNpc.transform.localScale = Vector3.one * npcScale;

            ARNPCSimpleMover mover = newNpc.GetComponent<ARNPCSimpleMover>();
            if (mover != null)
            {
                mover.Initialize(walkableManager, selectedPlane);
            }
            else
            {
                Debug.LogWarning("[ARNPCSpawner] El prefab del NPC no tiene ARNPCSimpleMover.");
            }

            spawnedNpcs.Add(newNpc);

            Debug.Log($"[ARNPCSpawner] NPC instanciado delante de la cámara en {spawnPoint}. Total NPCs: {spawnedNpcs.Count}");
        }

        private Vector3 GetClosestWalkablePoint(Vector3 targetPoint, ARWalkablePlaneData planeData)
        {
            if (planeData == null || planeData.WalkablePoints == null || planeData.WalkablePoints.Count == 0)
            {
                return targetPoint;
            }

            float closestDistance = float.MaxValue;
            Vector3 closestPoint = planeData.WalkablePoints[0];

            for (int i = 0; i < planeData.WalkablePoints.Count; i++)
            {
                float distance = Vector3.Distance(targetPoint, planeData.WalkablePoints[i]);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = planeData.WalkablePoints[i];
                }
            }

            return closestPoint;
        }

        public void DestroyAllNPCs()
        {
            for (int i = 0; i < spawnedNpcs.Count; i++)
            {
                if (spawnedNpcs[i] != null)
                {
                    Destroy(spawnedNpcs[i]);
                }
            }

            spawnedNpcs.Clear();
            Debug.Log("[ARNPCSpawner] Todos los NPCs han sido eliminados.");
        }

        public int GetSpawnedNPCCount()
        {
            return spawnedNpcs.Count;
        }
    }
}