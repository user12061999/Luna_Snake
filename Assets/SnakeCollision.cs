using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeCollision : MonoBehaviour
{
    private SnakeController controller;
    private SnakeAudio audio;

    void Awake()
    {
        controller = GetComponent<SnakeController>();
        audio      = GetComponent<SnakeAudio>();
    }

    // =============== SPIKE ===================
    public bool CheckSpikeHit()
    {
        float checkDist = controller.stepDistance * 0.9f;

        LayerMask safeSupportMask = controller.groundMask | controller.wallMask | controller.finishMask | controller.appleMask;
        LayerMask combinedMask    = safeSupportMask | controller.spikeMask;

        bool hasAnySpikeSupport = false;
        bool hasAnySafeSupport  = false;

        for (int i = 0; i < controller.segments.Count; i++)
        {
            Transform seg = controller.segments[i];

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

                if ((controller.spikeMask & hitLayerMask) != 0)
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

    // Spike không phải là điểm tựa
    public bool ShouldFallOneUnit()
    {
        LayerMask supportMask = controller.groundMask | controller.wallMask | controller.finishMask | controller.appleMask;

        for (int i = 0; i < controller.segments.Count; i++)
        {
            if (Physics2D.Raycast(
                controller.segments[i].position,
                Vector2.down,
                controller.stepDistance * 0.9f,
                supportMask))
            {
                return false;
            }
        }
        return true;
    }

    // =============== FINISH GATE ===================
    public bool CheckFinish()
    {
        if (controller.isFinishing) return true;

        float radius = controller.stepDistance * 0.45f;
        Transform head = controller.segments[0];

        Collider2D hit = Physics2D.OverlapCircle(head.position, radius, controller.finishMask);
        if (hit != null)
        {
            StartFinishSequence(hit);
            return true;
        }
        return false;
    }

    void StartFinishSequence(Collider2D gate)
    {
        if (controller.isFinishing) return;
        controller.isFinishing = true;

        audio.PlayGateSFX();
        audio.StopMusic();

        Vector3 gatePos = gate.bounds.center;
        StartCoroutine(SnakeMoveIntoGate(gatePos));
    }

    IEnumerator SnakeMoveIntoGate(Vector3 gatePos)
    {
        controller.isBusy = true;

        while (controller.segments.Count > 0)
        {
            Vector3 dir = (gatePos - controller.segments[0].position);
            if (dir.magnitude < controller.stepDistance * 0.5f)
            {
                // HEAD đã vào cổng -> ẩn
                HideSegment(0);
                continue;
            }

            Vector3 stepDir = GetStepDirection(dir);

            // di chuyển như rắn
            yield return StartCoroutine(MoveOneStepTowards(stepDir));

            if ((controller.segments[0].position - gatePos).sqrMagnitude < 0.1f)
            {
                HideSegment(0);
            }
        }

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
        Transform seg = controller.segments[index];

        var rend = seg.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        var col = seg.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        controller.segments.RemoveAt(index);
        Destroy(seg.gameObject);
    }

    // phiên bản dùng gateSuckDurationPerSegment (không được dùng trong flow hiện tại,
    // nhưng mình giữ nguyên như script cũ)
    IEnumerator AbsorbIntoGate(Vector3 gatePos)
    {
        controller.isBusy = true;

        for (int i = 0; i < controller.segments.Count; i++)
        {
            Transform seg = controller.segments[i];
            if (seg == null) continue;

            Vector3 start = seg.position;
            float t = 0f;

            while (t < controller.gateSuckDurationPerSegment)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / controller.gateSuckDurationPerSegment);
                seg.position = Vector3.Lerp(start, gatePos, a);
                yield return null;
            }

            var rend = seg.GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
            var col = seg.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            yield return new WaitForSeconds(0.02f);
        }

        WinGame();
    }

    IEnumerator MoveOneStepTowards(Vector3 stepDir)
    {
        List<Vector3> startPos = new List<Vector3>(controller.segments.Count);
        for (int i = 0; i < controller.segments.Count; i++)
            startPos.Add(controller.segments[i].position);

        int count = startPos.Count;
        List<Vector3> target = new List<Vector3>(startPos);

        target[0] = startPos[0] + stepDir * controller.stepDistance;
        for (int i = 1; i < count; i++)
            target[i] = startPos[i - 1];

        float t = 0f;
        while (t < controller.moveDuration)
        {
            t += Time.deltaTime;
            float a = t / controller.moveDuration;

            for (int i = 0; i < count; i++)
                controller.segments[i].position = Vector3.Lerp(startPos[i], target[i], a);

            yield return null;
        }

        for (int i = 0; i < count; i++)
            controller.segments[i].position = target[i];
    }

    // =============== GAME OVER / WIN =================
    void GameOver()
    {
        if (controller.isDead || controller.isWin) return;
        controller.isDead = true;

        audio.PlayDieSFX();
        controller.headAnim?.PlayDie();
        LunaManager.ins.ShowEndCard(2f);
        audio.PlayLoseMusic();
        Debug.Log("GAME OVER (Spike)");
    }

    void WinGame()
    {
        if (controller.isWin || controller.isDead) return;
        controller.isWin = true;

        LunaManager.ins.ShowWinCard(2f);
        audio.PlayGateSFX();
        audio.StopMusic();
        audio.PlayWinMusic();
        Debug.Log("WIN! (GateFinish)");
    }
}
