using System.Collections.Generic;
using UnityEngine;

namespace ARNavigation
{
    public class ARPlaneWalkableManager : MonoBehaviour
    {
        [Header("Trackables")]
        [SerializeField] private string trackablesObjectName = "Trackables";

        [Header("Point Generation")]
        [SerializeField] private float pointSpacing = 0.2f;
        [SerializeField] private float raycastHeight = 2f;
        [SerializeField] private float raycastDistance = 5f;

        [Header("Grouping")]
        [SerializeField] private float rotationTolerance = 5f;
        [SerializeField] private float heightTolerance = 0.2f;

        private Transform trackables;
        private int previousChildCount = -1;

        private readonly List<ARWalkablePlaneData> planeDataList = new List<ARWalkablePlaneData>();
        private readonly Dictionary<int, List<ARWalkablePlaneData>> groupedPlanes = new Dictionary<int, List<ARWalkablePlaneData>>();

        private void Update()
        {
            if (trackables == null)
            {
                TryFindTrackables();
                return;
            }

            if (trackables.childCount != previousChildCount)
            {
                previousChildCount = trackables.childCount;
                RefreshPlanes();
            }
        }

        private void TryFindTrackables()
        {
            GameObject trackablesObject = GameObject.Find(trackablesObjectName);

            if (trackablesObject != null)
            {
                trackables = trackablesObject.transform;
                previousChildCount = -1;
                RefreshPlanes();
                Debug.Log("[ARPlaneWalkableManager] Trackables encontrado.");
            }
        }

        private void RefreshPlanes()
        {
            planeDataList.Clear();
            groupedPlanes.Clear();

            if (trackables == null) return;

            for (int i = 0; i < trackables.childCount; i++)
            {
                Transform plane = trackables.GetChild(i);

                MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
                MeshCollider meshCollider = plane.GetComponent<MeshCollider>();

                if (meshFilter == null || meshCollider == null || meshFilter.sharedMesh == null)
                    continue;

                ARWalkablePlaneData planeData = new ARWalkablePlaneData
                {
                    PlaneTransform = plane,
                    MeshFilter = meshFilter,
                    MeshCollider = meshCollider,
                    HeightY = plane.position.y
                };

                planeData.GroupKey = GetGroupKey(plane.rotation, plane.position);
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

        public List<ARWalkablePlaneData> GetAllPlanes()
        {
            return planeDataList;
        }

        public Dictionary<int, List<ARWalkablePlaneData>> GetGroupedPlanes()
        {
            return groupedPlanes;
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

        public int GetPlaneGroupKey(ARWalkablePlaneData planeData)
        {
            if (planeData == null) return -1;
            return planeData.GroupKey;
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
    }
}