using System.Collections.Generic;
using UnityEngine;

namespace VortexGame.Core
{
    public sealed class VortexEntity : MonoBehaviour
    {
        private readonly List<WorldObject> orbitingObjects = new List<WorldObject>(32);
        private Material materialInstance;
        private Vector2 movementInput;
        private Vector3 aimDirection = Vector3.forward;
        private float boostHold;
        private float shieldHold;
        private float shockCooldown;
        private float releaseCooldown;
        private float stealCooldown;
        private float hapticCooldown;

        public bool IsPlayer { get; private set; }
        public string DisplayName { get; private set; }
        public Color ThemeColor { get; private set; }
        public float Energy { get; private set; }
        public float CurrentRadius { get; private set; }
        public bool IsPullMode { get; private set; }
        public bool ShieldActive => shieldHold > 0f && Energy > 1f;
        public float ZoneScore { get; private set; }
        public IReadOnlyList<WorldObject> OrbitingObjects => orbitingObjects;

        public void Initialize(string displayName, Color themeColor, bool isPlayer, float startingEnergy)
        {
            DisplayName = displayName;
            ThemeColor = themeColor;
            IsPlayer = isPlayer;
            Energy = startingEnergy;
            IsPullMode = true;

            materialInstance = GetComponent<Renderer>().material;
            materialInstance.color = themeColor;
            RefreshScale();
        }

        public void SetInput(Vector2 moveInput)
        {
            movementInput = Vector2.ClampMagnitude(moveInput, 1f);
            if (movementInput.sqrMagnitude > 0.001f)
            {
                aimDirection = new Vector3(movementInput.x, 0f, movementInput.y).normalized;
            }
        }

        public void SetBoost(bool held)
        {
            boostHold = held ? 1f : 0f;
        }

        public void SetShield(bool held)
        {
            shieldHold = held ? 1f : 0f;
        }

        public void ToggleMode()
        {
            IsPullMode = !IsPullMode;
            TriggerHaptics();
        }

        public void Tick(float deltaTime, VortexGameManager manager)
        {
            if (hapticCooldown > 0f)
            {
                hapticCooldown -= deltaTime;
            }

            shockCooldown = Mathf.Max(0f, shockCooldown - deltaTime);
            releaseCooldown = Mathf.Max(0f, releaseCooldown - deltaTime);
            stealCooldown = Mathf.Max(0f, stealCooldown - deltaTime);

            if (ShieldActive)
            {
                Energy = Mathf.Max(0.5f, Energy - (deltaTime * 2.2f));
                RefreshScale();
            }

            Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y);
            float speed = 6f + (Mathf.Sqrt(Energy) * 0.08f);
            if (boostHold > 0f && Energy > 5f)
            {
                speed *= 1.75f;
                Energy = Mathf.Max(0.5f, Energy - (deltaTime * 4.5f));
                RefreshScale();
            }

            transform.position += movement * speed * deltaTime;
            ClampInsideArena(manager.ArenaRadius);
            ProcessFieldInfluence(deltaTime, manager);
            TryStealOrbiters(manager);
        }

        public void AddEnergy(float value)
        {
            Energy += value;
            RefreshScale();
        }

        public void AddZoneScore(float value)
        {
            ZoneScore += value;
        }

        public void ResetZoneScore()
        {
            ZoneScore = 0f;
        }

        public bool TryCapture(WorldObject target)
        {
            if (target == null || orbitingObjects.Count >= OrbitCapacity)
            {
                return false;
            }

            float angle = orbitingObjects.Count * Mathf.PI * 0.66f;
            float distance = CurrentRadius + 1.2f + (orbitingObjects.Count % 3) * 0.5f;
            float speed = 2.4f + orbitingObjects.Count * 0.09f;
            orbitingObjects.Add(target);
            target.EnterOrbit(this, distance, angle, speed);
            return true;
        }

        public void RemoveOrbitingReference(WorldObject target)
        {
            orbitingObjects.Remove(target);
        }

        public void AbsorbOrbitingObject(WorldObject target)
        {
            if (!orbitingObjects.Contains(target))
            {
                return;
            }

            AddEnergy(target.EnergyValue);
            target.SetInactive();
            TriggerHaptics();
        }

        public void ReleaseOrbitingObjects()
        {
            if (releaseCooldown > 0f || orbitingObjects.Count == 0)
            {
                return;
            }

            releaseCooldown = 1.1f;
            Vector3 releaseDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection : transform.forward;

            for (int i = orbitingObjects.Count - 1; i >= 0; i--)
            {
                WorldObject orbiter = orbitingObjects[i];
                orbiter.transform.position = transform.position + (releaseDirection * (CurrentRadius + 0.8f + i * 0.15f));
                orbiter.LaunchProjectile(this, Quaternion.Euler(0f, (i - orbitingObjects.Count * 0.5f) * 10f, 0f) * releaseDirection, 11f + (Energy * 0.03f));
            }

            orbitingObjects.Clear();
            TriggerHaptics();
        }

        public void ActivateShockwave(VortexGameManager manager)
        {
            if (shockCooldown > 0f || Energy < 10f)
            {
                return;
            }

            shockCooldown = 4f;
            Energy -= 10f;
            RefreshScale();
            float radius = CurrentRadius + 5f;

            for (int i = 0; i < manager.ActiveObjects.Count; i++)
            {
                WorldObject target = manager.ActiveObjects[i];
                if (target.State == WorldObjectState.Inactive)
                {
                    continue;
                }

                Vector3 offset = target.transform.position - transform.position;
                float distance = offset.magnitude;
                if (distance > radius || distance < 0.01f)
                {
                    continue;
                }

                target.ReleaseToFree(offset.normalized * Mathf.Lerp(9f, 2f, distance / radius));
            }

            for (int i = 0; i < manager.Vortexes.Count; i++)
            {
                VortexEntity other = manager.Vortexes[i];
                if (other == this)
                {
                    continue;
                }

                Vector3 offset = other.transform.position - transform.position;
                float distance = offset.magnitude;
                if (distance <= radius)
                {
                    other.transform.position += offset.normalized * 1.5f;
                    other.Energy = Mathf.Max(1f, other.Energy - 4f);
                    other.RefreshScale();
                }
            }

            TriggerHaptics();
        }

        public void ReceiveProjectileHit(WorldObject projectile, VortexEntity attacker)
        {
            float impact = projectile.EnergyValue * (attacker == null ? 0.8f : 1f);
            float damage = ShieldActive ? impact * 0.2f : impact * 0.75f;
            Energy = Mathf.Max(1f, Energy - damage);
            RefreshScale();

            if (attacker != null)
            {
                attacker.AddEnergy(impact * 0.3f);
            }
        }

        public void TickAIAbilities(VortexGameManager manager, VortexEntity target)
        {
            if (target == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < CurrentRadius + 6f && Energy > target.Energy * 0.7f)
            {
                ReleaseOrbitingObjects();
            }

            if (distance < CurrentRadius + 4f && shockCooldown <= 0f && Energy > 12f)
            {
                ActivateShockwave(manager);
            }
        }

        private void ProcessFieldInfluence(float deltaTime, VortexGameManager manager)
        {
            float attractionRadius = CurrentRadius + 5f + Mathf.Sqrt(Energy) * 0.22f;
            float orbitThreshold = CurrentRadius + 1.8f;

            for (int i = 0; i < manager.ActiveObjects.Count; i++)
            {
                WorldObject target = manager.ActiveObjects[i];
                if (target.State == WorldObjectState.Inactive || target.Owner == this)
                {
                    continue;
                }

                Vector3 offset = transform.position - target.transform.position;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance > attractionRadius * attractionRadius || sqrDistance < 0.0001f)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(sqrDistance);
                Vector3 direction = offset / distance;
                float forceStrength = (Energy * 0.7f) / Mathf.Max(0.6f, target.Mass);

                if (IsPullMode)
                {
                    target.ApplyForce(direction * forceStrength, deltaTime);
                    if (target.State == WorldObjectState.Free && distance <= orbitThreshold)
                    {
                        TryCapture(target);
                    }
                }
                else
                {
                    target.ApplyForce(-direction * forceStrength * 0.8f, deltaTime);
                }
            }
        }

        private void TryStealOrbiters(VortexGameManager manager)
        {
            if (stealCooldown > 0f)
            {
                return;
            }

            for (int i = 0; i < manager.Vortexes.Count; i++)
            {
                VortexEntity other = manager.Vortexes[i];
                if (other == this || other.orbitingObjects.Count == 0)
                {
                    continue;
                }

                float stealRange = CurrentRadius + other.CurrentRadius + 2.6f;
                if ((other.transform.position - transform.position).sqrMagnitude > stealRange * stealRange)
                {
                    continue;
                }

                if (Energy < other.Energy * 0.85f)
                {
                    continue;
                }

                WorldObject stolen = other.orbitingObjects[other.orbitingObjects.Count - 1];
                TryCapture(stolen);
                stealCooldown = 1.5f;
                TriggerHaptics();
                return;
            }
        }

        private void RefreshScale()
        {
            CurrentRadius = 1.1f + Mathf.Sqrt(Mathf.Max(1f, Energy)) * 0.085f;
            transform.localScale = new Vector3(CurrentRadius * 1.2f, 0.7f, CurrentRadius * 1.2f);
        }

        private void ClampInsideArena(float arenaRadius)
        {
            Vector3 position = transform.position;
            Vector2 flat = new Vector2(position.x, position.z);
            float limit = arenaRadius - CurrentRadius;
            if (flat.sqrMagnitude <= limit * limit)
            {
                return;
            }

            Vector2 clamped = flat.normalized * limit;
            transform.position = new Vector3(clamped.x, position.y, clamped.y);
        }

        private int OrbitCapacity => Mathf.Clamp(4 + Mathf.FloorToInt(Mathf.Sqrt(Energy) * 0.5f), 4, 18);

        private void TriggerHaptics()
        {
            if (!IsPlayer || hapticCooldown > 0f)
            {
                return;
            }

            hapticCooldown = 0.2f;
            Handheld.Vibrate();
        }
    }
}
