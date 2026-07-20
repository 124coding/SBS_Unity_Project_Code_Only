using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    [Header("카메라 세팅")]
    public CinemachineConfiner2D mainCameraConfiner;
    public CinemachineCamera mainVirtualCamera;

    private Room currentRoom;
    private Room[] allRooms;
    private Coroutine deactivationCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        allRooms = FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 시작할 때 모든 방의 부모(contents)를 깔끔하게 꺼둡니다.
        foreach (var room in allRooms)
        {
            if (room.contents != null) room.contents.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        GameObject player = null;

        // 플레이어 찾기 대기
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;
        }

        // 카메라 세팅
        if (mainVirtualCamera != null)
        {
            Transform cameraTarget = player.transform.Find("CameraTarget");
            Transform targetToFollow = (cameraTarget != null) ? cameraTarget : player.transform;

            mainVirtualCamera.Target.TrackingTarget = targetToFollow;
            mainVirtualCamera.OnTargetObjectWarped(targetToFollow, targetToFollow.position - mainVirtualCamera.transform.position);

            Debug.Log("<color=cyan>[RoomManager] 카메라 타겟 할당 완료: " + targetToFollow.name + "</color>");
        }

        // [핵심 최적화] 무거운 연산인 GetComponent를 시작할 때 딱 한 번만 해서 넘겨줍니다.
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        StartCoroutine(RoomCheckRoutine(player, playerRb));
    }

    private IEnumerator RoomCheckRoutine(GameObject player, Rigidbody2D rb)
    {
        WaitForSeconds wait = new WaitForSeconds(0.1f);
        while (true)
        {
            UpdateCurrentRoom(player, rb);
            yield return wait;
        }
    }

    // 매 프레임 Find를 하지 않아 CPU 부하(렉)가 대폭 감소합니다.
    private void UpdateCurrentRoom(GameObject player, Rigidbody2D rb)
    {
        if (player == null) return;

        Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
        Vector2 targetPos = (Vector2)player.transform.position + (velocity * 0.2f);

        Room bestRoom = null;
        float minDistance = float.MaxValue;

        foreach (var room in allRooms)
        {
            if (room.IsPlayerInRoom(targetPos))
            {
                float dist = Vector2.Distance(targetPos, room.GetCenter());
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestRoom = room;
                }
            }
        }

        if (bestRoom != null && bestRoom != currentRoom)
        {
            SwitchRoom(bestRoom);
        }
    }

    private void SwitchRoom(Room newRoom)
    {
        // 1. 새 방 활성화 (물리 버그 방지를 위해 무조건 한 번에 즉시 켭니다)
        if (newRoom.contents != null)
        {
            newRoom.contents.SetActive(true);
        }

        // 2. 카메라 컨파이너 즉시 교체
        if (newRoom.cameraConfiner != null)
        {
            mainCameraConfiner.BoundingShape2D = newRoom.cameraConfiner;
        }

        // 3. 이전 방 지연 비활성화 코루틴 관리
        if (deactivationCoroutine != null) StopCoroutine(deactivationCoroutine);
        deactivationCoroutine = StartCoroutine(DelayedDeactivateRooms(newRoom));

        currentRoom = newRoom;
    }

    private IEnumerator DelayedDeactivateRooms(Room activeRoom)
    {
        yield return new WaitForSeconds(0.5f);

        foreach (var room in allRooms)
        {
            if (room != activeRoom && room.contents != null)
            {
                room.contents.SetActive(false);
            }
        }
    }
}