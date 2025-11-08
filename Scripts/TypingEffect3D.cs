using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class TypingEffect3D : MonoBehaviour
{
    [Header("Typing Settings")]
    public TextMeshPro text3D;
    [TextArea(3, 10)]
    public string fullText;
    public float typingSpeed = 0.05f;

    [Header("Optional")]
    public AudioSource typingSoundLoop;

    [Header("Auto Start")]
    public bool playOnStart = true;

    [Header("On Finish")]
    public TypingEffect3D nextTypingEffect; 

    private Coroutine typingCoroutine;

    void Start()
    {
        if (playOnStart)
        {
            StartTyping();
        }
    }

    public void StartTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        text3D.text = "";

        if (typingSoundLoop && !typingSoundLoop.isPlaying)
            typingSoundLoop.Play();

        foreach (char c in fullText)
        {
            text3D.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        if (typingSoundLoop && typingSoundLoop.isPlaying)
            typingSoundLoop.Stop();

        
        if (nextTypingEffect != null)
        {
            yield return new WaitForSeconds(1f); 
            nextTypingEffect.StartTyping();
        }
    }
}
