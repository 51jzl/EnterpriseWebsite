namespace PetaPoco
{
    using System;
    using System.Runtime.CompilerServices;

    public class AnsiString
    {
        public AnsiString(string str)
        {
            this.Value = str;
        }

        public string Value { get; private set; }
    }
}

