using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch
{
    public class RandomList<T>
    {
        private T[] _items;
        private int _index;
        private Random _random;

        public RandomList(T[] items)
        {
            _random = new Random();
            _items = new T[items.Length];
            Array.Copy(items, _items, items.Length);
            _items.Shuffle(_random);
        }

        public T Next()
        {
            var item = _items[_index];
            if (_index++ >= (_items.Length - 1))
            {
                _index = 0;
                _items.Shuffle(_random);
            }

            return item;
        }
    }
}
