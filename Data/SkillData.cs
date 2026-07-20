using System.Collections.Generic;
using UnityEngine;

public enum ProjectileMotionType
{
    StopAtTarget, // 타겟 위치에서 멈추고 터짐 (파이어볼, 화살)
    Penetrate     // 타겟을 뚫고 화면 밖으로 날아감 (검기, 파도)
}

// 다단히트 스텝 데이터 구조체
[System.Serializable]
public class SkillStep
{
    [Tooltip("해당 타격 순서(Index)에서 터질 효과 리스트")]
    public List<EffectGroup> stepEffects = new List<EffectGroup>();
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill Data")]
public class SkillData : ScriptableObject, IEffectProvider
{
    [Header("Basic Settings")]
    public string skillID;            // 스킬 ID
    public string skillName;          // 스킬 이름
    [TextArea(1, 5)]
    public string skillDescription;   // 스킬 설명
    public int mpCost;                // 스킬 발동 자체의 MP 소모량
    public int postActionGaugeDelay;  // ActionGauge 소모량

    [Header("Timing Settings")]
    [Tooltip("시전 이펙트(마법진 등)가 나오고 투사체가 발사되기 전까지 대기하는 캐스팅 시간")]
    public float castDelay = 0f;

    public float effectDuration = 1.5f;

    public int GetPostActionGauge() => postActionGaugeDelay;

    public bool isAoE; // 광역기 여부

    [Header("Effect Settings")]
    public float castEffectScale = 1.0f;
    public float projectileEffectScale = 1.0f;
    public float effectScale = 1.0f;

    [Tooltip("체크하면 타겟 각각의 위치에 이펙트가 개별 생성됩니다. (체크 해제 시 타겟들의 중앙에 1개만 생성)")]
    public bool spawnOnEachTarget = true;

    [Header("Timing Settings")]
    [Tooltip("타격 후 원래 자리로 돌아가기 전까지 대기 시간")]
    public float postHitDelay = 0.4f;

    [Header("Visuals")]
    public Sprite skillIcon;

    [Header("Attribute")]
    public ElementData skillElement;

    [Header("Animation Settings")]
    public bool isRanged; // 원거리 스킬인가? (true이면 적 앞으로 이동하지 않음)

    public string prepAnimName;
    public string animName = "Skill";

    [Header("Projectile Settings")]
    [SerializeField, Tooltip("활, 파이어볼 등 날아가는 투사체 프리팹 (없으면 즉발 혹은 머리 위 소환)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;

    [Tooltip("투사체의 이동 방식 (타겟에서 폭발할지, 뚫고 지나갈지)")]
    public ProjectileMotionType projectileMotionType = ProjectileMotionType.StopAtTarget;

    [Tooltip("관통형일 경우, 타겟을 지나쳐 얼마나 더 날아갈 것인가?")]
    public float flyThroughDistance = 15f;

    // TODO: 필요 시 사용
    //[Header("Camera Feedback")]
    //[Tooltip("타격 순간의 카메라 흔들림 강도 (0이면 안 흔들림)")]
    //public float cameraShakeIntensity = 0f;
    //public float cameraShakeDuration = 0.2f;

    [Tooltip("에너지파, 레이저 등 즉발로 쭈욱 이어지는 이펙트 프리팹")]
    public GameObject laserPrefab;

    [Tooltip("스킬 시전 시 내 몸에서 터질 이펙트 (마법진 등)")]
    public GameObject castEffectPrefab;

    [Tooltip("스킬이 적중할 때 적 몸/머리 위에서 터질 이펙트 (폭발, 연속 베기, 메테오 스프라이트 시트 등)")]
    public GameObject hitEffectPrefab;

    [Tooltip("아군에게 적용될 때 터질 이펙트 (힐, 버프 등)")]
    public GameObject beneficialEffectPrefab;

    [Header("TargettingSet")]
    public TargetType validTargetGroup;

    [Header("Logic Settings (Single Hit)")]
    [Tooltip("단발성 스킬일 때 발동될 효과 리스트")]
    public List<EffectGroup> skillEffects = new List<EffectGroup>();

    [Header("Logic Settings (Multi Hit)")]
    [Tooltip("다단히트 스킬일 때 애니메이션 이벤트 Index(0,1,2..)와 매칭되어 실행될 단계별 효과 리스트")]
    public List<SkillStep> skillSteps = new List<SkillStep>();

    // IEffectProvider 인터페이스 구현 
    // (다단히트 스킬인 경우 모든 스텝의 효과를 모아서 반환하여 AI나 시스템이 인지하도록 함)
    public List<EffectGroup> GetEffects()
    {
        if (skillSteps != null && skillSteps.Count > 0)
        {
            List<EffectGroup> allEffects = new List<EffectGroup>();
            foreach (var step in skillSteps)
            {
                allEffects.AddRange(step.stepEffects);
            }
            return allEffects;
        }
        return skillEffects;
    }

    [Header("AI Setting")]
    public int lastUsedValue;
}