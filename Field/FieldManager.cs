using System.Linq;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform defaultSpawnPoint;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            player = Instantiate(playerPrefab);
        }

        // 좌표 복귀 로직
        if (DataManager.Instance.isReturningFromBattle)
        {
            // 전투 후 복귀일 때
            player.transform.position = DataManager.Instance.lastPlayerPosition;
            DataManager.Instance.isReturningFromBattle = false;
        }
        else if (DataManager.Instance.isLoadedFromSave)
        {
            // 세이브 불러오기일 때
            player.transform.position = DataManager.Instance.lastPlayerPosition;
            DataManager.Instance.isLoadedFromSave = false;
        }
        else if (!string.IsNullOrEmpty(DataManager.Instance.targetSpawnID))
        {
            // 특정 룸/리스폰 포인트 이동일 때
            RestArea spawnRest = FindObjectsByType<RestArea>(FindObjectsSortMode.None)
                                .FirstOrDefault(r => r.restAreaID == DataManager.Instance.targetSpawnID);
            if (spawnRest != null) player.transform.position = spawnRest.spawnPoint.position;
            DataManager.Instance.targetSpawnID = "";
        }
        else
        {
            player.transform.position = defaultSpawnPoint.position;
        }
    }
}