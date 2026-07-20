using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FieldPlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackSpeed = 0.5f;    // 공격 쿨타임
    [Tooltip("이펙트가 생성된 후, 실제 타격 판정이 들어갈 때까지의 미세한 딜레이 (즉발을 원하면 0)")]
    public float attackHitDelay = 0.05f;

    [Header("Visual Effects")]
    private FieldPlayerSwapManager swapManager;
    public float effectDestroyTime = 0.5f; // 이펙트가 화면에서 사라질 시간 (메모리 누수 방지)

    public Transform attackPoint;       // 공격 판정 위치
    public Vector2 attackBoxSize = new Vector2(3f, 3f); // 타격 박스 크기
    public LayerMask targetLayer;       // 때릴 수 있는 레이어 (기존 groundLayer 대신 HitBox 등 지정 추천)

    private float attackTimeCounter;
    private InputAction attackAction; // 액션을 미리 캐싱

    private void Awake()
    {
        swapManager = GetComponent<FieldPlayerSwapManager>();
    }

    private void Start()
    {
        attackAction = InputManager.Instance.inputActions.Field.Attack;
    }

    private void Update()
    {

        // 공격 쿨타임 감소
        if (attackTimeCounter > 0)
        {
            attackTimeCounter -= Time.deltaTime;
        }

        // 공격 입력 확인 및 실행
        if (attackAction.WasPressedThisFrame() && attackTimeCounter <= 0)
        {
            StartCoroutine(AttackRountine());
        }
    }

    private IEnumerator AttackRountine()
    {
        attackTimeCounter = attackSpeed;

        // TODO: FieldPlayerAnimator 스크립트를 통해 공격 애니메이션 재생!
        // GetComponent<FieldPlayerAnimator>().TriggerAttack();

        GameObject effectPrefab = null;
        float effectScaleMultiplier = 1.0f;
        if (swapManager != null && swapManager.CurrentVisualData != null)
        {
            effectPrefab = swapManager.CurrentVisualData.attackEffectPrefab;
            effectScaleMultiplier = swapManager.CurrentVisualData.effectScaleMultiplier;
        }

        Debug.Log("근접 공격 얍!");

        if (effectPrefab != null && attackPoint != null)
        {
            // 이펙트 생성
            GameObject effect = Instantiate(effectPrefab, attackPoint.position, Quaternion.identity);

            // 플레이어가 바라보는 방향 계산 (오른쪽: 1, 왼쪽: -1)
            float facingDirection = Mathf.Sign(transform.localScale.x);

            // 핵심: 공격 박스 크기(attackBoxSize)를 이펙트 스케일에 강제 주입!
            // X축(가로)에는 캐릭터가 바라보는 방향(facingDirection)까지 곱해줍니다.
            effect.transform.localScale = new Vector3(
                attackBoxSize.x * facingDirection * effectScaleMultiplier,
                attackBoxSize.y * effectScaleMultiplier,
                1f
            );

            Destroy(effect, effectDestroyTime);
        }

        // 미세 선 딜레이 대기
        if (attackHitDelay > 0)
        {
            yield return new WaitForSeconds(attackHitDelay);
        }

        // OverlapBox로 타격 판정 검사
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, targetLayer);

        foreach (var hitObject in hitObjects)
        {
            if (hitObject != null)
            {
                // 부서지는 벽 타격 처리
                if (hitObject.TryGetComponent(out DisappearWall wall))
                {
                    Debug.Log("DisappearWall Hit");
                    wall.HitByAttack();
                }

                if(hitObject.TryGetComponent(out FieldMonster monster))
                {
                    monster.OnHitByPlayerWeapon(gameObject.transform.position);
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
        }
    }
}