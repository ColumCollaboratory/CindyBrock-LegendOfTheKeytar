using UnityEngine;
using BattleRoyalRhythm.Input;
using BattleRoyalRhythm.UI;

namespace BattleRoyalRhythm.Audio
{
    [CreateAssetMenu(fileName = "NewMusicSheet", menuName = "Music Sheet")]
    public sealed class BossActionSheet : ScriptableObject
    {
        [SerializeField] public PlayerAction[] notes = null;

        public PlayerAction Next => throw new System.NotImplementedException();
    }
}
