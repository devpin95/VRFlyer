using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordingManager : MonoBehaviour
{
    public GameData gameData;
    
    private MemoryStream _memStream;
    private BinaryWriter _binWriter;
    // private BinaryReader _binReader;

    private bool _recordingInitialized = false;
    private bool _initialGameStateRecorded = false;

    private void Awake()
    {
        if (!gameData.recordingSession) return;
        
        _memStream = new MemoryStream();
        // _binReader = new BinaryReader(_memStream);
        _binWriter = new BinaryWriter(_memStream);

        _memStream.SetLength(0);
        _binWriter.Seek(0, SeekOrigin.Begin);

        _recordingInitialized = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void FixedUpdate()
    {
        if (!_recordingInitialized) return;
        if (!_initialGameStateRecorded) return;
    }

    public void SetInitialGameState()
    {
        if (!gameData.recordingSession) return;

        _binWriter.Write(gameData.Seed);
        _binWriter.Write(gameData.PlayerStartingOffset.x);
        _binWriter.Write(gameData.PlayerStartingOffset.y);
        _binWriter.Write(gameData.PerlinOffset.x);
        _binWriter.Write(gameData.PerlinOffset.y);
        
        _initialGameStateRecorded = true;
    }
}
