using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeMovement : MonoBehaviour
{
    private SnakeController controller;
    private SnakeCollision collision;
    private SnakeAudio audio;
    private SnakeVisuals visuals;

    void Awake()
    {
        controller = GetComponent<SnakeController>();
        collision  = GetComponent<SnakeCollision>();
        audio      = GetComponent<SnakeAudio>();
        visuals    = GetComponent<SnakeVisuals>();
    }

    // ================= START MOVE ===================
    public void TryStartMove(Vector2 dir)
    {
        if (controller.isBusy || controller.isDead || controller.isWin || controller.isFinishing) return;
        if (dir == Vector2.zero) return;

        controller.moveDir = dir.normalized;
        audio.StartMusicIfNeeded();
        StartCoroutine(PerformStepWithGravity());
    }

    // ================= STEP + GRAVITY ================
    IEnumerator PerformStepWithGravity()
    {
        controller.isBusy = true;

        yield return StartCoroutine(MoveOneStepAnimated());

        if (!controller.moveSucceeded || controller.isDead || controller.isWin)
        {
            controller.isBusy = false;
            yield break;
        }

        yield return StartCoroutine(ApplyGravityAnimated());

        controller.isBusy = false;
    }

    // ================= ONE STEP + EAT/GROW ===========
    // ✅ SnakeMovement.cs (đã sửa để đẩy rock và đợi rock di chuyển xong)
IEnumerator MoveOneStepAnimated()
{
    controller.moveSucceeded = false;

    Vector3 dir3 = new Vector3(controller.moveDir.x, controller.moveDir.y, 0f);
    LayerMask blockMask = controller.wallMask | controller.groundMask | controller.segmentMask | controller.spikeMask;

    // kiểm tra va chạm phía trước, bỏ qua chính đầu rắn
    bool blocked = false;
    RaycastHit2D[] blockHits = Physics2D.RaycastAll(controller.segments[0].position, dir3, controller.stepDistance, blockMask);
    blocked = false;
    bool hasRockToPush = false;
    PushableRock rockToPush = null;

    foreach (var hit in blockHits)
    {
        if (hit.collider != null && hit.collider.transform != controller.segments[0])
        {
            // Nếu là Rock thì thử đẩy
            if (hit.collider.CompareTag("Rock"))
            {
                rockToPush = hit.collider.GetComponent<PushableRock>();
                hasRockToPush = true;
            }
            else
            {
                blocked = true;
                break;
            }
        }
    }

// Nếu bị chặn bởi vật khác ngoài Rock → không di chuyển
    if (blocked)
    {
        BlockMove();
        yield break;
    }

// Nếu có Rock thì thử đẩy
    if (hasRockToPush && rockToPush != null)
    {
        Vector3 oldRockPos = rockToPush.transform.position;

        yield return StartCoroutine(rockToPush.TryPush(controller.moveDir));

        // Đợi 1 frame để Unity update collider (an toàn)
        yield return null;

        Vector3 newRockPos = rockToPush.transform.position;

        // Nếu rock không di chuyển, coi như đẩy thất bại
        if (Vector3.Distance(oldRockPos, newRockPos) < 0.01f)
        {
            BlockMove();
            yield break;
        }

        // Nếu rock đã đẩy thành công → tiếp tục move rắn
    }
    
    audio.PlayMove();

    // kiểm tra táo để grow
    Collider2D appleToEat = null;
    RaycastHit2D[] hits = Physics2D.RaycastAll(controller.segments[0].position, dir3, controller.stepDistance);
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
    targetPos[0] = startPos[0] + dir3 * controller.stepDistance;
    for (int i = 1; i < oldCount; i++)
        targetPos[i] = startPos[i - 1];

    float t = 0f;
    while (t < controller.moveDuration)
    {
        t += Time.deltaTime;
        float a = Mathf.Clamp01(t / controller.moveDuration);
        for (int i = 0; i < controller.segments.Count; i++)
            controller.segments[i].position = Vector3.Lerp(startPos[i], targetPos[i], a);

        visuals.UpdateSegmentRotations();
        yield return null;
    }

    for (int i = 0; i < controller.segments.Count; i++)
        controller.segments[i].position = targetPos[i];

    if (collision.CheckSpikeHit() || collision.CheckFinish())
    {
        controller.moveSucceeded = false;
        yield break;
    }

    if (!willGrow)
    {
        controller.moveSucceeded = true;
        visuals.UpdateSegmentRotations();
        controller.headAnim?.PlayIdle();
        yield break;
    }

    // có ăn táo
    audio.PlayEat();
    controller.headAnim?.PlayEat();
    Destroy(appleToEat.gameObject);

    List<Vector3> newPositions = new List<Vector3>(oldCount + 1);
    newPositions.Add(startPos[0] + dir3 * controller.stepDistance);
    newPositions.Add(startPos[0]);
    for (int i = 1; i < oldCount; i++)
        newPositions.Add(startPos[i]);

    Transform newBody = Instantiate(controller.bodyPrefab, newPositions[1], Quaternion.identity, controller.segmentsParent);
    controller.segments.Insert(1, newBody);

    for (int i = 0; i < controller.segments.Count; i++)
        controller.segments[i].position = newPositions[i];

    if (collision.CheckSpikeHit() || collision.CheckFinish())
    {
        controller.moveSucceeded = false;
        yield break;
    }

    controller.moveSucceeded = true;
    visuals.UpdateSegmentRotations();
    controller.headAnim?.PlayIdle();
}

void BlockMove()
{
    audio.PlayCantMove();
    controller.headAnim?.PlayStuck();
}



    // ================= GRAVITY =======================
    IEnumerator ApplyGravityAnimated()
    {
        while (collision.ShouldFallOneUnit())
        {
            audio.PlayFall();

            List<Vector3> startPos = GetPositions();
            List<Vector3> targetPos = new List<Vector3>(startPos.Count);
            for (int i = 0; i < startPos.Count; i++)
                targetPos.Add(startPos[i] + Vector3.down * controller.stepDistance);

            float t = 0f;
            while (t < controller.fallDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / controller.fallDuration);

                for (int i = 0; i < controller.segments.Count; i++)
                    controller.segments[i].position = Vector3.Lerp(startPos[i], targetPos[i], a);

                visuals.UpdateSegmentRotations();
                yield return null;
            }

            for (int i = 0; i < controller.segments.Count; i++)
                controller.segments[i].position = targetPos[i];

            if (collision.CheckSpikeHit() || collision.CheckFinish())
                yield break;
        }
    }

    // helper giống GetPositions cũ
    List<Vector3> GetPositions()
    {
        List<Vector3> result = new List<Vector3>(controller.segments.Count);
        for (int i = 0; i < controller.segments.Count; i++)
            result.Add(controller.segments[i].position);
        return result;
    }
}
