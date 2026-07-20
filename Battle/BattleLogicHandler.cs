using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BattleLogicHandler : MonoBehaviour
{
    private CharacterStatus attackerStatus;
    private List<ITurnEntity> allCombatants;

    private void Awake()
    {
        attackerStatus = GetComponent<CharacterStatus>();
    }

    public void SetCombatants(List<ITurnEntity> allCombatants)
    {
        this.allCombatants = allCombatants;
    }

    public void ExecuteDefendLogic()
    {
        Debug.Log($"[{attackerStatus.name}]가 방어 태세를 취합니다.");

        // TODO: 값 정하기
        attackerStatus.ApplyMpChange(5);
        attackerStatus.isDefending = true;
        // TODO: 값 정하기
        attackerStatus.CurrentActionGauge = 0f;
    }

    public List<CharacterStatus> ResolveTargets(CharacterStatus mainTarget, SkillData skill)
    {
        HashSet<CharacterStatus> uniqueTargets = new HashSet<CharacterStatus>();

        // 스킬의 모든 페이로드 수집 (Step이 있다면 Step까지 포함)
        List<EffectGroup> allPayloads = new List<EffectGroup>();
        if (skill.skillEffects != null) allPayloads.AddRange(skill.skillEffects);
        if (skill.skillSteps != null)
        {
            foreach (var step in skill.skillSteps)
                if (step.stepEffects != null) allPayloads.AddRange(step.stepEffects);
        }

        // 각 페이로드가 노리는 진짜 타겟들을 모아서 중복 없이(HashSet) 저장
        foreach (var groupPayload in allPayloads)
        {
            var payloadTargets = GetIdealTargetsForPayload(groupPayload, mainTarget);
            foreach (var t in payloadTargets)
            {
                uniqueTargets.Add(t);
            }
        }

        return uniqueTargets.ToList();
    }

    private List<CharacterStatus> GetIdealTargetsForPayload(EffectGroup groupPayload, CharacterStatus mainTarget)
    {
        List<CharacterStatus> resolved = new List<CharacterStatus>();

        foreach(var payload in groupPayload.payloads)
        {
            switch (payload.effectTarget)
            {
                case EffectTargetCategory.MainTarget:
                    // 스킬의 원래 타겟
                    if (mainTarget != null) resolved.Add(mainTarget);
                    break;

                case EffectTargetCategory.LowestHpEnemies:
                    var aliveEnemies = allCombatants
                        .Select(e => e.EntityTransform.GetComponent<CharacterStatus>())
                        .Where(c => c != null && c.CurrentHP > 0 && c.IsPlayer != this.attackerStatus.IsPlayer);
                    resolved.AddRange(aliveEnemies.OrderBy(c => c.CurrentHP).Take(payload.targetCount));
                    break;

                case EffectTargetCategory.RandomEnemies:
                    // 살아있는 적군 리스트를 만들고
                    var randomEnemies = allCombatants
                        .Select(e => e.EntityTransform.GetComponent<CharacterStatus>())
                        .Where(c => c != null && c.CurrentHP > 0 && c.IsPlayer != this.attackerStatus.IsPlayer)
                        .ToList();

                    // 리스트를 무작위로 섞은(Shuffle) 뒤 N명 뽑기
                    resolved.AddRange(randomEnemies.OrderBy(c => Random.value).Take(payload.targetCount));
                    break;

                case EffectTargetCategory.Self:
                    // 시전자 본인
                    resolved.Add(this.attackerStatus);
                    break;

                case EffectTargetCategory.AllAllies:
                    // 시전자와 같은 진영의 살아있는 모두
                    bool isRevivePayload = payload.effectType == EffectType.Revive;

                    foreach (var entity in allCombatants)
                    {
                        CharacterStatus c = entity.EntityTransform.GetComponent<CharacterStatus>();

                        // 수정: 부활 효과면 시체도 포함, 아니면 산 사람만!
                        bool isTargetValid = isRevivePayload ? true : (c.CurrentHP > 0);

                        if (isTargetValid && c.IsPlayer == this.attackerStatus.IsPlayer)
                            resolved.Add(c);
                    }
                    break;

                case EffectTargetCategory.AllEnemies:
                    // 시전자의 반대 진영 살아있는 모두
                    foreach (var entity in allCombatants)
                    {
                        CharacterStatus c = entity.EntityTransform.GetComponent<CharacterStatus>();
                        if (c.CurrentHP > 0 && c.IsPlayer != this.attackerStatus.IsPlayer)
                            resolved.Add(c);
                    }
                    break;

                case EffectTargetCategory.LowestHpAlly:
                    // 시전자 진영 중 남은 체력 비율(%)이 가장 적은 아군 1명
                    CharacterStatus lowestAlly = null;
                    float lowestRatio = float.MaxValue;
                    foreach (var entity in allCombatants)
                    {
                        CharacterStatus c = entity.EntityTransform.GetComponent<CharacterStatus>();
                        if (c.CurrentHP > 0 && c.IsPlayer == this.attackerStatus.IsPlayer)
                        {
                            float hpRatio = (float)c.CurrentHP / c.MaxHp;
                            if (hpRatio < lowestRatio)
                            {
                                lowestRatio = hpRatio;
                                lowestAlly = c;
                            }
                        }
                    }
                    if (lowestAlly != null) resolved.Add(lowestAlly);
                    break;
            }
        }

        return resolved;
    }

    public Dictionary<EffectGroup, List<CharacterStatus>> CreateTargetSnapshot(CharacterStatus mainTarget, List<EffectGroup> allPayloads)
    {
        var snapshot = new Dictionary<EffectGroup, List<CharacterStatus>>();

        foreach (EffectGroup group in allPayloads)
        {
            if (group == null) continue;

            List<CharacterStatus> rawTargets = GetIdealTargetsForPayload(group, mainTarget);


            HashSet<CharacterStatus> uniqueGroupTargets = new HashSet<CharacterStatus>(rawTargets);

            snapshot[group] = uniqueGroupTargets.ToList();
        }

        return snapshot;
    }

    public void ProcessCost(int costMp, int postActionGauage)
    {
        if (attackerStatus == null) return;

        // MP 감소 (1번만)
        attackerStatus.ApplyMpChange(-costMp);

        // 액션 게이지 갱신 (1번만)
        attackerStatus.CurrentActionGauge = postActionGauage;
    }

    public void ApplyPayloadsWithSnapshot(
    List<CharacterStatus> hitTargets,
    List<EffectGroup> groupPayloadsToApply,
    Dictionary<EffectGroup, List<CharacterStatus>> snapshot,
    ElementData element)
    {
        WeaknessSetting rules = BattleEvents.RequestWeaknessSettings?.Invoke();

        foreach (EffectGroup groupPayload in groupPayloadsToApply)
        {
            if (groupPayload == null) continue;

            // 해당 그룹이 공격 대상(스냅샷)들을 가져옵니다.
            if (!snapshot.TryGetValue(groupPayload, out List<CharacterStatus> lockedTargets))
                continue;

            // 현재 타격된 대상들과의 교집합
            List<CharacterStatus> actualTargets = lockedTargets.Where(t => hitTargets.Contains(t)).ToList();
            if (actualTargets.Count == 0) continue;

            if (groupPayload.groupApplyChance < 100f)
            {
                if (Random.Range(0, 100) > groupPayload.groupApplyChance && !groupPayload.ignoreResistance)
                {
                    Debug.Log($"[Chance] 그룹 효과 발동 실패");
                    continue;
                }
            }

            // [확률/조건 체크]
            // 이제 그룹 전체를 묶어서 판단하지 않고, 각 페이로드 내부에서 판단합니다.
            foreach (var payload in groupPayload.payloads)
            {
                foreach (var target in actualTargets)
                {
                    // 생존 체크
                    if (target.CurrentHP <= 0 && payload.effectType != EffectType.Revive) continue;

                    // [핵심 방어막] 타겟 카테고리 검증 (본인/적군/아군)
                    if (!IsTargetValidForPayload(target, payload)) continue;

                    // 5. [저항/명중 체크]
                    bool isBeneficial = IsBeneficialEffect(payload.effectType);
                    if (!isBeneficial)
                    {
                        if (!CheckResist(target, element, rules)) continue;
                        if (!CheckHit(payload, target)) continue;
                    }

                    // 모든 검증 통과 -> 적용
                    ApplySingleEffect(target, payload, element, rules);
                }
            }
        }
    }

    private bool IsTargetValidForPayload(CharacterStatus target, EffectPayload payload)
    {
        if (target == null) return false;

        switch (payload.effectTarget)
        {
            case EffectTargetCategory.Self:
                return target == this.attackerStatus;

            case EffectTargetCategory.MainTarget:
            case EffectTargetCategory.AllEnemies:
            case EffectTargetCategory.RandomEnemies:
            case EffectTargetCategory.LowestHpEnemies:
                return target.IsPlayer != this.attackerStatus.IsPlayer;

            case EffectTargetCategory.AllAllies:
            case EffectTargetCategory.LowestHpAlly:
                return target.IsPlayer == this.attackerStatus.IsPlayer;
        }

        return true;
    }

    private bool IsBeneficialEffect(EffectType type)
    {
        return type == EffectType.Buff ||
               type == EffectType.Heal ||
               type == EffectType.HealPercentMaxHP ||
               type == EffectType.HealFlat ||
               type == EffectType.RestoreMP ||
               type == EffectType.Cleanse ||
               type == EffectType.TurnBaton ||
               type == EffectType.Revive;
    }

    private bool CheckHit(EffectPayload payload, CharacterStatus target)
    {
        // TODO: 회피 UI 띄우기
        // 공격자 실명(Blind) 체크
        if (attackerStatus.HasEffect(StatusEffectType.Blind) && Random.Range(0f, 100f) < 30f)
        {
            Debug.Log($"[Miss] {attackerStatus.name}이(가) 실명 상태라 공격이 빗나갔습니다!");
            return false;
        }

        float evasionRate = target.EvasionRate;

        // 0~100 사이 랜덤값이 회피율보다 작으면 회피 성공! - 회피율은 최대 75
        if (Random.Range(0f, 100f) < Mathf.Clamp(evasionRate, 0, 75))
        {
            Debug.Log($"{target.name}이 공격을 회피했습니다!");
            return false; // 공격 실패
        }

        return true; // 명중!
    }

    private bool CheckResist(CharacterStatus target, ElementData element, WeaknessSetting rules)
    {
        float baseResist = target.EffectResistance;
        float elementMultiplier = BattleCalculator.CalculateResistance(target, element, rules);
        elementMultiplier = Mathf.Max(0.1f, elementMultiplier);

        // 속성 상성이 반영된 '실질 저항 수치' 계산
        float effectiveResist = baseResist / elementMultiplier;

        // 점감 곡선(Asymptotic Curve) 공식 적용!
        // balanceConstant는 게임 밸런스 상수
        // 이 값이 클수록 저항 확률이 오르는 속도가 둔해집니다.
        float balanceConstant = 100f;

        // effectiveResist가 0 이하일 경우를 대비한 방어코드
        if (effectiveResist <= 0) effectiveResist = 0;

        // 최종 확률은 무조건 0 ~ 99.99...% 사이
        float finalResistChance = (effectiveResist / (effectiveResist + balanceConstant)) * 100f;

        // 판정
        if (Random.Range(0f, 100f) < finalResistChance)
        {
            Debug.Log($"[Resist] {target.name}이(가) 효과에 저항했습니다! (저항 확률: {finalResistChance:F1}%)");
            // TODO: 여기서 적에게 '저항함(Resist)' UI 텍스트를 띄워주는 이벤트 호출
            return false;
        }

        return true;
    }

    // 특정 캐릭터가 스킬의 특수 기믹(상태이상, 속성 등) 조건을 만족했는지 검사하는 함수
    private bool CheckConditionMet(CharacterStatus checkTarget, EffectPayload payload)
    {
        bool conditionMet = true;
        bool hasAnyCondition = false;

        // 상태이상 조건 검사
        if (payload.conditionTargetStatus != StatusEffectType.None)
        {
            hasAnyCondition = true;
            if (!checkTarget.HasEffect(payload.conditionTargetStatus))
                conditionMet = false;
            else Debug.Log("Status 적중");
        }

        if (!hasAnyCondition) return false;
        if (payload.conditionIgnoreResistance) return true;

        // 확률 검사 (조건을 만족했더라도 운이 없으면 안 터짐)
        if (conditionMet && Random.Range(0f, 100f) >= payload.conditionApplyChance)
        {
            conditionMet = false;
            Debug.Log($"[Synergy] {checkTarget.name}의 조건은 만족했지만 확률에 의해 기믹 미발동!");
        }

        return conditionMet;
    }

    // 조건이 발동했을 때 추가 상태이상을 주거나 기존 상태이상을 지우는 함수
    private void ApplySynergyStatusEffects(CharacterStatus checkTarget, EffectPayload payload)
    {
        if (payload.extraStatusOnCondition != StatusEffectType.None)
        {
            checkTarget.AddStatEffect(payload.extraStatusOnCondition, 0, payload.extraStatusTurn, EffectModifierType.Flat);
            Debug.Log($"[Synergy] {checkTarget.name}에게 추가 기믹으로 {payload.extraStatusOnCondition} 부여!");
        }

        if (payload.removeConditionAfterHit && payload.conditionTargetStatus != StatusEffectType.None)
        {
            checkTarget.RemoveStatusEffect(payload.conditionTargetStatus);
            Debug.Log($"[Synergy] {checkTarget.name}의 {payload.conditionTargetStatus} 효과 삭제!");
        }
    }

    private void ApplySingleEffect(CharacterStatus target, EffectPayload payload, ElementData element, WeaknessSetting rules)
    {
        bool targetConditionMet = CheckConditionMet(target, payload);

        Debug.Log(payload.effectType.ToString());

        switch (payload.effectType)
        {
            case EffectType.Damage:
                CharacterStatus protector = target.GetActiveProtector();

                if (protector != null && protector != target)
                {
                    float shareRatio = protector.GetDamageShareRatio();

                    // ----------------------------------------------------
                    // [1] 원래 타겟(Target) 데미지 및 기믹 독립 계산
                    // ----------------------------------------------------
                    float targetEffectValue = payload.effectValue * (1f - shareRatio);
                    int targetDamage = BattleCalculator.CalculateDamage(
                        this.attackerStatus, target, targetEffectValue, payload.formulaType, element, rules
                    );

                    if (targetConditionMet && payload.conditionMultiplier > 0)
                    {
                        targetDamage = Mathf.RoundToInt(targetDamage * payload.conditionMultiplier);
                        Debug.Log($"[Synergy] 타겟({target.name}) 기믹 발동! 데미지 증폭!");
                    }

                    // ----------------------------------------------------
                    // [2] 탱커(Protector) 데미지 및 기믹 독립 계산
                    // ----------------------------------------------------
                    float protectorEffectValue = payload.effectValue * shareRatio;
                    int protectorDamage = BattleCalculator.CalculateDamage(
                        this.attackerStatus, protector, protectorEffectValue, payload.formulaType, element, rules
                    );

                    bool protectorConditionMet = CheckConditionMet(protector, payload);
                    if (protectorConditionMet && payload.conditionMultiplier > 0)
                    {
                        protectorDamage = Mathf.RoundToInt(protectorDamage * payload.conditionMultiplier);
                        Debug.Log($"[Synergy] 탱커({protector.name}) 기믹 발동! 데미지 증폭!");
                    }

                    // ----------------------------------------------------
                    // [3] 최종 데미지 적용 및 연계 상태이상 처리
                    // ----------------------------------------------------
                    target.ApplyHpChange(-targetDamage, element, rules);
                    protector.ApplyHpChange(-protectorDamage, element, rules, true);

                    if (this.attackerStatus != null)
                    {
                        var extraHitBuff = this.attackerStatus.GetStatusEffect(StatusEffectType.ExtraHitChance);

                        // 기획: 무조건 50% 확률로 발동
                        if (extraHitBuff != null && UnityEngine.Random.Range(0f, 100f) <= 50f)
                        {
                            // 기획: 버프의 amount를 데미지 배율로 사용 (예: amount 60 = 60% 데미지)
                            float extraMultiplier = extraHitBuff.amount / 100f;
                            int targetExtraDamage = Mathf.RoundToInt(targetDamage * extraMultiplier);

                            Debug.Log($"[추가타 발동!] 타겟 본 데미지: {targetDamage} -> 추가 데미지: {targetExtraDamage}");

                            // 추가 데미지 즉시 적용
                            target.ApplyHpChange(-targetExtraDamage);

                            int protectorExtraDamage = Mathf.RoundToInt(protectorDamage * extraMultiplier);

                            Debug.Log($"[추가타 발동!] 보호자 본 데미지: {protectorDamage} -> 추가 데미지: {protectorExtraDamage}");

                            // 추가 데미지 즉시 적용
                            protector.ApplyHpChange(-protectorExtraDamage);

                            // TODO: 추가타가 터졌다는 걸 시각적으로 알리기 위해 아주 가벼운 이벤트 쏘기
                            // BattleEvents.OnExtraHitVisualRequested?.Invoke(target);
                        }
                    }

                    if (protectorConditionMet) ApplySynergyStatusEffects(protector, payload);

                    Debug.Log($"[Damage Share] 타겟({target.name}): {targetDamage} 피해, 탱커({protector.name}): {protectorDamage} 피해 분산!");
                }
                else
                {
                    // 보호자가 없을 때의 일반 로직
                    int damage = BattleCalculator.CalculateDamage(
                        this.attackerStatus, target, payload.effectValue, payload.formulaType, element, rules
                    );

                    if (targetConditionMet && payload.conditionMultiplier > 0)
                        damage = Mathf.RoundToInt(damage * payload.conditionMultiplier);

                    target.ApplyHpChange(-damage, element, rules);

                    if (this.attackerStatus != null)
                    {
                        var extraHitBuff = this.attackerStatus.GetStatusEffect(StatusEffectType.ExtraHitChance);

                        // 기획: 무조건 50% 확률로 발동
                        if (extraHitBuff != null && UnityEngine.Random.Range(0f, 100f) <= 50f)
                        {
                            // 기획: 버프의 amount를 데미지 배율로 사용 (예: amount 60 = 60% 데미지)
                            float extraMultiplier = extraHitBuff.amount / 100f;
                            int extraDamage = Mathf.RoundToInt(damage * extraMultiplier);

                            Debug.Log($"[추가타 발동!] 본 데미지: {damage} -> 추가 데미지: {extraDamage}");

                            // 추가 데미지 즉시 적용
                            target.ApplyHpChange(-extraDamage);

                            // TODO: 추가타가 터졌다는 걸 시각적으로 알리기 위해 아주 가벼운 이벤트 쏘기
                            // BattleEvents.OnExtraHitVisualRequested?.Invoke(target);
                        }
                    }

                    Debug.Log($"타겟({target.name}): {damage} 피해");
                }
                break;

            case EffectType.Heal:
            case EffectType.HealPercentMaxHP:
            case EffectType.HealFlat:
                // 힐량 통합 계산 (가독성을 위해 스위치 안에서 로직 분리해도 좋음)
                int finalHeal = 0;
                if (payload.effectType == EffectType.Heal) finalHeal = BattleCalculator.CalculateHeal(attackerStatus, payload.effectValue);
                else if (payload.effectType == EffectType.HealPercentMaxHP) finalHeal = Mathf.Max(1, Mathf.RoundToInt(target.MaxHp * (payload.effectValue / 100f)));
                else finalHeal = Mathf.Max(1, Mathf.RoundToInt(payload.effectValue));

                // [저주 기믹] 대상을 확인해서 저주가 있으면 힐을 딜로 바꿈
                if (target.HasEffect(StatusEffectType.Curse))
                {
                    Debug.Log($"[Curse] 저주로 인해 {target.name}의 힐({finalHeal})이 데미지로 변환됩니다!");
                    target.ApplyHpChange(-finalHeal, null, null); // 데미지로 적용
                }
                else
                {
                    target.ApplyHpChange(finalHeal);
                    Debug.Log($"[Heal] {target.name} 체력 {finalHeal} 회복!");
                }
                break;

            case EffectType.RestoreMP:
                int mpAmount = Mathf.RoundToInt(payload.effectValue);
                target.ApplyMpChange(mpAmount);
                Debug.Log($"[MP] {target.name} 마나 {mpAmount} 회복!");
                break;

            case EffectType.TurnBaton:
                BattleEvents.OnTurnOverrideRequested?.Invoke(target);
                Debug.Log($"[Turn] {target.name}에게 다음 턴이 강제 부여됨!");
                break;

            case EffectType.Revive:
                if (target.CurrentHP <= 0) // 죽어있을 때만
                {
                    // effectValue를 퍼센트(%)로 사용 (예: 50이면 50% 체력으로 부활)
                    target.Revive(Mathf.RoundToInt(payload.effectValue));
                }
                break;

            case EffectType.Buff:
            case EffectType.Debuff:
                target.AddStatEffect(payload.statusEffectType, payload.effectValue, payload.durationTurns, payload.modifierType);
                Debug.Log($"[Status] {target.name}에게 {payload.statusEffectType} 부여! ({payload.durationTurns}턴)");
                break;

            case EffectType.Cleanse:
                target.CleanseDebuffs();
                Debug.Log($"[Cleanse] {target.name}의 디버프 정화!");
                break;

            case EffectType.ConsumeStatusAndRestoreMP:
                int consumedTurns = target.ConsumeStatusEffect(payload.statusEffectType);

                if (consumedTurns > 0)
                {
                    // 남은 턴 수만큼 MP 회복 (예: 1턴당 10 MP)
                    int mpToRestore = consumedTurns;
                    attackerStatus.ApplyMpChange(mpToRestore);
                    Debug.Log($"{target.name}이 {consumedTurns}턴의 화상을 흡수하여 {mpToRestore} MP를 회복했습니다!");
                }
                break;
        }

        if (targetConditionMet && target.CurrentHP > 0)
        {
            ApplySynergyStatusEffects(target, payload);
        }
    }
}
