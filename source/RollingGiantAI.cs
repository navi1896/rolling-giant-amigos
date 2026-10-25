using GameNetcodeStuff;
using RollingGiant.Settings;
using System.Diagnostics.CodeAnalysis;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace RollingGiant;

public class RollingGiantAI : EnemyAI {
   private const float ROAMING_AUDIO_PERCENT = 0.4f;

#pragma warning disable 649
   [SerializeField] private AISearchRoutine _searchForPlayers;
   [SerializeField] private Collider _mainCollider;
   [SerializeField] private AudioClip[] _stopNoises;
#pragma warning restore 649

   private static RollingGiantAiType _aiType => NetworkHandler.AiType;
   private static SharedAiSettings _sharedAiSettings => CustomConfig.SharedAiSettings;
   
   private AudioSource _rollingSFX;
   private NetworkVariable<float> _velocity = new();
   private NetworkVariable<float> _waitTimer = new();
   private NetworkVariable<float> _moveTimer = new();
   private NetworkVariable<float> _lookTimer = new();
   private NetworkVariable<float> _agroTimer = new();

   private float _timeSinceHittingPlayer;
   private bool _wantsToChaseThisClient;
   private bool _hasEnteredChaseState;
   private bool _wasStopped;
   private bool _wasFeared;
   private bool _isAgro;
   private float _lastSpeed;
   private bool _tooBig;
   private PlayerControllerB? _lastChaseTarget;
   private bool _factoryExitRequestPending;
   private bool _hasExitedFactory;
   private float _retryFactoryExitRequestAfter;

   public bool IsPlayerControlled { get; set; }
   public float PlayerControlledSpeed { get; set; }
   public bool ForceMoveWhileLookedAt { get; set; }

   public bool IsStoppedByPlayerLook => _wasStopped && !ForceMoveWhileLookedAt;

   public void PlayLocalStopSound() {
      if (_stopNoises != null && _stopNoises.Length > 0 && creatureVoice != null) {
         RoundManager.PlayRandomClip(creatureVoice, _stopNoises, false);
      }
   }

   public bool TryTeleportOutsideFromController(out Vector3 destination) {
      destination = transform.position;
      if (!IsServer || Time.time < _retryFactoryExitRequestAfter) return false;

      var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
      foreach (var currentSideEntrance in entrances) {
         if (currentSideEntrance == null) continue;
         var doorPos = currentSideEntrance.entrancePoint != null ? currentSideEntrance.entrancePoint.position : currentSideEntrance.transform.position;
         if (Vector3.Distance(transform.position, doorPos) > 5f && Vector3.Distance(transform.position, currentSideEntrance.transform.position) > 5f) continue;

         EntranceTeleport? targetEntrance = currentSideEntrance.exitScript;
         if (targetEntrance == null)
         {
            foreach (var candidate in entrances)
            {
               if (candidate != null && candidate.entranceId == currentSideEntrance.entranceId && candidate.isEntranceToBuilding != currentSideEntrance.isEntranceToBuilding)
               {
                  targetEntrance = candidate;
                  break;
               }
            }
         }
         if (targetEntrance == null) continue;
         var arrivalPosition = targetEntrance.entrancePoint != null ? targetEntrance.entrancePoint.position : targetEntrance.transform.position;
         if (NavMesh.SamplePosition(arrivalPosition, out var navHit, 6f, NavMesh.AllAreas)) {
            arrivalPosition = navHit.position;
         }

         _retryFactoryExitRequestAfter = Time.time + 2f;
         FollowPlayerOutsideClientRpc(arrivalPosition, targetEntrance.transform.eulerAngles.y, 0);
         destination = arrivalPosition;
         return true;
      }

      return false;
   }

   private static void LogInfo(object message) {
#if DEBUG
      Plugin.Log.LogInfo(message);
#endif
   }

   private static float NextDouble() {
      if (!RoundManager.Instance || RoundManager.Instance.LevelRandom == null) {
         return Random.value;
      }
      
      return (float)RoundManager.Instance.LevelRandom.NextDouble();
   }

   public override void Start() {
      base.Start();
      
      Init(transform.localScale.x);
      
      if (IsHost || IsOwner) {
         AssignInitData_LocalClient();
      }
      
      LogInfo($"Rolling giant spawned with ai type: {NetworkHandler.AiType}, owner? {IsOwner}");
   }

   private void Init(float scale) {
      agent = gameObject.GetComponentInChildren<NavMeshAgent>();
      _rollingSFX = transform.Find("RollingSFX").GetComponent<AudioSource>();
      
      var mixer = SoundManager.Instance.diageticMixer.outputAudioMixerGroup;
      _rollingSFX.outputAudioMixerGroup = mixer;
      creatureVoice.outputAudioMixerGroup = mixer;
      creatureSFX.outputAudioMixerGroup = mixer;
      
      _rollingSFX.loop = true;
      _rollingSFX.clip = Plugin.WalkSound;
      
      var time = NextDouble() * Plugin.WalkSound.length;
      _rollingSFX.time = time;
      _rollingSFX.pitch = Mathf.Lerp(1.1f, 0.8f, Mathf.InverseLerp(0.9f, 1.2f, scale));
      _rollingSFX.volume = 0;
      _rollingSFX.Play();
      
      isOutside = enemyType.isOutsideEnemy || enemyType.isDaytimeEnemy;
      if (isOutside) {
         allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
      } else
        {
            allAINodes = GameObject.FindGameObjectsWithTag("AINode");
        }
   }

   public void ResetValues() {
      if (IsHost || IsServer) {
         _waitTimer.Value = 0;
         _moveTimer.Value = 0;
         _lookTimer.Value = 0;
         _agroTimer.Value = 0;
         SwitchToBehaviourState(0);
         EndChasingPlayer_ClientRpc();
         ResetValues_ClientRpc();
      }
   }

   [ClientRpc]
   private void ResetValues_ClientRpc() {
      _isAgro = false;
      _wasStopped = false;
      _wasFeared = false;
   }

   public override void DaytimeEnemyLeave() {
      base.DaytimeEnemyLeave();
      
      foreach (var renderer in transform.GetComponentsInChildren<Renderer>()) {
         if (renderer.name == "object_3") continue;
         renderer.sharedMaterial = Plugin.BlackAndWhiteMaterial;
      }

      _mainCollider.isTrigger = true;
   }

   public override void DoAIInterval() {
      if (daytimeEnemyLeaving) {
         _mainCollider.isTrigger = true;
         return;
      }
      
      base.DoAIInterval();
      
      if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead) return;
      
      // LogInfo($"{agent.speed}, {_velocity.Value}");
   
      if (IsPlayerControlled) {
         _wasStopped = AmIBeingLookedAt(out _) && !ForceMoveWhileLookedAt;
         if (!_wasStopped) {
            _wasFeared = false;
         }
         return;
      }

      switch (currentBehaviourStateIndex) {
         // searching
         case 0:
            if (!IsServer) {
               ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
               break;
            }
   
            if (!_searchForPlayers.inProgress) {
               StartSearch(transform.position, _searchForPlayers);
               LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] StartSearch({transform.position}, _searchForPlayers)");
               return;
            }

            foreach (var player in StartOfRound.Instance.allPlayerScripts) {
               if (player.isPlayerDead) continue;
               if (!player.IsSpawned) continue;
               if (isOutside == player.isInsideFactory) continue;
               if (!isOutside && !PlayerIsTargetable(player)) continue;

               var distance = Vector3.Distance(transform.position, player.transform.position);
               var inRange = distance < (isOutside ? 90 : 30);
               if (!Physics.Linecast(transform.position + Vector3.up * 0.5f, player.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && inRange) {
                  SwitchToBehaviourState(1);
                  LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] SwitchToBehaviourState(1), found {player?.playerUsername} at distance {distance}m");
                  return;
               }
            }
            
            if (isOutside) {
               var closest = GetClosestPlayer();
               if (closest && !Physics.Linecast(transform.position + Vector3.up * 0.5f, closest.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
                  var distance = Vector3.Distance(transform.position, closest.transform.position);
                  var inRange = distance < (isOutside ? 90 : 30);
                  if (inRange) {
                     targetPlayer = closest;
                     SwitchToBehaviourState(1);
                     LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] ClosestPlayer! SwitchToBehaviourState(1), found {targetPlayer?.playerUsername}");
                  }
               }
            }
            break;
         // chasing
         case 1:
            // not in range of any player, so go back to wandering
            if (!TargetClosestPlayer()) {
               movingTowardsTargetPlayer = false;
               if (_searchForPlayers.inProgress) {
                  break;
               }
               SwitchToBehaviourState(0);
               LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] lost player; StartSearch({transform.position}, _searchForPlayers)");
               break;
            }
   
            if (!_searchForPlayers.inProgress) {
               break;
            }
   
            // stop the current search as we found a player!
            StopSearch(_searchForPlayers);
            movingTowardsTargetPlayer = true;
            LogInfo($"[DoAIInterval::{NetworkHandler.AiType}] StopSearch(_searchForPlayers), found {targetPlayer?.playerUsername}");
            break;
      }
   }
   
   public override void Update() {
      if (daytimeEnemyLeaving) {
         _mainCollider.isTrigger = true;
         _rollingSFX.volume = 0;
         return;
      }
   
      if (IsHost || IsServer) {
         _velocity.Value = IsPlayerControlled ? PlayerControlledSpeed : agent.velocity.magnitude;
      }
   
      base.Update();
   
      if (isEnemyDead) return;
   
      _lastSpeed = _velocity.Value;
      if (!IsPlayerControlled) {
         CalculateAgentSpeed();
      }
      
      _timeSinceHittingPlayer += Time.deltaTime;
      _mainCollider.isTrigger = _velocity.Value > 0.01f || IsPlayerControlled;
      
      var speed = _velocity.Value;
      // LogInfo($"[Update::{NetworkHandler.AiType}] _wasStopped: {_wasStopped}, speed: {speed}/{_sharedAiSettings.moveSpeed}, moveTowardsDestination: {moveTowardsDestination}: movingTowardsTargetPlayer: {movingTowardsTargetPlayer}, onNavmesh: {agent.isOnNavMesh}, isActiveAndEnabled: {agent.isActiveAndEnabled}");
      // LogInfo($"[Update::{NetworkHandler.AiType}] speed: {speed}/{_sharedAiSettings.moveSpeed}, _rollingSFX.volume: {_rollingSFX.volume}");
      if (speed > 0.1f) {
         _rollingSFX.volume = Mathf.Lerp(0, Mathf.Clamp01(ROAMING_AUDIO_PERCENT * speed + 0.05f), speed / _sharedAiSettings.moveSpeed);
      } else {
         _rollingSFX.volume = Mathf.Lerp(_rollingSFX.volume, 0, Time.deltaTime);
      }
   
      var gameNetworkManager = GameNetworkManager.Instance;
      var localPlayer = gameNetworkManager.localPlayerController;
      if (_wasStopped && !_wasFeared) {
         if (localPlayer.HasLineOfSightToPosition(eye.position, 70, 25)) {
            _wasFeared = true;
            // LogInfo($"[Update] feared");
   
            var distance = Vector3.Distance(transform.position, localPlayer.transform.position);
            if (distance < 4) {
               gameNetworkManager.localPlayerController.JumpToFearLevel(0.9f);
            } else if (distance < 9) {
               gameNetworkManager.localPlayerController.JumpToFearLevel(0.4f);
            }
   
            if (_lastSpeed > 1) {
               RoundManager.PlayRandomClip(creatureVoice, _stopNoises, false);
               // LogInfo($"[Update::{_sharedAiSettings.aiType}] _lastSpeed: {_lastSpeed}");
            }
   
            // LogInfo($"[Update::{_sharedAiSettings.aiType}] _wasStopped: {_wasStopped}, _wasFeared: {_wasFeared}, _lastSpeed: {_lastSpeed}");
         }
      }
   
      if (IsPlayerControlled) {
         _wasStopped = AmIBeingLookedAt(out _) && !ForceMoveWhileLookedAt;
         if (!_wasStopped) {
            _wasFeared = false;
         }
         return;
      }

      switch (currentBehaviourStateIndex) {
         // searching
         case 0:
         {
            if (_hasEnteredChaseState) {
               _hasEnteredChaseState = false;
               _wantsToChaseThisClient = false;
               _wasStopped = false;
               _wasFeared = false;
               agent.speed = 0;
               if (_aiType is not RollingGiantAiType.OnceSeenAgroAfterTimer) {
                  _isAgro = false;
                  if (IsOwner) {
                     _agroTimer.Value = 0;
                  }
               }
               if (IsOwner) {
                  _waitTimer.Value = 0;
                  _moveTimer.Value = 0;
                  _lookTimer.Value = 0;
               }
            }
            
            if (IsOwner) {
               if (TargetClosestPlayer(requireLineOfSight: true)) {
                  if (_wantsToChaseThisClient) {
                     break;
                  }

                  _wantsToChaseThisClient = true;
                  BeginChasingPlayer_ServerRpc((int)targetPlayer.playerClientId);
                  LogInfo($"[Update::{NetworkHandler.AiType}] began chasing local player {targetPlayer?.playerUsername}");
               }
            }
         }
            break;
         // chasing
         case 1:
         {
            if (!_hasEnteredChaseState) {
               _hasEnteredChaseState = true;
               _wantsToChaseThisClient = false;
               _wasStopped = false;
               _wasFeared = false;
               if (_aiType is not RollingGiantAiType.OnceSeenAgroAfterTimer) {
                  _isAgro = false;
                  if (IsOwner) {
                     _agroTimer.Value = 0;
                  }
               }
               if (IsOwner) {
                  _waitTimer.Value = 0;
                  _moveTimer.Value = 0;
                  _lookTimer.Value = 0;
               }
            }
   
            if (stunNormalizedTimer > 0) {
               break;
            }

            var lastPlayer = targetPlayer;
            if (IsOwner) {
               if (lastPlayer != null && lastPlayer.isInsideFactory != isOutside) {
                  _lastChaseTarget = lastPlayer;
               }

               // An interior/exterior Giant cannot pathfind across the disconnected indoor
               // and outdoor navmeshes. If its chased player crosses a nearby entrance, move this
               // same networked Giant to the paired entrance and keep chasing.
               var playerLeaving = lastPlayer != null ? lastPlayer : _lastChaseTarget;
               if (TryRequestFactoryExit(playerLeaving)) {
                  break;
               }

               // nobody is in range so go back to searching
               if (!isOutside && !TargetClosestPlayer()) {
                  _lastChaseTarget = null;
                  SwitchToBehaviourState(0);
                  EndChasingPlayer_ServerRpc();
                  LogInfo($"[Update::{NetworkHandler.AiType}] not in range; SwitchToBehaviourState(0)");
                  break;
               }

               if (isOutside && !TargetClosestPlayer()) {
                  var player = GetClosestPlayer();
                  if (!player) {
                     SwitchToBehaviourState(0);
                     EndChasingPlayer_ServerRpc();
                     LogInfo($"[Update::{NetworkHandler.AiType}] not in range; SwitchToBehaviourState(0)");
                     break;
                  }
                  targetPlayer = player;
               }
               
               if (_wasStopped && _sharedAiSettings.rotateToLookAtPlayer) {
                  if (_lookTimer.Value >= _sharedAiSettings.delayBeforeLookingAtPlayer) {
                     // rotate visuals to look at player
                     var lookAt = targetPlayer.transform.position;
                     var position = transform.position;
                     var dir = lookAt - position;
                     dir.y = 0;
                     dir.Normalize();
   
                     var quaternion = Quaternion.LookRotation(dir);
                     transform.rotation = Quaternion.Lerp(transform.rotation, quaternion, Time.deltaTime / _sharedAiSettings.lookAtPlayerDuration);
                  }
               }
            }
   
            var aiType = NetworkHandler.AiType;
            switch (aiType) {
               case RollingGiantAiType.Coilhead:
                  if (AmIBeingLookedAt(out _)) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.InverseCoilhead:
                  if (!AmIBeingLookedAt(out _) && _isAgro && CheckLineOfSightToAnyPlayer()) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.RandomlyMoveWhileLooking:
                  if (AmIBeingLookedAt(out _) && _moveTimer.Value <= 0) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.LookingTooLongKeepsAgro:
                  if (AmIBeingLookedAt(out _) && _agroTimer.Value < 1f) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               case RollingGiantAiType.FollowOnceAgro:
                  // ?
                  break;
               case RollingGiantAiType.OnceSeenAgroAfterTimer:
                  if (_isAgro && _agroTimer.Value > 0) {
                     _wasStopped = true;
                     return;
                  }
                  break;
               default:
                  Plugin.Log.LogWarning($"Unknown ai type: {aiType}");
                  break;
            }
   
            _wasStopped = false;
            _wasFeared = false;
            // LogInfo($"[Update::{aiType}] _wasStopped: {_wasStopped}, _wasFeared: {_wasFeared}, _lastSpeed: {_lastSpeed}");

            // if (IsOwner) {
            //    if (lastPlayer != targetPlayer) {
            //       SetMovingTowardsTargetPlayer(targetPlayer);
            //       // ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
            //       LogInfo($"[Update::{NetworkHandler.AiType}] SetMovingTowardsTargetPlayer, player {targetPlayer?.playerUsername}");
            //    }
            // }

            if (IsOwner) {
               if (lastPlayer != targetPlayer) {
                  SetMovingTowardsTargetPlayer(targetPlayer);
                  LogInfo($"[Update::{NetworkHandler.AiType}] SetMovingTowardsTargetPlayer, player {targetPlayer?.playerUsername}");
               }
            }

            // if (!IsOwner || lastPlayer == targetPlayer || targetPlayer != localPlayer) {
            //    return;
            // }
         }
            break;
      }
   }

   // public bool GetClosestPlayer() {
   //    this.targetPlayer = null;
   //    
   //    // did the player see the giant
   //    if (AmIBeingLookedAt(out var closestPlayer)) {
   //       targetPlayer = closestPlayer;
   //       return closestPlayer;
   //    }
   //    
   //    var closestDistance = 2000f;
   //    for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; ++i) {
   //       var player = StartOfRound.Instance.allPlayerScripts[i];
   //       if (player.isPlayerDead) continue;
   //       if (!player.IsSpawned) continue;
   //       
   //       var position = player.transform.position;
   //       
   //       // did the giant see this player
   //       var pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       // if (!pathIsIntersectedByLineOfSight) {
   //       //    var closestNode = ChooseClosestNodeToPosition(player.transform.position, avoidLineOfSight: false);
   //       //    if (closestNode) {
   //       //       position = closestNode.position;
   //       //       pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       //    }
   //       // }
   //       
   //       if (!pathIsIntersectedByLineOfSight) {
   //          continue;
   //       }
   //       
   //       if (isOutside == player.isInsideFactory) {
   //          continue;
   //       }
   //       
   //       var distance = Vector3.Distance(transform.position, position);
   //       if (distance < closestDistance) {
   //          closestDistance = distance;
   //          targetPlayer = player;
   //       }
   //    }
   //
   //    return targetPlayer;
   // }
   
   // public new bool TargetClosestPlayer(float bufferDistance = 1.5f, bool requireLineOfSight = false, float viewWidth = 70f) {
   //    mostOptimalDistance = 2000;
   //    var targetPlayer = this.targetPlayer;
   //    this.targetPlayer = null;
   //    for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; ++i) {
   //       var player = StartOfRound.Instance.allPlayerScripts[i];
   //       if (player.isPlayerDead) continue;
   //       if (!player.IsSpawned) continue;
   //       // if (isOutside && player.isInsideFactory) continue;
   //
   //       var position = player.transform.position;
   //       var pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       // if (isOutside) {
   //       //    if (pathIsIntersectedByLineOfSight) {
   //       //       // get closest navmesh point
   //       //       var closestNode = ChooseClosestNodeToPosition(player.transform.position, avoidLineOfSight: false);
   //       //       if (closestNode) {
   //       //          position = closestNode.position;
   //       //          pathIsIntersectedByLineOfSight = PathIsIntersectedByLineOfSight(position, avoidLineOfSight: false);
   //       //       }
   //       //       
   //       //       // ? this gets modified above so this is resetting it otherwise it is too low
   //       //       mostOptimalDistance += 10;
   //       //    }
   //       // }
   //       
   //       var hasLineOfSightToPosition = HasLineOfSightToPosition(player.gameplayCamera.transform.position, viewWidth, 40);
   //       if (!pathIsIntersectedByLineOfSight && (!requireLineOfSight || hasLineOfSightToPosition)) {
   //          tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
   //          if (tempDist < (double)mostOptimalDistance) {
   //             // LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] [{i}] ({player?.playerUsername}) tempDist: {tempDist}, mostOptimalDistance: {mostOptimalDistance}");
   //             mostOptimalDistance = tempDist;
   //             this.targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
   //          } else {
   //             // LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] [{i}] ({player?.playerUsername}) tempDist: {tempDist}, mostOptimalDistance: {mostOptimalDistance}");
   //          }
   //       } else {
   //          // LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] [{i}] ({player?.playerUsername}) pathIsIntersectedByLineOfSight: {pathIsIntersectedByLineOfSight}, hasLineOfSightToPosition: {hasLineOfSightToPosition}");
   //       }
   //    }
   //
   //    if (this.targetPlayer && this.targetPlayer != targetPlayer && bufferDistance > 0.0 && targetPlayer && Mathf.Abs(mostOptimalDistance - Vector3.Distance(transform.position, targetPlayer.transform.position)) < (double)bufferDistance) {
   //       this.targetPlayer = targetPlayer;
   //    } else if (this.targetPlayer != targetPlayer) {
   //       // LogInfo($"[TargetClosestPlayer::{NetworkHandler.AiType}] this.targetPlayer: {this.targetPlayer?.playerUsername}, targetPlayer: {targetPlayer?.playerUsername}, mostOptimalDistance: {mostOptimalDistance}, bufferDistance: {bufferDistance}, distance: {(targetPlayer ? Vector3.Distance(transform.position, targetPlayer.transform.position) : -1)}");
   //    }
   //
   //    return this.targetPlayer;
   // }

   private static float SmoothLerp(float a, float b, float t) {
      return a + (t * t) * (b - a);
   }

   private bool TryRequestFactoryExit(PlayerControllerB? player) {
      if (player == null || player.isPlayerDead || !player.IsSpawned) {
         return false;
      }
      // Only transition when the player is on the opposite side of the door from the Giant
      if (player.isInsideFactory != isOutside) {
         return false;
      }

      if (Time.time < _retryFactoryExitRequestAfter) {
         return _factoryExitRequestPending;
      }

      if (!isOutside) {
         var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
         foreach (var insideEntrance in entrances) {
            if (insideEntrance == null || !insideEntrance.isEntranceToBuilding) continue;
            if (!TryGetEntrancePair(insideEntrance.entranceId, out _, out var outsideEntrance)) continue;

            var insideDoorPosition = insideEntrance.entrancePoint != null
               ? insideEntrance.entrancePoint.position
               : insideEntrance.transform.position;
            var outsideDoorPosition = outsideEntrance.entrancePoint != null
               ? outsideEntrance.entrancePoint.position
               : outsideEntrance.transform.position;
            if (Vector3.Distance(transform.position, insideDoorPosition) > 16f) continue;
            if (Vector3.Distance(player.transform.position, outsideDoorPosition) > 35f &&
                Vector3.Distance(player.transform.position, outsideEntrance.transform.position) > 35f) continue;

            _factoryExitRequestPending = true;
            _retryFactoryExitRequestAfter = Time.time + 2f;
            RequestFactoryExitServerRpc(insideEntrance.entranceId, (int)player.playerClientId);
            return true;
         }
      } else {
         var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
         foreach (var insideEntrance in entrances) {
            if (insideEntrance == null || !insideEntrance.isEntranceToBuilding) continue;
            if (!TryGetEntrancePair(insideEntrance.entranceId, out _, out var outsideEntrance)) continue;

            var insideDoorPosition = insideEntrance.entrancePoint != null
               ? insideEntrance.entrancePoint.position
               : insideEntrance.transform.position;
            var outsideDoorPosition = outsideEntrance.entrancePoint != null
               ? outsideEntrance.entrancePoint.position
               : outsideEntrance.transform.position;
            if (Vector3.Distance(transform.position, outsideDoorPosition) > 16f &&
                Vector3.Distance(transform.position, outsideEntrance.transform.position) > 16f) continue;
            if (Vector3.Distance(player.transform.position, insideDoorPosition) > 35f &&
                Vector3.Distance(player.transform.position, insideEntrance.transform.position) > 35f) continue;

            _factoryExitRequestPending = true;
            _retryFactoryExitRequestAfter = Time.time + 2f;
            RequestFactoryExitServerRpc(insideEntrance.entranceId, (int)player.playerClientId);
            return true;
         }
      }

      return false;
   }

   [ServerRpc(RequireOwnership = false)]
   private void RequestFactoryExitServerRpc(int entranceId, int targetPlayerClientId) {
      if (!IsServer) return;
      if (!TryGetEntrancePair(entranceId, out var insideEntrance, out var outsideEntrance)) return;

      var targetPlayer = FindPlayerByClientId(targetPlayerClientId);
      if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.IsSpawned) return;
      if (targetPlayer.isInsideFactory != isOutside) return;

      var insideDoorPosition = insideEntrance.entrancePoint != null
         ? insideEntrance.entrancePoint.position
         : insideEntrance.transform.position;
      var outsideDoorPosition = outsideEntrance.entrancePoint != null
         ? outsideEntrance.entrancePoint.position
         : outsideEntrance.transform.position;

      if (!isOutside) {
         if (Vector3.Distance(transform.position, insideDoorPosition) > 20f) return;
         if (Vector3.Distance(targetPlayer.transform.position, outsideDoorPosition) > 40f &&
             Vector3.Distance(targetPlayer.transform.position, outsideEntrance.transform.position) > 40f) return;

         var arrivalPosition = outsideDoorPosition;
         var outsideNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
         GameObject? nearestOutsideNode = null;
         var nearestOutsideDistance = float.MaxValue;
         foreach (var node in outsideNodes) {
            if (node == null) continue;
            var distance = (node.transform.position - arrivalPosition).sqrMagnitude;
            if (distance >= nearestOutsideDistance) continue;
            nearestOutsideDistance = distance;
            nearestOutsideNode = node;
         }

         var samplePosition = nearestOutsideNode != null && nearestOutsideDistance < 30f * 30f
            ? nearestOutsideNode.transform.position
            : arrivalPosition;
         if (!NavMesh.SamplePosition(samplePosition, out var navHit, 6f, NavMesh.AllAreas)) {
            Plugin.Log.LogWarning("Rolling Giant reached an exit, but no outdoor NavMesh point was found beside the paired entrance.");
            return;
         }

         _hasExitedFactory = true;
         var networkObject = GetComponent<NetworkObject>();
         if (networkObject != null && networkObject.IsSpawned && networkObject.OwnerClientId != NetworkManager.ServerClientId) {
            networkObject.ChangeOwnership(NetworkManager.ServerClientId);
         }

         FollowPlayerOutsideClientRpc(navHit.position, outsideEntrance.transform.eulerAngles.y, targetPlayerClientId);
      } else {
         if (Vector3.Distance(transform.position, outsideDoorPosition) > 20f &&
             Vector3.Distance(transform.position, outsideEntrance.transform.position) > 20f) return;
         if (Vector3.Distance(targetPlayer.transform.position, insideDoorPosition) > 40f &&
             Vector3.Distance(targetPlayer.transform.position, insideEntrance.transform.position) > 40f) return;

         var arrivalPosition = insideDoorPosition;
         var insideNodes = GameObject.FindGameObjectsWithTag("AINode");
         GameObject? nearestInsideNode = null;
         var nearestInsideDistance = float.MaxValue;
         foreach (var node in insideNodes) {
            if (node == null) continue;
            var distance = (node.transform.position - arrivalPosition).sqrMagnitude;
            if (distance >= nearestInsideDistance) continue;
            nearestInsideDistance = distance;
            nearestInsideNode = node;
         }

         var samplePosition = nearestInsideNode != null && nearestInsideDistance < 25f * 25f
            ? nearestInsideNode.transform.position
            : arrivalPosition;
         if (!NavMesh.SamplePosition(samplePosition, out var navHit, 6f, NavMesh.AllAreas)) {
            Plugin.Log.LogWarning("Rolling Giant reached an entrance outside, but no indoor NavMesh point was found inside.");
            return;
         }

         _hasExitedFactory = false;
         var networkObject = GetComponent<NetworkObject>();
         if (networkObject != null && networkObject.IsSpawned && networkObject.OwnerClientId != NetworkManager.ServerClientId) {
            networkObject.ChangeOwnership(NetworkManager.ServerClientId);
         }

         FollowPlayerOutsideClientRpc(navHit.position, insideEntrance.transform.eulerAngles.y, targetPlayerClientId);
      }
   }

   [ClientRpc]
   private void FollowPlayerOutsideClientRpc(Vector3 position, float yaw, int targetPlayerClientId) {
      var outsideNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
      var insideNodes = GameObject.FindGameObjectsWithTag("AINode");
      var minOutDist = float.MaxValue;
      var minInDist = float.MaxValue;
      foreach (var n in outsideNodes) {
         if (n != null) minOutDist = Mathf.Min(minOutDist, (n.transform.position - position).sqrMagnitude);
      }
      foreach (var n in insideNodes) {
         if (n != null) minInDist = Mathf.Min(minInDist, (n.transform.position - position).sqrMagnitude);
      }

      var nowOutside = minOutDist <= minInDist;
      isOutside = nowOutside;
      _hasExitedFactory = nowOutside;
      _factoryExitRequestPending = false;
      _retryFactoryExitRequestAfter = Time.time + 2f;
      allAINodes = nowOutside ? outsideNodes : insideNodes;

      var rotation = Quaternion.Euler(0f, yaw, 0f);
      transform.SetPositionAndRotation(position, rotation);
      if (agent == null) agent = GetComponentInChildren<NavMeshAgent>();
      if (agent != null) {
         if (agent.isOnNavMesh) agent.ResetPath();
         agent.Warp(position);
         if (agent.isOnNavMesh) agent.ResetPath();
      }

      targetPlayer = FindPlayerByClientId(targetPlayerClientId);
      _lastChaseTarget = targetPlayer;
      if (targetPlayer != null) {
         movingTowardsTargetPlayer = true;
         SwitchToBehaviourStateOnLocalClient(1);
         SetMovingTowardsTargetPlayer(targetPlayer);
      }

      Plugin.Log.LogInfo("Rolling Giant followed its target outside through the paired facility exit.");
   }

   private static bool TryGetEntrancePair(
      int entranceId,
      [NotNullWhen(true)] out EntranceTeleport? insideEntrance,
      [NotNullWhen(true)] out EntranceTeleport? outsideEntrance) {
      insideEntrance = null;
      outsideEntrance = null;
      var entrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>();
      foreach (var candidate in entrances) {
         if (candidate == null || !candidate.isEntranceToBuilding || candidate.entranceId != entranceId) continue;
         insideEntrance = candidate;
         outsideEntrance = candidate.exitScript;
         if (outsideEntrance != null && !outsideEntrance.isEntranceToBuilding) return true;

         outsideEntrance = null;
         foreach (var pairedCandidate in entrances) {
            if (pairedCandidate != null && !pairedCandidate.isEntranceToBuilding && pairedCandidate.entranceId == entranceId) {
               outsideEntrance = pairedCandidate;
               return true;
            }
         }
      }

      insideEntrance = null;
      outsideEntrance = null;
      return false;
   }

   private static PlayerControllerB? FindPlayerByClientId(int playerClientId) {
      var players = StartOfRound.Instance != null ? StartOfRound.Instance.allPlayerScripts : null;
      if (players == null) return null;
      foreach (var player in players) {
         if (player != null && (int)player.playerClientId == playerClientId) return player;
      }
      return null;
   }

   public override void OnCollideWithPlayer(Collider other) {
      if (daytimeEnemyLeaving) {
         return;
      }
      
      base.OnCollideWithPlayer(other);
      if (_timeSinceHittingPlayer < 0.6f) {
         return;
      }

      var player = MeetsStandardPlayerCollisionConditions(other);
      if (!player) return;

      if (_tooBig && player.isInHangarShipRoom) {
         return;
      }

      _timeSinceHittingPlayer = 0.2f;
      var index = StartOfRound.Instance.playerRagdolls.IndexOf(Plugin.PlayerRagdoll);
      player.DamagePlayer(90, causeOfDeath: CauseOfDeath.Strangulation, deathAnimation: index);
      agent.speed = 0;

      GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
   }

   private void CalculateAgentSpeed() {
      if (stunNormalizedTimer >= 0) {
         agent.speed = 0;
         agent.acceleration = 200;
         return;
      }

      // searching
      if (currentBehaviourStateIndex == 0) {
         MoveAccelerate();
      }
      // chasing
      else if (currentBehaviourStateIndex == 1) {
         // if not on the ground, accelerate to reach it
         if (IsOwner && !IsAgentOnNavMesh(agent.gameObject)) {
            MoveAccelerate();
            LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] not on navmesh");
            return;
         }

         var isLookedAt = AmIBeingLookedAt(out _);
         if (IsOwner) {
            if (isLookedAt) {
               _lookTimer.Value += Time.deltaTime;
            } else {
               _lookTimer.Value = 0;
            }
         }

         var aiType = NetworkHandler.AiType;
         switch (aiType) {
            case RollingGiantAiType.Coilhead:
               if (isLookedAt) {
                  MoveDecelerate();
                  // LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] {player?.playerUsername} is lookin at, {_lookTimer}sec");
                  return;
               }

               MoveAccelerate();
               // LogInfo($"[CalculateAgentSpeed::{NetworkHandler.AiType}] not looking at, {_lookTimer}sec");
               break;
            case RollingGiantAiType.InverseCoilhead:
               if (!isLookedAt && _isAgro) {
                  if (CheckLineOfSightToAnyPlayer()) {
                     MoveDecelerate();
                     return;
                  }
               }

               MoveAccelerate();
               _isAgro = true;
               break;
            case RollingGiantAiType.RandomlyMoveWhileLooking:
               if (isLookedAt) {
                  if (_waitTimer.Value <= 0 && _moveTimer.Value <= 0) {
                     GenerateWaitTime();
                  }

                  if (_waitTimer.Value > 0 && _moveTimer.Value <= 0) {
                     MoveDecelerate();

                     if (IsOwner) {
                        LogInfo($"_waitTimer: {_waitTimer.Value}");
                        _waitTimer.Value -= Time.deltaTime;
                        if (_waitTimer.Value <= 0) {
                           GenerateMoveTime();
                        }
                     }
                     return;
                  }
               }

               MoveAccelerate();

               if (_moveTimer.Value > 0) {
                  if (IsOwner) {
                     LogInfo($"_moveTimer: {_moveTimer.Value}");
                     _moveTimer.Value -= Time.deltaTime;
                     if (_moveTimer.Value <= 0) {
                        GenerateWaitTime();
                     }
                  }
               }
               break;
            case RollingGiantAiType.LookingTooLongKeepsAgro:
               if (!_isAgro) {
                  if (isLookedAt) {
                     if (IsOwner) {
                        _agroTimer.Value += Time.deltaTime / _sharedAiSettings.lookTimeBeforeAgro;
                     }
                     LogInfo($"[Update::{NetworkHandler.AiType}] _agroTimer: {_agroTimer.Value}");
                     if (_agroTimer.Value >= 1f) {
                        _isAgro = true;
                        LogInfo($"[Update::{NetworkHandler.AiType}] got agro");
                     }

                     MoveDecelerate();
                  } else {
                     if (IsOwner) {
                        _agroTimer.Value = Mathf.Lerp(_agroTimer.Value, 0, Time.deltaTime / (_sharedAiSettings.lookTimeBeforeAgro * 1.5f));
                        LogInfo($"[Update::{NetworkHandler.AiType}] _agroTimer: {_agroTimer.Value}");
                     }

                     MoveAccelerate();
                  }
                  return;
               }

               MoveAccelerate();
               break;
            case RollingGiantAiType.FollowOnceAgro:
               if (!_isAgro) {
                  if (isLookedAt) {
                     _isAgro = true;
                     MoveDecelerate();
                     LogInfo($"[Update::{NetworkHandler.AiType}] got agro");
                     return;
                  }
               }

               MoveAccelerate();
               break;
            case RollingGiantAiType.OnceSeenAgroAfterTimer:
               if (!_isAgro) {
                  if (isLookedAt) {
                     _isAgro = true;
                     LogInfo($"[Update::{NetworkHandler.AiType}] got agro");
                     if (IsOwner) {
                        _agroTimer.Value = Mathf.Lerp(_sharedAiSettings.waitTimeMin, _sharedAiSettings.waitTimeMax, NextDouble());
                     }
                     MoveDecelerate();
                     return;
                  }
               } else if (_agroTimer.Value > 0) {
                  LogInfo($"[Update::{NetworkHandler.AiType}] _agroTimer: {_agroTimer.Value}");
                  if (IsOwner) {
                     _agroTimer.Value -= Time.deltaTime;
                  }

                  if (_agroTimer.Value <= 0) {
                     LogInfo($"[Update::{NetworkHandler.AiType}] chasing time");
                  }
                  MoveDecelerate();
                  return;
               }

               MoveAccelerate();
               break;
            default:
               Plugin.Log.LogWarning($"Unknown ai type: {aiType}");
               break;
         }
      }
   }

   private void MoveAccelerate() {
      agent.speed = _sharedAiSettings.moveAcceleration == 0
         ? _sharedAiSettings.moveSpeed
         : Mathf.Lerp(agent.speed, _sharedAiSettings.moveSpeed, Time.deltaTime / _sharedAiSettings.moveAcceleration);
      agent.acceleration = Mathf.Lerp(agent.acceleration, 200, Time.deltaTime);
   }

   private void MoveDecelerate() {
      agent.speed = _sharedAiSettings.moveDeceleration == 0 ? 0 : Mathf.Lerp(agent.speed, 0, Time.deltaTime / _sharedAiSettings.moveDeceleration);
      agent.acceleration = 200;
   }

   private bool AmIBeingLookedAt(out PlayerControllerB closestPlayer) {
      var players = StartOfRound.Instance.allPlayerScripts;
      var closestDistance = float.MaxValue;
      closestPlayer = null;

      foreach (var player in players) {
         if (!isOutside && !PlayerIsTargetable(player)) continue;
         if (player.isPlayerDead) continue;
         if (!player.IsSpawned) continue;
         // if (!PlayerIsTargetable(player, overrideInsideFactoryCheck: isOutside)) {
         //    // LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] !PlayerIsTargetable({player?.playerUsername})");
         //    continue;
         // }
         // transform.position + Vector3.up * 1.6f
         
         if (player.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f)) {
            var distance = Vector3.Distance(transform.position, player.transform.position);
            // LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] HasLineOfSightToPosition({player?.playerUsername}), distance: {distance}");
            if (distance < closestDistance) {
               closestDistance = distance;
               closestPlayer = player;
            }
         } else {
            // LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] !HasLineOfSightToPosition({player?.playerUsername})");
         }
      }

      // LogInfo($"[AmIBeingLookedAt::{NetworkHandler.AiType}] closestPlayer: {closestPlayer?.playerUsername}");
      return closestPlayer;
   }

   private bool CheckLineOfSightTo(PlayerControllerB player) {
      return player && CheckLineOfSightForPosition(player.gameplayCamera.transform.position, 68f);
   }
   
   private bool CheckLineOfSightToAnyPlayer() {
      var players = StartOfRound.Instance.allPlayerScripts;
      foreach (var player in players) {
         if (!player.IsSpawned) continue;
         if (player.isPlayerDead) continue;
         
         if (CheckLineOfSightTo(player)) {
            return true;
         }
      }

      return false;
   }
   
   // public bool HasLineOfSightToPosition(
   //    PlayerControllerB playerControllerB,
   //    Vector3 pos,
   //    float width = 45f,
   //    int range = 60,
   //    float proximityAwareness = -1f)
   // {
   //    float num = Vector3.Distance(playerControllerB.transform.position, pos);
   //    var b0 = num < (double)range;
   //    var b1 = (Vector3.Angle(playerControllerB.playerEye.transform.forward, pos - playerControllerB.gameplayCamera.transform.position) < (double)width ||
   //              num < (double)proximityAwareness);
   //    var b2 = !Physics.Linecast(playerControllerB.playerEye.transform.position,
   //       pos,
   //       out var hit,
   //       StartOfRound.Instance.collidersRoomDefaultAndFoliage,
   //       QueryTriggerInteraction.Ignore);
   //    LogInfo($"[HasLineOfSightToPosition::{NetworkHandler.AiType}] b0: {b0}, b1: {b1}, b2: {b2} ({hit.collider?.name})");
   //    return b0 && b1 && b2;
   // }

   private bool IsAgentOnNavMesh(GameObject agentObject) {
      var agentPosition = agentObject.transform.position;

      // Check for nearest point on navmesh to agent, within onMeshThreshold
      if (NavMesh.SamplePosition(agentPosition, out var hit, 3, NavMesh.AllAreas)) {
         // Check if the positions are vertically aligned
         if (Mathf.Approximately(agentPosition.x, hit.position.x)
             && Mathf.Approximately(agentPosition.z, hit.position.z)) {
            // Lastly, check if object is below navmesh
            return agentPosition.y >= hit.position.y;
         }
      }

      return false;
   }

   [ServerRpc(RequireOwnership = false)]
   private void BeginChasingPlayer_ServerRpc(int playerId) {
      BeginChasingPlayer_ClientRpc(playerId);
      // LogInfo($"[BeginChasingPlayer_ServerRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(1)");
   }

   [ClientRpc]
   private void BeginChasingPlayer_ClientRpc(int playerId) {
      SwitchToBehaviourStateOnLocalClient(1);
      var player = StartOfRound.Instance.allPlayerScripts[playerId];
      SetMovingTowardsTargetPlayer(player);
      // LogInfo($"[BeginChasingPlayer_ClientRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(1)");
   }
   
   [ServerRpc(RequireOwnership = false)]
   private void EndChasingPlayer_ServerRpc() {
      EndChasingPlayer_ClientRpc();
      // LogInfo($"[EndChasingPlayer_ServerRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(0)");
   }
   
   [ClientRpc]
   private void EndChasingPlayer_ClientRpc() {
      movingTowardsTargetPlayer = false;
      SwitchToBehaviourStateOnLocalClient(0);
      // LogInfo($"[EndChasingPlayer_ClientRpc::{_sharedAiSettings.aiType}] SwitchToBehaviourStateOnLocalClient(0)");
   }
   
   private void GenerateWaitTime() {
      if (!IsOwner) return;
      var waitTime = Mathf.Lerp(_sharedAiSettings.waitTimeMin, _sharedAiSettings.waitTimeMax, NextDouble());
      _waitTimer.Value = waitTime;
   }
   
   private void GenerateAgroTime() {
      if (!IsOwner) return;
      _agroTimer.Value = _sharedAiSettings.lookTimeBeforeAgro;
   }
   
   private void GenerateMoveTime() {
      if (!IsOwner) return;
      var moveTime = Mathf.Lerp(_sharedAiSettings.randomMoveTimeMin, _sharedAiSettings.randomMoveTimeMax, NextDouble());
      _moveTimer.Value = moveTime;
   }

   [ServerRpc(RequireOwnership = false)]
   private void AssignInitData_ServerRpc(float scale) {
      AssignAgentData(scale);
      agent.transform.localScale = Vector3.one * scale;
      AssignInitData_ClientRpc(scale);
      // LogInfo($"[AssignInitData_ServerRpc::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }
   
   private void AssignInitData_LocalClient() {
      var config = CustomConfig.Instance;
      var sizeRange = isOutside ? new Vector2(config.GiantScaleOutsideMin, config.GiantScaleOutsideMax) : new Vector2(config.GiantScaleInsideMin, config.GiantScaleInsideMax);
      var scale = Mathf.Lerp(sizeRange.x, sizeRange.y, NextDouble());
      AssignAgentData(scale);
      agent.transform.localScale = Vector3.one * scale;
      AssignInitData_ServerRpc(scale);
      // LogInfo($"[AssignInitData_LocalClient::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }

   [ClientRpc]
   private void AssignInitData_ClientRpc(float scale) {
      // agent.height = 5f;
      updatePositionThreshold = float.MaxValue;
      AssignAgentData(scale);
      agent.transform.localScale = Vector3.one * scale;
      
      // LogInfo($"[AssignInitData_ClientRpc::{_sharedAiSettings.aiType}] agent.transform.localScale: {agent.transform.localScale}");
   }


   private void AssignAgentData(float scale) 
    {
      Init(scale);
      if (scale >= 1.2f) {
         var areas = agent.areaMask;
         // var exclude = 1 << NavMesh.GetAreaFromName("SmallSpace") | 1 << NavMesh.GetAreaFromName("MediumSpace") | 1 << NavMesh.GetAreaFromName("Climb");
         areas &= ~(1 << NavMesh.GetAreaFromName("SmallSpace"));
         areas &= ~(1 << NavMesh.GetAreaFromName("MediumSpace"));
         areas &= ~(1 << NavMesh.GetAreaFromName("Climb"));
         areas &= ~(1 << NavMesh.GetAreaFromName("PlayerShip"));
         agent.areaMask = areas;
         _tooBig = true;
      }
   }

}
