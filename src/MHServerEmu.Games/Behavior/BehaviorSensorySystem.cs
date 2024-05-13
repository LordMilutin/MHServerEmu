﻿using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class BehaviorSensorySystem
    {
        private AIController _pAIController;
        private float _leashDistanceSq = 3000.0f * 3000.0f; // Default LeashDistance in PopulationObject
        public bool CanLeash { get; private set; }

        public void Sense()
        {
            var agent = _pAIController.Owner;
            if (agent != null)
            {
                UpdateAvatarSensory();
                if (agent.IsDormant == false)
                {
                    IsLeashingDistanceMet();
                    HandleInterrupts();
                }
            }
        }

        public void Initialize(AIController aIController, BehaviorProfilePrototype profile, SpawnSpec spec)
        {
            _pAIController = aIController;
            CanLeash = profile.CanLeash;
            if (spec != null)
                _leashDistanceSq = MathHelper.Square(spec.LeashDistance);
        }

        private bool IsLeashingDistanceMet()
        {
            if (CanLeash)
            {
                Agent ownerAgent = _pAIController?.Owner;
                if (ownerAgent == null) return false;
                BehaviorBlackboard blackboard = _pAIController.Blackboard;
                if (_leashDistanceSq > 0.0f && Vector3.DistanceSquared2D(blackboard.SpawnPoint, ownerAgent.RegionLocation.Position) > _leashDistanceSq) 
                {
                    blackboard.PropertyCollection[PropertyEnum.AIIsLeashing] = true;
                    return true;
                }
            }
            return false;
        }

        public void UpdateAvatarSensory()
        {
            throw new NotImplementedException();
        }

        private void HandleInterrupts()
        {
            throw new NotImplementedException();
        }

        public WorldEntity GetCurrentTarget()
        {
            throw new NotImplementedException();
        }

        internal bool ShouldSense()
        {
            throw new NotImplementedException();
        }

        internal void ValidateCurrentTarget(CombatTargetType targetType)
        {
            throw new NotImplementedException();
        }

    }
}
