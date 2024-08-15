namespace ServiceBusUtils
{
    internal class InterlockedInt32<T>(T initialValue) where T: struct, IConvertible
    {
        private int _value = Convert.ToInt32(initialValue);

        /// <summary>
        /// TODO: replace int on T
        /// </summary>
        public int Value => Interlocked.CompareExchange(ref _value, 0, 0);

        // methods
        public bool TryChange(T toValue)
        {
            var converted = Convert.ToInt32(toValue);
            return Interlocked.Exchange(ref _value, converted) < converted;
        }

        public bool TryChange(T fromValue, T toValue)
        {
            var convertedFrom = Convert.ToInt32(fromValue);
            var convertedTo = Convert.ToInt32(toValue);
            if (convertedFrom == convertedTo)
            {
                throw new ArgumentException("fromValue and toValue must be different.");
            }
            return Interlocked.CompareExchange(ref _value, convertedTo, convertedFrom) == convertedFrom;
        }
    }
}
