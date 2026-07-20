using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// --------------------------------------------------------
// 세이브 데이터를 위한 직렬화 클래스들
// --------------------------------------------------------

[System.Serializable]
public class CharacterSaveData
{
    public string characterName;
    public int currentLevel;
    public int currentEXP;
    public int currentHp;
    public List<string> learnedSkillNames;

    public CharacterSaveData(string name, int level, int exp, int hp, List<string> skillNames)
    {
        this.characterName = name;
        this.currentLevel = level;
        this.currentEXP = exp;
        this.currentHp = hp;
        this.learnedSkillNames = skillNames;
    }
}

[System.Serializable]
public class SaveEntry
{
    public string id;
    public CharacterSaveData data;

    public SaveEntry(string id, CharacterSaveData data)
    {
        this.id = id;
        this.data = data;
    }
}

[System.Serializable]
public class ItemSaveEntry
{
    public string itemName;
    public int quantity;

    public ItemSaveEntry(string name, int qty)
    {
        this.itemName = name;
        this.quantity = qty;
    }
}

[System.Serializable]
public class ObjectStateEntry
{
    public string id;
    public bool state;

    public ObjectStateEntry(string id, bool state)
    {
        this.id = id;
        this.state = state;
    }
}

[System.Serializable]
public class ObjectRotationEntry
{
    public string id;
    public float rotation;

    public ObjectRotationEntry(string id, float rotation)
    {
        this.id = id;
        this.rotation = rotation;
    }
}

[System.Serializable]
public class SaveWrapper
{
    // TODO: 직업 수정 필요
    [Header("캐릭터 및 파티 상태")]
    public List<SaveEntry> characterEntries = new List<SaveEntry>();
    public string currentSelectedCharacter; // 저장 당시 조종하던 캐릭터 타입 (Default, Warrior 등)
    public bool isWarriorUnlocked;          // 전사 해금 여부
    public bool isRogueUnlocked;            // 도적 해금 여부
    public bool isGunnerUnlocked;       // 총잡이 해금 여부

    [Header("인벤토리 아이템")]
    public List<ItemSaveEntry> inventoryEntries = new List<ItemSaveEntry>(); // 가방 리스트

    [Header("맵 탐험 및 내비게이션")]
    public string savedSceneName;           // 플레이어가 서있던 필드 씬 이름
    public Vector3 savedPlayerPosition;     // 플레이어의 정확한 좌표

    [Header("처치 목록 (데스노트)")]
    public List<string> permanentlyDefeated = new List<string>();

    [Header("해금된 휴식 장소")]
    public List<string> unlockedRestAreas = new List<string>();

    [Header("필드 기믹 및 수집품 데이터")]
    public List<string> ownedCards = new List<string>();
    public List<string> unlockedRelics = new List<string>();
    public int fuseCount = 0;

    public List<ObjectStateEntry> objectStates = new List<ObjectStateEntry>();
    public List<ObjectRotationEntry> objectRotations = new List<ObjectRotationEntry>();
}

// --------------------------------------------------------
// 메인 데이터 매니저 (DontDestroyOnLoad)
// --------------------------------------------------------
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("Master Databases (데이터 복구용 원본 창고)")]
    public List<CharacterData> startingPartyTemplates; // 캐릭터 원본 SO 목록
    public List<ItemData> masterItemDatabase;          // 프로젝트 안의 모든 아이템 SO 목록
    public List<SkillData> masterSkillDatabase;        // 프로젝트 안의 모든 스킬 SO 목록
    public List<ElementData> masterElementDatabase;        // 프로젝트 안의 모든 속성 SO 목록

    [Header("Party Runtime Status")]
    public List<CharacterData> currentPartyMembers;    // 현재 파티에 소속된 명단

    [Header("Current Battle Runtime Info")]
    public List<CharacterData> currentEnemyParty;
    public EncounterType currentEncounterType;
    public DirectingType currentDirectingType;
    public string currentBattleMonsterID;
    public bool currentBattleIsRespawnable;

    // 인벤토리 (인게임 런타임용)
    public Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();

    // 캐릭터 실시간 스탯 금고 (마스터 로스터)
    public Dictionary<string, CharacterSaveData> allCharacters = new Dictionary<string, CharacterSaveData>();

    // 데스노트 목록
    public List<string> tempDefeatedIDs = new List<string>();
    public List<string> permanentDefeatedIDs = new List<string>();

    [Header("Navigation Data")]
    public string lastFieldSceneName;
    public Vector3 lastPlayerPosition;
    public bool isReturningFromBattle = false;

    public string targetSpawnID;
    public bool isLoadedFromSave = false;

    // 필드 스왑 매니저의 데이터를 데이터 매니저가 임시 대리 보관/캐싱해둡니다.
    [Header("Character Swap Cache")]
    public string currentCharacterType = "Default";
    public bool isWarriorUnlocked = false;
    public bool isRogueUnlocked = false;
    public bool isGunnerUnlocked = false;

    [Header("Teleport Waypoints")]
    public List<string> unlockedRestAreas = new List<string>();

    [Header("CardHash")]
    public HashSet<string> ownedCards = new HashSet<string>();

    [Header("RelicsHash")]
    public HashSet<string> unlockedRelics = new HashSet<string>();

    [Header("FuseCount")]
    public int fuseCount = 0;

    // 기믹 ID별로 상태(bool/int 등)를 저장
    public Dictionary<string, bool> objectStates = new Dictionary<string, bool>();
    public Dictionary<string, float> objectRotations = new Dictionary<string, float>();

    // 상태 저장
    public void SetObjectState(string id, bool state) => objectStates[id] = state;

    public void SetObjectRotation(string id, float rot)
    {
        if (objectRotations.ContainsKey(id)) objectRotations[id] = rot;
        else objectRotations.Add(id, rot);
    }

    // 상태 불러오기
    public bool GetObjectState(string id) => objectStates.ContainsKey(id) ? objectStates[id] : false;
    public float GetObjectRotation(string id) => objectRotations.TryGetValue(id, out float rot) ? rot : 0f;

    // TODO: 치트 세팅
    [Header("Cheat Settings")]
    public float cheatFlySpeed = 15f;    // 날아다닐 때의 속도
    private bool isCheatMode = false;    // 현재 치트 모드인지 확인

    // TODO: Test용 삭제 필요
    private void Start()
    {
        SceneManager.LoadScene("Stage_1");
        GameStateManager.Instance.ChangeState(GameState.Field);
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Resources.Load<EffectIconDatabase>("EffectIconDatabase");
            InitDefaultParty();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (InputManager.Instance.GetFieldReset())
        {

            // 1. 현재 켜져있는 씬의 이름을 가져옵니다.
            string currentSceneName = SceneManager.GetActiveScene().name;

            // 2. 해당 이름의 씬을 다시 로드합니다.
            SceneManager.LoadScene(currentSceneName);

            Debug.Log($"{currentSceneName} 맵을 다시 로드했습니다!");
        }

        if (InputManager.Instance.GetFieldCheat())
        {
            ToggleCheatMode();
        }

        // 3. 치트 모드가 켜져 있다면, 방향키로 날아다니게 만들기
        if (isCheatMode)
        {
            HandleCheatMovement();
        }
    }

    public void SavePlayerState(Vector3 position, string sceneName)
    {
        lastPlayerPosition = position;
        lastFieldSceneName = sceneName;
    }

    private void ToggleCheatMode()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        isCheatMode = !isCheatMode;

        Collider2D col = player.GetComponent<Collider2D>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        // 이동 스크립트뿐만 아니라 점프 관련 스크립트도 모두 가져옵니다.
        FieldPlayerMovement movement = player.GetComponent<FieldPlayerMovement>();
        FieldPlayerJump jump = player.GetComponent<FieldPlayerJump>();

        if (isCheatMode)
        {
            // [치트 ON]
            if (col != null) col.isTrigger = true;

            // 이동과 점프(중력 조작) 로직을 모두 정지!
            if (movement != null) movement.enabled = false;
            if (jump != null) jump.enabled = false; // 이 줄이 핵심입니다!

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
            }

            Debug.Log("<color=yellow> 치트 모드 활성화: 비행 및 벽 뚫기 가능!</color>");
        }
        else
        {
            // [치트 OFF] 
            if (col != null) col.isTrigger = false;

            // 이동과 점프 로직 복구!
            if (movement != null) movement.enabled = true;
            if (jump != null) jump.enabled = true; // 다시 켜줍니다.

            if (rb != null)
            {
                // 인스펙터에 설정해두셨던 기본 중력값으로 돌려주세요 (보통 1 ~ 3 사이)
                rb.gravityScale = 2f;
            }

            Debug.Log("<color=green> 치트 모드 해제: 정상 상태 복구</color>");
        }
    }

    private void HandleCheatMovement()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 물리 엔진(Rigidbody)을 무시하고 Transform 좌표를 강제로 움직여서 날아다닙니다.
        // (InputManager의 방향키 입력을 받아와도 됩니다)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = new Vector3(moveX, moveY, 0f).normalized;
        player.transform.Translate(moveDir * cheatFlySpeed * Time.deltaTime);
    }

    private void InitDefaultParty()
    {
        allCharacters.Clear();
        currentPartyMembers.Clear();

        foreach (CharacterData template in startingPartyTemplates)
        {
            if (template == null) continue;

            // SO 에셋에 들어있는 스킬들의 '이름'만 문자열로 쏙 뽑아냅니다.
            List<string> defaultSkillNames = template.defaultSkills.Select(s => s.name).ToList();

            CharacterSaveData defaultData = new CharacterSaveData(
                template.CharacterName, 1, 0, template.MaxHp, defaultSkillNames
            );
            allCharacters[template.CharacterName] = defaultData;
            currentPartyMembers.Add(template);
        }
    }

    public int GetRealMaxHp(CharacterData template, CharacterSaveData saveData)
    {
        if (template == null || saveData == null) return 1;

        int baseHp = template.MaxHp;

        // 예시: 레벨업 당 체력이 20씩 오른다고 가정 (TODO: 나중에 SO에 hpGrowth 변수를 추가하셔도 됩니다!)
        int levelBonus = (saveData.currentLevel - 1) * 20;

        int skillTreeBonus = 0;
        // 나중에 스킬트리 만들면 이렇게 추가!
        // if (saveData.unlockedSkillTreeNodes.Contains("Warrior_HP_Up_1")) skillTreeBonus += 50;

        return baseHp + levelBonus + skillTreeBonus;
    }

    public void FullHealParty()
    {
        foreach (CharacterData member in currentPartyMembers)
        {
            if (allCharacters.TryGetValue(member.CharacterName, out CharacterSaveData data))
            {
                // 계산된 '진짜 최대 체력/마나'를 구해와서, 현재 체력/마나에 꽉 채워줍니다.
                data.currentHp = GetRealMaxHp(member, data);

                Debug.Log($"[DataManager] {member.CharacterName} 체력/마나 풀 회복 완료! (HP: {data.currentHp})");
            }
        }

        // 만약 필드에 돌아다니고 있는 플레이어 캐릭터(PlayerInstance)가 
        // 체력 UI와 연동되어 있다면 여기서 업데이트 이벤트를 한번 쏴주면 좋습니다.
    }

    // [업그레이드] 게임 저장 메인 함수
    public void SaveGame()
    {
        SaveWrapper wrapper = new SaveWrapper();

        // 캐릭터 금고(Dictionary) -> JSON용 List 변환
        foreach (var kvp in allCharacters)
        {
            wrapper.characterEntries.Add(new SaveEntry(kvp.Key, kvp.Value));
        }

        // 캐릭터 능력 해금 상태 저장 (스왑 매니저 연동 변수들)
        wrapper.currentSelectedCharacter = currentCharacterType;
        wrapper.isWarriorUnlocked = isWarriorUnlocked;
        wrapper.isRogueUnlocked = isRogueUnlocked;
        wrapper.isGunnerUnlocked = isGunnerUnlocked;

        // 인벤토리 딕셔너리 -> JSON용 List 변환
        foreach (var kvp in inventory)
        {
            if (kvp.Key == null) continue;
            wrapper.inventoryEntries.Add(new ItemSaveEntry(kvp.Key.itemName, kvp.Value));
        }

        // 위치 및 내비게이션 데이터 구우며 저장
        wrapper.savedSceneName = lastFieldSceneName;
        wrapper.savedPlayerPosition = lastPlayerPosition;

        // 데스노트 백업
        wrapper.permanentlyDefeated = new List<string>(permanentDefeatedIDs);

        wrapper.ownedCards = ownedCards.ToList();
        wrapper.unlockedRelics = unlockedRelics.ToList();
        wrapper.fuseCount = fuseCount;

        // Dictionary는 반복문을 통해 껍데기 클래스 리스트로 변환
        foreach (var kvp in objectStates)
        {
            wrapper.objectStates.Add(new ObjectStateEntry(kvp.Key, kvp.Value));
        }

        foreach (var kvp in objectRotations)
        {
            wrapper.objectRotations.Add(new ObjectRotationEntry(kvp.Key, kvp.Value));
        }

        // 파일 쓰기
        string path = Application.persistentDataPath + "/savegame.json";
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(path, json);

        // 해금 휴식 장소 저장
        wrapper.unlockedRestAreas = new List<string>(unlockedRestAreas);

        Debug.Log($"[DataManager] 모든 능력, 인벤토리, 위치 데이터 저장 완료: {path}");
    }

    // [업그레이드] 게임 로드 메인 함수
    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/savegame.json";

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[DataManager] 세이브 파일이 존재하지 않아 로드를 취소합니다.");
            return;
        }

        string json = File.ReadAllText(path);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);

        // 캐릭터 금고 복구
        allCharacters.Clear();
        foreach (SaveEntry entry in wrapper.characterEntries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
            allCharacters[entry.id] = entry.data;
        }

        // 캐릭터 파티 및 배치 복구
        currentPartyMembers.Clear();
        foreach (CharacterData template in startingPartyTemplates)
        {
            if (template == null) continue;
            if (allCharacters.ContainsKey(template.CharacterName))
            {
                currentPartyMembers.Add(template);
            }
        }

        // 캐릭터 능력 및 스왑 해금 데이터 복구
        currentCharacterType = wrapper.currentSelectedCharacter;
        isWarriorUnlocked = wrapper.isWarriorUnlocked;
        isRogueUnlocked = wrapper.isRogueUnlocked;
        isGunnerUnlocked = wrapper.isGunnerUnlocked;

        // 인벤토리 아이템 복구 (문자열 이름으로 마스터 DB에서 원본 SO 에셋을 매칭해 찾아옵니다)
        inventory.Clear();
        foreach (ItemSaveEntry entry in wrapper.inventoryEntries)
        {
            ItemData originalItem = masterItemDatabase.Find(i => i.itemName == entry.itemName);
            if (originalItem != null)
            {
                inventory[originalItem] = entry.quantity;
            }
        }

        // 위치 및 내비게이션 데이터 복구
        lastFieldSceneName = wrapper.savedSceneName;
        lastPlayerPosition = wrapper.savedPlayerPosition;

        isReturningFromBattle = false;
        isLoadedFromSave = true;

        // 영구 데스노트 복구 및 임시 데스노트 청소
        permanentDefeatedIDs = new List<string>(wrapper.permanentlyDefeated);
        tempDefeatedIDs.Clear();

        // List를 다시 HashSet으로 덮기
        ownedCards = new HashSet<string>(wrapper.ownedCards);
        unlockedRelics = new HashSet<string>(wrapper.unlockedRelics);
        fuseCount = wrapper.fuseCount;

        // List를 순회하며 Dictionary 복구
        objectStates.Clear();
        foreach (var entry in wrapper.objectStates)
        {
            objectStates[entry.id] = entry.state;
        }

        objectRotations.Clear();
        foreach (var entry in wrapper.objectRotations)
        {
            objectRotations[entry.id] = entry.rotation;
        }

        // 해금된 휴식 장소 복구
        unlockedRestAreas = new List<string>(wrapper.unlockedRestAreas);

        Debug.Log("[DataManager] 해금 능력, 인벤토리, 마지막 탐험 위치까지 완벽 복구 성공!");

        // 씬 강제 로드 처리 (타이틀 화면에서 세이브파일을 읽자마자 마지막 탐험 맵으로 플레이어를 날려보냅니다)
        SceneManager.LoadScene(lastFieldSceneName);
    }

    // 외부 캐릭터가 가진 스킬 목록을 금고에 저장할 때 호출하는 도우미 보충 함수
    public void SaveCharacterStatusWithSO(string charName, int level, int exp, int hp, List<SkillData> skillDatas)
    {
        // SkillData 리스트를 string 이름 리스트로 우회 가공하여 안전하게 저장
        List<string> skillNames = skillDatas.Select(s => s.name).ToList();
        if (allCharacters.ContainsKey(charName))
        {
            allCharacters[charName] = new CharacterSaveData(charName, level, exp, hp, skillNames);
        }
    }

    public CharacterSaveData LoadCharacterStatus(string charName)
    {
        return allCharacters[charName];
    }

    // 스킬 복구용 함수 (CharacterStatus 등에서 세이브 데이터를 기반으로 스킬 SO를 찾아올 때 씁니다)
    public List<SkillData> GetSkillSOListFromNames(List<string> names)
    {
        List<SkillData> foundSkills = new List<SkillData>();
        foreach (string n in names)
        {
            SkillData skill = masterSkillDatabase.Find(s => s.name == n);
            if (skill != null) foundSkills.Add(skill);
        }
        return foundSkills;
    }

    // (기존 StartBattle, ConsumeItem, MarkDefeated, IsDefeated 로직 유지)
    public void StartBattle(List<CharacterData> enemies, EncounterType eType, DirectingType dType, string monsterID, bool respawnable)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        currentEnemyParty = enemies;
        currentEncounterType = eType;
        currentDirectingType = dType;
        currentBattleMonsterID = monsterID;
        currentBattleIsRespawnable = respawnable;
        Camera.main.orthographic = false;
        // SceneManager.LoadScene("BattleTestScene");
    }

    public bool ConsumeItem(ItemData item)
    {
        if (inventory.ContainsKey(item) && inventory[item] > 0)

        {
            inventory[item]--;
            Debug.Log($"[DataManager] {item.itemName} 사용. 남은 개수: {inventory[item]}");

            if (inventory[item] <= 0)
            {
                inventory.Remove(item);
            }
            return true;
        }
        Debug.LogWarning($"[DataManager] {item.itemName} 아이템이 부족합니다!");
        return false;
    }

    public bool IsDefeated(string id) => tempDefeatedIDs.Contains(id) || permanentDefeatedIDs.Contains(id);
    public void ResetRespawnableMonsters() => tempDefeatedIDs.Clear();
    public void MarkDefeated(string id, bool isRespawnable)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (isRespawnable) tempDefeatedIDs.Add(id);
        else permanentDefeatedIDs.Add(id);

        Debug.Log($"[데이터매니저] {id} 사망 처리 완료! (영구여부: {!isRespawnable})");
    }

    public void UnlockRestArea(string areaID)
    {
        if (!unlockedRestAreas.Contains(areaID))
        {
            unlockedRestAreas.Add(areaID);
            Debug.Log($"[텔레포트 해금] 새로운 휴식 장소 발견: {areaID}");
        }
    }

    public void AddCard(string cardId)
    {
        if (!ownedCards.Contains(cardId))
        {
            ownedCards.Add(cardId);
            Debug.Log($"카드 획득: {cardId}");
        }
    }

    public bool HasCard(string cardId)
    {
        return ownedCards.Contains(cardId);
    }

    public void AddRelic(string relicId)
    {
        unlockedRelics.Add(relicId);
        Debug.Log($"아이템/유물 획득: {relicId}");
    }

    public bool HasItem(string relicId)
    {
        // "None" 이나 빈 칸이면 기본 지급 능력으로 간주 (선택 사항)
        if (string.IsNullOrEmpty(relicId)) return true;

        return unlockedRelics.Contains(relicId);
    }

    public void AddFuse()
    {
        fuseCount++;
    }
}