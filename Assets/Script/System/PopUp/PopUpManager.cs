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

    void Awake() {
        _map.Clear();
        foreach(var e in entries) {
            if(e.id && e.panel) { _map[e.id] = e.panel; e.panel.SetActive(false); } 
        }
        _current = null;
    }

    public void Show(PopUpId id) {
        if(!id) return;

        CloseAll();
        if(_map.TryGetValue(id, out var go)) {
            go.SetActive(true);
            _current = id;
        }
    }

    public void CloseAll() {
        foreach(var kv in _map) kv.Value.SetActive(false);
        _current = null;
    }
}