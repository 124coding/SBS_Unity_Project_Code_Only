using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectIconDatabase", menuName = "Battle/Effect Icon Database")]
public class EffectIconDatabase : ScriptableObject
{
    [System.Serializable]
    public struct EffectIconMapping
    {
        public StatusEffectType effectType;
        public Sprite icon;
    }

    public static EffectIconDatabase Instance;

    private void OnEnable()
    {
        Instance = this;
    }

    public List<EffectIconMapping> iconMappings;

    // Dictionary로 변환해서 사용하면 검색이 빠릅니다.
    private Dictionary<StatusEffectType, Sprite> iconDict;

    public Sprite GetIcon(StatusEffectType type)
    {
        if (iconDict == null)
        {
            iconDict = new Dictionary<StatusEffectType, Sprite>();
            foreach (var mapping in iconMappings)
                iconDict[mapping.effectType] = mapping.icon;
        }
        return iconDict.TryGetValue(type, out var icon) ? icon : null;
    }
}