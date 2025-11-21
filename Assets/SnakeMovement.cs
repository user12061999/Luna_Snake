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
    IEnumerator MoveOneStepAnimated()
    {
        controller.moveSucceeded = false;

        Vector3 dir3 = new Vector3(controller.moveDir.x, controller.moveDir.y, 0f);
        LayerMask blockMask = controller.wallMask | controller.groundMask | controller.segmentMask | controller.spikeMask;

        // chặn bước nếu phía trước là block (tường / ground / segment / spike)
        if (Physics2D.Raycast(controller.segments[0].position, dir3, controller.stepDistance, blockMask))
        {
            audio.PlayCantMove();
            controller.headAnim?.PlayStuck();
            yield break;
        }

        audio.PlayMove();

        // check Apple để biết có grow không
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

        // lưu vị trí ban đầu
        List<Vector3> startPos = GetPositions();
        int oldCount = startPos.Count;
        List<Vector3> targetPos = new List<Vector3>(startPos);

        // target: head tiến 1 ô, các segment follow
        targetPos[0] = startPos[0] + dir3 * controller.stepDistance;
        for (int i = 1; i < oldCount; i++)
            targetPos[i] = startPos[i - 1];

        // lerp di chuyển
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

        // snap
        for (int i = 0; i < controller.segments.Count; i++)
            controller.segments[i].position = targetPos[i];

        // check Spike / Finish
        if (collision.CheckSpikeHit() || collision.CheckFinish())
        {
            controller.moveSucceeded = false;
            yield break;
        }

        // không grow
        if (!willGrow)
        {
            controller.moveSucceeded = true;
            visuals.UpdateSegmentRotations();
            controller.headAnim?.PlayIdle();
            yield break;
        }

        // có grow
        audio.PlayEat();
        controller.headAnim?.PlayEat();
        Destroy(appleToEat.gameObject);

        // tính lại newPositions sau khi grow
        List<Vector3> newPositions = new List<Vector3>(oldCount + 1);
        newPositions.Add(startPos[0] + dir3 * controller.stepDistance); // head mới
        newPositions.Add(startPos[0]);                                  // body mới sau head
        for (int i = 1; i < oldCount; i++)
            newPositions.Add(startPos[i]);

        // spawn body mới
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
