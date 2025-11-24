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
        collision = GetComponent<SnakeCollision>();
        audio = GetComponent<SnakeAudio>();
        visuals = GetComponent<SnakeVisuals>();
    }

    public void TryStartMove(Vector2 dir)
    {
        if (controller.isBusy || controller.isDead || controller.isWin || controller.isFinishing) return;
        if (dir == Vector2.zero) return;

        controller.isBusy = true;
        controller.moveDir = dir.normalized;
        audio.StartMusicIfNeeded();
        StartCoroutine(PerformStepWithGravity());
    }

    IEnumerator PerformStepWithGravity()
    {
        yield return StartCoroutine(MoveOneStepAnimated());

        // Sau khi di chuyển xong, nếu rắn vẫn sống → áp dụng trọng lực
        if (!controller.moveSucceeded || controller.isDead || controller.isWin || controller.isFinishing)
        {
            controller.isBusy = false;
            yield break;
        }

        yield return StartCoroutine(ApplyGravityAnimated());
        controller.isBusy = false;
    }

    IEnumerator MoveOneStepAnimated()
    {
        controller.moveSucceeded = false;
        Vector3 dir3 = new Vector3(controller.moveDir.x, controller.moveDir.y, 0f);

        // === PHASE 1: Kiểm tra vật cản phía trước ===
        LayerMask blockMask = controller.wallMask | controller.groundMask | controller.segmentMask;
        RaycastHit2D[] hits = Physics2D.RaycastAll(controller.segments[0].position, dir3, controller.stepDistance, blockMask);

        PushableRock rockToPush = null;
        bool blocked = false;

        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == controller.segments[0]) continue;

            if (hit.collider.CompareTag("Rock"))
            {
                // Ghi nhận rock (nhưng KHÔNG coi là blocked ngay)
                rockToPush = hit.collider.GetComponent<PushableRock>();
            }
            else
            {
                // Gặp tường, ground, hoặc segment → bị chặn
                blocked = true;
                break;
            }
        }

        if (blocked)
        {
            BlockMove();
            yield break;
        }

        // === PHASE 2: Đẩy rock (nếu có) ===
        if (rockToPush != null)
        {
            bool pushSuccess = rockToPush.TryPush(controller.moveDir);
            if (!pushSuccess)
            {
                BlockMove();
                yield break;
            }
        }

        // === PHASE 3: Di chuyển rắn ===
        controller.inputCooldown = 0.1f;
        audio.PlayMove();

        // Kiểm tra có táo không
        Collider2D appleToEat = null;
        RaycastHit2D[] appleHits = Physics2D.RaycastAll(controller.segments[0].position, dir3, controller.stepDistance, controller.appleMask);
        foreach (var hit in appleHits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Apple"))
            {
                appleToEat = hit.collider;
                break;
            }
        }

        bool willGrow = (appleToEat != null);

        // Lưu vị trí ban đầu
        List<Vector3> startPos = GetPositions();
        int oldCount = startPos.Count;

        // Tính vị trí đích sau khi di chuyển
        List<Vector3> targetPos = new List<Vector3>(startPos);
        targetPos[0] = startPos[0] + dir3 * controller.stepDistance;
        for (int i = 1; i < oldCount; i++)
            targetPos[i] = startPos[i - 1];

        // Hoạt ảnh di chuyển
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

        // Đặt chính xác vị trí cuối
        for (int i = 0; i < controller.segments.Count; i++)
            controller.segments[i].position = targetPos[i];

        yield return null; // Frame để collider cập nhật

        // Kiểm tra va chạm sau khi di chuyển
        if (collision.CheckSpikeHit() || collision.CheckFinish())
        {
            controller.moveSucceeded = false;
            yield break;
        }

        // === PHASE 4: Ăn táo & mọc thêm ===
        if (willGrow)
        {
            audio.PlayEat();
            controller.headAnim?.PlayEat();
            Destroy(appleToEat.gameObject);

            // Thêm segment mới ngay sau đầu
            Vector3 newBodyPos = startPos[0];
            Transform newBody = Instantiate(controller.bodyPrefab, newBodyPos, Quaternion.identity, controller.segmentsParent);
            controller.segments.Insert(1, newBody);

            // Cập nhật lại vị trí tất cả segment (đã có đoạn mới)
            List<Vector3> finalPositions = new List<Vector3>(oldCount + 1);
            finalPositions.Add(targetPos[0]); // đầu
            finalPositions.Add(startPos[0]);  // body mới
            for (int i = 1; i < oldCount; i++)
                finalPositions.Add(startPos[i]);

            for (int i = 0; i < controller.segments.Count; i++)
                controller.segments[i].position = finalPositions[i];

            yield return null;
            if (collision.CheckSpikeHit() || collision.CheckFinish())
            {
                controller.moveSucceeded = false;
                yield break;
            }
        }

        controller.moveSucceeded = true;
        visuals.UpdateSegmentRotations();
        controller.headAnim?.PlayIdle();
    }

    void BlockMove()
    {
        audio.PlayCantMove();
        controller.headAnim?.PlayStuck();
        controller.isBusy = false; // Đảm bảo không bị kẹt busy
    }

    IEnumerator ApplyGravityAnimated()
    {
        while (collision.ShouldFallOneUnit())
        {
            audio.PlayFall();

            List<Vector3> startPos = GetPositions();
            List<Vector3> targetPos = new List<Vector3>();
            foreach (var pos in startPos)
                targetPos.Add(pos + Vector3.down * controller.stepDistance);

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

            yield return null; // Frame cập nhật collider

            if (collision.CheckSpikeHit() || collision.CheckFinish())
                yield break;
        }
    }

    List<Vector3> GetPositions()
    {
        List<Vector3> result = new List<Vector3>(controller.segments.Count);
        for (int i = 0; i < controller.segments.Count; i++)
            result.Add(controller.segments[i].position);
        return result;
    }
}