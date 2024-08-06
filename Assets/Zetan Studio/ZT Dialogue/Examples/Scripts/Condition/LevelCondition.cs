using System;
using UnityEngine;

namespace ZetanStudio.Examples
{
    using ConditionSystem;

    #region 等级相关
    [Serializable]
    public abstract class LevelCondition : Condition
    {
        [field: SerializeField, Min(0)]
        public int Level { get; private set; } = 1;

        public override bool IsValid => Level >= 0;
    }
    [Serializable, Name("等级等于"), Group("等级相关")]
    public sealed class LevelEqualsTo : LevelCondition
    {
        public override bool IsMet()
        {
            return PlayerManager.player.level == Level;
        }
    }
    [Serializable, Name("等级大于"), Group("等级相关")]
    public sealed class LevelHigherThan : LevelCondition
    {
        public override bool IsMet()
        {
            return PlayerManager.player.level > Level;
        }
    }
    [Serializable, Name("等级小于"), Group("等级相关")]
    public sealed class LevelLowerThan : LevelCondition
    {
        public override bool IsMet()
        {
            return PlayerManager.player.level < Level;
        }
    }
    #endregion
}