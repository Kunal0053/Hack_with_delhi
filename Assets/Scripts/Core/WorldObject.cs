using UnityEngine;

namespace VortexGame.Core
{
    public sealed class WorldObject : MonoBehaviour
    {
        private Material materialInstance;

        public WorldObjectState State { get; private set; }
        public VortexEntity Owner { get; private set; }
        public float Mass { get; private set; }
        public float EnergyValue { get; private set; }
        public float Radius { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float OrbitAngle { get; private set; }
        public float OrbitDistance { get; private set; }
        public float OrbitSpeed { get; private set; }
        public float OrbitTimer { get; private set; }
        public float ProjectileLifetime { get; private set; }

        public void Initialize(Renderer objectRenderer)
        {
            materialInstance = objectRenderer.material;
            SetInactive();
        }

        public void Activate(Vector3 position, float mass, float energyValue, Color color)
        {
            transform.position = position;
            Mass = mass;
            EnergyValue = energyValue;
            Radius = Mathf.Lerp(0.35f, 0.9f, Mathf.InverseLerp(1f, 10f, mass));
            transform.localScale = Vector3.one * Radius;
            Velocity = Vector3.zero;
            Owner = null;
            OrbitTimer = 0f;
            ProjectileLifetime = 0f;
            State = WorldObjectState.Free;
            gameObject.SetActive(true);
            materialInstance.color = color;
        }

        public void SetInactive()
        {
            DetachFromOwnerList();
            State = WorldObjectState.Inactive;
            Owner = null;
            Velocity = Vector3.zero;
            gameObject.SetActive(false);
        }

        public void ApplyForce(Vector3 force, float deltaTime)
        {
            if (State != WorldObjectState.Free && State != WorldObjectState.Projectile)
            {
                return;
            }

            Velocity += force * deltaTime;
        }

        public void EnterOrbit(VortexEntity owner, float orbitDistance, float orbitAngle, float orbitSpeed)
        {
            DetachFromOwnerList();
            Owner = owner;
            OrbitDistance = orbitDistance;
            OrbitAngle = orbitAngle;
            OrbitSpeed = orbitSpeed;
            OrbitTimer = 0f;
            Velocity = Vector3.zero;
            State = WorldObjectState.Orbiting;
        }

        public void LaunchProjectile(VortexEntity owner, Vector3 direction, float speed)
        {
            DetachFromOwnerList();
            Owner = owner;
            State = WorldObjectState.Projectile;
            ProjectileLifetime = 2.8f;
            Velocity = direction.normalized * speed;
        }

        public void ReleaseToFree(Vector3 impulse)
        {
            DetachFromOwnerList();
            Owner = null;
            State = WorldObjectState.Free;
            Velocity = impulse;
            OrbitTimer = 0f;
        }

        public void Tick(float deltaTime, VortexGameManager manager)
        {
            switch (State)
            {
                case WorldObjectState.Free:
                    TickFree(deltaTime, manager);
                    break;
                case WorldObjectState.Orbiting:
                    TickOrbit(deltaTime, manager);
                    break;
                case WorldObjectState.Projectile:
                    TickProjectile(deltaTime, manager);
                    break;
            }
        }

        private void TickFree(float deltaTime, VortexGameManager manager)
        {
            Velocity *= 1f / (1f + (2.6f * deltaTime));
            transform.position += Velocity * deltaTime;
            ClampInsideArena(manager.ArenaRadius, ref Velocity);
        }

        private void TickOrbit(float deltaTime, VortexGameManager manager)
        {
            if (Owner == null || !Owner.isActiveAndEnabled)
            {
                ReleaseToFree(Vector3.zero);
                return;
            }

            OrbitTimer += deltaTime;
            OrbitAngle += OrbitSpeed * deltaTime;
            Vector3 orbitOffset = new Vector3(Mathf.Cos(OrbitAngle), 0f, Mathf.Sin(OrbitAngle)) * OrbitDistance;
            Vector3 targetPosition = Owner.transform.position + orbitOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-12f * deltaTime));

            if (OrbitTimer >= manager.AbsorbDelay)
            {
                Owner.AbsorbOrbitingObject(this);
            }
        }

        private void TickProjectile(float deltaTime, VortexGameManager manager)
        {
            ProjectileLifetime -= deltaTime;
            transform.position += Velocity * deltaTime;
            ClampInsideArena(manager.ArenaRadius, ref Velocity);

            if (ProjectileLifetime <= 0f)
            {
                ReleaseToFree(Velocity * 0.2f);
                return;
            }

            for (int i = 0; i < manager.Vortexes.Count; i++)
            {
                VortexEntity target = manager.Vortexes[i];
                if (target == Owner || target == null)
                {
                    continue;
                }

                float hitRadius = target.CurrentRadius + Radius + 0.2f;
                if ((target.transform.position - transform.position).sqrMagnitude <= hitRadius * hitRadius)
                {
                    target.ReceiveProjectileHit(this, Owner);
                    ReleaseToFree((transform.position - target.transform.position).normalized * 3f);
                    return;
                }
            }
        }

        private void ClampInsideArena(float arenaRadius, ref Vector3 velocity)
        {
            Vector3 position = transform.position;
            Vector2 flat = new Vector2(position.x, position.z);
            float limit = arenaRadius - 0.5f;
            if (flat.sqrMagnitude <= limit * limit)
            {
                return;
            }

            Vector2 clamped = flat.normalized * limit;
            transform.position = new Vector3(clamped.x, position.y, clamped.y);
            Vector3 normal = new Vector3(clamped.x, 0f, clamped.y).normalized;
            velocity = Vector3.Reflect(velocity, normal) * 0.35f;
        }

        private void DetachFromOwnerList()
        {
            if (Owner != null)
            {
                Owner.RemoveOrbitingReference(this);
            }
        }
    }
}
