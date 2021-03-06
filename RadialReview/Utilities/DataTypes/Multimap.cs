﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
    public class Multimap<K, V> : IEnumerable<KeyValuePair<K, List<V>>>
    {
        private Dictionary<K, List<V>> Map { get; set; }

        public Multimap<K, V2> ConvertValue<V2>(Func<V, V2> valueConvert)
        {
            return new Multimap<K, V2>() {
                Map = Map.ToDictionary(x => x.Key, x => x.Value.Select(y => valueConvert(y)).ToList())
            };
        }
        public Multimap<K2, V> ConvertKey<K2>(Func<K, K2> keyConvert)
        {
            return new Multimap<K2, V>() {
                Map = Map.ToDictionary(x => keyConvert(x.Key), x=>x.Value)
            };
        }

        public Multimap()
        {
            Map = new Dictionary<K, List<V>>();
        }

        public void Add(K key, V value)
        {
            if(!Map.ContainsKey(key))
                Map[key]=new List<V>();
            Map[key].Add(value);
        }

        public void AddNTimes(K key, V value, int count)
        {
            for (int i = 0; i < count; i++)
                Add(key, value);
        }

        public List<V> Get(K key)
        {
            if (!Map.ContainsKey(key))
                Map[key] = new List<V>();
            return Map[key];
        }

        public IEnumerable<K> AllKeys()
        {
            return Map.Keys.Select(x => x);
        }

        public IEnumerator<KeyValuePair<K, List<V>>> GetEnumerator()
        {
            return Map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Map.GetEnumerator();
        }
    }
}