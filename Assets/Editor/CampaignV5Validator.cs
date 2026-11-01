using System.Collections.Generic;
using TapBlast;
using UnityEditor.Build;
public static class CampaignV5Validator
{
    public static void Validate(LevelConfig level)
    {
        CampaignV3Validator.Validate(level);
        if (level.DesignVersion < 5) throw new BuildFailedException("Expected v5 level " + level.LevelId);
        var ids = new HashSet<string>();
        foreach (var wave in level.Waves)
        {
            float previous = -8;
            foreach (var e in wave.encounters)
            {
                if (string.IsNullOrEmpty(e.id) || !ids.Add(e.id) || e.at - previous < 8 || e.warning <= 0 || e.active <= 0 || e.at + e.warning + e.active > wave.rules.duration + .01f)
                    throw new BuildFailedException("Invalid/overlapping v5 encounter in " + level.LevelId);
                previous = e.at;
            }
            foreach (var cue in wave.schedule)
            {
                if (string.IsNullOrEmpty(cue.id) || !ids.Add(cue.id) || cue.required && string.IsNullOrEmpty(cue.gateId)) throw new BuildFailedException("Missing stable cue/gate ID");
                if (cue.moving && (cue.telegraph < .45f || cue.transit < .22f || cue.cadence < 1 || cue.cadence > 1.61f)) throw new BuildFailedException("Invalid lane timing");
            }
        }
        if (level.LevelId % 10 == 0)
        {
            if (!level.V5Boss) throw new BuildFailedException("Boss needs exactly five phase assets");
            for (int p = 0; p < 5; p++)
            {
                var phase = level.BossPhases[p];
                if (phase == null || phase.coreHits != level.Waves[p].rules.coreHits || phase.coreHits < 1) throw new BuildFailedException("Invalid boss core gate");
            }
        }
    }
}
