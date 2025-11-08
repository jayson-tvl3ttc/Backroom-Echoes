using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CabinetDoorAudio : MonoBehaviour
{
    [Header("音效设置")]
    public AudioSource audioSource;
    public AudioClip handleTurnSound;       // 门把手扭动音效
    public AudioClip handleReleaseSound;    // 门把手回弹音效

    

    void Start()
    {
        // 获取XR交互组件并绑定事件
        XRGrabInteractable interactable = GetComponent<XRGrabInteractable>();
        if (interactable != null)
        {
            // 当玩家开始抓取门把手时播放音效
            interactable.selectEntered.AddListener(OnHandleGrab);
            // 当玩家松开门把手时播放回弹音效
            interactable.selectExited.AddListener(OnHandleRelease);
        }
        
    }

    void OnHandleGrab(SelectEnterEventArgs args)
    {
        PlayHandleTurnSound();
    }

    void PlayHandleTurnSound()
    {
        if (audioSource != null && handleTurnSound != null)
        {
            audioSource.PlayOneShot(handleTurnSound);        
        }
        
    }

    void OnHandleRelease(SelectExitEventArgs args)
    {
        PlayHandleReleaseSound();
    }

    void PlayHandleReleaseSound()
    {
        if (audioSource != null && handleReleaseSound != null)
        {
            audioSource.PlayOneShot(handleReleaseSound);
        }
        
    }

}