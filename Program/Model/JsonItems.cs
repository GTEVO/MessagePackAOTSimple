using System;
using System.Collections.Generic;
using System.Text;

namespace Model.JsonItem
{
    public struct StructItem
    {
        public int Level { get; set; }

        public int Exp { get; set; }

        public int DeltaExp { get; set; }
    }

    public class ClassItem
    {
        public int Level { get; set; }

        public int Exp { get; set; }

        public int DeltaExp { get; set; }
    }

    public class StructItemList
    {
        public List<StructItem> Items { get; set; }
    }

    public class StructItemsArray
    {
        public StructItem[] Items { get; set; }
    }

    public class ClassItemList
    {
        public List<ClassItem> Items { get; set; }
    }

    public class ClassItemsArray
    {
        public ClassItem[] Items { get; set; }
    }
}
