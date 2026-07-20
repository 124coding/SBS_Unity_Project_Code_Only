using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EffectData
{
    public StatusEffectType effectType;
    public float amount;    // 변화량 (버프면 양수, 디버프면 음수)
    public int remainingTurns;   // 남은 턴 수
    public EffectModifierType modifierType;

    public Sprite EffectIcon;

    // 생성자
    public EffectData(StatusEffectType type, float amount, int turns, EffectModifierType modifierType)
    {
        effectType = type;
        this.amount = amount;
        remainingTurns = turns;
        this.modifierType = modifierType;
    }
}

public class EffectSystem : MonoBehaviour
{
    private CharacterStatus characterStatus;

    // 몸에 걸린 모든 버프/디버프를 관리
    [SerializeField] public List<EffectData> activeEffects = new List<EffectData>();

    public int CalculateModifiedStat(int baseValue, StatusEffectType upEffect, StatusEffectType downEffect, int minValue = 0)
    {
        // 시체라면 스피드 등을 0으로 처리 (기존 동일)
        if (characterStatus != null && characterStatus.CurrentHP <= 0 && upEffect == StatusEffectType.SpeedUp) return 0;

        int bonus = 0;
        float multiplier = 100f;

        // 순수하게 업/다운 효과들만 먼저 계산합니다.
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == upEffect)
            {
                if (effect.modifierType == EffectModifierType.Flat) bonus += (int)effect.amount;
                else if (effect.modifierType == EffectModifierType.Percent) multiplier += effect.amount;
            }
            else if (effect.effectType == downEffect)
            {
                if (effect.modifierType == EffectModifierType.Flat) bonus -= (int)effect.amount;
                else if (effect.modifierType == EffectModifierType.Percent) multiplier -= effect.amount;
            }
        }

        // 기본 계산 완료 (베이스 스탯 + 고정 버프) * 퍼센트 버프
        int finalStat = Mathf.RoundToInt((multiplier / 100f) * (baseValue + bonus));

        // 최종값이 최소값(보통 0)보다 아래로 떨어지지 않게 제한해서 반환
        return Mathf.Max(minValue, finalStat);
    }

    public bool IsStunned => HasStatusEffect(StatusEffectType.Electrocute) || HasStatusEffect(StatusEffectType.Nightmare);

    public float GetDamageShareRatio()
    {
        var effect = activeEffects.Find(e => e.effectType == StatusEffectType.DamageShare);
        return effect != null ? (effect.amount / 100f) : 0f;
    }

    private void Awake()
    {
        // 같은 게임오브젝트에 붙어있는 CharacterStatus 컴포넌트를 가져옵니다.
        characterStatus = GetComponent<CharacterStatus>();

        if (characterStatus == null)
        {
            Debug.LogError($"[{gameObject.name}] EffectSystem은 CharacterStatus가 필요합니다!");
        }
    }

    public void ActiveEffectsClear()
    {
        activeEffects.Clear();
    }

    public void RemoveEffect(EffectData effect)
    {
        activeEffects.RemoveAll(e => e.effectType == effect.effectType && e.modifierType == effect.modifierType);
    }

    public EffectData GetEffect(EffectData effect) {
        return activeEffects.Find(e => e.effectType == effect.effectType || e.modifierType == effect.modifierType);
    }

    public void AddStatEffect(StatusEffectType type, float amount, int turns, EffectModifierType modifierType)
    {
        // 부식 상태일 때 방어력 증가 버프 불가
        if (type == StatusEffectType.DefUp && HasStatusEffect(StatusEffectType.Corrosion))
        {
            Debug.Log($"[Corrosion] 부식 상태이므로 방어력 증가를 획득할 수 없습니다!");
            return; // 버프 무시
        }

        // 부식이 걸릴 때, 기존 방어력 증가 버프 삭제
        if (type == StatusEffectType.Corrosion)
        {
            activeEffects.RemoveAll(e => e.effectType == StatusEffectType.DefUp && e.modifierType == EffectModifierType.Percent);
            Debug.Log($"[Corrosion] 녹이 슬었습니다! 기존 방어력 증가(턴제) 버프가 삭제됩니다.");
        }

        var existingEffect = activeEffects.Find(e => e.effectType == type && e.modifierType == modifierType);

        if (existingEffect != null)
        {
            // 3. 중복 적용 로직 (영구 vs 턴제 분기)
            if (existingEffect.remainingTurns == -1)
            {
                // [영구 버프] 수치(Amount)를 누적해서 합산함 (슬더스의 '근력' 방식)
                existingEffect.amount += amount;
                Debug.Log($"[{type}] 영구 버프 중첩! 수치가 {amount}만큼 증가하여 총 {existingEffect.amount}이 되었습니다.");
            }
            else
            {
                // [턴제 상태이상] 턴(Turns) 수를 연장하거나 갱신함

                // 방식 A: 기존 턴 수에 새로운 턴 수를 더함 (슬더스의 '취약', '약화' 방식)
                existingEffect.remainingTurns += turns;

                // 방식 B: 더 긴 턴 수로 덮어씌움 (일반적인 RPG 방식)
                // existingEffect.turns = Mathf.Max(existingEffect.turns, turns);

                // (선택) 만약 새로 들어온 턴제 버프의 수치가 기존보다 더 높다면 덮어씌움
                //if (amount > existingEffect.amount)
                //{
                //    existingEffect.amount = amount;
                //}

                Debug.Log($"[{type}] 턴제 효과 갱신! 턴 수가 {existingEffect.remainingTurns}턴으로 늘어났습니다.");
            }
        }
        else
        {
            // 새로운 상태이상인 경우 기존처럼 새로 추가
            EffectData newData = new EffectData(type, amount, turns, modifierType);
            newData.EffectIcon = EffectIconDatabase.Instance.GetIcon(type);
            activeEffects.Add(newData);
            Debug.Log($"[{type}] 새로운 효과 추가됨! (수치: {amount}, 턴: {turns})");
        }

        // UI 및 스탯 갱신 이벤트 호출 (버프 아이콘 업데이트, 최종 스탯 재계산 등)
        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
    }

    public void TickEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].remainingTurns == -1)
            {
                continue;
            }

            activeEffects[i].remainingTurns--;

            if (activeEffects[i].remainingTurns <= 0)
            {
                activeEffects.RemoveAt(i);
            }
        }
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == type) return true;
        }
        return false;
    }

    public EffectData GetStatusEffect(StatusEffectType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == type)
            {
                return effect;
            }
        }

        return null;
    }


    public void RemoveStatusEffect(StatusEffectType type)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == type)
            {
                activeEffects.Remove(effect);
                return;
            }
        }

        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    public bool TryConsumeShield()
    {
        // 'Shield' 타입의 효과를 찾습니다.
        EffectData shieldEffect = activeEffects.Find(e => e.effectType == StatusEffectType.Shield);

        if (shieldEffect != null)
        {
            // 방어막을 소모하고 리스트에서 제거
            activeEffects.Remove(shieldEffect);

            // UI 갱신 알림
            BattleEvents.OnEffectsChanged?.Invoke(characterStatus);

            Debug.Log($"{gameObject.name}이 방어막으로 공격을 막았습니다!");
            return true; // 방어 성공
        }

        return false; // 방어막 없음
    }

    public void CleanseDebuffs()
    {
        // 해로운 디버프 타겟팅 조건 정의
        // 스탯 감소(Down), 감전, 실명, 화상, 젖음, 저주, 악몽, 부식 등 모든 부정적 효과 제거
        activeEffects.RemoveAll(e =>
            e.effectType == StatusEffectType.AtkDown ||
            e.effectType == StatusEffectType.DefDown ||
            e.effectType == StatusEffectType.SpeedDown ||
            e.effectType == StatusEffectType.ResDown ||
            e.effectType == StatusEffectType.Electrocute ||
            e.effectType == StatusEffectType.Blind ||
            e.effectType == StatusEffectType.Burn ||
            e.effectType == StatusEffectType.Wet ||
            e.effectType == StatusEffectType.Curse ||
            e.effectType == StatusEffectType.Nightmare ||
            e.effectType == StatusEffectType.Corrosion
        );

        Debug.Log($"[{gameObject.name}]의 모든 해로운 상태이상 및 디버프가 정화되었습니다!");

        // 상태가 변경되었음을 UI와 타임라인 매니저에 알림
        BattleEvents.OnEffectsChanged?.Invoke(characterStatus);
        BattleEvents.OnTimelineUpdateRequested?.Invoke();
    }

    public int ConsumeStatusEffect(StatusEffectType type)
    {
        // 해당 상태 이상 찾기
        EffectData targetEffect = activeEffects.Find(e => e.effectType == type);

        if (targetEffect != null)
        {
            int remainingTurns = targetEffect.remainingTurns;

            // 상태 이상 제거
            activeEffects.Remove(targetEffect);

            // UI 갱신 알림
            BattleEvents.OnEffectsChanged?.Invoke(characterStatus);

            return remainingTurns; // 제거한 턴 수 반환
        }

        return 0; // 없으면 0 반환
    }

    public bool ProcessTurnStartEffects()
    {
        if (characterStatus.CurrentHP <= 0) return true; // 이미 시체면 무시

        int totalDotDamage = 0;

        // activeEffects는 이전 턴에 EffectData 타입으로 들어간 상태이상 리스트입니다.
        foreach (var effect in activeEffects)
        {
            if (effect.effectType == StatusEffectType.Burn)
            {
                int calculatedDamage = Mathf.Max(1, Mathf.RoundToInt(characterStatus.MaxHp * 0.05f));

                totalDotDamage += calculatedDamage;
            }
        }

        // 피해량 처리
        if (totalDotDamage > 0)
        {
            characterStatus.ApplyHpChange(-totalDotDamage, null, null);
        }

        // 도트 데미지를 입고 죽었는지 여부 반환
        return characterStatus.CurrentHP <= 0;
    }
}
