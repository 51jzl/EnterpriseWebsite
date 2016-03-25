namespace PetaPoco
{
    using System;

    public interface ITransaction : IDisposable
    {
        void Complete();
    }
}

