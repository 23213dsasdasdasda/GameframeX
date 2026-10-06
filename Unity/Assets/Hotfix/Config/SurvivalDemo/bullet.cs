using System;
using GameframeX.Config;
using UnityEngine;

/// <summary>
/// Handles projectile movement and hit detection for the survival demo.
/// </summary>
public class Bullet : MonoBehaviour
{
    private const bool EnableBulletDebugLog = true;
    private const bool EnableBulletRotationDebugLog = true;

    private WeaponConfig _config;
    private Vector3 _direction;
    private Quaternion _rotationOffset = Quaternion.identity;
    private float _travelDistance;
    private Vector3 _lastPosition;
    private SphereCollider _sphereCollider;
    private const float InitialGroundIgnoreDistance = 1.5f;

    public Action<Collider, Vector3> HitCallback { get; set; }

    public void Init(WeaponConfig config, Vector3 direction, Quaternion rotationOffset, Action<Collider, Vector3> hitCallback = null)
    {
        _config = config;
        _direction = direction.normalized;
        _rotationOffset = rotationOffset;
        HitCallback = hitCallback;

        if (_direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up) * _rotationOffset;
            if (EnableBulletRotationDebugLog)
            {
                Debug.Log($"[BulletRotation] init dir={_direction} offset={_rotationOffset.eulerAngles} final={transform.rotation.eulerAngles}");
            }
        }

        _sphereCollider = GetComponent<SphereCollider>();
        _lastPosition = transform.position;
        _travelDistance = 0f;
        Destroy(gameObject, _config.LifeTime);
    }

    private void Update()
    {
        if (_config == null)
        {
            return;
        }

        transform.position += _direction * _config.BulletSpeed * Time.deltaTime;
        if (_direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up) * _rotationOffset;
            if (EnableBulletRotationDebugLog)
            {
                Debug.Log($"[BulletRotation] tick dir={_direction} offset={_rotationOffset.eulerAngles} final={transform.rotation.eulerAngles}");
            }
        }

        CheckHitBetweenFrames();
        _lastPosition = transform.position;
    }

    private void CheckHitBetweenFrames()
    {
        var move = transform.position - _lastPosition;
        var distance = move.magnitude;
        if (distance <= 0.0001f)
        {
            return;
        }

        _travelDistance += distance;

        var radius = _sphereCollider != null ? _sphereCollider.radius * transform.lossyScale.x : 0.05f;
        radius = Mathf.Clamp(radius, 0.03f, 0.18f);
        var hits = Physics.SphereCastAll(_lastPosition, radius, move.normalized, distance, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
        {
            if (EnableBulletDebugLog)
            {
                Debug.Log($"[Bullet] no hit. pos={transform.position} last={_lastPosition} dir={move.normalized} dist={distance:0.###} radius={radius:0.###}");
            }
            return;
        }

        if (EnableBulletDebugLog)
        {
            Debug.Log($"[Bullet] scan hits={hits.Length} pos={transform.position} dist={distance:0.###} radius={radius:0.###}");
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (IsProjectileCollider(hit.collider))
            {
                continue;
            }

            if (ShouldIgnoreGroundHit(hit))
            {
                if (EnableBulletDebugLog)
                {
                    Debug.Log($"[Bullet] ignore early ground collider={hit.collider.name} point={hit.point} normal={hit.normal} travel={_travelDistance:0.###}");
                }
                continue;
            }

            if (EnableBulletDebugLog)
            {
                Debug.Log($"[Bullet] hit collider={hit.collider.name} root={hit.collider.transform.root.name} point={hit.point} trigger={hit.collider.isTrigger}");
            }

            HitCallback?.Invoke(hit.collider, hit.point);
            if (_config != null && _config.DestroyOnHit)
            {
                Destroy(gameObject);
            }
            break;
        }
    }

    private void OnTriggerEnter(Collider foreign)
    {
        if (foreign == null)
        {
            return;
        }

        if (foreign.transform == transform || foreign.transform.IsChildOf(transform))
        {
            return;
        }

        if (IsProjectileCollider(foreign))
        {
            return;
        }

        if (EnableBulletDebugLog)
        {
            Debug.Log($"[Bullet] trigger enter collider={foreign.name} root={foreign.transform.root.name} bullet={name}");
        }

        HitCallback?.Invoke(foreign, transform.position);
        if (_config != null && _config.DestroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private bool ShouldIgnoreGroundHit(RaycastHit hit)
    {
        if (_travelDistance > InitialGroundIgnoreDistance)
        {
            return false;
        }

        if (hit.normal.y < 0.55f)
        {
            return false;
        }

        var colliderName = hit.collider != null ? hit.collider.name : string.Empty;
        if (string.IsNullOrWhiteSpace(colliderName))
        {
            return true;
        }

        colliderName = colliderName.ToLowerInvariant();
        return colliderName.Contains("ground") ||
               colliderName.Contains("terrain") ||
               colliderName.Contains("floor") ||
               colliderName.Contains("road") ||
               colliderName.Contains("land") ||
               colliderName.Contains("simplemesh");
    }

    private bool IsProjectileCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.GetComponentInParent<Bullet>() != null)
        {
            return true;
        }

        var colliderName = collider.name;
        return !string.IsNullOrWhiteSpace(colliderName) &&
               colliderName.IndexOf("bullet", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
