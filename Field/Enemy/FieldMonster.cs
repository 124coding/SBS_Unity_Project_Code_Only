using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FieldMonster : MonoBehaviour
{
    [Header("Unique Settings")]
    public string monsterID;

    [Header("Respawn Settings")]
    public bool isRespawnable = true;

    [Header("EncounterDirecting")]
    public DirectingType directingType = DirectingType.NormalRunIn;

    [Header("EnemyParty")]
    // 필드에서 몬스터와 부딪힐 때마다 덮어씌워질 적 파티 명단
    public List<CharacterData> enemyParty = new List<CharacterData>();

    private void OnEnable()
    {
        if (DataManager.Instance != null)
        {
            if ((DataManager.Instance.tempDefeatedIDs.Contains(monsterID) && isRespawnable) ||
                DataManager.Instance.permanentDefeatedIDs.Contains(monsterID))
            {
                Destroy(gameObject);
                Debug.Log("해당 적 삭제");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);
            // 플레이어의 현재 위치(좌표)를 넘겨줍니다.
            TriggerBattle(EncounterType.Normal, other.transform.position);
        }
    }

    // 플레이어의 무기나 투사체에 맞았을 때 호출될 함수 (매개변수로 플레이어 위치 전달 필요)
    public void OnHitByPlayerWeapon(Vector3 playerPosition)
    {
        TriggerBattle(EncounterType.PlayerAdvantage, playerPosition);
    }

    // 플레이어가 패링에 성공했을 때 호출될 함수
    public void OnParriedByPlayer(Vector3 playerPosition)
    {
        TriggerBattle(EncounterType.Parried, playerPosition);
    }

    private void TriggerBattle(EncounterType type, Vector3 playerPosition)
    {
        Debug.Log($"[필드 몬스터] 플레이어와 조우! (타입: {type})");

        EncounterManager.Instance.TriggerEncounter(
            enemyParty,
            type,
            directingType,
            monsterID,
            isRespawnable
        );
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateUniqueID()
    {
        monsterID = System.Guid.NewGuid().ToString();
    }
}