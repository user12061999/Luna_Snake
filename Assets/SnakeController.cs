using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [Header("Segments Prefabs")]
    public Transform headPrefab;
    public Transform bodyPrefab;
    public Transform tailPrefab;
    public Transform segmentsParent;
    [Header("Move Settings")]
    public float stepDistance = 1f;
    public float moveDuration = 0.12f;
    public float fallDuration = 0.08f;

    [Header("Physics Masks")]
    public LayerMask wallMask;      // tường / block
    public LayerMask groundMask;    // nền đứng được
    public LayerMask segmentMask;   // layer của các segment rắn
    public LayerMask spikeMask;     // layer Spike
    public LayerMask finishMask;    // layer GateFinish
    public LayerMask appleMask;    // layer GateFinish

    [Header("Audio - SFX")]
    public AudioSource sfxSource;
    public AudioClip sfxMove;
    public AudioClip sfxCantMove;
    public AudioClip sfxFall;
    public AudioClip sfxEat;
    public AudioClip sfxDie;
    public AudioClip sfxGate;
    [Header("Audio - Music")]
    public AudioSource musicSource;
    public AudioClip musicClip;
    public AudioClip loseMusicClip;
    public AudioClip winMusicClip;

    private bool musicStarted = false;

    private List<Transform> segments = new List<Transform>(); // [0] = head, [last] = tail
    private Vector2 moveDir = Vector2.right;
    private bool isBusy = false;
    private bool moveSucceeded = false;
    private bool isDead = false;
    private bool isWin = false;
    [Header("Finish Gate")]
    public float gateSuckDurationPerSegment = 0.08f; // thời gian mỗi segment bị hút

    private bool isFinishing = false; // đang trong animation chui vào cổng

    private SnakeHeadAnimator headAnim; // Thêm biến này để dùng Spine anim

    void Start()
    {
        SpawnInitialSnake();
    }

    void Update()
    {
        // Test input từ keyboard
        if (Input.GetKeyDown(KeyCode.W)) OnMoveUp();
        else if (Input.GetKeyDown(KeyCode.S)) OnMoveDown();
        else if (Input.GetKeyDown(KeyCode.A)) OnMoveLeft();
        else if (Input.GetKeyDown(KeyCode.D)) OnMoveRight();

        // Check nếu có Apple bên cạnh thì trigger idleToEat
        CheckAppleAroundHead();
    }

    void CheckAppleAroundHead()
    {
        if (segments.Count == 0 || headAnim == null) return;

        Vector3[] directions = new Vector3[]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right
        };

        foreach (var dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(segments[0].position, dir, stepDistance * 0.9f, appleMask);
            if (hit.collider != null && hit.collider.CompareTag("Apple"))
            {
                headAnim.PlayIdleToEat();
                return;
            }
        }
    }

    public void OnMoveUp() => TryStartMove(Vector2.up);
    public void OnMoveDown() => TryStartMove(Vector2.down);
    public void OnMoveLeft() => TryStartMove(Vector2.left);
    public void OnMoveRight() => TryStartMove(Vector2.right);

    void TryStartMove(Vector2 dir)
    {
        if (isBusy || isDead || isWin || isFinishing) return;
        if (dir == Vector2.zero) return;


        moveDir = dir.normalized;
        StartMusicIfNeeded();
        StartCoroutine(PerformStepWithGravity());
    }

    void SpawnInitialSnake()
    {
        segments.Clear();


        Vector3 pos = transform.position;


        Transform head = Instantiate(headPrefab, pos, Quaternion.identity, segmentsParent);
        segments.Add(head);


        headAnim = head.GetComponentInChildren<SnakeHeadAnimator>();
        headAnim?.PlayIdle();


        Transform body = Instantiate(bodyPrefab, pos - new Vector3(stepDistance, 0f, 0f), Quaternion.identity, segmentsParent);
        segments.Add(body);


        Transform tail = Instantiate(tailPrefab, pos - new Vector3(stepDistance * 2f, 0f, 0f), Quaternion.identity, segmentsParent);
        segments.Add(tail);


        UpdateSegmentRotations();
    }

    // giữ nguyên phần còn lại...


    // =========================================================
    // STEP + GRAVITY
    // =========================================================
    IEnumerator PerformStepWithGravity()
    {
        isBusy = true;

        yield return StartCoroutine(MoveOneStepAnimated());

        if (!moveSucceeded || isDead || isWin)
        {
            isBusy = false;
            yield break;
        }

        yield return StartCoroutine(ApplyGravityAnimated());

        isBusy = false;
    }

    // =========================================================
    // MOVE ONE STEP (ANIMATED) + APPLE → GROW SAU HEAD
    // =========================================================
    IEnumerator MoveOneStepAnimated()
    {
        moveSucceeded = false;
        Vector3 dir3 = new Vector3(moveDir.x, moveDir.y, 0f);
        LayerMask blockMask = wallMask | groundMask | segmentMask | spikeMask;


        if (Physics2D.Raycast(segments[0].position, dir3, stepDistance, blockMask))
        {
            PlaySFX(sfxCantMove);
            headAnim?.PlayStuck();
            yield break;
        }


        PlaySFX(sfxMove);


        Collider2D appleToEat = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(segments[0].position, dir3, stepDistance);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Apple"))
            {
                appleToEat = hit.collider;
                break;
            }
        }
        bool willGrow = (appleToEat != null);


        List<Vector3> startPos = GetPositions();
        int oldCount = startPos.Count;
        List<Vector3> targetPos = new List<Vector3>(startPos);


        targetPos[0] = startPos[0] + dir3 * stepDistance;
        for (int i = 1; i < oldCount; i++)
            targetPos[i] = startPos[i - 1];


        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / moveDuration);


            for (int i = 0; i < segments.Count; i++)
                segments[i].position = Vector3.Lerp(startPos[i], targetPos[i], a);


            UpdateSegmentRotations();
            yield return null;
        }


        for (int i = 0; i < segments.Count; i++)
            segments[i].position = targetPos[i];


        if (CheckSpikeHit() || CheckFinish())
        {
            moveSucceeded = false;
            yield break;
        }


        if (!willGrow)
        {
            moveSucceeded = true;
            UpdateSegmentRotations();
            headAnim?.PlayIdle();
            yield break;
        }


        PlaySFX(sfxEat);
        headAnim?.PlayEat();
        Destroy(appleToEat.gameObject);


        List<Vector3> newPositions = new List<Vector3>(oldCount + 1);
        newPositions.Add(startPos[0] + dir3 * stepDistance);
        newPositions.Add(startPos[0]);
        for (int i = 1; i < oldCount; i++)
            newPositions.Add(startPos[i]);


        Transform newBody = Instantiate(bodyPrefab, newPositions[1], Quaternion.identity, segmentsParent);
        segments.Insert(1, newBody);


        for (int i = 0; i < segments.Count; i++)
            segments[i].position = newPositions[i];


        if (CheckSpikeHit() || CheckFinish())
        {
            moveSucceeded = false;
            yield break;
        }


        moveSucceeded = true;
        UpdateSegmentRotations();
        headAnim?.PlayIdle();
    }

    // =========================================================
    // GRAVITY (ANIMATED)
    // =========================================================
    IEnumerator ApplyGravityAnimated()
    {
        while (ShouldFallOneUnit())
        {
            // rơi 1 “ô” -> SFX fall
            PlaySFX(sfxFall);

            List<Vector3> startPos = GetPositions();
            List<Vector3> targetPos = new List<Vector3>(startPos.Count);
            for (int i = 0; i < startPos.Count; i++)
                targetPos.Add(startPos[i] + Vector3.down * stepDistance);

            float t = 0f;
            while (t < fallDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fallDuration);

                for (int i = 0; i < segments.Count; i++)
                    segments[i].position = Vector3.Lerp(startPos[i], targetPos[i], a);

                UpdateSegmentRotations();
                yield return null;
            }

            for (int i = 0; i < segments.Count; i++)
                segments[i].position = targetPos[i];

            if (CheckSpikeHit() || CheckFinish())
                yield break;
        }
    }

    bool ShouldFallOneUnit()
    {
        // Spike không phải là điểm tựa
        LayerMask supportMask = groundMask | wallMask | finishMask| appleMask;

        for (int i = 0; i < segments.Count; i++)
        {
            if (Physics2D.Raycast(
                segments[i].position,
                Vector2.down,
                stepDistance * 0.9f,
                supportMask))
            {
                return false;
            }
        }
        return true;
    }

    // =========================================================
    // SPIKE & FINISH
    // =========================================================
    // Chỉ GameOver khi CÓ spike bên dưới nhưng KHÔNG segment nào có điểm tựa an toàn
    bool CheckSpikeHit()
    {
        float checkDist = stepDistance * 0.9f;

        LayerMask safeSupportMask = groundMask | wallMask | finishMask | appleMask;
        LayerMask combinedMask = safeSupportMask | spikeMask;

        bool hasAnySpikeSupport = false;
        bool hasAnySafeSupport = false;

        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];

            RaycastHit2D[] hits = Physics2D.RaycastAll(
                seg.position,
                Vector2.down,
                checkDist,
                combinedMask
            );

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                int hitLayerMask = 1 << hit.collider.gameObject.layer;

                if ((spikeMask & hitLayerMask) != 0)
                    hasAnySpikeSupport = true;

                if ((safeSupportMask & hitLayerMask) != 0)
                    hasAnySafeSupport = true;
            }
        }

        if (hasAnySpikeSupport && !hasAnySafeSupport)
        {
            GameOver();
            return true;
        }

        return false;
    }

    bool CheckFinish()
    {
        // nếu đã bắt đầu hút rồi thì coi như "đã trúng cổng"
        if (isFinishing) return true;

        float radius = stepDistance * 0.45f;
        Transform head = segments[0];

        Collider2D hit = Physics2D.OverlapCircle(head.position, radius, finishMask);
        if (hit != null)
        {
            StartFinishSequence(hit);
            return true; // báo cho move/gravity dừng lại
        }
        return false;
    }
    void StartFinishSequence(Collider2D gate)
    {
        if (isFinishing) return;
        isFinishing = true;

        PlaySFX(sfxGate);
        StopMusic();

        Vector3 gatePos = gate.bounds.center;

        // bắt đầu hút bằng di chuyển kiểu rắn
        StartCoroutine(SnakeMoveIntoGate(gatePos));
    }
    IEnumerator SnakeMoveIntoGate(Vector3 gatePos)
    {
        isBusy = true;

        // hướng từ head -> gate
        while (segments.Count > 0)
        {
            // bước di chuyển
            Vector3 dir = (gatePos - segments[0].position);
            if (dir.magnitude < stepDistance * 0.5f)
            {
                // HEAD đã vào cổng -> ẩn head
                HideSegment(0);
                continue;
            }

            Vector3 stepDir = GetStepDirection(dir);

            // di chuyển từng bước như MoveOneStepAnimated()
            yield return StartCoroutine(MoveOneStepTowards(stepDir, gatePos));

            // nếu head tới cổng → ẩn dần
            if ((segments[0].position - gatePos).sqrMagnitude < 0.1f)
            {
                HideSegment(0);
            }
        }

        // tất cả đã vào cổng
        WinGame();
    }
    Vector3 GetStepDirection(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return new Vector3(Mathf.Sign(dir.x), 0, 0);

        return new Vector3(0, Mathf.Sign(dir.y), 0);
    }

    void HideSegment(int index)
    {
        Transform seg = segments[index];

        var rend = seg.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        var col = seg.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        segments.RemoveAt(index);
        Destroy(seg.gameObject);
    }

    IEnumerator AbsorbIntoGate(Vector3 gatePos)
    {
        isBusy = true;  // chặn mọi logic khác

        // duyệt từ head -> tail
        for (int i = 0; i < segments.Count; i++)
        {
            Transform seg = segments[i];
            if (seg == null) continue;

            Vector3 start = seg.position;
            float t = 0f;

            // lerp segment vào tâm cổng
            while (t < gateSuckDurationPerSegment)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / gateSuckDurationPerSegment);
                seg.position = Vector3.Lerp(start, gatePos, a);
                yield return null;
            }

            // ẩn segment khi đã "vào trong cổng"
            var rend = seg.GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
            var col = seg.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            yield return new WaitForSeconds(0.02f);
        }

        // khi tất cả đã chui vào cổng -> win
        WinGame();
    }

    IEnumerator MoveOneStepTowards(Vector3 stepDir, Vector3 gatePos)
    {
        List<Vector3> startPos = GetPositions();
        int count = startPos.Count;

        // target = như rắn di chuyển
        List<Vector3> target = new List<Vector3>(startPos);
        target[0] = startPos[0] + stepDir * stepDistance;

        for (int i = 1; i < count; i++)
            target[i] = startPos[i - 1];

        // animate
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float a = t / moveDuration;

            for (int i = 0; i < count; i++)
                segments[i].position = Vector3.Lerp(startPos[i], target[i], a);

            yield return null;
        }

        // snap
        for (int i = 0; i < count; i++)
            segments[i].position = target[i];
    }

    void GameOver()
    {
        if (isDead || isWin) return;
        isDead = true;


        PlaySFX(sfxDie);
        headAnim?.PlayDie();
        LunaManager.ins.ShowEndCard(2f);
        PlayLoseSound();
        Debug.Log("GAME OVER (Spike)");
    }


    void WinGame()
    {
        if (isWin || isDead) return;
        isWin = true;


        LunaManager.ins.ShowWinCard(2f);
        PlaySFX(sfxGate);
        StopMusic();
        PlayWinSound();
        Debug.Log("WIN! (GateFinish)");
    }

    // =========================================================
    // AUDIO UTILS
    // =========================================================
    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    void StartMusicIfNeeded()
    {
        if (musicStarted) return;
        if (musicSource == null || musicClip == null) return;

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.Play();
        musicStarted = true;
    }

    public void PlayLoseSound()
    {
        if (musicSource == null) return;

        musicSource.clip = loseMusicClip;
        musicSource.loop = false;
        musicSource.Play();
    }
    public void PlayWinSound()
    {
        if (musicSource == null) return;

        musicSource.clip = winMusicClip;
        musicSource.loop = false;
        musicSource.Play();
    }
    void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    // =========================================================
    // UTILS
    // =========================================================
    List<Vector3> GetPositions()
    {
        List<Vector3> result = new List<Vector3>(segments.Count);
        for (int i = 0; i < segments.Count; i++)
            result.Add(segments[i].position);
        return result;
    }

    void UpdateSegmentRotations()
    {
        if (segments.Count == 0) return;

        // xoay HEAD theo hướng moveDir
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float headAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            segments[0].rotation = Quaternion.Euler(0, 0, headAngle);
        }

        // xoay TAIL theo hướng từ segment trước -> tail
        if (segments.Count >= 2)
        {
            Transform tail = segments[segments.Count - 1];
            Transform beforeTail = segments[segments.Count - 2];

            Vector3 dir = (tail.position - beforeTail.position).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float tailAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                tail.rotation = Quaternion.Euler(0, 0, tailAngle);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (segments == null || segments.Count == 0) return;

        Gizmos.color = Color.red;
        float r = stepDistance * 0.45f;
        foreach (var seg in segments)
        {
            if (seg != null)
                Gizmos.DrawWireSphere(seg.position, r);
        }
    }
}
