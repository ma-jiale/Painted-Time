// using UnityEngine;
// using UnityEngine.Events;

// /// <summary>
// /// 时间稳定性检查器 - 监控TimeAnchor并检查其TimeValue是否在稳定范围内保持足够时间
// /// 现在支持与TemporalKnotManager集成，用于多时间结的笼子谜题
// /// 
// /// 用途：创建"时间稳定化挑战"游戏玩法
// /// 附加到需要稳定时间才能解锁的笼子对象上
// /// </summary>
// [RequireComponent(typeof(TimeAnchor))]
// public class TimeStabilityChecker : MonoBehaviour
// {
//     [Header("模式选择")]
//     [SerializeField, Tooltip("使用时间结系统（多结谜题）还是简单稳定系统")]
//     private bool useKnotSystem = true;
    
//     [Header("简单稳定模式 - 稳定要求")]
//     [SerializeField, Tooltip("稳定范围最小值")]
//     private float stabilityRangeMin = 0.4f;
    
//     [SerializeField, Tooltip("稳定范围最大值")]
//     private float stabilityRangeMax = 0.6f;
    
//     [SerializeField, Tooltip("需要保持稳定的时间（秒）")]
//     private float requiredStabilityDuration = 3f;
    
//     [Header("稳定进度")]
//     [SerializeField, Tooltip("允许部分进度还是要求连续稳定？")]
//     private bool allowPartialProgress = true;
    
//     [SerializeField, Tooltip("离开范围时进度衰减速度（0 = 不衰减）")]
//     [Range(0f, 2f)]
//     private float progressDecaySpeed = 0.5f;
    
//     [Header("视觉反馈")]
//     [SerializeField, Tooltip("解锁时启用的对象（如宝物）")]
//     private GameObject unlockedObject;
    
//     [SerializeField, Tooltip("解锁时播放的粒子效果")]
//     private ParticleSystem unlockEffect;
    
//     [SerializeField, Tooltip("解锁时播放的音频")]
//     private AudioSource unlockAudio;
    
//     [Header("事件")]
//     [SerializeField, Tooltip("达到稳定时触发的事件")]
//     private UnityEvent onStabilityAchieved;
    
//     [SerializeField, Tooltip("失去稳定时触发的事件")]
//     private UnityEvent onStabilityLost;
    
//     [SerializeField, Tooltip("成功解锁时触发的事件")]
//     private UnityEvent onUnlocked;
    
//     [Header("时间结系统引用")]
//     [SerializeField, Tooltip("时间结管理器（如果使用结系统）")]
//     private TemporalKnotManager knotManager;
    
//     // 内部状态
//     private TimeAnchor timeAnchor;
//     private float currentStabilityTime = 0f;
//     private bool isStable = false;
//     private bool isUnlocked = false;
    
//     /// <summary>
//     /// 获取当前稳定进度（0到1）
//     /// </summary>
//     public float StabilityProgress
//     {
//         get
//         {
//             if (useKnotSystem && knotManager != null && knotManager.CurrentKnot != null)
//             {
//                 return knotManager.CurrentKnot.LoosenProgress;
//             }
//             return Mathf.Clamp01(currentStabilityTime / requiredStabilityDuration);
//         }
//     }
    
//     /// <summary>
//     /// 获取当前是否在稳定范围内
//     /// </summary>
//     public bool IsInStableRange
//     {
//         get
//         {
//             if (useKnotSystem && knotManager != null && knotManager.CurrentKnot != null && timeAnchor != null)
//             {
//                 return knotManager.CurrentKnot.IsInTargetRange(timeAnchor.TimeValue);
//             }
//             return isStable;
//         }
//     }
    
//     /// <summary>
//     /// 获取挑战是否已完成
//     /// </summary>
//     public bool IsUnlocked
//     {
//         get
//         {
//             if (useKnotSystem && knotManager != null)
//             {
//                 return knotManager.AllKnotsUntied;
//             }
//             return isUnlocked;
//         }
//     }
    
//     /// <summary>
//     /// 获取时间结管理器
//     /// </summary>
//     public TemporalKnotManager KnotManager => knotManager;
    
//     /// <summary>
//     /// 获取TimeAnchor
//     /// </summary>
//     public TimeAnchor TimeAnchorRef => timeAnchor;
    
//     private void Awake()
//     {
//         timeAnchor = GetComponent<TimeAnchor>();
        
//         // 尝试获取或创建时间结管理器
//         if (useKnotSystem)
//         {
//             if (knotManager == null)
//             {
//                 knotManager = GetComponent<TemporalKnotManager>();
//             }
            
//             // 如果仍然没有，自动添加
//             if (knotManager == null)
//             {
//                 knotManager = gameObject.AddComponent<TemporalKnotManager>();
//                 Debug.Log($"[{gameObject.name}] 自动添加了 TemporalKnotManager 组件");
//             }
//         }
        
//         // 初始隐藏解锁对象
//         if (unlockedObject != null)
//         {
//             unlockedObject.SetActive(false);
//         }
//     }
    
//     private void OnEnable()
//     {
//         if (timeAnchor != null)
//         {
//             timeAnchor.OnTimeValueChanged += OnTimeValueChanged;
//         }
        
//         // 订阅时间结事件
//         if (useKnotSystem && knotManager != null)
//         {
//             knotManager.onAllKnotsUntied.AddListener(OnAllKnotsUnlocked);
//         }
//     }
    
//     private void OnDisable()
//     {
//         if (timeAnchor != null)
//         {
//             timeAnchor.OnTimeValueChanged -= OnTimeValueChanged;
//         }
        
//         // 取消订阅时间结事件
//         if (useKnotSystem && knotManager != null)
//         {
//             knotManager.onAllKnotsUntied.RemoveListener(OnAllKnotsUnlocked);
//         }
//     }
    
//     private void Update()
//     {
//         // 如果使用时间结系统，进度由KnotManager管理
//         if (useKnotSystem) return;
        
//         if (isUnlocked) return;
        
//         CheckStability();
//         UpdateProgress();
//     }
    
//     /// <summary>
//     /// 当所有时间结都解开时触发
//     /// </summary>
//     private void OnAllKnotsUnlocked()
//     {
//         UnlockObject();
//     }
    
//     /// <summary>
//     /// 检查当前时间值是否在稳定范围内（简单模式）
//     /// </summary>
//     private void CheckStability()
//     {
//         if (timeAnchor == null) return;
        
//         float timeValue = timeAnchor.TimeValue;
//         bool nowStable = timeValue >= stabilityRangeMin && timeValue <= stabilityRangeMax;
        
//         // 状态变化：进入稳定
//         if (nowStable && !isStable)
//         {
//             isStable = true;
//             onStabilityAchieved?.Invoke();
//             Debug.Log($"[{gameObject.name}] 进入稳定范围！");
//         }
//         // 状态变化：离开稳定
//         else if (!nowStable && isStable)
//         {
//             isStable = false;
//             onStabilityLost?.Invoke();
//             Debug.Log($"[{gameObject.name}] 离开稳定范围！");
//         }
//     }
    
//     /// <summary>
//     /// 更新稳定进度计时器（简单模式）
//     /// </summary>
//     private void UpdateProgress()
//     {
//         if (isStable)
//         {
//             // 稳定时增加进度
//             currentStabilityTime += Time.deltaTime;
            
//             // 检查是否达到要求
//             if (currentStabilityTime >= requiredStabilityDuration)
//             {
//                 UnlockObject();
//             }
//         }
//         else
//         {
//             // 不稳定时处理进度
//             if (!allowPartialProgress)
//             {
//                 // Reset progress completely
//                 currentStabilityTime = 0f;
//             }
//             else if (progressDecaySpeed > 0f)
//             {
//                 // Decay progress gradually
//                 currentStabilityTime -= progressDecaySpeed * Time.deltaTime;
//                 currentStabilityTime = Mathf.Max(0f, currentStabilityTime);
//             }
//         }
//     }
    
//     /// <summary>
//     /// 时间值变化时调用
//     /// </summary>
//     private void OnTimeValueChanged(float newTimeValue)
//     {
//         // 稳定性检查在Update中进行，这里可以添加即时反馈
//     }
    
//     /// <summary>
//     /// 解锁对象（完成挑战）
//     /// </summary>
//     private void UnlockObject()
//     {
//         if (isUnlocked) return;
        
//         isUnlocked = true;
//         Debug.Log($"[{gameObject.name}] 已解锁！");
        
//         // 显示解锁内容
//         if (unlockedObject != null)
//         {
//             unlockedObject.SetActive(true);
//         }
        
//         // 播放效果
//         if (unlockEffect != null)
//         {
//             unlockEffect.Play();
//         }
        
//         if (unlockAudio != null)
//         {
//             unlockAudio.Play();
//         }
        
//         // 触发事件
//         onUnlocked?.Invoke();
//     }
    
//     /// <summary>
//     /// 重置挑战（用于测试或重玩）
//     /// </summary>
//     public void ResetChallenge()
//     {
//         isUnlocked = false;
//         currentStabilityTime = 0f;
//         isStable = false;
        
//         if (unlockedObject != null)
//         {
//             unlockedObject.SetActive(false);
//         }
        
//         // 重置时间结系统
//         if (useKnotSystem && knotManager != null)
//         {
//             knotManager.ResetAllKnots();
//         }
        
//         Debug.Log($"[{gameObject.name}] 挑战已重置");
//     }
    
//     /// <summary>
//     /// 手动设置稳定范围（用于动态难度）
//     /// </summary>
//     public void SetStabilityRange(float min, float max)
//     {
//         stabilityRangeMin = Mathf.Clamp(min, -1f, 1f);
//         stabilityRangeMax = Mathf.Clamp(max, -1f, 1f);
        
//         // 确保 min < max
//         if (stabilityRangeMin > stabilityRangeMax)
//         {
//             float temp = stabilityRangeMin;
//             stabilityRangeMin = stabilityRangeMax;
//             stabilityRangeMax = temp;
//         }
//     }
    
//     /// <summary>
//     /// 获取当前目标区间（用于UI显示）
//     /// </summary>
//     public void GetCurrentTargetRange(out float min, out float max)
//     {
//         if (useKnotSystem && knotManager != null && knotManager.CurrentKnot != null)
//         {
//             min = knotManager.CurrentKnot.targetRangeMin;
//             max = knotManager.CurrentKnot.targetRangeMax;
//         }
//         else
//         {
//             min = stabilityRangeMin;
//             max = stabilityRangeMax;
//         }
//     }
    
//     // 调试可视化
//     private void OnDrawGizmosSelected()
//     {
//         // 绘制稳定范围指示器（仅在运行时使用IsInStableRange）
//         Gizmos.color = Application.isPlaying ? (IsInStableRange ? Color.green : Color.yellow) : Color.yellow;
        
//         // Draw a line representing the stability range
//         Vector3 basePos = transform.position + Vector3.up * 2f;
//         float rangeWidth = 2f;
        
//         // Map -1..1 to visual position
//         float minPos = (stabilityRangeMin + 1f) / 2f * rangeWidth - rangeWidth / 2f;
//         float maxPos = (stabilityRangeMax + 1f) / 2f * rangeWidth - rangeWidth / 2f;
        
//         Gizmos.DrawLine(
//             basePos + Vector3.left * (rangeWidth / 2f),
//             basePos + Vector3.right * (rangeWidth / 2f)
//         );
        
//         Gizmos.color = Color.green;
//         Gizmos.DrawLine(
//             basePos + Vector3.right * minPos,
//             basePos + Vector3.right * maxPos
//         );
        
//         // Draw progress
//         if (Application.isPlaying && currentStabilityTime > 0f)
//         {
//             Gizmos.color = Color.cyan;
//             Gizmos.DrawWireSphere(basePos, StabilityProgress * 0.5f);
//         }
//     }
// }
