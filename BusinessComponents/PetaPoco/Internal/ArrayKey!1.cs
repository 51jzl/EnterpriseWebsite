namespace PetaPoco.Internal
{
    using System;

    internal class ArrayKey<T>
    {
        private int _hashCode;
        private T[] _keys;

        public ArrayKey(T[] keys)
        {
            this._keys = keys;
            this._hashCode = 0x11;
            foreach (T local in keys)
            {
                this._hashCode = (this._hashCode * 0x17) + ((local == null) ? 0 : local.GetHashCode());
            }
        }

        private bool Equals(ArrayKey<T> other)
        {
            if (other == null)
            {
                return false;
            }
            if (other._hashCode != this._hashCode)
            {
                return false;
            }
            if (other._keys.Length != this._keys.Length)
            {
                return false;
            }
            for (int i = 0; i < this._keys.Length; i++)
            {
                if (!object.Equals(this._keys[i], other._keys[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ArrayKey<T>);
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }
    }
}

