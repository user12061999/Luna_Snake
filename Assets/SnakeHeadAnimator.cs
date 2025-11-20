using UnityEngine;
using Spine.Unity;

public class SnakeHeadAnimator : MonoBehaviour
{
    [Header("Spine")]
    public SkeletonAnimation skeletonAnim;
    public SkeletonDataAsset skeletonData;

    [Header("Animation Names")]
    [SpineAnimation("", "skeletonData")] public string idle;
    [SpineAnimation("", "skeletonData")] public string idleToEat;
    [SpineAnimation("", "skeletonData")] public string eat;
    [SpineAnimation("", "skeletonData")] public string die;
    [SpineAnimation("", "skeletonData")] public string stuck;

    private string currentAnim;

    public void Play(string animName, bool loop = true)
    {
        if (skeletonAnim == null || string.IsNullOrEmpty(animName)) return;
        if (currentAnim == animName) return;

        skeletonAnim.state.SetAnimation(0, animName, loop);
        currentAnim = animName;
    }

    public void PlayIdle() => Play(idle, true);
    public void PlayIdleToEat() => Play(idleToEat, false);
    public void PlayEat() => Play(eat, false);
    public void PlayDie() => Play(die, false);
    public void PlayStuck() => Play(stuck, false);
}