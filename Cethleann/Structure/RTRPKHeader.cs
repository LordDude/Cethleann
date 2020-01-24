namespace Cethleann.Structure
{
#pragma warning disable 1591
    public struct RTRPKHeader
    {
        public DataType Magic { get; set; }
        public int LongMagic { get; set; }
        public int Version { get; set; }
        public int HeaderSize { get; set; }
        public int FileSize { get; set; }
        public int PointerCount { get; set; }
        public int SizeCount { get; set; }
        public int Unknown { get; set; }
        public int PointerTablePointer { get; set; }
        public int SizeTablePointer { get; set; }
    }
#pragma warning restore 1591
}
