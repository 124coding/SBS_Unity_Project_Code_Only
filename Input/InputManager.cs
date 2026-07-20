using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    // 싱글톤
    public static InputManager Instance { get; private set; }

    public PlayerControls inputActions;

    // 글로벌 키 전용 이벤트 방송
    public Action OnToggleMenuPressed;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            inputActions = new PlayerControls();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // 글로벌 입력은 언제나 작동
        inputActions.Global.ToggleMenu.performed += OnToggleMenu;

        // 기본 필드 탐험 모드
        if (GameStateManager.Instance != null)
        {
            GameEvents.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        inputActions.Disable();

        inputActions.Global.ToggleMenu.performed -= OnToggleMenu;

        if (GameStateManager.Instance != null)
        {
            GameEvents.OnStateChanged -= HandleStateChanged;
        }
    }

    private void OnToggleMenu(InputAction.CallbackContext context)
    {
        GameStateManager.Instance.TogglePause();
    }

    public void SwitchActionMap(string mapName)
    {
        inputActions.asset.Disable();
        inputActions.Global.Enable();

        // targetMap을 이름으로 찾아서 켬
        InputActionMap targetMap = inputActions.asset.FindActionMap(mapName);

        if (targetMap != null)
        {
            targetMap.Enable();
            Debug.Log($"[InputManager] 입력 모드 변경: {mapName}");
        }
        else
        {
            Debug.LogError($"[InputManager] {mapName} 맵을 찾을 수 없습니다!");
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Field:
                SwitchActionMap("Field");
                break;
            case GameState.Battle:
                SwitchActionMap("Battle");
                break;
            case GameState.Cutscene:
                SwitchActionMap("Cutscene"); // 컷신 중에는 아무것도 못하게 빈 맵이나 UI 맵으로
                break;
            case GameState.Paused:
                SwitchActionMap("Global"); // 일시정지 중
                break;
                // 퍼즐, 에이밍은 특정 기믹 오브젝트와 상호작용 시 직접 SwitchActionMap 호출
        }
    }

    public void StopPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    // 현재 필드 이동 방향 벡터값 리턴 (WASD 조합 수치)
    //public Vector2 GetFieldMoveInput() => inputActions.Field.Move.ReadValue<Vector2>();

    //public bool GetFieldJumpDown() => inputActions.Field.Jump.WasPressedThisFrame();
    //public bool GetFieldJumpHeld() => inputActions.Field.Jump.IsPressed();
    //public bool GetFieldDashDown() => inputActions.Field.Dash.WasPressedThisFrame();
    //public bool GetFieldAttackDown() => inputActions.Field.Attack.WasPressedThisFrame();
    //public bool GetFieldInteractDown() => inputActions.Field.Interact.WasPressedThisFrame();
    //public bool GetFieldSkillDown() => inputActions.Field.UseSkill.WasPressedThisFrame();

    //// Interact 누르고 있기
    //public bool GetFieldInteractHeld() => inputActions.Field.Interact.IsPressed();

    //// Interact 떼기
    //public bool GetFieldInteractUp() => inputActions.Field.Interact.WasReleasedThisFrame();

    //// 캐릭터 스왑
    //public bool GetSwap1Down() => inputActions.Field.Swap1.WasPressedThisFrame();
    //public bool GetSwap2Down() => inputActions.Field.Swap2.WasPressedThisFrame();
    //public bool GetSwap3Down() => inputActions.Field.Swap3.WasPressedThisFrame();
    //public bool GetSwap4Down() => inputActions.Field.Swap4.WasPressedThisFrame();

    // TODO: 삭제 필요
    public bool GetFieldReset() => inputActions.Field.TestReset.WasPressedThisFrame();
    public bool GetFieldCheat() => inputActions.Field.TestCheat.WasPressedThisFrame();

    // 퍼즐용 입력 창구
    //public bool GetPuzzleRotateClockwise() => inputActions.Puzzle.RotateClockwise.IsPressed();
    //public bool GetPuzzleRotateCounterClockwise() => inputActions.Puzzle.RotateCounterClockwise.IsPressed();
    //public bool GetPuzzleInteractDown() => inputActions.Puzzle.Interact.WasPressedThisFrame();

    //public bool GetPuzzlePreviousObject() => inputActions.Puzzle.PreviousObject.WasPressedThisFrame();

    //public bool GetPuzzleNextObject() => inputActions.Puzzle.NextObject.WasPressedThisFrame();

    //// Aiming
    //public bool GetAimShootDown() => inputActions.Aiming.AimShoot.WasPressedThisFrame();
    //public bool GetCancelAimDown() => inputActions.Aiming.Cancel.WasPressedThisFrame();

}
