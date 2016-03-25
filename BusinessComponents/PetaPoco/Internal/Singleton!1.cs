namespace PetaPoco.Internal
{
    using System;

    internal static class Singleton<T> where T: new()
    {
        public static T Instance;

        static Singleton()
        {
            Singleton<T>.Instance = (default(T) == null) ? Activator.CreateInstance<T>() : default(T);
        }
    }
}

