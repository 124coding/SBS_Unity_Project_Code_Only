using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance { get; private set; }

    [Header("Encounter Settings")]
    public Image fadeOverlay;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeOverlay == null)
            {
                fadeOverlay = GetComponentInChildren<Image>(true);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TriggerEncounter(List<CharacterData> enemyList, EncounterType encounterType, DirectingType directingType, string monsterID, bool isRespawnable)
    {
        Debug.Log("전투 발생! 전역 매니저에서 화면 전환 시작...");

        DataManager.Instance.lastFieldSceneName = SceneManager.GetActiveScene().name;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) DataManager.Instance.lastPlayerPosition = player.transform.position;

        DataManager.Instance.isReturningFromBattle = true;
        GameStateManager.Instance.ChangeState(GameState.Battle);

        fadeOverlay.gameObject.SetActive(true);
        fadeOverlay.color = new Color(0, 0, 0, 0);

        fadeOverlay.DOFade(1f, fadeDuration).OnComplete(() =>
        {
            StartCoroutine(LoadBattleSceneAsync(enemyList, encounterType, directingType, monsterID, isRespawnable));
        });
    }

    private IEnumerator LoadBattleSceneAsync(List<CharacterData> enemyList, EncounterType encounterType, DirectingType directingType, string monsterID, bool isRespawnable)
    {
        // DataManager에 전투 데이터를 세팅합니다. 
        DataManager.Instance.StartBattle(enemyList, encounterType, directingType, monsterID, isRespawnable);

        // 2. 비동기 씬 로드 시작 (BattleTestScene 이름은 프로젝트에 맞게 확인해주세요)
        AsyncOperation op = SceneManager.LoadSceneAsync("BattleTestScene");
        op.allowSceneActivation = false; // 씬이 다 로드되어도 강제로 시작되지 않게 멱살 잡기!

        // 3. 씬이 90% 이상 로드될 때까지 대기
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 4. 로드가 끝났으니 씬을 활성화(깨우기) 시킵니다.
        op.allowSceneActivation = true;

        // 5. 씬이 완전히 켜질 때까지 잠깐 대기 (이때 BattleManager의 Awake/Start가 실행됨)
        yield return new WaitUntil(() => op.isDone);

        // 6. 씬 준비가 끝났으니 검은 화면을 다시 투명하게 걷어냅니다.
        fadeOverlay.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            fadeOverlay.gameObject.SetActive(false);

            // 화면이 완전히 밝아지면, 전투 씬 매니저에게 "이제 연출 시작해!" 라고 방송!
            BattleEvents.OnBattleReadyToStart?.Invoke();
        });
    }
}