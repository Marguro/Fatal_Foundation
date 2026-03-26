using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Enemies
{
    [RequireComponent(typeof(EnemyBase))]
    public class PatrolChaserAI : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 4.5f;
        [SerializeField] private float patrolWaitTime = 1.25f;
        [SerializeField] private float patrolPointReachedDistance = 0.5f;

        [Header("Detection")]
        [SerializeField] private float visionRange = 14f;
        [SerializeField, Range(0f, 180f)] private float visionAngle = 65f;
        [SerializeField] private float scanInterval = 0.2f;

        [Header("Chase")]
        [FormerlySerializedAs("loseTargetDistance")]
        [SerializeField] private float chaseDuration = 6f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 15f;

        [Header("Patrol")]
        [Tooltip("Optional patrol center points. If empty, enemy will roam around spawn point.")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float randomPatrolRadius = 8f;

        private EnemyBase enemyBase;
        private Transform targetPlayer;
        private Vector3 spawnPoint;
        private int currentPatrolIndex;
        private float nextPatrolMoveTime;
        private float nextScanTime;
        private float chaseEndTime;
        private bool isWaitingAtPatrolPoint;

        private enum State
        {
            Patrolling,
            Chasing,
            ReturningToPatrol
        }

        private State currentState = State.Patrolling;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            enemyBase = GetComponent<EnemyBase>();
            spawnPoint = transform.position;
            enemyBase.MoveSpeed = patrolSpeed;
        }

        private void Update()
        {
            if (!IsServer || enemyBase == null) return;

            if (targetPlayer != null && !IsTargetValid(targetPlayer))
            {
                targetPlayer = null;
            }

            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + scanInterval;
                if (targetPlayer == null)
                {
                    Transform visibleTarget = FindVisiblePlayer();
                    if (visibleTarget != null)
                    {
                        StartChase(visibleTarget);
                    }
                }
            }

            switch (currentState)
            {
                case State.Patrolling:
                    UpdatePatrol();
                    break;
                case State.Chasing:
                    UpdateChase();
                    break;
                case State.ReturningToPatrol:
                    UpdateReturnToPatrol();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (targetPlayer != null)
            {
                StartChase(targetPlayer);
                return;
            }

            PatrolRandomAroundCenter();
        }

        private void PatrolRandomAroundCenter()
        {
            if (enemyBase.Agent == null || !enemyBase.Agent.enabled || !enemyBase.Agent.isOnNavMesh) return;

            if (!enemyBase.Agent.pathPending && enemyBase.Agent.remainingDistance <= patrolPointReachedDistance)
            {
                if (!isWaitingAtPatrolPoint)
                {
                    nextPatrolMoveTime = Time.time + patrolWaitTime;
                    isWaitingAtPatrolPoint = true;
                }

                if (Time.time < nextPatrolMoveTime)
                {
                    enemyBase.StopMoving();
                    return;
                }

                isWaitingAtPatrolPoint = false;

                Vector3 randomOffset = Random.insideUnitSphere * randomPatrolRadius;
                randomOffset.y = 0f;
                Vector3 candidate = GetCurrentPatrolCenter() + randomOffset;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    enemyBase.MoveTo(hit.position);
                    AdvancePatrolIndex();
                }

                nextPatrolMoveTime = Time.time + patrolWaitTime;
            }
            else
            {
                isWaitingAtPatrolPoint = false;
            }
        }

        private void UpdateChase()
        {
            if (targetPlayer == null)
            {
                StartReturnToPatrol();
                return;
            }

            if (Time.time >= chaseEndTime)
            {
                targetPlayer = null;
                StartReturnToPatrol();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);

            enemyBase.MoveTo(targetPlayer.position);

            if (distanceToTarget <= attackRange && HasLineOfSight(targetPlayer))
            {
                if (TryGetHealthSystem(targetPlayer, out HealthSystem targetHealth))
                {
                    enemyBase.AttackTarget(targetHealth, attackDamage);
                }
            }
        }

        private void StartChase(Transform playerTarget)
        {
            targetPlayer = playerTarget;
            currentState = State.Chasing;
            enemyBase.MoveSpeed = chaseSpeed;
            isWaitingAtPatrolPoint = false;
            chaseEndTime = Time.time + chaseDuration;
        }

        private void StartReturnToPatrol()
        {
            currentState = State.ReturningToPatrol;
            enemyBase.MoveSpeed = chaseSpeed;
            isWaitingAtPatrolPoint = false;
        }

        private void UpdateReturnToPatrol()
        {
            Vector3 returnPosition = GetPatrolReturnPosition();
            enemyBase.MoveTo(returnPosition);

            if (Vector3.Distance(transform.position, returnPosition) <= patrolPointReachedDistance)
            {
                StartPatrolMode();
            }
        }

        private Vector3 GetPatrolReturnPosition()
        {
            return GetCurrentPatrolCenter();
        }

        private Vector3 GetCurrentPatrolCenter()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return spawnPoint;
            }

            int checkedCount = 0;
            int index = currentPatrolIndex;

            while (checkedCount < patrolPoints.Length)
            {
                Transform patrolCenter = patrolPoints[index];
                if (patrolCenter != null)
                {
                    currentPatrolIndex = index;
                    return patrolCenter.position;
                }

                index = (index + 1) % patrolPoints.Length;
                checkedCount++;
            }

            return spawnPoint;
        }

        private void StartPatrolMode()
        {
            targetPlayer = null;
            currentState = State.Patrolling;
            enemyBase.MoveSpeed = patrolSpeed;
            enemyBase.StopMoving();
            nextPatrolMoveTime = Time.time + patrolWaitTime;
            isWaitingAtPatrolPoint = false;
        }

        private Transform FindVisiblePlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            float bestDistance = float.MaxValue;
            Transform bestTarget = null;

            for (int i = 0; i < players.Length; i++)
            {
                Transform player = players[i].transform;
                if (!IsTargetValid(player)) continue;

                float distance = Vector3.Distance(transform.position, player.position);
                if (distance > visionRange || distance >= bestDistance) continue;
                if (!IsWithinVisionCone(player.position)) continue;
                if (!HasLineOfSight(player)) continue;

                bestDistance = distance;
                bestTarget = player;
            }

            return bestTarget;
        }

        private bool IsTargetValid(Transform candidate)
        {
            if (candidate == null) return false;

            if (!TryGetHealthSystem(candidate, out HealthSystem health)) return false;
            return health != null && health.currentHealth.Value > 0f;
        }

        private bool TryGetHealthSystem(Transform candidate, out HealthSystem health)
        {
            health = null;
            if (candidate == null) return false;

            // Support player setups where tag/collider object is a child while HealthSystem sits on root.
            health = candidate.GetComponent<HealthSystem>();
            if (health == null) health = candidate.GetComponentInParent<HealthSystem>();
            if (health == null) health = candidate.GetComponentInChildren<HealthSystem>();

            return health != null;
        }

        private bool IsWithinVisionCone(Vector3 worldPosition)
        {
            Vector3 direction = (worldPosition - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, direction);
            return angle <= visionAngle * 0.5f;
        }

        private bool HasLineOfSight(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 targetPoint = target.position + Vector3.up * 1.0f;
            Vector3 direction = (targetPoint - origin).normalized;
            float distance = Vector3.Distance(origin, targetPoint);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
            {
                return hit.transform == target || hit.transform.IsChildOf(target);
            }

            return false;
        }

        private void AdvancePatrolIndex()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }

            currentPatrolIndex++;
            if (currentPatrolIndex >= patrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, visionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Vector3 leftRay = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightRay = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, leftRay * visionRange);
            Gizmos.DrawRay(transform.position, rightRay * visionRange);
        }
    }
}


