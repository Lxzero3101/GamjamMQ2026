using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed = 0.05f;

    [Header("Settings")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    private int index;
    private bool isRunning = false;
    private bool hasTriggered = false;
    private PlayerMovement playerMovement;

    // ── Trigger Detection ──────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other) // swap to OnTriggerEnter(Collider other) if 3D
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;
        if (isRunning) return;

        hasTriggered = true;

        // Grab PlayerMovement from the player that touched this trigger
        playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false; // freeze player entirely
        }

        StartDialogue();
    }

    // ── Dialogue Logic ─────────────────────────────────────────────
    void StartDialogue()
    {
        isRunning = true;
        index = 0;
        textComponent.text = string.Empty;
        textComponent.gameObject.SetActive(true);
        StartCoroutine(TypeLine());
    }

    void Update()
    {
        if (!isRunning) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (textComponent.text == lines[index])
                NextLine();
            else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
        }
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isRunning = false;
        textComponent.text = string.Empty;
        textComponent.gameObject.SetActive(false);

        // Unfreeze player
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    // Call this from anywhere if you want to allow re-triggering
    public void ResetTrigger() => hasTriggered = false;
}