using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    [RequireComponent(typeof(EnemyBase))]
    public class StalkerAI : NetworkBehaviour
    {
        [Header("Stalker Logic Settings")]
        [Tooltip("How long the Stalker stares before attacking (seconds)")]
        [SerializeField] private float stareDuration = 5f;
        [Tooltip("Damage dealt after stare duration completes")]
        [SerializeField] private float heavyAttackDamage = 30f;
        [Tooltip("Distance to flee after being seen or attacking")]
        [SerializeField] private float fleeDistance = 15f;
        [Tooltip("Speed when fleeing")]
        [SerializeField] private float fleeSpeed = 6.0f;
        [Tooltip("Speed when hunting/following")]
        [SerializeField] private float huntSpeed = 3.5f;
        [Tooltip("Speed when charging at player")]
        [SerializeField] private float chargeSpeed = 8.0f;
        [Tooltip("Distance to start Staring behavior")]
        [SerializeField] private float closeDistance = 5f;
        [Tooltip("Distance to start Charging")]
        [SerializeField] private float chargeStartDistance = 8f;
        [Tooltip("Cooldown before hunting again after fleeing")]
        [SerializeField] private float loopCooldown = 5.0f;
        [Tooltip("Angle cone in front of player to detect Stalker (degrees)")]
        [SerializeField] private float detectionAngle = 60f;
        [Tooltip("Max distance player can see Stalker")]
        [SerializeField] private float maxDetectionDistance = 25f;

        private EnemyBase _enemyBase;
        private Transform _targetPlayer;
        
        // State Machine
        private enum StalkerState { Hunting, Staring, Charging, Fleeing, Cooldown }
        private StalkerState _currentState = StalkerState.Hunting;
        private float _stateTimer;
        private float _stareCounter;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            _enemyBase = GetComponent<EnemyBase>();
            _enemyBase.MoveSpeed = huntSpeed;
            _stareCounter = stareDuration;
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize detection logic
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, closeDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chargeStartDistance);

            Gizmos.color = Color.cyan;
            if (_targetPlayer != null)
            {
                // Draw cone lines
                Vector3 forward = _targetPlayer.forward;
                Vector3 playerPos = _targetPlayer.position;
                Quaternion leftRayRotation = Quaternion.AngleAxis(-detectionAngle, Vector3.up);
                Quaternion rightRayRotation = Quaternion.AngleAxis(detectionAngle, Vector3.up);
                Vector3 leftRayDirection = leftRayRotation * forward;
                Vector3 rightRayDirection = rightRayRotation * forward;
                
                Gizmos.DrawRay(playerPos, leftRayDirection * maxDetectionDistance);
                Gizmos.DrawRay(playerPos, rightRayDirection * maxDetectionDistance);
                Gizmos.DrawLine(playerPos, transform.position);
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            // Ensure Agent is on NavMesh to prevent "not moving" and errors
            if (_enemyBase.Agent != null && !_enemyBase.Agent.isOnNavMesh)
            {
                // Try to warp to nearest navmesh
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    _enemyBase.Agent.Warp(hit.position);
                    _enemyBase.Agent.isStopped = false; // Reset stop state
                }
            }

            // Continually find nearest player if no target or check occasionally
            // For simplicity, we check every frame if null, otherwise just verify distance
            if (_targetPlayer == null)
            {
                FindNearestPlayer();
                if (_targetPlayer == null) return; // No players found
            }

            switch (_currentState)
            {
                case StalkerState.Hunting:
                    UpdateHunting();
                    break;
                case StalkerState.Staring:
                    UpdateStaring();
                    break;
                case StalkerState.Charging:
                    UpdateCharging();
                    break;
                case StalkerState.Fleeing:
                    UpdateFleeing();
                    break;
                case StalkerState.Cooldown: // Waiting to hunt again
                    if (Time.time >= _stateTimer)
                    {
                        TransitionTo(StalkerState.Hunting);
                    }
                    break;
            }
        }

        private void FindNearestPlayer()
        {
            var players = GameObject.FindGameObjectsWithTag("Player");
            float minDst = float.MaxValue;
            Transform bestTarget = null;
            
            foreach (var p in players)
            {
                if (p == null) continue; 
                float dst = Vector3.Distance(transform.position, p.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    bestTarget = p.transform;
                }
            }
            _targetPlayer = bestTarget;
        }

        private bool IsSeenByPlayer()
        {
            if (_targetPlayer == null) return false;

            // Use Camera.main if available for more accurate view detection (FPS/TPS)
            Transform viewer = Camera.main != null ? Camera.main.transform : _targetPlayer;

            // Distance check
            float dist = Vector3.Distance(transform.position, viewer.position);
            if (dist > maxDetectionDistance) return false;
            
            // Check if Stalker is within player's forward cone
            Vector3 dirToStalker = (transform.position - viewer.position).normalized;
            float angle = Vector3.Angle(viewer.forward, dirToStalker);
            
            // If angle is small enough, player is looking primarily at Stalker
            if (angle < detectionAngle)
            {
                 // Simple line of sight check
                 RaycastHit hit;
                 // Raycast from viewer position
                 if (Physics.Raycast(viewer.position, dirToStalker, out hit, maxDetectionDistance))
                 {
                     // If we hit the stalker (or part of it), it's seen
                     // We check root or transform match
                     if (hit.transform == transform || hit.transform.IsChildOf(transform)) return true;
                 }
            }
            return false;
        }

        private bool HasLineOfSightToPlayer()
        {
            if (_targetPlayer == null) return false;
            
            // Check for obstacles between Stalker and Player
            // Raycast from eye level (approx 1.5m) to player eye level
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = _targetPlayer.position + Vector3.up * 1.5f;
            Vector3 direction = (target - origin).normalized;
            float distance = Vector3.Distance(origin, target);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
            {
                // If we hit something that is NOT the player (and not ourselves/child), line of sight is blocked
                if (hit.transform != _targetPlayer && !hit.transform.IsChildOf(_targetPlayer) && 
                    hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    return false; // Blocked
                }
            }
            return true; // Clear line of sight
        }

        private void TransitionTo(StalkerState newState)
        {
            _currentState = newState;
            switch (newState)
            {
                case StalkerState.Hunting:
                    _enemyBase.MoveSpeed = huntSpeed;
                    break;
                case StalkerState.Staring:
                    _enemyBase.StopMoving();
                    _stareCounter = stareDuration;
                    Debug.Log("Stalker started staring...");
                    // Face player for dramatic effect
                    if (_targetPlayer) transform.LookAt(_targetPlayer);
                    break;
                case StalkerState.Charging:
                    _enemyBase.MoveSpeed = chargeSpeed;
                    Debug.Log("Stalker CHARGING!");
                    break;
                case StalkerState.Fleeing:
                    _enemyBase.MoveSpeed = fleeSpeed;
                    
                    if (_targetPlayer)
                    {
                        // Calculate flee position away from player
                        Vector3 evadeDir = (transform.position - _targetPlayer.position).normalized;
                        Vector3 fleePos = transform.position + evadeDir * fleeDistance;
                        
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(fleePos, out hit, 10f, NavMesh.AllAreas))
                        {
                            _enemyBase.MoveTo(hit.position);
                        }
                        else
                        {
                            // Fallback if NavMesh not found exactly there
                            _enemyBase.MoveTo(transform.position + evadeDir * 5f);
                        }
                    }
                    break;
                case StalkerState.Cooldown:
                    _enemyBase.StopMoving();
                    _stateTimer = Time.time + loopCooldown;
                    break;
            }
        }

        private void UpdateHunting()
        {
            if (_targetPlayer == null) return;

            // Strategy: 
            // 1. If seen, try to find cover immediately (hide behind obstacles)
            // 2. If close enough, Stare -> Charge
            // 3. Else, creep behind player

            float distToPlayer = Vector3.Distance(transform.position, _targetPlayer.position);

            // Check vision first - prioritize hiding if seen while hunting
            if (IsSeenByPlayer())
            {
                 // Simple hide: Find a spot away or behind cover
                 // For now, simpler flee to break LOS
                 Debug.Log("Seen while hunting! Trying to hide.");
                 TransitionTo(StalkerState.Fleeing); 
                 return;
            }

            // If close enough and unseen, start staring sequence
            if (distToPlayer < closeDistance)
            {
                TransitionTo(StalkerState.Staring);
                return;
            }

            // Default: Creep behind player
            Vector3 targetPos = _targetPlayer.position - (_targetPlayer.forward * 4f); // Stay further back (4m) to be stealthy
            _enemyBase.MoveTo(targetPos);
        }

        private void UpdateStaring()
        {
            // Keep looking at player
            if (_targetPlayer)
            {
                Vector3 lookPos = _targetPlayer.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);

                // Line of Sight Check (Obstacle Check)
                // If something blocks the view while staring (e.g., player went around a corner),
                // stop staring and go back to hunting to find a better angle.
                if (!HasLineOfSightToPlayer())
                {
                     Debug.Log("Stalker lost line of sight while staring. Repositioning...");
                     TransitionTo(StalkerState.Hunting);
                     return;
                }
            }

            // Check if seen -> Flee immediately (Player "staring back" scares it off)
            if (IsSeenByPlayer())
            {
                Debug.Log("Stalker seen while staring! Fleeing!");
                TransitionTo(StalkerState.Fleeing);
                return;
            }

            // Stare countdown
            _stareCounter -= Time.deltaTime;

            if (_stareCounter <= 0f)
            {
                // Action: Charge Attack!
                TransitionTo(StalkerState.Charging);
            }
        }

        private void UpdateCharging()
        {
             if (_targetPlayer == null) 
             {
                 TransitionTo(StalkerState.Cooldown);
                 return;
             }

             // Obstacle check during charge:
             // If player hides behind a wall while we are charging, we might hit the wall instead.
             // Or if we simply lost sight before hitting them.
             if (!HasLineOfSightToPlayer() && Vector3.Distance(transform.position, _targetPlayer.position) > 3f)
             {
                 // Lost sight mid-charge and not close enough to finish the hit.
                 // Go back to hunting to find them again.
                 Debug.Log("Lost sight during charge! Repositioning.");
                 TransitionTo(StalkerState.Hunting);
                 return;
             }

             // Rush towards player
             _enemyBase.MoveTo(_targetPlayer.position);

             float dist = Vector3.Distance(transform.position, _targetPlayer.position);
             
             // Attack range check
             if (dist < 1.5f)
             {
                 var health = _targetPlayer.GetComponent<HealthSystem>();
                 if (health != null)
                 {
                     Debug.Log($"Stalker CHARGE HIT for {heavyAttackDamage} damage!");
                     _enemyBase.AttackTarget(health, heavyAttackDamage);
                 }
                 // Hit and Run
                 TransitionTo(StalkerState.Fleeing);
             }
        }
        
        private void UpdateFleeing()
        {
            // Check if reached destination or far enough
            if (_enemyBase.Agent != null && _enemyBase.Agent.isOnNavMesh)
            {
                if (_enemyBase.Agent.remainingDistance < 1f)
                {
                    TransitionTo(StalkerState.Cooldown);
                }
            }
            else
            {
                // If agent is not valid or not on NavMesh, exit state to avoid getting stuck
                TransitionTo(StalkerState.Cooldown);
            }
        }
    }
}
