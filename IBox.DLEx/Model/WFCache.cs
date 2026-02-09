using IBox.Common.Objects;

namespace IBox.DLEx.Model
{
    internal class WFCache : IBoxDisposable
    {
        private Type? objType;
        private object? obj;

        public Type? ObjType { get => objType; set => objType = value; }
        public object? Obj { get => obj; set => obj = value; }
    }
}