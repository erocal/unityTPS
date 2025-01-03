﻿using System;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    #region -- 資源參考區 --

    [Header("移動速度")]
    [SerializeField, Tooltip("移動速度")] float moveSpeed = 8;
    [SerializeField, Range(1, 3), Tooltip("CapsLock加速的倍數")] float cprintSpeedModifier = 2;
    //[SerializeField, Range(0, 1), Tooltip("蹲下時的減速倍數")] float crouchedSpeedModifer = 0.5f;
    [SerializeField, Tooltip("旋轉速度")] float rotateSpeed = 5f;
    [SerializeField, Tooltip("加速度百分比")] float addSpeedRatio = 0.1f;

    [Space(20)]
    [Header("跳躍參數")]
    [SerializeField, Tooltip("跳躍時向上施加的力量")] float jumpForce = 15;
    [SerializeField, Tooltip("在空中下施加的力量")] float gravityDownForce = 50;
    [SerializeField, Tooltip("檢查與地面之間的距離")] float distanceToGround = 0.1f;
    
    [Header("儲存腳的位置")]
    [SerializeField] Transform feet;

    [Space(20)]
    [Header("準星Icon")]
    [SerializeField] GameObject crosshair;

    [Space(20)]
    [Header("音效")]
    [SerializeField] private SoundEffects soundEffects;

    [Space(20)]
    [Header("地圖區域")]
    [Tooltip("管理地圖區域相關方法")]
    [SerializeField] MapAreaManager mapAreaManager;
    [Tooltip("玩家所在地圖區域")]
    [SerializeField] MapArea playerStandMapArea = MapArea.StartArea;

    #endregion

    #region -- 變數參考區 --

    [System.Serializable]
    public class SoundEffects
    {

        [SerializeField, Tooltip("休息的音效")]
        private AudioClip feelsleepSFX;

        [SerializeField, Tooltip("跑步喘氣的音效")]
        private AudioClip runtiredSFX;

        [SerializeField, Tooltip("跳躍時的音效")]
        private AudioClip jumptwiceSFX;

        [SerializeField, Tooltip("瞄準時的音效")]
        private AudioClip targetlockonSFX;

        [SerializeField, Tooltip("走路的音效")]
        private AudioClip stepSFX;

        [SerializeField, Tooltip("跑步的音效")]
        private AudioClip runstepSFX;

        // 提供屬性存取 (可根據需要新增)
        public AudioClip FeelSleepSFX => feelsleepSFX;
        public AudioClip RunTiredSFX => runtiredSFX;
        public AudioClip JumpTwiceSFX => jumptwiceSFX;
        public AudioClip TargetLockonSFX => targetlockonSFX;
        public AudioClip StepSFX => stepSFX;
        public AudioClip RunStepSFX => runstepSFX;

    }

    #region -- 常數 --

    private const int DASH_SPEED_MODIFIER = 3;

    #endregion

    InputController input;
    ActionSystem actionSystem;
    Organism organism;
    CharacterController controller;
    [HideInInspector] public Animator animator;
    Health health;
    WeaponManager weaponManager;
    AudioSource audioSource;

    int jumpCount = 1;

    #region -- 計時器 --

    private float resttimerrate = 2.0f;
    private float runtimerrate = 2.0f;
    private float steptimerrate = 2.0f;
    private float runsteptimerrate = 2.0f;

    #endregion

    [Tooltip("出生點")]
    public Vector3 spawn;
    [Tooltip("下一幀要移動到的目標位置")]
    Vector3 targetMovement;
    [Tooltip("下一幀跳躍到的方向")]
    Vector3 jumpDirection;
    [Tooltip("上一幀的移動速度")]
    float lastFrameSpeed = 0.0f;
    [Tooltip("是否在瞄準狀態")]
    bool isAim;

    bool canDash = true;

    #endregion

    #region -- 初始化/運作 --

    void Awake()
    {

        input = GameManagerSingleton.Instance.InputController;
        actionSystem = GameManagerSingleton.Instance.ActionSystem;
        organism = Organism.Instance;
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        weaponManager = GetComponent<WeaponManager>();
        audioSource = GetComponent<AudioSource>();

        spawn = new Vector3(-473.3f, 21.93f, 245.9f);

#if UNITY_EDITOR
        if (mapAreaManager == null) Log.Error("地圖管理員未掛載!");
#endif

        // 訂閱死亡事件
        actionSystem.OnDie += OnDie;
    }

    private void Update()
    {

        UpdateTimer();

        AimBehaviour();
        
        MoveBehaviour();
        
        JumpBehaviour();
        
        RestBehaviour();

    }

    #endregion

    #region -- 方法參考區 --

    #region -- 事件相關 --

    /// <summary>
    /// 玩家死亡時處理方法
    /// </summary>
    private void OnDie(int id)
    {

        if (id != organism.GetPlayer().GetInstanceID()) return;

        animator.SetTrigger("IsDead");
        //取消玩家的控制
        this.GetComponent<PlayerController>().enabled = false;

    }

    #endregion

    #region -- 計時器 --

    /// <summary>
    /// 計時器
    /// </summary>
    private void UpdateTimer()
    {
        resttimerrate -= Time.deltaTime;
        runtimerrate -= Time.deltaTime;
        steptimerrate -= Time.deltaTime;
        runsteptimerrate -= Time.deltaTime;
    }

    #endregion

    /// <summary>
    /// 處理瞄準行為
    /// </summary>
    private void AimBehaviour()
    {
        bool lastTimeAim = isAim;
        if (input.GetFireInputDown() && weaponManager.GetActiveWeapon() != null)
        {
            isAim = true;
        }
        if (input.GetAimInputDown() && weaponManager.GetActiveWeapon() != null)
        {
            isAim = !isAim;
        }

        if (lastTimeAim != isAim)
        {
            if (crosshair != null)
            {
                crosshair.SetActive(isAim);
                if (soundEffects.TargetLockonSFX != null && crosshair.activeInHierarchy != false)
                {
                    audioSource.PlayOneShot(soundEffects.TargetLockonSFX);
                }
            }
            actionSystem.Aim(isAim);
        }

        animator.SetBool("IsAim", isAim);
    }

    /// <summary>
    /// 處理移動行為
    /// </summary>
    private void MoveBehaviour()
    {
        targetMovement = Vector3.zero;
        Vector3 pretargetMovement = targetMovement;
        targetMovement += input.GetMoveInput().z * GetCurrentCameraForward();
        targetMovement += input.GetMoveInput().x * GetCurrentCameraRight();

        if (targetMovement != pretargetMovement)
        {
            resttimerrate = 2.0f;
        }

        // 避免對角線超過1
        targetMovement = Vector3.ClampMagnitude(targetMovement, 1);

        // 下一幀的移動速度
        float nextFrameSpeed = 0;

        // 是否按下加速
        if (targetMovement == Vector3.zero)
        {
            nextFrameSpeed = 0f;
        }
        else if (input.GetCapInput() && !isAim)// 如果加速鍵被按下且不在瞄準時
        {

            
            nextFrameSpeed = 1f;
            targetMovement *= cprintSpeedModifier;

            if(canDash)
            {
                targetMovement *= DASH_SPEED_MODIFIER;
                animator.SetTrigger("IsDash");
                canDash = false;
            }

            SmoothRotation(targetMovement);
            actionSystem.PlayerSpeedUp();

            // 如果按下跳躍鍵，且人物處在地面上時
            if (input.GetJumpInputDown() && IsGrounded())
            {
                animator.SetTrigger("IsJump");
                jumpDirection = Vector3.zero;
                jumpDirection += jumpForce * Vector3.up;
                jumpCount = 0;
            }

        }
        else if (!isAim)// 如果不處於瞄準
        {
            nextFrameSpeed = 0.5f;
            SmoothRotation(targetMovement);
        }

        if(!input.GetCapInput())
        {
            canDash = true;
            actionSystem.ReleaseCaplock();
        }

        // 處於瞄準時
        if (isAim)
        {
            SmoothRotation(GetCurrentCameraForward());
        }

        // 當前後Frame速度不一致，線性更改速度
        if (lastFrameSpeed != nextFrameSpeed)
        {
            lastFrameSpeed = Mathf.Lerp(lastFrameSpeed, nextFrameSpeed, addSpeedRatio);
        }

        // 當在走路時，撥放走路音效
        if (lastFrameSpeed > 0.2f && lastFrameSpeed <= 0.5f)
        {
            if (soundEffects.StepSFX != null && steptimerrate <= 1.2f)
            {
                audioSource.PlayOneShot(soundEffects.StepSFX);
                steptimerrate = 2.0f;
            }
        }

        // 當在奔跑時，撥放走路音效
        if (lastFrameSpeed > 0.7f)
        {
            if (soundEffects.RunStepSFX != null && runsteptimerrate <= 1.7f)
            {
                audioSource.PlayOneShot(soundEffects.RunStepSFX);
                runsteptimerrate = 2.0f;
            }
        }

        // 如果長時間奔跑，播放疲累音效
        if (lastFrameSpeed > 0.9f)
        {
            if (soundEffects.RunTiredSFX != null && runtimerrate <= -0.426f)
            {
                audioSource.PlayOneShot(soundEffects.RunTiredSFX);
                runtimerrate = 2.0f;
            }
        }

        // 更動unity裡面人物動畫的相關數值
        animator.SetFloat("WalkSpeed", lastFrameSpeed);
        animator.SetFloat("Vertical", input.GetMoveInput().z);
        animator.SetFloat("Horizontal", input.GetMoveInput().x);

        // 動態變化移動速度
        controller.Move(moveSpeed * Time.deltaTime * targetMovement);
    }
    
    /// <summary>
    /// 取得目前相機的正面方向
    /// </summary>
    private Vector3 GetCurrentCameraForward()
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f;
        //歸一化
        cameraForward.Normalize();
        return cameraForward;
    }
    
    /// <summary>
    /// 取得目前相機的右側方向
    /// </summary>
    private Vector3 GetCurrentCameraRight()
    {
        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0f;
        //歸一化
        cameraRight.Normalize();
        return cameraRight;
    }

    /// <summary>
    /// 平滑旋轉角度到目標方向
    /// </summary>
    /// <param name="targetMovement">目標方向</param>
    private void SmoothRotation(Vector3 targetMovement)
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetMovement, Vector3.up), rotateSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 檢測玩家是否在地上
    /// </summary>
    /// <returns>回傳玩家是否在地上</returns>
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -Vector3.up, distanceToGround);
    }
    
    /// <summary>
    /// 處理跳躍行為
    /// </summary>
    private void JumpBehaviour()
    {
        // 如果人物處於地面
        if (IsGrounded())
        {
            jumpCount = 1;
        }

        // 當人物處於地面，按下跳躍鍵且沒有瞄準時
        if (input.GetJumpInputDown() && jumpCount > 0 && !isAim && IsGrounded())
        {
            animator.SetTrigger("IsJump");
            jumpDirection = Vector3.zero;
            jumpDirection += jumpForce * Vector3.up;
            jumpCount = 0;
        }
        else if (input.GetJumpInputDown() && jumpCount == 0)
        {
            jumpDirection = Vector3.zero;
            jumpDirection += jumpForce * Vector3.up;
            jumpCount--;

            // 播放跳躍音效
            if (soundEffects.JumpTwiceSFX != null)
            {
                audioSource.PlayOneShot(soundEffects.JumpTwiceSFX);
            }
        }

        jumpDirection.y -= gravityDownForce * Time.deltaTime;
        jumpDirection.y = Mathf.Max(jumpDirection.y, -gravityDownForce);

        controller.Move(jumpDirection * Time.deltaTime);
    }

    /// <summary>
    /// 處理休息行為
    /// </summary>
    private void RestBehaviour()
    {
        if(resttimerrate <= -30.0f)
        {
            animator.SetTrigger("IsRest");
            resttimerrate = 2.0f;
            if (soundEffects.FeelSleepSFX != null)
            {
                audioSource.PlayOneShot(soundEffects.FeelSleepSFX);
            }
        }
    }

    /// <summary>
    /// 玩家復活
    /// </summary>
    public async Task IsAlive()
    {
        // 根據玩家區域，開啟地圖
        await mapAreaManager.SwitchMapArea((int)playerStandMapArea);

        health.Alive();
        animator.SetTrigger("IsAlive");
        
        // 初始玩家生成位置
        ChangePosition(spawn);
        //還給玩家控制權
        this.GetComponent<PlayerController>().enabled = true;
    }

    /// <summary>
    /// 將玩家改變位置
    /// </summary>
    private void ChangePosition(Vector3 teleportPosition)
    {
        this.transform.position = teleportPosition;
    }

    #region -- Get方法 --

    /// <summary>
    /// 取得準星Icon
    /// </summary>
    public GameObject GetCrosshair()
    {
        return crosshair;
    }

    #endregion

    #region -- Set方法 --

    /// <summary>
    /// 取得玩家所在地圖區域
    /// </summary>
    public void SetPlayerStandMapArea(MapArea playerStandMapArea)
    {
        this.playerStandMapArea = playerStandMapArea;
    }

    #endregion

    #endregion

}
