using System;
using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Represents a range along a linear number line
    /// of whole integers.
    /// </summary>
    [Serializable]
    public struct Bounds1DInt
    {
        #region Inspector Fields
        [Tooltip("The left limit of the range (inclusive).")]
        [SerializeField] private int min;
        [Tooltip("The right limit of the range (inclusive).")]
        [SerializeField] private int max;
        #endregion
        #region Constructors
        /// <summary>
        /// Creates a new bounds with the given limits (inclusive).
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public Bounds1DInt(int min, int max)
        {
            // TODO maybe add validation/exception here?
            this.min = min;
            this.max = max;
        }
        #endregion
        #region Properties
        /// <summary>
        /// The minimum boundary of the range (inclusive).
        /// </summary>
        public int Min
        {
            get => min;
            set
            {
                // TODO haven't decided how range error
                // checking should be handled on set;
                // not used in this game (yet).
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// The maximum boundary of the range (inclusive).
        /// </summary>
        public int Max
        {
            get => max;
            set
            {
                // TODO haven't decided how range error
                // checking should be handled on set;
                // not used in this game (yet).
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
