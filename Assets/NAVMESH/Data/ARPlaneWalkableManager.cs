using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARNavigation
{
    public class ARPlaneWalkableManager : MonoBehaviour
    {
        [Header("AR")]
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Point Generation")]
        [SerializeField] private float pointSpacing = 0.2f;
        [SerializeField] private float raycastHeight = 2f;
        [SerializeField] private float raycastDistance = 5f;

        [Header("Grouping")]
        [SerializeField] private float rotationTolerance = 5f;
        [SerializeField] private float heightTolerance = 0.2f;

        [Header("Runtime Debug")]
        [SerializeField] private bool showRuntimeDebugPoints = false;
        [SerializeField] private GameObject debugPointPrefab;
        [SerializeField] private float debugPointScale = 0.03f;
        [SerializeField] private Transform debugContainer;

        private readonly List<ARWalkablePlaneData> planeDataList = new List<ARWalkablePlaneData>();
        private readonly Dictionary<int, List<ARWalkablePlaneData>> groupedPlanes = new Dictionary<int, List<ARWalkablePlaneData>>();
        private readonly List<GameObject> spawnedDebugPoints = new List<GameObject>();

        private void OnEnable()
        {
            if (planeManager == null)
            {
                planeManager = FindObjectOfType<ARPlaneManager>();
            }

            if (planeManager == null)
            {
                Debug.LogError("[ARPlaneWalkableManager] No se encontró ARPlaneManager.");
                return;
            }

            planeManager.planesChanged += OnPlanesChanged;

            foreach (ARPlane plane in planeManager.trackables)
            {
                SubscribeToPlane(plane);
            }

            RefreshPlanes();
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.planesChanged -= OnPlanesChanged;

                foreach (ARPlane plane in planeManager.trackables)
                {
                    UnsubscribeFromPlane(plane);
                }
            }
        }

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            for (int i = 0; i < args.added.Count; i++)
            {
                SubscribeToPlane(args.added[i]);
            }

            for (int i = 0; i < args.removed.Count; i++)
            {
                UnsubscribeFromPlane(args.removed[i]);
            }

            RefreshPlanes();
        }

        private void SubscribeToPlane(ARPlane plane)
        {
            if (plane == null) return;
            plane.boundaryChanged -= OnPlaneBoundaryChanged;
            plane.boundaryChanged += OnPlaneBoundaryChanged;
        }

        private void UnsubscribeFromPlane(ARPlane plane)
        {
            if (plane == null) return;
            plane.boundaryChanged -= OnPlaneBoundaryChanged;
        }

        private void OnPlaneBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
        {
            RefreshPlanes();
        }

        private void RefreshPlanes()
        {
            planeDataList.Clear();
            groupedPlanes.Clear();

            if (planeManager == null) return;

            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane == null) continue;

                MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
                MeshCollider meshCollider = plane.GetComponent<MeshCollider>();

                if (meshFilter == null || meshCollider == null || meshFilter.sharedMesh == null)
                    continue;

                ARWalkablePlaneData planeData = new ARWalkablePlaneData
                {
                    PlaneTransform = plane.transform,
                    MeshFilter = meshFilter,
                    MeshCollider = meshCollider,
                    HeightY = plane.transform.position.y
                };

                planeData.GroupKey = GetGroupKey(plane.transform.rotation, plane.transform.position);
                GenerateWalkablePoints(planeData);

                if (planeData.WalkablePoints.Count > 0)
                {
                    planeDataList.Add(planeData);

                    if (!groupedPlanes.ContainsKey(planeData.GroupKey))
                    {
                        groupedPlanes.Add(planeData.GroupKey, new List<ARWalkablePlaneData>());
                    }

                    groupedPlanes[planeData.GroupKey].Add(planeData);
                }
            }

            if (showRuntimeDebugPoints)
            {
                DrawRuntimeDebugPoints();
            }

            Debug.Log($"[ARPlaneWalkableManager] Planos válidos: {planeDataList.Count}, grupos: {groupedPlanes.Count}");
        }

        private void GenerateWalkablePoints(ARWalkablePlaneData planeData)
        {
            planeData.WalkablePoints.Clear();

            Bounds bounds = planeData.MeshCollider.bounds;

            for (float x = bounds.min.x; x <= bounds.max.x; x += pointSpacing)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += pointSpacing)
                {
                    Vector3 rayOrigin = new Vector3(x, bounds.max.y + raycastHeight, z);

                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance))
                    {
                        if (hit.collider == planeData.MeshCollider)
                        {
                            planeData.WalkablePoints.Add(hit.point);
                        }
                    }
                }
            }
        }

        private int GetGroupKey(Quaternion rotation, Vector3 position)
        {
            Vector3 euler = rotation.eulerAngles;

            int rotX = Mathf.RoundToInt(euler.x / rotationTolerance);
            int rotY = Mathf.RoundToInt(euler.y / rotationTolerance);
            int rotZ = Mathf.RoundToInt(euler.z / rotationTolerance);
            int posY = Mathf.RoundToInt(position.y / heightTolerance);

            return rotX * 100000000 + rotY * 100000 + rotZ * 100 + posY;
        }

        private void DrawRuntimeDebugPoints()
        {
            ClearRuntimeDebugPoints();

            for (int i = 0; i < planeDataList.Count; i++)
            {
                List<Vector3> points = planeDataList[i].WalkablePoints;

                for (int j = 0; j < points.Count; j += 3)
                {
                    GameObject pointVisual;

                    if (debugPointPrefab != null)
                    {
                        pointVisual = Instantiate(debugPointPrefab, points[j], Quaternion.identity);
                    }
                    else
                    {
                        pointVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        pointVisual.transform.position = points[j];

                        Collider col = pointVisual.GetComponent<Collider>();
                        if (col != null)
                        {
                            Destroy(col);
                        }
                    }

                    pointVisual.transform.localScale = Vector3.one * debugPointScale;
                    pointVisual.name = "WalkableDebugPoint";

                    if (debugContainer != null)
                    {
                        pointVisual.transform.SetParent(debugContainer);
                    }

                    spawnedDebugPoints.Add(pointVisual);
                }
            }
        }

        private void ClearRuntimeDebugPoints()
        {
            for (int i = 0; i < spawnedDebugPoints.Count; i++)
            {
                if (spawnedDebugPoints[i] != null)
                {
                    Destroy(spawnedDebugPoints[i]);
                }
            }

            spawnedDebugPoints.Clear();
        }

        public List<ARWalkablePlaneData> GetAllPlanes()
        {
            return planeDataList;
        }

        public ARWalkablePlaneData GetRandomPlane()
        {
            if (planeDataList.Count == 0) return null;
            return planeDataList[Random.Range(0, planeDataList.Count)];
        }

        public Vector3 GetRandomPointFromPlane(ARWalkablePlaneData planeData)
        {
            if (planeData == null || planeData.WalkablePoints.Count == 0)
                return Vector3.zero;

            return planeData.WalkablePoints[Random.Range(0, planeData.WalkablePoints.Count)];
        }

        public List<Vector3> GetPointsFromSameGroup(int groupKey)
        {
            List<Vector3> points = new List<Vector3>();

            if (!groupedPlanes.ContainsKey(groupKey))
                return points;

            List<ARWalkablePlaneData> planesInGroup = groupedPlanes[groupKey];

            for (int i = 0; i < planesInGroup.Count; i++)
            {
                points.AddRange(planesInGroup[i].WalkablePoints);
            }

            return points;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            if (planeDataList == null) return;

            for (int i = 0; i < planeDataList.Count; i++)
            {
                if (planeDataList[i] == null) continue;

                List<Vector3> points = planeDataList[i].WalkablePoints;
                for (int j = 0; j < points.Count; j++)
                {
                    Gizmos.DrawSphere(points[j], 0.02f);
                }
            }
        }
#endif
    }
}