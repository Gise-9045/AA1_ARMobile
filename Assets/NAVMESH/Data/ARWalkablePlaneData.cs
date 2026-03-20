using System.Collections.Generic;
using UnityEngine;

namespace ARNavigation
{
    [System.Serializable]
    public class ARWalkablePlaneData
    {
        public Transform PlaneTransform;
        public MeshFilter MeshFilter;
        public MeshCollider MeshCollider;

        public int GroupKey;
        public float HeightY;

        public List<Vector3> WalkablePoints = new List<Vector3>();
    }
}