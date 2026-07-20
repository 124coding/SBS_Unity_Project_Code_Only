using UnityEngine;

public class LaserMirror : InteractObject
{
    [Header("Settings")]
    public float rotationSpeed = 30f;
    public GameObject highlightEffect; // 선택되었음을 알리는 오브젝트(예: 외곽선 스프라이트)

    public LaserPuzzleManager myManager;

    public void Rotate(float direction)
    {
        // direction: 1 또는 -1
        transform.Rotate(0, 0, direction * rotationSpeed * Time.deltaTime);
    }

    public void SetSelected(bool isSelected)
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(isSelected);
    }

    protected override void OnInteraction()
    {
        // 거울이 직접 퍼즐 모드를 켜지 않고 매니저를 찾아서 실행
        if (myManager != null) myManager.StartPuzzle(this);
    }

    public void SetMirror()
    {
        DataManager.Instance.SetObjectState(uniqueID, true);
        DataManager.Instance.SetObjectRotation(uniqueID, transform.eulerAngles.z);
    }

    protected override void LoadState(bool isActivated)
    {
        if(isActivated)
        {
            float savedZ = DataManager.Instance.GetObjectRotation(uniqueID);
            transform.eulerAngles = new Vector3(0, 0, savedZ);
        }
    }
}