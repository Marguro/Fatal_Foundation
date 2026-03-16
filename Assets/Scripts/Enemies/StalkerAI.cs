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
        [SerializeField] private float stareDuration = 10f;
        [Tooltip("Damage dealt after stare duration completes")]
        [SerializeField] private float heavyAttackDamage = 50f;
        [Tooltip("Distance to flee after being seen or attacking")]
        [SerializeField] private float fleeDistance = 15f;
        [Tooltip("Speed when fleeing")]
        [SerializeField] private float fleeSpeed = 6.0f;
        [Tooltip("Speed when hunting/following")]
        [SerializeField] private float huntSpeed = 3.5f;
        [Tooltip("Distance to start Staring behavior")]
        [SerializeField] private float closeDistance = 5f;
        [Tooltip("Cooldown before hunting again after fleeing")]
        [SerializeField] private float loopCooldown = 5.0f;
        [Tooltip("Angle cone in front of player to detect Stalker (degrees)")]
        [SerializeField] private float detectionAngle = 60f;

        private EnemyBase _enemyBase;
        private Transform _targetPlayer;
        
        // State Machine
        private enum StalkerState { Hunting, Staring, Fleeing, Cooldown }
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
            
            // Check if Stalker is within player's forward cone
            Vector3 dirToStalker = (transform.position - _targetPlayer.position).normalized;
            float angle = Vector3.Angle(_targetPlayer.forward, dirToStalker);
            
            // If angle is small enough, player is looking primarily at Stalker
            if (angle < detectionAngle)
            {
                 // Simple line of sight check
                 RaycastHit hit;
                 // Raycast from slightly above player feet (eye level approx 1.6m)
                 if (Physics.Raycast(_targetPlayer.position + Vector3.up * 1.6f, dirToStalker, out hit, 100f))
                 {
                     // If we hit the stalker (or part of it), it's seen
                     // We check root or transform match
                     if (hit.transform == transform || hit.transform.IsChildOf(transform)) return true;
                 }
            }
            return false;
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
            // Move partially behind player if possible, or just follow
            // To properly "creep behind", we want a point behind the player's back
            Vector3 targetPos = _targetPlayer.position - (_targetPlayer.forward * 2f); 
            
            // If very close to player, switch to Staring
            if (Vector3.Distance(transform.position, _targetPlayer.position) < closeDistance)
            {
                TransitionTo(StalkerState.Staring);
                return;
            }

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
            }

            // Check if seen
            if (IsSeenByPlayer())
            {
                Debug.Log("Stalker seen! Fleeing!");
                TransitionTo(StalkerState.Fleeing);
                return;
            }

            // Stare countdown
            _stareCounter -= Time.deltaTime;

            if (_stareCounter <= 0f)
            {
                // Attack!
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
             if (_targetPlayer)
             {
                 var health = _targetPlayer.GetComponent<HealthSystem>();
                 if (health != null)
                 {
                     Debug.Log($"Stalker attacking for {heavyAttackDamage} damage!");
                     _enemyBase.AttackTarget(health, heavyAttackDamage);
                 }
             }
             // After attack, flee and cooldown
             TransitionTo(StalkerState.Fleeing);
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
