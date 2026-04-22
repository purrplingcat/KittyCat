namespace PurrplingCore.Toolkit;

public class FixedStepTimer
{
    private float _accumulator;

    public float TargetFixedTime { get; set; } = 1f / 50f;
    public float MaxDeltaTime { get; set; } = 0.25f;

    public FixedStepTimer() { }

    public FixedStepTimer(float targetFixedTime)
    {
        TargetFixedTime = targetFixedTime;
    }

    public FixedStepTimer(float targetFixedTime, float maxDeltaTime)
    {
        TargetFixedTime = targetFixedTime;
        MaxDeltaTime = maxDeltaTime;
    }


    public void Tick(float realDeltaTime, Action fixedUpdate)
    {
        if (realDeltaTime > MaxDeltaTime)
        {
            realDeltaTime = MaxDeltaTime;
        }

        _accumulator += realDeltaTime;

        while (_accumulator >= TargetFixedTime)
        {
            fixedUpdate(); 
            _accumulator -= TargetFixedTime;
        }
    }
}
