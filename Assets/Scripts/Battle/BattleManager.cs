using System.Collections.Generic;
using System.Text;
using Wuxing.Config;

namespace Wuxing.Battle
{
    public static class BattleManager
    {
        private static readonly string[] DefaultPlayerIds = { "C001", "C002" };
        private static readonly string[] DefaultEnemyIds = { "E001", "E004" };

        public static BattleResult RunSampleBattle()
        {
            var characterDatabase = CharacterDatabaseLoader.Load();
            var enemyDatabase = EnemyDatabaseLoader.Load();
            var skillDatabase = SkillDatabaseLoader.Load();

            if (characterDatabase == null || enemyDatabase == null || skillDatabase == null)
            {
                return new BattleResult
                {
                    IsVictory = false,
                    WinnerSide = "None",
                    TotalRounds = 0,
                    PlayerTeamSummary = "No data",
                    EnemyTeamSummary = "No data",
                    Logs = new List<string> { "Battle config missing. Import all CSV files first." }
                };
            }

            var playerTeam = BuildPlayerTeam(characterDatabase);
            var enemyTeam = BuildEnemyTeam(enemyDatabase);
            var result = Simulate(playerTeam, enemyTeam, skillDatabase);
            result.PlayerTeamSummary = FormatTeam(playerTeam);
            result.EnemyTeamSummary = FormatTeam(enemyTeam);
            return result;
        }

        public static string FormatTeam(BattleTeamRuntime team)
        {
            if (team == null || team.Units.Count == 0)
            {
                return "No units";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < team.Units.Count; i++)
            {
                var unit = team.Units[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(unit.Name)
                    .Append("  HP ")
                    .Append(unit.CurrentHP)
                    .Append('/')
                    .Append(unit.MaxHP)
                    .Append("  MP ")
                    .Append(unit.CurrentMP)
                    .Append('/')
                    .Append(unit.MaxMP);
            }

            return builder.ToString();
        }

        private static BattleTeamRuntime BuildPlayerTeam(CharacterDatabase database)
        {
            var team = new BattleTeamRuntime();
            for (var i = 0; i < DefaultPlayerIds.Length; i++)
            {
                var config = database.GetById(DefaultPlayerIds[i]);
                if (config != null)
                {
                    team.Units.Add(BattleUnitRuntime.FromCharacter(config));
                }
            }

            return team;
        }

        private static BattleTeamRuntime BuildEnemyTeam(EnemyDatabase database)
        {
            var team = new BattleTeamRuntime();
            for (var i = 0; i < DefaultEnemyIds.Length; i++)
            {
                var config = database.GetById(DefaultEnemyIds[i]);
                if (config != null)
                {
                    team.Units.Add(BattleUnitRuntime.FromEnemy(config));
                }
            }

            return team;
        }

        private static BattleResult Simulate(BattleTeamRuntime playerTeam, BattleTeamRuntime enemyTeam, SkillDatabase skillDatabase)
        {
            var result = new BattleResult();
            const int maxRounds = 20;

            for (var round = 1; round <= maxRounds; round++)
            {
                result.TotalRounds = round;
                result.Logs.Add("Round " + round);

                ResolveTeamTurn(playerTeam, enemyTeam, skillDatabase, result.Logs);
                if (enemyTeam.IsAllDead)
                {
                    result.IsVictory = true;
                    result.WinnerSide = "Player";
                    result.Logs.Add("Enemy team defeated.");
                    return result;
                }

                ResolveTeamTurn(enemyTeam, playerTeam, skillDatabase, result.Logs);
                if (playerTeam.IsAllDead)
                {
                    result.IsVictory = false;
                    result.WinnerSide = "Enemy";
                    result.Logs.Add("Player team defeated.");
                    return result;
                }
            }

            result.IsVictory = !playerTeam.IsAllDead;
            result.WinnerSide = result.IsVictory ? "Player" : "Enemy";
            result.Logs.Add("Battle reached round limit.");
            return result;
        }

        private static void ResolveTeamTurn(BattleTeamRuntime actingTeam, BattleTeamRuntime targetTeam, SkillDatabase skillDatabase, List<string> logs)
        {
            for (var i = 0; i < actingTeam.Units.Count; i++)
            {
                var actor = actingTeam.Units[i];
                if (actor.IsDead)
                {
                    continue;
                }

                var target = targetTeam.GetFirstAlive();
                if (target == null)
                {
                    return;
                }

                var skill = GetFirstCastableSkill(actor, skillDatabase);
                if (skill != null)
                {
                    ResolveSkill(actor, actingTeam, targetTeam, target, skill, logs);
                }
                else
                {
                    ResolveBasicAttack(actor, target, logs);
                }
            }
        }

        private static SkillConfig GetFirstCastableSkill(BattleUnitRuntime actor, SkillDatabase skillDatabase)
        {
            for (var i = 0; i < actor.SkillIds.Count; i++)
            {
                var skill = skillDatabase.GetById(actor.SkillIds[i]);
                if (skill != null && actor.CurrentMP >= skill.MPCost && skill.Category != "被动")
                {
                    return skill;
                }
            }

            return null;
        }

        private static void ResolveSkill(
            BattleUnitRuntime actor,
            BattleTeamRuntime actingTeam,
            BattleTeamRuntime targetTeam,
            BattleUnitRuntime defaultTarget,
            SkillConfig skill,
            List<string> logs)
        {
            actor.CurrentMP -= skill.MPCost;

            if (skill.EffectType == "Heal")
            {
                var ally = actingTeam.GetFirstAlive();
                if (ally == null)
                {
                    return;
                }

                var healValue = skill.Power;
                ally.CurrentHP = Clamp(ally.CurrentHP + healValue, 0, ally.MaxHP);
                logs.Add(actor.Name + " casts " + skill.Name + " and heals " + ally.Name + " for " + healValue + ".");
                return;
            }

            if (skill.TargetType == "AllEnemies")
            {
                logs.Add(actor.Name + " casts " + skill.Name + ".");
                for (var i = 0; i < targetTeam.Units.Count; i++)
                {
                    var target = targetTeam.Units[i];
                    if (target.IsDead)
                    {
                        continue;
                    }

                    var damage = CalculateSkillDamage(actor, target, skill);
                    target.CurrentHP = Clamp(target.CurrentHP - damage, 0, target.MaxHP);
                    logs.Add("  " + target.Name + " takes " + damage + " damage.");
                }

                return;
            }

            if (skill.EffectType == "Shield")
            {
                var shieldGain = skill.Power;
                actor.CurrentHP = Clamp(actor.CurrentHP + shieldGain, 0, actor.MaxHP);
                logs.Add(actor.Name + " casts " + skill.Name + " and steadies for " + shieldGain + ".");
                return;
            }

            var singleDamage = CalculateSkillDamage(actor, defaultTarget, skill);
            defaultTarget.CurrentHP = Clamp(defaultTarget.CurrentHP - singleDamage, 0, defaultTarget.MaxHP);
            logs.Add(actor.Name + " casts " + skill.Name + " on " + defaultTarget.Name + " for " + singleDamage + " damage.");
        }

        private static void ResolveBasicAttack(BattleUnitRuntime actor, BattleUnitRuntime target, List<string> logs)
        {
            var damage = Clamp(actor.ATK - target.DEF, 1, 9999);
            target.CurrentHP = Clamp(target.CurrentHP - damage, 0, target.MaxHP);
            actor.CurrentMP = Clamp(actor.CurrentMP + 5, 0, actor.MaxMP);
            logs.Add(actor.Name + " attacks " + target.Name + " for " + damage + " damage.");
        }

        private static int CalculateSkillDamage(BattleUnitRuntime actor, BattleUnitRuntime target, SkillConfig skill)
        {
            var basePower = actor.ATK + skill.Power;
            return Clamp(basePower - target.DEF, 1, 9999);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
