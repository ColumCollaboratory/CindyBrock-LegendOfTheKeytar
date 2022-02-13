using UnityEngine;
using CindyBrock.Input;
using CindyBrock.UI;

namespace CindyBrock.Audio
{
    [CreateAssetMenu(fileName = "NewMusicSheet", menuName = "Music Sheet")]
    public sealed class BossActionSheet : ScriptableObject
    {
        [SerializeField] public PlayerAction[] notes = null;

        public PlayerAction Next => throw new System.NotImplementedException();
    }
}
