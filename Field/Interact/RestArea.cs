using UnityEngine;
using UnityEngine.SceneManagement;

public class RestArea : InteractObject
{
    [Header("Rest Area Settings")]
    public string restAreaID = ""; // 이 장소의 고유 ID

    public Transform spawnPoint;

    protected override void OnInteraction()
    {

        DataManager.Instance.UnlockRestArea(restAreaID);

        // 핵심: 이 장소를 기점으로 자동 세이브!
        DataManager.Instance.lastFieldSceneName = SceneManager.GetActiveScene().name;

        DataManager.Instance.lastPlayerPosition = spawnPoint.position;

        DataManager.Instance.SaveGame();
        Debug.Log("게임 자동 저장 완료!");

        // 휴식 UI 메뉴 띄우기 (스킬 트리, 텔레포트 등)
        OpenRestAreaMenu();
    }

    protected override void LoadState(bool isActivated)
    {
        // TODO: 후광 및 텔레포트 가능 지점에 추가
        if (isActivated) { }
    }

    private void HealParty()
    {
        // 파티원 전원 체력/마나 풀 회복 로직
        DataManager.Instance.FullHealParty();

        DataManager.Instance.ResetRespawnableMonsters();
    }

    private void OpenRestAreaMenu()
    {
        // TODO: UI 매니저를 통해 [스킬 트리 / 빠른 이동 / 취소] 버튼이 있는 패널을 엽니다.
        // 예: UIManager.Instance.ShowRestAreaPanel();

        // UI가 열려있는 동안 플레이어가 못 움직이게 이동 스크립트를 잠시 꺼두면 좋습니다.
    }

    public void OnClickFastTravelButton(RestArea targetRestArea)
    {
        string targetScene = targetRestArea.gameObject.scene.name; // 오브젝트가 속한 씬 이름 자동 추출
        Vector3 targetPos = targetRestArea.spawnPoint.position;

        TeleportManager.Instance.TeleportPlayer(targetScene, targetPos);
    }
}
