namespace PetaPoco
{
    using System;

    [Flags]
    public enum SqlBehaviorFlags
    {
        All = 3,
        Insert = 1,
        Update = 2
    }
}

