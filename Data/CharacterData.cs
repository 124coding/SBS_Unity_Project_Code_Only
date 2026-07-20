using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Battle/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Visuals")]
    public Sprite characterIcon;
    public GameObject visualModelPrefab;

    [Header("AI Phases & Patterns")]
    public List<EnemyPhase> phaseList = new List<EnemyPhase>();
    public List<SpecialPattern> specialPatterns = new List<SpecialPattern>();

    public enum CharacterSize { Small = 1, Medium = 2, Large = 3}
    public CharacterSize mySize = CharacterSize.Medium;

    [Header("Player Only")]
    [SerializeField] private int aggroLevel;

    [Header("Base stats")]
    [SerializeField] private int baseLevel = 1;
    [SerializeField] private string characterName;
    [SerializeField] private int maxHp;
    [SerializeField] private int maxMp;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int speed;
    [SerializeField] private int effectResistance;
    [SerializeField] private bool isPlayer;
    [SerializeField] private Sprite basicSprite;
    [SerializeField] private Sprite deathSprite;

    [Header("Basic Action")]
    [Tooltip("이 캐릭터의 평타 (마나 소모 0짜리 스킬 데이터를 넣으세요)")]
    public SkillData basicAttackData;

    [Header("Skills")]
    [Tooltip("이 몬스터(또는 초기 플레이어)가 기본적으로 가질 스킬들")]
    public List<SkillData> defaultSkills = new List<SkillData>();

    [Header("EnemyOnly")]
    [Tooltip("이 몬스터가 가질 스킬 갯수")]
    public int maxSkillEquipCount = 4;

    [Tooltip("이 몬스터가 가질 수 있는 속성")]
    public List<ElementData> allAvailableElement = new List<ElementData>();

    [Tooltip("이 몬스터가 가진 속성별 스킬 풀")]
    public List<SkillData> allAvailableSkills = new List<SkillData>();

    public AIPersonalityProfile aiProfile;

    public int AggroLevel => aggroLevel;

    public int BaseLevel => baseLevel;
    public string CharacterName => characterName;
    public int MaxHp => maxHp;
    public int MaxMp => maxMp;
    public int Attack => attack;
    public int Defense => defense;
    public int Speed => speed;
    public int EffectResistance => effectResistance;
    public bool IsPlayer => isPlayer;

    public Sprite BasicSprite => basicSprite;
    public Sprite DeathSprite => deathSprite;
}
