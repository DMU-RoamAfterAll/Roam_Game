using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections;

public class AreaLocateControl : MonoBehaviour {
    [Header("GameData")]
    public GameObject Player; //플레이어
    public PlayerControl pc;
    public float minDistance; //section과의 최소 거리
    public int riverHeight;   //중간 river 구역의 폭
    public int areaNumber;    //area의 개수
    
    [Header("Data")]
    public int createdAreaCount;                //총 구역 개수
    public RandomSectionSpawner[] areas;        //구역을 참조

    [Header("Base Point")]
    public float[] width;
    public float[] height;
    public Vector2[] basePoint;

    public event System.Action OnAreaMoveFinished;

    void Awake() {
        createdAreaCount = 0;
    }

    void Start() {
        minDistance = MapSceneDataManager.mapData.initialMinDistance;
        riverHeight = MapSceneDataManager.mapData.riverHeight;
        areaNumber  = MapSceneDataManager.mapData.areaNumber;

        Player = MapSceneDataManager.Instance.Player;
        pc     = MapSceneDataManager.Instance.pc;
    }

    ///구역의 규격을 알아내는 함수
    public void FindAreaPoint() {
        if(createdAreaCount == areaNumber) {
            createdAreaCount--;

            areas = MapSceneDataManager.Instance.areaObjects
                .Where(spawner => spawner.CompareTag(Tag.Area))
                .Select(go => go.GetComponent<RandomSectionSpawner>())
                .ToArray();

            float x = 0;
            float y = 0;

            basePoint = new Vector2[createdAreaCount];
            width     = new float[createdAreaCount];
            height    = new float[createdAreaCount];

            for(int i = 0; i < createdAreaCount; i++) {
                width[i]  = areas[i].maxX - areas[i].minX;
                height[i] = areas[i].maxY - areas[i].minY;

                switch(i) {
                    case 0 :
                        x = minDistance + (width[i] / 2);
                        x *= -1f;
                        y = (height[i] / 2);
                        break;

                    case 1 :
                        x = minDistance + (width[i] / 2);
                        y = (height[i] / 2);
                        break;

                    case 2 :
                        x = minDistance + (width[i] / 2);
                        x *= -1f;
                        y = riverHeight + (height[0] > height[1] ? height[0] : height[1]) + minDistance + (height[i] / 2);
                        break;

                    case 3 :
                        x = minDistance + (width[i] / 2);
                        y = riverHeight + (height[0] > height[1] ? height[0] : height[1]) + minDistance + (height[i] / 2);
                        break;

                    case 4 :
                        x = 0;
                        y = minDistance + (height[i] / 2) +
                            (height[0] > height[1] ? height[0] : height[1]) + riverHeight + minDistance +
                            (height[2] > height[3] ? height[2] : height[3]);
                        break;
                }

                basePoint[i] = new Vector2(x, y);
            }

            StartCoroutine(MoveArea());
        }
    }

    IEnumerator MoveArea() {
        int count = createdAreaCount;

        // 1) 애니메이션 없이 즉시 목표 위치로 이동
        for (int i = 0; i < count; i++) {
            areas[i].transform.position = basePoint[i];
        }

        // 2) 리버/아이리스 섹션 생성
        CreateRiverSection();
        CreateIrisSection();

        // 3) 링크 스포너 준비
        var linker = GetComponent<LinkSectionSpawner>();
        if (linker == null) linker = gameObject.AddComponent<LinkSectionSpawner>();

        // 한 프레임 양보
        yield return null;

        // 4) 튜토리얼만 남기도록 비활성화
        StartToturial();

        // 5) 이벤트 알림
        OnAreaMoveFinished?.Invoke();
        EventManager.RaiseAreaMoveFinished();

        // 6) 펜딩 세이브 적용(있다면)
        var slm = SaveLoadManager.Instance;
        if (slm != null && slm.pendingLoadData != null) {
            Debug.Log("[AreaLocate] Applying pending save after map assembled");
            yield return StartCoroutine(slm.ApplyLoadedData(slm.pendingLoadData));
            slm.pendingLoadData = null;
        }

        // 7) 저장/업로드 및 상태 갱신
        SaveLoadManager.Instance.SaveNow();
        SaveLoadManager.Instance.SaveNowAndUpload(GameDataManager.Data.playerName);

        MapSceneDataManager.mapData.isMapSetUp = true;
        pc.isCanMove = true;
        pc.DetectSection();
        Debug.Log("Detect Section");

        // --- 백드롭 배치: 아래끝을 y=-15에 고정, 비율 유지하며 모든 Area를 덮도록 ---
        if (TryGetAllAreasBounds(out var center, out var size, out var area)) {
            Debug.Log($"All Areas Bounds → center={center}, size={size}, area={area}");

            const string BackdropName = "__AreasBackdrop__";
            var go = GameObject.Find(BackdropName);
            SpriteRenderer sr;

            if (go == null) {
                go = new GameObject(BackdropName);
                go.transform.SetParent(this.transform, false);
                sr = go.AddComponent<SpriteRenderer>();

                sr.sharedMaterial = GetUnlitSpriteMaterial();
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                // Resources/ImgObj/MapBack.png  → "ImgObj/MapBack"
                sr.sprite = Resources.Load<Sprite>("ImgObj/MapBack");
            } else {
                sr = go.GetComponent<SpriteRenderer>();
            }

            if (sr == null || sr.sprite == null) {
                Debug.LogWarning("[AreaBackdrop] SpriteRenderer 또는 Sprite가 없습니다. 경로를 확인하세요.");
            } else {
                // 레이어/소팅 레이어 World로 통일
                int worldLayer = LayerMask.NameToLayer("World");
                if (worldLayer == -1) {
                    Debug.LogWarning("[AreaBackdrop] 'World' 레이어가 없습니다. Tags & Layers에서 만들어 주세요.");
                } else {
                    go.layer = worldLayer;
                }
                sr.sortingLayerName = "World";   // Sorting Layers에 'World'가 있어야 함
                sr.sortingOrder     = -1000;     // 필요 시 조절

                // 스프라이트 원본 크기(월드 단위, pivot 무관/PPU 반영)
                var sprBounds = sr.sprite.bounds;
                Vector2 sbSize = sprBounds.size;

                if (sbSize.x > 1e-5f && sbSize.y > 1e-5f) {
                    // 덮어야 하는 가로 폭: 모든 Area의 width
                    float targetWidth = size.x;

                    // 덮어야 하는 세로 높이: 아래끝을 -15로 고정하고, 위는 Area의 최상단까지
                    float maxY = center.y + size.y * 0.5f; // 모든 Area의 최상단
                    float bottomY = -35f;                  // 요구조건: 아래끝 고정
                    float targetHeight = Mathf.Max(0f, maxY - bottomY);

                    // 비율 유지(Uniform Scale)로 전체 덮기 → 더 큰 스케일 사용
                    float scaleX = targetWidth  / sbSize.x;
                    float scaleY = targetHeight / sbSize.y;
                    float uniform = Mathf.Max(scaleX, scaleY);
                    sr.transform.localScale = new Vector3(uniform, uniform, 1f);

                    // 아래끝(y=-15)에 맞추기 위해, 피벗→아랫변 로컬거리를 스케일해 더해줌
                    float localPivotToBottom = -sprBounds.min.y;       // 로컬 단위
                    float worldPivotToBottom = localPivotToBottom * uniform;

                    // 최종 위치:
                    //   X는 Area의 중앙(center.x)로 정렬
                    //   Y는 아래끝이 -15가 되도록 보정
                    float posX = center.x;
                    float posY = bottomY + worldPivotToBottom;
                    sr.transform.position = new Vector3(posX, posY, 0f);
                } else {
                    Debug.LogWarning("[AreaBackdrop] Sprite bounds size가 0에 가깝습니다. PPU/Import 설정 확인.");
                }
            }
        }
    }

    void CreateRiverSection() {
        float maxUpperHeight = Mathf.Max(height[0], height[1]);
        float riverCenterY = maxUpperHeight + (riverHeight / 2f) + (minDistance / 2f);

        GameObject go = Instantiate(MapSceneDataManager.mapData.riverSectionPrefab, new Vector2(0, riverCenterY), Quaternion.identity);
        MapSceneDataManager.Instance.riverSection = go.GetComponent<SectionData>();
        go.transform.SetParent(this.transform);
    }

    void CreateIrisSection() {
        float irisHeight = basePoint[4].y + (height[4] / 2) + minDistance;

        GameObject go = Instantiate(MapSceneDataManager.mapData.IrisSectionPrefab, new Vector2(0, irisHeight), Quaternion.identity);
        go.transform.SetParent(this.transform);
    }

    void StartToturial() {
        if(GameDataManager.Data.tutorialClear) return;
        
        for(int i = 0; i < areaNumber - 1; i++) { areas[i].gameObject.SetActive(false); }
        foreach(var s in areas[areaNumber - 1].Sections) {
            s.gameObject.AddComponent<TutorialManager>();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if (areas == null || areas.Length == 0) return;
        if (TryGetAllAreasBounds(out var c, out var s, out _)) {
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireCube(c, s);
        }
    }
#endif

    // 모든 Area(areas[])를 포함하는 최소 경계의 중심(center)과 크기(size: width,height)을 구함.
    // 성공 시 true, 실패(areas 비어있음 등) 시 false 반환.
    public bool TryGetAllAreasBounds(out Vector2 center, out Vector2 size, out float area) {
        center = default;
        size   = default;
        area   = 0f;

        if (areas == null || areas.Length == 0) return false;

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        bool any = false;

        foreach (var a in areas) {
            if (a == null) continue;

            // RandomSectionSpawner의 min/max는 로컬 기준 → 월드 경계 = 트랜스폼 위치 + 로컬 경계
            Vector3 p = a.transform.position;

            float worldMinX = p.x + a.minX;
            float worldMaxX = p.x + a.maxX;
            float worldMinY = p.y + a.minY;
            float worldMaxY = p.y + a.maxY;

            if (worldMinX < minX) minX = worldMinX;
            if (worldMaxX > maxX) maxX = worldMaxX;
            if (worldMinY < minY) minY = worldMinY;
            if (worldMaxY > maxY) maxY = worldMaxY;

            any = true;
        }

        if (!any) return false;

        size   = new Vector2(maxX - minX, maxY - minY);                   // 최소 너비/높이
        center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f); // 중심
        area   = size.x * size.y;                                         // (참고) 면적

        return true;
    }

    private static Material _unlitSpriteMat;
    private static Material GetUnlitSpriteMaterial()
    {
        if (_unlitSpriteMat == null)
        {
            // URP 2D에서는 Sprites/Default가 언리트(빛 영향 X)
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Texture"); // 폴백
            _unlitSpriteMat = new Material(shader);
        }
        return _unlitSpriteMat;
    }
}