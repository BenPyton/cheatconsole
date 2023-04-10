#if DEVELOPMENT_BUILD || UNITY_EDITOR
#define CONSOLE_DEBUG 
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CheatConsole : MonoBehaviour
{
    public readonly static UnityEvent OnOpen = new UnityEvent();
    public readonly static UnityEvent OnClose = new UnityEvent();

    public static void Log(string message)
    {
#if CONSOLE_DEBUG
        if (s_instance != null)
        {
            s_instance.m_outputLog += "\n" + message;
        }
#endif
    }

    public static void LogError(string message)
    {
#if CONSOLE_DEBUG
        if (s_instance != null)
        {
            s_instance.m_outputLog += "\n<color=red>[Error] " + message + "</color>";
        }
#endif
    }

    #region Private

    // Start is called before the first frame update
    void Awake()
    {
#if CONSOLE_DEBUG
        if (s_instance == null)
        {
            s_instance = this;
            DontDestroyOnLoad(gameObject);
            m_container?.SetActive(m_consoleEnabled);
            m_cursor = Instantiate(m_inputText, m_container?.transform);
            m_outputText.text = string.Empty;
            m_inputText.text = string.Empty;
            CheatManager.GetAllMethodNames(); // used only to init cheat manager, but used later to autocomplete
        }
        else
        {
            Destroy(gameObject);
        }
#else
        Destroy(gameObject);
#endif
    }


#if CONSOLE_DEBUG
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Quote))
        {
            m_consoleEnabled = !m_consoleEnabled;
            m_container?.SetActive(m_consoleEnabled);
            if(m_consoleEnabled)
            {
                OnOpen.Invoke();
            }
            else
            {
                OnClose.Invoke();
            }
        }
        else if (m_consoleEnabled)
        {
            UpdateInputField();
            UpdateOutputLog();
        }
    }

    private void UpdateInputField()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            PreviousCommand();
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            NextCommand();
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            m_cursorIndex++;
            m_cursorIndex = Mathf.Min(m_cursorIndex, m_inputCommand.Length);
            m_dirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            m_cursorIndex--;
            m_cursorIndex = Mathf.Max(m_cursorIndex, 0);
            m_dirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Delete)
            && m_cursorIndex < m_inputCommand.Length)
        {
            m_inputCommand = m_inputCommand.Remove(m_cursorIndex, 1);
            m_dirty = true;
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            string[] allMethodNames = CheatManager.GetAllMethodNames();
            List<string> matchingNames = new(allMethodNames.Where(s => s.Contains(m_inputCommand, System.StringComparison.CurrentCultureIgnoreCase)));
            if (matchingNames.Count == 0)
            {
                LogError($"No methods found that contain {m_inputCommand}");
            }
            else if (matchingNames.Count == 1)
            {
                m_inputCommand = matchingNames[0];
                m_cursorIndex = m_inputCommand.Count();
                m_dirty = true;
            }
            else
            {
                foreach (string methodName in matchingNames)
                {
                    Log($"[Suggestion] {methodName}");
                }
            }
        }
        else
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\n' || c == '\r')
                {
                    ExecuteCommand();
                    m_cursorIndex = 0;
                    m_dirty = true;
                    break;
                }
                else if (c == '\b')
                {
                    if (m_cursorIndex > 0)
                    {
                        m_inputCommand = m_inputCommand.Remove(m_cursorIndex - 1, 1);
                        m_cursorIndex--;
                        m_dirty = true;
                    }
                }
                else
                {
                    m_inputCommand = m_inputCommand.Insert(m_cursorIndex, c.ToString());
                    m_cursorIndex++;
                    m_dirty = true;
                }
            }
        }

        if (m_dirty)
        {
            m_inputText.text = m_prefix + m_inputCommand;
            m_cursor.text = new string(' ', m_prefix.Length + m_cursorIndex) + '_';
            m_dirty = false;
        }

        m_cursor.gameObject.SetActive((int)(Time.realtimeSinceStartup * 1000) % 1000 < 500);
    }

    private void UpdateOutputLog()
    {
        m_outputText.text = m_outputLog;
    }

    private void ExecuteCommand()
    {
        if (string.IsNullOrWhiteSpace(m_inputCommand))
            return;

        Log(m_prefix + m_inputCommand);

        m_currentHistoryIndex = -1;
        m_commandHistory.Add(m_inputCommand);
        if(!CheatManager.Execute(m_inputCommand))
        {
            LogError(CheatManager.ErrorMessage);
        }
        m_inputCommand = string.Empty;

        while(m_commandHistory.Count > MAX_COMMAND_HISTORY)
        {
            m_commandHistory.RemoveAt(0);
        }
    }

    private void PreviousCommand()
    {
        if(m_currentHistoryIndex < 0)
            m_currentHistoryIndex = m_commandHistory.Count - 1;
        else if(m_currentHistoryIndex > 0)
            m_currentHistoryIndex--;

        m_inputCommand = m_commandHistory[m_currentHistoryIndex];
        m_cursorIndex = m_inputCommand.Length;
        m_dirty = true;
    }

    private void NextCommand()
    {
        if (m_currentHistoryIndex < 0)
            return;

        if (m_currentHistoryIndex >= m_commandHistory.Count - 1)
            m_currentHistoryIndex = -1;
        else
            m_currentHistoryIndex++;

        m_inputCommand = m_currentHistoryIndex < 0 ? string.Empty : m_commandHistory[m_currentHistoryIndex];
        m_cursorIndex = m_inputCommand.Length;
        m_dirty = true;
    }

    private const int MAX_COMMAND_HISTORY = 20;
    private static CheatConsole s_instance = null;
    private List<string> m_commandHistory = new List<string>();
    private Text m_cursor = null;
    private string m_prefix = "> ";
    private string m_inputCommand = string.Empty;
    private string m_outputLog = string.Empty;
    private int m_currentHistoryIndex = 0;
    private int m_cursorIndex = 0;
    private bool m_dirty = true;
    private bool m_consoleEnabled = false;
#endif

    [SerializeField] private GameObject m_container = null;
    [SerializeField] private Text m_inputText = null;
    [SerializeField] private Text m_outputText = null;

    #endregion // Private

    #region Editor

#if UNITY_EDITOR
    [MenuItem("GameObject/Cheat Console", false, 30)]
    public static void CreateConsole(MenuCommand menuCommand)
    {
        CheatConsole canvas = CreateCanvas();
        RectTransform container = CreateContainer(canvas.gameObject);
        ScrollRect scrollView = CreateScrollview(container.gameObject);
        RectTransform viewport = CreateViewport(scrollView.gameObject);
        Scrollbar scrollbar = CreateScrollbar(scrollView.gameObject);
        RectTransform slidingArea = CreateSlidingArea(scrollbar.gameObject);
        RectTransform handle = CreateHandle(slidingArea.gameObject);
        RectTransform outputText = CreateOutputText(viewport.gameObject);
        RectTransform inputText = CreateInputText(container.gameObject);

        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollView.viewport = viewport.GetComponent<RectTransform>();
        scrollView.verticalScrollbar = scrollbar;
        scrollView.content = outputText;
        canvas.m_container = container.gameObject;
        canvas.m_outputText = outputText.GetComponent<Text>();
        canvas.m_inputText = inputText.GetComponent<Text>();
        container.gameObject.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "Create " + canvas.gameObject.name);
        Selection.activeObject = canvas.gameObject;
    }

    private static CheatConsole CreateCanvas()
    {
        GameObject canvas = new GameObject("CheatConsole"
            , typeof(RectTransform)
            , typeof(Canvas)
            , typeof(CanvasScaler)
            , typeof(GraphicRaycaster)
            , typeof(CheatConsole));

        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvas.gameObject.layer = LayerMask.NameToLayer("UI");

        return canvas.GetComponent<CheatConsole>();
    }

    private static RectTransform CreateContainer(GameObject parent)
    {
        GameObject container = new GameObject("Container"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(Image));
        GameObjectUtility.SetParentAndAlign(container, parent);

        container.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        container.GetComponent<RectTransform>().anchorMax = Vector2.one;
        container.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        container.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        container.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);

        return container.GetComponent<RectTransform>();
    }

    private static ScrollRect CreateScrollview(GameObject parent)
    {
        GameObject scrollView = new GameObject("ScrollView"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(ScrollRect));
        GameObjectUtility.SetParentAndAlign(scrollView, parent);

        scrollView.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        scrollView.GetComponent<RectTransform>().anchorMax = Vector2.one;
        scrollView.GetComponent<RectTransform>().offsetMin = new Vector2(10, 50);
        scrollView.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -10);
        scrollView.GetComponent<ScrollRect>().horizontal = false;
        scrollView.GetComponent<ScrollRect>().movementType = ScrollRect.MovementType.Clamped;
        scrollView.GetComponent<ScrollRect>().inertia = false;
        scrollView.GetComponent<ScrollRect>().scrollSensitivity = 5;
        scrollView.GetComponent<ScrollRect>().verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollView.GetComponent<ScrollRect>().verticalScrollbarSpacing = -3;

        return scrollView.GetComponent<ScrollRect>();
    }
    
    private static RectTransform CreateViewport(GameObject parent)
    {
        GameObject viewport = new GameObject("Viewport"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(Image)
            , typeof(Mask));
        GameObjectUtility.SetParentAndAlign(viewport, parent);
        
        viewport.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        viewport.GetComponent<Image>().type = Image.Type.Sliced;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        return viewport.GetComponent<RectTransform>();
    }

    private static Scrollbar CreateScrollbar(GameObject parent)
    {
        GameObject scrollbar = new GameObject("Scrollbar"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(Image)
            , typeof(Scrollbar));
        GameObjectUtility.SetParentAndAlign(scrollbar, parent);

        scrollbar.GetComponent<RectTransform>().pivot = Vector2.one;
        scrollbar.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0);
        scrollbar.GetComponent<RectTransform>().anchorMax = Vector2.one;
        scrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(-20, 0);
        scrollbar.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        scrollbar.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        scrollbar.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        scrollbar.GetComponent<Image>().type = Image.Type.Sliced;

        return scrollbar.GetComponent<Scrollbar>();
    }

    private static RectTransform CreateSlidingArea(GameObject parent)
    {
        GameObject slidingArea = new GameObject("SlidingArea"
            , typeof(RectTransform));
        GameObjectUtility.SetParentAndAlign(slidingArea, parent);
        
        slidingArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        slidingArea.GetComponent<RectTransform>().anchorMax = Vector2.one;
        slidingArea.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        slidingArea.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -10);

        return slidingArea.GetComponent<RectTransform>();
    }

    private static RectTransform CreateHandle(GameObject parent)
    {
        GameObject handle = new GameObject("Handle"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(Image));
        GameObjectUtility.SetParentAndAlign(handle, parent);
        
        handle.GetComponent<RectTransform>().offsetMin = new Vector2(-10, -10);
        handle.GetComponent<RectTransform>().offsetMax = new Vector2(10, 10);
        handle.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
        handle.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        handle.GetComponent<Image>().type = Image.Type.Sliced;

        return handle.GetComponent<RectTransform>();
    }

    private static RectTransform CreateOutputText(GameObject parent)
    {
        GameObject outputText = new GameObject("OutputText"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(Text)
            , typeof(ContentSizeFitter));
        GameObjectUtility.SetParentAndAlign(outputText, parent);

        outputText.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
        outputText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        outputText.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
        outputText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        outputText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        outputText.GetComponent<Text>().color = Color.white;
        outputText.GetComponent<Text>().font = GetFont();
        outputText.GetComponent<Text>().fontSize = 24;
        outputText.GetComponent<Text>().supportRichText = true;
        outputText.GetComponent<Text>().alignment = TextAnchor.LowerLeft;
        outputText.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return outputText.GetComponent<RectTransform>();
    }

    private static RectTransform CreateInputText(GameObject parent)
    {
        GameObject inputText = new GameObject("InputText"
            , typeof(RectTransform)
            , typeof(CanvasRenderer)
            , typeof(Text));
        GameObjectUtility.SetParentAndAlign(inputText, parent);

        inputText.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
        inputText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        inputText.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
        inputText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        inputText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 40);
        inputText.GetComponent<Text>().color = Color.white;
        inputText.GetComponent<Text>().font = GetFont();
        inputText.GetComponent<Text>().fontSize = 24;
        inputText.GetComponent<Text>().supportRichText = false;
        inputText.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        inputText.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Overflow;
        inputText.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;

        return inputText.GetComponent<RectTransform>();
    }

    private static Font m_cachedFont = null;
    private static Font GetFont()
    {
        if(m_cachedFont == null)
        {
            string fontPath = "Packages/com.pyton.cheatconsole/Runtime/CONSOLA.TTF";
            m_cachedFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (m_cachedFont == null)
                Debug.LogWarning(string.Format("Can't load font from \"{0}\"", fontPath));
        }
        return m_cachedFont;
    }
#endif

    #endregion // Editor
}
