using System;
using System.Collections.Generic;
using UnityEngine;

public class PopUpManager : MonoBehaviour {
    [Serializable]
    public struct Entry {
        public PopUpId id;
        public GameObject panel;
    }

    [SerializeField] private List<Entry> entries = new();
    private readonly Dictionary<PopUpId, GameObject> _map = new();
    private PopUpId _current;

    bool isMapSet = false;

    void Awake() {
        isMapSet = false;
        EventManager.AreaMoveFinished += OnAreaMoveFinished;
        _map.Clear();
        foreach(var e in entries) {
            if(e.id && e.panel) { _map[e.id] = e.panel; e.panel.SetActive(false); } 
        }
        _current = null;
    }

    public void Show(PopUpId id) {
        if(!isMapSet || !id) return;

        CloseAll();
        if(_map.TryGetValue(id, out var go)) {
            MapSceneDataManager.Instance.isPopUpOn = true;
            go.SetActive(true);
            _current = id;
        }
    }

    public void CloseAll() {
        MapSceneDataManager.Instance.isPopUpOn = false;
        foreach(var kv in _map) kv.Value.SetActive(false);
        _current = null;
    }

    void OnAreaMoveFinished() {
        isMapSet = true;
    }
}