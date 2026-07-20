using System.Collections;
using UnityEngine;

public class ActionVisualizer : MonoBehaviour
{
    public CommonBattleVFX commonVFX;

    private Animator anim;

    private void Awake() => anim = GetComponentInChildren<Animator>();

    public void PlayAnimation(string stateName)
    {
        // 애니메이션을 재생할 때마다 자식 모델링의 Animator를 찾음 (동적 생성 대응)
        Animator anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogError("Animator를 찾을 수 없습니다! 모델링이 제대로 생성/연결되었는지 확인하세요.");
            return;
        }

        anim.Play(stateName, 0, 0f);
    }

    public GameObject PlayEffect(GameObject prefab, Vector3 position, float scale = 1.0f)
    {
        if (prefab != null)
        {
            GameObject fx = Instantiate(prefab, position, Quaternion.identity);
            fx.transform.localScale = Vector3.one * scale; // 생성 직후 크기 조절

            return fx;
        }

        return null;
    }

    public IEnumerator MoveTo(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;

        float elapsed = 0f;
        float moveDirX = targetPos.x - startPos.x;

        if (Mathf.Abs(moveDirX) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            // 원래 크기 비율(Mathf.Abs)은 유지하면서 방향(Sign)만 결정합니다.
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveDirX);
            transform.localScale = scale;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }
        transform.position = targetPos;
    }

    public GameObject PlayProjectileWithReturn(GameObject prefab, Vector3 spawnPos, Vector3 targetPos, float speed, out Coroutine projCoroutine)
    {
        GameObject projObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        projCoroutine = StartCoroutine(ProjectileMovementRoutine(projObj, targetPos, speed));

        return projObj;
    }

    // 투사체가 날아가고 도달할 때까지 대기하는 실제 로직 (기존 코드 재활용)
    private IEnumerator ProjectileMovementRoutine(GameObject projObj, Vector3 targetPos, float speed)
    {
        // 이미 위에서 생성된 projObj를 매개변수로 받아서 사용합니다.
        Projectile proj = projObj.GetComponent<Projectile>();
        bool isHit = false;

        GameObject dummy = new GameObject("Dummy");
        dummy.transform.position = targetPos;

        if (proj != null)
        {
            proj.Fire(dummy.transform, speed, () => isHit = true);
            yield return new WaitUntil(() => isHit);
        }
        else
        {
            Debug.LogWarning("투사체 프리팹에 Projectile 스크립트가 없습니다!");
            yield return new WaitForSeconds(0.5f);
        }

        // 목적지에 도달하면 더미 삭제
        Destroy(dummy);
    }

    // 레이저 연출도 이곳으로 분리
    public GameObject PlayLaser(GameObject prefab, Vector3 spawnPos, Vector3 targetPos)
    {
        if (prefab == null) return null;
        GameObject laserObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        LaserVFX laser = laserObj.GetComponent<LaserVFX>();
        if (laser != null) laser.FireLaser(spawnPos, targetPos);

        return laserObj;
    }
}