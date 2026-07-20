using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterStatus : MonoBehaviour, ITurnEntity
{
    [Header("CharacterData")]
    [SerializeField] public CharacterData characterData;

    protected EffectSystem effectSystem;
    protected ElementSystem elementSystem;
    protected Animator anim;
    protected SpriteRenderer sr;

    [Header("Current State")]
    // 현재 레벨
    [SerializeField] private int currentLevel;

    // 현재 경험치
    [SerializeField] private int currentEXP;

    // 속도, 행동 게이지
    [SerializeField] private int baseSpeed;
    [SerializeField] private float actionGauge;
    // 최대 체력, 마나
    [SerializeField] private int baseHp;
    [SerializeField] private int baseMp;
    // 현재 체력, 마나
    [SerializeField] private int currentHp;
    [SerializeField] private int currentMp;
    // 기본 공격력
    [SerializeField] private int baseAttack;

    // 기본 방어력
    [SerializeField] private int baseDefense;

    // 기본 저항력
    [SerializeField] private int baseEffectResistance;

    // 기본 회피 (무조건 0)
    [SerializeField] public int baseEvasionRate = 0;

    // 현재 방어중인지
    public bool isDefending = false;

    [HideInInspector] public int cumulativeAggro = 0;

    [Header("Equipped Skills")]
    public List<SkillData> equippedSkills = new List<SkillData>();

    public IReadOnlyList<EffectData> ActiveEffects => effectSystem.activeEffects;

    public List<ElementData> ElementDatas => elementSystem.elementDatas;
    public bool IsFullyBroken => elementSystem.isFullyBroken;

    public int CurrentLevel => currentLevel;

    public int CurrentEXP => currentEXP;

    public int MaxHp
    {
        get
        {
            return baseHp;
        }
    }

    public int MaxMp
    {
        get
        {
            return baseMp;
        }
    }

    public int CurrentHP => currentHp;
    public int CurrentMP => currentMp;

    public bool IsPlayer => characterData != null && characterData.IsPlayer;
    public int Speed => effectSystem.CalculateModifiedStat(baseSpeed, StatusEffectType.SpeedUp, StatusEffectType.SpeedDown, 0);

    public int Attack => effectSystem.CalculateModifiedStat(baseAttack, StatusEffectType.AtkUp, StatusEffectType.AtkDown, 1);

    public int Defense => effectSystem.CalculateModifiedStat(baseDefense, StatusEffectType.DefUp, StatusEffectType.DefDown, 0);

    public int EffectResistance => effectSystem.CalculateModifiedStat(baseEffectResistance, StatusEffectType.ResUp, StatusEffectType.ResDown, 0);

    public int EvasionRate => effectSystem.CalculateModifiedStat(baseEvasionRate, StatusEffectType.EvsUp, StatusEffectType.EvsDown, 0);
    public int AggroLevel
    {
        get
        {
            int baseAggro = characterData != null ? characterData.AggroLevel : 1;

            return Mathf.Max(0, Mathf.RoundToInt(baseAggro + cumulativeAggro));
        }
    }

    [Header("Status Effect States")]
    public bool IsStunned => effectSystem.IsStunned;

    public float CurrentActionGauge
    {
        get => actionGauge;
        set => actionGauge = value;
    }
    public Transform EntityTransform => this.transform;

    public void ApplySaveData(CharacterSaveData saveData)
    {
        this.currentLevel = saveData.currentLevel;
        this.currentEXP = saveData.currentEXP;
        this.currentHp = saveData.currentHp;
        this.equippedSkills = DataManager.Instance.GetSkillSOListFromNames(saveData.learnedSkillNames);
    }

    private void Awake()
    {
        effectSystem = GetComponent<EffectSystem>();
        elementSystem = GetComponent<ElementSystem>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    public virtual void Initialize(CharacterData data, CharacterSaveData saveData = null)
    {
        this.characterData = data;

        currentLevel = characterData.BaseLevel;
        baseHp = characterData.MaxHp;
        baseMp = characterData.MaxMp;
        currentHp = characterData.MaxHp;
        currentMp = 5;
        baseSpeed = characterData.Speed;
        baseAttack = characterData.Attack;
        baseDefense = characterData.Defense;
        actionGauge = 0f;

        // 만약 DataManager에서 넘어온 세이브 데이터(현재 상태)가 있다면 덮어쓰기
        if (saveData != null)
        {
            ApplySaveData(saveData);
        }
        else
        {
            equippedSkills = new List<SkillData>(characterData.defaultSkills);
        }

        if (!data.IsPlayer)
        {
            elementSystem.AssignRandomElements();
            UpdateSkills();
        }

        Debug.Log($"[{gameObject.name}] 초기화 성공! 최종 스피드: {Speed}");
    }

    public void UpdateSkills()
    {
        // 후보군 풀(Candidate Pool) 생성
        List<SkillData> candidatePool = new List<SkillData>();

        foreach (SkillData skill in characterData.allAvailableSkills)
        {
            // 무속성 스킬이면 후보군에 무조건 포함
            if (skill.skillElement == null)
            {
                candidatePool.Add(skill);
                continue;
            }

            // 현재 적이 껍질로 들고 있는 속성과 일치하면 후보군에 포함
            if (elementSystem != null && elementSystem.elementDatas.Contains(skill.skillElement))
            {
                candidatePool.Add(skill);
            }
        }

        int targetEquipCount = Mathf.Min(characterData.maxSkillEquipCount, candidatePool.Count);
        List<SkillData> newEquippedSkills = new List<SkillData>();

        // 무작위 셔플 (Fisher-Yates 알고리즘)
        // 리스트 자체를 랜덤하게 마구 섞어버립니다. (가장 가볍고 중복 없는 정석 방식)
        for (int i = 0; i < candidatePool.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, candidatePool.Count);
            SkillData temp = candidatePool[i];
            candidatePool[i] = candidatePool[randomIndex];
            candidatePool[randomIndex] = temp;
        }

        for (int i = 0; i < targetEquipCount; i++)
        {
            newEquippedSkills.Add(candidatePool[i]);
        }

        // 최종 슬롯 덮어쓰기
        equippedSkills = newEquippedSkills;

        Debug.Log($"[{gameObject.name}] 속성 변경에 따른 스킬 셔플 완료! (후보 {candidatePool.Count}개 중 {equippedSkills.Count}개 장착)");
    }

    public virtual void ApplyHpChange(int finalamount, ElementData hitElement = null, WeaknessSetting rules = null, bool isSharedDamage = false)
    {
        if (characterData == null) return;

        bool isBlocked = false; // 방어 여부 플래그
        if (finalamount < 0 && effectSystem.TryConsumeShield())
        {
            isBlocked = true;
        }

        if (!isBlocked && finalamount < 0)
        {
            // 방어 중이라면 최종 데미지 반감
            if (isDefending) finalamount = Mathf.RoundToInt(finalamount * 0.5f);

            // 브레이크 시스템 처리
            if (elementSystem != null && hitElement != null && rules != null && !IsFullyBroken)
            {
                elementSystem.ProcessBreak(hitElement, rules);
            }
        }

        if (!isBlocked)
        {
            currentHp = Mathf.Clamp(currentHp + finalamount, 0, MaxHp);
            BattleEvents.OnHealthChanged?.Invoke(this, finalamount);
        }

        if (isBlocked)
        {
            // TODO: 막혔을때 이펙트 혹은 힐은 따로 받는 등의 로직
        }

        if (currentHp <= 0)
        {
            Debug.Log($"[{gameObject.name}] 사망!");

            effectSystem.ActiveEffectsClear();

            BattleEvents.OnCharacterDied?.Invoke(this);

            StartCoroutine(DeathRoutine());
        }
    }

    protected virtual int InterceptFinalDamage(int finalDamage)
    {
        return finalDamage;
    }

    public void ApplyMpChange(int amount)
    {
        if (characterData == null) return;
        if (amount == 0) return;

        // 계산 진행 (0 ~ MaxMP 제한)
        currentMp = Mathf.Clamp(currentMp + amount, 0, MaxMp);

        BattleEvents.OnMpChanged?.Invoke(this, currentMp);
    }

    // 사망 연출
    private System.Collections.IEnumerator DeathRoutine()
    { 
        if (anim != null)
        {
            // 애니메이션 트리거 작동 (애니메이터의 스테이트 이름이 "Die"라고 가정)
            anim.Play("Die");

            // 트리거가 반영되어 실제로 스테이트 전환이 시작될 때까지 1프레임 대기
            yield return null;

            // 다른 애니메이션에서 "Die" 스테이트로 완전히 전환될 때까지 대기 (트랜지션 시간 고려)
            while (!anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
            {
                yield return null;
            }

            // "Die" 애니메이션 재생이 완전히 끝날 때(진행도 100% = 1.0f 이상)까지 대기
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null;
            }

            // 애니메이션이 끝났으므로 Animator를 비활성화
            // -> 애니메이터가 켜져 있으면 SpriteRenderer를 매 프레임 강제로 덮어쓰기 때문에 꺼야 합니다.
            anim.enabled = false;
        }

        // 최종 죽음 스프라이트로 완전히 고정
        if (sr != null && characterData != null && characterData.DeathSprite != null)
        {
            sr.sprite = characterData.DeathSprite;
        }

        Debug.Log($"[{gameObject.name}] 사망 연출 완료 및 스프라이트 고정");
    }

    public void Revive(int healPercent)
    {
        if (currentHp > 0) return;

        if (sr != null && characterData != null && characterData.BasicSprite != null)
        {
            sr.sprite = characterData.BasicSprite;
        }

        if (anim != null)
        {
            // 꺼두었던 애니메이터를 다시 활성화
            anim.enabled = true;

            // 죽어있는 스프라이트 상태에서 즉시 Idle 상태 0초 시점으로 강제 리셋 및 재생
            anim.Play("Idle", 0, 0f);
        }

        // 체력 회복 및 UI 갱신
        currentHp = Mathf.RoundToInt((healPercent / 100f) * MaxHp);
        BattleEvents.OnHealthChanged.Invoke(this, currentHp);

        Debug.Log($"[{gameObject.name}] 부활! (체력: {currentHp})");

        // 타임라인 새로고침하라고 방송
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    // 파티원 중 나를 대신해줄 보호자가 있는지 찾습니다.
    public CharacterStatus GetActiveProtector()
    {
        // 전체 전투원 목록 가져오기
        List<ITurnEntity> allCombatants = BattleEvents.RequestAllCombatants?.Invoke();
        if (allCombatants == null) return null;

        foreach (var c in allCombatants)
        {
            CharacterStatus s = c.EntityTransform.GetComponent<CharacterStatus>();

            // 1. 내가 아니고, 2. 살아있고, 3. 같은 편이며, 4. DamageShare 버프가 있는 캐릭터
            if (s != null && s != this && s.CurrentHP > 0 && s.IsPlayer == this.IsPlayer)
            {
                if (s.effectSystem.HasStatusEffect(StatusEffectType.DamageShare))
                {
                    return s; // 든든한 탱커 발견!
                }
            }
        }
        return null;
    }

    public float GetDamageShareRatio()
    {
        return effectSystem.GetDamageShareRatio();
    }

    public bool HasEffect(StatusEffectType type)
    {
        return effectSystem != null && effectSystem.HasStatusEffect(type);
    }

    public EffectData GetStatusEffect(StatusEffectType type)
    {
        return effectSystem.GetStatusEffect(type);
    }

    public void AddStatEffect(StatusEffectType type, float amount, int turns, EffectModifierType modifierType)
    {
        effectSystem.AddStatEffect(type, amount, turns, modifierType);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        effectSystem.RemoveStatusEffect(type);
    }

    public void CleanseDebuffs()
    {
        effectSystem.CleanseDebuffs();
    }

    public int ConsumeStatusEffect(StatusEffectType effect)
    {
        return effectSystem.ConsumeStatusEffect(effect);
    }

    public void TickEffects()
    {
        if (effectSystem != null)
        {
            effectSystem.TickEffects();
        }
    }

    public bool ProcessTurnStartEffects()
    {
        // effectSystem이 null일 경우를 대비해 안전하게 호출
        if (effectSystem != null)
        {
            return effectSystem.ProcessTurnStartEffects();
        }

        // 컴포넌트가 없으면 도트 데미지로 죽을 일도 없음(false)
        return false;
    }

    public bool CheckWillBreak(ElementData hitElement, WeaknessSetting rules)
    {
        // effectSystem이 null일 경우를 대비해 안전하게 호출
        if (elementSystem != null)
        {
            return elementSystem.CheckWillBreak(hitElement, rules);
        }

        // 컴포넌트가 없으면 도트 데미지로 죽을 일도 없음(false)
        return false;
    }
}