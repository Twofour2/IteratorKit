﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IteratorKit.Util
{
    public static class ITKUtil
    {
        public static Vector2 GetWorldFromTile(Vector2 pos)
        {
            // does reverse of the Room GetTilePosition
            return new Vector2(((pos.x + 1) * 20) - 20, ((pos.y + 1) * 20) - 20);
        }
    }
    public class ITKMultiValueDictionary<Key, Value> : Dictionary<Key, List<Value>>
    {
        public void Add(Key key, Value value)
        {
            List<Value> values;
            if (!this.TryGetValue(key, out values))
            {
                values = new List<Value>();
                this.Add(key, values);
            }
            values.Add(value);
        }

        public List<Value> AllValues(){
            return this.Values.SelectMany(x => x).ToList();
        }
    }
}