using System.Collections.Generic;
using UnityEngine;

namespace ARNavigation
{
    public class ARNPCSimpleMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 0.8f;
        [SerializeField] private float rotationSpeed = 6f;
        [SerializeField] private float reachDistance = 0.08f;
        [SerializeField] private float waitTimeAtPoint = 1.0f;

        [Header("Height")]
        [SerializeField] private float groundOffset = 0.02f;

        private ARPlaneWalkableManager walkableManager;
        private ARWalkablePlaneData currentPlaneData;

        private List<Vector3> availablePoints = new List<Vector3>();
        private Vector3 currentTarget;
        private bool isInitialized = false;
        private bool hasTarget = false;
        private float waitTimer = 0f;

        public void Initialize(ARPlaneWalkableManager manager, ARWalkablePlaneData planeData)
        {
            walkableManager = manager;
            currentPlaneData = planeData;

            if (walkableManager == null)
            {
                Debug.LogWarning("[ARNPCSimpleMover] WalkableManager es null.");
                return;
            }

            if (currentPlaneData == null)
            {
                Debug.LogWarning("[ARNPCSimpleMover] PlaneData es null.");
                return;
            }

            RefreshAvailablePoints();

            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("[ARNPCSimpleMover] No hay puntos disponibles en el grupo del plano.");
                return;
            }

            SnapToClosestPoint();
            PickNextTarget();

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            if (!hasTarget)
            {
                waitTimer += Time.deltaTime;

                if (waitTimer >= waitTimeAtPoint)
                {
                    waitTimer = 0f;
                    RefreshAvailablePoints();
                    PickNextTarget();
                }

                return;
            }

            MoveTowardsTarget();
        }

        private void RefreshAvailablePoints()
        {
            availablePoints.Clear();

            if (walkableManager == null || currentPlaneData == null)
                return;

            int groupKey = currentPlaneData.GroupKey;
            List<Vector3> points = walkableManager.GetPointsFromSameGroup(groupKey);

            if (points != null)
            {
                availablePoints.AddRange(points);
            }
        }

        private void PickNextTarget()
        {
            if (availablePoints == null || availablePoints.Count == 0)
            {
                hasTarget = false;
                return;
            }

            int randomIndex = Random.Range(0, availablePoints.Count);
            currentTarget = availablePoints[randomIndex];
            currentTarget.y += groundOffset;

            hasTarget = true;
        }

        private void MoveTowardsTarget()
        {
            Vector3 targetPosition = new Vector3(currentTarget.x, transform.position.y, currentTarget.z);
            Vector3 direction = targetPosition - transform.position;

            float distance = direction.magnitude;

            if (distance <= reachDistance)
            {
                hasTarget = false;
                return;
            }

            Vector3 moveDirection = direction.normalized;
            Vector3 nextPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

            // Mantener altura basada en el target real del plano
            nextPosition.y = currentTarget.y;

            transform.position = nextPosition;

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void SnapToClosestPoint()
        {
            if (availablePoints == null || availablePoints.Count == 0)
                return;

            float closestDistance = float.MaxValue;
            Vector3 closestPoint = transform.position;

            for (int i = 0; i < availablePoints.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, availablePoints[i]);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = availablePoints[i];
                }
            }

            closestPoint.y += groundOffset;
            transform.position = closestPoint;
        }

        private void OnDrawGizmosSelected()
        {
            if (!hasTarget) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentTarget, 0.04f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }
}
