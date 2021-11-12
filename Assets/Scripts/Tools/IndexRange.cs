namespace Tools
{
    #region Delegates
    /// <summary>
    /// An action to be performed at each index i.
    /// </summary>
    /// <param name="i">The current index.</param>
    public delegate void ForAction(int i);
    /// <summary>
    /// An action to be performed on each element
    /// in this range.
    /// </summary>
    /// <typeparam name="T">The type of element.</typeparam>
    /// <param name="element">the current collection element.</param>
    public delegate void ForEachAction<T>(T element);
    #endregion

    // This class exists as a helper for:
    // 1) Simplifying code that iterates and loops indices.
    // 2) Making multiple arrays contiguous (optimizing cache hits).
    // TODO needs to be profiled whether delegates actually
    // make this overall less efficient.
    /// <summary>
    /// Stores a value that is automatically wrapped
    /// into a given range.
    /// </summary>
    public struct IndexRange
    {
        #region Fields
        private int localValue, length, min;
        #endregion
        #region Constructors
        /// <summary>
        /// Creates an index range using the given
        /// range (inclusive min and max).
        /// </summary>
        /// <param name="from">The minimum value of the index.</param>
        /// <param name="to">The maximum value of the index.</param>
        public IndexRange(int from, int to)
        {
            // Allow reversed range arguments.
            if (from < to)
            {
                min = from;
                length = to - from;
            }
            else
            {
                min = to;
                length = from - to;
            }
            localValue = 0;
        }
        #endregion
        #region Properties
        /// <summary>
        /// The minimum valid index.
        /// </summary>
        public int Min => min;
        /// <summary>
        /// The maximum valid index.
        /// </summary>
        public int Max => min + length;
        /// <summary>
        /// The current value of the index.
        /// </summary>
        public int Value
        {
            get => localValue + min;
            set => LocalValue = value - min;
        }
        /// <summary>
        /// The value relative to the start (Min) of the range.
        /// </summary>
        public int LocalValue
        {
            get => localValue;
            set
            {
                // TODO not sure whether this
                // is efficient. Maybe % is faster?
                // Research w/ godbolt.
                while (value > length)
                    value -= length;
                while (value < 0)
                    value += length;
                localValue = value;
            }
        }
        #endregion
        #region Utility Methods
        // These methods exist to abstract the localization
        // of the range away from other sections of code. This
        // could arguably make code less legible.
        /// <summary>
        /// Runs a for loop along the range of valid indices.
        /// </summary>
        /// <param name="action">The action at each index.</param>
        public void For(ForAction action)
        {
            for (int i = min, length = Max; i <= length; i++)
                action(i);
        }
        /// <summary>
        /// Runs a foreach loop along the range of valid indices on a collection.
        /// </summary>
        /// <typeparam name="T">The type in the collection.</typeparam>
        /// <param name="array">The array to iterate over.</param>
        /// <param name="action">The action to perform.</param>
        public void ForEach<T>(T[] array, ForEachAction<T> action)
        {
            for (int i = min, length = Max; i <= length; i++)
                action(array[i]);
        }
        #endregion
        #region Operator Overloads
        // Enables wrapping for arithmetic operators.
        // TODO could implement +=, -=, /=, *=... etc.
        public static IndexRange operator ++(IndexRange index)
        {
            index.localValue++;
            if (index.localValue > index.length)
                index.localValue = 0;
            return index;
        }
        public static IndexRange operator --(IndexRange index)
        {
            index.localValue--;
            if (index.localValue < 0)
                index.localValue = index.length;
            return index;
        }
        // Lets you use index range like an int.
        public static implicit operator int(IndexRange index) => index.Value;
        #endregion
    }
}
