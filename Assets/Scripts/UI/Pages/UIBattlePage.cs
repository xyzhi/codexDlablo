using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Wuxing.Battle;
using Wuxing.Localization;

namespace Wuxing.UI
{
    public class UIBattlePage : UIPage
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button tipButton;
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text playerTeamText;
        [SerializeField] private Text enemyTeamText;
        [SerializeField] private Text battleLogText;

        public override void OnOpen(object data)
        {
            RefreshPreview();
        }

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnClickBack);
            }

            if (tipButton != null)
            {
                tipButton.onClick.AddListener(OnClickTip);
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnClickStartBattle);
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnClickBack);
            }

            if (tipButton != null)
            {
                tipButton.onClick.RemoveListener(OnClickTip);
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.RemoveListener(OnClickStartBattle);
            }
        }

        private void OnClickBack()
        {
            UIManager.Instance.ShowPage("MainMenu");
        }

        private void OnClickTip()
        {
            UIManager.Instance.ShowToastByKey("toast.battle_next");
        }

        private void OnClickStartBattle()
        {
            var result = BattleManager.RunSampleBattle();
            if (statusText != null)
            {
                statusText.text = result.IsVictory
                    ? LocalizationManager.GetText("battle.status_victory")
                    : LocalizationManager.GetText("battle.status_defeat");
            }

            if (playerTeamText != null)
            {
                playerTeamText.text = result.PlayerTeamSummary;
            }

            if (enemyTeamText != null)
            {
                enemyTeamText.text = result.EnemyTeamSummary;
            }

            if (battleLogText != null)
            {
                var builder = new StringBuilder();
                for (var i = 0; i < result.Logs.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(result.Logs[i]);
                }

                battleLogText.text = builder.ToString();
            }
        }

        private void RefreshPreview()
        {
            if (statusText != null)
            {
                statusText.text = LocalizationManager.GetText("battle.status_idle");
            }

            if (playerTeamText != null)
            {
                playerTeamText.text = LocalizationManager.GetText("battle.player_content");
            }

            if (enemyTeamText != null)
            {
                enemyTeamText.text = LocalizationManager.GetText("battle.enemy_content");
            }

            if (battleLogText != null)
            {
                battleLogText.text = LocalizationManager.GetText("battle.log_content");
            }
        }
    }
}
