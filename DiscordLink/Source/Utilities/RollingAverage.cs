using System;

namespace DiscordLink.Source.Utilities
{
    public class RollingAverage
    {
        public double Average { get; private set; } = 0;

        private int[] _values;
        private int _period;
        private int _index = 0;
        private int _sum = 0;
        private int _addCount = 0;

        public RollingAverage(int period)
        {
            _period = period;
            _values = new int[period];
        }

        public void Add(int newValue)
        {
            ++_addCount;
            _sum = _sum - _values[_index] + newValue;
            _values[_index] = newValue;
            _index = (_index + 1) % _period;
            Average = ((double)_sum) / (_period - Math.Max(_period - _addCount, 0));
        }
    }
}
