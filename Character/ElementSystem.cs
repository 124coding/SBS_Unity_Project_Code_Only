using System.Collections.Generic;
using UnityEngine;

public class ElementSystem : MonoBehaviour
{
    private CharacterStatus characterStatus;

    [Header("Break System")]
    public bool isFullyBroken = false;
    public int elementCount = 1;

    public List<ElementData> elementDatas = new List<ElementData>();
    public List<ElementData> unbrokenElements = new List<ElementData>();

    private void Awake()
    {
        characterStatus = GetComponent<CharacterStatus>();
    }

    public void AssignRandomElements()
    {
        elementDatas.Clear();
        unbrokenElements.Clear();
        isFullyBroken = false;

        if (DataManager.Instance.masterElementDatabase == null || DataManager.Instance.masterElementDatabase.Count == 0)
        {
            Debug.LogWarning("속성 풀(allAvailableElements)이 비어있습니다!");
            return;
        }

        List<ElementData> tempPool = new List<ElementData>(characterStatus.characterData.allAvailableElement);

        for (int i = 0; i < elementCount; i++)
        {
            if (tempPool.Count == 0) break; // 더 이상 뽑을 속성이 없으면 중단

            // 랜덤하게 인덱스 하나를 뽑음
            int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);

            // unbrokenElements에 추가하고 임시 풀에서는 제거 (중복 방지)
            elementDatas.Add(tempPool[randomIndex]);
            unbrokenElements.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }

        Debug.Log($"[{gameObject.name}] 랜덤 약점 {unbrokenElements.Count}개 부여 완료!");
    }

    // 맞았을 때 껍질이 까지는지 검사 및 처리
    public void ProcessBreak(ElementData hitElement, WeaknessSetting rules)
    {
        if (isFullyBroken || hitElement == null || rules == null) return;

        bool isPeelingOccurred = false;

        for (int i = unbrokenElements.Count - 1; i >= 0; --i)
        {
            if (unbrokenElements[i].GetMultiplier(hitElement, rules) > 1.0f)
            {
                Debug.Log($"[{unbrokenElements[i].elementName}] 껍질 파괴! (남은 껍질: {unbrokenElements.Count - 1}개)");
                unbrokenElements.RemoveAt(i);
                isPeelingOccurred = true;
            }
        }

        if (isPeelingOccurred && unbrokenElements.Count == 0)
        {
            isFullyBroken = true;
            Debug.Log($"FULL BREAK!! [{gameObject.name}]의 모든 약점이 간파되었습니다! 1MORE 획득!");
            BattleEvents.OnBreakOccurred?.Invoke(characterStatus);
        }
    }

    public bool CheckWillBreak(ElementData hitElement, WeaknessSetting rules)
    {
        if (isFullyBroken || hitElement == null || rules == null) return false;

        bool willPeel = false;
        int remainingShields = unbrokenElements.Count;

        foreach (var e in unbrokenElements)
        {
            if (e.GetMultiplier(hitElement, rules) > 1.0f)
            {
                willPeel = true;
                remainingShields--;
            }
        }

        return willPeel && (remainingShields == 0);
    }
}
