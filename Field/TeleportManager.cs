using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TeleportPlayer(string targetSceneName, Vector3 targetPosition)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // [조건 1] 이동할 목표가 '현재 씬'과 같은 경우 (예: Stage 1 내부 이동)
        if (currentSceneName == targetSceneName)
        {
            ExecuteLocalTeleport(targetPosition);
        }
        // [조건 2] 이동할 목표가 '다른 씬'인 경우 (예: Stage 2 -> Stage 1 이동)
        else
        {
            StartCoroutine(LoadSceneAndTeleportRoutine(targetSceneName, targetPosition));
        }
    }

    // 씬 내부에서 좌표만 슉 옮기는 함수 (기존 방식)
    private void ExecuteLocalTeleport(Vector3 targetPosition)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = targetPosition;

            // 시네머신 카메라 갱신 연출 등이 있다면 여기서 처리
            Debug.Log("동일 씬 내부 순간이동 완료!");
        }
    }

    // 다른 씬을 로드하고 플레이어를 배치하는 코루틴
    private IEnumerator LoadSceneAndTeleportRoutine(string targetSceneName, Vector3 targetPosition)
    {
        // 1. (선택) 화면 페이드 아웃 코드가 있다면 실행
        // yield return StartCoroutine(FadeOut());

        // 2. 유니티 씬 전환 (DontDestroyOnLoad로 선언된 플레이어와 데이터는 유지됨)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null; // 씬 로딩 완료까지 대기
        }

        // 3. 새 씬이 열렸으므로 플레이어를 찾아서 좌표 이동
        ExecuteLocalTeleport(targetPosition);

        // 4. (선택) 화면 페이드 인
        // yield return StartCoroutine(FadeIn());

        Debug.Log($"{targetSceneName} 씬으로 전환 및 순간이동 완료!");
    }
}