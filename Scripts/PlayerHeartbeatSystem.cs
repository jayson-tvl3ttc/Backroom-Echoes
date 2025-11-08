using UnityEngine;
using System.Collections;

public class PlayerHeartbeatSystem : MonoBehaviour
{
    [Header("音频设置")]
    public AudioClip heartbeatClip;        // 心跳声音频
    public AudioClip breathingClip;        // 喘息声音频

    [Header("检测设置")]
    public float dangerDistance = 8f;       // 危险距离
    public float checkInterval = 0.2f;      // 检测间隔

    [Header("音频设置")]
    public float heartbeatVolume = 0.8f;    // 心跳音要
    public float breathingVolume = 0.6f;    // 喘息音量
    public float fadeSpeed = 2f;            // 渐变速度
    public float breathingDelay = 1f;       // 喘息延迟

    [Header("调试设置")]
    public bool enableDebugLogs = true;

    // 私有变量
    private EnemyAI[] enemies;
    private bool isInDanger = false;
    private bool wasInDanger = false;
    private AudioSource heartbeatAudio;
    private AudioSource breathingAudio;
    private Coroutine fadeCoroutine;
    private Coroutine breathingCoroutine;

    void Start()
    {
        InitializeSystem();
    }

    void InitializeSystem()
    {
        // 查找所有敌人
        enemies = FindObjectsOfType<EnemyAI>();

        // 创建音频组件
        CreateAudioSources();

        // 开始检测
        StartCoroutine(DetectEnemiesCoroutine());

        if (enableDebugLogs)
            Debug.Log($"心跳系统初始化完成，找到 {enemies.Length} 个敌人");
    }

    void CreateAudioSources()
    {
        // 创建心跳AudioSource
        heartbeatAudio = gameObject.AddComponent<AudioSource>();
        heartbeatAudio.clip = heartbeatClip;
        heartbeatAudio.loop = true;
        heartbeatAudio.volume = 0f;
        heartbeatAudio.spatialBlend = 0f; // 2D音频
        heartbeatAudio.playOnAwake = false;

        // 创建喘息AudioSource
        breathingAudio = gameObject.AddComponent<AudioSource>();
        breathingAudio.clip = breathingClip;
        breathingAudio.loop = false;
        breathingAudio.volume = 0f;
        breathingAudio.spatialBlend = 0f; // 2D音频
        breathingAudio.playOnAwake = false;
    }

    IEnumerator DetectEnemiesCoroutine()
    {
        while (true)
        {
            CheckDangerStatus();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    void CheckDangerStatus()
    {
        wasInDanger = isInDanger;
        isInDanger = false;

        // 检查是否有敌人在危险范围内
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= dangerDistance)
            {
                isInDanger = true;
                break;
            }
        }

        // 状态变化时处理音频
        if (isInDanger != wasInDanger)
        {
            if (isInDanger)
            {
                StartHeartbeat();
            }
            else
            {
                StopHeartbeat();
            }
        }
    }

    void StartHeartbeat()
    {
        if (enableDebugLogs)
            Debug.Log("敌人进入危险范围，开始心跳");

        // 停止之前的协程
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (breathingCoroutine != null) StopCoroutine(breathingCoroutine);

        // 开始播放心跳
        if (!heartbeatAudio.isPlaying)
        {
            heartbeatAudio.Play();
        }

        // 渐入效果
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    void StopHeartbeat()
    {
        if (enableDebugLogs)
            Debug.Log("敌人离开危险范围，停止心跳");

        // 停止渐入协程
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // 开始渐出和喘息
        fadeCoroutine = StartCoroutine(FadeOutAndBreathe());
    }

    IEnumerator FadeIn()
    {
        while (heartbeatAudio.volume < heartbeatVolume)
        {
            heartbeatAudio.volume = Mathf.MoveTowards(heartbeatAudio.volume, heartbeatVolume, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        if (enableDebugLogs)
            Debug.Log("心跳渐入完成");
    }

    IEnumerator FadeOutAndBreathe()
    {
        // 渐出心跳
        while (heartbeatAudio.volume > 0f)
        {
            heartbeatAudio.volume = Mathf.MoveTowards(heartbeatAudio.volume, 0f, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        // 停止心跳
        heartbeatAudio.Stop();

        if (enableDebugLogs)
            Debug.Log("心跳渐出完成");

        // 延迟后播放喘息声
        yield return new WaitForSeconds(breathingDelay);

        // 播放喘息声
        if (breathingAudio && breathingClip)
        {
            breathingAudio.volume = breathingVolume;
            breathingAudio.Play();

            if (enableDebugLogs)
                Debug.Log("播放喘息声");
        }
    }

   

    // 调试用：显示危险范围
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 绘制危险检测范围
        Gizmos.color = isInDanger ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dangerDistance);
    }
}