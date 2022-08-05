using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Inu.Cate
{
    public class Anchor
    {
        private static int nextId = 0;

        public readonly Function Function;
        public readonly ISet<int> OriginAddresses = new HashSet<int>();
        private readonly int id;
        private int? address;

        public Anchor(Function function)
        {
            Function = function;
            id = ++nextId;
        }

        public override string ToString()
        {
            return Label;
        }

        public string Label => Function.Name + Compiler.Instance.LabelPrefix + "Anchor" + id;

        public void AddOriginAddress(int originAddress)
        {
            OriginAddresses.Add(originAddress);
        }

        public int? Address {
            get => address;
            set {
                Debug.Assert(address == null);
                address = value;
            }
        }
    }
}
