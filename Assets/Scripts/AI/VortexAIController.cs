using UnityEngine;
using VortexGame.Core;

namespace VortexGame.AI
{
    public sealed class VortexAIController : MonoBehaviour
    {
        private VortexEntity self;
        private float retargetTimer;
        private Vector3 currentDestination;

        public void Initialize(VortexEntity controlledVortex)
        {
            self = controlledVortex;
            retargetTimer = 0f;
        }

        public void Tick(float deltaTime, VortexGameManager manager)
        {
            if (self == null)
            {
                return;
            }

            retargetTimer -= deltaTime;
            VortexEntity strongestThreat = FindStrongestNearby(manager);
            VortexEntity weakestTarget = FindWeakestNearby(manager);

            if (retargetTimer <= 0f)
            {
                retargetTimer = Random.Range(0.3f, 0.7f);
                currentDestination = ChooseDestination(manager, strongestThreat, weakestTarget);
            }

            Vector3 delta = currentDestination - self.transform.position;
            Vector2 move = new Vector2(delta.x, delta.z).normalized;
            self.SetInput(move);
            self.SetBoost(strongestThreat != null && strongestThreat.Energy > self.Energy * 1.15f);
            self.SetShield(strongestThreat != null && Vector3.Distance(self.transform.position, strongestThreat.transform.position) < 5f);
            self.TickAIAbilities(manager, weakestTarget);

            if (self.Energy < 15f && !self.IsPullMode)
            {
                self.ToggleMode();
            }
            else if (weakestTarget != null && weakestTarget.Energy < self.Energy * 0.85f && self.IsPullMode == false)
            {
                self.ToggleMode();
            }
        }

        private VortexEntity FindStrongestNearby(VortexGameManager manager)
        {
            VortexEntity best = null;
            float strongestEnergy = 0f;

            for (int i = 0; i < manager.Vortexes.Count; i++)
            {
                VortexEntity candidate = manager.Vortexes[i];
                if (candidate == self)
                {
                    continue;
                }

                float distance = Vector3.Distance(self.transform.position, candidate.transform.position);
                if (distance > 13f || candidate.Energy <= strongestEnergy)
                {
                    continue;
                }

                strongestEnergy = candidate.Energy;
                best = candidate;
            }

            return best;
        }

        private VortexEntity FindWeakestNearby(VortexGameManager manager)
        {
            VortexEntity best = null;
            float weakestEnergy = float.MaxValue;

            for (int i = 0; i < manager.Vortexes.Count; i++)
            {
                VortexEntity candidate = manager.Vortexes[i];
                if (candidate == self)
                {
                    continue;
                }

                float distance = Vector3.Distance(self.transform.position, candidate.transform.position);
                if (distance > 15f || candidate.Energy >= weakestEnergy)
                {
                    continue;
                }

                weakestEnergy = candidate.Energy;
                best = candidate;
            }

            return best;
        }

        private Vector3 ChooseDestination(VortexGameManager manager, VortexEntity strongestThreat, VortexEntity weakestTarget)
        {
            if (strongestThreat != null && strongestThreat.Energy > self.Energy * 1.15f)
            {
                Vector3 away = self.transform.position - strongestThreat.transform.position;
                return self.transform.position + away.normalized * 5f;
            }

            if (weakestTarget != null && weakestTarget.Energy < self.Energy * 0.9f)
            {
                return weakestTarget.transform.position;
            }

            WorldObject nearestResource = manager.FindNearestFreeObject(self.transform.position);
            if (nearestResource != null)
            {
                return nearestResource.transform.position;
            }

            return new Vector3(Random.Range(-manager.ArenaRadius * 0.8f, manager.ArenaRadius * 0.8f), 0.5f, Random.Range(-manager.ArenaRadius * 0.8f, manager.ArenaRadius * 0.8f));
        }
    }
}
