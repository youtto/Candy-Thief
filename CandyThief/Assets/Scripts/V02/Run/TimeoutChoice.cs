using System.Collections.Generic;

public struct TimeoutCandidate
{
    public string Id;
    public bool IsOption;
    public bool CanExecute;
    public int SpawnWeight;
}

// 시간 초과 추첨. SpawnWeight는 카드 등장용이며 여기서는 쓰지 않는다.
public static class TimeoutChoice
{
    public static bool TryPick(IReadOnlyList<TimeoutCandidate> candidates, IIntRoll roll, out TimeoutCandidate picked)
    {
        picked = default(TimeoutCandidate);
        if (candidates == null || roll == null)
            return false;

        int count = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].CanExecute && !string.IsNullOrEmpty(candidates[i].Id))
                count++;
        }

        if (count == 0)
            return false;

        int ticket = roll.Next(count);
        if (ticket < 0)
            ticket = 0;
        if (ticket >= count)
            ticket = count - 1;

        int seen = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].CanExecute || string.IsNullOrEmpty(candidates[i].Id))
                continue;
            if (seen == ticket)
            {
                picked = candidates[i];
                return true;
            }

            seen++;
        }

        return false;
    }
}
