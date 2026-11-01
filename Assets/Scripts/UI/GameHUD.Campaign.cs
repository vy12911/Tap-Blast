using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace TapBlast
{
    public sealed partial class GameHUD
    {
        [SerializeField] private TMP_Text waveStatus, pressureStatus, ultimateStatus, campaignResult;
        [SerializeField] private Button ultimateButton, practiceButton, iconModeButton;
        [SerializeField] private ArenaTapSurface tapSurface;
        [SerializeField] private Image ultimateFill;
        [SerializeField] private Image[] pressurePips = new Image[0];
        private bool iconMode;
        public void BindCampaign(Action ultimate, Action practice, Action empty)
        {
            if (ultimateButton != null) ultimateButton.onClick.AddListener(() => ultimate());
            if (practiceButton != null) practiceButton.onClick.AddListener(() => practice());
            if (tapSurface != null) tapSurface.Bind(empty);
            if (iconModeButton != null) iconModeButton.onClick.AddListener(() => { iconMode = !iconMode; ProgressStore.SaveIconMode(iconMode); UpdateIconSetting(); });
            iconMode = ProgressStore.IconMode; UpdateIconSetting();
        }
        private void UpdateIconSetting()
        {
            if (iconModeButton != null) iconModeButton.GetComponentInChildren<TMP_Text>().text = "Shape cues<pos=78%>" + (iconMode ? "On" : "Off");
            WorldEffects.SetIcons(iconMode);
        }
        private void RefreshCampaign(RoundSession session, LevelConfig config)
        {
            bool active = config.Scripted && session.State == RoundState.Playing;
            if (waveStatus == null) return;
            if (session.Practice) levelChip.text = "PRACTICE"; else if (levelChip.text == "PRACTICE") levelChip.text = "LEVEL " + config.LevelId.ToString("00");
            waveStatus.gameObject.SetActive(active); pressureStatus.gameObject.SetActive(active);
            ultimateButton.gameObject.SetActive(active && config.AllowUltimate);
            if (!active) return;
            var wave = session.Wave;
            waveStatus.text = (session.WaveIndex + 1) + " / " + session.WaveCount;
            pressureStatus.text = "PRESSURE " + session.Pressure + "/" + session.PressureLimit;
            for (int i = 0; i < pressurePips.Length; i++) { pressurePips[i].gameObject.SetActive(i < session.PressureLimit); pressurePips[i].color = i < session.Pressure ? UIArt.Warning : new Color32(196, 192, 232, 255); }
            pressureStatus.color = session.Pressure >= session.PressureLimit - 1 ? UIArt.Warning : UIArt.Ink;
            objective.text = wave.GoalLabel.ToUpperInvariant() + "  " + Mathf.Min(session.WaveProgress, wave.quota) + "/" + wave.quota + (wave.pairs > 0 ? "  PAIRS " + session.WavePairs + "/" + wave.pairs : "");
            if (wave.coreHits > 0) objective.text += "  CORE " + session.WaveCoreHits + "/" + wave.coreHits;
            if (wave.priorityOrder > 0) objective.text += "  ! " + session.WavePriorityOrder + "/" + wave.priorityOrder;
            float completed = Mathf.Min(session.WaveProgress, wave.quota) + Mathf.Min(session.WavePairs, wave.pairs) + Mathf.Min(session.WaveCoreHits, wave.coreHits) + Mathf.Min(session.WavePriorityOrder, wave.priorityOrder);
            progressTarget = Mathf.Clamp01(completed / Mathf.Max(1, wave.quota + wave.pairs + wave.coreHits + wave.priorityOrder));
            if (session.State != RoundState.Playing || reducedMotion) ShowProgress(progressTarget);
            ultimateStatus.text = session.UltimateCharge >= 100 ? "ULTIMATE READY" : "ULTIMATE " + session.UltimateCharge + "%";
            ultimateButton.interactable = session.UltimateCharge >= 100;
            ultimateFill.fillAmount = session.UltimateCharge / 100f;
            mechanicStatus.gameObject.SetActive(true);
            mechanicStatus.text = "DIRECT " + (session.Accuracy * 100).ToString("0") + "%   CHAINS " + session.Chains + "   PAIRS " + session.PairsCompleted;
        }
        private void CampaignResults(RoundSession session)
        {
            if (campaignResult == null) return;
            campaignResult.gameObject.SetActive(session.Scripted);
            int failures = ProgressStore.FailureCount(session.LevelId);
            practiceButton.gameObject.SetActive(session.Scripted && !session.Won && failures >= 3);

            if (!session.Scripted) return;
            StatTile(0, Mathf.RoundToInt(session.Accuracy * 100), "Direct accuracy %\nGates " + session.ClearedWaves + "/" + session.WaveCount, "3B5BDB");
            StatTile(1, session.BestCombo, "Best combo\nChains " + session.Chains + " · Pairs " + session.PairsCompleted, "D9600F");
            StatTile(2, session.Pressure, "Pressure / " + session.PressureLimit + "\nCritical " + session.CriticalMisses + " · Bombs " + session.BombHits, session.Pressure == 0 ? "16834A" : "C2264F");
            campaignResult.text = "Perfects 0 · Gems +0";
            resultSubtitle.text = session.Practice ? "Practice finished · no rewards or stars" : session.Won ? "Every mandatory gate cleared" : session.FailureReason;

            string tip = session.LevelId == 8 ? "Tip: tap ! first. Optional circles can wait." : session.LevelId >= 14 ? "Tip: finish each numbered pair 1 then 2; leave Bombs alone." : "Tip: finish the displayed wave quota before its time ends.";
            if (!session.Won && failures >= 2) campaignResult.text += "\n" + tip;
            else if (session.Won) campaignResult.text += "\nNext: " + (session.LevelId < 20 ? "a new wave skill" : "replay for mastery");
            else campaignResult.text += "\nReplay to retry this wave skill.";
        }
    }
}
