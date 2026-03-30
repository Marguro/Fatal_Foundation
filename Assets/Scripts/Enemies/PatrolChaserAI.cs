using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace Enemies
{
    [RequireComponent(typeof(EnemyBase))]
    public class PatrolChaserAI : NetworkBehaviour
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;
        private readonly int speedAnimParam = Animator.StringToHash("Speed");
        private readonly int attackAnimParam = Animator.StringToHash("Attack");

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
            if (animator == null) animator = GetComponentInChildren<Animator>();
            
            spawnPoint = transform.position;
            enemyBase.MoveSpeed = patrolSpeed;
        }

        public void SetPatrolPoints(Transform[] newPoints)
        {
            patrolPoints = newPoints;
            currentPatrolIndex = 0;
            // Optionally force return to patrol right away
            if (currentState == State.Patrolling)
            {
                isWaitingAtPatrolPoint = false;
                nextPatrolMoveTime = Time.time;
            }
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

            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (animator == null || enemyBase.Agent == null) return;
            
            // อัปเดตความเร็วลงใน Animator
            float currentVelocity = enemyBase.Agent.velocity.magnitude;
            animator.SetFloat(speedAnimParam, currentVelocity);
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

            bool inSight = HasLineOfSight(targetPlayer);
            if (inSight)
            {
                chaseEndTime = Time.time + chaseDuration;
            }

            if (Time.time >= chaseEndTime)
            {
                targetPlayer = null;
                StartReturnToPatrol();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);

            enemyBase.MoveTo(targetPlayer.position);

            if (distanceToTarget <= attackRange && inSight)
            {
                if (TryGetHealthSystem(targetPlayer, out HealthSystem targetHealth))
                {
                    enemyBase.AttackTarget(targetHealth, attackDamage);
                    if (animator != null) animator.SetTrigger(attackAnimParam);
                    
                    // หน่วงเวลาไม่ให้โจมตีรัวเกินไป (ถ้าระบบ EnemyBase ยังไม่มี cooldown)
                    enemyBase.StopMoving();
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
            float bestDistance = float.MaxValue;
            Transform bestTarget = null;

            List<Transform> candidates = GetPlayerCandidates();
            for (int i = 0; i < candidates.Count; i++)
            {
                Transform player = candidates[i];
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

        private List<Transform> GetPlayerCandidates()
        {
            var candidates = new List<Transform>();
            NetworkManager networkManager = NetworkManager.Singleton;

            if (networkManager != null && networkManager.IsServer)
            {
                var clients = networkManager.ConnectedClientsList;
                for (int i = 0; i < clients.Count; i++)
                {
                    NetworkObject playerObject = clients[i].PlayerObject;
                    if (playerObject == null) continue;
                    candidates.Add(playerObject.transform);
                }
            }

            // Fallback keeps local test scenes working even without NGO player objects.
            if (candidates.Count == 0)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i] != null)
                    {
                        candidates.Add(players[i].transform);
                    }
                }
            }

            return candidates;
        }

        private bool IsTargetValid(Transform candidate)
        {
            if (candidate == null) return false;

            // Some player prefabs may not carry HealthSystem on the same transform hierarchy.
            // In that case we still allow chasing; damage will only apply when health is present.
            if (!TryGetHealthSystem(candidate, out HealthSystem health)) return true;
            return health.currentHealth.Value > 0f;
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

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance);
            if (hits == null || hits.Length == 0)
            {
                return true;
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform == null) continue;
                if (hits[i].collider != null && hits[i].collider.isTrigger) continue;

                // Ignore own collider hierarchy.
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                return hitTransform == target || hitTransform.IsChildOf(target);
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

        private void OnDrawGizmos()
        {
            // แสดงวงกลมระยะโจมตีตลอดเวลาด้วยสีแดง
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, visionRange);

            Vector3 leftRay = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
            Vector3 rightRay = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, leftRay * visionRange);
            Gizmos.DrawRay(transform.position, rightRay * visionRange);
        }
    }
}

