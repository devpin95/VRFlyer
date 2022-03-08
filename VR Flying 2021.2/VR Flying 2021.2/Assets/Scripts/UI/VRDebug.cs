using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VRDebug : MonoBehaviour
{
    public int maxLogDisplay = 5;
    [SerializeField] private int logCount = 0;
    [SerializeField] private int displayCount = 0;
    
    [Header("List Colors")]
    public Color even = Color.white;
    public Color odd = Color.black;
    
    [Header("Message Colors")]
    public Color errorColor = Color.red;
    public Color assertColor = Color.magenta;
    public Color warningColor = Color.yellow;
    public Color logColor = Color.green;
    public Color exceptionColor = Color.blue;

    public GameObject debugMessagePrefab;

    [Header("Options")] 
    public bool hideWarnings = false;

    private const string padding = "   ";

    private Dictionary<LogType, Color> logColors;
    private Queue<Entry> entryQueue;

    private struct Entry
    {
        public string content;
        public string author;
        public LogType type;

        public Entry(string content, string author, LogType type)
        {
            this.content = content;
            this.author = author;
            this.type = type;
        }
    }

    private void Start()
    {
        Application.logMessageReceived += GetLog;

        entryQueue = new Queue<Entry>();
        
        logColors = new Dictionary<LogType, Color>();
        logColors.Add(LogType.Error, errorColor);
        logColors.Add(LogType.Assert, assertColor);
        logColors.Add(LogType.Warning, warningColor);
        logColors.Add(LogType.Log, logColor);
        logColors.Add(LogType.Exception, exceptionColor);
    }

    private void Update()
    {
        if (entryQueue.Count > 0)
        {
            Entry entry = entryQueue.Dequeue();
            
            if ( hideWarnings && entry.type == LogType.Warning ) return;
        
            ++logCount;
            ++displayCount;
        
            // make a new message
            GameObject newMessageObj = Instantiate(debugMessagePrefab, parent: transform);
            newMessageObj.GetComponent<Image>().color = (logCount % 2 == 0 ? even : odd);

            TextMeshProUGUI tmp = newMessageObj.transform.Find("Message Text").GetComponent<TextMeshProUGUI>();
            tmp.text = padding + "(" + logCount + ") " + entry.content + "\n<color=#ededed>" + SplitAndFormatTrace(entry.author) + "</color>";
            tmp.color = logColors[entry.type];

            if (displayCount > maxLogDisplay)
            {
                int needToDelete = displayCount - maxLogDisplay;
            
                for ( int i = 0; i < needToDelete; ++i )
                {
                    GameObject targetChild = transform.GetChild(0).gameObject;
                    Destroy(targetChild);
                    --displayCount;
                }
            }
        }
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= GetLog;
    }

    public void GetLog(string content, string author, LogType type)
    {
        Entry entry = new Entry(content, author, type);

        lock (entryQueue)
        {
            entryQueue.Enqueue(entry);
        }
        
    }

    private string SplitAndFormatTrace(string trace)
    {
        string formatted = "";

        string[] crumbs = trace.Split("\n");

        foreach (var crumb in crumbs)
        {
            formatted += "\n" + padding + crumb;
        }

        return formatted;
    }
}