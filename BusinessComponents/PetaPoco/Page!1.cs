namespace PetaPoco
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class Page<T>
    {
        public object Context { get; set; }

        public long CurrentPage { get; set; }

        public List<T> Items { get; set; }

        public long ItemsPerPage { get; set; }

        public long TotalItems { get; set; }

        public long TotalPages { get; set; }
    }
}

