using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Doody.GameEvents;
using Doody.Framework.DialogueSystem;

public class DialogueUI : EventListener
{
    #region UI References
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    #endregion

    #region Typewriter Settings
    [Header("Typewriter Effect")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float defaultTypewriterSpeed = 30f;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private string continuePromptText = "Continue...";
    [SerializeField] private string exitPromptText = "Exit";
    private List<TextToken> currentTokens;
    private List<DialogueOption> currentOptions;
    private DialogueNode currentNode;
    #endregion

    #region Visual Effects
    [Header("Visual Effects")]
    [SerializeField] private bool useSineEffect = true;
    [SerializeField] private float sineAmplitude = 10f;
    [SerializeField] private float sineFrequency = 2f;
    [SerializeField] private float sineEffectDuration = 0.5f;

    [SerializeField] private float defaultShakeIntensity = 2f;
    [SerializeField] private float defaultShakeSpeed = 20f;

    [SerializeField, Range(1, 120)] private int effectsUpdateFPS = 30;
    private float effectsUpdateInterval;
    #endregion

    #region Audio Settings
    [Header("Typewriter SFX")]
    [SerializeField] private AudioSource audioSource;
    private AudioClip typeSound;
    [SerializeField] private AudioClip defaultTypeSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField, Range(0.95f, 1.05f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.95f, 1.05f)] private float maxPitch = 1.05f;
    [SerializeField, Range(0f, 1f)] private float typeVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float completeVolume = 1f;
    [SerializeField] private int charactersPerSound = 1;
    [SerializeField] private bool playSoundForSpaces = false;
    [SerializeField] private bool playSoundForPunctuation = true;
    [SerializeField] private float completeSoundDelay = 0.1f;
    #endregion

    #region Button Colors
    [Header("Button Colors")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color hoverButtonColor = new(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color pressedButtonColor = new(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color disabledButtonColor = new(0.5f, 0.5f, 0.5f, 0.5f);
    #endregion

    #region Data Classes
    private class CharacterEffectData
    {
        public int visualCharIndex;
        public bool shouldShake;
        public float shakeIntensity;
        public float shakeSpeed;
        public float sineStartTime;
    }
    #endregion

    #region Private Fields
    private readonly List<GameObject> activeButtons = new();
    private readonly List<CharacterEffectData> characterEffects = new();
    private Coroutine typewriterCoroutine;
    private Coroutine effectsCoroutine;
    private bool isTyping = false;
    private bool waitingForContinue = false;
    private int characterCount = 0;
    private StringBuilder richTextBuilder = new();
    private DialogueTextParser textParser;

    // For storing original vertex positions
    private List<Vector3>[] originalVertices;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (allowSkip && Input.GetKeyDown(skipKey))
        {
            if (isTyping)
            {
                SkipTypewriter();
            }
            else if (waitingForContinue)
            {
                // Determine which action to take based on what button is showing
                if (currentNode != null && currentNode.nextDialogue != null)
                {
                    OnContinueClicked();
                }
                else
                {
                    OnExitClicked();
                }
            }
        }
    }

    private void Start()
    {
        Listen<DialogueStartedEvent>(OnDialogueStarted);
        Listen<DialogueEndedEvent>(OnDialogueEnded);
        dialoguePanel.SetActive(false);
        SetupAudioSource();
        effectsUpdateInterval = 1f / effectsUpdateFPS;
        textParser = new DialogueTextParser(defaultShakeIntensity, defaultShakeSpeed);
    }
    #endregion

    #region Event Handlers
    private void OnDialogueStarted(DialogueStartedEvent evt) => DisplayDialogue(evt.Node, evt.Tree);
    private void OnDialogueEnded(DialogueEndedEvent evt) => HideDialogue();
    #endregion

    #region Dialogue Display
    private void DisplayDialogue(DialogueNode node, DialogueTree tree)
    {
        StopActiveCoroutines();
        ClearOptions();

        currentNode = node;
        dialoguePanel.SetActive(true);
        SetSpeakerInfo(tree.speakerName);
        if (node.typeWriterSfx != null)
        {
            typeSound = node.typeWriterSfx;
        }
        else
        {
            typeSound = defaultTypeSound;
        }
        if (!node.playTypewriterSfx)
            typeSound = null;
        if (useTypewriter)
        {

            float speed = node.typewriterSpeed > 0 ? node.typewriterSpeed : defaultTypewriterSpeed;
            typewriterCoroutine = StartCoroutine(TypewriterEffect(node.dialogueText, node.options, speed));

        }
        else
        {
            DisplayFullText(node.dialogueText);
            ShowOptionsOrContinue(node.options);
            PlayCompleteSound();
        }
    }

    private void DisplayFullText(string dialogueText)
    {
        var tokens = textParser.ParseText(dialogueText);
        ApplyTokensToText(tokens);
        StartEffectsCoroutine();
    }

    private void HideDialogue()
    {
        StopActiveCoroutines();
        dialoguePanel.SetActive(false);
        ClearOptions();
        characterEffects.Clear();
        richTextBuilder.Clear();
        originalVertices = null;
        waitingForContinue = false;
        currentNode = null;
    }

    private void StopActiveCoroutines()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        if (effectsCoroutine != null)
        {
            StopCoroutine(effectsCoroutine);
            effectsCoroutine = null;
        }
        isTyping = false;
    }
    #endregion

    #region Typewriter System
    private IEnumerator TypewriterEffect(string fullText, List<DialogueOption> options, float speed)
    {

        isTyping = true;
        characterEffects.Clear();
        richTextBuilder.Clear();
        originalVertices = null;

        var tokens = textParser.ParseText(fullText);

        // Store tokens for skip functionality
        currentTokens = tokens;
        currentOptions = options;

        dialogueText.text = "";
        characterCount = 0;
        float delay = 1f / speed;

        StartEffectsCoroutine();

        int visualCharIndex = 0;

        foreach (var token in tokens)
        {
            if (token.delayBefore > 0)
                yield return new WaitForSeconds(token.delayBefore);

            // Add opening color tag if needed
            if (token.hasHighlight)
            {
                richTextBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(token.highlightColor)}>");
            }

            foreach (char c in token.text)
            {
                richTextBuilder.Append(c);
                dialogueText.text = richTextBuilder.ToString();

                // Create effect data for this visual character
                if (token.shakeIntensity > 0)
                {
                    var effectData = new CharacterEffectData
                    {
                        visualCharIndex = visualCharIndex,
                        shouldShake = true,
                        shakeIntensity = token.shakeIntensity,
                        shakeSpeed = token.shakeSpeed,
                        sineStartTime = Time.time
                    };
                    characterEffects.Add(effectData);
                }

                visualCharIndex++;
                characterCount++;

                TryPlayCharacterSound(c);
                yield return new WaitForSeconds(delay);
            }

            // Add closing color tag if needed
            if (token.hasHighlight)
            {
                richTextBuilder.Append("</color>");
                dialogueText.text = richTextBuilder.ToString();
            }
        }

        CompleteTyping(options);
    }

    private void SkipTypewriter()
    {
        if (typewriterCoroutine == null) return;

        StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = null;
        isTyping = false;

        // Use the same method as DisplayFullText to apply all tokens
        ApplyTokensToText(currentTokens);

        // Show options or continue button
        ShowOptionsOrContinue(currentOptions);

        PlayCompleteSound();
    }

    private void TryPlayCharacterSound(char c)
    {
        if (typeSound == null) return;
        if (c == ' ' && !playSoundForSpaces) return;
        if (IsPunctuation(c) && !playSoundForPunctuation) return;
        if (characterCount % charactersPerSound != 0) return;

        PlayTypeSound();
    }

    private void CompleteTyping(List<DialogueOption> options)
    {
        isTyping = false;
        typewriterCoroutine = null;

        if (completeSound != null)
            StartCoroutine(PlayDelayedCompleteSound());

        ShowOptionsOrContinue(options);
    }

    private IEnumerator PlayDelayedCompleteSound()
    {
        yield return new WaitForSeconds(completeSoundDelay);
        audioSource.PlayOneShot(completeSound, completeVolume);
    }
    #endregion

    #region Continue System
    private void ShowOptionsOrContinue(List<DialogueOption> options)
    {
        // Check if there are valid options after filtering
        bool hasValidOptions = false;
        if (options != null && options.Count > 0)
        {
            foreach (DialogueOption option in options)
            {
                if (DialogueManager.Instance.MeetsOptionRequirements(option))
                {
                    hasValidOptions = true;
                    break;
                }
            }
        }

        if (hasValidOptions)
        {
            CreateOptionButtons(options);
        }
        else
        {
            // Check if there's a next dialogue to continue to
            bool hasNextDialogue = currentNode != null && currentNode.nextDialogue != null;

            if (hasNextDialogue)
            {
                CreateContinueButton();
            }
            else
            {
                CreateExitButton();
            }
        }
    }

    private void CreateContinueButton()
    {
        GameObject button = Instantiate(optionButtonPrefab, optionsContainer);
        var buttonComponent = button.GetComponent<Button>();
        var text = button.GetComponentInChildren<TMP_Text>();

        if (buttonComponent != null && text != null)
        {
            text.text = continuePromptText;
            buttonComponent.colors = CreateButtonColors();
            buttonComponent.onClick.AddListener(OnContinueClicked);
            activeButtons.Add(button);
            waitingForContinue = true;
        }
        else
        {
            Debug.LogError("Option button prefab is missing required components!");
            Destroy(button);
        }
    }

    private void CreateExitButton()
    {
        GameObject button = Instantiate(optionButtonPrefab, optionsContainer);
        var buttonComponent = button.GetComponent<Button>();
        var text = button.GetComponentInChildren<TMP_Text>();

        if (buttonComponent != null && text != null)
        {
            text.text = exitPromptText;
            buttonComponent.colors = CreateButtonColors();
            buttonComponent.onClick.AddListener(OnExitClicked);
            activeButtons.Add(button);
            waitingForContinue = true;
        }
        else
        {
            Debug.LogError("Option button prefab is missing required components!");
            Destroy(button);
        }
    }

    private void OnContinueClicked()
    {
        if (!waitingForContinue || currentNode == null) return;

        waitingForContinue = false;

        // Check if the current node has a next dialogue to continue to
        if (currentNode.nextDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(currentNode.nextDialogue);
        }
        else
        {
            DialogueManager.Instance.EndDialogue();
        }
    }

    private void OnExitClicked()
    {
        if (!waitingForContinue) return;

        waitingForContinue = false;
        DialogueManager.Instance.EndDialogue();
    }
    #endregion

    #region Visual Effects (Position Only)
    private void StartEffectsCoroutine()
    {
        if (effectsCoroutine != null)
            StopCoroutine(effectsCoroutine);
        effectsCoroutine = StartCoroutine(UpdateTextEffectsOptimized());
    }

    private IEnumerator UpdateTextEffectsOptimized()
    {
        WaitForSeconds waitInterval = new WaitForSeconds(effectsUpdateInterval);

        while (true)
        {
            yield return waitInterval;

            if (characterEffects.Count == 0)
                continue;

            dialogueText.ForceMeshUpdate();
            var textInfo = dialogueText.textInfo;

            if (textInfo.characterCount == 0)
                continue;

            // Initialize original vertices storage if needed
            if (originalVertices == null || originalVertices.Length != textInfo.meshInfo.Length)
            {
                originalVertices = new List<Vector3>[textInfo.meshInfo.Length];
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    originalVertices[i] = new List<Vector3>();
                }
            }

            // Store original vertices for each mesh
            for (int meshIndex = 0; meshIndex < textInfo.meshInfo.Length; meshIndex++)
            {
                var meshInfo = textInfo.meshInfo[meshIndex];
                var vertices = meshInfo.vertices;

                originalVertices[meshIndex].Clear();
                originalVertices[meshIndex].AddRange(vertices);
            }

            bool meshUpdated = false;

            // Apply position effects only
            for (int i = 0; i < characterEffects.Count; i++)
            {
                var effect = characterEffects[i];

                if (effect.visualCharIndex >= textInfo.characterCount)
                    continue;

                var charInfo = textInfo.characterInfo[effect.visualCharIndex];
                if (!charInfo.isVisible) continue;

                if (ApplyPositionEffects(effect.visualCharIndex, effect, textInfo))
                {
                    meshUpdated = true;
                }
            }

            if (meshUpdated)
                UpdateMeshes(textInfo);
        }
    }

    private bool ApplyPositionEffects(int index, CharacterEffectData effect, TMP_TextInfo textInfo)
    {
        var charInfo = textInfo.characterInfo[index];
        if (!charInfo.isVisible) return false;

        int vertexIndex = charInfo.vertexIndex;
        int materialIndex = charInfo.materialReferenceIndex;

        if (materialIndex >= textInfo.meshInfo.Length)
            return false;

        Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

        if (vertexIndex < 0 || vertexIndex + 3 >= vertices.Length)
            return false;

        // Calculate offset for position effects
        Vector3 offset = CalculateOffset(effect, index);

        if (offset == Vector3.zero)
            return false;

        // Apply vertex position offset relative to original positions
        bool hasOriginalData = originalVertices != null && materialIndex < originalVertices.Length &&
                              originalVertices[materialIndex].Count > vertexIndex + 3;

        if (hasOriginalData)
        {
            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + offset;
            }
            return true;
        }

        return false;
    }

    private Vector3 CalculateOffset(CharacterEffectData effect, int index)
    {
        Vector3 offset = Vector3.zero;
        float timeSinceStart = Time.time - effect.sineStartTime;

        if (useSineEffect && timeSinceStart < sineEffectDuration)
        {
            float progress = timeSinceStart / sineEffectDuration;
            float sineValue = Mathf.Sin(timeSinceStart * sineFrequency * Mathf.PI * 2);
            offset.y += sineValue * sineAmplitude * (1 - progress);
        }

        if (effect.shouldShake)
        {
            offset.x += Mathf.Sin(Time.time * effect.shakeSpeed + index) * effect.shakeIntensity;
            offset.y += Mathf.Cos(Time.time * effect.shakeSpeed * 1.3f + index) * effect.shakeIntensity;
        }

        return offset;
    }

    private void UpdateMeshes(TMP_TextInfo textInfo)
    {
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            dialogueText.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void ApplyTokensToText(List<TextToken> tokens)
    {
        characterEffects.Clear();
        richTextBuilder.Clear();
        originalVertices = null;
        int visualCharIndex = 0;

        foreach (var token in tokens)
        {
            // Add opening color tag if needed
            if (token.hasHighlight)
            {
                richTextBuilder.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(token.highlightColor)}>");
            }

            foreach (char c in token.text)
            {
                richTextBuilder.Append(c);

                // Create effect data for shake/sine
                if (token.shakeIntensity > 0)
                {
                    var effectData = new CharacterEffectData
                    {
                        visualCharIndex = visualCharIndex,
                        shouldShake = true,
                        shakeIntensity = token.shakeIntensity,
                        shakeSpeed = token.shakeSpeed,
                        sineStartTime = Time.time
                    };
                    characterEffects.Add(effectData);
                }

                visualCharIndex++;
            }

            // Add closing color tag if needed
            if (token.hasHighlight)
            {
                richTextBuilder.Append("</color>");
            }
        }

        dialogueText.text = richTextBuilder.ToString();
        dialogueText.ForceMeshUpdate();
    }
    #endregion

    #region Audio
    private void SetupAudioSource()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void PlayTypeSound()
    {
        if (typeSound == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(typeSound, typeVolume);
    }

    private void PlayCompleteSound()
    {
        if (completeSound != null && audioSource != null)
            audioSource.PlayOneShot(completeSound, completeVolume);
    }

    private static readonly char[] punctuationChars =
        { '.', ',', '!', '?', ';', ':', '-', '—', '(', ')', '[', ']', '{', '}', '\'', '"' };

    private static bool IsPunctuation(char c) => System.Array.IndexOf(punctuationChars, c) >= 0;
    #endregion

    #region Options Management
    private void CreateOptionButtons(List<DialogueOption> options)
    {
        if (options == null || options.Count == 0)
        {
            return;
        }

        ValidateOptionComponents();

        foreach (DialogueOption option in options)
        {
            if (!DialogueManager.Instance.MeetsOptionRequirements(option))
            {
                Debug.Log($"[DialogueUI] Hiding option '{option.optionText}' - requirements not met");
                continue;
            }

            GameObject button = Instantiate(optionButtonPrefab, optionsContainer);
            if (SetupButton(button, option))
                activeButtons.Add(button);
        }
    }

    private void ValidateOptionComponents()
    {
        if (optionButtonPrefab == null)
            throw new System.NullReferenceException("Option button prefab is not assigned!");
        if (optionsContainer == null)
            throw new System.NullReferenceException("Options container is not assigned!");
    }

    private bool SetupButton(GameObject buttonObj, DialogueOption option)
    {
        var button = buttonObj.GetComponent<Button>();
        var text = buttonObj.GetComponentInChildren<TMP_Text>();

        if (button == null || text == null)
        {
            Debug.LogError("Option button prefab is missing required components!");
            Destroy(buttonObj);
            return false;
        }

        text.text = option.optionText;
        button.colors = CreateButtonColors();
        button.onClick.AddListener(() => OnOptionClicked(option));
        return true;
    }

    private ColorBlock CreateButtonColors() => new()
    {
        normalColor = normalButtonColor,
        highlightedColor = hoverButtonColor,
        pressedColor = pressedButtonColor,
        disabledColor = disabledButtonColor,
        fadeDuration = 0.1f,
        colorMultiplier = 1f
    };

    private void OnOptionClicked(DialogueOption option) => DialogueManager.Instance.ChooseOption(option);

    private void ClearOptions()
    {
        foreach (GameObject button in activeButtons)
        {
            if (button != null)
                Destroy(button);
        }
        activeButtons.Clear();
    }
    #endregion

    #region Helper Methods
    private void SetSpeakerInfo(string speakerName)
    {
        if (!string.IsNullOrEmpty(speakerName))
            speakerNameText.text = speakerName;
    }
    #endregion
}

public class TextToken
{
    public string text;
    public float delayBefore;
    public float shakeIntensity;
    public float shakeSpeed;
    public Color highlightColor;
    public bool hasHighlight;
}